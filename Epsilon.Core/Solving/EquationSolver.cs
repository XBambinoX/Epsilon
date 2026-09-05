namespace Epsilon.Core;

public static class RootFindingExtensions
{
    private const int DefaultRealScanSteps = 200;
    private const double RootMergeTolerance = 1e-6;
    private const double InfMappingEdgeEpsilon = 1e-9;

    private const int DefaultComplexGridSteps = 12;
    private const double ComplexRootMergeTolerance = 1e-6;

    public static IReadOnlyList<double> FindRealRoots(
        this Expr left,
        Expr right,
        double leftLimit = double.NegativeInfinity,
        double rightLimit = double.PositiveInfinity,
        int scanSteps = DefaultRealScanSteps)
    {
        Expr diff = new Subtract(left, right).Simplify();
        return diff.FindRealRoots(leftLimit, rightLimit, scanSteps);
    }

    public static IReadOnlyList<double> FindRealRoots(
        this Expr expr,
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
            return FindRealRootsFinite(expr, leftLimit, rightLimit, scanSteps);

        Func<double, double> mapToX = MakeInfiniteMapping(leftLimit, rightLimit, leftInf, rightInf, out double tMin, out double tMax);

        return FindRealRootsMapped(expr, mapToX, tMin, tMax, scanSteps);
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

    private static IReadOnlyList<double> FindRealRootsMapped(
        Expr expr, Func<double, double> mapToX, double tMin, double tMax, int scanSteps)
    {
        var roots = new List<double>();
        double tStep = (tMax - tMin) / scanSteps;

        double prevT = tMin;
        double prevX = mapToX(prevT);
        double prevF = SafeEvaluate(expr, prevX);

        for (int i = 1; i <= scanSteps; i++)
        {
            double t = tMin + i * tStep;
            double x = mapToX(t);
            double f = SafeEvaluate(expr, x);

            if (double.IsNaN(f) || double.IsInfinity(f))
            {
                prevT = t; prevX = x; prevF = f;
                continue;
            }

            if (Math.Abs(f) < RootMergeTolerance)
            {
                double direction = Math.Sign(t - prevT);
                if (f == 0)
                {
                    TryAdd(roots, x);
                }
                else
                {
                    double tFar = Math.Clamp(t + direction * tStep * 3, tMin, tMax);
                    double xFar = mapToX(tFar);
                    double fFar = SafeEvaluate(expr, xFar);

                    if (!IsAsymptoticApproach(f, fFar))
                        TryAdd(roots, x);
                }
            }
            else if (IsSignChange(prevF, f))
            {
                double guessX = (prevX + x) / 2;
                var (root, found) = expr.TryFindRoot(guessX);

                if (found && root is double r && IsBetween(r, prevX, x))
                {
                    TryAdd(roots, r);
                }
                else if (RootFinder.BisectFallback(expr, Math.Min(prevX, x), Math.Max(prevX, x)) is double br)
                {
                    TryAdd(roots, br);
                }
            }

            prevT = t; prevX = x; prevF = f;
        }

        roots.Sort();
        return roots;
    }

    private static IReadOnlyList<double> FindRealRootsFinite(
        Expr expr, double leftLimit, double rightLimit, int scanSteps)
    {
        var roots = new List<double>();
        double step = (rightLimit - leftLimit) / scanSteps;

        double previousX = leftLimit;
        double previousF = SafeEvaluate(expr, previousX);

        for (int i = 1; i <= scanSteps; i++)
        {
            double x = leftLimit + i * step;
            double f = SafeEvaluate(expr, x);

            if (double.IsNaN(f) || double.IsInfinity(f))
            {
                previousX = x;
                previousF = f;
                continue;
            }

            if (Math.Abs(f) < RootMergeTolerance)
            {
                double direction = Math.Sign(x - previousX);
                if (direction == 0) direction = 1;
                if (f == 0)
                {
                    TryAdd(roots, x);
                }
                else
                {
                    double xFar = Math.Clamp(x + direction * step * 3, leftLimit, rightLimit);
                    double fFar = SafeEvaluate(expr, xFar);

                    if (!IsAsymptoticApproach(f, fFar))
                        TryAdd(roots, x);
                }
            }
            else if (IsSignChange(previousF, f))
            {
                double guess = (previousX + x) / 2;
                var (root, found) = expr.TryFindRoot(guess);

                if (found && root is double r && r >= previousX && r <= x)
                {
                    TryAdd(roots, r);
                }
                else if (RootFinder.BisectFallback(expr, previousX, x) is double br)
                {
                    TryAdd(roots, br);
                }
            }

            previousX = x;
            previousF = f;
        }

        roots.Sort();
        return roots;
    }

    public static IReadOnlyList<Complex> FindComplexRoots(
        this Expr expr,
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
                var (root, found) = expr.TryFindComplexRoot(guess);

                if (found && root is Complex r && IsWithinBounds(r, reMin, reMax, imMin, imMax))
                    TryAddComplex(roots, r);
            }
        }

        return roots;
    }

    public static IReadOnlyList<Complex> FindComplexRoots(
        this Expr left,
        Expr right,
        double reMin, double reMax,
        double imMin, double imMax,
        int gridSteps = DefaultComplexGridSteps)
    {
        Expr diff = new Subtract(left, right).Simplify();
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

    private static bool IsAsymptoticApproach(double f, double fFar)
    {
        if (double.IsNaN(fFar) || double.IsInfinity(fFar)) return false;

        bool sameSignOrZero = Math.Sign(f) == Math.Sign(fFar) || f == 0 || fFar == 0;
        bool stillSmall = Math.Abs(fFar) < RootMergeTolerance * 10;

        return sameSignOrZero && stillSmall;
    }

    private static double SafeEvaluate(Expr expr, double x)
    {
        try { return expr.Evaluate(x); }
        catch { return double.NaN; }
    }

    private static bool IsBetween(double v, double a, double b)
        => v >= Math.Min(a, b) && v <= Math.Max(a, b);

    private static bool IsSignChange(double a, double b)
    {
        return
            !double.IsNaN(a) &&
            !double.IsNaN(b) &&
            !double.IsInfinity(a) &&
            !double.IsInfinity(b) &&
            Math.Sign(a) != Math.Sign(b);
    }

    private static void TryAdd(List<double> roots, double candidate)
    {
        if (!roots.Any(r => Math.Abs(r - candidate) < RootMergeTolerance))
            roots.Add(candidate);
    }
}