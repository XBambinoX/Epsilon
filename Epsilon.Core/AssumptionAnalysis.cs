namespace Epsilon.Core;

/// <summary>
/// Bridges Assumptions (which are keyed by variable name) to arbitrary Expr subtrees,
/// so simplification rules can ask "is this whole expression provably nonzero/positive?"
/// rather than only "is this one variable nonzero?".
///
/// Conservative by design: if it can't prove something, it returns false rather than
/// guessing - a missed simplification is always safer than an incorrect one. This is why
/// e.g. Subtract has no dedicated case anywhere below: a - b's sign depends on comparing
/// the two operands' magnitudes, which isn't something we can decide structurally without
/// numeric bounds, so it's deliberately left to fall through to "unknown".
/// </summary>
public static class AssumptionAnalysis
{
    /// <summary>
    /// True only if expr can be proven nonzero under assumptions. Structural rules
    /// (Multiply/Divide/Power/roots) reduce to "are the pieces nonzero", each of which
    /// recurses the same way - a missing case anywhere just means "can't prove it", never
    /// a wrong answer.
    /// </summary>
    public static bool IsProvablyNonZero(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => !c.Value.IsZero,
        Variable v => assumptions.IsNonZero(v.Name),
        Pi => true,
        E => true,
        Exp(_) => true, // exp(x) > 0 for every real x - never zero, whatever the argument's sign
        Negate(var a) => a.IsProvablyNonZero(assumptions),
        Abs(var a) => a.IsProvablyNonZero(assumptions),
        Multiply(var l, var r) => l.IsProvablyNonZero(assumptions) && r.IsProvablyNonZero(assumptions),
        Divide(var n, var d) => n.IsProvablyNonZero(assumptions) && d.IsProvablyNonZero(assumptions),
        Power(var b, _) => b.IsProvablyNonZero(assumptions), // nonzero base to any defined power stays nonzero
        Sqrt(var a) => a.IsProvablyNonZero(assumptions),
        NthRoot(var a, _) => a.IsProvablyNonZero(assumptions),
        // Falls back to the sign-specific rules below, so any case added there in the
        // future (e.g. Add of two provably-positive terms) automatically strengthens
        // this too, without needing to be duplicated here.
        _ => expr.IsProvablyPositive(assumptions) || expr.IsProvablyNegative(assumptions)
    };

    public static bool IsProvablyPositive(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => c.Value.Sign > 0,
        Variable v => assumptions.IsPositive(v.Name),
        Pi => true,
        E => true,
        Exp(_) => true,
        Abs(var a) => a.IsProvablyNonZero(assumptions), // |a| > 0 iff a != 0
        Add(var l, var r) =>
            (l.IsProvablyPositive(assumptions) && r.IsProvablyNonNegative(assumptions)) ||
            (l.IsProvablyNonNegative(assumptions) && r.IsProvablyPositive(assumptions)),
        Multiply(var l, var r) =>
            (l.IsProvablyPositive(assumptions) && r.IsProvablyPositive(assumptions)) ||
            (l.IsProvablyNegative(assumptions) && r.IsProvablyNegative(assumptions)),
        Divide(var n, var d) =>
            (n.IsProvablyPositive(assumptions) && d.IsProvablyPositive(assumptions)) ||
            (n.IsProvablyNegative(assumptions) && d.IsProvablyNegative(assumptions)),
        // Even integer power: positive whenever the base isn't zero, regardless of its sign.
        Power(var b, Constant e) when e.Value.IsInteger && e.Value.Sign > 0 && e.Value.Numerator % 2 == 0 =>
            b.IsProvablyNonZero(assumptions),
        Power(var b, _) => b.IsProvablyPositive(assumptions),
        Sqrt(var a) => a.IsProvablyPositive(assumptions), // sqrt(a) = 0 iff a = 0, so strictly positive iff a is
        NthRoot(var a, Constant n) when n.Value.IsInteger && n.Value.Numerator % 2 != 0 =>
            a.IsProvablyPositive(assumptions),
        _ => false
    };

    public static bool IsProvablyNonNegative(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => c.Value.Sign >= 0,
        Variable v => assumptions.IsNonNegative(v.Name),
        Pi => true,
        E => true,
        Exp(_) => true,
        Abs(_) => true, // |anything| >= 0 always, regardless of assumptions
        Add(var l, var r) => l.IsProvablyNonNegative(assumptions) && r.IsProvablyNonNegative(assumptions),
        Multiply(var l, var r) =>
            (l.IsProvablyNonNegative(assumptions) && r.IsProvablyNonNegative(assumptions)) ||
            (l.IsProvablyNonPositive(assumptions) && r.IsProvablyNonPositive(assumptions)),
        Divide(var n, var d) =>
            (n.IsProvablyNonNegative(assumptions) && d.IsProvablyPositive(assumptions)) ||
            (n.IsProvablyNonPositive(assumptions) && d.IsProvablyNegative(assumptions)),
        Power(_, Constant e) when e.Value.IsInteger && e.Value.Sign > 0 && e.Value.Numerator % 2 == 0 => true, // even integer power
        Power(var b, _) => b.IsProvablyNonNegative(assumptions),
        Sqrt(_) => true, // the principal square root is always >= 0 by convention, whenever it's real
        NthRoot(_, Constant n) when n.Value.IsInteger && n.Value.Numerator % 2 == 0 => true, // even root, principal branch
        NthRoot(var a, _) => a.IsProvablyNonNegative(assumptions), // odd root (or unknown parity): sign follows a
        _ => expr.IsProvablyPositive(assumptions)
    };

    public static bool IsProvablyNonPositive(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => c.Value.Sign <= 0,
        Variable v => assumptions.IsNonPositive(v.Name),
        Negate(var a) => a.IsProvablyNonNegative(assumptions),
        Add(var l, var r) => l.IsProvablyNonPositive(assumptions) && r.IsProvablyNonPositive(assumptions),
        Multiply(var l, var r) =>
            (l.IsProvablyNonNegative(assumptions) && r.IsProvablyNonPositive(assumptions)) ||
            (l.IsProvablyNonPositive(assumptions) && r.IsProvablyNonNegative(assumptions)),
        Divide(var n, var d) =>
            (n.IsProvablyNonNegative(assumptions) && d.IsProvablyNegative(assumptions)) ||
            (n.IsProvablyNonPositive(assumptions) && d.IsProvablyPositive(assumptions)),
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
        Multiply(var l, var r) =>
            (l.IsProvablyPositive(assumptions) && r.IsProvablyNegative(assumptions)) ||
            (l.IsProvablyNegative(assumptions) && r.IsProvablyPositive(assumptions)),
        Divide(var n, var d) =>
            (n.IsProvablyPositive(assumptions) && d.IsProvablyNegative(assumptions)) ||
            (n.IsProvablyNegative(assumptions) && d.IsProvablyPositive(assumptions)),
        NthRoot(var a, Constant n) when n.Value.IsInteger && n.Value.Numerator % 2 != 0 =>
            a.IsProvablyNegative(assumptions), // odd root: sign follows the argument
        _ => false
    };
}