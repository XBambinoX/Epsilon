namespace Epsilon.Core;

// Shared bottom-up tree walker used by both Simplifier and Canonicalizer.
// Centralizes the "list every Expr node type once" concern so a new node
// type only needs to be added here, not copy-pasted across every pass
// that walks the tree.
internal static class TreeRewriter
{
    // Walks the tree bottom-up, applying `leafRule` at every node. If
    // `leafRule` returns non-null, the result is rewritten again (so a rule
    // can produce a form that another rule further transforms).
    public static Expr Rewrite(Expr expr, Func<Expr, Expr?> leafRule)
    {
        Expr? replaced = leafRule(expr);
        if (replaced is not null)
            return Rewrite(replaced, leafRule);

        return RewriteChildren(expr, child => Rewrite(child, leafRule));
    }

    // Rebuilds `expr` with each child passed through `recurse`, using
    // reference-equality to avoid allocating a new node when nothing
    // beneath it changed.
    public static Expr RewriteChildren(Expr expr, Func<Expr, Expr> recurse) => expr switch
    {
        Add(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Add(a, b)),
        Subtract(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Subtract(a, b)),
        Multiply(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Multiply(a, b)),
        Divide(var n, var d) => RebuildBinary(expr, n, d, recurse, static (a, b) => new Divide(a, b)),
        Power(var b, var e) => RebuildBinary(expr, b, e, recurse, static (a, b) => new Power(a, b)),
        Negate(var a) => RebuildUnary(expr, a, recurse, static x => new Negate(x)),

        Sin(var a) => RebuildUnary(expr, a, recurse, static x => new Sin(x)),
        Cos(var a) => RebuildUnary(expr, a, recurse, static x => new Cos(x)),
        Tan(var a) => RebuildUnary(expr, a, recurse, static x => new Tan(x)),
        Cot(var a) => RebuildUnary(expr, a, recurse, static x => new Cot(x)),
        Sec(var a) => RebuildUnary(expr, a, recurse, static x => new Sec(x)),
        Csc(var a) => RebuildUnary(expr, a, recurse, static x => new Csc(x)),
        Asin(var a) => RebuildUnary(expr, a, recurse, static x => new Asin(x)),
        Acos(var a) => RebuildUnary(expr, a, recurse, static x => new Acos(x)),
        Atan(var a) => RebuildUnary(expr, a, recurse, static x => new Atan(x)),
        Sinh(var a) => RebuildUnary(expr, a, recurse, static x => new Sinh(x)),
        Cosh(var a) => RebuildUnary(expr, a, recurse, static x => new Cosh(x)),
        Tanh(var a) => RebuildUnary(expr, a, recurse, static x => new Tanh(x)),
        Asinh(var a) => RebuildUnary(expr, a, recurse, static x => new Asinh(x)),
        Acosh(var a) => RebuildUnary(expr, a, recurse, static x => new Acosh(x)),
        Atanh(var a) => RebuildUnary(expr, a, recurse, static x => new Atanh(x)),
        Coth(var a) => RebuildUnary(expr, a, recurse, static x => new Coth(x)),
        Sech(var a) => RebuildUnary(expr, a, recurse, static x => new Sech(x)),
        Csch(var a) => RebuildUnary(expr, a, recurse, static x => new Csch(x)),
        Exp(var a) => RebuildUnary(expr, a, recurse, static x => new Exp(x)),
        Ln(var a) => RebuildUnary(expr, a, recurse, static x => new Ln(x)),
        Sqrt(var a) => RebuildUnary(expr, a, recurse, static x => new Sqrt(x)),
        Abs(var a) => RebuildUnary(expr, a, recurse, static x => new Abs(x)),
        Sign(var a) => RebuildUnary(expr, a, recurse, static x => new Sign(x)),
        Floor(var a) => RebuildUnary(expr, a, recurse, static x => new Floor(x)),
        Ceiling(var a) => RebuildUnary(expr, a, recurse, static x => new Ceiling(x)),
        Round(var a) => RebuildUnary(expr, a, recurse, static x => new Round(x)),

        Min(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Min(a, b)),
        Max(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Max(a, b)),
        NthRoot(var a, var n) => RebuildBinary(expr, a, n, recurse, static (a2, b) => new NthRoot(a2, b)),

        // Constant, Variable, Pi, E, ImaginaryUnit — leaves, nothing to recurse into
        _ => expr
    };

    private static Expr RebuildUnary(Expr original, Expr child, Func<Expr, Expr> recurse, Func<Expr, Expr> build)
    {
        Expr newChild = recurse(child);
        return ReferenceEquals(newChild, child) ? original : build(newChild);
    }

    private static Expr RebuildBinary(Expr original, Expr left, Expr right, Func<Expr, Expr> recurse, Func<Expr, Expr, Expr> build)
    {
        Expr newLeft = recurse(left);
        Expr newRight = recurse(right);
        return ReferenceEquals(newLeft, left) && ReferenceEquals(newRight, right)
            ? original
            : build(newLeft, newRight);
    }
}