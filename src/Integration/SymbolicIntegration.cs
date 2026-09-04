namespace Epsilon;

public static class SymbolicIntegrator
{
    public static Expr AntiDerivative(this Expr expr)
    {
        try
        {
            return TableRule(expr);
        }
        catch (NotSupportedException)
        {
            return TryByParts(expr);
        }
    }


    private static Expr AntiDerivative(this Expr expr, int depth)
    {
        if (depth > 50)
            throw new InvalidOperationException(
                $"Integration recursion exceeded 50 levels for '{expr.Print()}' — likely a non-converging by-parts chain.");

        try
        {
            return TableRule(expr);
        }
        catch (NotSupportedException)
        {
            return TryByParts(expr, depth + 1);
        }
    }

    private static Expr TableRule(Expr expr) => expr switch
    {
        Constant c => new Multiply(c, new Variable()),
        Variable => new Divide(new Power(new Variable(), new Constant(2)), new Constant(2)),
        Add(var l, var r) => new Add(l.AntiDerivative(), r.AntiDerivative()),
        Subtract(var l, var r) => new Subtract(l.AntiDerivative(), r.AntiDerivative()),
        Multiply(Constant c, var f) => new Multiply(c, f.AntiDerivative()),
        Multiply(var f, Constant c) => new Multiply(c, f.AntiDerivative()),
        Divide(var f, Constant c) => new Divide(f.AntiDerivative(), c),

        Power(Variable, Constant n) when n.Value != -1 =>
            new Divide(new Power(new Variable(), new Constant(n.Value + 1)), new Constant(n.Value + 1)),
        Power(Variable, Constant n) when n.Value == -1 => new Ln(new Variable()),
        Divide(Constant one, Variable) when one.Value == 1 => new Ln(new Variable()),

        // Exponential / logarithmic

        Exp(Variable) => new Exp(new Variable()),
        Ln(Variable) => TryByParts(expr),

        // Trigonometric

        Sin(Variable) => new Subtract(new Constant(0), new Cos(new Variable())),
        Cos(Variable) => new Sin(new Variable()),
        Tan(Variable) => new Subtract(new Constant(0), new Ln(new Cos(new Variable()))),
        Cot(Variable) => new Ln(new Sin(new Variable())),
        Sec(Variable) => new Ln(new Add(new Sec(new Variable()), new Tan(new Variable()))),
        Csc(Variable) => new Subtract(new Constant(0), new Ln(new Add(new Csc(new Variable()), new Cot(new Variable())))),

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

        Divide(Constant one, Sqrt(Subtract(Constant c, Power(Variable, Constant n))))
            when one.Value == 1 && c.Value == 1 && n.Value == 2 => new Asin(new Variable()),

        Divide(Constant one, Sqrt(Subtract(Power(Variable, Constant n), Constant c)))
            when one.Value == 1 && c.Value == 1 && n.Value == 2 =>
                new Ln(new Add(new Variable(), new Sqrt(new Subtract(new Power(new Variable(), new Constant(2)), new Constant(1))))),

        // Rational standard integrals

        Divide(Constant one, Add(Power(Variable, Constant n), Constant c))
            when one.Value == 1 && n.Value == 2 && c.Value == 1 => new Atan(new Variable()),

        Divide(Constant one, Add(Constant c, Power(Variable, Constant n)))
            when one.Value == 1 && n.Value == 2 && c.Value == 1 => new Atan(new Variable()),

        _ => throw new NotSupportedException(
            $"No table rule for '{expr.Print()}'.")
    };


    // LIATE priority: lower number = more likely to be chosen as 'u'
    private static int LiatePriority(Expr e) => e switch
    {
        Ln => 0,
        Asin or Acos or Atan => 1,
        Variable or Power(Variable, Constant) => 2,
        Sin or Cos or Tan => 3,
        Exp => 4,
        Multiply(Constant, var inner) => LiatePriority(inner),
        Multiply(var inner, Constant) => LiatePriority(inner),
        _ => 5
    };

    private static Expr TryByParts(Expr expr, int depth = 0)
    {
        // Special case: ln(x) alone -> u = ln(x), dv = dx, du = 1/x dx, v = x
        if (expr is Ln(Variable))
        {
            Expr u = expr;
            Expr v = new Variable();
            Expr du = expr.Differentiate();
            Expr integrand = new Multiply(v, du).Simplify();
            return new Subtract(new Multiply(u, v), integrand.AntiDerivative(depth + 1));
        }

        if (expr is not Multiply(var left, var right))
        {
            throw new NotSupportedException(
                $"Cannot integrate '{expr.Print()}' — no table rule and it's not a product for by-parts.");
        }

        // Pick u = whichever factor has lower LIATE priority (comes first in LIATE order)
        (Expr u2, Expr dv) = LiatePriority(left) <= LiatePriority(right)
            ? (left, right)
            : (right, left);

        Expr du2 = u2.Differentiate();
        Expr v2 = dv.AntiDerivative(depth + 1); // may itself throw NotSupportedException — that's fine, propagates up

        Expr remainder = new Multiply(v2, du2).Simplify();
        Expr integratedRemainder = remainder.AntiDerivative(depth + 1); // recursive — may need by-parts again

        return new Subtract(new Multiply(u2, v2), integratedRemainder);
    }
}