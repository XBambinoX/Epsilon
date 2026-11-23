using Epsilon.Core;

namespace Epsilon.Solving;

// The real roots of a function of one variable, numerically: all of them in a range by a scan,
// or one near a guess by Newton's method. Points where the function is undefined are never roots.
internal static class RealRoots
{
    private const int MinScanSteps = 200;
    private const int MaxScanSteps = 100_000;
    private const double MergeTolerance = 1e-6;

    // A local minimum of |f| counts as a (touching) root only if f there is this small
    // relative to f on the neighbouring grid points - x^2 + 1e-7 must not have a root.
    private const double TouchingRootTolerance = 1e-10;

    // |f| at the last defined point before a domain edge, relative to f on the grid, below
    // which the edge may be a root. Looser than for touching roots: next to a square-root
    // edge that isn't a double, f is still about sqrt(1 ulp) ~ 1e-8 away from 0.
    private const double EdgeRootTolerance = 1e-6;

    // |x| below which an exact zero of f may just be underflow (see ResolveZeroPlateau).
    private const double UnderflowPlateauLimit = 1e-100;
    private const double InfiniteMappingEdge = 1e-9;

    private const double NewtonTolerance = 1e-10;
    private const int MaxNewtonSteps = 100;

    // All roots in [from, to], in ascending order; a limit may be infinite. Infinite limits are
    // handled by a change of variable.
    internal static List<double> InRange(Expr f, string variable, double from, double to)
    {
        if (double.IsFinite(from) && double.IsFinite(to))
            return Scan(f, variable, Grid(f, variable, from, to));

        double edge = InfiniteMappingEdge;
        Func<double, double> mapToX = (double.IsInfinity(from), double.IsInfinity(to)) switch
        {
            (true, true) => t => t / (1 - t * t),
            (false, true) => t => from + t / (1 - t),
            _ => t => to - t / (1 - t),
        };
        double tMin = double.IsInfinity(from) && double.IsInfinity(to) ? -1 + edge : 0, tMax = 1 - edge;
        return Scan(f, variable, [.. Enumerable.Range(0, MinScanSteps + 1)
            .Select(i => mapToX(i == MinScanSteps ? tMax : tMin + i * (tMax - tMin) / MinScanSteps))]);
    }

    // A root near the guess by Newton's method, then polished while the steps make |f| smaller;
    // null if the iteration doesn't converge (a flat derivative, divergence, too many steps).
    internal static double? Near(Expr f, string variable, double guess)
    {
        Expr slope = f.Differentiate(variable);
        double x = guess;
        for (int i = 0; i < MaxNewtonSteps; i++)
        {
            double fx = At(f, variable, x);
            if (Math.Abs(fx) < NewtonTolerance)
                return Polished(f, slope, variable, x, fx);

            double next = x - fx / At(slope, variable, x);
            if (!double.IsFinite(next))
                return null;
            x = next;
        }

        return null;
    }

    private static double Polished(Expr f, Expr slope, string variable, double x, double fx) =>
        Polished(x => At(f, variable, x), x => At(slope, variable, x), x, fx, double.PositiveInfinity);

    // Polishes values next to roots of f, a few ulps at most, so that one worked out from an exact
    // value, as π / 6, becomes the double nearest the root. Compiled for many values: compiling
    // takes longer than evaluating a few times.
    internal static Func<double, double> Polisher(Expr f, string variable, bool many)
    {
        Func<double, double> F, slope;
        try
        {
            F = many ? Compiled(f, variable) : Function(f, variable);
            slope = many ? Compiled(f.Differentiate(variable), variable) : Function(f.Differentiate(variable), variable);
        }
        catch (Exception e) when (e is NotSupportedException or NotImplementedException)
        {
            return x => x;
        }

        return x => Polished(F, slope, x, F(x), 1e-12 * Math.Max(1, Math.Abs(x)));
    }

