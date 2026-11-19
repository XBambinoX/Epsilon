using Epsilon.Core;

namespace Epsilon.Solving;

// The complex roots of a function of one variable, numerically: those in a rectangle by Newton's
// method from every point of a grid, or one near a guess. Points where the function is undefined
// are never roots.
internal static class ComplexRoots
{
    private const int MinGridSteps = 12;
    private const int MaxGridSteps = 200;
    private const int MaxPolishSteps = 50;
    private const int MaxNewtonSteps = 100;
    private const double NewtonTolerance = 1e-10;

    // |z| below which a part of a root may just be underflow (see ResolvePlateau).
    private const double UnderflowPlateauLimit = 1e-100;

    // Where IsSameRoot looks at |f| between two candidates: the fractional parts of k * golden
    // ratio, spread over (0, 1) but never in step with evenly spaced roots. Samples at k/16 would
    // all land on roots of sin(10x) between -0.8pi and 0.8pi and merge the two.
    private static readonly double[] SegmentSamples = [.. Enumerable.Range(1, 16).Select(k => k * 0.6180339887498949 % 1)];

    // The roots in the rectangle with corners low and high, each once. Without a completeness
    // guarantee: a root is found only if some start converges to it.
    internal static List<ComplexNumber> InRectangle(Expr f, string variable, ComplexNumber low, ComplexNumber high)
    {
        Expr slope = f.Differentiate(variable);
        Func<ComplexNumber, ComplexNumber> F = Function(f, variable), dF = Function(slope, variable),
            d2F = Function(slope.Differentiate(variable), variable);

        // Two starts per half period of the fastest sine or cosine along each side.
        double fastest = RealRoots.Frequencies(f, variable).DefaultIfEmpty(0).Max();
        int reSteps = GridSteps(high.Real - low.Real, fastest), imSteps = GridSteps(high.Imaginary - low.Imaginary, fastest);

        var roots = new List<(ComplexNumber Root, double Residual)>();
        for (int i = 0; i <= reSteps; i++)
        {
            for (int j = 0; j <= imSteps; j++)
            {
                var guess = new ComplexNumber(
                    low.Real + i * (high.Real - low.Real) / reSteps,
                    low.Imaginary + j * (high.Imaginary - low.Imaginary) / imSteps);
                if (Newton(F, dF, guess) is not { } root
                    || Polished(F, dF, d2F, root) is not var (polished, residual)
                    || ResolvePlateau(F, polished, residual) is not { } resolved
                    || !(low.Real <= resolved.Real && resolved.Real <= high.Real && low.Imaginary <= resolved.Imaginary && resolved.Imaginary <= high.Imaginary))
                    continue;

                AddRoot(roots, F, resolved, residual);
            }
        }

        return [.. roots.Select(root => root.Root)];
    }

    // A root near the guess by Newton's method, then polished; null if the iteration doesn't converge.
    internal static ComplexNumber? Near(Expr f, string variable, ComplexNumber guess)
    {
        Expr slope = f.Differentiate(variable);
        Func<ComplexNumber, ComplexNumber> F = Function(f, variable), dF = Function(slope, variable);
        return Newton(F, dF, guess) is { } root && Polished(F, dF, Function(slope.Differentiate(variable), variable), root) is var (polished, residual)
            ? ResolvePlateau(F, polished, residual)
            : null;
    }

    internal static Func<ComplexNumber, ComplexNumber> Function(Expr f, string variable)
    {
        // One dictionary per function, with the variable overwritten on every call.
        var bindings = new Dictionary<string, ComplexNumber>();
        return z =>
        {
            bindings[variable] = z;
            try
            {
                return f.EvaluateComplex(bindings);
            }
            catch
            {
                return new ComplexNumber(double.NaN, double.NaN);
            }
        };
    }

    private static int GridSteps(double side, double frequency)
    {
        double steps = Math.Ceiling(2 * side * frequency / Math.PI);
        return double.IsFinite(steps) ? (int)Math.Clamp(steps, MinGridSteps, MaxGridSteps) : MinGridSteps;
    }

    private static ComplexNumber? Newton(Func<ComplexNumber, ComplexNumber> f, Func<ComplexNumber, ComplexNumber> derivative, ComplexNumber z)
    {
        for (int i = 0; i < MaxNewtonSteps; i++)
        {
            ComplexNumber fz = f(z);
            if (fz.Magnitude < NewtonTolerance)
                return z;

            ComplexNumber dfz = derivative(z);
            if (dfz.Magnitude < 1e-14)
                return null;

            z -= fz / dfz;
            if (!double.IsFinite(z.Real) || !double.IsFinite(z.Imaginary))
                return null;
        }

        return null;
    }

