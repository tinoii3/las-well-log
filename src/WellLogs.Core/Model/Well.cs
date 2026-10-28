using System;
using System.Collections.Generic;

namespace WellLogs.Core.Model
{
    public sealed class Well
    {
        private readonly List<Curve> _curves = new List<Curve>();

        public Well(string name, double[] depth, string? depthUnit)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name.Trim();
            Depth = depth ?? throw new ArgumentNullException(nameof(depth));
            DepthUnit = depthUnit?.Trim() ?? string.Empty;
        }

        public string Name { get; }
        public double[] Depth { get; }
        public string DepthUnit { get; }
        public int SampleCount => Depth.Length;
        public double? DeclaredNull { get; set; }
        public double? DeclaredStep { get; set; }
        public IReadOnlyList<Curve> Curves => _curves;
        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void AddCurve(Curve curve)
        {
            if (curve == null)
            {
                throw new ArgumentNullException(nameof(curve));
            }

            if (curve.Length != Depth.Length)
            {
                throw new ArgumentException("Curve " + curve.Mnemonic + " has " + curve.Length + " samples but depth has " + Depth.Length + ".", nameof(curve));
            }
            _curves.Add(curve);
        }

        public Curve? FindCurve(string mnemonic)
        {
            foreach (Curve c in _curves)
            {
                if (string.Equals(c.Mnemonic, mnemonic, StringComparison.OrdinalIgnoreCase))
                {
                    return c;
                }
            }
            return null;
        }
    }
}