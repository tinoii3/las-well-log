using System;
using WellLogs.Core.Model;

namespace WellLogs.Tests
{
    public class WellTests
    {
        [Fact]
        public void AddCurve_WithMatchingLength_Succeeds()
        {
            var well = new Well("A", new[] { 1.0, 2.0, 3.0 }, "M");
            well.AddCurve(new Curve("GR", "GAPI", "Gamma", new[] { 10.0, 20.0, 30.0 }));

            Assert.Single(well.Curves);
        }

        [Fact]
        public void AddCurve_WithWrongLength_Throws()
        {
            var well = new Well("A", new[] { 1.0, 2.0, 3.0 }, "M");

            Assert.Throws<ArgumentException>(() =>
                well.AddCurve(new Curve("GR", "GAPI", "", new[] { 10.0, 20.0 })));
        }

        [Fact]
        public void FindCurve_IgnoresCase()
        {
            var well = new Well("A", new[] { 1.0 }, "M");
            well.AddCurve(new Curve("GR", "GAPI", "", new[] { 10.0 }));

            Assert.NotNull(well.FindCurve("gr"));
            Assert.Null(well.FindCurve("RHOB"));
        }

        [Fact]
        public void LoadResult_WithWarning_IsOkWithWarnings()
        {
            var well = new Well("A", new[] { 1.0 }, "M");
            var issues = new[] { new Issue(IssueSeverity.Warning, "NULL_MISMATCH", "test") };

            LoadResult r = LoadResult.Success("a.las", well, issues);

            Assert.Equal(LoadStatus.OkWithWarnings, r.Status);
        }
    }
}