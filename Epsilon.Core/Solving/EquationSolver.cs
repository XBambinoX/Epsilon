namespace Epsilon.Core;

/// <summary>Numeric search for all roots of an expression or equation in a range.</summary>
public static class RootFindingExtensions
{
    private const int DefaultRealScanSteps = 200;
    private const double RootMergeTolerance = 1e-6;

    // A local minimum of |f| counts as a (touching) root only if f there is this small
    // relative to f on the neighbouring grid points - x^2 + 1e-7 must not have a root.
    private const double TouchingRootTolerance = 1e-10;

    // |f| at the last defined point before a domain edge, relative to f on the grid, below
    // which the edge may be a root. Looser than for touching roots: next to a square-root
    // edge that isn't a double, f is still about sqrt(1 ulp) ~ 1e-8 away from 0.
    private const double EdgeRootTolerance = 1e-6;

    // |x| below which an exact zero of f may just be underflow (see ResolveZeroPlateau).
    private const double UnderflowPlateauLimit = 1e-100;
    private const double InfMappingEdgeEpsilon = 1e-9;

    private const int DefaultComplexGridSteps = 12;
    private const int MaxComplexPolishSteps = 50;

    // Where IsSameComplexRoot looks at |f| between two candidates: the fractional parts of
    // k * golden ratio, spread over (0, 1) but never in step with evenly spaced roots. Samples at
    // k/16 would all land on roots of sin(10x) between -0.8pi and 0.8pi and merge the two.
    private static readonly double[] SegmentSamples =
        [.. Enumerable.Range(1, 16).Select(k => k * 0.6180339887498949 % 1)];

