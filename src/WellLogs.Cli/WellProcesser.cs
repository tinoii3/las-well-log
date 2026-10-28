using System;
using System.Collections.Generic;
using System.IO;
using WellLogs.Core.Conditioning;
using WellLogs.Core.Las;
using WellLogs.Core.Model;
using WellLogs.Plotting;

namespace WellLogs.Cli
{
    internal sealed class NameRegistry
    {
        private readonly HashSet<string> _used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public string Unique(string stem)
        {
            foreach (char bad in Path.GetInvalidFileNameChars())
            {
                stem = stem.Replace(bad, '_');
            }

            if (stem.Length == 0)
            {
                stem = "well";
            }

            lock (_used)
            {
                string name = stem;
                int k = 2;
                while (!_used.Add(name))
                {
                    name = stem + "_" + k++;
                }

                return name;
            }
        }
    }

    internal static class WellProcessor
    {
        private static readonly CurveKind[] Plotted = { CurveKind.GammaRay, CurveKind.Resistivity, CurveKind.BulkDensity, CurveKind.NeutronPorosity };

        public static WellSummary Process(string path, string plotDir, NameRegistry names)
        {
            var s = new WellSummary { File = Path.GetFileName(path) };

            LoadResult lr = LasReader.Load(path);
            s.Status = lr.Status;
            s.LoadIssues.AddRange(lr.Issues);

            Well? well = lr.Well;
            if (well == null)
            {
                s.Note = "not plotted: file could not be loaded";
                return s;
            }

            s.WellName = well.Name;
            s.RawSamples = well.SampleCount;
            s.RawCurves = well.Curves.Count;

            ConditionedWell cw = WellConditioner.Condition(well, path);
            s.ConditioningIssues.AddRange(cw.Report.Entries);
            s.Samples = cw.Depth.Length;

            if (cw.Depth.Length > 0)
            {
                s.DepthTop = cw.Depth[0];
                s.DepthBottom = cw.Depth[cw.Depth.Length - 1];
            }

            foreach (CurveKind kind in Plotted)
            {
                Curve? c = cw.Get(kind);
                if (c != null)
                {
                    s.Selected[kind] = c.Mnemonic;
                }
            }

            if (cw.Depth.Length == 0 || cw.Curves.Count == 0)
            {
                s.Note = "not plotted: no plottable curves after conditioning";
                return s;
            }

            try
            {
                string name = names.Unique(Path.GetFileNameWithoutExtension(path));
                WellLogPngWriter.Save(cw, Path.Combine(plotDir, name + ".png"));
                s.PlotFile = name + ".png";
            }
            catch (Exception ex)
            {
                s.Note = "plot failed: " + ex.GetType().Name + ": " + ex.Message;
            }

            return s;
        }
    }
}