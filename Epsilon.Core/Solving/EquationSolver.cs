namespace Epsilon.Core;

/// <summary>Numeric search for all roots of an expression or equation in a range.</summary>
public static class RootFindingExtensions
{
    private const int DefaultRealScanSteps = 200;
    private const double RootMergeTolerance = 1e-6;

    // A local minimum of |f| counts as a (touching) root only if f there is this small
    // relative to f on the neighbouring grid points - x^2 + 1e-7 must not have a root.
    private const double TouchingRootTolerance = 1e-10;
    private const double InfMappingEdgeEpsilon = 1e-9;

    private const int DefaultComplexGridSteps = 12;
    private const double ComplexRootMergeTolerance = 1e-6;

    /// <summary>
    /// All real solutions of the equation <paramref name="left"/> = <paramref name="right"/> for
    /// <paramref name="variable"/> in the range. Points where either side is undefined are never
    /// reported (<c>x^2/x = 0</c> has no root at 0).
    /// </summary>
    /// <param name="left">The left side of the equation.</param>
    /// <param name="right">The right side of the equation.</param>
    /// <param name="variable">The variable to solve for.</param>
    /// <param name="fixedBindings">Values for the other variables, if any.</param>
    /// <param name="leftLimit">Lower end of the search range; may be -infinity.</param>
    /// <param name="rightLimit">Upper end of the search range; may be +infinity.</param>
    /// <param name="scanSteps">Number of grid intervals to scan; more finds closely spaced roots.</param>
    /// <returns>The roots in ascending order, each to about full double precision.</returns>
    /// <exception cref="ArgumentException">A limit is NaN or <paramref name="leftLimit"/> ≥ <paramref name="rightLimit"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scanSteps"/> is less than 2.</exception>
    public static IReadOnlyList<double> FindRealRoots(
        this Expr left,
        Expr right,
        string variable,
        IReadOnlyDictionary<string, double>? fixedBindings = null,
        double leftLimit = double.NegativeInfinity,
        double rightLimit = double.PositiveInfinity,
        int scanSteps = DefaultRealScanSteps)
    {
        Expr diff = new Subtract(left, right).Simplify(SimplifyMode.Strict); // keep singular points: x^2/x = 0 has no root at 0
        return diff.FindRealRoots(variable, fixedBindings, leftLimit, rightLimit, scanSteps);
    }

    /// <summary>
    /// All real roots of <paramref name="expr"/> = 0 for <paramref name="variable"/> in the range,
    /// found by scanning a grid and refining each sign change or touching point (even-multiplicity
    /// roots such as x^2) by bisection. Infinite limits are handled by a change of variable.
    /// </summary>
    /// <remarks>
    /// A numeric method: roots closer together than the grid spacing can be missed, so increase
    /// <paramref name="scanSteps"/> or narrow the range when that matters.
    /// </remarks>
    /// <param name="expr">The function whose zeros are wanted.</param>
    /// <param name="variable">The variable to solve for.</param>
    /// <param name="fixedBindings">Values for the other variables, if any.</param>
    /// <param name="leftLimit">Lower end of the search range; may be -infinity.</param>
    /// <param name="rightLimit">Upper end of the search range; may be +infinity.</param>
    /// <param name="scanSteps">Number of grid intervals to scan.</param>
    /// <returns>The roots in ascending order.</returns>
    /// <exception cref="ArgumentException">A limit is NaN or <paramref name="leftLimit"/> ≥ <paramref name="rightLimit"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scanSteps"/> is less than 2.</exception>
    public static IReadOnlyList<double> FindRealRoots(
        this Expr expr,
        string variable,
        IReadOnlyDictionary<string, double>? fixedBindings = null,
        double leftLimit = double.NegativeInfinity,
        double rightLimit = double.PositiveInfinity,
        int scanSteps = DefaultRealScanSteps)
    {
        if (double.IsNaN(leftLimit) || double.IsNaN(rightLimit))
            throw new ArgumentException("Limits cannot be NaN.");

        if (leftLimit >= rightLimit)
            throw new ArgumentException("Left limit must be less than right limit.");

        if (scanSteps < 2)
            throw new ArgumentOutOfRangeException(nameof(scanSteps));

        bool leftInf = double.IsNegativeInfinity(leftLimit);
        bool rightInf = double.IsPositiveInfinity(rightLimit);

        if (!leftInf && !rightInf)
            return ScanForRealRoots(expr, variable, fixedBindings, static t => t, leftLimit, rightLimit, scanSteps);

        Func<double, double> mapToX = MakeInfiniteMapping(leftLimit, rightLimit, leftInf, rightInf, out double tMin, out double tMax);

        return ScanForRealRoots(expr, variable, fixedBindings, mapToX, tMin, tMax, scanSteps);
    }