    // What Function gives, bit for bit, faster.
    private static Func<double, double> Compiled(Expr f, string variable)
    {
        Func<double, double> compiled;
        try
        {
            compiled = f.Compile(variable);
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or NotSupportedException)
        {
            return Function(f, variable);
        }

        return x =>
        {
            try
            {
                return compiled(x);
            }
            catch
            {
                return double.NaN;
            }
        };
    }

    private static double Polished(Func<double, double> f, Func<double, double> slope, double x, double fx, double maxStep)
    {
        for (int i = 0; i < 3 && fx != 0; i++)
        {
            double next = x - fx / slope(x), fNext = f(next);
            if (!(Math.Abs(fNext) < Math.Abs(fx)) || !(Math.Abs(next - x) <= maxStep))
                break;
            (x, fx) = (next, fNext);
        }

        return x;
    }

    // At least 200 steps, and 16 per pi of each sine or cosine where it oscillates: a step of
    // pi / (16 |u'(x)|) for sin(u). 200 even steps found 82 of the 318 zeros of sin(x) on
    // [0, 1000], and 11 of the 31 of sin(1/x) on [0.01, 1], where 1/x speeds up towards 0.01.
    private static double[] Grid(Expr f, string variable, double from, double to)
    {
        List<Func<double, double>> speeds = [.. Oscillating(f, variable).Select(argument => Slope(argument, variable)).OfType<Expr>()
            .Select(slope => Function(slope, variable))];
        double longest = (to - from) / MinScanSteps;
        double Density(double x) =>
            16 / Math.PI * speeds.Select(speed => Math.Abs(speed(x))).Where(double.IsFinite).DefaultIfEmpty(0).Max();

        // Where that would take more than 100 000 points, as for sin(x) on [0, 1e6], all the steps
        // grow alike, so that the grid still covers the whole range.
        double needed = speeds.Count == 0 ? 0 : Enumerable.Range(0, 1000).Average(i => Density(from + (i + 0.5) * (to - from) / 1000)) * (to - from);
        double scale = Math.Min(1, MaxScanSteps / needed);

        // Past 100 000 points even so, as for sin(1/x) on [1e-9, 1], the rest gets the longest steps.
        var xs = new List<double> { from };
        for (double x = from; x < to;)
        {
            double density = xs.Count < MaxScanSteps ? scale * Density(x) : 0;
            double next = Math.Min(x + Math.Min(1 / density, longest), to);
            x = next > x ? next : to;
            xs.Add(x);
        }

        return [.. xs];
    }

    // The arguments of the sines and cosines in f, as well as of tan, cot, sec and csc.
    internal static IEnumerable<Expr> Oscillating(Expr f, string variable)
    {
        if (f is Sin or Cos or Tan or Cot or Sec or Csc && ((UnaryExpr)f).Argument.DependsOn(variable))
            yield return ((UnaryExpr)f).Argument;

        foreach (Expr child in f.Children)
            foreach (Expr argument in Oscillating(child, variable))
                yield return argument;
    }

    private static Expr? Slope(Expr argument, string variable)
    {
        try
        {
            return argument.Differentiate(variable);
        }
        catch (Exception e) when (e is NotSupportedException or NotImplementedException)
        {
            return null;
        }
    }

