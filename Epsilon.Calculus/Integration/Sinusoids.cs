using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    // (p + q cos u + r sin u)/(a + b cos u + c sin u) for a linear u and numbers, rational or not:
    // the numerator is alpha D + beta D' + gamma for D the denominator, and the integral
    // alpha u + beta ln|D| and gamma times that of 1/D.
    private Expr? OverSinusoid(Expr numerator, Expr denominator)
    {
        if (Sinusoid(denominator, null) is not var (u, a, b, c) || Sinusoid(numerator, u) is not var (_, p, q, r) || Slope(u) is not { } k)
            return null;
        if (IsZero(a) && (IsZero(b) || IsZero(c)))
            return null;

        Expr square = Exact(b * b + c * c);
        if (IsZero(square))
            return null;

        Expr alpha = Exact((q * b + r * c) / square), beta = Exact((q * c - r * b) / square), gamma = Exact(p - alpha * a);
        Expr result = 0;
        if (!IsZero(alpha))
            result += alpha * u;
        if (!IsZero(beta))
            result += beta * new Ln(new Abs(denominator));
        if (!IsZero(gamma))
        {
            if (Reciprocal(u, a, b, c, square, denominator) is not var (factor, divisor, body))
                return null;
            result += Divided(Exact(gamma * factor), divisor, body);
        }

        return (result / k).Simplify();

        // Over sqrt(3) rather than times sqrt(3)/3.
        static Expr Divided(Expr factor, Expr divisor, Expr body) =>
            QuadraticSurd.Of(factor) is { B.IsZero: true } && QuadraticSurd.Of(divisor) is { B.IsZero: false }
                ? factor * body / divisor
                : Exact(factor / divisor) * body;
    }

    // The integral of 1/D for D = a + b cos u + c sin u by u, as a factor over a divisor times the
    // rest, with s = sqrt|a^2 - b^2 - c^2|: for a^2 > b^2 + c^2 and a > 0,
    // (u + 2 atan((c cos u - b sin u)/(a + s + b cos u + c sin u)))/s, whose denominator stays
    // positive, where (2/s) atan(((a - b) tan(u/2) + c)/s) jumps; for a^2 < b^2 + c^2,
    // ln|(b^2 + c^2 + (a b - c s) cos u + (a c + b s) sin u)/D|/s; and (b sin u - c cos u)/(a D)
    // between.
    private static (Expr Factor, Expr Divisor, Expr Body)? Reciprocal(Expr u, Expr a, Expr b, Expr c, Expr square, Expr d)
    {
        Expr discriminant = Exact(a * a - square);
        if (IsZero(discriminant))
        {
            var (scale, sides) = Normalized([b, -c], signed: true);
            return (scale, a, (sides[0] * new Sin(u) + sides[1] * new Cos(u)) / d);
        }

        double value = discriminant.Evaluate(), size = (a * a + square).Evaluate();
        if (!double.IsFinite(value) || Math.Abs(value) <= 1e-12 * size)
            return null;

        if (value < 0)
        {
            Expr s = Exact(new Sqrt(Exact(-discriminant)));
            var (_, terms) = Normalized([Exact(a * b - c * s), Exact(a * c + b * s), square]);
            return (1, s, new Ln(new Abs((terms[2] + terms[0] * new Cos(u) + terms[1] * new Sin(u)) / d)));
        }

        int sign = a.Evaluate() < 0 ? -1 : 1;
        var (root, sum) = Root(Exact(sign * a), square);
        var (_, wave) = Normalized([Exact(sign * b), Exact(sign * c), sum]);
        Expr angle = wave[1] * new Cos(u) - wave[0] * new Sin(u), positive = wave[2] + wave[0] * new Cos(u) + wave[1] * new Sin(u);
        return (sign, root, u + 2 * new Atan(angle / positive));
    }

    // sqrt(a^2 - square) and a + sqrt(a^2 - square), with cosh(t)^2 - 1 as sinh(t)^2 and
    // cosh(t) + sinh(t) as exp(t): the parts of tan(x + i) have cosh(2) next to cos(2x).
    private static (Expr Root, Expr Sum) Root(Expr a, Expr square)
    {
        if (a is Cosh { Argument: var t } && square is Constant { Value.IsOne: true })
        {
            Expr positive = t.Evaluate() < 0 ? Exact(-t) : t;
            return (new Sinh(positive), new Exp(positive));
        }

        Expr root = Exact(new Sqrt(Exact(a * a - square)));
        return (root, Exact(a + root));
    }

    // Coefficients as a scale times integers or surds with no common factor, the first nonzero of
    // the first two 1 or, unless signed, -1: (25, -20, 15) as 20 (-5/4, 1, -3/4) * (-4) = 5 (5, -4, 3).
    // The scale is 1 where one isn't a surd.
    private static (Expr Scale, Expr[] Scaled) Normalized(Expr[] coefficients, bool signed = false)
    {
        var surds = coefficients.Select(QuadraticSurd.Of).ToArray();
        if (surds.Any(surd => surd is null) || surds.Take(2).FirstOrDefault(surd => !surd!.Value.IsZero) is not { } lead)
            return (1, coefficients);

        QuadraticSurd magnitude = signed || lead.ToExpr().Evaluate() > 0 ? lead : -lead;
        var units = surds.Select(surd => surd!.Value / magnitude).ToArray();
        if (units.Any(unit => unit is null))
            return (1, coefficients);

        var parts = units.SelectMany(unit => new[] { unit!.Value.A, unit.Value.B }).Where(part => !part.IsZero).ToArray();
        System.Numerics.BigInteger common = parts.Aggregate(System.Numerics.BigInteger.One,
            (lcm, part) => lcm / System.Numerics.BigInteger.GreatestCommonDivisor(lcm, part.Denominator) * part.Denominator);
        System.Numerics.BigInteger divisor = parts.Aggregate(System.Numerics.BigInteger.Zero,
            (gcd, part) => System.Numerics.BigInteger.GreatestCommonDivisor(gcd, (part * new Rational(common)).Numerator));
        var factor = new QuadraticSurd(new Rational(common, divisor), 0, 1);
        return ((magnitude / factor)!.Value.ToExpr(), [.. units.Select(unit => (unit!.Value * factor)!.Value.ToExpr())]);
    }

    // e as a + b cos u + c sin u with numbers a, b and c, for the u given or the one in e.
    private (Expr U, Expr A, Expr B, Expr C)? Sinusoid(Expr e, Expr? u)
    {
        u ??= Waves(e).FirstOrDefault();
        if (u is null)
            return null;

        var s = new Variable(FreshName(e));
        var c = new Variable(FreshName(e + s));
        Expr linear = Replace(Replace(e, new Sin(u), s), new Cos(u), c);
        if (linear.DependsOn(variable) || !IsLinear(linear))
            return null;

        Expr a = Exact(linear.Substitute(s.Name, 0).Substitute(c.Name, 0));
        Expr b = Exact(linear.Substitute(s.Name, 0).Substitute(c.Name, 1) - a), sine = Exact(linear.Substitute(s.Name, 1).Substitute(c.Name, 0) - a);
        return new[] { a, b, sine }.All(n => n.GetVariables().Count == 0 && double.IsFinite(n.Evaluate())) ? (u, a, b, sine) : null;

        bool IsLinear(Expr x) => !x.DependsOn(s.Name) && !x.DependsOn(c.Name) || x switch
        {
            Variable => true,
            Add or Subtract or Negate => x.Children.All(IsLinear),
            Multiply product => !product.Left.DependsOn(s.Name) && !product.Left.DependsOn(c.Name) && IsLinear(product.Right)
                || !product.Right.DependsOn(s.Name) && !product.Right.DependsOn(c.Name) && IsLinear(product.Left),
            Divide quotient => !quotient.Denominator.DependsOn(s.Name) && !quotient.Denominator.DependsOn(c.Name) && IsLinear(quotient.Numerator),
            _ => false,
        };
    }

    private IEnumerable<Expr> Waves(Expr e) =>
        e is Sin or Cos && e.DependsOn(variable) ? [((UnaryExpr)e).Argument] : e.Children.SelectMany(Waves);

    private static Expr Exact(Expr e) => QuadraticSurd.Of(e)?.ToExpr() ?? e.Simplify();
}
