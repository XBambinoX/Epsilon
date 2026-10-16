using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    private const int MaxParts = 8;

    private IntegrationTable Deeper => new(variable, depth, parts + 1);

    private Expr? ByParts(Expr f)
    {
        if (parts >= MaxParts || Choose(f) is not { } choice || Step(choice.U, choice.Dv) is not { } step)
            return null;

        Expr uv = choice.U * step.V;
        if (Ratio(step.Remainder, f) is { } c)
            return Is(c, -1) ? null : Shorter(uv / (1 + c));

        if (choice.Cyclic && Choose(step.Remainder) is { } again && Step(again.U, again.Dv) is { } second
            && Ratio(second.Remainder, f) is { } c2 && !Is(c2, 1))
            return Shorter((uv - again.U * second.V) / (1 - c2));

        return Deeper.Integrate(step.Remainder) is { } rest ? Shorter(uv - rest) : null;
    }

    private static Expr Shorter(Expr e)
    {
        Expr simplified = e.Simplify(), expanded = e.Expand();
        return expanded.Print().Length <= simplified.Print().Length ? expanded : simplified;
    }

    // v, and v du whose integral is left.
    private (Expr V, Expr Remainder)? Step(Expr u, Expr dv) =>
        Deeper.Integrate(dv) is { } v && Derivative(u) is { } du ? (v, (v * du).Simplify()) : null;

    private Expr? Ratio(Expr g, Expr f) => Cancel(g, f) is var ratio && !ratio.DependsOn(variable) ? ratio : null;

    private (Expr U, Expr Dv, bool Cyclic)? Choose(Expr f)
    {
        var factors = new List<Expr>();
        Flatten(f, factors);

        Expr? u = factors.FirstOrDefault(factor => Raised(factor) is Ln { Argument: var a } && IsAlgebraic(a))
            ?? factors.FirstOrDefault(factor => Raised(factor) is (Asin or Acos or Atan or Asinh or Acosh or Atanh) and UnaryExpr { Argument: var a } && IsAlgebraic(a))
            ?? factors.FirstOrDefault(factor => factor.DependsOn(variable) && IsPolynomial(factor));
        bool cyclic = u is null;
        if (cyclic)
        {
            u = factors.FirstOrDefault(factor => factor is Sin or Cos or Sinh or Cosh);
            var others = factors.Where(factor => factor.DependsOn(variable) && !ReferenceEquals(factor, u)).ToList();
            bool fits = u is not null && others.Count switch
            {
                0 => HasLogarithm(((UnaryExpr)u).Argument),
                1 => others[0] is Exp || others[0] is Power power && !power.Base.DependsOn(variable),
                _ => false,
            };
            if (!fits)
                return null;
        }

        int index = factors.FindIndex(factor => ReferenceEquals(factor, u));
        factors.RemoveAt(index);
        if (factors.Count == 0 && IsPolynomial(u!))
            return null;

        Expr dv = factors.Count == 0 ? 1 : factors.Aggregate((product, factor) => product * factor);
        return (u!, dv, cyclic);
    }

    private void Flatten(Expr e, List<Expr> factors)
    {
        switch (e)
        {
            case Multiply product:
                Flatten(product.Left, factors);
                Flatten(product.Right, factors);
                break;
            case Divide quotient:
                Flatten(quotient.Numerator, factors);
                factors.Add(quotient.Denominator.DependsOn(variable) ? new Power(quotient.Denominator, -1) : 1 / quotient.Denominator);
                break;
            default:
                factors.Add(e);
                break;
        }
    }

    private static Expr Raised(Expr factor) =>
        factor is Power { Exponent: Constant { Value: { IsInteger: true, Sign: > 0 } } } power ? power.Base : factor;

    // A logarithm or inverse function of anything else, as atan(tan(x/2)/sqrt(3)) from 1/(2 + cos(x)),
    // has a derivative that integration by parts only makes longer.
    private bool IsAlgebraic(Expr e) => e switch
    {
        Sqrt or NthRoot or Abs => e.Children.All(IsAlgebraic),
        UnaryExpr => !e.DependsOn(variable),
        Power power => !power.Exponent.DependsOn(variable) && IsAlgebraic(power.Base),
        _ => e.Children.All(IsAlgebraic),
    };

    private static bool HasLogarithm(Expr e) => e is Ln || e.Children.Any(HasLogarithm);
}
