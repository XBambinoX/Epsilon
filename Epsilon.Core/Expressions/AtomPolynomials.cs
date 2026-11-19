namespace Epsilon.Core;

// Polynomials with rational coefficients in the atoms of expressions: variables, and anything
// else that isn't a sum, product, number or positive integer power, such as sin(x), sqrt(x) or
// 1/x, each taken as a symbol of its own. Where one such polynomial divides another exactly, the
// quotient of the expressions is the quotient polynomial wherever the divisor isn't 0.
internal sealed class AtomPolynomials
{
    private const int MaxTerms = 1000;
    private const int MaxPower = 64;
    private const int MaxDivisionSteps = 10_000;

    private readonly List<Expr> _atoms = [];
    private readonly Dictionary<Expr, int> _index = [];

    // null where the polynomial would have more than MaxTerms terms.
    public Dictionary<Monomial, Rational>? Of(Expr e)
    {
        switch (e)
        {
            case Constant c:
                return c.Value.IsZero ? [] : new() { [Monomial.One] = c.Value };
            case Add(var l, var r):
                return Sum(Of(l), Of(r), Rational.One);
            case Subtract(var l, var r):
                return Sum(Of(l), Of(r), Rational.MinusOne);
            case Negate(var a):
                return Scaled(Of(a), Rational.MinusOne);
            case Multiply(var l, var r):
                return Product(Of(l), Of(r));
            case Divide(var n, Constant c) when !c.Value.IsZero:
                return Scaled(Of(n), Rational.One / c.Value);
            case Power(var b, Constant { Value: { IsInteger: true, Sign: > 0 } k }) when k.Numerator <= MaxPower:
                var power = Of(b);
                var result = power;
                for (int i = 1; i < (int)k.Numerator && result is not null; i++)
                    result = Product(result, power);
                return result;
            default:
                if (!_index.TryGetValue(e, out int index))
                {
                    index = _atoms.Count;
                    _atoms.Add(e);
                    _index[e] = index;
                }
                return new() { [Monomial.Atom(index)] = Rational.One };
        }
    }

    // The atoms of a polynomial with the highest power each has in it.
    public IEnumerable<(Expr Atom, int Degree)> Degrees(Dictionary<Monomial, Rational> polynomial)
    {
        var degrees = new int[_atoms.Count];
        foreach (Monomial monomial in polynomial.Keys)
            for (int i = 0; i < degrees.Length; i++)
                degrees[i] = Math.Max(degrees[i], monomial.Exponent(i));
        for (int i = 0; i < degrees.Length; i++)
            if (degrees[i] > 0)
                yield return (_atoms[i], degrees[i]);
    }

    // The highest power of an atom in e as a polynomial, without multiplying it out: a quotient
    // needs the dividend to reach the divisor's degree in every atom.
    public static int Degree(Expr e, Expr atom) => e switch
    {
        Constant => 0,
        Add(var l, var r) => Math.Max(Degree(l, atom), Degree(r, atom)),
        Subtract(var l, var r) => Math.Max(Degree(l, atom), Degree(r, atom)),
        Negate(var a) => Degree(a, atom),
        Multiply(var l, var r) => Degree(l, atom) + Degree(r, atom),
        Divide(var n, Constant c) when !c.Value.IsZero => Degree(n, atom),
        Power(var b, Constant { Value: { IsInteger: true, Sign: > 0 } k }) when k.Numerator <= MaxPower => (int)k.Numerator * Degree(b, atom),
        _ => e.Equals(atom) ? 1 : 0,
    };

    // Whether e is multiplied out already, a sum of products of atoms: dividing such a sum
    // loses no factors that were kept apart.
    public static bool IsExpanded(Expr e) => e switch
    {
        Add(var l, var r) => IsExpanded(l) && IsExpanded(r),
        Subtract(var l, var r) => IsExpanded(l) && IsExpanded(r),
        _ => IsMonomial(e),
    };

    private static bool IsMonomial(Expr e) => e switch
    {
        Add or Subtract => false,
        Multiply(var l, var r) => IsMonomial(l) && IsMonomial(r),
        Negate(var a) => IsMonomial(a),
        Divide(var n, Constant) => IsMonomial(n),
        Power(var b, Constant { Value: { IsInteger: true, Sign: > 0 } }) => IsMonomial(b),
        _ => true,
    };

    public Expr ToExpr(Dictionary<Monomial, Rational> polynomial)
    {
        Rational constant = Rational.Zero;
        var terms = new List<(Rational, Expr)>();
        foreach (var (monomial, coefficient) in polynomial)
        {
            if (monomial.Equals(Monomial.One))
                constant += coefficient;
            else
                terms.Add((coefficient, monomial.ToExpr(_atoms)));
        }
        return Simplifier.BuildSum(terms, constant);
    }

