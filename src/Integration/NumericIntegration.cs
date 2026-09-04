namespace Epsilon;

public static class Integrator
{
    private const int MaxDepth = 20;

    public static double Integrate(
        this Expr expr,
        double a,
        double b,
        double tolerance = 1e-8)
    {
        if (tolerance <= 0.0)
            throw new ArgumentException(
                "Tolerance must be positive.",
                nameof(tolerance));

        if (a == b)
            return 0.0;

        if (a > b)
            return -expr.Integrate(b, a, tolerance);

        double fa = EvaluateFinite(expr, a);
        double fb = EvaluateFinite(expr, b);

        double mid = (a + b) / 2.0;
        double fm = EvaluateFinite(expr, mid);

        double whole = Simpson(a, b, fa, fm, fb);

        return AdaptiveSimpson(
            expr,
            a,
            b,
            fa,
            fm,
            fb,
            whole,
            tolerance,
            MaxDepth);
    }

    private static double AdaptiveSimpson(
        Expr expr,
        double a,
        double b,
        double fa,
        double fm,
        double fb,
        double whole,
        double tolerance,
        int depth)
    {
        double mid = (a + b) / 2.0;
        double leftMid = (a + mid) / 2.0;
        double rightMid = (mid + b) / 2.0;

        double flm = EvaluateFinite(expr, leftMid);
        double frm = EvaluateFinite(expr, rightMid);

        double left = Simpson(
            a,
            mid,
            fa,
            flm,
            fm);

        double right = Simpson(
            mid,
            b,
            fm,
            frm,
            fb);

        double delta = left + right - whole;

        if (depth == 0 || Math.Abs(delta) <= 15.0 * tolerance)
            return left + right + delta / 15.0;

        return AdaptiveSimpson(
            expr,
            a,
            mid,
            fa,
            flm,
            fm,
            left,
            tolerance / 2.0,
            depth - 1)
            +
            AdaptiveSimpson(
                expr,
                mid,
                b,
                fm,
                frm,
                fb,
                right,
                tolerance / 2.0,
                depth - 1);
    }

    private static double Simpson(
        double a,
        double b,
        double fa,
        double fm,
        double fb)
    {
        return (b - a) / 6.0 * (fa + 4.0 * fm + fb);
    }

    private static double EvaluateFinite(
        Expr expr,
        double x)
    {
        double value = expr.Evaluate(x);

        if (!double.IsFinite(value))
            throw new InvalidOperationException(
                $"Function is not finite at x = {x}.");

        return value;
    }
}