    /// <summary>
    /// All real solutions of <paramref name="left"/> = <paramref name="right"/> for the equation's
    /// only variable. See <see cref="FindRealRoots(Expr, Expr, string, IReadOnlyDictionary{string, double}, double, double, int)"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The equation does not have exactly one variable.</exception>
    public static IReadOnlyList<double> FindRealRoots(
        this Expr left,
        Expr right,
        double leftLimit = double.NegativeInfinity,
        double rightLimit = double.PositiveInfinity,
        int scanSteps = DefaultRealScanSteps)
    {
        Expr diff = new Subtract(left, right).Simplify(SimplifyMode.Strict); // keep singular points: x^2/x = 0 has no root at 0
        return diff.FindRealRoots(leftLimit, rightLimit, scanSteps);
    }

    /// <summary>
    /// All real roots of <paramref name="expr"/> = 0 for its only variable.
    /// See <see cref="FindRealRoots(Expr, string, IReadOnlyDictionary{string, double}, double, double, int)"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The expression does not have exactly one variable.</exception>
    public static IReadOnlyList<double> FindRealRoots(
        this Expr expr,
        double leftLimit = double.NegativeInfinity,
        double rightLimit = double.PositiveInfinity,
        int scanSteps = DefaultRealScanSteps)
    {
        string variable = expr.GetSingleVariable();
        return expr.FindRealRoots(variable, null, leftLimit, rightLimit, scanSteps);
    }

    private static Func<double, double> MakeInfiniteMapping(
        double leftLimit, double rightLimit, bool leftInf, bool rightInf,
        out double tMin, out double tMax)
    {
        double eps = InfMappingEdgeEpsilon;

        if (leftInf && rightInf)
        {
            tMin = -1.0 + eps;
            tMax = 1.0 - eps;
            return t => t / (1.0 - t * t);
        }

        if (rightInf)
        {
            double a = leftLimit;
            tMin = 0.0;
            tMax = 1.0 - eps;
            return t => a + t / (1.0 - t);
        }

        double b = rightLimit;
        tMin = 0.0;
        tMax = 1.0 - eps;
        return t => b - t / (1.0 - t);
    }

    // Scans a grid in parameter space t (x = mapToX(t); the identity for finite limits) and
    // reports three kinds of roots, each located to full double precision:
    //   1. an exact zero on a grid point;
    //   2. a strict sign change between neighbours (odd multiplicity) - bisection on f;
    //   3. a touching root (even multiplicity, e.g. x^2 or (x-1)^2): |f| has a local minimum
    //      with no sign change around it - bisection on f', then accepted only if f ~ 0 there.
    // Zero is never treated as a sign of its own, which used to turn a grid hit on a double
    // root into a fake "sign change" and a second, slightly-off root next to it.
    private static IReadOnlyList<double> ScanForRealRoots(
        Expr expr, string variable, IReadOnlyDictionary<string, double>? fixedBindings,
        Func<double, double> mapToX, double tMin, double tMax, int scanSteps)
    {
        double F(double x) => SafeEvaluate(expr, variable, fixedBindings, x);

        var xs = new double[scanSteps + 1];
        var fs = new double[scanSteps + 1];
        double tStep = (tMax - tMin) / scanSteps;
        for (int i = 0; i <= scanSteps; i++)
        {
            xs[i] = mapToX(i == scanSteps ? tMax : tMin + i * tStep);
            fs[i] = F(xs[i]);
        }

        // Only needed for touching roots; built lazily since most scans never get there.
        Func<double, double>? derivative = null;
        bool derivativeTried = false;

        var roots = new List<double>();

        for (int i = 0; i <= scanSteps; i++)
        {
            if (!double.IsFinite(fs[i]))
                continue;

            if (fs[i] == 0)
            {
                TryAdd(roots, xs[i]);
                continue;
            }

            if (i < scanSteps && double.IsFinite(fs[i + 1]) && fs[i + 1] != 0 &&
                Math.Sign(fs[i]) != Math.Sign(fs[i + 1]))
            {
                double r = BisectToPrecision(F, xs[i], xs[i + 1], fs[i]);

                // A pole changes sign too (1/x at 0): near a root |f| shrinks, near a pole it grows.
                if (Math.Abs(F(r)) <= Math.Min(Math.Abs(fs[i]), Math.Abs(fs[i + 1])))
                    TryAdd(roots, r);
            }

            if (IsTouchingCandidate(fs, i, scanSteps))
            {
                if (!derivativeTried)
                {
                    derivative = TryBuildDerivative(expr, variable, fixedBindings);
                    derivativeTried = true;
                }

                if (derivative is not null &&
                    TryFindTouchingRoot(F, derivative, xs[i - 1], xs[i + 1], fs[i - 1], fs[i + 1]) is double c)
                {
                    TryAdd(roots, c);
                }
            }
        }

        roots.Sort();
        return roots;
    }

