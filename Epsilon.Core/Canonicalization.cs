namespace Epsilon.Core;

public static class Canonicalizer
{
    public static Expr Canonicalize(this Expr expr) => expr switch
    {
        Add(var l, var r) => OrderCommutative(l.Canonicalize(), r.Canonicalize(), AddRank, static (a, b) => new Add(a, b)),
        Multiply(var l, var r) => OrderCommutative(l.Canonicalize(), r.Canonicalize(), MultiplyRank, static (a, b) => new Multiply(a, b)),

        Subtract(var l, var r) => new Subtract(l.Canonicalize(), r.Canonicalize()),
        Divide(var n, var d) => new Divide(n.Canonicalize(), d.Canonicalize()),
        Power(var b, var e) => new Power(b.Canonicalize(), e.Canonicalize()),

        Sin(var a) => new Sin(a.Canonicalize()),
        Cos(var a) => new Cos(a.Canonicalize()),
        Tan(var a) => new Tan(a.Canonicalize()),
        Cot(var a) => new Cot(a.Canonicalize()),
        Sec(var a) => new Sec(a.Canonicalize()),
        Csc(var a) => new Csc(a.Canonicalize()),
        Asin(var a) => new Asin(a.Canonicalize()),
        Acos(var a) => new Acos(a.Canonicalize()),
        Atan(var a) => new Atan(a.Canonicalize()),
        Sinh(var a) => new Sinh(a.Canonicalize()),
        Cosh(var a) => new Cosh(a.Canonicalize()),
        Tanh(var a) => new Tanh(a.Canonicalize()),
        Exp(var a) => new Exp(a.Canonicalize()),
        Ln(var a) => new Ln(a.Canonicalize()),
        Sqrt(var a) => new Sqrt(a.Canonicalize()),
        NthRoot(var a, var n) => new NthRoot(a.Canonicalize(), n.Canonicalize()),

        _ => expr
    };

    private static Expr OrderCommutative(Expr a, Expr b, Func<Expr, Expr, Expr> build)
    {
        int rankA = CanonicalRank(a);
        int rankB = CanonicalRank(b);

        if (rankA > rankB)
            return build(b, a);

        if (rankA == rankB)
        {
            // Same category (e.g. both non-constant, or both constant)
            int comparison = string.CompareOrdinal(a.ToString(), b.ToString());
            if (comparison > 0)
                return build(b, a);
        }

        return build(a, b);
    }

    // Lower rank sorts first: variable terms before pure constants
    private static int CanonicalRank(Expr e) => e switch
    {
        Constant => 0,   // numeric coefficients always come first
        Pi or E => 2,
        _ when IsPureConstant(e) => 2,
        _ => 1
    };

    private static bool IsPureConstant(Expr e) => e switch
    {
        Constant => true,
        Pi => true,
        E => true,
        Add(var l, var r) => IsPureConstant(l) && IsPureConstant(r),
        Subtract(var l, var r) => IsPureConstant(l) && IsPureConstant(r),
        Multiply(var l, var r) => IsPureConstant(l) && IsPureConstant(r),
        Divide(var l, var r) => IsPureConstant(l) && IsPureConstant(r),
        Power(var b, var ex) => IsPureConstant(b) && IsPureConstant(ex),
        _ => false
    };

    private static Expr OrderCommutative(Expr a, Expr b, Func<Expr, int> rank, Func<Expr, Expr, Expr> build)
    {
        int rankA = rank(a);
        int rankB = rank(b);

        if (rankA > rankB)
            return build(b, a);

        if (rankA == rankB)
        {
            int comparison = string.CompareOrdinal(a.ToString(), b.ToString());
            if (comparison > 0)
                return build(b, a);
        }

        return build(a, b);
    }

    private static int AddRank(Expr e) => IsPureConstant(e) ? 1 : 0;
    
    private static int MultiplyRank(Expr e) => e switch
    {
        Constant => 0,
        _ => 1
    };
}