    // The quotient where divisor divides dividend exactly, else null.
    public static Dictionary<Monomial, Rational>? ExactQuotient(Dictionary<Monomial, Rational> dividend, Dictionary<Monomial, Rational> divisor)
    {
        if (divisor.Count == 0)
            return null;
        var (leading, leadingCoefficient) = Leading(divisor);
        var rest = new Dictionary<Monomial, Rational>(dividend);
        var quotient = new Dictionary<Monomial, Rational>();
        for (int step = 0; rest.Count > 0; step++)
        {
            var (top, topCoefficient) = Leading(rest);
            if (step >= MaxDivisionSteps || !leading.Divides(top))
                return null;
            Monomial factor = top.Over(leading);
            Rational ratio = topCoefficient / leadingCoefficient;
            quotient[factor] = ratio;
            foreach (var (monomial, coefficient) in divisor)
                Accumulate(rest, monomial * factor, -ratio * coefficient);
        }
        return quotient;
    }

    private static (Monomial, Rational) Leading(Dictionary<Monomial, Rational> polynomial)
    {
        KeyValuePair<Monomial, Rational> best = default;
        bool first = true;
        foreach (var term in polynomial)
            if (first || term.Key.CompareTo(best.Key) > 0)
            {
                best = term;
                first = false;
            }
        return (best.Key, best.Value);
    }

    private static Dictionary<Monomial, Rational>? Sum(Dictionary<Monomial, Rational>? a, Dictionary<Monomial, Rational>? b, Rational sign)
    {
        if (a is null || b is null)
            return null;
        var sum = new Dictionary<Monomial, Rational>(a);
        foreach (var (monomial, coefficient) in b)
            Accumulate(sum, monomial, sign * coefficient);
        return sum.Count > MaxTerms ? null : sum;
    }

    private static Dictionary<Monomial, Rational>? Scaled(Dictionary<Monomial, Rational>? a, Rational factor)
    {
        if (a is null)
            return null;
        var scaled = new Dictionary<Monomial, Rational>();
        foreach (var (monomial, coefficient) in a)
            scaled[monomial] = coefficient * factor;
        return scaled;
    }

    private static Dictionary<Monomial, Rational>? Product(Dictionary<Monomial, Rational>? a, Dictionary<Monomial, Rational>? b)
    {
        if (a is null || b is null || (long)a.Count * b.Count > 16 * MaxTerms)
            return null;
        var product = new Dictionary<Monomial, Rational>();
        foreach (var (ma, ca) in a)
            foreach (var (mb, cb) in b)
                Accumulate(product, ma * mb, ca * cb);
        return product.Count > MaxTerms ? null : product;
    }

    private static void Accumulate(Dictionary<Monomial, Rational> polynomial, Monomial monomial, Rational coefficient)
    {
        Rational total = polynomial.GetValueOrDefault(monomial) + coefficient;
        if (total.IsZero)
            polynomial.Remove(monomial);
        else
            polynomial[monomial] = total;
    }
}

// The exponents of the atoms, atom i at index i, without trailing zeros.
internal readonly struct Monomial : IEquatable<Monomial>, IComparable<Monomial>
{
    private readonly int[] _exponents;

    private Monomial(int[] exponents) => _exponents = exponents;

    public static Monomial One { get; } = new([]);

    public static Monomial Atom(int index)
    {
        var exponents = new int[index + 1];
        exponents[index] = 1;
        return new Monomial(exponents);
    }

    private int this[int i] => i < _exponents.Length ? _exponents[i] : 0;

    public int Exponent(int atom) => this[atom];

    public static Monomial operator *(Monomial a, Monomial b)
    {
        var exponents = new int[Math.Max(a._exponents.Length, b._exponents.Length)];
        for (int i = 0; i < exponents.Length; i++)
            exponents[i] = a[i] + b[i];
        return new Monomial(exponents);
    }

    public bool Divides(Monomial m)
    {
        for (int i = 0; i < _exponents.Length; i++)
            if (_exponents[i] > m[i])
                return false;
        return true;
    }

    // this / m, where m divides this.
    public Monomial Over(Monomial m)
    {
        var exponents = new int[_exponents.Length];
        for (int i = 0; i < exponents.Length; i++)
            exponents[i] = _exponents[i] - m[i];
        int length = exponents.Length;
        while (length > 0 && exponents[length - 1] == 0)
            length--;
        return new Monomial(exponents[..length]);
    }

    public Expr ToExpr(List<Expr> atoms)
    {
        Expr? product = null;
        for (int i = 0; i < _exponents.Length; i++)
        {
            if (_exponents[i] == 0)
                continue;
            Expr factor = _exponents[i] == 1 ? atoms[i] : new Power(atoms[i], new Constant(_exponents[i]));
            product = product is null ? factor : new Multiply(product, factor);
        }
        return product ?? new Constant(1);
    }

    // Lexicographic: the higher power of the first atom comes first.
    public int CompareTo(Monomial other)
    {
        for (int i = 0; i < Math.Max(_exponents.Length, other._exponents.Length); i++)
            if (this[i] != other[i])
                return this[i].CompareTo(other[i]);
        return 0;
    }

    public bool Equals(Monomial other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is Monomial other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (int exponent in _exponents)
            hash.Add(exponent);
        return hash.ToHashCode();
    }
}
