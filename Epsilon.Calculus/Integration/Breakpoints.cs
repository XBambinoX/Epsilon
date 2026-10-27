using Epsilon.Core;

namespace Epsilon.Calculus;

// An end of a piece of the interval: its value, and the exact point to shift the integrand to.
internal readonly record struct IntegrationPoint(double Value, Expr Exact);

// The points strictly between a and b where an integrand may be singular, undefined or not
// smooth: the zeros of denominators, of the bases of fractional and negative powers and of the
// arguments of roots, logarithms, abs and sign; the poles of tan, cot, sec and csc; the edges of
// the domains of the inverse functions; the kinks of min and max; the jumps of floor, ceiling
// and round of linear arguments; and the peaks and steps of exponentials, sech, cosh, tanh and
// atan, where the argument or its slope vanishes: on an infinite piece the nodes far out are too
// sparse to see exp(-(x - 200)^2) or sech(10(x - 1000))^2, and all their values there are 0.
// Exact where the part that vanishes is linear, numeric otherwise.
internal static class Breakpoints
{
    private const int MaxJumps = 10_000;
    private const int MinScanSteps = 200;
    private const int MaxScanSteps = 100_000;

    public static List<IntegrationPoint> Find(Expr expr, string variable, double a, double b)
    {
        var points = new List<IntegrationPoint>();
        foreach (Expr part in VanishingParts(expr, variable, a, b).Distinct())
            points.AddRange(Zeros(part, variable, a, b));

        // A root found numerically is a double or two off the same one found exactly, which is kept.
        var merged = new List<IntegrationPoint>();
        foreach (IntegrationPoint point in points.OrderBy(point => point.Value))
        {
            if (merged.Count == 0 || point.Value - merged[^1].Value > 4 * (Math.BitIncrement(Math.Abs(point.Value)) - Math.Abs(point.Value)))
                merged.Add(point);
            else if (merged[^1].Exact is Constant)
                merged[^1] = point;
        }

        return merged;
    }

    private static IEnumerable<Expr> VanishingParts(Expr expr, string variable, double a, double b)
    {
        if (!expr.DependsOn(variable))
            yield break;

        switch (expr)
        {
            case Divide divide:
                yield return divide.Denominator;
                break;
            case Exp or Sech or Cosh or Tanh or Atan:
                foreach (Expr part in Centers(((UnaryExpr)expr).Argument, variable))
                    yield return part;
                break;
            case Power power when !power.Base.DependsOn(variable):
                foreach (Expr part in Centers(power.Exponent, variable))
                    yield return part;
                break;
            case Power power when power.Exponent is not Constant { Value: { IsInteger: true, Sign: >= 0 } }:
                yield return power.Base;
                break;
            case NthRoot root:
                yield return root.Argument;
                break;
            case Sqrt or Ln or Abs or Sign or Coth or Csch:
                yield return ((UnaryExpr)expr).Argument;
                break;
            case Tan or Sec:
                yield return new Cos(((UnaryExpr)expr).Argument);
                break;
            case Cot or Csc:
                yield return new Sin(((UnaryExpr)expr).Argument);
                break;
            case Asin or Acos or Atanh:
                yield return ((UnaryExpr)expr).Argument - 1;
                yield return ((UnaryExpr)expr).Argument + 1;
                break;
            case Acosh acosh:
                yield return acosh.Argument - 1;
                break;
            case Min min:
                yield return min.Left - min.Right;
                break;
            case Max max:
                yield return max.Left - max.Right;
                break;
            case Floor or Ceiling or Round:
                foreach (Expr jump in Jumps((UnaryExpr)expr, variable, a, b))
                    yield return jump;
                break;
        }

        foreach (Expr child in expr.Children)
            foreach (Expr part in VanishingParts(child, variable, a, b))
                yield return part;
    }

    // exp(u) peaks where the slope of u vanishes, and is 1 where u is: there 1/(1 + exp(u)) steps.
    // sech(u) peaks, and tanh(u) and atan(u) step, where u vanishes, or where |u| is least.
    private static IEnumerable<Expr> Centers(Expr u, string variable)
    {
        yield return u;
        if (Slope(u, variable) is { } slope)
            yield return slope;
    }

    // floor and ceiling jump where the argument is an integer, round where it is an integer plus 1/2.
    private static IEnumerable<Expr> Jumps(UnaryExpr step, string variable, double a, double b)
    {
        Expr argument = step.Argument;
        if (!double.IsFinite(a) || !double.IsFinite(b) || LinearRoot(argument, variable) is null)
            yield break;

        Expr offset = step is Round ? new Rational(1, 2) : 0;
        double atA = argument.Evaluate((variable, a)), atB = argument.Evaluate((variable, b));
        double low = Math.Ceiling(Math.Min(atA, atB) - offset.Evaluate());
        double high = Math.Floor(Math.Max(atA, atB) - offset.Evaluate());
        if (!(high - low < MaxJumps))
            yield break;

        for (double k = low; k <= high; k++)
            yield return argument - ((long)k + offset);
    }

