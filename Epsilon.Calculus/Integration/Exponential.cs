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

    // A product of exponentials b^u of linear u and constant bases, exp among them, and of
    // constant powers of them is itself over the sum of the rates u' ln(b): 2^x 3^x / ln(6), the
    // logarithms of rational bases with integer rates taken together.
    private Expr? ExponentialProduct(Expr f)
    {
        if (f is not (Multiply or Divide))
            return null;

        var factors = new List<Expr>();
        Flatten(f, factors);
        Expr rate = 0;
        Rational together = 1;
        int count = 0;
        foreach (Expr factor in factors.Where(factor => factor.DependsOn(variable)))
        {
            if (Rate(factor) is not var (b, a) || b.GetVariables().Count == 0 && !(b.Evaluate() > 0))
                return null;

            count++;
            if (b is Constant { Value: { Sign: > 0 } n } && a is Constant { Value.IsInteger: true } k)
                together *= n.Pow((int)k.Value.Numerator);
            else
                rate += b is EulerNumber ? a : a * new Ln(b);
        }

        if (count < 2)
            return null;

        rate = (together.IsOne ? rate : rate + new Ln(new Constant(together))).Simplify();
        return IsZero(rate) || rate.GetVariables().Count == 0 && rate.Evaluate() == 0 ? null : f / rate;
    }

    // b and the slope a of b^(a x + c) for a constant b, e for exp, and of a constant power of one.
    private (Expr Base, Expr Slope)? Rate(Expr e) => e switch
    {
        Exp exp when Slope(exp.Argument) is { } a => (new EulerNumber(), a),
        Power { Base: var b } power when !b.DependsOn(variable) && Slope(power.Exponent) is { } a => (b, a),
        Power { Exponent: var k } power when !k.DependsOn(variable) && Rate(power.Base) is var (b, a) => (b, (a * k).Simplify()),
        Sqrt root when Rate(root.Argument) is var (b, a) => (b, (a / 2).Simplify()),
        _ => null,
    };

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