    // |f| has a strict local minimum at i and all three values share one (nonzero) sign.
    private static bool IsTouchingCandidate(double[] fs, int i, int last)
    {
        if (i == 0 || i == last)
            return false;

        double prev = fs[i - 1], cur = fs[i], next = fs[i + 1];
        if (!double.IsFinite(prev) || !double.IsFinite(next))
            return false;

        int sign = Math.Sign(cur);
        return Math.Sign(prev) == sign && Math.Sign(next) == sign &&
               Math.Abs(cur) < Math.Abs(prev) && Math.Abs(cur) <= Math.Abs(next);
    }

    private static Func<double, double>? TryBuildDerivative(
        Expr expr, string variable, IReadOnlyDictionary<string, double>? fixedBindings)
    {
        try
        {
            Expr d = expr.Differentiate(variable);
            return x => SafeEvaluate(d, variable, fixedBindings, x);
        }
        catch (NotImplementedException)
        {
            return null; // e.g. floor/sign: no symbolic derivative, so touching roots can't be located
        }
    }

    // At a touching root f' changes sign; find that point and accept it only if f is
    // (numerically) zero there, relative to the size of f on the surrounding grid points.
    private static double? TryFindTouchingRoot(
        Func<double, double> f, Func<double, double> derivative,
        double a, double b, double fa, double fb)
    {
        double da = derivative(a), db = derivative(b);
        if (!double.IsFinite(da) || !double.IsFinite(db) || da == 0 || db == 0 || Math.Sign(da) == Math.Sign(db))
            return null;

        double c = BisectToPrecision(derivative, a, b, da);
        double scale = Math.Max(1.0, Math.Max(Math.Abs(fa), Math.Abs(fb)));

        return Math.Abs(f(c)) <= TouchingRootTolerance * scale ? c : null;
    }

    // Bisects [a, b] (with g(a) = ga and g(b) of the opposite sign) until the interval
    // can't be split any further in double precision.
    private static double BisectToPrecision(Func<double, double> g, double a, double b, double ga)
    {
        for (int i = 0; i < 200; i++)
        {
            double mid = a + (b - a) / 2;
            if (mid == a || mid == b)
                break;

            double gm = g(mid);
            if (gm == 0)
                return mid;
            if (!double.IsFinite(gm))
                break;

            if (Math.Sign(gm) == Math.Sign(ga))
            {
                a = mid;
                ga = gm;
            }
            else
            {
                b = mid;
            }
        }

        return a + (b - a) / 2;
    }

