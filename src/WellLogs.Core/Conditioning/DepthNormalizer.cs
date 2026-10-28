using System;
using System.Collections.Generic;
using System.Linq;

namespace WellLogs.Core.Conditioning
{
    public sealed class DepthResult
    {
        public DepthResult(double[] depth, int[] rows)
        {
            Depth = depth;
            Rows = rows;
        }

        public double[] Depth { get; }
        public int[] Rows { get; }
    }

    public static class DepthNormalizer
    {
        public static DepthResult Normalize(double[] depth, double? declaredStep, ConditioningReport report)
        {
            var valid = new List<int>(depth.Length);
            for (int i = 0; i < depth.Length; i++)
            {
                if (!double.IsNaN(depth[i]) && !double.IsInfinity(depth[i]))
                {
                    valid.Add(i);
                }
            }

            int invalid = depth.Length - valid.Count;
            if (invalid > 0)
            {
                report.Warn("DEPTH_INVALID_ROWS", invalid + " row(s) with missing or non-numeric depth were removed.");
            }

            int[] rows = valid.ToArray();
            int up = 0, down = 0;
            for (int k = 1; k < rows.Length; k++)
            {
                double d = depth[rows[k]] - depth[rows[k - 1]];
                if (d > 0)
                {
                    up++;
                }
                else if (d < 0)
                {
                    down++;
                }
            }
            if (down > up)
            {
                Array.Reverse(rows);
                report.Info("DEPTH_REVERSED", "Depth was recorded in descending order; rows reversed to ascending.");
            }

            bool sorted = true;
            for (int k = 1; k < rows.Length; k++)
            {
                if (depth[rows[k]] < depth[rows[k - 1]]) 
                { 
                    sorted = false; 
                    break; 
                }
            }

            if (!sorted)
            {
                rows = rows.OrderBy(i => depth[i]).ToArray();
                report.Warn("DEPTH_RESORTED", "Depth was not monotonic; rows were stably sorted by depth.");
            }

            var kept = new List<int>(rows.Length);
            foreach (int r in rows)
            {
                if (kept.Count > 0 && depth[r] == depth[kept[kept.Count - 1]])
                {
                    continue;
                }
                kept.Add(r);
            }

            int dups = rows.Length - kept.Count;
            if (dups > 0)
            {
                report.Warn("DEPTH_DUPLICATES_REMOVED", dups + " duplicate depth row(s) removed (first occurrence kept).");
            }
            rows = kept.ToArray();

            ReportSampling(depth, rows, declaredStep, report);

            return new DepthResult(ArrayOps.Gather(depth, rows), rows);
        }

        private static void ReportSampling(double[] depth, int[] rows, double? declaredStep, ConditioningReport report)
        {
            if (rows.Length < 3)
            {
                return;
            }

            var diffs = new double[rows.Length - 1];
            for (int k = 1; k < rows.Length; k++)
            {
                diffs[k - 1] = depth[rows[k]] - depth[rows[k - 1]];
            }

            double median = ArrayOps.MedianIgnoringNaN(diffs);
            double tol = median * 0.01;
            int off = 0;
            foreach (double d in diffs)
            {
                if (Math.Abs(d - median) > tol) off++;
            }
            double fraction = (double)off / diffs.Length;

            if (fraction > 0.05)
            {
                report.Warn("DEPTH_IRREGULAR_SAMPLING", "Irregular depth sampling: median step " + ArrayOps.Fmt(median) + ", " + ArrayOps.Fmt(fraction * 100) + "% of steps differ by more than 1%. Data not resampled.");
            }
            else
            {
                report.Info("DEPTH_SAMPLING", "Regular sampling, median step " + ArrayOps.Fmt(median) + ".");
            }

            if (declaredStep.HasValue && declaredStep.Value != 0 && Math.Abs(Math.Abs(declaredStep.Value) - median) > 0.01 * median)
            {
                report.Warn("STEP_MISMATCH", "Header STEP " + ArrayOps.Fmt(declaredStep.Value) + " differs from data median step " + ArrayOps.Fmt(median) + "; data values used.");
            }
        }
    }
}