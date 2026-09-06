namespace Epsilon.Core;

public static class RootFinder
{
    private const double DefaultTolerance = 1e-10;
    private const int MaxIterations = 100;

    public static (double? Root, bool Found) TryFindRoot(
        this Expr expr,
        string variable,
        double initialGuess,
        IReadOnlyDictionary<string, double>? fixedBindings = null,
        double tolerance = DefaultTolerance,
        int maxIterations = MaxIterations)
    {
        Expr derivative = expr.Differentiate(variable);
        double x = initialGuess;

        Dictionary<string, double> BuildBindings(double value)
        {
            var dict = fixedBindings is null
                ? new Dictionary<string, double>()
                : new Dictionary<string, double>(fixedBindings);
            dict[variable] = value;
            return dict;
        }

        for (int i = 0; i < maxIterations; i++)
        {
            double fx = expr.Evaluate(BuildBindings(x));
            if (Math.Abs(fx) < tolerance)
                return (x, true);

            double dfx = derivative.Evaluate(BuildBindings(x));
            if (Math.Abs(dfx) < 1e-14)
                return (null, false);

            double next = x - fx / dfx;
            if (double.IsNaN(next) || double.IsInfinity(next))
                return (null, false);

            x = next;
        }

        return (null, false);
    }

    public static (double? Root, bool Found) TryFindRoot(
        this Expr expr, double initialGuess,
        double tolerance = DefaultTolerance, int maxIterations = MaxIterations)
    {
        string variable = expr.GetSingleVariable();
        return expr.TryFindRoot(variable, initialGuess, null, tolerance, maxIterations);
    }

    public static (Complex? Root, bool Found) TryFindComplexRoot(
        this Expr expr,
        string variable,
        Complex initialGuess,
        IReadOnlyDictionary<string, Complex>? fixedBindings = null,
        double tolerance = DefaultTolerance,
        int maxIterations = MaxIterations)
    {
        Expr derivative = expr.Differentiate(variable);
        Complex z = initialGuess;

        Dictionary<string, Complex> BuildBindings(Complex value)
        {
            var dict = fixedBindings is null
                ? new Dictionary<string, Complex>()
                : new Dictionary<string, Complex>(fixedBindings);
            dict[variable] = value;
            return dict;
        }

        for (int i = 0; i < maxIterations; i++)
        {
            Complex fz = expr.EvaluateComplex(BuildBindings(z));
            if (fz.Magnitude < tolerance)
                return (z, true);

            Complex dfz = derivative.EvaluateComplex(BuildBindings(z));
            if (dfz.Magnitude < 1e-14)
                return (null, false);

            Complex next = z - fz / dfz;
            if (double.IsNaN(next.Real) || double.IsNaN(next.Imaginary) ||
                double.IsInfinity(next.Real) || double.IsInfinity(next.Imaginary))
                return (null, false);

            z = next;
        }

        return (null, false);
    }

    // Existing single-variable overload, now delegating to the general one above.
    public static (Complex? Root, bool Found) TryFindComplexRoot(
        this Expr expr, Complex initialGuess,
        double tolerance = DefaultTolerance, int maxIterations = MaxIterations)
    {
        string variable = expr.GetSingleVariable();
        return expr.TryFindComplexRoot(variable, initialGuess, null, tolerance, maxIterations);
    }

    private static double? Bisect(
        Expr expr, string variable, double a, double b,
        IReadOnlyDictionary<string, double>? fixedBindings,
        double tolerance = 1e-10, int maxIterations = 100)
    {
        Dictionary<string, double> BuildBindings(double value)
        {
            var dict = fixedBindings is null
                ? new Dictionary<string, double>()
                : new Dictionary<string, double>(fixedBindings);
            dict[variable] = value;
            return dict;
        }

        double fa = expr.Evaluate(BuildBindings(a));
        double fb = expr.Evaluate(BuildBindings(b));

        if (double.IsNaN(fa) || double.IsNaN(fb) || double.IsInfinity(fa) || double.IsInfinity(fb))
            return null;

        if (Math.Sign(fa) == Math.Sign(fb)) return null;

        double mid = (a + b) / 2;

        for (int i = 0; i < maxIterations; i++)
        {
            mid = (a + b) / 2;
            double fm = expr.Evaluate(BuildBindings(mid));

            if (double.IsNaN(fm) || double.IsInfinity(fm))
                return null;

            if (Math.Abs(fm) < tolerance) return mid;

            if (Math.Sign(fm) == Math.Sign(fa)) { a = mid; fa = fm; }
            else b = mid;
        }

        double finalF = expr.Evaluate(BuildBindings(mid));
        return Math.Abs(finalF) < tolerance * 100 ? mid : null;
    }

    internal static double? BisectFallback(
        Expr expr, string variable, IReadOnlyDictionary<string, double>? fixedBindings, double a, double b)
    {
        return Bisect(expr, variable, a, b, fixedBindings);
    }

    // Old overload — keep for single-variable convenience call sites
    internal static double? BisectFallback(Expr expr, double a, double b)
    {
        string variable = expr.GetSingleVariable();
        return Bisect(expr, variable, a, b, null);
    }
}