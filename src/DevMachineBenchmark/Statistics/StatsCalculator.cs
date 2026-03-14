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

    /// <summary>
    /// Coefficient of Variation: StdDev / Mean * 100. Returns null if mean is 0 or n &lt;= 1.
    /// </summary>
    public static double? CoefficientOfVariation(double[] values)
    {
        if (values.Length <= 1) return null;
        var mean = Mean(values);
        if (mean == 0) return null;
        return StdDev(values) / mean * 100;
    }

    /// <summary>
    /// 95% confidence interval around the mean using the t-distribution.
    /// Returns null if n &lt;= 1.
    /// </summary>
    public static (double Low, double High)? ConfidenceInterval95(double[] values)
    {
        if (values.Length <= 1) return null;
        var mean = Mean(values);
        var stdErr = StdDev(values) / Math.Sqrt(values.Length);
        var tValue = TValue95(values.Length - 1);
        var margin = tValue * stdErr;
        return (mean - margin, mean + margin);
    }

    /// <summary>
    /// Interquartile range (Q3 - Q1).
    /// </summary>
    public static double IQR(double[] values)
    {
        if (values.Length < 4) return Max(values) - Min(values);
        var sorted = values.OrderBy(v => v).ToArray();
        return Percentile(sorted, 75) - Percentile(sorted, 25);
    }

    /// <summary>
    /// Trimmed mean: removes top and bottom 10% of values (at least 1 from each end when n >= 5).
    /// Falls back to regular mean for small samples.
    /// </summary>
    public static double TrimmedMean(double[] values)
    {
        if (values.Length < 5) return Mean(values);
        var sorted = values.OrderBy(v => v).ToArray();
        var trimCount = Math.Max(1, (int)(sorted.Length * 0.1));
        var trimmed = sorted.Skip(trimCount).Take(sorted.Length - 2 * trimCount).ToArray();
        return trimmed.Length > 0 ? trimmed.Average() : Mean(values);
    }

    /// <summary>
    /// Returns the number of outliers where a value exceeds 2x the median.
    /// </summary>
    public static int OutlierCount(double[] values)
    {
        if (values.Length < 3) return 0;
        var median = Median(values);
        if (median == 0) return 0;
        return values.Count(v => v > 2 * median);
    }

    /// <summary>
    /// Returns true if Max > 2x Median, indicating the presence of outliers.
    /// </summary>
    public static bool HasOutliers(double[] values)
    {
        if (values.Length < 3) return false;
        var median = Median(values);
        return median > 0 && Max(values) > 2 * median;
    }

    /// <summary>
    /// Calculates a percentile value from a pre-sorted array using linear interpolation.
    /// </summary>
    public static double Percentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0) return 0;
        if (sortedValues.Length == 1) return sortedValues[0];

        var index = (percentile / 100.0) * (sortedValues.Length - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper) return sortedValues[lower];

        var fraction = index - lower;
        return sortedValues[lower] * (1 - fraction) + sortedValues[upper] * fraction;
    }

    /// <summary>
    /// Critical t-values for 95% CI (two-tailed) by degrees of freedom.
    /// </summary>
    private static double TValue95(int degreesOfFreedom) =>
        degreesOfFreedom switch
        {
            1 => 12.706,
            2 => 4.303,
            3 => 3.182,
            4 => 2.776,
            5 => 2.571,
            6 => 2.447,
            7 => 2.365,
            8 => 2.306,
            9 => 2.262,
            10 => 2.228,
            11 => 2.201,
            12 => 2.179,
            13 => 2.160,
            14 => 2.145,
            15 => 2.131,
            16 => 2.120,
            17 => 2.110,
            18 => 2.101,
            19 => 2.093,
            20 => 2.086,
            <= 25 => 2.060,
            <= 30 => 2.042,
            <= 40 => 2.021,
            <= 60 => 2.000,
            <= 120 => 1.980,
            _ => 1.960, // approaches z-value for large n
        };
}
