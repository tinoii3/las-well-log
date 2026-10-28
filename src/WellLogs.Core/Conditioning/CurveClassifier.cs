using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WellLogs.Core.Conditioning
{
    public sealed class CurveScore
    {
        public CurveScore(CurveKind kind, double score)
        {
            Kind = kind;
            Score = score;
        }

        public CurveKind Kind { get; }
        public double Score { get; }
    }

    public static class CurveClassifier
    {
        public const double MinScore = 3.0;

        private static readonly string[] NotFormationResistivity = { "mud", "filtrate", "cake", "flushed", "invaded", "invasion", "water", "micro", "standoff", "diameter", "caliper", "sample" };
        private static readonly string[] NotDensity ={ "correction", "porosity", "por.", "standoff", "difference", "matrix", "fluid", "caliper", "delta" };
        private static readonly string[] NotNeutron ={ "delta", "difference", "count", "uncorrected" };
        
        private static readonly Dictionary<string, double> ResistivityNames = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            { { "ILD", 3 }, { "LLD", 3 }, { "RT", 3 }, { "RD", 3 }, { "RILD", 3 }, { "RLLD", 3 }, { "RESD", 3 }, { "AT90", 3 }, { "DDLL", 3 } };

        private static readonly Dictionary<string, double> DensityNames = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            { { "RHOB", 4 }, { "RHOZ", 3 }, { "DEN", 3 }, { "RHO8", 2 }, { "RHOL", 1 } };

        private static readonly Dictionary<string, double> NeutronNames =new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            { { "NPHI", 4 }, { "TNPH", 3 }, { "NPOR", 3 }, { "NPRL", 3 }, { "CNL", 3 }, { "HNPO", 2 } };

        private static readonly Regex InchRegex = new Regex(@"(\d+(?:\.\d+)?)\s*(?:in|inch)\b", RegexOptions.CultureInvariant);
        private static readonly Regex TrailingNumber = new Regex(@"^[A-Z_]+(\d{1,3})$", RegexOptions.CultureInvariant);

        public static CurveScore Classify(string mnemonic, string unit, string description)
        {
            string m = mnemonic.Trim().ToUpperInvariant();
            string u = UnitCatalog.Normalize(unit);
            string d = description.ToLowerInvariant();

            CurveScore best = new CurveScore(CurveKind.Other, 0);
            best = Better(best, CurveKind.GammaRay, ScoreGamma(m, u, d));
            best = Better(best, CurveKind.Resistivity, ScoreResistivityLike(m, u, d, false));
            best = Better(best, CurveKind.Conductivity, ScoreResistivityLike(m, u, d, true));
            best = Better(best, CurveKind.BulkDensity, ScoreDensity(m, u, d));
            best = Better(best, CurveKind.NeutronPorosity, ScoreNeutron(m, u, d));
            return best;
        }

        private static CurveScore Better(CurveScore current, CurveKind kind, double score)
        {
            return score >= MinScore && score > current.Score ? new CurveScore(kind, score) : current;
        }

        private static double ScoreGamma(string m, string u, string d)
        {
            double s = 0;
            if (UnitCatalog.IsGamma(u))
            {
                s += 3;
            }

            if (d.Contains("gamma"))
            {
                s += 3;
            }

            if (m == "GR")
            {
                s += 4;
            }
            else if (m.StartsWith("GR", StringComparison.Ordinal))
            {
                s += 2;
            }
            else if (m.EndsWith("GR", StringComparison.Ordinal))
            {
                s += 1;
            }

            if (ContainsAny(d, "thorium", "uranium", "potassium", "contribution"))
            {
                s -= 4;
            }
            return s;
        }

        private static double ScoreResistivityLike(string m, string u, string d, bool conductivity)
        {
            double s = 0;
            if (conductivity)
            {
                if (UnitCatalog.IsConductivity(u)) s += 4;
                if (d.Contains("conductivity")) s += 3;
            }
            else
            {
                if (UnitCatalog.IsResistivity(u)) s += 4;
                if (d.Contains("resistivity")) s += 2;
                s += NameBonus(ResistivityNames, m);
            }

            if (ContainsAny(d, NotFormationResistivity)) s -= 10;
            if (s > 0) s += InvestigationBonus(m, d);
            return s;
        }

        private static double InvestigationBonus(string m, string d)
        {
            double b = 0;
            if (d.Contains("deep"))
            {
                b += 3;
            }

            if (d.Contains("true"))
            {
                b += 3;
            }

            if (d.Contains("medium"))
            {
                b += 1;
            }

            if (d.Contains("shallow"))
            {
                b -= 1;
            }

            double reach = 0;
            Match inch = InchRegex.Match(d);
            if (inch.Success && double.TryParse(inch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double inches))
            {
                reach = inches;
            }

            Match tail = TrailingNumber.Match(m);
            if (tail.Success && int.TryParse(tail.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
            {
                reach = Math.Max(reach, n);
            }

            return b + Math.Min(reach, 90.0) / 30.0;
        }

        private static double ScoreDensity(string m, string u, string d)
        {
            double s = 0;
            if (UnitCatalog.IsDensityGcc(u) || UnitCatalog.IsDensityKgM3(u))
            {
                s += 3;
            }

            if (d.Contains("density"))
            {
                s += 2;
            }

            if (d.Contains("bulk") || d.Contains("formation density"))
            {
                s += 2;
            }

            s += NameBonus(DensityNames, m);
            if (ContainsAny(d, NotDensity))
            {
                s -= 10;
            }
            return s;
        }

        private static double ScoreNeutron(string m, string u, string d)
        {
            double s = 0;
            if (d.Contains("neutron"))
            {
                s += 3;
                if (d.Contains("por")) s += 2;
            }

            if (UnitCatalog.IsPercent(u) || UnitCatalog.IsFraction(u))
            {
                s += 1.5;
            }

            s += NameBonus(NeutronNames, m);
            if (ContainsAny(d, NotNeutron))
            {
                s -= 10;
            }
            return s;
        }

        private static double NameBonus(Dictionary<string, double> table, string m)
        {
            return table.TryGetValue(m, out double b) ? b : 0;
        }

        private static bool ContainsAny(string text, params string[] words)
        {
            foreach (string w in words)
            {
                if (text.Contains(w))
                {
                    return true;
                }
            }
            return false;
        }
    }
}