    private static IEnumerable<IntegrationPoint> Zeros(Expr part, string variable, double a, double b)
    {
        if (!part.DependsOn(variable))
            return [];

        if (LinearRoot(part, variable) is { } root)
        {
            double value = root.Evaluate();
            return a < value && value < b ? [new IntegrationPoint(value, root)] : [];
        }

        if (part is Sin or Cos && WaveZeros((UnaryExpr)part, variable, a, b) is { } zeros)
            return zeros.SelectMany(zero => Zeros(zero, variable, a, b));

        var found = part.FindRealRoots(variable, null, a, b, ScanSteps(part, variable, a, b))
            .Where(value => a < value && value < b)
            .Select(value => new IntegrationPoint(value, new Constant(value)));
        return [.. PolynomialRoots(part, variable, a, b), .. found];
    }

    // Points that aren't doubles, found exactly, so that the integrand can be shifted to them:
    // sin(u) vanishes where u is a multiple of pi, and next to pi sin(x) is -sin(d) of the
    // distance; the double nearest pi is 1.2e-16 off, which limited the accuracy to 1e-8.
    private static IEnumerable<Expr>? WaveZeros(UnaryExpr wave, string variable, double a, double b)
    {
        Expr argument = wave.Argument;
        if (!double.IsFinite(a) || !double.IsFinite(b) || LinearRoot(argument, variable) is null)
            return null;

        Expr offset = wave is Cos ? new Pi() / 2 : 0;
        double atA = argument.Evaluate((variable, a)), atB = argument.Evaluate((variable, b));
        double low = Math.Ceiling((Math.Min(atA, atB) - offset.Evaluate()) / Math.PI) - 1;
        double high = Math.Floor((Math.Max(atA, atB) - offset.Evaluate()) / Math.PI) + 1;
        if (!(high - low < MaxJumps))
            return null;

        return Enumerable.Range(0, (int)(high - low) + 1).Select(i => argument - ((long)(low + i) * new Pi() + offset));
    }

    // The roots of the linear and quadratic factors of a polynomial: next to sqrt(2), x^2 - 2 is
    // 2 sqrt(2) d + d^2 of the distance.
    private static IEnumerable<IntegrationPoint> PolynomialRoots(Expr part, string variable, double a, double b)
    {
        if (RationalPolynomial.Of(part, variable) is not { Degree: >= 2 } polynomial)
            yield break;

        foreach (var (factor, _) in polynomial.Factor())
        {
            Rational discriminant = factor[1] * factor[1] - 4 * factor[0] * factor[2];
            Expr[] roots = factor.Degree switch
            {
                1 => [new Constant(-factor[0] / factor[1])],
                2 when discriminant.Sign > 0 =>
                [
                    ((-factor[1] - new Sqrt(discriminant)) / (2 * factor[2])).Simplify(),
                    ((-factor[1] + new Sqrt(discriminant)) / (2 * factor[2])).Simplify(),
                ],
                _ => [],
            };

            foreach (Expr root in roots)
                if (root.Evaluate() is var value && a < value && value < b)
                    yield return new IntegrationPoint(value, root);
        }
    }

    // The roots are looked for on a grid, which misses two of them in one of its steps: on
    // [0, 1000] the 200 steps FindRealRoots takes by default found 82 of the 318 zeros of sin(x).
    // Where the part oscillates through trigonometric functions of linear arguments, the grid
    // gets 16 steps per pi of the fastest of them.
    private static int ScanSteps(Expr part, string variable, double a, double b)
    {
        double fastest = Frequencies(part, variable).DefaultIfEmpty(0).Max();
        double steps = Math.Ceiling(16 * (b - a) * fastest / Math.PI);
        return double.IsFinite(steps) ? (int)Math.Clamp(steps, MinScanSteps, MaxScanSteps) : MinScanSteps;
    }

    private static IEnumerable<double> Frequencies(Expr expr, string variable)
    {
        if (expr is Sin or Cos or Tan or Cot or Sec or Csc
            && Slope(((UnaryExpr)expr).Argument, variable) is { } slope && !slope.DependsOn(variable))
            yield return Math.Abs(slope.Evaluate());

        foreach (Expr child in expr.Children)
            foreach (double frequency in Frequencies(child, variable))
                yield return frequency;
    }

    // -u(0) / u' when u' is a nonzero constant.
    private static Expr? LinearRoot(Expr u, string variable)
    {
        if (Slope(u, variable) is not { } slope || slope.DependsOn(variable)
            || slope.Evaluate() is var value && (value == 0 || !double.IsFinite(value)))
            return null;

        return (-u.Substitute(variable, 0) / slope).Simplify(SimplifyMode.Strict);
    }

    private static Expr? Slope(Expr u, string variable)
    {
        try
        {
            return u.Differentiate(variable);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
