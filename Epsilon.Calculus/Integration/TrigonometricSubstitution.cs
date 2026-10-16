using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    // A rational function of x and sqrt(q), q = p((x - h)^2 + k), with r = sqrt(|k|): by
    // x - h = r sin(a) for p, k < 0, r sinh(a) for p, k > 0, and s r cosh(a) for p > 0 > k, where
    // s is the sign of x - h, carried as a symbol whose square is 1. sqrt(q) is then sqrt(|p|) r
    // times cos(a), cosh(a) or sinh(a), and the integrand a rational function of those. For cosh,
    // a = s ln|(x - h + sqrt(q)/sqrt(p))/r| on both sides, so s a has no sign in it.
    private Expr? ByTrigonometricSubstitution(Expr f)
    {
        var w = new Variable(FreshName(f));
        if (RadicalForm(f, w) is not var (r, q) || QuadraticIntegrals.Of(q, variable) is not { Vertex: var (p, h, k, pSign, kSign) })
            return null;
        if (pSign < 0 && kSign >= 0 || kSign == 0)
            return null;

        var a = new Variable(FreshName(f + w));
        var sign = new Variable(FreshName(f + w + a));
        Expr radius = new Sqrt(kSign < 0 ? -k : k).Simplify(), scale = new Sqrt(pSign < 0 ? -p : p).Simplify();
        Expr shifted = _x - h;
        // dx = sqrt(q) / sqrt(|p|) da, with the sign s for cosh.
        var (substitute, root, perRoot, sin, cos, angle) = pSign < 0
            ? (h + radius * new Sin(a), scale * radius * new Cos(a), 1 / scale, shifted / radius, w / (scale * radius), new Asin(shifted / radius))
            : kSign > 0
                ? (h + radius * new Sinh(a), scale * radius * new Cosh(a), 1 / scale, shifted / radius, w / (scale * radius), (Expr)new Asinh(shifted / radius))
                : (h + sign * radius * new Cosh(a), scale * radius * new Sinh(a), sign / scale, w / (scale * radius), sign * shifted / radius,
                    sign * new Ln(new Abs((shifted + w / scale) / radius)));

        if (Branch(sign) is { } integral)
            return Joined(integral.Substitute(sign.Name, 1).Simplify(), integral.Substitute(sign.Name, -1).Simplify())
                ?? integral.Substitute(sign.Name, new Sign(shifted)).Simplify();

        // With h in the sum x = h + s r cosh(a), s stays in the integrand: each branch apart.
        if (pSign < 0 || kSign > 0 || Branch(1) is not { } right || Branch(-1) is not { } left)
            return null;

        return Joined(right, left) ?? Shorter((right + left) / 2 + new Sign(shifted) * (right - left) / 2);

        // The same on both sides of the gap, without the sign.
        static Expr? Joined(Expr right, Expr left) => right.Equals(left) ? right : null;

        Expr? Branch(Expr s)
        {
            Expr integrand = ((r * w).Simplify().Substitute(variable, substitute).Substitute(w.Name, root) * perRoot).Substitute(sign.Name, s);
            if (new IntegrationTable(a.Name, depth + 1, parts).Integrate(WithoutSquares(integrand.Simplify(), sign.Name).Simplify()) is not { } integral)
                return null;

            Expr back = FromAngle(integral.Simplify(), a, sin.Substitute(sign.Name, s), cos.Substitute(sign.Name, s), angle.Substitute(sign.Name, s), hyperbolic: pSign > 0);
            Expr result = WithoutSquares(Shorter(WithoutRootPowers(WithoutSquares(back.Simplify(), sign.Name), w, q)), sign.Name);
            return result.Substitute(w.Name, new Sqrt(q)).Simplify();
        }
    }

    // w^n for w = sqrt(q) and |n| > 1 as a power of q, times w for an odd n.
    private static Expr WithoutRootPowers(Expr e, Variable w, Expr q) => e switch
    {
        Power { Base: Variable v, Exponent: Constant { Value: { IsInteger: true } n } } when v.Name == w.Name && n.Abs() > 1 =>
            n.Numerator.IsEven ? new Power(q, n / 2) : n.Sign > 0 ? new Power(q, (n - 1) / 2) * w : new Power(q, (n + 1) / 2) / w,
        _ when e.Children.Length > 0 => e.WithChildren([.. e.Children.Select(child => WithoutRootPowers(child, w, q))]),
        _ => e,
    };

    // f as R(x, w) for w = sqrt(q), with q the one quadratic under every square root of f.
    private (Expr R, Expr Q)? RadicalForm(Expr f, Variable w)
    {
        Expr? q = null;
        return Rewrite(f) is { } r && q is not null && IsRational(r) ? (r, q) : null;

        Expr? Rewrite(Expr e)
        {
            (Expr Radicand, int Power)? root = e switch
            {
                Sqrt sqrt when sqrt.Argument.DependsOn(variable) => (sqrt.Argument, 1),
                Power { Exponent: Constant { Value: var half } } power when half.Denominator == 2 && power.Base.DependsOn(variable) =>
                    (power.Base, (int)half.Numerator),
                _ => null,
            };
            if (root is var (radicand, m))
            {
                q ??= radicand;
                return q.Equals(radicand) ? (m == 1 ? w : new Power(w, m)) : null;
            }

            var children = new Expr[e.Children.Length];
            for (int i = 0; i < children.Length; i++)
                if (Rewrite(e.Children[i]) is { } child)
                    children[i] = child;
                else
                    return null;

            return children.Length == 0 ? e : e.WithChildren(children);
        }

        bool IsRational(Expr e) => !e.DependsOn(variable) && !e.DependsOn(w.Name) || e switch
        {
            Variable => true,
            Add or Subtract or Negate or Multiply or Divide => e.Children.All(IsRational),
            Power { Exponent: Constant { Value.IsInteger: true } } power => IsRational(power.Base),
            _ => false,
        };
    }

    // The angle back to x: the functions of k a by the multiple-angle formulas, of a/2 by
    // tan(a/2) = sin(a)/(1 + cos(a)) and its kin, exp(k a) as (sinh(a) + cosh(a))^k, and a itself
    // as its inverse function of x.
    private static Expr FromAngle(Expr e, Variable a, Expr sin, Expr cos, Expr angle, bool hyperbolic)
    {
        if (e is Variable v && v.Name == a.Name)
            return angle;

        bool family = hyperbolic ? e is Sinh or Cosh or Tanh or Coth or Sech or Csch or Exp : e is Sin or Cos or Tan or Cot or Sec or Csc;
        if (family && e.DependsOn(a.Name) && (((UnaryExpr)e).Argument / a).Simplify() is Constant { Value: var k })
        {
            if (k == new Rational(1, 2) && e is Tan or Tanh)
                return sin / (1 + cos);
            if (k == new Rational(1, 2) && e is Cot or Coth)
                return (1 + cos) / sin;
            if (k.IsInteger && k.Abs() <= MaxMultipleAngle)
            {
                if (e is Exp)
                    return new Power(sin + cos, k);

                var (s, c) = MultipleAngle((int)k.Numerator, sin, cos, hyperbolic);
                return e switch
                {
                    Sin or Sinh => s,
                    Cos or Cosh => c,
                    Tan or Tanh => s / c,
                    Cot or Coth => c / s,
                    Sec or Sech => 1 / c,
                    _ => 1 / s,
                };
            }
        }

        return e.Children.Length == 0 ? e : e.WithChildren([.. e.Children.Select(child => FromAngle(child, a, sin, cos, angle, hyperbolic))]);
    }

    // A sign squared is 1, and its reciprocal itself.
    private static Expr WithoutSquares(Expr e, string sign) => e switch
    {
        Power { Base: Variable v, Exponent: Constant { Value: { IsInteger: true } n } } when v.Name == sign =>
            n.Numerator.IsEven ? 1 : v,
        Power { Base: Multiply product, Exponent: Constant { Value.IsInteger: true } } power when product.DependsOn(sign) =>
            WithoutSquares(new Power(product.Left, power.Exponent), sign) * WithoutSquares(new Power(product.Right, power.Exponent), sign),
        _ when e.Children.Length > 0 => e.WithChildren([.. e.Children.Select(child => WithoutSquares(child, sign))]),
        _ => e,
    };
}
