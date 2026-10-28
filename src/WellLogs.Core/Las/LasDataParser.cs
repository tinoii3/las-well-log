using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using WellLogs.Core.Model;

namespace WellLogs.Core.Las
{
    public sealed class DataParseResult
    {
        public DataParseResult(double[][] columns, int rowCount)
        {
            Columns = columns;
            RowCount = rowCount;
        }

        public double[][] Columns { get; }
        public int RowCount { get;}
    }

    public static class LasDataParser
    {
        private static readonly char[] Whitespace = { ' ', '\t' };

        public static DataParseResult Parse(TextReader reader, int columnCount, string delimiter, bool wrapped, int capacityHint, List<Issue> issues)
        {
            if (columnCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnCount));
            }

            bool comma = string.Equals(delimiter, "COMMA", StringComparison.OrdinalIgnoreCase);
            var sink = new RowSink(columnCount, capacityHint);
            var row = new double[columnCount];
            int col = 0, bad = 0, skipped = 0;
            bool stream = wrapped;
            bool first = true;
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                string t = line.Trim();
                if (t.Length == 0 || t[0] == '#')
                {
                    continue;
                }
                if (t[0] == '~')
                {
                    break;
                }

                string[] tokens = comma ? t.Split(',') : t.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);

                if (first)
                {
                    first = false;
                    if (!stream && tokens.Length == 1 && columnCount > 1)
                    {
                        stream = true;
                        issues.Add(new Issue(IssueSeverity.Warning, "WRAP_AUTODETECTED", "Header says WRAP=NO but data looks wrapped; read as wrapped."));
                    }
                }

                if (!stream)
                {
                    if (tokens.Length != columnCount)
                    {
                        skipped++;
                        continue;
                    }

                    for (int i = 0; i < columnCount; i++)
                    {
                        row[i] = ParseToken(tokens[i], ref bad);
                    }

                    sink.Add(row);
                }
                else
                {
                    foreach (string tok in tokens)
                    {
                        row[col++] = ParseToken(tok, ref bad);
                        if (col == columnCount)
                        {
                            sink.Add(row);
                            col = 0;
                        }
                    }
                }
            }

            if (col != 0)
            {
                issues.Add(new Issue(IssueSeverity.Warning, "DATA_INCOMPLETE_ROW", "Last row is incomplete (" + col + " of " + columnCount + " values); dropped."));
            }

            if (skipped > 0)
            {
                issues.Add(new Issue(IssueSeverity.Warning, "ROW_COLUMN_MISMATCH", skipped + " data line(s) did not have " + columnCount + " columns and were skipped."));
            }

            if (bad > 0)
            {
                issues.Add(new Issue(IssueSeverity.Warning, "BAD_NUMBER_TOKENS", bad + " non-numeric value(s) in data section were treated as missing."));
            }

            return new DataParseResult(sink.Finish(), sink.Rows);
        }

        private static double ParseToken(string tok, ref int bad)
        {
            string s = tok.Trim();
            if (s.Length == 0)
            {
                return double.NaN;
            }

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
            {
                return double.IsInfinity(v) ? double.NaN : v;
            }

            bad++;
            return double.NaN;
        }

        private sealed class RowSink
        {
            private readonly double[][] _cols;
            private int _capacity;

            public RowSink(int columnCount, int capacityHint)
            {
                int maxRows = Math.Max(16, 4000000 / columnCount);
                _capacity = Math.Max(16, Math.Min(capacityHint, maxRows));
                _cols = new double[columnCount][];

                for (int i = 0; i < columnCount; i++)
                {
                    _cols[i] = new double[_capacity];
                }
            }

            public int Rows { get; private set; }

            public void Add(double[] row)
            {
                if (Rows == _capacity)
                {
                    _capacity *= 2;
                    for (int i = 0; i < _cols.Length; i++)
                    {
                        Array.Resize(ref _cols[i], _capacity);
                    }
                }

                for (int i = 0; i < _cols.Length; i++)
                {
                    _cols[i][Rows] = row[i];
                }

                Rows++;
            }

            public double[][] Finish()
            {
                for (int i = 0; i < _cols.Length; i++)
                {
                    Array.Resize(ref _cols[i], Rows);
                }

                return _cols;
            }
        }
    }
}