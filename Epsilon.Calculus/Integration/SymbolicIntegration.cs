using Epsilon.Core;

namespace Epsilon.Calculus;

public static class SymbolicIntegrator
{
    public static Expr AntiDerivative(this Expr expr) => expr.AntiDerivative(0);

    internal static Expr AntiDerivative(this Expr expr, int depth)
    {
        if (depth > 50)
            throw new InvalidOperationException(
                $"Integration recursion exceeded 50 levels for '{expr.Print()}' — likely a non-converging chain.");

        try { return TableRule(expr); }
        catch (NotSupportedException) { }

        try { return expr.TrySubstitution(depth + 1); }
        catch (NotSupportedException) { }

        return expr.TryByParts(depth + 1);
    }

    internal static Expr TableRule(Expr expr) => expr switch
    {
        Constant c => new Multiply(c, new Variable()),
        Variable => new Divide(new Power(new Variable(), new Constant(2)), new Constant(2)),
        Add(var l, var r) => new Add(l.AntiDerivative(0), r.AntiDerivative(0)),
        Subtract(var l, var r) => new Subtract(l.AntiDerivative(0), r.AntiDerivative(0)),
        Multiply(Constant c, var f) => new Multiply(c, f.AntiDerivative(0)),
        Multiply(var f, Constant c) => new Multiply(c, f.AntiDerivative(0)),
        Divide(var f, Constant c) => new Divide(f.AntiDerivative(0), c),

        Power(Variable, Constant n) when n.Value != -1 =>
            new Divide(new Power(new Variable(), new Constant(n.Value + 1)), new Constant(n.Value + 1)),
        Power(Variable, Constant n) when n.Value == -1 => new Ln(new Variable()),
        Divide(Constant one, Variable) when one.Value == 1 => new Ln(new Variable()),

        // Exponential / logarithmic

        Exp(Variable) => new Exp(new Variable()),

        // Fixed: hardcoded formula, no recursion through by-parts needed
        Ln(Variable) => new Subtract(
            new Multiply(new Variable(), new Ln(new Variable())),
            new Variable()
        ),

        Sqrt(Variable) => new Multiply(new Constant(2.0 / 3.0), new Power(new Variable(), new Constant(1.5))),

        NthRoot(Variable, Constant n) when n.Value != -1 =>
            new Divide(
                new Power(new Variable(), new Add(new Divide(new Constant(1), n), new Constant(1))),
                new Add(new Divide(new Constant(1), n), new Constant(1))
            ),

        // Trigonometric

        Sin(Variable) => new Subtract(new Constant(0), new Cos(new Variable())),
        Cos(Variable) => new Sin(new Variable()),
        Tan(Variable) => new Subtract(new Constant(0), new Ln(new Cos(new Variable()))),
        Cot(Variable) => new Ln(new Sin(new Variable())),
        Sec(Variable) => new Ln(new Add(new Sec(new Variable()), new Tan(new Variable()))),
        Csc(Variable) => new Subtract(new Constant(0), new Ln(new Add(new Csc(new Variable()), new Cot(new Variable())))),

        // sec^2(x), csc^2(x) — common enough to hardcode
        Power(Sec(Variable), Constant n) when n.Value == 2 => new Tan(new Variable()),
        Power(Csc(Variable), Constant n) when n.Value == 2 => new Subtract(new Constant(0), new Cot(new Variable())),

        // Inverse trigonometric

        Asin(Variable) => new Add(
            new Multiply(new Variable(), new Asin(new Variable())),
            new Multiply(new Constant(-1), new Sqrt(
                new Subtract(new Constant(1), new Power(new Variable(), new Constant(2)))))),

        Acos(Variable) => new Add(
            new Multiply(new Variable(), new Acos(new Variable())),
            new Sqrt(new Subtract(new Constant(1), new Power(new Variable(), new Constant(2))))),

        Atan(Variable) => new Subtract(
            new Multiply(new Variable(), new Atan(new Variable())),
            new Multiply(new Constant(0.5), new Ln(
                new Add(new Constant(1), new Power(new Variable(), new Constant(2)))))),

        // Hyperbolic

        Sinh(Variable) => new Cosh(new Variable()),
        Cosh(Variable) => new Sinh(new Variable()),
        Tanh(Variable) => new Ln(new Cosh(new Variable())),

        // Rational standard integrals

        Divide(Constant one, Sqrt(Subtract(Constant c, Power(Variable, Constant n))))
            when one.Value == 1 && c.Value == 1 && n.Value == 2 => new Asin(new Variable()),

        Divide(Constant one, Sqrt(Subtract(Power(Variable, Constant n), Constant c)))
            when one.Value == 1 && c.Value == 1 && n.Value == 2 =>
                new Ln(new Add(new Variable(), new Sqrt(new Subtract(new Power(new Variable(), new Constant(2)), new Constant(1))))),

        Divide(Constant one, Add(Power(Variable, Constant n), Constant c))
            when one.Value == 1 && n.Value == 2 && c.Value == 1 => new Atan(new Variable()),

        Divide(Constant one, Add(Constant c, Power(Variable, Constant n)))
            when one.Value == 1 && n.Value == 2 && c.Value == 1 => new Atan(new Variable()),

        _ => throw new NotSupportedException($"No table rule for '{expr.Print()}'.")
    };
}