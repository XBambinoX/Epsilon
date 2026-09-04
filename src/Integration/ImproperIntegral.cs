namespace Epsilon;

public static class ImproperIntegrator
{
    private const double DefaultTolerance = 1e-6;
    private const double InitialWindowWidth = 10.0;
    private const double WindowGrowthFactor = 1.5;
    private const int StepsPerWindow = 200;
    private const int MaxWindows = 200;
    private const int StableWindowsRequired = 3;

    public static (double? Value, bool IsConvergent) TryIntegrateImproper(
        this Expr expr, double a, double b, double tolerance = DefaultTolerance)
    {
        bool aInf = double.IsNegativeInfinity(a);
        bool bInf = double.IsPositiveInfinity(b);

        if (!aInf && !bInf)
            return (expr.Integrate(a, b), true);

        if (aInf && bInf)
        {
            var (leftValue, leftOk) = expr.TryIntegrateImproper(a, 0, tolerance);
            var (rightValue, rightOk) = expr.TryIntegrateImproper(0, b, tolerance);

            if (!leftOk || !rightOk || leftValue is null || rightValue is null)
                return (null, false);

            return (leftValue.Value + rightValue.Value, true);
        }

        bool reversed = aInf;
        double pos = reversed ? b : a;
        double width = InitialWindowWidth;
        double total = 0;
        int stableCount = 0;

        for (int i = 0; i < MaxWindows; i++)
        {
            double windowA = reversed ? pos - width : pos;
            double windowB = reversed ? pos : pos + width;

            double contribution = expr.Integrate(windowA, windowB, StepsPerWindow);

            if (double.IsNaN(contribution) || double.IsInfinity(contribution))
                return (null, false);

            total += contribution;
            pos = reversed ? windowA : windowB;

            bool negligible = Math.Abs(contribution) < tolerance * Math.Max(1.0, Math.Abs(total));
            stableCount = negligible ? stableCount + 1 : 0;

            if (stableCount >= StableWindowsRequired)
                return (total, true);

            width *= WindowGrowthFactor;
        }

        return (null, false);
    }

    public static bool IsConvergent(this Expr expr, double a, double b, double tolerance = DefaultTolerance)
        => expr.TryIntegrateImproper(a, b, tolerance).IsConvergent;

    public static double IntegrateImproper(this Expr expr, double a, double b, double tolerance = DefaultTolerance)
    {
        var (value, isConvergent) = expr.TryIntegrateImproper(a, b, tolerance);

        if (!isConvergent || value is null)
            throw new InvalidOperationException($"Improper integral does not converge on [{a}, {b}].");

        return value.Value;
    }
}