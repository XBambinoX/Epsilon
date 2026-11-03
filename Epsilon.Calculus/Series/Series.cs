using Epsilon.Core;

namespace Epsilon.Calculus;

/// <summary>
/// A series of an expression at a point a: the terms c (x - a)^q up to a degree, with exact
/// coefficients and rational exponents, and the order of what is left, O((x - a)^Order). The
/// exponents may be negative, as in a Laurent series, or fractions, as in a Puiseux series.
/// </summary>
public sealed class Series
{
    private readonly Expr _base;

    internal Series(string variable, Expr point, IReadOnlyList<(Rational Exponent, Expr Coefficient)> terms, Rational order, bool rightSided)
    {
        Variable = variable;
        Point = point;
        Terms = terms;
        Order = order;
        RightSided = rightSided;
        var x = new Epsilon.Core.Variable(variable);
        _base = point is Constant { Value.IsZero: true } ? x : (x - point).Simplify();
    }

    /// <summary>The variable of the series.</summary>
    public string Variable { get; }

    /// <summary>The point a it is taken at.</summary>
    public Expr Point { get; }

    /// <summary>The terms with a coefficient other than 0, by increasing exponent.</summary>
    public IReadOnlyList<(Rational Exponent, Expr Coefficient)> Terms { get; }

    /// <summary>The exponent of the remainder: the rest of the expression is O((x - a)^Order).</summary>
    public Rational Order { get; }

    /// <summary>
    /// Whether the series holds only for x &gt; a: a power that isn't an integer, a root, abs or
    /// sign of a part that vanishes at a was taken. sqrt(x) at 0 is x^(1/2) there, and sqrt(x^2)
    /// is x.
    /// </summary>
    public bool RightSided { get; }

    /// <summary>The coefficient of (x - a)^exponent, 0 for an exponent without a term.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The exponent is <see cref="Order"/> or more, beyond the series.</exception>
    public Expr Coefficient(Rational exponent)
    {
        if (exponent >= Order)
            throw new ArgumentOutOfRangeException(nameof(exponent), exponent, $"The series is known below the order {Order}.");

        foreach (var (q, c) in Terms)
            if (q == exponent)
                return c;
        return 0;
    }

    /// <summary>The sum of the terms, without the remainder, as an expression in the variable.</summary>
    public Expr ToExpr() => Terms.Count == 0
        ? 0
        : Terms.Select(term => term.Coefficient * Raised(term.Exponent)).Aggregate((sum, term) => sum + term).Simplify();

    /// <summary>
    /// The terms by increasing exponent and the remainder: <c>1 - (1/6) * x^2 + (1/120) * x^4 + O(x^5)</c>.
    /// </summary>
    public string Print()
    {
        var text = new System.Text.StringBuilder();
        foreach (var (q, c) in Terms)
        {
            Expr opposite = Opposite(c);
            bool negative = Negative(c) && !Negative(opposite);
            Expr magnitude = negative ? opposite : c;
            Expr term = q.IsZero ? magnitude : magnitude is Constant { Value.IsOne: true } ? Raised(q) : new Multiply(magnitude, Raised(q));
            text.Append(text.Length == 0 ? negative ? "-" : "" : negative ? " - " : " + ");
            text.Append(term is Add or Subtract ? $"({term.Print()})" : term.Print());
        }

        text.Append(text.Length == 0 ? "O(" : " + O(").Append(Raised(Order).Print()).Append(')');
        return text.ToString();
    }

    /// <inheritdoc/>
    public override string ToString() => Print();

    // -c without the double sign the printer gives -(-sin(a) / 2).
    private static Expr Opposite(Expr c) => c switch
    {
        Constant constant => new Constant(-constant.Value),
        Negate negate => negate.Argument,
        Divide quotient => new Divide(Opposite(quotient.Numerator), quotient.Denominator),
        Multiply product => new Multiply(Opposite(product.Left), product.Right),
        _ => SeriesExpander.Tidy(-c),
    };

    private static bool Negative(Expr c) => c.Print() is var text && (text.StartsWith('-') || text.StartsWith("(-"));

    private Expr Raised(Rational exponent) => exponent.IsOne ? _base : new Power(_base, exponent);
}
