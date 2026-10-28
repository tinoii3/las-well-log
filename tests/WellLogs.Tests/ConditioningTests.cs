using System.Collections.Generic;
using System.Linq;
using WellLogs.Core.Conditioning;
using WellLogs.Core.Model;

namespace WellLogs.Tests
{
    public class NullHandlerTests
    {
        [Fact]
        public void Declared_Zero_Is_Not_Treated_As_Null()
        {
            var report = new ConditioningReport();
            double[] s = NullHandler.BuildSentinelSet(0.0, report);
            double[] v = { -999.25, 0.0, 5.0 };

            int n = NullHandler.Apply(v, s);

            Assert.Equal(1, n);
            Assert.True(double.IsNaN(v[0]));
            Assert.Equal(0.0, v[1]);
            Assert.Contains(report.Entries, e => e.Code == "NULL_DECLARED_NOT_APPLIED");
        }

        [Fact]
        public void Distinctive_Declared_Null_Is_Added()
        {
            double[] s = NullHandler.BuildSentinelSet(999.25, new ConditioningReport());
            double[] v = { 999.25, 1.0 };

            NullHandler.Apply(v, s);

            Assert.True(double.IsNaN(v[0]));
            Assert.Equal(1.0, v[1]);
        }

        [Fact]
        public void Match_Uses_Tolerance_Not_Exact_Equality()
        {
            double[] s = NullHandler.BuildSentinelSet(null, new ConditioningReport());
            double[] v = { -999.2500001 };

            NullHandler.Apply(v, s);

            Assert.True(double.IsNaN(v[0]));
        }

        [Fact]
        public void Infinity_Becomes_NaN()
        {
            double[] s = NullHandler.BuildSentinelSet(null, new ConditioningReport());
            double[] v = { double.PositiveInfinity, 1.0 };

            int n = NullHandler.Apply(v, s);

            Assert.Equal(1, n);
            Assert.True(double.IsNaN(v[0]));
        }
    }

    public class DepthNormalizerTests
    {
        [Fact]
        public void Descending_Depth_Is_Reversed()
        {
            var report = new ConditioningReport();
            DepthResult r = DepthNormalizer.Normalize(new[] { 3.0, 2.0, 1.0 }, null, report);

            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, r.Depth);
            Assert.Equal(new[] { 2, 1, 0 }, r.Rows);
            Assert.Contains(report.Entries, e => e.Code == "DEPTH_REVERSED");
        }