    /// <summary>
    /// Complex roots of <paramref name="expr"/> = 0 in the rectangle
    /// [<paramref name="reMin"/>, <paramref name="reMax"/>] × [<paramref name="imMin"/>, <paramref name="imMax"/>]i,
    /// found by Newton's method started from every point of a grid.
    /// </summary>
    /// <remarks>
    /// A numeric method without a completeness guarantee: a root is found only if some grid start
    /// converges to it. Increase <paramref name="gridSteps"/> for more starting points.
    /// </remarks>
    /// <param name="expr">The function whose zeros are wanted.</param>
    /// <param name="variable">The variable to solve for.</param>
    /// <param name="fixedBindings">Values for the other variables, if any.</param>
    /// <param name="reMin">Lower bound of the real part.</param>
    /// <param name="reMax">Upper bound of the real part.</param>
    /// <param name="imMin">Lower bound of the imaginary part.</param>
    /// <param name="imMax">Upper bound of the imaginary part.</param>
    /// <param name="gridSteps">Grid intervals per axis; (gridSteps + 1)^2 starting points.</param>
    /// <returns>The distinct roots found, in no particular order.</returns>
    /// <exception cref="ArgumentException">A minimum is not less than its maximum.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="gridSteps"/> is less than 1.</exception>
    public static IReadOnlyList<ComplexNumber> FindComplexRoots(
        this Expr expr,
        string variable,
        IReadOnlyDictionary<string, ComplexNumber>? fixedBindings,
        double reMin, double reMax,
        double imMin, double imMax,
        int gridSteps = DefaultComplexGridSteps)
    {
        if (reMin >= reMax || imMin >= imMax)
            throw new ArgumentException("Min must be less than max for both real and imaginary ranges.");

        if (gridSteps < 1)
            throw new ArgumentOutOfRangeException(nameof(gridSteps));

        var roots = new List<ComplexNumber>();

        double reStep = (reMax - reMin) / gridSteps;
        double imStep = (imMax - imMin) / gridSteps;

        for (int i = 0; i <= gridSteps; i++)
        {
            for (int j = 0; j <= gridSteps; j++)
            {
                double re = reMin + i * reStep;
                double im = imMin + j * imStep;

                var guess = new ComplexNumber(re, im);
                var (root, found) = expr.TryFindComplexRoot(variable, guess, fixedBindings);

                if (found && root is ComplexNumber r && IsWithinBounds(r, reMin, reMax, imMin, imMax))
                    TryAddComplex(roots, r);
            }
        }

        return roots;
    }

    /// <summary>
    /// Complex roots of <paramref name="expr"/> = 0 for its only variable.
    /// See <see cref="FindComplexRoots(Expr, string, IReadOnlyDictionary{string, ComplexNumber}, double, double, double, double, int)"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The expression does not have exactly one variable.</exception>
    public static IReadOnlyList<ComplexNumber> FindComplexRoots(
        this Expr expr,
        double reMin, double reMax,
        double imMin, double imMax,
        int gridSteps = DefaultComplexGridSteps)
    {
        string variable = expr.GetSingleVariable();
        return expr.FindComplexRoots(variable, null, reMin, reMax, imMin, imMax, gridSteps);
    }

    /// <summary>
    /// Complex solutions of <paramref name="left"/> = <paramref name="right"/> for the equation's only
    /// variable, skipping points where either side is undefined.
    /// See <see cref="FindComplexRoots(Expr, string, IReadOnlyDictionary{string, ComplexNumber}, double, double, double, double, int)"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The equation does not have exactly one variable.</exception>
    public static IReadOnlyList<ComplexNumber> FindComplexRoots(
        this Expr left,
        Expr right,
        double reMin, double reMax,
        double imMin, double imMax,
        int gridSteps = DefaultComplexGridSteps)
    {
        Expr diff = new Subtract(left, right).Simplify(SimplifyMode.Strict); // keep singular points: x^2/x = 0 has no root at 0
        return diff.FindComplexRoots(reMin, reMax, imMin, imMax, gridSteps);
    }

    private static bool IsWithinBounds(ComplexNumber z, double reMin, double reMax, double imMin, double imMax)
    {
        double margin = 0.05 * Math.Max(reMax - reMin, imMax - imMin);
        return z.Real >= reMin - margin && z.Real <= reMax + margin &&
               z.Imaginary >= imMin - margin && z.Imaginary <= imMax + margin;
    }

    private static void TryAddComplex(List<ComplexNumber> roots, ComplexNumber candidate)
    {
        if (!roots.Any(r => (r - candidate).Magnitude < ComplexRootMergeTolerance))
            roots.Add(candidate);
    }

    private static double SafeEvaluate(Expr expr, string variable, IReadOnlyDictionary<string, double>? fixedBindings, double x)
    {
        try
        {
            var dict = fixedBindings is null
                ? new Dictionary<string, double>()
                : new Dictionary<string, double>(fixedBindings);
            dict[variable] = x;
            return expr.Evaluate(dict);
        }
        catch { return double.NaN; }
    }

    private static void TryAdd(List<double> roots, double candidate)
    {
        if (!roots.Any(r => Math.Abs(r - candidate) < RootMergeTolerance))
            roots.Add(candidate);
    }
}