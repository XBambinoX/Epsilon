using Epsilon.Core;

namespace Epsilon.Calculus;

// An end of a piece of the interval: its value, and the exact point to shift the integrand to,
// which is only the decimal of the value where the point was found numerically.
internal readonly record struct IntegrationPoint(double Value, Expr Exact, bool Approximate = false);

// The points strictly between a and b where an integrand may be singular, undefined or not
// smooth: the zeros of denominators, of the bases of fractional and negative powers and of the
// arguments of roots, logarithms, abs and sign; the poles of tan, cot, sec and csc; the edges of
// the domains of the inverse functions; the kinks of min and max; the jumps of floor, ceiling
// and round of linear arguments; the peaks and steps of exponentials, sech, cosh, tanh and atan,
// where the argument or its slope vanishes; and the turns of polynomials among those parts: on
// an infinite piece the nodes far out are too sparse to see exp(-(x - 200)^2) or
// sech(10(x - 1000))^2, and all their values there are 0.
// Exact where the part that vanishes is linear, numeric otherwise.
internal static class Breakpoints
{
    private const int MaxJumps = 10_000;
    private const int MinScanSteps = 200;
    private const int MaxScanSteps = 100_000;

    public static List<IntegrationPoint> Find(Expr expr, string variable, double a, double b)
    {
        var points = new List<IntegrationPoint>();
        foreach (Expr part in VanishingParts(expr, variable, a, b, singularOnly: false).Distinct())
        {
            points.AddRange(Zeros(part, variable, a, b));
            if (Turn(part, variable) is { } slope)
                points.AddRange(Zeros(slope, variable, a, b));
        }

        return Merged(points);
    }

    // Only the points where the expression may jump or be undefined, without its kinks and peaks.
    public static List<IntegrationPoint> Singular(Expr expr, string variable, double a, double b) =>
        Merged([.. SingularParts(expr, variable, a, b).SelectMany(part => Factors(part, variable)).Distinct()
            .SelectMany(part => WaveRoots(part, variable, a, b) ?? Zeros(part, variable, a, b))]);

    // The parts whose zeros those are. Where a and b are NaN, the jumps of floor, ceiling and round
    // aren't counted, and their argument stands for them.
    public static IEnumerable<Expr> SingularParts(Expr expr, string variable, double a, double b) =>
        VanishingParts(expr, variable, a, b, singularOnly: true);

    // A root found numerically is a double or two off the same one found exactly, which is kept.
    private static List<IntegrationPoint> Merged(List<IntegrationPoint> points)
    {
        var merged = new List<IntegrationPoint>();
        foreach (IntegrationPoint point in points.OrderBy(point => point.Value))
        {
            if (merged.Count == 0 || point.Value - merged[^1].Value > 4 * (Math.BitIncrement(Math.Abs(point.Value)) - Math.Abs(point.Value)))
                merged.Add(point);
            else if (merged[^1].Approximate || merged[^1].Exact is Constant && !point.Approximate)
                merged[^1] = point;
        }

        return merged;
    }

    private static IEnumerable<Expr> VanishingParts(Expr expr, string variable, double a, double b, bool singularOnly)
    {
        if (!expr.DependsOn(variable))
            yield break;

        switch (expr)
        {
            case Divide divide:
                yield return divide.Denominator;
                break;
            case Exp or Sech or Cosh or Tanh or Atan when !singularOnly:
                foreach (Expr part in Centers(((UnaryExpr)expr).Argument, variable))
                    yield return part;
                break;
            case Power power when !power.Base.DependsOn(variable):
                if (!singularOnly)
                    foreach (Expr part in Centers(power.Exponent, variable))
                        yield return part;
                break;
            case Power power when power.Exponent is not Constant { Value: { IsInteger: true, Sign: >= 0 } }:
                yield return power.Base;
                break;
            case NthRoot root:
                yield return root.Argument;
                break;
            case Sqrt or Ln or Sign or Coth or Csch:
            case Abs when !singularOnly:
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
            case Min min when !singularOnly:
                yield return min.Left - min.Right;
                break;
            case Max max when !singularOnly:
                yield return max.Left - max.Right;
                break;
            case Floor or Ceiling or Round when double.IsNaN(a):
                yield return ((UnaryExpr)expr).Argument;
                break;
            case Floor or Ceiling or Round:
                foreach (Expr jump in Jumps((UnaryExpr)expr, variable, a, b))
                    yield return jump;
                break;
        }

        foreach (Expr child in expr.Children)
            foreach (Expr part in VanishingParts(child, variable, a, b, singularOnly))
                yield return part;
    }

    // exp(u) peaks where the slope of u vanishes, and is 1 where u is: there 1/(1 + exp(u)) steps.
    // sech(u) peaks, and tanh(u) and atan(u) step, where u vanishes, or where |u| is least.
    // The turns of a polynomial u are found with its zeros.
    private static IEnumerable<Expr> Centers(Expr u, string variable)
    {
        yield return u;
        if (RationalPolynomial.Of(u, variable) is null && Slope(u, variable) is { } slope)
            yield return slope;
    }

