using WellLogs.Core.Las;

namespace WellLogs.Tests
{
    public class HeaderValueResolverTests
    {
        [Theory]
        [InlineData("WELL", "WELL", "University 4-29 #2", "University 4-29 #2")]
        [InlineData("COMP", "COMPANY", "Apache Corporation", "Apache Corporation")]
        [InlineData("COMP", "COMPANY", "Chesapeake Operating Company", "Chesapeake Operating Company")]
        [InlineData("DATE", "LOG DATE", "14-Jul-2013", "14-Jul-2013")]
        [InlineData("UWI", "UNIQUE WELL ID", "42461387870000", "42461387870000")]
        [InlineData("OPER", "OPERATOR", "Pioneer Natural", "Pioneer Natural")] // ป้ายที่ไม่เคยอยู่ในรายการใด ๆ
        public void Label_Before_Colon_Is_Swapped(string mnem, string value, string desc, string expected)
        {
            Assert.Equal(expected, HeaderValueResolver.Resolve(mnem, value, desc));
        }

        [Theory]
        [InlineData("COMP", "PIONEER NATURAL RESOURCES USA, INC.", "Company")]
        [InlineData("COMP", "BP COMPANY", "Company")]
        [InlineData("STRT", "71.5000", "START DEPTH")]
        [InlineData("SRVC", "Weatherford", "Service company")]
        public void Standard_Style_Is_Left_Alone(string mnem, string value, string desc)
        {
            Assert.Equal(value, HeaderValueResolver.Resolve(mnem, value, desc));
        }

        [Fact]
        public void Label_Only_Line_Becomes_Empty()
        {
            Assert.Equal("", HeaderValueResolver.Resolve("WELL", "WELL", ""));
        }

        [Fact]
        public void Real_Value_With_Empty_Description_Is_Kept()
        {
            Assert.Equal("ACME COMPANY", HeaderValueResolver.Resolve("COMP", "ACME COMPANY", ""));
        }

        [Fact]
        public void Empty_Value_Stays_Empty()
        {
            Assert.Equal("", HeaderValueResolver.Resolve("CTRY", "", "Country"));
        }
    }
}