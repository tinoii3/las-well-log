namespace WellLogs.Core.Las
{
    public static class LasLineParser
    {
        public static HeaderLine? TryParse(string line)
        {
            return TryParse(line, false);
        }

        public static HeaderLine? TryParse(string line, bool lenient)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            string trimmed = line.TrimStart();
            if (trimmed[0] == '#' || trimmed[0] == '~')
            {
                return null;
            }

            int dot = trimmed.IndexOf('.');
            if (dot < 0)
            {
                return null;
            }

            if (!lenient && dot == 0)
            {
                return null;
            }

            string mnemonic = trimmed.Substring(0, dot).Trim();
            if (!lenient && (mnemonic.Length == 0 || mnemonic.IndexOf(' ') >= 0))
            {
                return null;
            }

            string rest = trimmed.Substring(dot + 1);

            int unitEnd = 0;
            while (unitEnd < rest.Length && !char.IsWhiteSpace(rest[unitEnd]))
            {
                unitEnd++;
            }

            string unit = rest.Substring(0, unitEnd);
            string afterUnit = rest.Substring(unitEnd);

            int colon = afterUnit.LastIndexOf(':');
            string value;
            string description;

            if (colon >= 0)
            {
                value = afterUnit.Substring(0, colon).Trim();
                description = afterUnit.Substring(colon + 1).Trim();
            }
            else
            {
                value = afterUnit.Trim();
                description = string.Empty;
            }

            return new HeaderLine(mnemonic, unit, value, description);
        }
    }
}