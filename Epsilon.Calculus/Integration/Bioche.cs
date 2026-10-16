using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    private const int MaxMultipleAngle = 12;

    // A rational function R(sin u, cos u) of a linear u by Bioche's rules: t = cos u when R dx is
    // unchanged by u -> -u (R odd in sin), t = sin u by u -> pi - u (odd in cos), t = tan u by
    // u -> pi + u (even in both together). Each gives a rational function of t; of those that
    // apply, the shortest result is taken. Where none does, t = tan(u/2).
    private Expr? ByBioche(Expr f)
    {
        var s = new Variable(FreshName(f));
        var c = new Variable(FreshName(f + s));
        var t = new Variable(FreshName(f + s + c));
        if (SinCosForm(f, s, c) is not var (r, u) || Slope(u) is not { } a)
            return null;

        Expr q = 1 - new Power(t, 2), square = 1 + new Power(t, 2);
        var rules = new List<(Expr H, Expr G)>();
        if (Transform(r, s, (1, true), c, (t, false), q) is (var odd, true))
            rules.Add((-odd, new Cos(u)));
        if (Transform(r, s, (t, false), c, (1, true), q) is (var oddInCos, true))
            rules.Add((oddInCos, new Sin(u)));
        if (Transform(r, s, (t, true), c, (1, true), 1 / square) is (var even, false))
            rules.Add((even / square, new Tan(u)));

        // sin u = 2t/(1 + t^2), cos u = (1 - t^2)/(1 + t^2) and du = 2 dt/(1 + t^2).
        if (rules.Count == 0)
            rules.Add((r.Substitute(s.Name, 2 * t / square).Substitute(c.Name, q / square) * 2 / square, new Tan(u / 2)));

        return rules
            .Select(rule => Back((rule.H / a).Simplify(), t, rule.G))
            .OfType<Expr>()
            .Select(integral => Shorter(WithoutAtanOfTan(integral.Simplify())))
            .MinBy(integral => integral.Print().Length);
    }

    // atan(tan(u)) is u less a constant on each interval where tan u is defined.
    private static Expr WithoutAtanOfTan(Expr e) => e switch
    {
        Atan { Argument: Tan tan } => tan.Argument,
        _ when e.Children.Length > 0 => e.WithChildren([.. e.Children.Select(WithoutAtanOfTan)]),
        _ => e,
    };

    // f as R(s, c) for s = sin u, c = cos u and a linear u, tan, cot, sec and csc written with
    // them, and those of k u by the multiple-angle formulas.
    private (Expr R, Expr U)? SinCosForm(Expr f, Variable s, Variable c)
    {
        var arguments = new List<Expr>();
        Arguments(f, arguments);
        if (arguments.Count == 0 || arguments.Any(argument => Slope(argument) is null))
            return null;

        Expr u = arguments.MinBy(argument => Slope(argument) is { } slope && slope.GetVariables().Count == 0 ? Math.Abs(slope.Evaluate()) : double.MaxValue)!;
        Expr a = Slope(u)!;
        return Rewrite(f) is { } r && !r.DependsOn(variable) && IsRational(r) ? (r, u) : null;

        bool IsRational(Expr e) => !e.DependsOn(s.Name) && !e.DependsOn(c.Name) || e switch
        {
            Variable => true,
            Add or Subtract or Negate or Multiply or Divide => e.Children.All(IsRational),
            Power { Exponent: Constant { Value.IsInteger: true } } power => IsRational(power.Base),
            _ => false,
        };

        Expr? Rewrite(Expr e)
        {
            if (e is Sin or Cos or Tan or Cot or Sec or Csc && e.DependsOn(variable))
            {
                Expr w = ((UnaryExpr)e).Argument;
                if ((Slope(w)! / a).Simplify() is not Constant { Value: { IsInteger: true } k } || BigAngle(k) || (w - k * u).Simplify() is not Constant { Value.IsZero: true })
                    return null;

                var (sin, cos) = MultipleAngle((int)k.Numerator, s, c);
                return e switch
                {
                    Sin => sin,
                    Cos => cos,
                    Tan => sin / cos,
                    Cot => cos / sin,
                    Sec => 1 / cos,
                    _ => 1 / sin,
                };
            }

            var children = new Expr[e.Children.Length];
            for (int i = 0; i < children.Length; i++)
                if (Rewrite(e.Children[i]) is { } child)
                    children[i] = child;
                else
                    return null;

            return children.Length == 0 ? e : e.WithChildren(children);
        }

        static bool BigAngle(Rational k) => k.Abs() > MaxMultipleAngle;
    }

    private void Arguments(Expr e, List<Expr> arguments)
    {
        if (e is Sin or Cos or Tan or Cot or Sec or Csc && e.DependsOn(variable))
            arguments.Add(((UnaryExpr)e).Argument);
        else
            foreach (Expr child in e.Children)
                Arguments(child, arguments);
    }

    // sin(k u) and cos(k u) as polynomials in s = sin u and c = cos u, or sinh(k u) and cosh(k u)
    // in s = sinh u and c = cosh u.
    private static (Expr Sin, Expr Cos) MultipleAngle(int k, Expr s, Expr c, bool hyperbolic = false)
    {
        Expr sin = 0, cos = 1;
        for (int i = 0; i < Math.Abs(k); i++)
            (sin, cos) = ((sin * c + cos * s).Expand(), (hyperbolic ? cos * c + sin * s : cos * c - sin * s).Expand());

        return (k < 0 ? (-sin).Simplify() : sin, cos);
    }

    // e as E sigma for an odd e and as E for an even one, under the change of sign of the symbols
    // given as odd, where sigma^2 = q: products and quotients of two odd parts give q, and a
    // power of an odd part q to half its exponent. Null when a part is neither odd nor even.
    private static (Expr E, bool Odd)? Transform(Expr e, Variable s, (Expr E, bool Odd) sin, Variable c, (Expr E, bool Odd) cos, Expr q)
    {
        if (e is Variable v && v.Name == s.Name)
            return sin;
        if (e is Variable w && w.Name == c.Name)
            return cos;
        if (!e.DependsOn(s.Name) && !e.DependsOn(c.Name))
            return (e, false);

        (Expr E, bool Odd)? Of(Expr child) => Transform(child, s, sin, c, cos, q);

        switch (e)
        {
            case Negate negate when Of(negate.Argument) is var (n, odd):
                return (-n, odd);
            case Add or Subtract when Of(e.Children[0]) is var (l, leftOdd) && Of(e.Children[1]) is var (r, rightOdd) && leftOdd == rightOdd:
                return (e is Add ? l + r : l - r, leftOdd);
            case Multiply product when Of(product.Left) is var (l, leftOdd) && Of(product.Right) is var (r, rightOdd):
                return (leftOdd && rightOdd ? l * r * q : l * r, leftOdd != rightOdd);
            case Divide quotient when Of(quotient.Numerator) is var (n, numeratorOdd) && Of(quotient.Denominator) is var (d, denominatorOdd):
                return (!numeratorOdd && denominatorOdd ? n / (d * q) : n / d, numeratorOdd != denominatorOdd);
            case Power { Exponent: Constant { Value: { IsInteger: true } k } } power when k.Abs() <= 64 && Of(power.Base) is var (b, odd):
                int m = (int)k.Numerator;
                if (!odd)
                    return (new Power(b, m), false);
                int half = m % 2 == 0 ? m / 2 : (m - 1) / 2;
                return (new Power(b, m) * new Power(q, half), m % 2 != 0);
            default:
                return null;
        }
    }
}
