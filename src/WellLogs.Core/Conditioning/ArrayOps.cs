using System;
using System.Globalization;

namespace WellLogs.Core.Conditioning
{
    public static class ArrayOps
    {
        public static double[] Gather(double[] src, int[] rows)
        {
            var result = new double[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                result[i] = src[rows[i]];
            }
            return result;
        }

        public static double[] Slice(double[] src, int start, int length)
        {
            var result = new double[length];
            Array.Copy(src, start, result, 0, length);
            return result;
        }

        public static double MedianIgnoringNaN(double[] values)
        {
            int n = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (!double.IsNaN(values[i]))
                {
                    n++;
                }
            }

            if (n == 0)
            {
                return double.NaN;
            }

            var tmp = new double[n];
            int k = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (!double.IsNaN(values[i]))
                {
                    tmp[k++] = values[i];
                }
            }

            Array.Sort(tmp);
            return n % 2 == 1 ? tmp[n / 2] : (tmp[n / 2 - 1] + tmp[n / 2]) / 2.0;
        }

        public static string Fmt(double v)
        {
            return v.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}