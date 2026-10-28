using System;
using System.IO;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using WellLogs.Core.Conditioning;
using WellLogs.Core.Model;

namespace WellLogs.Plotting
{
    public static class WellLogPlotBuilder
    {
        private const string DepthKey = "depth";
        private const double GrStart = 0.00, GrEnd = 0.31;
        private const double ResStart = 0.345, ResEnd = 0.655;
        private const double DnStart = 0.69, DnEnd = 1.00;

        private const double GrMin = 0, GrMax = 150;
        private const double ResMin = 0.2, ResMax = 2000;
        private const double DenMin = 1.95, DenMax = 2.95;
        private const double NeuMin = -0.15, NeuMax = 0.45;

        private static readonly OxyColor GrColor = OxyColor.FromRgb(0, 140, 0);
        private static readonly OxyColor ResColor = OxyColor.FromRgb(200, 0, 0);
        private static readonly OxyColor DenColor = OxyColor.FromRgb(200, 0, 0);
        private static readonly OxyColor NeuColor = OxyColor.FromRgb(0, 70, 220);
        private static readonly OxyColor GridColor = OxyColor.FromRgb(215, 215, 215);

        public static PlotModel Build(ConditionedWell well)
        {
            if (well.Depth.Length == 0)
            {
                throw new ArgumentException("Well has no depth samples to plot.", nameof(well));
            }

            double top = well.Depth[0];
            double bottom = well.Depth[well.Depth.Length - 1];

            if (bottom - top < 1e-9)
            {
                bottom = top + 1;
            }

            double midY = (top + bottom) / 2.0;
            double thirdY = top + (bottom - top) / 3.0;
            double twoThirdY = top + 2.0 * (bottom - top) / 3.0;

            var model = new PlotModel
            {
                Title = well.Name,
                Subtitle = Path.GetFileName(well.SourceFile) + "   |   depth: meters (measured depth)",
                TitleFontSize = 14,
                SubtitleFontSize = 10,
                Background = OxyColors.White,
                IsLegendVisible = false
            };

            model.Axes.Add(new LinearAxis
            {
                Key = DepthKey,
                Position = AxisPosition.Left,
                StartPosition = 1,
                EndPosition = 0,
                Minimum = top,
                Maximum = bottom,
                Title = "Depth (m)",
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = GridColor,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = GridColor
            });

            Curve? gr = well.Get(CurveKind.GammaRay);
            AddXAxis(model, "gr", false, GrMin, GrMax, GrStart, GrEnd, false, 0, gr != null ? gr.Mnemonic + " (GAPI)" : "Gamma Ray - no data", GrColor, true);

            if (gr != null)
            {
                AddSeries(model, gr,  well.Depth, "gr",  GrColor,  GrMin,  GrMax);
            }
            else
            {
                AddNoData(model, "gr", (GrMin + GrMax) / 2.0, midY, "no gamma ray");
            }

            Curve? res = well.Get(CurveKind.Resistivity);
            AddXAxis(model, "res", true, ResMin, ResMax, ResStart, ResEnd, false, 0, res != null ? res.Mnemonic + " (ohm-m)" : "Resistivity - no data", ResColor, true);
            
            if (res != null)
            {
                AddSeries(model, res, well.Depth, "res", ResColor, ResMin, ResMax);
            }
            else
            {
                AddNoData(model, "res", Math.Sqrt(ResMin * ResMax), midY, "no resistivity");
            }

            Curve? den = well.Get(CurveKind.BulkDensity);
            Curve? neu = well.Get(CurveKind.NeutronPorosity);
            AddXAxis(model, "den", false, DenMin, DenMax, DnStart, DnEnd, false, 0, den != null ? den.Mnemonic + " (g/cc)" : "Density - no data", DenColor, true);
            AddXAxis(model, "neu", false, NeuMin, NeuMax, DnStart, DnEnd, true, 1, neu != null ? neu.Mnemonic + " (v/v)" : "Neutron - no data", NeuColor, false);
            
            if (den != null)
            {
                AddSeries(model, den, well.Depth, "den", DenColor, DenMin, DenMax);
            }
            else
            {
                AddNoData(model, "den", (DenMin + DenMax) / 2.0, thirdY, "no density");
            }
            
            if (neu != null)
            {
                AddSeries(model, neu, well.Depth, "neu", NeuColor, NeuMin, NeuMax);
            }
            else
            {
                AddNoData(model, "neu", (NeuMin + NeuMax) / 2.0, twoThirdY, "no neutron porosity");
            }

            return model;
        }

        private static void AddXAxis(PlotModel model, string key, bool log, double min, double max,
            double start, double end, bool reversed, int tier, string title, OxyColor color, bool grid)
        {
            Axis axis = log ? (Axis)new LogarithmicAxis() : new LinearAxis();
            axis.Key = key;
            axis.Position = AxisPosition.Top;
            axis.Minimum = min;
            axis.Maximum = max;
            axis.StartPosition = reversed ? end : start;
            axis.EndPosition = reversed ? start : end;
            axis.PositionTier = tier;
            axis.Title = title;
            axis.TitleColor = color;
            axis.TextColor = color;
            axis.TicklineColor = color;
            axis.AxislineColor = color;
            axis.AxislineStyle = LineStyle.Solid;
            axis.IsPanEnabled = false;
            axis.IsZoomEnabled = false;

            if (grid)
            {
                axis.MajorGridlineStyle = LineStyle.Solid;
                axis.MajorGridlineColor = GridColor;
                axis.MinorGridlineStyle = LineStyle.Dot;
                axis.MinorGridlineColor = GridColor;
            }
            model.Axes.Add(axis);
        }

        private static void AddSeries(PlotModel model, Curve curve, double[] depth, string xKey, OxyColor color, double axisMin, double axisMax)
        {
            var series = new LineSeries
            {
                XAxisKey = xKey,
                YAxisKey = DepthKey,
                Color = color,
                StrokeThickness = 1
            };

            double[] v = curve.Values;
            int n = Math.Min(v.Length, depth.Length);

            for (int i = 0; i < n; i++)
            {
                double x = v[i];
                bool visible = !double.IsNaN(x) && x >= axisMin && x <= axisMax;
                series.Points.Add(visible ? new DataPoint(x, depth[i]) : new DataPoint(double.NaN, depth[i]));
            }
            model.Series.Add(series);
        }

        private static void AddNoData(PlotModel model, string xKey, double x, double y, string text)
        {
            model.Annotations.Add(new TextAnnotation
            {
                Text = text,
                TextPosition = new DataPoint(x, y),
                XAxisKey = xKey,
                YAxisKey = DepthKey,
                Stroke = OxyColors.Transparent,
                FontSize = 13
            });
        }
    }
}