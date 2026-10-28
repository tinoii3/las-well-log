namespace WellLogs.Core.Conditioning
{
    public static class UnitConverter
    {
        public const double FeetToMeters = 0.3048;

        public static double DepthToMetersFactor(string unit, ConditioningReport report)
        {
            string u = UnitCatalog.Normalize(unit);
            if (UnitCatalog.IsFeet(u))
            {
                report.Info("DEPTH_UNIT_CONVERTED", "Depth converted from feet to meters (x0.3048).");
                return FeetToMeters;
            }

            if (UnitCatalog.IsMeters(u))
            {
                return 1.0;
            }

            report.Warn("DEPTH_UNIT_UNKNOWN", "Depth unit '" + unit + "' is not recognised; assumed meters (no conversion).");
            return 1.0;
        }

        public static void ToFraction(double[] values, string unit, string mnemonic, ConditioningReport report)
        {
            string u = UnitCatalog.Normalize(unit);
            double median = ArrayOps.MedianIgnoringNaN(values);
            bool percent;

            if (UnitCatalog.IsPercent(u))
            {
                percent = true;
            }
            else if (UnitCatalog.IsFraction(u))
            {
                percent = median > 1.5;
                if (percent)
                {
                    report.Warn("POROSITY_UNIT_MISLABELED", mnemonic + ": unit says fraction but median is " + ArrayOps.Fmt(median) + "; treated as percent.");
                }
            }
            else
            {
                percent = median > 1.5;
                report.Info("POROSITY_UNIT_INFERRED", mnemonic + ": unit '" + unit + "' not recognised; inferred " + (percent ? "percent" : "fraction") + " from median " + ArrayOps.Fmt(median) + ".");
            }

            if (percent)
            {
                Scale(values, 0.01);
                report.Info("POROSITY_CONVERTED", mnemonic + ": percent converted to fraction (/100).");
            }
        }

        public static void ToGramPerCc(double[] values, string unit, string mnemonic, ConditioningReport report)
        {
            string u = UnitCatalog.Normalize(unit);
            if (UnitCatalog.IsDensityGcc(u))
            {
                return;
            }

            if (UnitCatalog.IsDensityKgM3(u))
            {
                Scale(values, 0.001);
                report.Info("DENSITY_CONVERTED", mnemonic + ": kg/m3 converted to g/cc (/1000).");
                return;
            }

            double median = ArrayOps.MedianIgnoringNaN(values);
            if (median > 100)
            {
                Scale(values, 0.001);
                report.Info("DENSITY_UNIT_INFERRED", mnemonic + ": unit '" + unit + "' not recognised; median " + ArrayOps.Fmt(median) + " looks like kg/m3, converted to g/cc.");
            }
        }

        public static void ConductivityToResistivity(double[] values, string mnemonic, ConditioningReport report)
        {
            for (int i = 0; i < values.Length; i++)
            {
                double c = values[i];
                values[i] = c > 0 ? 1000.0 / c : double.NaN;
            }
            report.Info("RES_FROM_CONDUCTIVITY", "No resistivity curve found; resistivity derived from conductivity " + mnemonic + " (R = 1000 / C).");
        }

        private static void Scale(double[] values, double factor)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] *= factor;
            }
        }
    }
}