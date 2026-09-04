using Epsilon.Core;

namespace Epsilon.Calculus;

internal static class ByParts
{
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

    internal static Expr TryByParts(this Expr expr, int depth)
    {
        if (expr is not Multiply(var left, var right))
            throw new NotSupportedException(
                $"Cannot integrate '{expr.Print()}' — no table/substitution rule and it's not a product for by-parts.");

        (Expr u, Expr dv) = LiatePriority(left) <= LiatePriority(right)
            ? (left, right)
            : (right, left);

        Expr du = u.Differentiate();
        Expr v = dv.AntiDerivative(depth);

        Expr remainder = new Multiply(v, du).Simplify();
        Expr integratedRemainder = remainder.AntiDerivative(depth);

        return new Subtract(new Multiply(u, v), integratedRemainder);
    }
}