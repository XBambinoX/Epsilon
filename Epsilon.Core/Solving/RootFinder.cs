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

    public static (ComplexNumber? Root, bool Found) TryFindComplexRoot(
        this Expr expr,
        string variable,
        ComplexNumber initialGuess,
        IReadOnlyDictionary<string, ComplexNumber>? fixedBindings = null,
        double tolerance = DefaultTolerance,
        int maxIterations = MaxIterations)
    {
        Expr derivative = expr.Differentiate(variable);
        ComplexNumber z = initialGuess;

        Dictionary<string, ComplexNumber> BuildBindings(ComplexNumber value)
        {
            var dict = fixedBindings is null
                ? new Dictionary<string, ComplexNumber>()
                : new Dictionary<string, ComplexNumber>(fixedBindings);
            dict[variable] = value;
            return dict;
        }

        for (int i = 0; i < maxIterations; i++)
        {
            ComplexNumber fz = expr.EvaluateComplex(BuildBindings(z));
            if (fz.Magnitude < tolerance)
                return (z, true);

            ComplexNumber dfz = derivative.EvaluateComplex(BuildBindings(z));
            if (dfz.Magnitude < 1e-14)
                return (null, false);

            ComplexNumber next = z - fz / dfz;
            if (double.IsNaN(next.Real) || double.IsNaN(next.Imaginary) ||
                double.IsInfinity(next.Real) || double.IsInfinity(next.Imaginary))
                return (null, false);

            z = next;
        }

        return (null, false);
    }

    // Existing single-variable overload, now delegating to the general one above.
    public static (ComplexNumber? Root, bool Found) TryFindComplexRoot(
        this Expr expr, ComplexNumber initialGuess,
        double tolerance = DefaultTolerance, int maxIterations = MaxIterations)
    {
        string variable = expr.GetSingleVariable();
        return expr.TryFindComplexRoot(variable, initialGuess, null, tolerance, maxIterations);
    }
}