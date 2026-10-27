namespace WellLogs.Core.Las
{
    public sealed class HeaderLine
    {
        public HeaderLine(string mnemonic, string unit, string value, string description)
        {
            Mnemonic = mnemonic;
            Unit = unit;
            Value = value;
            Description = description;
        }

        public string Mnemonic { get; }
        public string Unit { get; }
        public string Value { get; }
        public string Description { get; }
    }
}