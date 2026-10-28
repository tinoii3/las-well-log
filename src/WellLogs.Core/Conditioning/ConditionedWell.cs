using System.Collections.Generic;
using WellLogs.Core.Model;

namespace WellLogs.Core.Conditioning
{
    public sealed class ConditionedWell
    {
        public ConditionedWell(string name, string sourceFile, double[] depth, IReadOnlyDictionary<CurveKind, Curve> curves, ConditioningReport report)
        {
            Name = name;
            SourceFile = sourceFile;
            Depth = depth;
            Curves = curves;
            Report = report;
        }

        public string Name { get; }
        public string SourceFile { get; }
        public double[] Depth { get; }
        public string DepthUnit => "m";
        public IReadOnlyDictionary<CurveKind, Curve> Curves { get; }
        public ConditioningReport Report { get; }

        public Curve? Get(CurveKind kind)
        {
            return Curves.TryGetValue(kind, out Curve? c) ? c : null;
        }
    }
}