    // Scans the grid xs and reports three kinds of roots, each located to full double precision:
    //   1. an exact zero on a grid point;
    //   2. a strict sign change between neighbours (odd multiplicity) - bisection on f;
    //   3. a touching root (even multiplicity, e.g. x^2 or (x-1)^2): |f| has a local minimum
    //      with no sign change around it - bisection on f', then accepted only if f ~ 0 there.
    // Zero is never a sign of its own, which would turn a grid hit on a double root into a
    // fake "sign change" and a second, slightly-off root next to it.
    private static List<double> Scan(Expr f, string variable, double[] xs)
    {
        Func<double, double> F = Function(f, variable);

        int steps = xs.Length - 1;
        double[] fs = [.. xs.Select(F)];

        // Only needed for touching roots; built lazily since most scans never get there.
        Func<double, double>? derivative = null;
        bool derivativeTried = false;

        var roots = new List<double>();
        for (int i = 0; i <= steps; i++)
        {
            // The domain ends between these two grid points (sqrt(x) around 0): a root can
            // sit exactly on the edge, where no sign change is visible.
            if (i < steps && double.IsFinite(fs[i]) != double.IsFinite(fs[i + 1]))
            {
                bool leftDefined = double.IsFinite(fs[i]);
                double definedValue = leftDefined ? fs[i] : fs[i + 1];
                if (DomainEdgeRoot(f, variable, F,
                        definedX: leftDefined ? xs[i] : xs[i + 1],
                        undefinedX: leftDefined ? xs[i + 1] : xs[i],
                        scale: Math.Max(1, Math.Abs(definedValue))) is double edgeRoot)
                    Add(roots, edgeRoot);
            }

            if (!double.IsFinite(fs[i]))
                continue;

            if (fs[i] == 0)
            {
                Add(roots, xs[i]);
                continue;
            }

            if (i < steps && double.IsFinite(fs[i + 1]) && fs[i + 1] != 0 && Math.Sign(fs[i]) != Math.Sign(fs[i + 1]))
            {
                // A pole changes sign too (1/x at 0): near a root |f| shrinks, near a pole it grows.
                if (Bisected(F, xs[i], xs[i + 1], fs[i]) is double r && Math.Abs(F(r)) <= Math.Min(Math.Abs(fs[i]), Math.Abs(fs[i + 1])))
                    Add(roots, r);
            }

            if (IsTouchingCandidate(fs, i, steps))
            {
                if (!derivativeTried)
                {
                    derivative = Derivative(f, variable);
                    derivativeTried = true;
                }

                if (derivative is not null && TouchingRoot(F, derivative, xs[i - 1], xs[i + 1], fs[i - 1], fs[i + 1]) is double c)
                    Add(roots, c);
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

        double previous = fs[i - 1], current = fs[i], next = fs[i + 1];
        if (!double.IsFinite(previous) || !double.IsFinite(next))
            return false;

        int sign = Math.Sign(current);
        return Math.Sign(previous) == sign && Math.Sign(next) == sign
            && Math.Abs(current) < Math.Abs(previous) && Math.Abs(current) <= Math.Abs(next);
    }

    private static Func<double, double>? Derivative(Expr f, string variable)
    {
        try
        {
            Expr slope = f.Differentiate(variable);
            return Function(slope, variable);
        }
        // floor and sign have no symbolic derivative, so touching roots can't be located.
        catch (Exception e) when (e is NotSupportedException or NotImplementedException)
        {
            return null;
        }
    }

    // At a touching root f' changes sign; find that point and accept it only if f is
    // (numerically) zero there, relative to the size of f on the surrounding grid points.
    private static double? TouchingRoot(Func<double, double> f, Func<double, double> derivative, double a, double b, double fa, double fb)
    {
        double da = derivative(a), db = derivative(b);
        if (!double.IsFinite(da) || !double.IsFinite(db) || da == 0 || db == 0 || Math.Sign(da) == Math.Sign(db))
            return null;
        if (Bisected(derivative, a, b, da) is not double c)
            return null;

        double scale = Math.Max(1, Math.Max(Math.Abs(fa), Math.Abs(fb)));
        return Math.Abs(f(c)) <= TouchingRootTolerance * scale ? c : null;
    }

    // Bisects between a and b (g(a) = ga, g(b) of the opposite sign) down to two adjacent
    // doubles. Works on the ordered bit patterns, so it takes at most 64 steps and reaches exact
    // zeros such as x = 0 (halving values stalled around 1e-63). Null if g is undefined somewhere
    // on the way: the sign change then goes through a hole or a pole (x^2/x or 1/x at 0).
    private static double? Bisected(Func<double, double> g, double a, double b, double ga)
    {
        long lo = ToOrderedBits(a), hi = ToOrderedBits(b);
        double gb = g(b);

        while (Int128.Abs((Int128)hi - lo) > 1)
        {
            long midBits = (long)(((Int128)lo + hi) / 2);
            double mid = FromOrderedBits(midBits), gm = g(mid);

            if (gm == 0)
                return Math.Abs(mid) < UnderflowPlateauLimit ? ResolveZeroPlateau(g, mid) : mid;
            if (!double.IsFinite(gm))
                return null;

            if (Math.Sign(gm) == Math.Sign(ga))
                (lo, ga) = (midBits, gm);
            else
                (hi, gb) = (midBits, gm);
        }

        return Math.Abs(ga) <= Math.Abs(gb) ? FromOrderedBits(lo) : FromOrderedBits(hi);
    }

    // A root on the edge of the domain is accepted if f is (nearly) 0 at the last defined
    // point and carries on through the edge over the complex numbers: sqrt(x) at 0,
    // sqrt(1 - x^2) at 1, sqrt(2 - x^2) at sqrt(2) (an edge that is not a double, so f is
    // only ~1e-8 there). The complex check rejects functions that merely tend to 0 towards a
    // point where they are undefined - x*ln(x) and x^2/x at 0 give 0*(-inf) and 0/0 there
    // even over C. Even an exact 0 needs the check: x^2/x is exactly 0 at x = -5e-324.
    private static double? DomainEdgeRoot(Expr f, string variable, Func<double, double> F, double definedX, double undefinedX, double scale)
    {
        var (edge, beyond) = BisectDomainEdge(F, definedX, undefinedX);

        double tolerance = EdgeRootTolerance * scale;
        if (!(Math.Abs(F(edge)) <= tolerance))
            return null;

        double continued = AtComplex(f, variable, beyond).Magnitude;
        return continued <= tolerance ? edge : null; // false for NaN
    }

    // Bisects between a point where f is finite and one where it isn't, down to two adjacent
    // doubles, so that it can land exactly on an edge like 0 or 1.
    private static (double Defined, double Undefined) BisectDomainEdge(Func<double, double> f, double defined, double undefined)
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

    // Next to x = 0, powers underflow: x^3 and x^2/x are exactly 0 on a whole plateau of tiny x,
    // so a zero found there says nothing about where the root is. x = 0 itself decides: a root
    // at 0 (x^3), or a hole (x^2/x, undefined at 0) - then no root at all.
    private static double? ResolveZeroPlateau(Func<double, double> g, double zeroAt)
    {
        double atZero = g(0);
        if (!double.IsFinite(atZero))
            return null;
        return atZero == 0 ? 0 : zeroAt;
    }

    // Maps doubles to longs monotonically (-0 and +0 both to 0), so neighbouring doubles are
    // neighbouring longs.
    private static long ToOrderedBits(double x)
    {
        long bits = BitConverter.DoubleToInt64Bits(x);
        return bits >= 0 ? bits : long.MinValue - bits;
    }

    private static double FromOrderedBits(long ordered) =>
        BitConverter.Int64BitsToDouble(ordered >= 0 ? ordered : long.MinValue - ordered);

    // One dictionary per function, with the variable overwritten on every call.
    private static Func<double, double> Function(Expr f, string variable)
    {
        var bindings = new Dictionary<string, double>();
        return x =>
        {
            bindings[variable] = x;
            try
            {
                return f.Evaluate(bindings);
            }
            catch
            {
                return double.NaN;
            }
        };
    }

    private static double At(Expr f, string variable, double x)
    {
        try
        {
            return f.Evaluate(new Dictionary<string, double> { [variable] = x });
        }
        catch
        {
            return double.NaN;
        }
    }

    private static ComplexNumber AtComplex(Expr f, string variable, double x)
    {
        try
        {
            return f.EvaluateComplex(new Dictionary<string, ComplexNumber> { [variable] = x });
        }
        catch
        {
            return new ComplexNumber(double.NaN, double.NaN);
        }
    }

    // The scan finds the roots in order but for a touching root, which lies before the root of
    // the sign change after it at most: the last few are the only ones it can repeat.
    private static void Add(List<double> roots, double candidate)
    {
        for (int i = roots.Count - 1; i >= Math.Max(0, roots.Count - 4); i--)
            if (Math.Abs(roots[i] - candidate) < MergeTolerance)
                return;
        roots.Add(candidate);
    }
}
