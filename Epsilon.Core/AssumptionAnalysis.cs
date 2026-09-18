using System.Numerics;

namespace Epsilon.Core;

/// <summary>
/// Bridges Assumptions (which are keyed by variable name) to arbitrary Expr subtrees,
/// so simplification rules can ask "is this whole expression provably nonzero/positive?"
/// rather than only "is this one variable nonzero?".
///
/// Conservative by design: if it can't prove something, it returns false rather than
/// guessing - a missed simplification is always safer than an incorrect one.
/// </summary>
public static class AssumptionAnalysis
{
    private static bool IsOddInteger(Rational value) => value.IsInteger && value.Numerator % 2 != 0;
    private static bool IsEvenInteger(Rational value) => value.IsInteger && value.Numerator % 2 == 0;

    public static bool IsProvablyNonZero(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => !c.Value.IsZero,
        Variable v => assumptions.IsNonZero(v.Name),
        Pi => true,
        E => true,
        Exp(_) => true,
        Negate(var a) => a.IsProvablyNonZero(assumptions),
        Abs(var a) => a.IsProvablyNonZero(assumptions),
        Multiply(var l, var r) => l.IsProvablyNonZero(assumptions) && r.IsProvablyNonZero(assumptions),
        Divide(var n, var d) => n.IsProvablyNonZero(assumptions) && d.IsProvablyNonZero(assumptions),
        Power(var b, _) => b.IsProvablyNonZero(assumptions),
        Sqrt(var a) => a.IsProvablyNonZero(assumptions),
        NthRoot(var a, _) => a.IsProvablyNonZero(assumptions),
        _ => expr.IsProvablyPositive(assumptions) || expr.IsProvablyNegative(assumptions)
    };

    public static bool IsProvablyPositive(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => c.Value.Sign > 0,
        Variable v => assumptions.IsPositive(v.Name),
        Pi => true,
        E => true,
        Exp(_) => true,
        Negate(var a) => a.IsProvablyNegative(assumptions),
        Abs(var a) => a.IsProvablyNonZero(assumptions),
        Add(var l, var r) =>
            (l.IsProvablyPositive(assumptions) && r.IsProvablyNonNegative(assumptions)) ||
            (l.IsProvablyNonNegative(assumptions) && r.IsProvablyPositive(assumptions)),
        Subtract(var l, var r) =>
            (l.IsProvablyPositive(assumptions) && r.IsProvablyNonPositive(assumptions)) ||
            (l.IsProvablyNonNegative(assumptions) && r.IsProvablyNegative(assumptions)),
        Multiply(var l, var r) =>
            (l.IsProvablyPositive(assumptions) && r.IsProvablyPositive(assumptions)) ||
            (l.IsProvablyNegative(assumptions) && r.IsProvablyNegative(assumptions)),
        Divide(var n, var d) =>
            (n.IsProvablyPositive(assumptions) && d.IsProvablyPositive(assumptions)) ||
            (n.IsProvablyNegative(assumptions) && d.IsProvablyNegative(assumptions)),
        Power(var b, Constant e) when IsEvenInteger(e.Value) => b.IsProvablyNonZero(assumptions),
        Power(var b, Constant e) when IsOddInteger(e.Value) => b.IsProvablyPositive(assumptions),
        Power(var b, _) => b.IsProvablyPositive(assumptions),
        Sqrt(var a) => a.IsProvablyPositive(assumptions),
        NthRoot(var a, Constant n) when IsOddInteger(n.Value) => a.IsProvablyPositive(assumptions),
        _ => false
    };

