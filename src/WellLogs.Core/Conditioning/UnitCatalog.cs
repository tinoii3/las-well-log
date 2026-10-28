using System.Collections.Generic;
using System.Text;

namespace WellLogs.Core.Conditioning
{
    public static class UnitCatalog
    {
        private static HashSet<string> Set(params string[] items) { return new HashSet<string>(items); }

        private static readonly HashSet<string> Feet = Set("F", "FT", "FEET", "FOOT");
        private static readonly HashSet<string> Meters = Set("M", "METER", "METERS", "METRE", "METRES");
        private static readonly HashSet<string> Gamma = Set("GAPI", "API");
        private static readonly HashSet<string> Ohm = Set("OHMM", "OHM-M", "OHM.M", "OHM*M", "OHMS", "OHM");
        private static readonly HashSet<string> Conduct = Set("MM/M", "MMHO/M", "MS/M");
        private static readonly HashSet<string> DensityGcc = Set("G/C3", "G/CC", "GM/CC", "G/CM3", "GM/C3", "GM/CM3");
        private static readonly HashSet<string> DensityKg = Set("KG/M3");
        private static readonly HashSet<string> Percent = Set("%", "PU", "PERCENT", "PCT");
        private static readonly HashSet<string> Fraction = Set("V/V", "CFCF", "FRAC", "DECI", "M3/M3", "FT3/FT3", "CF/CF", "FRACTION");

        public static string Normalize(string unit)
        {
            if (unit == null || unit.Length == 0)
            {
                return string.Empty;
            }
            var sb = new StringBuilder(unit.Length);
            foreach (char ch in unit)
            {
                if (char.IsWhiteSpace(ch))
                {
                    continue;
                }
                sb.Append(ch == '\u00B3' ? '3' : char.ToUpperInvariant(ch));
            }
            return sb.ToString();
        }

        public static bool IsFeet(string u) { return Feet.Contains(u); }
        public static bool IsMeters(string u) { return Meters.Contains(u); }
        public static bool IsGamma(string u) { return Gamma.Contains(u); }
        public static bool IsResistivity(string u) { return Ohm.Contains(u); }
        public static bool IsConductivity(string u) { return Conduct.Contains(u); }
        public static bool IsDensityGcc(string u) { return DensityGcc.Contains(u); }
        public static bool IsDensityKgM3(string u) { return DensityKg.Contains(u); }
        public static bool IsPercent(string u) { return Percent.Contains(u); }
        public static bool IsFraction(string u) { return Fraction.Contains(u); }
    }
}