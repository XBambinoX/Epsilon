namespace Epsilon.Core;

/// <summary>
/// Bridges Assumptions (which are keyed by variable name) to arbitrary Expr subtrees,
/// so simplification rules can ask "is this whole expression provably nonzero/positive?"
/// rather than only "is this one variable nonzero?".
///
/// Conservative by design: if it can't prove something, it returns false rather than
/// guessing — a missed simplification is always safer than an incorrect one.
/// </summary>
public static class AssumptionAnalysis
{
    /// <summary>
    /// True only if expr can be proven nonzero under assumptions:
    /// - a nonzero Constant is always nonzero,
    /// - a Variable is nonzero only if assumptions say so explicitly,
    /// - Sums/products/etc. are not analyzed structurally yet — conservatively false
    ///   unless one of the simpler cases above applies. This can grow more precise later
    ///   (e.g. Add of two provably-positive terms is provably positive) without breaking
    ///   any caller, since "false" (i.e. "can't prove it") is always a safe answer here.
    /// </summary>
    public static bool IsProvablyNonZero(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => !c.Value.IsZero,
        Variable v => assumptions.IsNonZero(v.Name),
        Pi => true,
        E => true,
        Negate(var a) => a.IsProvablyNonZero(assumptions),
        Abs(var a) => a.IsProvablyNonZero(assumptions),
        _ => false
    };

    public static bool IsProvablyPositive(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => c.Value.Sign > 0,
        Variable v => assumptions.IsPositive(v.Name),
        Pi => true,
        E => true,
        Abs(var a) => a.IsProvablyNonZero(assumptions), // |a| > 0 iff a != 0
        _ => false
    };

    public static bool IsProvablyNonNegative(this Expr expr, Assumptions assumptions) => expr switch
    {
        Constant c => c.Value.Sign >= 0,
        Variable v => assumptions.IsNonNegative(v.Name),
        Pi => true,
        E => true,
        Abs(_) => true, // |anything| >= 0 always, regardless of assumptions
        Power(_, Constant e) when e.Value.IsInteger && e.Value.Sign > 0 && e.Value.Numerator % 2 == 0 => true, // even integer power
        _ => false
    };
}