    public static bool IsProvablyNonNegative(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => c.Value.Sign >= 0,
        Variable v => assumptions.IsNonNegative(v.Name),
        Pi => true,
        E => true,
        Exp(_) => true,
        Negate(var a) => a.IsProvablyNonPositive(assumptions),
        Abs(_) => true,
        Add(var l, var r) => l.IsProvablyNonNegative(assumptions) && r.IsProvablyNonNegative(assumptions),
        Subtract(var l, var r) =>
            l.IsProvablyNonNegative(assumptions) && r.IsProvablyNonPositive(assumptions),
        Multiply(var l, var r) =>
            (l.IsProvablyNonNegative(assumptions) && r.IsProvablyNonNegative(assumptions)) ||
            (l.IsProvablyNonPositive(assumptions) && r.IsProvablyNonPositive(assumptions)),
        Divide(var n, var d) =>
            (n.IsProvablyNonNegative(assumptions) && d.IsProvablyPositive(assumptions)) ||
            (n.IsProvablyNonPositive(assumptions) && d.IsProvablyNegative(assumptions)),
        Power(_, Constant e) when IsEvenInteger(e.Value) => true,
        Power(var b, Constant e) when IsOddInteger(e.Value) => b.IsProvablyNonNegative(assumptions),
        Power(var b, _) => b.IsProvablyNonNegative(assumptions),
        Sqrt(_) => true,
        NthRoot(_, Constant n) when IsEvenInteger(n.Value) => true,
        NthRoot(var a, _) => a.IsProvablyNonNegative(assumptions),
        _ => expr.IsProvablyPositive(assumptions)
    };

    public static bool IsProvablyNonPositive(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => c.Value.Sign <= 0,
        Variable v => assumptions.IsNonPositive(v.Name),
        Negate(var a) => a.IsProvablyNonNegative(assumptions),
        Add(var l, var r) => l.IsProvablyNonPositive(assumptions) && r.IsProvablyNonPositive(assumptions),
        Subtract(var l, var r) =>
            l.IsProvablyNonPositive(assumptions) && r.IsProvablyNonNegative(assumptions),
        Multiply(var l, var r) =>
            (l.IsProvablyNonNegative(assumptions) && r.IsProvablyNonPositive(assumptions)) ||
            (l.IsProvablyNonPositive(assumptions) && r.IsProvablyNonNegative(assumptions)),
        Divide(var n, var d) =>
            (n.IsProvablyNonNegative(assumptions) && d.IsProvablyNegative(assumptions)) ||
            (n.IsProvablyNonPositive(assumptions) && d.IsProvablyPositive(assumptions)),
        // Odd integer exponent (any sign): sign of the result follows the base.
        Power(var b, Constant e) when IsOddInteger(e.Value) => b.IsProvablyNonPositive(assumptions),
        NthRoot(var a, Constant n) when IsOddInteger(n.Value) => a.IsProvablyNonPositive(assumptions),
        _ => expr.IsProvablyNegative(assumptions)
    };

    public static bool IsProvablyNegative(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => c.Value.Sign < 0,
        Variable v => assumptions.IsNegative(v.Name),
        Negate(var a) => a.IsProvablyPositive(assumptions),
        Add(var l, var r) =>
            (l.IsProvablyNegative(assumptions) && r.IsProvablyNonPositive(assumptions)) ||
            (l.IsProvablyNonPositive(assumptions) && r.IsProvablyNegative(assumptions)),
        Subtract(var l, var r) =>
            (l.IsProvablyNegative(assumptions) && r.IsProvablyNonNegative(assumptions)) ||
            (l.IsProvablyNonPositive(assumptions) && r.IsProvablyPositive(assumptions)),
        Multiply(var l, var r) =>
            (l.IsProvablyPositive(assumptions) && r.IsProvablyNegative(assumptions)) ||
            (l.IsProvablyNegative(assumptions) && r.IsProvablyPositive(assumptions)),
        Divide(var n, var d) =>
            (n.IsProvablyPositive(assumptions) && d.IsProvablyNegative(assumptions)) ||
            (n.IsProvablyNegative(assumptions) && d.IsProvablyPositive(assumptions)),

        Power(var b, Constant e) when IsOddInteger(e.Value) => b.IsProvablyNegative(assumptions),
        NthRoot(var a, Constant n) when IsOddInteger(n.Value) => a.IsProvablyNegative(assumptions),
        _ => false
    };
}