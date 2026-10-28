using System;
using System.Collections.Generic;
using System.Linq;
using WellLogs.Core.Model;

namespace WellLogs.Core.Conditioning
{
    public static class WellConditioner
    {
        private static readonly CurveKind[] Wanted = { CurveKind.GammaRay, CurveKind.Resistivity, CurveKind.BulkDensity, CurveKind.NeutronPorosity };

        public static ConditionedWell Condition(Well well, string sourceFile)
        {
            var report = new ConditioningReport();
            try
            {
                return Run(well, sourceFile, report);
            }
            catch (Exception ex)
            {
                report.Error("CONDITIONING_FAILED", ex.GetType().Name + ": " + ex.Message);
                return Empty(well, sourceFile, report);
            }
        }

        private static ConditionedWell Run(Well well, string sourceFile, ConditioningReport report)
        {
            double[] sentinels = NullHandler.BuildSentinelSet(well.DeclaredNull, report);
            var scored = new List<Scored>(well.Curves.Count);

            foreach (Curve c in well.Curves)
            {
                scored.Add(new Scored(c, CurveClassifier.Classify(c.Mnemonic, c.Unit, c.Description)));
            }

            var picked = new Dictionary<CurveKind, Scored>();
            foreach (CurveKind kind in Wanted)
            {
                Scored? p = Pick(scored, kind, sentinels, report);
                if (p != null)
                {
                    picked[kind] = p;
                }
            }

            if (!picked.ContainsKey(CurveKind.Resistivity))
            {
                Scored? cond = Pick(scored, CurveKind.Conductivity, sentinels, report);
                if (cond != null)
                {
                    picked[CurveKind.Resistivity] = cond;
                }
            }

            foreach (CurveKind kind in Wanted)
                if (!picked.ContainsKey(kind))
                {
                    report.Warn("CURVE_MISSING", kind + ": no suitable curve found; this track will be empty.");
                }

            double[] depthClean = (double[])well.Depth.Clone();
            NullHandler.Apply(depthClean, sentinels);
            DepthResult dr = DepthNormalizer.Normalize(depthClean, well.DeclaredStep, report);
            if (dr.Rows.Length == 0)
            {
                report.Error("NO_VALID_DEPTH", "No rows with a valid depth.");
                return Empty(well, sourceFile, report);
            }

            double[] depth = dr.Depth;
            double factor = UnitConverter.DepthToMetersFactor(well.DepthUnit, report);
            if (factor != 1.0)
            {
                for (int i = 0; i < depth.Length; i++)
                {
                    depth[i] *= factor;
                }
            }

            var converted = new Dictionary<CurveKind, Curve>();
            foreach (KeyValuePair<CurveKind, Scored> pair in picked)
            {
                CurveKind kind = pair.Key;
                Curve src = pair.Value.Curve;

                double[] v = ArrayOps.Gather(src.Values, dr.Rows);
                int nulls = NullHandler.Apply(v, sentinels);
                if (nulls > 0)
                {
                    report.Info("NULLS_SET_MISSING", src.Mnemonic + ": " + nulls + " null/sentinel value(s) set to missing.");
                }

                string unit;
                string description = src.Description;
                switch (kind)
                {
                    case CurveKind.GammaRay:
                        unit = "GAPI";
                        break;

                    case CurveKind.Resistivity:
                        if (pair.Value.Kind == CurveKind.Conductivity)
                        {
                            UnitConverter.ConductivityToResistivity(v, src.Mnemonic, report);
                            description = "Derived from conductivity: " + src.Description;
                        }
                        unit = "OHMM";
                        break;

                    case CurveKind.BulkDensity:
                        UnitConverter.ToGramPerCc(v, src.Unit, src.Mnemonic, report);
                        unit = "G/C3";
                        break;

                    default:
                        UnitConverter.ToFraction(v, src.Unit, src.Mnemonic, report);
                        unit = "V/V";
                        break;
                }

                int outOfRange = PhysicalLimits.Apply(kind, v);
                if (outOfRange > 0)
                {
                    PhysicalLimits.TryGet(kind, out double min, out double max);
                    report.Warn("OUT_OF_RANGE_SET_MISSING", src.Mnemonic + ": " + outOfRange + " value(s) outside the physical range [" + ArrayOps.Fmt(min) + ", " + ArrayOps.Fmt(max) + "] set to missing.");
                }

                converted[kind] = new Curve(src.Mnemonic, unit, description, v);
            }

            Dictionary<CurveKind, Curve> finalCurves = converted;
            int n = depth.Length;
            if (converted.Count > 0)
            {
                int first = 0;
                int last = n - 1;
                while (first <= last && AllMissing(converted.Values, first))
                {
                    first++;
                }

                if (first > last)
                {
                    report.Warn("NO_VALID_DATA", "All selected curves are empty after cleaning.");
                    return new ConditionedWell(well.Name, sourceFile, new double[0], new Dictionary<CurveKind, Curve>(), report);
                }
                
                while (last > first && AllMissing(converted.Values, last))
                {
                    last--;
                }

                if (first > 0 || last < n - 1)
                {
                    int length = last - first + 1;
                    finalCurves = new Dictionary<CurveKind, Curve>();
                    foreach (KeyValuePair<CurveKind, Curve> kv in converted)
                    {
                        Curve c = kv.Value;
                        finalCurves[kv.Key] = new Curve(c.Mnemonic, c.Unit, c.Description, ArrayOps.Slice(c.Values, first, length));
                    }
                    depth = ArrayOps.Slice(depth, first, length);
                    report.Info("TRIMMED_ENDS", "Removed " + first + " leading and " + (n - 1 - last) + " trailing row(s) with no data in any selected curve.");
                }
            }

            foreach (KeyValuePair<CurveKind, Curve> kv in finalCurves)
            {
                ReportFlatRun(kv.Value, depth, report);
            }

            return new ConditionedWell(well.Name, sourceFile, depth, finalCurves, report);
        }

