using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WellLogs.Core.Conditioning;
using WellLogs.Core.Model;

namespace WellLogs.Cli
{
    internal static class ReportWriter
    {
        public static void WriteMarkdown(string path, IReadOnlyList<WellSummary> items, TimeSpan elapsed)
        {
            using (var w = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                w.WriteLine("# Conditioning report");
                w.WriteLine();
                w.WriteLine("Files processed: " + items.Count + " in " + elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");

                foreach (var g in items.GroupBy(i => i.Status).OrderBy(g => g.Key))
                {
                    w.WriteLine("- " + g.Key + ": " + g.Count());
                }

                w.WriteLine("- Plots written: " + items.Count(i => i.PlotFile.Length > 0));
                w.WriteLine();

                w.WriteLine("## Summary");
                w.WriteLine();
                w.WriteLine("| File | Status | Samples | GR | Resistivity | Density | Neutron | Warnings | Errors | Plot |");
                w.WriteLine("|---|---|---|---|---|---|---|---|---|---|");
                
                foreach (WellSummary s in Sorted(items))
                {
                    w.WriteLine("| " + Cell(s.File) + " | " + s.Status + " | " + s.Samples + " | " +
                        s.SelectedOrDash(CurveKind.GammaRay) + " | " + s.SelectedOrDash(CurveKind.Resistivity) + " | " +
                        s.SelectedOrDash(CurveKind.BulkDensity) + " | " + s.SelectedOrDash(CurveKind.NeutronPorosity) + " | " +
                        s.Count(IssueSeverity.Warning) + " | " + s.Count(IssueSeverity.Error) + " | " +
                        (s.PlotFile.Length > 0 ? s.PlotFile : "-") + " |");
                }
                w.WriteLine();

                w.WriteLine("## Details per file");
                foreach (WellSummary s in Sorted(items))
                {
                    w.WriteLine();
                    w.WriteLine("### " + s.File);
                    w.WriteLine();
                    w.WriteLine("- Load status: " + s.Status);

                    if (s.WellName.Length > 0)
                    {
                        w.WriteLine("- Well: " + s.WellName);
                    }

                    if (s.RawSamples > 0)
                    {
                        w.WriteLine("- Raw: " + s.RawSamples + " rows, " + s.RawCurves + " curves (excluding depth)");
                    }
              
                    if (s.DepthTop.HasValue && s.DepthBottom.HasValue)
                    {
                        w.WriteLine("- After conditioning: " + s.Samples + " rows, " + Num(s.DepthTop.Value) + " to " + Num(s.DepthBottom.Value) + " m");
                    }
                        
                    if (s.Note.Length > 0)
                    {
                        w.WriteLine("- Note: " + s.Note);
                    }

                    WriteIssues(w, "Load issues", s.LoadIssues);
                    WriteIssues(w, "Conditioning changes and observations", s.ConditioningIssues);
                }
            }
        }

        public static void WriteCsv(string path, IReadOnlyList<WellSummary> items)
        {
            using (var w = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                w.WriteLine("file,well,status,raw_rows,raw_curves,rows,depth_top_m,depth_bottom_m,gr,resistivity,density,neutron,warnings,errors,plot,note");
                foreach (WellSummary s in Sorted(items))
                {
                    w.WriteLine(string.Join(",", new[]
                    {
                        Csv(s.File), Csv(s.WellName), s.Status.ToString(), s.RawSamples.ToString(CultureInfo.InvariantCulture),
                        s.RawCurves.ToString(CultureInfo.InvariantCulture), s.Samples.ToString(CultureInfo.InvariantCulture),
                        s.DepthTop.HasValue ? Num(s.DepthTop.Value) : "", s.DepthBottom.HasValue ? Num(s.DepthBottom.Value) : "",
                        Csv(s.SelectedOrDash(CurveKind.GammaRay)), Csv(s.SelectedOrDash(CurveKind.Resistivity)),
                        Csv(s.SelectedOrDash(CurveKind.BulkDensity)), Csv(s.SelectedOrDash(CurveKind.NeutronPorosity)),
                        s.Count(IssueSeverity.Warning).ToString(CultureInfo.InvariantCulture),
                        s.Count(IssueSeverity.Error).ToString(CultureInfo.InvariantCulture),
                        Csv(s.PlotFile), Csv(s.Note)
                    }));
                }
            }
        }

        private static IEnumerable<WellSummary> Sorted(IReadOnlyList<WellSummary> items)
        {
            return items.OrderBy(i => i.File, StringComparer.OrdinalIgnoreCase);
        }

        private static void WriteIssues(StreamWriter w, string title, List<Issue> issues)
        {
            if (issues.Count == 0)
            {
                return;
            }

            w.WriteLine();
            w.WriteLine(title + ":");
            w.WriteLine();
            foreach (Issue i in issues)
            {
                w.WriteLine("- [" + i.Severity + "] `" + i.Code + "`: " + i.Message + (i.LineNumber.HasValue ? " (line " + i.LineNumber.Value + ")" : ""));
            }
        }

        private static string Num(double v)
        {
            return v.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string Cell(string s)
        {
            return s.Replace("|", "\\|");
        }

        private static string Csv(string s)
        {
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            {
                return s;
            }

            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
}