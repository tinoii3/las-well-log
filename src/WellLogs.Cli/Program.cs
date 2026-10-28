using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WellLogs.Core.Model;

namespace WellLogs.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string input = args.Length > 0 ? args[0] : "data";
            string output = args.Length > 1 ? args[1] : "output";

            if (!Directory.Exists(input))
            {
                Console.Error.WriteLine("Input folder not found: " + input);
                Console.Error.WriteLine("Usage: WellLogs.Cli <inputFolder> [outputFolder]");
                return 1;
            }

            string plotDir = Path.Combine(output, "plots");
            Directory.CreateDirectory(plotDir);

            string[] files = Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories).Where(f => string.Equals(Path.GetExtension(f), ".las", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Console.WriteLine("Found " + files.Length + " LAS file(s) in " + input);

            var sw = Stopwatch.StartNew();
            var results = new ConcurrentBag<WellSummary>();
            var names = new NameRegistry();
            int done = 0;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
            };

            Parallel.ForEach(files, options, file =>
            {
                WellSummary summary;
                try
                {
                    summary = WellProcessor.Process(file, plotDir, names);
                }
                catch (Exception ex)
                {
                    summary = new WellSummary
                    {
                        File = Path.GetFileName(file),
                        Status = LoadStatus.Failed,
                        Note = "unexpected error: " + ex.GetType().Name + ": " + ex.Message
                    };
                }

                results.Add(summary);
                int n = Interlocked.Increment(ref done);
                Console.WriteLine("[" + n + "/" + files.Length + "] " + summary.File + ": " + summary.Status + (summary.PlotFile.Length > 0 ? " -> " + summary.PlotFile : " (" + summary.Note + ")"));
            });

            sw.Stop();
            WellSummary[] all = results.ToArray();

            ReportWriter.WriteMarkdown(Path.Combine(output, "conditioning_report.md"), all, sw.Elapsed);
            ReportWriter.WriteCsv(Path.Combine(output, "summary.csv"), all);

            long peakMb = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            Console.WriteLine();
            Console.WriteLine("Done in " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s, peak memory " + peakMb + " MB.");
            Console.WriteLine("Loaded: " + all.Count(a => a.Status == LoadStatus.Ok || a.Status == LoadStatus.OkWithWarnings) + ", unsupported: " + all.Count(a => a.Status == LoadStatus.Unsupported) +
                ", failed: " + all.Count(a => a.Status == LoadStatus.Failed) +
                ", plots: " + all.Count(a => a.PlotFile.Length > 0));
            Console.WriteLine("Output: " + Path.GetFullPath(output));
            return 0;
        }
    }
}