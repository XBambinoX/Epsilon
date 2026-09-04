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

    private static Expr TableRule(Expr expr) => expr switch
    {
        Constant c => new Multiply(c, new Variable()),

        Variable => new Divide(new Power(new Variable(), new Constant(2)), new Constant(2)),

        Add(var l, var r) => new Add(l.AntiDerivative(), r.AntiDerivative()),
        Subtract(var l, var r) => new Subtract(l.AntiDerivative(), r.AntiDerivative()),

        Multiply(Constant c, var f) => new Multiply(c, f.AntiDerivative()),
        Multiply(var f, Constant c) => new Multiply(c, f.AntiDerivative()),

        Power(Variable, Constant n) when n.Value != -1 =>
            new Divide(new Power(new Variable(), new Constant(n.Value + 1)), new Constant(n.Value + 1)),
        Power(Variable, Constant n) when n.Value == -1 => new Ln(new Variable()),
        Divide(Constant one, Variable) when one.Value == 1 => new Ln(new Variable()),

        Sin(Variable) => new Subtract(new Constant(0), new Cos(new Variable())),
        Cos(Variable) => new Sin(new Variable()),
        Exp(Variable) => new Exp(new Variable()),

        Ln(Variable) => TryByParts(expr), // ln(x) itself needs by-parts (u=ln x, dv=dx)

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
        _ => 5
    };

    private static Expr TryByParts(Expr expr)
    {
        // Special case: ln(x) alone -> u = ln(x), dv = dx, du = 1/x dx, v = x
        if (expr is Ln(Variable))
        {
            Expr u = expr;
            Expr v = new Variable();
            Expr du = expr.Differentiate();
            Expr integrand = new Multiply(v, du).Simplify();
            return new Subtract(new Multiply(u, v), integrand.AntiDerivative());
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
        Expr v2 = dv.AntiDerivative(); // may itself throw NotSupportedException — that's fine, propagates up

        Expr remainder = new Multiply(v2, du2).Simplify();
        Expr integratedRemainder = remainder.AntiDerivative(); // recursive — may need by-parts again

        return new Subtract(new Multiply(u2, v2), integratedRemainder);
    }
}