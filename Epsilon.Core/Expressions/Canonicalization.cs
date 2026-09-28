namespace Epsilon.Core;

public static class Canonicalizer
{
    public static Expr Canonicalize(this Expr expr) => expr switch
    {
        Add(var l, var r) => CanonicalizeAddChain(l, r),
        Multiply(var l, var r) => CanonicalizeMultiplyChain(l, r),

        // Every other node type: recurse into children via the shared walker,
        // reusing Canonicalize itself so nested Add/Multiply chains still get
        // flattened and sorted, not just generically rebuilt.
        _ => expr.MapChildren(static child => child.Canonicalize())
    };

    // ---- Add: flatten -> canonicalize each term in place -> sort globally -> rebuild ----

    private static Expr CanonicalizeAddChain(Expr left, Expr right)
    {
        var terms = new List<Expr>();
        FlattenAdd(left, terms);
        FlattenAdd(right, terms);

        for (int i = 0; i < terms.Count; i++)
            terms[i] = terms[i].Canonicalize();

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
        var factors = new List<Expr>();
        FlattenMultiply(left, factors);
        FlattenMultiply(right, factors);

        for (int i = 0; i < factors.Count; i++)
            factors[i] = factors[i].Canonicalize();

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

    // Total order used to sort terms and factors: by node type name, then by payload
    // (Constant value, Variable name), then child by child. Generic over Children, so
    // it covers every node type without listing them.
    private static int StructuralCompare(Expr a, Expr b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        // Full name, not just Name: a Sin node from another module must not tie with
        // Epsilon.Core.Sin, or the canonical order of the two would depend on input order.
        // (Core types share one namespace, so their relative order is unchanged.)
        int typeCompare = string.CompareOrdinal(TypeKey(a), TypeKey(b));
        if (typeCompare != 0)
            return typeCompare;

        int payloadCompare = a.ComparePayload(b);
        if (payloadCompare != 0)
            return payloadCompare;

        ImmutableArray<Expr> left = a.Children, right = b.Children;
        for (int i = 0; i < Math.Min(left.Length, right.Length); i++)
        {
            int childCompare = StructuralCompare(left[i], right[i]);
            if (childCompare != 0)
                return childCompare;
        }

        return left.Length.CompareTo(right.Length);
    }

    private static string TypeKey(Expr expr) => expr.GetType().FullName ?? expr.GetType().Name;
}
