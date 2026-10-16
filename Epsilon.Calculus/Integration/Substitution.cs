using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    private const int MaxSubstitutions = 2;
    private Expr? BySubstitution(Expr f)
    {
        if (depth >= MaxSubstitutions)
            return null;
        if (PowersOfSinAndCos(f) is { } powers)
            return powers;

        var u = new Variable(FreshName(f));
        var parts = Parts(f).Distinct().ToList();
        foreach (Expr g in parts)
            if (DerivativeDivides(f, g, u) is { } integral)
                return integral;
        foreach (Expr g in parts)
            if (Inverse(f, g, u) is { } integral)
                return integral;

        return null;
    }

    private IEnumerable<Expr> Parts(Expr e)
    {
        foreach (Expr child in e.Children.Where(child => child.DependsOn(variable)))
        {
            if (child is not Variable)
                yield return child;
            if (child is Power { Exponent: Constant { Value: { IsInteger: true } k } } power && k.Numerator > 3)
                for (int d = 2; d < k.Numerator; d++)
                    if (k.Numerator % d == 0)
                        yield return new Power(power.Base, d);
            foreach (Expr part in Parts(child))
                yield return part;
        }
    }

    private Expr? DerivativeDivides(Expr f, Expr g, Variable u)
    {
        if (Slope(g) is not null)
            return null;

        foreach (Expr slope in Slopes(g))
        {
            Expr h = Cancel(Replace(f, g, u), Replace(slope, g, u));
            if (h.DependsOn(variable) && Monomial(g) is var (p, k, q))
                h = ReplacePowers(h, k, (u - q) / p).Simplify();
            if (!h.DependsOn(variable))
                return Back(h, u, g);
        }

        return null;
    }

    private (Expr P, Rational K, Expr Q)? Monomial(Expr g)
    {
        (Expr P, Rational K)? term;
        return g switch
        {
            Add { Left: var q, Right: var t } when !q.DependsOn(variable) && (term = Term(t)) is { } m => (m.P, m.K, q),
            Add { Left: var t, Right: var q } when !q.DependsOn(variable) && (term = Term(t)) is { } m => (m.P, m.K, q),
            Subtract { Left: var t, Right: var q } when !q.DependsOn(variable) && (term = Term(t)) is { } m => (m.P, m.K, -q),
            Subtract { Left: var q, Right: var t } when !q.DependsOn(variable) && (term = Term(t)) is { } m => (-m.P, m.K, q),
            _ when (term = Term(g)) is { } m => (m.P, m.K, 0),
            _ => null,
        };
    }

    private (Expr P, Rational K)? Term(Expr e) => e switch
    {
        Power { Base: Variable v, Exponent: Constant { Value: { IsInteger: true, Sign: > 0 } k } } when v.Name == variable => (1, k),
        Negate { Argument: var inner } when Term(inner) is { } m => (-m.P, m.K),
        Multiply { Left: var p, Right: var inner } when !p.DependsOn(variable) && Term(inner) is { } m => (p * m.P, m.K),
        _ => null,
    };

    private Expr ReplacePowers(Expr e, Rational k, Expr power) => e switch
    {
        Power { Base: Variable v, Exponent: Constant { Value: { IsInteger: true } m } } when v.Name == variable && (m / k).IsInteger =>
            new Power(power, m / k),
        _ when e.Children.Length > 0 => e.WithChildren([.. e.Children.Select(child => ReplacePowers(child, k, power))]),
        _ => e,
    };

    // The derivative, and for tan and its kin also in the form sec^2 that integrands use, where
    // the core gives 1/cos^2.
    private IEnumerable<Expr> Slopes(Expr g)
    {
        if (Derivative(g) is not { } slope)
            yield break;

        yield return slope;
        if (g is not UnaryExpr { Argument: var v } || Derivative(v) is not { } inner)
            yield break;

        Expr? other = g switch
        {
            Tan => new Power(new Sec(v), 2),
            Cot => -new Power(new Csc(v), 2),
            Tanh => new Power(new Sech(v), 2),
            Coth => -new Power(new Csch(v), 2),
            _ => null,
        };
        if (other is not null)
            yield return (other * inner).Simplify();
    }

    private static Expr Cancel(Expr numerator, Expr denominator)
    {
        var powers = new Dictionary<Expr, Rational>();
        Collect(numerator, 1, powers);
        Collect(denominator, -1, powers);

        Expr product = 1;
        foreach (var (b, exponent) in powers)
            if (!exponent.IsZero)
                product *= new Power(b, exponent);

        return product.Simplify();
    }

    private static void Collect(Expr e, Rational exponent, Dictionary<Expr, Rational> powers)
    {
        switch (e)
        {
            case Multiply product:
                Collect(product.Left, exponent, powers);
                Collect(product.Right, exponent, powers);
                return;
            case Divide quotient:
                Collect(quotient.Numerator, exponent, powers);
                Collect(quotient.Denominator, -exponent, powers);
                return;
            case Negate negate:
                Collect(-1, exponent, powers);
                Collect(negate.Argument, exponent, powers);
                return;
            case Sqrt root:
                CollectPower(root.Argument, new Rational(1, 2) * exponent, powers);
                return;
            case Power { Exponent: Constant k } power:
                CollectPower(power.Base, k.Value * exponent, powers);
                return;
            default:
                powers[e] = powers.GetValueOrDefault(e) + exponent;
                return;
        }
    }

    private static void CollectPower(Expr b, Rational exponent, Dictionary<Expr, Rational> powers)
    {
        if (exponent.IsInteger || b is not (Multiply or Divide or Negate))
            Collect(b, exponent, powers);
        else
            powers[b] = powers.GetValueOrDefault(b) + exponent;
    }

    // For g = nthroot(a x + b, n), x = (u^n - b)/a.
    private Expr? Inverse(Expr f, Expr g, Variable u)
    {
        (Expr Radicand, Expr N)? root = g switch
        {
            Sqrt sqrt => (sqrt.Argument, 2),
            NthRoot { Degree: Constant { Value: { IsInteger: true, Sign: > 0 } } degree } nthroot => (nthroot.Argument, degree),
            Power { Exponent: Constant { Value: { Numerator.IsOne: true, Denominator: var degree } } } power when degree > 1 =>
                (power.Base, new Rational(degree)),
            _ => null,
        };
        if (root is not var (radicand, n) || Slope(radicand) is not { } a)
            return null;

        Expr b = radicand.Substitute(variable, 0).Simplify();
        Expr x = (new Power(u, n) - b) / a;
        Expr dx = n * new Power(u, n - 1) / a;
        return Back((Replace(f, g, u).Substitute(variable, x) * dx).Simplify(), u, g);
    }

    private Expr? Back(Expr h, Variable u, Expr g) =>
        new IntegrationTable(u.Name, depth + 1, parts).Integrate(h) is { } integral ? integral.Substitute(u.Name, g) : null;

    private Expr? PowersOfSinAndCos(Expr f)
    {
        var factors = new List<(UnaryExpr Function, int Power)>();
        if (!Factors(f, factors))
            return null;

        Expr v = factors[0].Function.Argument;
        bool hyperbolic = factors[0].Function is Sinh or Cosh;
        if (Slope(v) is not { } a || factors.Any(factor => !factor.Function.Argument.Equals(v) || factor.Function is Sinh or Cosh != hyperbolic))
            return null;

        int m = factors.Where(factor => factor.Function is Sin or Sinh).Sum(factor => factor.Power);
        int n = factors.Where(factor => factor.Function is Cos or Cosh).Sum(factor => factor.Power);
        var w = new Variable(FreshName(f));
        Expr wSquared = new Power(w, 2);

        // dw = -sin v dv for w = cos v, but sinh v dv for w = cosh v.
        if (m % 2 == 1)
        {
            Expr h = hyperbolic
                ? new Power(wSquared - 1, (m - 1) / 2) * new Power(w, n) / a
                : -new Power(1 - wSquared, (m - 1) / 2) * new Power(w, n) / a;
            return Back(h.Simplify(), w, hyperbolic ? new Cosh(v) : new Cos(v))?.Expand();
        }

        if (n % 2 == 1)
        {
            Expr h = new Power(w, m) * new Power(hyperbolic ? 1 + wSquared : 1 - wSquared, (n - 1) / 2) / a;
            return Back(h.Simplify(), w, hyperbolic ? new Sinh(v) : new Sin(v))?.Expand();
        }

        Expr twice = hyperbolic ? new Cosh(2 * v) : new Cos(2 * v);
        Expr sinSquared = hyperbolic ? (twice - 1) / 2 : (1 - twice) / 2;
        return Integrate((new Power(sinSquared, m / 2) * new Power((1 + twice) / 2, n / 2)).Expand())?.Expand();
    }

    private bool Factors(Expr e, List<(UnaryExpr Function, int Power)> factors)
    {
        switch (e)
        {
            case Multiply product:
                return Factors(product.Left, factors) && Factors(product.Right, factors);
            case Sin or Cos or Sinh or Cosh:
                factors.Add(((UnaryExpr)e, 1));
                return true;
            case Power { Base: Sin or Cos or Sinh or Cosh, Exponent: Constant { Value: { IsInteger: true, Sign: > 0 } } k } power:
                factors.Add(((UnaryExpr)power.Base, (int)k.Value.Numerator));
                return true;
            default:
                return false;
        }
    }

    private string FreshName(Expr f)
    {
        string name = "u";
        while (name == variable || f.DependsOn(name))
            name += "'";
        return name;
    }

    // e with the part replaced by u, and its powers by powers of u: x^4 is u^2 for u = x^2, and
    // exp(2x) is u^2 for u = exp(x).
    private static Expr Replace(Expr e, Expr part, Variable u)
    {
        if (e.Equals(part))
            return u;
        if (Multiple(e, part) is { } n)
            return new Power(u, n);

        var children = e.Children;
        Expr[]? changed = null;
        for (int i = 0; i < children.Length; i++)
        {
            Expr child = Replace(children[i], part, u);
            if (!ReferenceEquals(child, children[i]))
                (changed ??= [.. children])[i] = child;
        }

        return changed is null ? e : e.WithChildren(changed);
    }

    // n when e is part^n for an integer n: the same base, with exponents in that ratio.
    private static Expr? Multiple(Expr e, Expr part)
    {
        static (Expr Base, Expr Exponent)? Split(Expr x) => x switch
        {
            Power power => (power.Base, power.Exponent),
            Exp exp => (new EulerNumber(), exp.Argument),
            _ => null,
        };

        if (Split(e) is not { } whole || Split(part) is not { } unit || !whole.Base.Equals(unit.Base))
            return null;

        return (whole.Exponent / unit.Exponent).Simplify() is Constant { Value.IsInteger: true } ratio ? ratio : null;
    }
}
