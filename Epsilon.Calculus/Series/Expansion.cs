using Epsilon.Core;

namespace Epsilon.Calculus;

// A series in t = x - a cut off at Precision: the terms c t^q for the exponents q below it, all
// known exactly, the zero ones left out. RightSided where it holds only for t > 0.
internal sealed class Expansion
{
    private readonly SortedDictionary<Rational, Expr> _terms;

    private Expansion(SortedDictionary<Rational, Expr> terms, Rational precision, bool rightSided)
    {
        _terms = terms;
        Precision = precision;
        RightSided = rightSided;
    }

    public IReadOnlyDictionary<Rational, Expr> Terms => _terms;

    public Rational Precision { get; }

    public bool RightSided { get; }

    public Rational? Valuation => _terms.Count == 0 ? null : _terms.Keys.First();

    public Expr Leading => _terms.Values.First();

    public Expr this[Rational exponent] => _terms.TryGetValue(exponent, out Expr? c) ? c : 0;

    public static Expansion Of(IEnumerable<KeyValuePair<Rational, Expr>> terms, Rational precision, bool rightSided)
    {
        var sums = new SortedDictionary<Rational, Expr>();
        foreach (var (exponent, c) in terms)
            if (exponent < precision)
                sums[exponent] = sums.TryGetValue(exponent, out Expr? sum) ? sum + c : c;

        var nonzero = new SortedDictionary<Rational, Expr>();
        foreach (var (exponent, sum) in sums)
            if (sum.Simplify() is var c && c is not Constant { Value.IsZero: true })
                nonzero[exponent] = c;

        return new Expansion(nonzero, precision, rightSided);
    }

    public static Expansion Constant(Expr c, Rational precision) => Of([KeyValuePair.Create(Rational.Zero, c)], precision, false);

    public static Expansion Zero(Rational precision) => Of([], precision, false);

    public Expansion Plus(Expansion other) =>
        Of(_terms.Concat(other._terms), Min(Precision, other.Precision), RightSided || other.RightSided);

    public Expansion Plus(Expr c) => Plus(Constant(c, Precision));

    public Expansion Negated() => Scaled(-1);

    // The precision of a product is that of either factor times the lowest term of the other.
    public Expansion Times(Expansion other)
    {
        Rational precision = Min(Precision + (other.Valuation ?? other.Precision), other.Precision + (Valuation ?? Precision));
        var products = from a in _terms
                       from b in other._terms
                       where a.Key + b.Key < precision
                       select KeyValuePair.Create(a.Key + b.Key, a.Value * b.Value);
        return Of(products, precision, RightSided || other.RightSided);
    }

    public Expansion Scaled(Expr c) => Of(_terms.Select(term => KeyValuePair.Create(term.Key, c * term.Value)), Precision, RightSided);

    public Expansion Shifted(Rational exponent) =>
        Of(_terms.Select(term => KeyValuePair.Create(term.Key + exponent, term.Value)), Precision + exponent, RightSided);

    public Expansion Truncated(Rational precision) => new(Below(precision), Min(precision, Precision), RightSided);

    public Expansion OnRight(bool rightSided) => rightSided && !RightSided ? new(_terms, Precision, true) : this;

    private SortedDictionary<Rational, Expr> Below(Rational precision) =>
        new(_terms.Where(term => term.Key < precision).ToDictionary(term => term.Key, term => term.Value));

    private static Rational Min(Rational a, Rational b) => a < b ? a : b;
}
