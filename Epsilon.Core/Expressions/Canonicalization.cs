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
        Abs(var a) => new Abs(a.Canonicalize()),
        Sqrt(var a) => new Sqrt(a.Canonicalize()),
        NthRoot(var a, var n) => new NthRoot(a.Canonicalize(), n.Canonicalize()),

        Asinh(var a) => new Asinh(a.Canonicalize()),
        Acosh(var a) => new Acosh(a.Canonicalize()),
        Atanh(var a) => new Atanh(a.Canonicalize()),
        Coth(var a) => new Coth(a.Canonicalize()),
        Sech(var a) => new Sech(a.Canonicalize()),
        Csch(var a) => new Csch(a.Canonicalize()),

        Negate(var a) => new Negate(a.Canonicalize()),

        Sign(var a) => new Sign(a.Canonicalize()),
        Floor(var a) => new Floor(a.Canonicalize()),
        Ceiling(var a) => new Ceiling(a.Canonicalize()),
        Round(var a) => new Round(a.Canonicalize()),
        Min(var l, var r) => new Min(l.Canonicalize(), r.Canonicalize()),
        Max(var l, var r) => new Max(l.Canonicalize(), r.Canonicalize()),

        _ => expr
    };

    // ---- Add: flatten -> canonicalize each term -> sort globally -> rebuild ----

    private static Expr CanonicalizeAddChain(Expr left, Expr right)
    {
        var rawTerms = new List<Expr>();
        FlattenAdd(left, rawTerms);
        FlattenAdd(right, rawTerms);

        var terms = rawTerms.Select(t => t.Canonicalize()).ToList();

        terms.Sort((a, b) =>
        {
            int rankCompare = AddRank(a).CompareTo(AddRank(b));
            return rankCompare != 0 ? rankCompare : StructuralCompare(a, b);
        });

        return RebuildLeftAssociative(terms, static (a, b) => new Add(a, b));
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

        factors.Sort((a, b) =>
        {
            int rankCompare = MultiplyRank(a).CompareTo(MultiplyRank(b));
            return rankCompare != 0 ? rankCompare : StructuralCompare(a, b);
        });

        return RebuildLeftAssociative(factors, static (a, b) => new Multiply(a, b));
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
    
    // Lower rank sorts first: variable terms, then real constants, then terms containing i
    private static int AddRank(Expr e) =>
        !IsPureConstant(e) ? 0 :
        ContainsImaginaryUnit(e) ? 2 :
        1;

    private static int MultiplyRank(Expr e) => e switch
    {
        Constant => 0,
        Negate(Constant) => 0,
        _ => 1
    };

    private static bool IsPureConstant(Expr e) => e switch
    {
        Constant => true,
        Pi => true,
        E => true,
        ImaginaryUnit => true,
        Negate(var a) => IsPureConstant(a),
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
        Negate(var a) => ContainsImaginaryUnit(a),
        Add(var l, var r) => ContainsImaginaryUnit(l) || ContainsImaginaryUnit(r),
        Subtract(var l, var r) => ContainsImaginaryUnit(l) || ContainsImaginaryUnit(r),
        Multiply(var l, var r) => ContainsImaginaryUnit(l) || ContainsImaginaryUnit(r),
        Divide(var l, var r) => ContainsImaginaryUnit(l) || ContainsImaginaryUnit(r),
        Power(var b, var ex) => ContainsImaginaryUnit(b) || ContainsImaginaryUnit(ex),
        _ => false
    };

    private static int StructuralCompare(Expr a, Expr b)
    {
        int typeCompare = string.CompareOrdinal(a.GetType().Name, b.GetType().Name);
        if (typeCompare != 0)
            return typeCompare;

        return (a, b) switch
        {
            (Constant x, Constant y) => x.Value.ToDouble().CompareTo(y.Value.ToDouble()),
            (Variable x, Variable y) => string.CompareOrdinal(x.Name, y.Name),
            (Pi, Pi) => 0,
            (E, E) => 0,
            (ImaginaryUnit, ImaginaryUnit) => 0,

            (Add(var l1, var r1), Add(var l2, var r2)) => CompareChildren(l1, r1, l2, r2),
            (Subtract(var l1, var r1), Subtract(var l2, var r2)) => CompareChildren(l1, r1, l2, r2),
            (Multiply(var l1, var r1), Multiply(var l2, var r2)) => CompareChildren(l1, r1, l2, r2),
            (Divide(var n1, var d1), Divide(var n2, var d2)) => CompareChildren(n1, d1, n2, d2),
            (Power(var b1, var e1), Power(var b2, var e2)) => CompareChildren(b1, e1, b2, e2),
            (Negate(var a1), Negate(var a2)) => StructuralCompare(a1, a2),

            (Sin(var a1), Sin(var a2)) => StructuralCompare(a1, a2),
            (Cos(var a1), Cos(var a2)) => StructuralCompare(a1, a2),
            (Tan(var a1), Tan(var a2)) => StructuralCompare(a1, a2),
            (Cot(var a1), Cot(var a2)) => StructuralCompare(a1, a2),
            (Sec(var a1), Sec(var a2)) => StructuralCompare(a1, a2),
            (Csc(var a1), Csc(var a2)) => StructuralCompare(a1, a2),
            (Asin(var a1), Asin(var a2)) => StructuralCompare(a1, a2),
            (Acos(var a1), Acos(var a2)) => StructuralCompare(a1, a2),
            (Atan(var a1), Atan(var a2)) => StructuralCompare(a1, a2),
            (Sinh(var a1), Sinh(var a2)) => StructuralCompare(a1, a2),
            (Cosh(var a1), Cosh(var a2)) => StructuralCompare(a1, a2),
            (Tanh(var a1), Tanh(var a2)) => StructuralCompare(a1, a2),
            (Asinh(var a1), Asinh(var a2)) => StructuralCompare(a1, a2),
            (Acosh(var a1), Acosh(var a2)) => StructuralCompare(a1, a2),
            (Atanh(var a1), Atanh(var a2)) => StructuralCompare(a1, a2),
            (Coth(var a1), Coth(var a2)) => StructuralCompare(a1, a2),
            (Sech(var a1), Sech(var a2)) => StructuralCompare(a1, a2),
            (Csch(var a1), Csch(var a2)) => StructuralCompare(a1, a2),
            (Exp(var a1), Exp(var a2)) => StructuralCompare(a1, a2),
            (Ln(var a1), Ln(var a2)) => StructuralCompare(a1, a2),
            (Sqrt(var a1), Sqrt(var a2)) => StructuralCompare(a1, a2),
            (Abs(var a1), Abs(var a2)) => StructuralCompare(a1, a2),
            (Sign(var a1), Sign(var a2)) => StructuralCompare(a1, a2),
            (Floor(var a1), Floor(var a2)) => StructuralCompare(a1, a2),
            (Ceiling(var a1), Ceiling(var a2)) => StructuralCompare(a1, a2),
            (Round(var a1), Round(var a2)) => StructuralCompare(a1, a2),

            (NthRoot(var a1, var n1), NthRoot(var a2, var n2)) => CompareChildren(a1, n1, a2, n2),
            (Min(var l1, var r1), Min(var l2, var r2)) => CompareChildren(l1, r1, l2, r2),
            (Max(var l1, var r1), Max(var l2, var r2)) => CompareChildren(l1, r1, l2, r2),

            _ => 0
        };

        static int CompareChildren(Expr l1, Expr r1, Expr l2, Expr r2)
        {
            int leftCompare = StructuralCompare(l1, l2);
            return leftCompare != 0 ? leftCompare : StructuralCompare(r1, r2);
        }
    }
}