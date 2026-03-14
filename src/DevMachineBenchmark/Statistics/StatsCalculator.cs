namespace DevMachineBenchmark.Statistics;

public static class StatsCalculator
{
    public static double Mean(double[] values) =>
        values.Length == 0 ? 0 : values.Average();

    public static double Median(double[] values)
    {
        if (values.Length == 0) return 0;
        var sorted = values.OrderBy(v => v).ToArray();
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    public static double StdDev(double[] values)
    {
        if (values.Length <= 1) return 0;
        var mean = Mean(values);
        var sumSquares = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSquares / (values.Length - 1));
    }

    public static double Min(double[] values) =>
        values.Length == 0 ? 0 : values.Min();

    public static double Max(double[] values) =>
        values.Length == 0 ? 0 : values.Max();
}
