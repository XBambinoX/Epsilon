namespace Epsilon.Core;

public static class RootFinder
{
    private const double DefaultTolerance = 1e-10;
    private const int MaxIterations = 100;

    public static (double? Root, bool Found) TryFindRoot(
        this Expr expr, double initialGuess,
        double tolerance = DefaultTolerance, int maxIterations = MaxIterations)
    {
        Expr derivative = expr.Differentiate();
        double x = initialGuess;

        for (int i = 0; i < maxIterations; i++)
        {
            double fx = expr.Evaluate(x);
            if (Math.Abs(fx) < tolerance)
                return (x, true);

            double dfx = derivative.Evaluate(x);
            if (Math.Abs(dfx) < 1e-14)
                return (null, false);

            double next = x - fx / dfx;
            if (double.IsNaN(next) || double.IsInfinity(next))
                return (null, false);

            x = next;
        }

        return (null, false);
    }

    public static (Complex? Root, bool Found) TryFindComplexRoot(
        this Expr expr, Complex initialGuess,
        double tolerance = DefaultTolerance, int maxIterations = MaxIterations)
    {
        Expr derivative = expr.Differentiate();
        Complex z = initialGuess;

        for (int i = 0; i < maxIterations; i++)
        {
            Complex fz = expr.EvaluateComplex(z);
            if (fz.Magnitude < tolerance)
                return (z, true);

            Complex dfz = derivative.EvaluateComplex(z);
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

    private static double? Bisect(Expr expr, double a, double b, double tolerance = 1e-10, int maxIterations = 100)
    {
        double fa = expr.Evaluate(a);
        double fb = expr.Evaluate(b);

        if (double.IsNaN(fa) || double.IsNaN(fb) || double.IsInfinity(fa) || double.IsInfinity(fb))
            return null;

        if (Math.Sign(fa) == Math.Sign(fb)) return null;

        double mid = (a + b) / 2;

        for (int i = 0; i < maxIterations; i++)
        {
            mid = (a + b) / 2;
            double fm = expr.Evaluate(mid);

            if (double.IsNaN(fm) || double.IsInfinity(fm))
                return null;

            if (Math.Abs(fm) < tolerance) return mid;

            if (Math.Sign(fm) == Math.Sign(fa)) { a = mid; fa = fm; }
            else b = mid;
        }

        double finalF = expr.Evaluate(mid);
        return Math.Abs(finalF) < tolerance * 100 ? mid : null;
    }

    internal static double? BisectFallback(Expr expr, double a, double b) => Bisect(expr, a, b);
}