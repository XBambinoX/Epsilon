using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    // A rational function of exp(k u), sinh(k u), cosh(k u) and their kin for a linear u, by
    // t = exp(u): sinh u = (t - 1/t)/2, cosh u = (t + 1/t)/2 and du = dt/t.
    private Expr? ByExponential(Expr f)
    {
        var arguments = new List<Expr>();
        ExponentialArguments(f, arguments);
        if (arguments.Count == 0 || arguments.Any(argument => Slope(argument) is null))
            return null;

        Expr u = arguments.MinBy(argument => Slope(argument) is { } slope && slope.GetVariables().Count == 0 ? Math.Abs(slope.Evaluate()) : double.MaxValue)!;
        Expr a = Slope(u)!;
        var t = new Variable(FreshName(f));
        if (Rewrite(f) is not { } r || r.DependsOn(variable) || !IsRational(r))
            return null;

        return Back((r / (a * t)).Simplify(), t, new Exp(u)) is { } integral ? Shorter(integral) : null;

        Expr? Rewrite(Expr e)
        {
            if (Exponent(e) is { } w)
            {
                if ((Slope(w)! / a).Simplify() is not Constant { Value: { IsInteger: true } k } || k.Abs() > MaxMultipleAngle
                    || (w - k * u).Simplify() is not Constant { Value.IsZero: true })
                    return null;

                Expr power = new Power(t, k), inverse = new Power(t, -k);
                return e switch
                {
                    Sinh => (power - inverse) / 2,
                    Cosh => (power + inverse) / 2,
                    Tanh => (power - inverse) / (power + inverse),
                    Coth => (power + inverse) / (power - inverse),
                    Sech => 2 / (power + inverse),
                    Csch => 2 / (power - inverse),
                    _ => power,
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

        bool IsRational(Expr e) => !e.DependsOn(t.Name) || e switch
        {
            Variable => true,
            Add or Subtract or Negate or Multiply or Divide => e.Children.All(IsRational),
            Power { Exponent: Constant { Value.IsInteger: true } } power => IsRational(power.Base),
            _ => false,
        };
    }

    private void ExponentialArguments(Expr e, List<Expr> arguments)
    {
        if (Exponent(e) is { } w)
            arguments.Add(w);
        else
            foreach (Expr child in e.Children)
                ExponentialArguments(child, arguments);
    }

    // The argument of exp, e^w and the hyperbolic functions, when it has the variable.
    private Expr? Exponent(Expr e) => e switch
    {
        Exp or Sinh or Cosh or Tanh or Coth or Sech or Csch when e.DependsOn(variable) => ((UnaryExpr)e).Argument,
        Power { Base: EulerNumber } power when power.Exponent.DependsOn(variable) => power.Exponent,
        _ => null,
    };
}
