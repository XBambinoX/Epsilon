namespace Epsilon.Core;

public static class Canonicalizer
{
    public static Expr Canonicalize(this Expr expr) => expr switch
    {
        Add(var l, var r) => CanonicalizeAddChain(l, r),
        Multiply(var l, var r) => CanonicalizeMultiplyChain(l, r),

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

    // ---- Add: flatten -> canonicalize each term -> sort globally -> rebuild ----

    private static Expr CanonicalizeAddChain(Expr left, Expr right)
    {
        var rawTerms = new List<Expr>();
        FlattenAdd(left, rawTerms);
        FlattenAdd(right, rawTerms);

        var terms = rawTerms.Select(t => t.Canonicalize()).ToList();

        var ordered = terms
            .OrderBy(AddRank)
            .ThenBy(t => t.ToString(), StringComparer.Ordinal)
            .ToList();

        return RebuildLeftAssociative(ordered, static (a, b) => new Add(a, b));
    }

    private static void FlattenAdd(Expr expr, List<Expr> terms)
    {
        if (expr is Add(var l, var r))
        {
            FlattenAdd(l, terms);
            FlattenAdd(r, terms);
        }
        else
        {
            terms.Add(expr);
        }
    }

    // ---- Multiply: same idea ----

    private static Expr CanonicalizeMultiplyChain(Expr left, Expr right)
    {
        var rawFactors = new List<Expr>();
        FlattenMultiply(left, rawFactors);
        FlattenMultiply(right, rawFactors);

        var factors = rawFactors.Select(t => t.Canonicalize()).ToList();

        var ordered = factors
            .OrderBy(MultiplyRank)
            .ThenBy(t => t.ToString(), StringComparer.Ordinal)
            .ToList();

        return RebuildLeftAssociative(ordered, static (a, b) => new Multiply(a, b));
    }

    private static void FlattenMultiply(Expr expr, List<Expr> factors)
    {
        if (expr is Multiply(var l, var r))
        {
            FlattenMultiply(l, factors);
            FlattenMultiply(r, factors);
        }
        else
        {
            factors.Add(expr);
        }
    }

    private static Expr RebuildLeftAssociative(List<Expr> items, Func<Expr, Expr, Expr> build)
    {
        Expr result = items[0];
        for (int i = 1; i < items.Count; i++)
            result = build(result, items[i]);
        return result;
    }

    // ---- Ranking (unchanged rules, now applied globally instead of pairwise) ----

    // Lower rank sorts first: variable terms, then real constants, then terms containing i
    private static int AddRank(Expr e) =>
        !IsPureConstant(e) ? 0 :
        ContainsImaginaryUnit(e) ? 2 :
        1;

    private static int MultiplyRank(Expr e) => e switch
    {
        Constant => 0,
        _ => 1
    };

    private static bool IsPureConstant(Expr e) => e switch
    {
        Constant => true,
        Pi => true,
        E => true,
        ImaginaryUnit => true,
        Add(var l, var r) => IsPureConstant(l) && IsPureConstant(r),
        Subtract(var l, var r) => IsPureConstant(l) && IsPureConstant(r),
        Multiply(var l, var r) => IsPureConstant(l) && IsPureConstant(r),
        Divide(var l, var r) => IsPureConstant(l) && IsPureConstant(r),
        Power(var b, var ex) => IsPureConstant(b) && IsPureConstant(ex),
        _ => false
    };

    private static bool ContainsImaginaryUnit(Expr e) => e switch
    {
        ImaginaryUnit => true,
        Add(var l, var r) => ContainsImaginaryUnit(l) || ContainsImaginaryUnit(r),
        Subtract(var l, var r) => ContainsImaginaryUnit(l) || ContainsImaginaryUnit(r),
        Multiply(var l, var r) => ContainsImaginaryUnit(l) || ContainsImaginaryUnit(r),
        Divide(var l, var r) => ContainsImaginaryUnit(l) || ContainsImaginaryUnit(r),
        Power(var b, var ex) => ContainsImaginaryUnit(b) || ContainsImaginaryUnit(ex),
        _ => false
    };
}