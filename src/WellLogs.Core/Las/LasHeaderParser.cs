using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using WellLogs.Core.Model;

namespace WellLogs.Core.Las
{
    public static class LasHeaderParser
    {
        public static LasHeader Parse(TextReader reader, List<Issue> issues)
        {
            var header = new LasHeader();
            char section = '?';
            int lineNo = 0;
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                lineNo++;
                string t = line.TrimStart();

                if (t.Length == 0 || t[0] == '#')
                {
                    continue;
                }

                if (t[0] == '~')
                {
                    section = ClassifySection(t, header);

                    if (section == 'A')
                    {
                        header.DataSectionFound = true;
                        break;
                    }

                    continue;
                }

                HeaderLine? h;
                switch (section)
                {
                    case 'V':
                        h = LasLineParser.TryParse(line);
                        if (h != null) ApplyVersionLine(header, h);
                        break;

                    case 'W':
                        h = LasLineParser.TryParse(line);
                        if (h == null)
                        {
                            issues.Add(new Issue(IssueSeverity.Warning, "WELL_LINE_UNREADABLE", "Cannot Parse well-information line: " + Truncate(line), lineNo));
                        }
                        else
                        {
                            ApplyWellLine(header, h);
                        }
                        break;

                    case 'C':
                        h = LasLineParser.TryParse(line);
                        if (h == null)
                        {
                            issues.Add(new Issue(IssueSeverity.Warning, "CURVE_LINE_UNREADABLE", "Cannot parse curve-information line: " + Truncate(line), lineNo));
                        }
                        else
                        {
                            header.Curves.Add(new CurveDefinition(h.Mnemonic, h.Unit, h.Description, IsArrayCurve(h)));
                        }

                        break;

                    default:
                        break;
                }
            }

            if (!header.DataSectionFound)
            {
                issues.Add(new Issue(IssueSeverity.Error, "NO_DATA_SECTION", "No ~A data section found."));
            }

            if (header.Version.HasValue && header.Version.Value >= 3.0 && !header.HasLas3Sections)
            {
                issues.Add(new Issue(IssueSeverity.Info, "VERS_STRUCTURE_MISMATCH","File declares VERS 3.0 but uses LAS 2.0 section layout; read as 2.0."));
            }

            return header;
        }

        private static char ClassifySection(string t, LasHeader header)
        {
            string name = t.Substring(1);
            int bar = name.IndexOf('|');

            if (bar >= 0)
            {
                name = name.Substring(0, bar);
            }

            name = name.Trim();
            if (name.Length == 0)
            {
                return '?';
            }

            if (name.StartsWith("LOG", StringComparison.OrdinalIgnoreCase))
            {
                header.HasLas3Sections = true;
                return 'L';
            }

            char c = char.ToUpperInvariant(name[0]);
            return "VWCPOA".IndexOf(c) >= 0 ? c : '?';
        }

        private static void ApplyVersionLine(LasHeader header, HeaderLine h)
        {
            switch (h.Mnemonic.ToUpperInvariant())
            {
                case "VERS":
                    header.Version = ParseDouble(h.Value);
                    break;
                case "WRAP":
                    header.Wrapped = h.Value.StartsWith("Y", StringComparison.OrdinalIgnoreCase);
                    break;
                case "DLM":
                    if (h.Value.Length > 0)
                    {
                        header.Delimiter = h.Value.ToUpperInvariant();
                    }
                    break;
            }
        }

        private static void ApplyWellLine(LasHeader header, HeaderLine h)
        {
            string value = HeaderValueResolver.Resolve(h.Mnemonic, h.Value, h.Description);

            switch (h.Mnemonic.ToUpperInvariant())
            {
                case "STRT":
                    header.Start = ParseDouble(value);
                    header.StartUnit = h.Unit;
                    break;
                case "STOP":
                    header.Stop = ParseDouble(value);
                    break;
                case "STEP":
                    header.Step = ParseDouble(value);
                    break;
                case "NULL":
                    header.NullValue = ParseDouble(value);
                    break;
                default:
                    header.WellProperties[h.Mnemonic] = value;
                    break;
            }
        }

        private static bool IsArrayCurve(HeaderLine h)
        {
            return h.Mnemonic.IndexOf('[') >= 0 || h.Description.IndexOf("{A", StringComparison.Ordinal) >= 0;
        }

        private static double? ParseDouble(string s)
        {
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                ? d
                : (double?)null;
        }

        private static string Truncate(string s)
        {
            s = s.Trim();
            return s.Length <= 80 ? s : s.Substring(0, 80) + "...";
        }
    }
}