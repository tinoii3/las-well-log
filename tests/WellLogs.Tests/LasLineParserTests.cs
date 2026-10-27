using WellLogs.Core.Las;

namespace WellLogs.Tests
{
    public class LasLineParserTests
    {
        [Fact]
        public void Standard_Line()
        {
            HeaderLine? h = LasLineParser.TryParse("STRT .F      71.5000         :START DEPTH     ");

            Assert.NotNull(h);
            Assert.Equal("STRT", h!.Mnemonic);
            Assert.Equal("F", h.Unit);
            Assert.Equal("71.5000", h.Value);
            Assert.Equal("START DEPTH", h.Description);
        }

        [Fact]
        public void Empty_Unit()
        {
            HeaderLine? h = LasLineParser.TryParse("NULL .          -999.25      :NULL VALUE");

            Assert.NotNull(h);
            Assert.Equal("NULL", h!.Mnemonic);
            Assert.Equal("", h.Unit);
            Assert.Equal("-999.25", h.Value);
        }

        [Fact]
        public void Odd_Unit_With_Leading_Dot()
        {
            HeaderLine? h = LasLineParser.TryParse("IDWD ..1IN                                             :Integrated Depth Wheel Depth {F13.4}");

            Assert.NotNull(h);
            Assert.Equal("IDWD", h!.Mnemonic);
            Assert.Equal(".1IN", h.Unit);
            Assert.Equal("", h.Value);
        }

        [Fact]
        public void Long_Mnemonic_With_Underscore_And_Brackets()
        {
            HeaderLine? a = LasLineParser.TryParse("DI_HRLT.IN                                             :HRLT Diameter of Invasion {F11.4}");
            HeaderLine? b = LasLineParser.TryParse("LQC_MAXS[0].                                           :MAXS Auxiliary Data Status Image {AF13.4}");

            Assert.Equal("DI_HRLT", a!.Mnemonic);
            Assert.Equal("IN", a.Unit);
            Assert.Equal("LQC_MAXS[0]", b!.Mnemonic);
            Assert.Equal("", b.Unit);
        }

        [Fact]
        public void Value_With_Spaces_And_Quotes()
        {
            HeaderLine? h = LasLineParser.TryParse("LOC  .        1850' FNL, 1978' FWL, SEC.3, BLK. 10  : Location");

            Assert.Equal("LOC", h!.Mnemonic);
            Assert.Equal("1850' FNL, 1978' FWL, SEC.3, BLK. 10", h.Value);
            Assert.Equal("Location", h.Description);
        }

        [Fact]
        public void Value_Containing_Colon_Uses_Last_Colon()
        {
            HeaderLine? h = LasLineParser.TryParse("TIME .        12:30:00                   :Time");

            Assert.Equal("12:30:00", h!.Value);
            Assert.Equal("Time", h.Description);
        }

        [Fact]
        public void Comment_Blank_And_Section_Lines_Return_Null()
        {
            Assert.Null(LasLineParser.TryParse("#MNEM.UNIT      DATA             DESCRIPTION"));
            Assert.Null(LasLineParser.TryParse("   "));
            Assert.Null(LasLineParser.TryParse("~WELL INFORMATION"));
        }

        [Fact]
        public void Continuation_Text_Without_Dot_Returns_Null()
        {
            Assert.Null(LasLineParser.TryParse("to eccenter the HGNS  2 PPC's to centralize sonic) :Remarks Line 1"));
        }
    }
}