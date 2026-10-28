namespace WellLogs.Core.Conditioning
{
    public static class PhysicalLimits
    {
        public static bool TryGet(CurveKind kind, out double min, out double max)
        {
            switch (kind)
            {
                case CurveKind.GammaRay: 
                    min = -20; max = 1000; 
                    return true;

                case CurveKind.Resistivity: 
                    min = 0.001; 
                    max = 100000; 
                    return true;

                case CurveKind.BulkDensity: 
                    min = 1.0; 
                    max = 3.5; 
                    return true;

                case CurveKind.NeutronPorosity: 
                    min = -0.15; 
                    max = 1.0; 
                    return true;

                default: 
                    min = 0; 
                    max = 0; 
                    return false;
            }
        }

        public static int Apply(CurveKind kind, double[] values)
        {
            if (!TryGet(kind, out double min, out double max))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < values.Length; i++)
            {
                double v = values[i];
                if (v < min || v > max)
                {
                    values[i] = double.NaN;
                    count++;
                }
            }
            return count;
        }
    }
}