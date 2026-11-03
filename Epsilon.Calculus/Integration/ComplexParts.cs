using Epsilon.Core;

namespace Epsilon.Calculus;

// An expression with the imaginary unit as u + i v, with u and v free of it and the variables
// taken as real: 1/(x + i) is x/(x^2 + 1) - i/(x^2 + 1), and exp(i x) is cos(x) + i sin(x). Each
// part is then integrated by the rules of real functions, which ln|x + i| for 1/(x + i) broke.
// Null where a function of a complex argument has no such parts here, as sqrt and atan.
internal static class ComplexParts
{
    private const int MaxPower = 16;

    public static (Expr Re, Expr Im)? Of(Expr e)
    {
        if (!HasImaginaryUnit(e))
            return (e, 0);

        switch (e)
        {
            case ImaginaryUnit:
                return (0, 1);
            case Add sum when Of(sum.Left) is var (a, b) && Of(sum.Right) is var (c, d):
                return (a + c, b + d);
            case Subtract difference when Of(difference.Left) is var (a, b) && Of(difference.Right) is var (c, d):
                return (a - c, b - d);
            case Negate negate when Of(negate.Argument) is var (a, b):
                return (-a, -b);
            case Multiply product when Of(product.Left) is var (a, b) && Of(product.Right) is var (c, d):
                return (a * c - b * d, a * d + b * c);
            case Divide quotient when Of(quotient.Numerator) is var (a, b) && Of(quotient.Denominator) is var (c, d):
                return Quotient((a, b), (c, d));
            case Power { Exponent: Constant { Value: { IsInteger: true } n } } power when n.Abs() <= MaxPower && Of(power.Base) is var (a, b):
                (Expr Re, Expr Im) raised = (1, 0);
                for (int k = 0; k < n.Abs(); k++)
                    raised = (raised.Re * a - raised.Im * b, raised.Re * b + raised.Im * a);
                return n.Sign < 0 ? Quotient((1, 0), raised) : raised;
            // b^(c + i d) = b^c (cos(d ln b) + i sin(d ln b)) for a real b, where b > 0.
            case Power power when !HasImaginaryUnit(power.Base) && Of(power.Exponent) is var (c, d):
                Expr ln = power.Base is EulerNumber ? 1 : new Ln(power.Base);
                return (new Power(power.Base, c) * new Cos(d * ln), new Power(power.Base, c) * new Sin(d * ln));
            case Exp exp when Of(exp.Argument) is var (a, b):
                return (new Exp(a) * new Cos(b), new Exp(a) * new Sin(b));
            case Sin sin when Of(sin.Argument) is var (a, b):
                return (new Sin(a) * new Cosh(b), new Cos(a) * new Sinh(b));
            case Cos cos when Of(cos.Argument) is var (a, b):
                return (new Cos(a) * new Cosh(b), -new Sin(a) * new Sinh(b));
            case Sinh sinh when Of(sinh.Argument) is var (a, b):
                return (new Sinh(a) * new Cos(b), new Cosh(a) * new Sin(b));
            case Cosh cosh when Of(cosh.Argument) is var (a, b):
                return (new Cosh(a) * new Cos(b), new Sinh(a) * new Sin(b));
            // The rest by their quotients over |cos z|^2 = (cos 2a + cosh 2b)/2 and its kin.
            case Tan tan when Of(tan.Argument) is var (a, b):
                return Over((new Sin(2 * a), new Sinh(2 * b)), new Cos(2 * a) + new Cosh(2 * b));
            case Cot cot when Of(cot.Argument) is var (a, b):
                return Over((new Sin(2 * a), -new Sinh(2 * b)), new Cosh(2 * b) - new Cos(2 * a));
            case Sec sec when Of(sec.Argument) is var (a, b):
                return Over((2 * new Cos(a) * new Cosh(b), 2 * new Sin(a) * new Sinh(b)), new Cos(2 * a) + new Cosh(2 * b));
            case Csc csc when Of(csc.Argument) is var (a, b):
                return Over((2 * new Sin(a) * new Cosh(b), -2 * new Cos(a) * new Sinh(b)), new Cosh(2 * b) - new Cos(2 * a));
            case Tanh tanh when Of(tanh.Argument) is var (a, b):
                return Over((new Sinh(2 * a), new Sin(2 * b)), new Cosh(2 * a) + new Cos(2 * b));
            case Coth coth when Of(coth.Argument) is var (a, b):
                return Over((new Sinh(2 * a), -new Sin(2 * b)), new Cosh(2 * a) - new Cos(2 * b));
            case Sech sech when Of(sech.Argument) is var (a, b):
                return Over((2 * new Cosh(a) * new Cos(b), -2 * new Sinh(a) * new Sin(b)), new Cosh(2 * a) + new Cos(2 * b));
            case Csch csch when Of(csch.Argument) is var (a, b):
                return Over((2 * new Sinh(a) * new Cos(b), -2 * new Cosh(a) * new Sin(b)), new Cosh(2 * a) - new Cos(2 * b));
            case Abs abs when Of(abs.Argument) is var (a, b):
                return (new Sqrt(new Power(a, 2) + new Power(b, 2)), 0);
            // ln(a + i b) = ln(a^2 + b^2)/2 + i (sign(b) pi/2 - atan(a/b)) for a constant b other
            // than 0: the angle goes from 0 to sign(b) pi as a goes down the real line.
            case Ln log when Of(log.Argument) is var (a, b) && b.GetVariables().Count == 0 && b.Evaluate() is var value && value != 0 && double.IsFinite(value):
                return (new Ln(new Power(a, 2) + new Power(b, 2)) / 2, Math.Sign(value) * new Pi() / 2 - new Atan(a / b));
            default:
                return null;
        }
    }

