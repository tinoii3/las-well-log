using System.IO;
using WellLogs.Core.Las;
using WellLogs.Core.Model;

namespace WellLogs.Tests
{
    public class LasReaderTests
    {
        private static LoadResult LoadText(string text)
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, text);
                return LasReader.Load(path);
            }
            finally
            {
                File.Delete(path);
            }
        }

        private const string Simple =
@"~VERSION INFORMATION
VERS. 2.0 : x
WRAP. NO : y
~WELL INFORMATION
STRT .M 1000.0 : s
STOP .M 1001.0 : s
STEP .M 0.5 : s
NULL . -999.25 : n
WELL . TEST-1 : w
~CURVE INFORMATION
DEPT .M : depth
GR .GAPI : gamma
~A
1000.0 50.0
1000.5 -999.25
1001.0 55.0
";

        [Fact]
        public void Valid_File_Loads()
        {
            LoadResult r = LoadText(Simple);

            Assert.Equal(LoadStatus.Ok, r.Status);
            Assert.NotNull(r.Well);
            Assert.Equal("TEST-1", r.Well!.Name);
            Assert.Equal(3, r.Well.SampleCount);
            Assert.Equal("M", r.Well.DepthUnit);
            Assert.Equal(-999.25, r.Well.DeclaredNull);
            Assert.Equal(55.0, r.Well.FindCurve("GR")!.Values[2]);
        }

        [Fact]
        public void Missing_File_Fails_Without_Throwing()
        {
            LoadResult r = LasReader.Load(Path.Combine(Path.GetTempPath(), "does-not-exist-123.las"));

            Assert.Equal(LoadStatus.Failed, r.Status);
            Assert.Contains(r.Issues, i => i.Code == "FILE_NOT_FOUND");
        }

        [Fact]
        public void Empty_File_Fails()
        {
            LoadResult r = LoadText("");

            Assert.Equal(LoadStatus.Failed, r.Status);
            Assert.Contains(r.Issues, i => i.Code == "EMPTY_FILE");
        }

        [Fact]
        public void Non_Las_Text_Fails()
        {
            LoadResult r = LoadText("hello world\nthis is not a las file\n");

            Assert.Equal(LoadStatus.Failed, r.Status);
            Assert.Contains(r.Issues, i => i.Code == "NO_DATA_SECTION");
        }

        [Fact]
        public void Array_Curves_Are_Unsupported_With_Reason()
        {
            string text =
@"~VERSION INFORMATION
VERS. 2.0 : x
~CURVE INFORMATION
DEPT .F : depth
WFA_FAST[0]. : wave {AF13.4}
~A
1 2
";
            LoadResult r = LoadText(text);

            Assert.Equal(LoadStatus.Unsupported, r.Status);
            Assert.Null(r.Well);
            Assert.Contains(r.Issues, i => i.Code == "ARRAY_CURVES_UNSUPPORTED");
        }

        [Fact]
        public void True_Las3_Is_Unsupported()
        {
            string text = "~Version\nVERS. 3.0 : x\n~Log_Definition\nDEPT.M : Depth\n~Log_Data | Log_Definition\n1.0\n";

            LoadResult r = LoadText(text);

            Assert.Equal(LoadStatus.Unsupported, r.Status);
            Assert.Contains(r.Issues, i => i.Code == "LAS3_UNSUPPORTED");
        }

        [Fact]
        public void Fake_Las3_With_Las2_Layout_Loads_With_Info_Issue()
        {
            string text =
@"~VERSION INFORMATION
VERS. 3.0 : x
DLM. SPACE : y
~CURVE INFORMATION
DEPT .F : DEPTH
GR .GAPI : Gamma
~Ascii | Curve
 9270.0 47.0
 9270.5 49.0
";
            LoadResult r = LoadText(text);

            Assert.NotNull(r.Well);
            Assert.Equal(2, r.Well!.SampleCount);
            Assert.Contains(r.Issues, i => i.Code == "VERS_STRUCTURE_MISMATCH");
        }

        [Fact]
        public void Wrapped_File_Loads_Correctly()
        {
            string text =
@"~VERSION INFORMATION
VERS. 1.2 : x
WRAP. YES : y
~CURVE INFORMATION
DEPT .F : depth
GR .GAPI : gamma
RT .OHMM : res
~A
1000.0
50.0 10.0
1000.5
51.0 -999.25
";
            LoadResult r = LoadText(text);

            Assert.NotNull(r.Well);
            Assert.Equal(2, r.Well!.SampleCount);
            Assert.Equal(51.0, r.Well.FindCurve("GR")!.Values[1]);
            Assert.Equal(-999.25, r.Well.FindCurve("RT")!.Values[1]);
        }

        [Fact]
        public void Well_Name_Falls_Back_To_File_Name()
        {
            string text = Simple.Replace("WELL . TEST-1 : w\n", "").Replace("WELL . TEST-1 : w\r\n", "");

            LoadResult r = LoadText(text);

            Assert.NotNull(r.Well);
            Assert.False(string.IsNullOrEmpty(r.Well!.Name));
        }
    }
}