namespace Epsilon.Core;

/// <summary>Newton's method from a single starting point.</summary>
public static class RootFinder
{
    internal const double DefaultTolerance = 1e-10;
    private const int MaxIterations = 100;

    /// <summary>
    /// Looks for a root of <paramref name="expr"/> = 0 near <paramref name="initialGuess"/> with
    /// Newton's method. Which root is found (if any) depends on the guess; use
    /// <see cref="RootFindingExtensions.FindRealRoots(Expr, string, IReadOnlyDictionary{string, double}, double, double, int)"/>
    /// for all roots in a range.
    /// </summary>
    /// <param name="expr">The function whose zero is wanted.</param>
    /// <param name="variable">The variable to solve for.</param>
    /// <param name="initialGuess">Where to start.</param>
    /// <param name="fixedBindings">Values for the other variables, if any.</param>
    /// <param name="tolerance">Stop once |f(x)| is below this.</param>
    /// <param name="maxIterations">Give up after this many steps.</param>
    /// <returns>The root and true; or null and false if the iteration did not converge (flat derivative, divergence, too many steps).</returns>
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

    /// <summary>Newton's method for the expression's only variable. See <see cref="TryFindRoot(Expr, string, double, IReadOnlyDictionary{string, double}, double, int)"/>.</summary>
    /// <exception cref="InvalidOperationException">The expression does not have exactly one variable.</exception>
    public static (double? Root, bool Found) TryFindRoot(
        this Expr expr, double initialGuess,
        double tolerance = DefaultTolerance, int maxIterations = MaxIterations)
    {
        string variable = expr.GetSingleVariable();
        return expr.TryFindRoot(variable, initialGuess, null, tolerance, maxIterations);
    }

    /// <summary>
    /// Looks for a complex root of <paramref name="expr"/> = 0 near <paramref name="initialGuess"/>
    /// with Newton's method.
    /// </summary>
    /// <param name="expr">The function whose zero is wanted.</param>
    /// <param name="variable">The variable to solve for.</param>
    /// <param name="initialGuess">Where to start.</param>
    /// <param name="fixedBindings">Values for the other variables, if any.</param>
    /// <param name="tolerance">Stop once |f(z)| is below this.</param>
    /// <param name="maxIterations">Give up after this many steps.</param>
    /// <returns>The root and true; or null and false if the iteration did not converge.</returns>
    public static (ComplexNumber? Root, bool Found) TryFindComplexRoot(
        this Expr expr,
        string variable,
        ComplexNumber initialGuess,
        IReadOnlyDictionary<string, ComplexNumber>? fixedBindings = null,
        double tolerance = DefaultTolerance,
        int maxIterations = MaxIterations)
    {
        Expr derivative = expr.Differentiate(variable);
        return NewtonComplex(
            ComplexFunction(expr, variable, fixedBindings),
            ComplexFunction(derivative, variable, fixedBindings),
            initialGuess, tolerance, maxIterations);
    }

    /// <summary><paramref name="expr"/> as a function of <paramref name="variable"/>, with the other variables fixed.</summary>
    internal static Func<ComplexNumber, ComplexNumber> ComplexFunction(
        Expr expr, string variable, IReadOnlyDictionary<string, ComplexNumber>? fixedBindings)
    {
        // One dictionary per function, with the variable overwritten on every call.
        var bindings = fixedBindings is null
            ? new Dictionary<string, ComplexNumber>()
            : new Dictionary<string, ComplexNumber>(fixedBindings);

        return z =>
        {
            bindings[variable] = z;
            return expr.EvaluateComplex(bindings);
        };
    }

    /// <summary>The Newton iteration of <see cref="TryFindComplexRoot(Expr, string, ComplexNumber, IReadOnlyDictionary{string, ComplexNumber}, double, int)"/> for a derivative computed once by the caller.</summary>
    internal static (ComplexNumber? Root, bool Found) NewtonComplex(
        Func<ComplexNumber, ComplexNumber> f, Func<ComplexNumber, ComplexNumber> derivative,
        ComplexNumber initialGuess, double tolerance = DefaultTolerance, int maxIterations = MaxIterations)
    {
        ComplexNumber z = initialGuess;

        for (int i = 0; i < maxIterations; i++)
        {
            ComplexNumber fz = f(z);
            if (fz.Magnitude < tolerance)
                return (z, true);

            ComplexNumber dfz = derivative(z);
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
    /// <summary>Complex Newton's method for the expression's only variable. See <see cref="TryFindComplexRoot(Expr, string, ComplexNumber, IReadOnlyDictionary{string, ComplexNumber}, double, int)"/>.</summary>
    /// <exception cref="InvalidOperationException">The expression does not have exactly one variable.</exception>
    public static (ComplexNumber? Root, bool Found) TryFindComplexRoot(
        this Expr expr, ComplexNumber initialGuess,
        double tolerance = DefaultTolerance, int maxIterations = MaxIterations)
    {
        string variable = expr.GetSingleVariable();
        return expr.TryFindComplexRoot(variable, initialGuess, null, tolerance, maxIterations);
    }
}