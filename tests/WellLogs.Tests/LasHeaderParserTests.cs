using System.Collections.Generic;
using System.IO;
using System.Linq;
using WellLogs.Core.Las;
using WellLogs.Core.Model;

namespace WellLogs.Tests
{
    public class LasHeaderParserTests
    {
        private static LasHeader Parse(string text, out List<Issue> issues, out StringReader reader)
        {
            issues = new List<Issue>();
            reader = new StringReader(text);
            return LasHeaderParser.Parse(reader, issues);
        }

        [Fact]
        public void Weatherford_Style_File_Declares_Null_As_Zero_And_Stops_At_Data()
        {
            string text =
@"~Version Information
VERS.          2.00                                  : CWLS
WRAP.          NO                                    : One line per depth step
~Well Information
STRT .FT        111.877                              : Start
STOP .FT       8759.877                              : Stop
STEP .FT          0.500                              : Step increment
NULL .         0.000                                 : Null value
WELL .         UNIVERSITY 10 - 13 # 2                : Well
~Curve Information
DEPT .FT       00 001 00 00                          : Logged depth
GRGC .GAPI     30 310 01 00                          : Gamma Ray
~Other Information
SERVICE ORDER # 3512212
~A    DEPT       GRGC
   111.877   -999.250
";
            LasHeader h = Parse(text, out var issues, out var reader);

            Assert.Equal(2.0, h.Version);
            Assert.False(h.Wrapped);
            Assert.Equal(0.0, h.NullValue);
            Assert.Equal(111.877, h.Start);
            Assert.Equal("UNIVERSITY 10 - 13 # 2", h.WellName);
            Assert.Equal(2, h.Curves.Count);
            Assert.Equal("FT", h.DepthUnit);
            Assert.True(h.DataSectionFound);
            Assert.Empty(issues);
            Assert.Equal("   111.877   -999.250", reader.ReadLine());
        }

        [Fact]
        public void Schlumberger_Label_Before_Colon_Is_Swapped()
        {
            string text =
@"~WELL INFORMATION
WELL .                    WELL:  University 4-29 #2
COMP .                 COMPANY:  Apache Corporation
~A
1 2
";
            LasHeader h = Parse(text, out _, out _);

            Assert.Equal("University 4-29 #2", h.WellName);
            Assert.Equal("Apache Corporation", h.WellProperties["COMP"]);
        }

        [Fact]
        public void Wrap_Yes_And_Descending_Step_Are_Read()
        {
            string text =
@"~VERSION INFORMATION
VERS.           2.0   :x
WRAP.           YES   :y
~WELL INFORMATION
STRT .F         9589.0       :START DEPTH
STEP .F           -0.5       :STEP
~A
9589.0
";
            LasHeader h = Parse(text, out _, out _);

            Assert.True(h.Wrapped);
            Assert.Equal(-0.5, h.Step);
        }

        [Fact]
        public void Fake_Las3_With_Las2_Layout_Is_Readable_And_Reports_Info()
        {
            string text =
@"~VERSION INFORMATION
VERS.           3.0   :x
DLM.          SPACE   :y
~CURVE INFORMATION
DEPT .F    :DEPTH
GR   .GAPI :Gamma Ray
~Ascii | Curve
 9270.0 47.0
";
            LasHeader h = Parse(text, out var issues, out _);

            Assert.True(h.DataSectionFound);
            Assert.False(h.HasLas3Sections);
            Assert.Equal("SPACE", h.Delimiter);
            Assert.Contains(issues, i => i.Code == "VERS_STRUCTURE_MISMATCH");
        }

        [Fact]
        public void Real_Las3_Sections_Are_Flagged()
        {
            string text =
@"~Version
VERS. 3.0 : x
~Log_Definition
DEPT.M : Depth
~Log_Data | Log_Definition
1.0
";
            LasHeader h = Parse(text, out _, out _);

            Assert.True(h.HasLas3Sections);
            Assert.False(h.DataSectionFound);
        }

        [Fact]
        public void Array_Curves_Are_Flagged()
        {
            string text =
@"~CURVE INFORMATION
DEPT .F                :DEPTH {F10.1}
GR_EDTC.GAPI           :EDTC Gamma Ray {F13.4}
WFA_FAST[0].           :Waveform Array {AI11}
IDWD ..1IN             :Depth Wheel {F13.4}
~A
1
";
            LasHeader h = Parse(text, out _, out _);

            Assert.True(h.HasArrayCurves);
            Assert.False(h.Curves[1].IsArray);
            Assert.True(h.Curves[2].IsArray);
            Assert.Equal(".1IN", h.Curves[3].Unit);
        }

        [Fact]
        public void Missing_Data_Section_Reports_Error()
        {
            LasHeader h = Parse("~VERSION INFORMATION\nVERS. 2.0 : x\n", out var issues, out _);

            Assert.False(h.DataSectionFound);
            Assert.Contains(issues, i => i.Code == "NO_DATA_SECTION" && i.Severity == IssueSeverity.Error);
        }

        [Fact]
        public void Garbage_Line_In_Curve_Section_Is_Warning_Not_Crash()
        {
            string text = "~CURVE INFORMATION\nDEPT .F : depth\nthis is not a header line\n~A\n1\n";

            LasHeader h = Parse(text, out var issues, out _);

            Assert.Single(h.Curves);
            Assert.Single(issues, i => i.Code == "CURVE_LINE_UNREADABLE");
        }
    }
}