    // The equation overloads of FindRealRoots/FindComplexRoots were a trap: a plain number as the
    // right side converts to Expr only implicitly, so the range overload wins and
    // sin.FindRealRoots(-1, 2, 3) searched sin(x) = 0 on [-1, 2] instead of solving sin(x) = -1.
    private const string ObsoleteRealEquation =
        "Use SolveNumerically. A plain number as the right side makes FindRealRoots pick its range overload instead.";
    private const string ObsoleteComplexEquation =
        "Use SolveComplexNumerically. A plain number as the right side makes FindComplexRoots pick its rectangle overload instead.";

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
    public static IReadOnlyList<double> SolveNumerically(
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
    /// All real solutions of <paramref name="left"/> = <paramref name="right"/> for the equation's
    /// only variable. See <see cref="SolveNumerically(Expr, Expr, string, IReadOnlyDictionary{string, double}, double, double, int)"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The equation does not have exactly one variable.</exception>
    public static IReadOnlyList<double> SolveNumerically(
        this Expr left,
        Expr right,
        double leftLimit = double.NegativeInfinity,
        double rightLimit = double.PositiveInfinity,
        int scanSteps = DefaultRealScanSteps)
    {
        Expr diff = new Subtract(left, right).Simplify(SimplifyMode.Strict); // keep singular points: x^2/x = 0 has no root at 0
        return diff.FindRealRoots(leftLimit, rightLimit, scanSteps);
    }

    /// <summary>Obsolete: use <see cref="SolveNumerically(Expr, Expr, string, IReadOnlyDictionary{string, double}, double, double, int)"/>.</summary>
    [Obsolete(ObsoleteRealEquation)]
    public static IReadOnlyList<double> FindRealRoots(
        this Expr left,
        Expr right,
        string variable,
        IReadOnlyDictionary<string, double>? fixedBindings = null,
        double leftLimit = double.NegativeInfinity,
        double rightLimit = double.PositiveInfinity,
        int scanSteps = DefaultRealScanSteps) =>
        left.SolveNumerically(right, variable, fixedBindings, leftLimit, rightLimit, scanSteps);

    /// <summary>Obsolete: use <see cref="SolveNumerically(Expr, Expr, double, double, int)"/>.</summary>
    [Obsolete(ObsoleteRealEquation)]
    public static IReadOnlyList<double> FindRealRoots(
        this Expr left,
        Expr right,
        double leftLimit = double.NegativeInfinity,
        double rightLimit = double.PositiveInfinity,
        int scanSteps = DefaultRealScanSteps) =>
        left.SolveNumerically(right, leftLimit, rightLimit, scanSteps);

    /// <summary>
    /// All real roots of <paramref name="expr"/> = 0 for <paramref name="variable"/> in the range,
    /// found by scanning a grid and refining each sign change, touching point (even-multiplicity
    /// roots such as x^2) and domain edge (sqrt(x) at 0) by bisection. Points where the expression
    /// is undefined are never reported. Infinite limits are handled by a change of variable.
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
            // The domain ends between these two grid points (sqrt(x) around 0): a root can
            // sit exactly on the edge, where no sign change is visible.
            if (i < scanSteps && double.IsFinite(fs[i]) != double.IsFinite(fs[i + 1]))
            {
                bool leftDefined = double.IsFinite(fs[i]);
                double definedValue = leftDefined ? fs[i] : fs[i + 1];

                if (TryFindDomainEdgeRoot(
                        expr, variable, fixedBindings, F,
                        definedX: leftDefined ? xs[i] : xs[i + 1],
                        undefinedX: leftDefined ? xs[i + 1] : xs[i],
                        scale: Math.Max(1.0, Math.Abs(definedValue))) is double edgeRoot)
                {
                    TryAdd(roots, edgeRoot);
                }
            }

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
                // A pole changes sign too (1/x at 0): near a root |f| shrinks, near a pole it grows.
                if (BisectToPrecision(F, xs[i], xs[i + 1], fs[i]) is double r &&
                    Math.Abs(F(r)) <= Math.Min(Math.Abs(fs[i]), Math.Abs(fs[i + 1])))
                {
                    TryAdd(roots, r);
                }
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
        // e.g. floor/sign: no symbolic derivative yet, so touching roots can't be located.
        // NotImplementedException too, for node types defined outside this library.
        catch (Exception e) when (e is NotSupportedException or NotImplementedException)
        {
            return null;
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

        if (BisectToPrecision(derivative, a, b, da) is not double c)
            return null;

        double scale = Math.Max(1.0, Math.Max(Math.Abs(fa), Math.Abs(fb)));

        return Math.Abs(f(c)) <= TouchingRootTolerance * scale ? c : null;
    }

    // Bisects between a and b (g(a) = ga, g(b) of the opposite sign) down to two adjacent
    // doubles. Works on the ordered bit patterns like BisectDomainEdge, so it takes at most
    // 64 steps and reaches exact zeros such as x = 0 (halving values stalled around 1e-63).
    // Returns null if g is undefined somewhere on the way: the sign change then goes through
    // a hole or a pole (x^2/x or 1/x at 0), not through a root.
    private static double? BisectToPrecision(Func<double, double> g, double a, double b, double ga)
    {
        long lo = ToOrderedBits(a), hi = ToOrderedBits(b);
        double gb = g(b);

        while (Int128.Abs((Int128)hi - lo) > 1)
        {
            long midBits = (long)(((Int128)lo + hi) / 2);
            double mid = FromOrderedBits(midBits);
            double gm = g(mid);

            if (gm == 0)
                return Math.Abs(mid) < UnderflowPlateauLimit ? ResolveZeroPlateau(g, mid) : mid;
            if (!double.IsFinite(gm))
                return null;

            if (Math.Sign(gm) == Math.Sign(ga))
            {
                lo = midBits;
                ga = gm;
            }
            else
            {
                hi = midBits;
                gb = gm;
            }
        }

        return Math.Abs(ga) <= Math.Abs(gb) ? FromOrderedBits(lo) : FromOrderedBits(hi);
    }

    /// <summary>
    /// Complex roots of <paramref name="expr"/> = 0 in the rectangle
    /// [<paramref name="reMin"/>, <paramref name="reMax"/>] × [<paramref name="imMin"/>, <paramref name="imMax"/>]i,
    /// found by Newton's method started from every point of a grid.
    /// </summary>
    /// <remarks>
    /// A numeric method without a completeness guarantee: a root is found only if some grid start
    /// converges to it. Increase <paramref name="gridSteps"/> for more starting points. Each root
    /// is reported once, a multiple root too; roots so close that |f| &lt; 1e-10 everywhere between
    /// them can't be told apart and are reported as one.
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

        Expr derivative = expr.Differentiate(variable);
        var f = RootFinder.ComplexFunction(expr, variable, fixedBindings);
        var df = RootFinder.ComplexFunction(derivative, variable, fixedBindings);
        var d2f = RootFinder.ComplexFunction(derivative.Differentiate(variable), variable, fixedBindings);

        var roots = new List<(ComplexNumber Root, double Residual)>();

        double reStep = (reMax - reMin) / gridSteps;
        double imStep = (imMax - imMin) / gridSteps;

        for (int i = 0; i <= gridSteps; i++)
        {
            for (int j = 0; j <= gridSteps; j++)
            {
                double re = reMin + i * reStep;
                double im = imMin + j * imStep;

                var guess = new ComplexNumber(re, im);
                var (root, found) = RootFinder.NewtonComplex(f, df, guess);
                if (!found || root is not ComplexNumber r)
                    continue;

                var (polished, residual) = PolishComplexRoot(f, df, d2f, r);
                if (IsWithinBounds(polished, reMin, reMax, imMin, imMax))
                    AddComplexRoot(roots, f, polished, residual);
            }
        }

        return [.. roots.Select(r => r.Root)];
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
    public static IReadOnlyList<ComplexNumber> SolveComplexNumerically(
        this Expr left,
        Expr right,
        double reMin, double reMax,
        double imMin, double imMax,
        int gridSteps = DefaultComplexGridSteps)
    {
        Expr diff = new Subtract(left, right).Simplify(SimplifyMode.Strict); // keep singular points: x^2/x = 0 has no root at 0
        return diff.FindComplexRoots(reMin, reMax, imMin, imMax, gridSteps);
    }

    /// <summary>Obsolete: use <see cref="SolveComplexNumerically(Expr, Expr, double, double, double, double, int)"/>.</summary>
    [Obsolete(ObsoleteComplexEquation)]
    public static IReadOnlyList<ComplexNumber> FindComplexRoots(
        this Expr left,
        Expr right,
        double reMin, double reMax,
        double imMin, double imMax,
        int gridSteps = DefaultComplexGridSteps) =>
        left.SolveComplexNumerically(right, reMin, reMax, imMin, imMax, gridSteps);

    private static bool IsWithinBounds(ComplexNumber z, double reMin, double reMax, double imMin, double imMax)
    {
        double margin = 0.05 * Math.Max(reMax - reMin, imMax - imMin);
        return z.Real >= reMin - margin && z.Real <= reMax + margin &&
               z.Imaginary >= imMin - margin && z.Imaginary <= imMax + margin;
    }

    // Newton's method converges only linearly to a root of multiplicity m and stops (|f| < 1e-10)
    // about (1e-10)^(1/m) away from it, at a different point for every start: (x - 1)^2 gave 80
    // roots around 0.99999. Newton's method on f/f', whose roots are all simple, converges
    // quadratically whatever m is: z -= f f' / (f'^2 - f f''). A step is kept only if it lowers
    // |f|: once rounding noise dominates next to the root, a step can jump far away from it.
    private static (ComplexNumber Root, double Residual) PolishComplexRoot(
        Func<ComplexNumber, ComplexNumber> f, Func<ComplexNumber, ComplexNumber> df,
        Func<ComplexNumber, ComplexNumber> d2f, ComplexNumber z)
    {
        ComplexNumber fz = f(z);

        for (int i = 0; i < MaxComplexPolishSteps && fz != ComplexNumber.Zero; i++)
        {
            ComplexNumber d1 = df(z);
            ComplexNumber next = z - fz * d1 / (d1 * d1 - fz * d2f(z));
            if (!double.IsFinite(next.Real) || !double.IsFinite(next.Imaginary))
                break;

            ComplexNumber fNext = f(next);
            if (!(fNext.Magnitude < fz.Magnitude)) // also stops on NaN
                break;

            (z, fz) = (next, fNext);
        }

        return (z, fz.Magnitude);
    }

    // Keeps one entry per root: the candidate with the smallest |f|.
    private static void AddComplexRoot(
        List<(ComplexNumber Root, double Residual)> roots, Func<ComplexNumber, ComplexNumber> f,
        ComplexNumber candidate, double residual)
    {
        int index = roots.FindIndex(r => IsSameComplexRoot(f, r.Root, candidate));
        if (index < 0)
            roots.Add((candidate, residual));
        else if (residual < roots[index].Residual)
            roots[index] = (candidate, residual);
    }

    // Two candidates are the same root when |f| stays below the root tolerance between them:
    // every point there counts as a root, so the two can't be told apart. This merges the
    // candidates for a multiple root of an expanded polynomial, which rounding noise leaves
    // slightly scattered, while x^3 - x keeps -1 and 1 (|f| reaches 0.38 between them).
    private static bool IsSameComplexRoot(Func<ComplexNumber, ComplexNumber> f, ComplexNumber a, ComplexNumber b)
    {
        if (a == b)
            return true;

        foreach (double t in SegmentSamples)
            if (!(f(a + (b - a) * t).Magnitude < RootFinder.DefaultTolerance))
                return false;

        return true;
    }

    // A root on the edge of the domain is accepted if f is (nearly) 0 at the last defined
    // point and carries on through the edge over the complex numbers: sqrt(x) at 0,
    // sqrt(1 - x^2) at 1, sqrt(2 - x^2) at sqrt(2) (an edge that is not a double, so f is
    // only ~1e-8 there). The complex check rejects functions that merely tend to 0 towards a
    // point where they are undefined - x*ln(x) and x^2/x at 0 give 0*(-inf) and 0/0 there
    // even over C - so, as everywhere else, no root is reported where f itself is undefined.
    // Even an exact 0 needs the check: x^2/x is exactly 0 at x = -5e-324, where x^2 underflows.
    private static double? TryFindDomainEdgeRoot(
        Expr expr, string variable, IReadOnlyDictionary<string, double>? fixedBindings,
        Func<double, double> f, double definedX, double undefinedX, double scale)
    {
        var (edge, beyond) = BisectDomainEdge(f, definedX, undefinedX);

        double tolerance = EdgeRootTolerance * scale;
        if (!(Math.Abs(f(edge)) <= tolerance))
            return null;

        double continued = SafeEvaluateComplex(expr, variable, fixedBindings, beyond).Magnitude;
        return continued <= tolerance ? edge : null; // false for NaN
    }

    // Bisects between a point where f is finite and one where it isn't, down to two adjacent
    // doubles. Works on the doubles' ordered bit patterns rather than their values, so it
    // always ends in at most 64 steps and can land exactly on an edge like 0 or 1.
    private static (double Defined, double Undefined) BisectDomainEdge(
        Func<double, double> f, double defined, double undefined)
    {
        long d = ToOrderedBits(defined), u = ToOrderedBits(undefined);

        while (Int128.Abs((Int128)d - u) > 1)
        {
            long mid = (long)(((Int128)d + u) / 2);
            if (double.IsFinite(f(FromOrderedBits(mid))))
                d = mid;
            else
                u = mid;
        }

        return (FromOrderedBits(d), FromOrderedBits(u));
    }

    // Next to x = 0, powers underflow: x^3 and x^2/x are exactly 0 on a whole plateau of
    // tiny x, so a zero found there says nothing about where the root is. x = 0 itself
    // decides: a root at 0 (x^3), or a hole (x^2/x, undefined at 0) - then no root at all.
    private static double? ResolveZeroPlateau(Func<double, double> g, double zeroAt)
    {
        double atZero = g(0);
        if (!double.IsFinite(atZero))
            return null;

        return atZero == 0 ? 0 : zeroAt;
    }

    // Maps doubles to longs monotonically (-0 and +0 both to 0), so neighbouring doubles
    // are neighbouring longs.
    private static long ToOrderedBits(double x)
    {
        long bits = BitConverter.DoubleToInt64Bits(x);
        return bits >= 0 ? bits : long.MinValue - bits;
    }

    private static double FromOrderedBits(long ordered) =>
        BitConverter.Int64BitsToDouble(ordered >= 0 ? ordered : long.MinValue - ordered);

    private static ComplexNumber SafeEvaluateComplex(
        Expr expr, string variable, IReadOnlyDictionary<string, double>? fixedBindings, double x)
    {
        try
        {
            var dict = new Dictionary<string, ComplexNumber>();
            if (fixedBindings is not null)
                foreach (var (name, value) in fixedBindings)
                    dict[name] = value;
            dict[variable] = x;
            return expr.EvaluateComplex(dict);
        }
        catch { return new ComplexNumber(double.NaN, double.NaN); }
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