namespace Epsilon.Core;

// Display order of the terms of a sum, shared by Printer and LatexPrinter: descending degree,
// then terms without variables last - x^2 + 2x + sin(x) + 1, as people write polynomials.
// Only the printed order changes; the tree keeps its canonical order, which Equals and the
// simplifier rely on.
internal static class SumTerms
{
    // Flattens an Add/Subtract chain into its terms, each with whether it is subtracted.
    // OrderBy is stable, so equal-degree terms keep their canonical order.
    public static IReadOnlyList<(bool Subtracted, Expr Term)> InDisplayOrder(Expr sum)
    {
        var terms = new List<(bool Subtracted, Expr Term)>();
        Flatten(sum, subtracted: false, terms);

        return terms
            .OrderBy(t => t.Term.GetVariables().Count == 0 ? 1 : 0)
            .ThenByDescending(t => Degree(t.Term))
            .ToList();
    }

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

    // Total degree of a monomial-like term (3x^2*y -> 3); 0 for anything else, such as sin(x).
    private static int Degree(Expr term) => term switch
    {
        Variable => 1,
        Power(Variable, Constant e) when e.Value.IsInteger => (int)Math.Clamp(e.Value.ToDouble(), -1e6, 1e6),
        Multiply(var l, var r) => Degree(l) + Degree(r),
        Divide(var n, Constant) => Degree(n),
        Negate(var a) => Degree(a),
        _ => 0
    };

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
