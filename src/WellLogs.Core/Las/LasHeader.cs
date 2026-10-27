using System;
using System.Collections.Generic;
using System.Linq;

namespace WellLogs.Core.Las
{
    public sealed class LasHeader
    {
        public double? Version { get; set; }
        public bool Wrapped { get; set; }
        public double? NullValue { get; set; }
        public double? Start { get; set; }
        public double? Stop { get; set; }
        public double? Step { get; set; }
        public string Delimiter { get; set; } = "SPACE";
        public string StartUnit { get; set; } = string.Empty;

        public bool HasLas3Sections { get; set; }
        public bool DataSectionFound { get; set; }
        public Dictionary<string, string> WellProperties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public List<CurveDefinition> Curves { get; } = new List<CurveDefinition>();
        public string WellName => WellProperties.TryGetValue("WELL", out string? v) && v.Length > 0 ? v : string.Empty;
        public string DepthUnit => Curves.Count > 0 && Curves[0].Unit.Length > 0 ? Curves[0].Unit : StartUnit;
        public bool HasArrayCurves => Curves.Any(c => c.IsArray);
    }
}