    // (a + i b)/(c + i d) = ((a c + b d) + i (b c - a d))/(c^2 + d^2).
    private static (Expr Re, Expr Im) Quotient((Expr Re, Expr Im) n, (Expr Re, Expr Im) d)
    {
        Expr norm = new Power(d.Re, 2) + new Power(d.Im, 2);
        return ((n.Re * d.Re + n.Im * d.Im) / norm, (n.Im * d.Re - n.Re * d.Im) / norm);
    }

    private static (Expr Re, Expr Im) Over((Expr Re, Expr Im) n, Expr d) => (n.Re / d, n.Im / d);

    // k (a x + c)^r where the line a x + c misses (-inf, 0], the cut of the principal power, as
    // x + i does: there (a x + c)^(r + 1)/((r + 1) a), or ln(a x + c)/a for r = -1, is analytic
    // all along it. Its real and imaginary parts would be nested roots.
    public static Expr? LinearPower(Expr e, string variable)
    {
        Expr? Inner(Expr inner) => LinearPower(inner, variable);
        switch (e)
        {
            case Negate negate:
                return Inner(negate.Argument) is { } negated ? -negated : null;
            case Multiply product when !product.Left.DependsOn(variable):
                return Inner(product.Right) is { } right ? product.Left * right : null;
            case Multiply product when !product.Right.DependsOn(variable):
                return Inner(product.Left) is { } left ? left * product.Right : null;
            case Divide quotient when !quotient.Denominator.DependsOn(variable):
                return Inner(quotient.Numerator) is { } numerator ? numerator / quotient.Denominator : null;
            case Divide quotient when !quotient.Numerator.DependsOn(variable) && Raised(quotient.Denominator, variable) is var (b, r):
                return Inner(new Power(b, -r)) is { } reciprocal ? quotient.Numerator * reciprocal : null;
        }

        if (Raised(e, variable) is not var (@base, exponent))
            return null;

        Expr a, c;
        try
        {
            a = @base.Differentiate(variable).Simplify();
            c = @base.Substitute(variable, 0).Simplify();
        }
        catch (NotSupportedException)
        {
            return null;
        }
        if (a.GetVariables().Count > 0 || c.GetVariables().Count > 0)
            return null;

        var none = new Dictionary<string, ComplexNumber>();
        ComplexNumber slope = a.EvaluateComplex(none), start = c.EvaluateComplex(none);
        bool misses = slope.Imaginary == 0
            ? slope.Real != 0 && start.Imaginary != 0
            : slope.Real * (-start.Imaginary / slope.Imaginary) + start.Real > 0;
        if (!misses)
            return null;

        Expr raised = (exponent + 1).Simplify();
        return raised is Constant { Value.IsZero: true } ? new Ln(@base) / a : new Power(@base, raised) / (raised * a);
    }

    private static (Expr Base, Expr Exponent)? Raised(Expr e, string variable) => e switch
    {
        Sqrt sqrt => (sqrt.Argument, new Rational(1, 2)),
        Power power when !power.Exponent.DependsOn(variable) => (power.Base, power.Exponent),
        _ => null,
    };

    public static bool HasImaginaryUnit(Expr e) => e is ImaginaryUnit || e.Children.Any(HasImaginaryUnit);
}
