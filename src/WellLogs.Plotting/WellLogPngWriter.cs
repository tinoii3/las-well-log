using System.IO;
using OxyPlot;
using OxyPlot.WindowsForms;
using WellLogs.Core.Conditioning;

namespace WellLogs.Plotting
{
    public static class WellLogPngWriter
    {
        private static readonly object RenderLock = new object();

        public static void Save(ConditionedWell well, string path, int width = 900, int height = 1800)
        {
            PlotModel model = WellLogPlotBuilder.Build(well);

            lock (RenderLock)
            {
                using (FileStream fs = File.Create(path))
                {
                    var exporter = new PngExporter { Width = width, Height = height };
                    exporter.Export(model, fs);
                }
            }
        }
    }
}