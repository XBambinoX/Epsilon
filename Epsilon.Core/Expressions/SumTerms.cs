namespace Epsilon.Core;

// Display order of the terms of a sum and the factors of a product, shared by Printer and
// LatexPrinter: x^2 * y + 3x * y^2 + sin(x) + 1, π * x * exp(x), as people write them.
// Only the printed order changes; the tree keeps its canonical order, which Equals and the
// simplifier rely on.
internal static class SumTerms
{
    // Flattens an Add/Subtract chain into its terms, each with whether it is subtracted:
    // descending degree, equal degrees by the powers of the variables in name order, terms
    // without variables last. OrderBy is stable, so the remaining ties keep their canonical order.
    public static IReadOnlyList<(bool Subtracted, Expr Term)> InDisplayOrder(Expr sum)
    {
        var terms = new List<(bool Subtracted, Expr Term)>();
        Flatten(sum, subtracted: false, terms);

        var ordered = terms
            .Select(t => (Entry: t, Constant: !t.Term.HasVariables(), Powers: Powers(t.Term)))
            .Select(t => (t.Entry, t.Constant, t.Powers, Degree: t.Powers.Values.Sum()))
            .OrderBy(t => t.Constant ? 1 : 0)
            .ThenByDescending(t => t.Degree)
            .ThenBy(t => t.Powers, Comparer<SortedDictionary<string, int>>.Create(CompareLexicographically))
            .ToList();

        // x - a, not -a + x: a positive term of the leading degree goes first. Lower degrees stay
        // after it, so -x^2 + 5 keeps its order.
        var head = ordered[0];
        if (IsNegative(head.Entry))
        {
            int positive = ordered.FindIndex(t => t.Constant == head.Constant && t.Degree == head.Degree && !IsNegative(t.Entry));
            if (positive > 0)
            {
                var first = ordered[positive];
                ordered.RemoveAt(positive);
                ordered.Insert(0, first);
            }
        }

        return ordered.Select(t => t.Entry).ToList();
    }

    // Whether the term prints with a minus in front: -x, -2x, -sin(x) / 2.
    private static bool IsNegative((bool Subtracted, Expr Term) entry) => entry.Subtracted != StartsWithMinus(entry.Term);

    private static bool StartsWithMinus(Expr term) => term switch
    {
        Constant c => c.Value.Sign < 0,
        Negate => true,
        Divide(var n, _) => StartsWithMinus(n),
        Multiply => ProductStartsWithMinus(term),
        _ => false
    };

    // The printers write the first number of a product as its coefficient, or the first factor.
    private static bool ProductStartsWithMinus(Expr product)
    {
        var factors = new List<Expr>();
        FlattenMultiply(product, factors);

        return factors.OfType<Constant>().FirstOrDefault() is { } coefficient
            ? coefficient.Value.Sign < 0
            : StartsWithMinus(FactorsInDisplayOrder(factors)[0]);
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

    // Numbers and other constants first, then the variables and their powers by name, then
    // the rest in canonical order: π * x^2 * y * exp(x).
    public static List<Expr> FactorsInDisplayOrder(List<Expr> factors) =>
        factors
            .OrderBy(FactorGroup)
            .ThenBy(f => VariableOf(f)?.Name ?? "", StringComparer.Ordinal)
            .ToList();

    private static int FactorGroup(Expr factor) =>
        VariableOf(factor) is not null ? 1 : !factor.HasVariables() ? 0 : 2;

    // A negated factor sorts as what it negates: -a * b, not b * -a.
    private static Variable? VariableOf(Expr factor) => factor switch
    {
        Variable v => v,
        Power(Variable v, _) => v,
        Negate(var a) => VariableOf(a),
        _ => null
    };

    private static void Flatten(Expr expr, bool subtracted, List<(bool, Expr)> terms)
    {
        switch (expr)
        {
            case Add(var l, var r):
                Flatten(l, subtracted, terms);
                Flatten(r, subtracted, terms);
                break;

            case Subtract(var l, var r):
                Flatten(l, subtracted, terms);
                Flatten(r, !subtracted, terms);
                break;

            default:
                terms.Add((subtracted, expr));
                break;
        }
    }

    // The integer powers of the variables in a monomial-like term: 3x^2 * y -> x^2, y.
    // Other factors, such as sin(x), add nothing.
    private static SortedDictionary<string, int> Powers(Expr term)
    {
        var powers = new SortedDictionary<string, int>(StringComparer.Ordinal);
        AddPowers(term, powers);
        return powers;
    }

    private static void AddPowers(Expr term, SortedDictionary<string, int> powers)
    {
        switch (term)
        {
            case Variable v:
                powers[v.Name] = powers.GetValueOrDefault(v.Name) + 1;
                break;

            case Power(Variable v, Constant e) when e.Value.IsInteger:
                powers[v.Name] = powers.GetValueOrDefault(v.Name) + (int)Math.Clamp(e.Value.ToDouble(), -1e6, 1e6);
                break;

            case Multiply(var l, var r):
                AddPowers(l, powers);
                AddPowers(r, powers);
                break;

            case Divide(var n, Constant):
                AddPowers(n, powers);
                break;

            case Negate(var a):
                AddPowers(a, powers);
                break;
        }
    }

    // The higher power of the first variable by name goes first: x^2 * y before x * y^2.
    private static int CompareLexicographically(SortedDictionary<string, int>? a, SortedDictionary<string, int>? b)
    {
        foreach (string name in a!.Keys.Union(b!.Keys).Order(StringComparer.Ordinal))
        {
            int compare = b.GetValueOrDefault(name).CompareTo(a.GetValueOrDefault(name));
            if (compare != 0)
                return compare;
        }

        return 0;
    }

    // Joins printed terms with " + " / " - ". A printed term that starts with '-' is a
    // negation of the whole term (-4x, -x^2, -(a + b), -3/4), so a + (-4x) prints as a - 4x.
    public static string Join(IReadOnlyList<(bool Subtracted, Expr Term)> terms, Func<Expr, string> print)
    {
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < terms.Count; i++)
        {
            var (subtracted, term) = terms[i];
            string text = print(term);

            bool negative = text.StartsWith('-');
            if (negative)
                text = text[1..];
            if (subtracted)
                negative = !negative;

            if (i == 0)
                sb.Append(negative ? "-" : "").Append(text);
            else
                sb.Append(negative ? " - " : " + ").Append(text);
        }

        return sb.ToString();
    }
}