        private static Scored? Pick(List<Scored> all, CurveKind kind, double[] sentinels, ConditioningReport report)
        {
            List<Scored> candidates = all.Where(s => s.Kind == kind).OrderByDescending(s => s.Score).ToList();
            foreach (Scored c in candidates)
            {
                if (NullHandler.CountValid(c.Curve.Values, sentinels) == 0)
                {
                    report.Info("CURVE_SKIPPED_EMPTY", c.Curve.Mnemonic + ": contains no valid values; next candidate tried.");
                    continue;
                }
                if (candidates.Count > 1)
                {
                    string others = string.Join(", ", candidates.Where(x => x != c).Select(x => x.Curve.Mnemonic));
                    report.Info("CURVE_SELECTED", kind + ": using " + c.Curve.Mnemonic + " (other candidates: " + others + ").");
                }
                return c;
            }
            return null;
        }

        private static bool AllMissing(IEnumerable<Curve> curves, int row)
        {
            foreach (Curve c in curves)
            {
                if (!double.IsNaN(c.Values[row]))
                {
                    return false;
                }
            }
            return true;
        }

        private static void ReportFlatRun(Curve c, double[] depth, ConditioningReport report)
        {
            double[] v = c.Values;
            int bestLen = 0, bestEnd = -1, runLen = 0;
            double runVal = double.NaN;

            for (int i = 0; i < v.Length; i++)
            {
                double x = v[i];
                if (double.IsNaN(x))
                {
                    runLen = 0;
                }
                else if (runLen > 0 && x == runVal)
                {
                    runLen++;
                }
                else 
                { 
                    runVal = x; 
                    runLen = 1; 
                }

                if (runLen > bestLen) 
                { 
                    bestLen = runLen; 
                    bestEnd = i; 
                }
            }

            int minRun = Math.Max(50, v.Length / 20);
            if (bestLen >= minRun)
            {
                report.Warn("FLAT_RUN", c.Mnemonic + ": " + bestLen + " consecutive identical values (" + ArrayOps.Fmt(v[bestEnd]) +
                    ") from depth " + ArrayOps.Fmt(depth[bestEnd - bestLen + 1]) + " to " + ArrayOps.Fmt(depth[bestEnd]) +
                    " m; possible tool saturation or dead channel. Data left unchanged.");
            }
        }

        private static ConditionedWell Empty(Well well, string sourceFile, ConditioningReport report)
        {
            return new ConditionedWell(well.Name, sourceFile, new double[0], new Dictionary<CurveKind, Curve>(), report);
        }

        private sealed class Scored
        {
            public Scored(Curve curve, CurveScore result)
            {
                Curve = curve;
                Kind = result.Kind;
                Score = result.Score;
            }

            public Curve Curve { get; }
            public CurveKind Kind { get; }
            public double Score { get; }
        }
    }
}