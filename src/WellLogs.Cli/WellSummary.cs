using System.Collections.Generic;
using WellLogs.Core.Conditioning;
using WellLogs.Core.Model;

namespace WellLogs.Cli
{
    internal sealed class WellSummary
    {
        public string File { get; set; } = string.Empty;
        public string WellName { get; set; } = string.Empty;
        public LoadStatus Status { get; set; }
        public int RawSamples { get; set; }
        public int RawCurves { get; set; }
        public int Samples { get; set; }
        public double? DepthTop { get; set; }
        public double? DepthBottom { get; set; }
        public string PlotFile { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;

        public List<Issue> LoadIssues { get; } = new List<Issue>();
        public List<Issue> ConditioningIssues { get; } = new List<Issue>();
        public Dictionary<CurveKind, string> Selected { get; } = new Dictionary<CurveKind, string>();

        public int Count(IssueSeverity severity)
        {
            int n = 0;
            foreach (Issue i in LoadIssues)
            {
                if (i.Severity == severity)
                {
                    n++;
                }
            }

            foreach (Issue i in ConditioningIssues)
            {
                if (i.Severity == severity)
                {
                    n++;
                }
            }
            return n;
        }

        public string SelectedOrDash(CurveKind kind)
        {
            return Selected.TryGetValue(kind, out string? m) ? m : "-";
        }
    }
}