    // A polynomial that comes close to 0 without vanishing peaks the integrand where it turns:
    // 1/(1 + 1e10 (x - 1000)^2) is 1e-5 wide at 1000, which the nodes over [-1e300, 1e300]
    // passed by, and FindRealRoots took 1000 for a root only where its grid happened to bracket
    // it, on [-1e6, 1e6] but not on [0, 1e6].
    private static Expr? Turn(Expr part, string variable) =>
        RationalPolynomial.Of(part, variable) is { Degree: >= 2 } polynomial
            ? polynomial.Derivative().ToExpr(new Variable(variable))
            : null;

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

        var (low, high) = RootRange(part, variable, a, b);
        var found = low < high
            ? part.FindRealRoots(variable, null, low, high, ScanSteps(part, variable, low, high))
                .Where(value => a < value && value < b)
                .Select(value => new IntegrationPoint(value, new Constant(value), Approximate: true))
            : [];
        return [.. PolynomialRoots(part, variable, a, b), .. found];
    }

    // The roots of a polynomial are less than 1 + max |a_k / a_n| from 0 (Cauchy's bound): on
    // [-1e300, 1e300] the grid had steps of 1e298 and overflowed.
    private static (double Low, double High) RootRange(Expr part, string variable, double a, double b)
    {
        if (RationalPolynomial.Of(part, variable) is not { Degree: >= 1 } polynomial)
            return (a, b);

        double bound = 1 + Enumerable.Range(0, polynomial.Degree).Max(k => Math.Abs((polynomial[k] / polynomial.Leading).ToDouble()));
        return double.IsFinite(bound) ? (Math.Max(a, -bound), Math.Min(b, bound)) : (a, b);
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

        foreach (Expr root in RealRoots(polynomial))
            if (root.Evaluate() is var value && a < value && value < b)
                yield return new IntegrationPoint(value, root);
    }

    private static IEnumerable<Expr> RealRoots(RationalPolynomial polynomial)
    {
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
                yield return root;
        }
    }

    // abs(u) and u^2 touch 0 where u crosses it, which the grid finds more surely.
    private static IEnumerable<Expr> Factors(Expr part, string variable) => part switch
    {
        Abs abs => Factors(abs.Argument, variable),
        Negate negate => Factors(negate.Argument, variable),
        Multiply product => Factors(product.Left, variable).Concat(Factors(product.Right, variable)),
        Divide quotient => Factors(quotient.Numerator, variable),
        Power { Exponent: Constant { Value.Sign: > 0 } } power => Factors(power.Base, variable),
        _ => part.DependsOn(variable) ? [part] : [],
    };

    // The zeros of a polynomial in tan, sin or cos of a linear argument u, exact: the
    // antiderivatives by t = tan(x/2) have tan(x/2) + 1 in denominators, which vanishes at 3 pi/2.
    private static List<IntegrationPoint>? WaveRoots(Expr part, string variable, double a, double b)
    {
        if (!double.IsFinite(a) || !double.IsFinite(b) || Wave(part, variable) is not { } wave
            || Slope(wave.Argument, variable) is not { } slope || slope.DependsOn(variable) || slope.Evaluate() == 0)
            return null;

        var t = new Variable("t'");
        Expr replaced = Replaced(part, wave, t);
        if (replaced.DependsOn(variable) || RationalPolynomial.Of(replaced, t.Name) is not { Degree: >= 1 } polynomial)
            return null;

        Expr argument = wave.Argument, start = argument.Substitute(variable, 0).Simplify();
        double atA = argument.Evaluate((variable, a)), atB = argument.Evaluate((variable, b));
        Expr period = wave is Tan ? new Pi() : 2 * new Pi();
        var points = new List<IntegrationPoint>();
        foreach (Expr root in RealRoots(polynomial))
        {
            Expr[] angles = wave switch
            {
                Tan => [new Atan(root)],
                Sin when Math.Abs(root.Evaluate()) <= 1 => [new Asin(root), new Pi() - new Asin(root)],
                Cos when Math.Abs(root.Evaluate()) <= 1 => [new Acos(root), -new Acos(root)],
                _ => [],
            };

            foreach (Expr angle in angles)
            {
                double low = Math.Ceiling((Math.Min(atA, atB) - angle.Evaluate()) / period.Evaluate());
                double high = Math.Floor((Math.Max(atA, atB) - angle.Evaluate()) / period.Evaluate());
                if (!(high - low < MaxJumps))
                    return null;

                for (double k = low; k <= high; k++)
                {
                    Expr x = ((angle + (long)k * period - start) / slope).Simplify();
                    if (x.Evaluate() is var value && a < value && value < b)
                        points.Add(new IntegrationPoint(value, x));
                }
            }
        }

        return points;
    }

    private static UnaryExpr? Wave(Expr e, string variable) => e switch
    {
        Tan or Sin or Cos when e.DependsOn(variable) => (UnaryExpr)e,
        _ => e.Children.Select(child => Wave(child, variable)).FirstOrDefault(wave => wave is not null),
    };

    private static Expr Replaced(Expr e, Expr target, Expr by) =>
        e.Equals(target) ? by : e.Children.Length == 0 ? e : e.WithChildren([.. e.Children.Select(child => Replaced(child, target, by))]);

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