        [Fact]
        public void Duplicate_Depths_Are_Removed_Keeping_First()
        {
            var report = new ConditioningReport();
            DepthResult r = DepthNormalizer.Normalize(new[] { 1.0, 2.0, 2.0, 3.0 }, null, report);

            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, r.Depth);
            Assert.Equal(new[] { 0, 1, 3 }, r.Rows);
            Assert.Contains(report.Entries, e => e.Code == "DEPTH_DUPLICATES_REMOVED");
        }

        [Fact]
        public void Unsorted_Depth_Is_Sorted_With_Warning()
        {
            var report = new ConditioningReport();
            DepthResult r = DepthNormalizer.Normalize(new[] { 1.0, 3.0, 2.0, 4.0 }, null, report);

            Assert.Equal(new[] { 1.0, 2.0, 3.0, 4.0 }, r.Depth);
            Assert.Equal(new[] { 0, 2, 1, 3 }, r.Rows);
            Assert.Contains(report.Entries, e => e.Code == "DEPTH_RESORTED");
        }

        [Fact]
        public void NaN_Depth_Rows_Are_Removed()
        {
            var report = new ConditioningReport();
            DepthResult r = DepthNormalizer.Normalize(new[] { 1.0, double.NaN, 3.0 }, null, report);

            Assert.Equal(new[] { 0, 2 }, r.Rows);
            Assert.Contains(report.Entries, e => e.Code == "DEPTH_INVALID_ROWS");
        }

        [Fact]
        public void Irregular_Sampling_Is_Reported()
        {
            var report = new ConditioningReport();
            double[] depth = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 20, 40, 60 };

            DepthNormalizer.Normalize(depth, null, report);

            Assert.Contains(report.Entries, e => e.Code == "DEPTH_IRREGULAR_SAMPLING");
        }

        [Fact]
        public void Regular_Sampling_Has_No_Warning()
        {
            var report = new ConditioningReport();
            double[] depth = { 0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5 };

            DepthNormalizer.Normalize(depth, 0.5, report);

            Assert.DoesNotContain(report.Entries, e => e.Severity == IssueSeverity.Warning);
        }

        [Fact]
        public void Header_Step_Mismatch_Is_Reported()
        {
            var report = new ConditioningReport();
            double[] depth = { 0, 0.5, 1.0, 1.5, 2.0 };

            DepthNormalizer.Normalize(depth, 1.0, report);

            Assert.Contains(report.Entries, e => e.Code == "STEP_MISMATCH");
        }
    }

    public class CurveClassifierTests
    {
        [Theory]
        [InlineData("GR", "GAPI", "Gamma Ray", CurveKind.GammaRay)]
        [InlineData("GRGC", "GAPI", "Gamma Ray", CurveKind.GammaRay)]
        [InlineData("GR_EDTC", "GAPI", "EDTC Gamma Ray {F13.4}", CurveKind.GammaRay)]
        [InlineData("TGR", "GAPI", "Thorium GR {F13.4}", CurveKind.Other)]
        [InlineData("RT_HRLT", "OHMM", "HRLT True Formation Resistivity {F11.4}", CurveKind.Resistivity)]
        [InlineData("TBIT90", "OHMM", "TBI 2ft 90in {F13.4}", CurveKind.Resistivity)]
        [InlineData("DDLL", "OHMM", "Deep Laterolog", CurveKind.Resistivity)]
        [InlineData("RMUD", "OHMM", "Mud Resistivity {F13.4}", CurveKind.Other)]
        [InlineData("RXO", "OHMM", "Flushed Zone Resistivity {F11.4}", CurveKind.Other)]
        [InlineData("RWA_HILT", "OHMM", "Apparent Formation Water Resistivity from HILT Lithology Computation {F11.4}", CurveKind.Other)]
        [InlineData("HMIN", "OHMM", "HIP Micro Inverse Resistivity {F11.4}", CurveKind.Other)]
        [InlineData("ATCO90", "MM/M", "Array Induction Two Foot Conductivity A90 {F13.4}", CurveKind.Conductivity)]
        [InlineData("RHOB", "G/C3", "Bulk Density {F13.4}", CurveKind.BulkDensity)]
        [InlineData("RHOZ", "G/C3", "Standard Resolution Formation Density {F11.4}", CurveKind.BulkDensity)]
        [InlineData("DEN", "G/C3", "Compensated Density", CurveKind.BulkDensity)]
        [InlineData("DRHO", "G/C3", "Bulk Density Correction {F13.4}", CurveKind.Other)]
        [InlineData("DPHI", "%", "Density Porosity {F13.4}", CurveKind.Other)]
        [InlineData("NPHI", "CFCF", "Thermal Neutron Porosity (original Ratio Method) in Selected Lithology {F11.4}", CurveKind.NeutronPorosity)]
        [InlineData("NPRL", "V/V", "Limestone Neutron Por.", CurveKind.NeutronPorosity)]
        [InlineData("TNPH", "%", "Thermal Neutron Porosity {F13.4}", CurveKind.NeutronPorosity)]
        [InlineData("DNPH", "CFCF", "Difference between Corrected Thermal Neutron Porosity and Uncorrected {F11.4}", CurveKind.Other)]
        [InlineData("SPHI", "CFCF", "Sonic Porosity {F11.4}", CurveKind.Other)]
        [InlineData("CALI", "IN", "Caliper {F13.4}", CurveKind.Other)]
        [InlineData("PEF", "", "Photoelectric Factor {F13.4}", CurveKind.Other)]
        public void Classifies_Real_World_Curves(string mnemonic, string unit, string description, CurveKind expected)
        {
            Assert.Equal(expected, CurveClassifier.Classify(mnemonic, unit, description).Kind);
        }
    }

    public class UnitConverterTests
    {
        [Fact]
        public void Percent_Unit_Is_Converted_To_Fraction()
        {
            var r = new ConditioningReport();
            double[] v = { 35.0, 20.0, double.NaN };

            UnitConverter.ToFraction(v, "%", "TNPH", r);

            Assert.Equal(0.35, v[0], 6);
            Assert.Equal(0.20, v[1], 6);
            Assert.True(double.IsNaN(v[2]));
            Assert.Contains(r.Entries, e => e.Code == "POROSITY_CONVERTED");
        }

        [Fact]
        public void Fraction_Unit_Is_Left_Alone()
        {
            var r = new ConditioningReport();
            double[] v = { 0.30, 0.20 };

            UnitConverter.ToFraction(v, "CFCF", "NPHI", r);

            Assert.Equal(0.30, v[0], 6);
            Assert.Empty(r.Entries);
        }

        [Fact]
        public void Mislabeled_Fraction_Unit_Is_Detected_From_Values()
        {
            var r = new ConditioningReport();
            double[] v = { 35.0, 36.0, 34.0 };

            UnitConverter.ToFraction(v, "V/V", "NPHI", r);

            Assert.Equal(0.35, v[0], 6);
            Assert.Contains(r.Entries, e => e.Code == "POROSITY_UNIT_MISLABELED");
        }

        [Fact]
        public void Unknown_Porosity_Unit_Is_Inferred_From_Values()
        {
            var r = new ConditioningReport();
            double[] v = { 30.0, 25.0 };

            UnitConverter.ToFraction(v, "", "NPHI", r);

            Assert.Equal(0.30, v[0], 6);
            Assert.Contains(r.Entries, e => e.Code == "POROSITY_UNIT_INFERRED");
        }

        [Fact]
        public void Density_In_Kg_Per_M3_Is_Converted()
        {
            var r = new ConditioningReport();
            double[] v = { 2450.0, 2500.0 };

            UnitConverter.ToGramPerCc(v, "KG/M3", "RHOB", r);

            Assert.Equal(2.45, v[0], 6);
        }

        [Fact]
        public void Density_With_Unknown_Unit_Is_Inferred_From_Values()
        {
            var r = new ConditioningReport();
            double[] v = { 2450.0 };

            UnitConverter.ToGramPerCc(v, "", "RHOB", r);

            Assert.Equal(2.45, v[0], 6);
        }

        [Fact]
        public void Feet_Convert_To_Meters()
        {
            Assert.Equal(0.3048, UnitConverter.DepthToMetersFactor("F", new ConditioningReport()), 6);
            Assert.Equal(0.3048, UnitConverter.DepthToMetersFactor("FT", new ConditioningReport()), 6);
        }

        [Fact]
        public void Meters_Need_No_Conversion()
        {
            Assert.Equal(1.0, UnitConverter.DepthToMetersFactor("M", new ConditioningReport()), 6);
        }

        [Fact]
        public void Unknown_Depth_Unit_Warns_And_Assumes_Meters()
        {
            var r = new ConditioningReport();

            double f = UnitConverter.DepthToMetersFactor(".1IN", r);

            Assert.Equal(1.0, f, 6);
            Assert.Contains(r.Entries, e => e.Code == "DEPTH_UNIT_UNKNOWN");
        }

        [Fact]
        public void Conductivity_Converts_To_Resistivity()
        {
            var r = new ConditioningReport();
            double[] v = { 8.0978, 0.0, -1.0, double.NaN };

            UnitConverter.ConductivityToResistivity(v, "ATCO90", r);

            Assert.Equal(1000.0 / 8.0978, v[0], 6);
            Assert.True(double.IsNaN(v[1]));
            Assert.True(double.IsNaN(v[2]));
            Assert.True(double.IsNaN(v[3]));
        }
    }

    public class WellConditionerTests
    {
        private static Curve C(string m, string u, string d, params double[] v)
        {
            return new Curve(m, u, d, v);
        }

        private static Well MakeWell(double[] depth, string depthUnit, double? declaredNull, double? declaredStep, params Curve[] curves)
        {
            var w = new Well("W", depth, depthUnit) { DeclaredNull = declaredNull, DeclaredStep = declaredStep };
            foreach (Curve c in curves) w.AddCurve(c);
            return w;
        }

        [Fact]
        public void Full_Pipeline_Reverses_Converts_And_Picks_Right_Curves()
        {
            Well w = MakeWell(
                new[] { 110.0, 109.5, 109.0, 108.5, 108.0 }, "F", 0.0, -0.5,
                C("GR", "GAPI", "Gamma Ray", 50, 55, -999.25, 60, 65),
                C("TGR", "GAPI", "Thorium GR", 1, 1, 1, 1, 1),
                C("RMUD", "OHMM", "Mud Resistivity", 0.1, 0.1, 0.1, 0.1, 0.1),
                C("TBIT90", "OHMM", "TBI 2ft 90in", 10, 20, 30, 40, 50),
                C("RHOB", "G/C3", "Bulk Density", 2.4, 2.5, 2.45, 2.4, 2.5),
                C("TNPH", "%", "Thermal Neutron Porosity", 30, 25, 20, 15, 10));

            ConditionedWell cw = WellConditioner.Condition(w, "w.las");

            Assert.Equal(5, cw.Depth.Length);
            Assert.Equal(108.0 * 0.3048, cw.Depth[0], 4);
            Assert.Equal("GR", cw.Get(CurveKind.GammaRay)!.Mnemonic);
            Assert.Equal("TBIT90", cw.Get(CurveKind.Resistivity)!.Mnemonic);
            Assert.Equal(65.0, cw.Get(CurveKind.GammaRay)!.Values[0]);
            Assert.True(double.IsNaN(cw.Get(CurveKind.GammaRay)!.Values[2]));
            Assert.Equal(0.10, cw.Get(CurveKind.NeutronPorosity)!.Values[0], 6);
            Assert.Contains(cw.Report.Entries, e => e.Code == "DEPTH_REVERSED");
            Assert.Contains(cw.Report.Entries, e => e.Code == "NULL_DECLARED_NOT_APPLIED");
            Assert.Contains(cw.Report.Entries, e => e.Code == "POROSITY_CONVERTED");
            Assert.Contains(cw.Report.Entries, e => e.Code == "DEPTH_UNIT_CONVERTED");
        }

        [Fact]
        public void Rows_With_No_Data_At_Both_Ends_Are_Trimmed()
        {
            Well w = MakeWell(
                new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }, "M", -999.25, null,
                C("GR", "GAPI", "Gamma Ray", -999.25, -999.25, 50, 60, -999.25));

            ConditionedWell cw = WellConditioner.Condition(w, "w.las");

            Assert.Equal(new[] { 3.0, 4.0 }, cw.Depth);
            Assert.Equal(new[] { 50.0, 60.0 }, cw.Get(CurveKind.GammaRay)!.Values);
            Assert.Contains(cw.Report.Entries, e => e.Code == "TRIMMED_ENDS");
        }

        [Fact]
        public void Resistivity_Is_Derived_From_Conductivity_When_Missing()
        {
            Well w = MakeWell(
                new[] { 1.0, 2.0, 3.0 }, "M", -999.25, null,
                C("ATCO90", "MM/M", "Array Induction Two Foot Conductivity A90", 8.0978, -999.25, 12.0));

            ConditionedWell cw = WellConditioner.Condition(w, "w.las");

            Curve res = cw.Get(CurveKind.Resistivity)!;
            Assert.Equal("ATCO90", res.Mnemonic);
            Assert.Equal(1000.0 / 8.0978, res.Values[0], 6);
            Assert.True(double.IsNaN(res.Values[1]));
            Assert.Contains(cw.Report.Entries, e => e.Code == "RES_FROM_CONDUCTIVITY");
        }

        [Fact]
        public void Long_Flat_Run_Is_Reported_But_Not_Changed()
        {
            double[] depth = Enumerable.Range(0, 60).Select(i => (double)i).ToArray();
            double[] gr = Enumerable.Repeat(100.0, 60).ToArray();
            Well w = MakeWell(depth, "M", -999.25, null, C("GR", "GAPI", "Gamma Ray", gr));

            ConditionedWell cw = WellConditioner.Condition(w, "w.las");

            Assert.Contains(cw.Report.Entries, e => e.Code == "FLAT_RUN");
            Assert.Equal(100.0, cw.Get(CurveKind.GammaRay)!.Values[30]);
        }

        [Fact]
        public void Well_Without_Useful_Curves_Does_Not_Crash()
        {
            Well w = MakeWell(
                new[] { 1.0, 2.0 }, "M", -999.25, null,
                C("CALI", "IN", "Caliper", 8.5, 8.6));

            ConditionedWell cw = WellConditioner.Condition(w, "w.las");

            Assert.Empty(cw.Curves);
            Assert.Contains(cw.Report.Entries, e => e.Code == "CURVE_MISSING");
        }

        [Fact]
        public void Empty_Candidate_Is_Skipped_For_Next_Best()
        {
            Well w = MakeWell(
                new[] { 1.0, 2.0, 3.0 }, "M", -999.25, null,
                C("GR", "GAPI", "Gamma Ray", -999.25, -999.25, -999.25),
                C("GR_EDTC", "GAPI", "EDTC Gamma Ray", 40, 50, 60));

            ConditionedWell cw = WellConditioner.Condition(w, "w.las");

            Assert.Equal("GR_EDTC", cw.Get(CurveKind.GammaRay)!.Mnemonic);
            Assert.Contains(cw.Report.Entries, e => e.Code == "CURVE_SKIPPED_EMPTY");
        }
    }
}