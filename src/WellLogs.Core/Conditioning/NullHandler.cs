using System;
using System.Collections.Generic;

namespace WellLogs.Core.Conditioning
{
    public static class NullHandler
    {
        public const double Tolerance = 1e-4;
        public const double MinDistinctiveNull = 99.0;
        private static readonly double[] CommonSentinels = { -999.25, -999.0, -9999.0, -99999.0 };

        public static double[] BuildSentinelSet(double? declared, ConditioningReport report)
        {
            var list = new List<double>(CommonSentinels);

            if (declared.HasValue && !double.IsNaN(declared.Value) && !double.IsInfinity(declared.Value))
            {
                double d = declared.Value;
                if (Math.Abs(d) < MinDistinctiveNull)
                {
                    report.Warn("NULL_DECLARED_NOT_APPLIED", "Header declares NULL = " + ArrayOps.Fmt(d) + ", which is also a legitimate measurement value; " + "not treated as missing. Common sentinels (-999.25, -999, -9999, -99999) are used instead.");
                }
                else if (!IsSentinel(d, list.ToArray()))
                {
                    list.Add(d);
                    report.Info("NULL_DECLARED_EXTRA", "Header NULL value " + ArrayOps.Fmt(d) + " added to the sentinel set.");
                }
            }

            return list.ToArray();
        }

        public static bool IsSentinel(double v, double[] sentinels)
        {
            for (int i = 0; i < sentinels.Length; i++)
            {
                if (Math.Abs(v - sentinels[i]) < Tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        public static int Apply(double[] values, double[] sentinels)
        {
            int count = 0;
            for (int i = 0; i < values.Length; i++)
            {
                double v = values[i];
                if (double.IsNaN(v))
                {
                    continue;
                }

                if (double.IsInfinity(v) || IsSentinel(v, sentinels))
                {
                    values[i] = double.NaN;
                    count++;
                }
            }
            return count;
        }

        public static int CountValid(double[] values, double[] sentinels)
        {
            int count = 0;
            for (int i = 0; i < values.Length; i++)
            {
                double v = values[i];
                if (double.IsNaN(v) || double.IsInfinity(v) || IsSentinel(v, sentinels))
                {
                    continue;
                }
                count++;
            }
            return count;
        }
    }
}