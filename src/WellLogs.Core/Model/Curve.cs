using System;

namespace WellLogs.Core.Model
{
    public sealed class Curve
    {
        public Curve(string mnemonic, string? unit, string? description, double[] values)
        {
            if (string.IsNullOrWhiteSpace(mnemonic))
            {
                throw new ArgumentException("Mnemonic is required.", nameof(mnemonic));
            }

            Mnemonic = mnemonic.Trim();
            Unit = unit?.Trim() ?? string.Empty;
            Description = description?.Trim() ?? string.Empty;
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public string Mnemonic { get; }
        public string Unit { get; }
        public string Description { get; }
        public double[] Values { get; }
        public int Length => Values.Length;

        public override string ToString()
        {
            return Mnemonic + " [" + Unit + "] x" + Length; 
        }
    }
}