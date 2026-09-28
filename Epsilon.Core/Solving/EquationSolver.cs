namespace Epsilon.Core;

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

    public static IReadOnlyList<Complex> FindComplexRoots(
        this Expr expr,
        string variable,
        IReadOnlyDictionary<string, Complex>? fixedBindings,
        double reMin, double reMax,
        double imMin, double imMax,
        int gridSteps = DefaultComplexGridSteps)
    {
        if (reMin >= reMax || imMin >= imMax)
            throw new ArgumentException("Min must be less than max for both real and imaginary ranges.");

        if (gridSteps < 1)
            throw new ArgumentOutOfRangeException(nameof(gridSteps));

        var roots = new List<Complex>();

        double reStep = (reMax - reMin) / gridSteps;
        double imStep = (imMax - imMin) / gridSteps;

        for (int i = 0; i <= gridSteps; i++)
        {
            for (int j = 0; j <= gridSteps; j++)
            {
                double re = reMin + i * reStep;
                double im = imMin + j * imStep;

                var guess = new Complex(re, im);
                var (root, found) = expr.TryFindComplexRoot(variable, guess, fixedBindings);

                if (found && root is Complex r && IsWithinBounds(r, reMin, reMax, imMin, imMax))
                    TryAddComplex(roots, r);
            }
        }

        return roots;
    }

    public static IReadOnlyList<Complex> FindComplexRoots(
        this Expr expr,
        double reMin, double reMax,
        double imMin, double imMax,
        int gridSteps = DefaultComplexGridSteps)
    {
        string variable = expr.GetSingleVariable();
        return expr.FindComplexRoots(variable, null, reMin, reMax, imMin, imMax, gridSteps);
    }

    public static IReadOnlyList<Complex> FindComplexRoots(
        this Expr left,
        Expr right,
        double reMin, double reMax,
        double imMin, double imMax,
        int gridSteps = DefaultComplexGridSteps)
    {
        Expr diff = new Subtract(left, right).Simplify(SimplifyMode.Strict); // keep singular points: x^2/x = 0 has no root at 0
        return diff.FindComplexRoots(reMin, reMax, imMin, imMax, gridSteps);
    }

    private static bool IsWithinBounds(Complex z, double reMin, double reMax, double imMin, double imMax)
    {
        double margin = 0.05 * Math.Max(reMax - reMin, imMax - imMin);
        return z.Real >= reMin - margin && z.Real <= reMax + margin &&
               z.Imaginary >= imMin - margin && z.Imaginary <= imMax + margin;
    }

    private static void TryAddComplex(List<Complex> roots, Complex candidate)
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