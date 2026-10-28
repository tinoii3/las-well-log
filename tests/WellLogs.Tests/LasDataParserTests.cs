using System.Collections.Generic;
using System.IO;
using WellLogs.Core.Las;
using WellLogs.Core.Model;

namespace WellLogs.Tests
{
    public class LasDataParserTests
    {
        private static DataParseResult Run(string text, int cols, string delim, bool wrapped, out List<Issue> issues)
        {
            issues = new List<Issue>();
            return LasDataParser.Parse(new StringReader(text), cols, delim, wrapped, 4, issues);
        }

        [Fact]
        public void Unwrapped_Rows_Are_Split_Into_Columns()
        {
            DataParseResult r = Run("1.0 10.0 100.0\n2.0 20.0 200.0\n3.0 30.0 300.0\n", 3, "SPACE", false, out var issues);

            Assert.Equal(3, r.RowCount);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, r.Columns[0]);
            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, r.Columns[1]);
            Assert.Equal(new[] { 100.0, 200.0, 300.0 }, r.Columns[2]);
            Assert.Empty(issues);
        }

        [Fact]
        public void Wrapped_Records_Are_Reassembled_Regardless_Of_Line_Breaks()
        {
            string text = "1000.0\n 50.0 10.0\n 7.0\n1000.5\n 51.0\n 11.0 8.0\n";

            DataParseResult r = Run(text, 4, "SPACE", true, out var issues);

            Assert.Equal(2, r.RowCount);
            Assert.Equal(new[] { 1000.0, 1000.5 }, r.Columns[0]);
            Assert.Equal(new[] { 10.0, 11.0 }, r.Columns[2]);
            Assert.Equal(new[] { 7.0, 8.0 }, r.Columns[3]);
            Assert.Empty(issues);
        }

        [Fact]
        public void Wrap_No_But_Data_Is_Wrapped_Is_Autodetected()
        {
            DataParseResult r = Run("1000.0\n 50.0 10.0\n", 3, "SPACE", false, out var issues);

            Assert.Equal(1, r.RowCount);
            Assert.Contains(issues, i => i.Code == "WRAP_AUTODETECTED");
        }

        [Fact]
        public void Comma_Delimiter_Keeps_Empty_Cells_As_NaN()
        {
            DataParseResult r = Run("1,,3\n2,5,6\n", 3, "COMMA", false, out _);

            Assert.Equal(2, r.RowCount);
            Assert.True(double.IsNaN(r.Columns[1][0]));
            Assert.Equal(3.0, r.Columns[2][0]);
            Assert.Equal(5.0, r.Columns[1][1]);
        }

        [Fact]
        public void Non_Numeric_Token_Becomes_NaN_And_Is_Reported_Once()
        {
            DataParseResult r = Run("1.0 abc\n2.0 xyz\n3.0 5.0\n", 2, "SPACE", false, out var issues);

            Assert.Equal(3, r.RowCount);
            Assert.True(double.IsNaN(r.Columns[1][0]));
            Assert.Equal(5.0, r.Columns[1][2]);
            Assert.Single(issues, i => i.Code == "BAD_NUMBER_TOKENS");
        }

        [Fact]
        public void Short_Line_In_Unwrapped_File_Is_Skipped_Without_Shifting_Columns()
        {
            DataParseResult r = Run("1.0 10.0\n2.0\n3.0 30.0\n", 2, "SPACE", false, out var issues);

            Assert.Equal(2, r.RowCount);
            Assert.Equal(new[] { 1.0, 3.0 }, r.Columns[0]);
            Assert.Equal(new[] { 10.0, 30.0 }, r.Columns[1]);
            Assert.Contains(issues, i => i.Code == "ROW_COLUMN_MISMATCH");
        }

        [Fact]
        public void Incomplete_Last_Row_In_Wrapped_File_Is_Dropped()
        {
            DataParseResult r = Run("1.0\n 10.0 100.0\n2.0\n 20.0\n", 3, "SPACE", true, out var issues);

            Assert.Equal(1, r.RowCount);
            Assert.Contains(issues, i => i.Code == "DATA_INCOMPLETE_ROW");
        }

        [Fact]
        public void Stops_At_Next_Section_And_Skips_Comments()
        {
            DataParseResult r = Run("# comment\n1.0 10.0\n\n2.0 20.0\n~Other\n3.0 30.0\n", 2, "SPACE", false, out _);

            Assert.Equal(2, r.RowCount);
        }

        [Fact]
        public void Grows_Beyond_Initial_Capacity()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 1000; i++) sb.Append(i).Append(' ').Append(i * 2).Append('\n');

            DataParseResult r = Run(sb.ToString(), 2, "SPACE", false, out _);

            Assert.Equal(1000, r.RowCount);
            Assert.Equal(1998.0, r.Columns[1][999]);
            Assert.Equal(1000, r.Columns[0].Length);
        }
    }
}