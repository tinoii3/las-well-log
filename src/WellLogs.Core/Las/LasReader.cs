using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WellLogs.Core.Model;

namespace WellLogs.Core.Las
{
    public static class LasReader
    {
        public static LoadResult Load(string path)
        {
            var issues = new List<Issue>();

            try
            {
                if (!File.Exists(path))
                {
                    return Fail(path, issues, "FILE_NOT_FOUND", "File does not exist.");
                }

                if (new FileInfo(path).Length == 0)
                {
                    Fail(path, issues, "EMPTY_FILE", "File is empty.");
                }

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan))
                using (var reader = new StreamReader(fs, Encoding.UTF8, true, 65536))
                {
                    LasHeader header = LasHeaderParser.Parse(reader, issues);

                    if (!header.DataSectionFound)
                    {
                        if (header.HasLas3Sections)
                        {
                            issues.Add(new Issue(IssueSeverity.Error, "LAS3_UNSUPPORTED", "True LAS 3.0 layout (~Log_* sections) is not supported."));
                            return LoadResult.Unsupported(path, issues);
                        }
                        return LoadResult.Failed(path, issues);
                    }

                    if (header.Curves.Count == 0)
                    {
                        return Fail(path, issues, "NO_CURVES", "No curves declared in ~Curve section.");
                    }

                    if (header.HasArrayCurves)
                    {
                        issues.Add(new Issue(IssueSeverity.Error, "ARRAY_CURVES_UNSUPPORTED", "File contains array/waveform curves (multiple values per depth); " + "column count cannot be determined reliably, so it is not read."));
                        return LoadResult.Unsupported(path, issues);
                    }

                    if (header.Version.HasValue)
                    {
                        issues.Add(new Issue(IssueSeverity.Info, "VERS_MISSING", "No VERS line; assumed LAS 2.0 layout."));
                    }

                    DataParseResult data = LasDataParser.Parse( reader, header.Curves.Count, header.Delimiter, header.Wrapped, EstimateRows(header), issues);

                    if (data.RowCount == 0)
                    {
                        return Fail(path, issues, "NO_DATA_ROWS", "Data section contains no complete rows.");
                    }

                    string name = header.WellName.Length > 0 ? header.WellName : Path.GetFileNameWithoutExtension(path);

                    var well = new Well(name, data.Columns[0], header.DepthUnit);
                    well.DeclaredNull = header.NullValue;
                    foreach (KeyValuePair<string, string> p in header.WellProperties) well.Properties[p.Key] = p.Value;

                    for (int i = 1; i < header.Curves.Count; i++)
                    {
                        CurveDefinition d = header.Curves[i];
                        well.AddCurve(new Curve(d.Mnemonic, d.Unit, d.Description, data.Columns[i]));
                    }

                    return LoadResult.Success(path, well, issues);
                }
            }
            catch (Exception ex)
            {
                issues.Add(new Issue(IssueSeverity.Error, "UNEXPECTED_ERROR", ex.GetType().Name + ": " + ex.Message));
                return LoadResult.Failed(path, issues);
            }
        }

        private static LoadResult Fail(string path, List<Issue> issues, string code, string message)
        {
            issues.Add(new Issue(IssueSeverity.Error, code, message));
            return LoadResult.Failed(path, issues);
        }

        private static int EstimateRows(LasHeader h)
        {
            if (h.Start.HasValue && h.Stop.HasValue && h.Step.HasValue && h.Step.Value != 0)
            {
                double est = Math.Abs((h.Stop.Value - h.Start.Value) / h.Step.Value) + 1;
                if (!double.IsNaN(est) && est > 0 && est < 5000000)
                {
                    return (int)est + 1;
                }
            }
            return 4096;
        }
    }
}