    // Newton's method converges only linearly to a root of multiplicity m and stops (|f| < 1e-10)
    // about (1e-10)^(1/m) away from it, at a different point for every start: (x - 1)^2 gave 80
    // roots around 0.99999. Newton's method on f/f', whose roots are all simple, converges
    // quadratically whatever m is: z -= f f' / (f'^2 - f f''). A step is kept only if it lowers
    // |f|: once rounding noise dominates next to the root, a step can jump far away from it.
    // A step onto a point where f is undefined means there was no root, only a hole that f
    // tends to 0 at: x^2/x steps onto 0, (x - 1)^2/(x - 1) onto 1.
    private static (ComplexNumber Root, double Residual)? Polished(
        Func<ComplexNumber, ComplexNumber> f, Func<ComplexNumber, ComplexNumber> df, Func<ComplexNumber, ComplexNumber> d2f, ComplexNumber z)
    {
        ComplexNumber fz = f(z);
        for (int i = 0; i < MaxPolishSteps && fz != ComplexNumber.Zero; i++)
        {
            ComplexNumber d1 = df(z);
            ComplexNumber next = z - fz * d1 / (d1 * d1 - fz * d2f(z));
            if (!double.IsFinite(next.Real) || !double.IsFinite(next.Imaginary))
                break;

            ComplexNumber fNext = f(next);
            if (!double.IsFinite(fNext.Real) || !double.IsFinite(fNext.Imaginary))
                return null;
            if (!(fNext.Magnitude < fz.Magnitude))
                break;

            (z, fz) = (next, fNext);
        }

        return (z, fz.Magnitude);
    }

    // Towards a point where f is undefined but tends to 0, f can underflow to 0 first: x^2*ln(x)
    // at 1e-162, or x*ln(x) still creeping at 5e-246 when polishing ends. Parts of a root below
    // UnderflowPlateauLimit are such leftovers and the point without them decides: no root where
    // f is undefined, else that point if f is no larger there (1, not 1 + 1e-276i).
    // Then parts below rounding next to the rest go too where f stays as small: 2 pi i, not
    // -2.6e-17 + 2 pi i, for exp(x) = 1. A point where f is undefined then keeps them.
    private static ComplexNumber? ResolvePlateau(Func<ComplexNumber, ComplexNumber> f, ComplexNumber z, double residual)
    {
        ComplexNumber snapped = Snapped(z, UnderflowPlateauLimit);
        if (snapped != z)
        {
            ComplexNumber atSnapped = f(snapped);
            if (!double.IsFinite(atSnapped.Real) || !double.IsFinite(atSnapped.Imaginary))
                return null;
            if (atSnapped.Magnitude <= residual)
                (z, residual) = (snapped, atSnapped.Magnitude);
        }

        ComplexNumber rounded = Snapped(z, 1e-15 * Math.Max(1, z.Magnitude));
        return rounded != z && f(rounded).Magnitude <= residual ? rounded : z;
    }

    private static ComplexNumber Snapped(ComplexNumber z, double below) =>
        new(Math.Abs(z.Real) < below ? 0 : z.Real, Math.Abs(z.Imaginary) < below ? 0 : z.Imaginary);

    // Keeps one entry per root: the candidate with the smallest |f|.
    private static void AddRoot(List<(ComplexNumber Root, double Residual)> roots, Func<ComplexNumber, ComplexNumber> f, ComplexNumber candidate, double residual)
    {
        int index = roots.FindIndex(root => IsSameRoot(f, root.Root, candidate));
        if (index < 0)
            roots.Add((candidate, residual));
        else if (residual < roots[index].Residual)
            roots[index] = (candidate, residual);
    }

    // Two candidates are the same root when |f| stays below the root tolerance between them:
    // every point there counts as a root, so the two can't be told apart. This merges the
    // candidates for a multiple root of an expanded polynomial, which rounding noise leaves
    // slightly scattered, while x^3 - x keeps -1 and 1 (|f| reaches 0.38 between them).
    private static bool IsSameRoot(Func<ComplexNumber, ComplexNumber> f, ComplexNumber a, ComplexNumber b)
    {
        if (a == b)
            return true;

        foreach (double t in SegmentSamples)
            if (!(f(a + (b - a) * t).Magnitude < NewtonTolerance))
                return false;

        return true;
    }
}
