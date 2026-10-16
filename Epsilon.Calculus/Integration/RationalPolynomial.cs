using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed class RationalPolynomial
{
    public const int MaxDegree = 64;
    private const long MaxDivided = 1_000_000;
    private const long MaxCandidates = 20_000;

    private readonly Rational[] _coefficients;

    private RationalPolynomial(params Rational[] coefficients)
    {
        int length = coefficients.Length;
        while (length > 1 && coefficients[length - 1].IsZero)
            length--;
        _coefficients = length == 0 ? [Rational.Zero] : coefficients[..length];
    }

    public static RationalPolynomial Zero { get; } = new(Rational.Zero);
    public static RationalPolynomial One { get; } = new(Rational.One);

    public int Degree => _coefficients.Length - 1;
    public Rational Leading => _coefficients[^1];
    public bool IsZero => _coefficients is [{ IsZero: true }];
    public Rational this[int k] => k < _coefficients.Length ? _coefficients[k] : Rational.Zero;

    private static RationalPolynomial Linear(Rational root) => new(-root, 1);

    public static RationalPolynomial? Of(Expr e, string variable) => e switch
    {
        Constant c => new(c.Value),
        Variable v => v.Name == variable ? new(0, 1) : null,
        Add sum => Of(sum.Left, variable) is { } left && Of(sum.Right, variable) is { } right ? left + right : null,
        Subtract difference => Of(difference.Left, variable) is { } left && Of(difference.Right, variable) is { } right ? left - right : null,
        Multiply product => Of(product.Left, variable) is { } left && Of(product.Right, variable) is { } right ? left * right : null,
        Negate negate => Of(negate.Argument, variable) is { } p ? -p : null,
        Divide quotient => Of(quotient.Numerator, variable) is { } n && Of(quotient.Denominator, variable) is { Degree: 0, IsZero: false } d
            ? n * (1 / d.Leading) : null,
        Power { Exponent: Constant { Value: { IsInteger: true, Sign: >= 0 } n } } power when n <= MaxDegree =>
            Of(power.Base, variable) is { } b && b.Degree * (int)n.Numerator <= MaxDegree ? b.Pow((int)n.Numerator) : null,
        _ => null,
    };

    public static RationalPolynomial operator +(RationalPolynomial a, RationalPolynomial b) =>
        new([.. Enumerable.Range(0, Math.Max(a._coefficients.Length, b._coefficients.Length)).Select(k => a[k] + b[k])]);

    public static RationalPolynomial operator -(RationalPolynomial a, RationalPolynomial b) => a + -b;

    public static RationalPolynomial operator -(RationalPolynomial a) => a * Rational.MinusOne;

    public static RationalPolynomial operator *(RationalPolynomial a, Rational c) => new([.. a._coefficients.Select(coefficient => coefficient * c)]);

    public static RationalPolynomial operator *(RationalPolynomial a, RationalPolynomial b)
    {
        var product = new Rational[a._coefficients.Length + b._coefficients.Length - 1];
        for (int i = 0; i < a._coefficients.Length; i++)
            for (int j = 0; j < b._coefficients.Length; j++)
                product[i + j] += a._coefficients[i] * b._coefficients[j];

        return new(product);
    }

    public static RationalPolynomial operator /(RationalPolynomial a, RationalPolynomial b) => a.DivRem(b).Quotient;

    public RationalPolynomial Pow(int n) => Enumerable.Repeat(this, n).Aggregate(One, (product, factor) => product * factor);

    public (RationalPolynomial Quotient, RationalPolynomial Remainder) DivRem(RationalPolynomial divisor)
    {
        int degree = divisor.Degree;
        if (Degree < degree)
            return (Zero, this);

        var remainder = (Rational[])_coefficients.Clone();
        var quotient = new Rational[Degree - degree + 1];
        for (int k = quotient.Length - 1; k >= 0; k--)
        {
            quotient[k] = remainder[k + degree] / divisor.Leading;
            for (int j = 0; j <= degree; j++)
                remainder[k + j] -= quotient[k] * divisor[j];
        }

        return (new(quotient), new(remainder[..degree]));
    }

    // s with s * this = 1 modulo m, by the extended Euclidean algorithm; this and m are coprime.
    public RationalPolynomial InverseModulo(RationalPolynomial m)
    {
        RationalPolynomial r0 = m, r1 = DivRem(m).Remainder, s0 = Zero, s1 = One;
        while (!r1.IsZero)
        {
            var (quotient, remainder) = r0.DivRem(r1);
            (r0, r1) = (r1, remainder);
            (s0, s1) = (s1, s0 - quotient * s1);
        }

        return (s0 * (1 / r0.Leading)).DivRem(m).Remainder;
    }

    public Rational Evaluate(Rational x)
    {
        Rational value = Rational.Zero;
        for (int k = Degree; k >= 0; k--)
            value = value * x + _coefficients[k];

        return value;
    }

    public RationalPolynomial Monic() => this * (1 / Leading);

    // For a monic polynomial, the multiple with coprime integer coefficients: 2x + 1 for x + 1/2.
    public RationalPolynomial Primitive()
    {
        Rational multiple = Rational.One;
        foreach (Rational c in _coefficients)
            multiple *= new Rational((c * multiple).Denominator);

        return this * multiple;
    }

    public RationalPolynomial Derivative() => new([.. _coefficients.Skip(1).Select((c, k) => c * (k + 1))]);

    public RationalPolynomial Antiderivative() => new([0, .. _coefficients.Select((c, k) => c / (k + 1))]);

    public Expr ToExpr(Expr x) => _coefficients.Select((c, k) => c * new Power(x, k)).Aggregate((sum, term) => sum + term).Simplify();

    // Monic factors with their multiplicities: x - r for each rational root r, quadratics with
    // rational coefficients, and what is left of each squarefree part, as x^3 - 2.
    public List<(RationalPolynomial Factor, int Multiplicity)> Factor()
    {
        var factors = new List<(RationalPolynomial Factor, int Multiplicity)>();
        int multiplicity = 0;
        foreach (RationalPolynomial part in SquareFree())
        {
            multiplicity++;
            RationalPolynomial rest = part;
            foreach (Rational root in part.RationalRoots())
            {
                factors.Add((Linear(root), multiplicity));
                rest /= Linear(root);
            }
            while (rest.Degree > 2 && rest.QuadraticFactor() is { } quadratic)
            {
                factors.Add((quadratic, multiplicity));
                rest /= quadratic;
            }
            if (rest.Degree > 0)
                factors.Add((rest, multiplicity));
        }

        return factors;
    }

    // Yun's algorithm: the monic polynomial is D1 D2^2 D3^3 ... with each Di squarefree and coprime to the others.
    private List<RationalPolynomial> SquareFree()
    {
        var parts = new List<RationalPolynomial>();
        RationalPolynomial derivative = Derivative(), common = Gcd(this, derivative);
        RationalPolynomial b = Monic() / common, c = derivative * (1 / Leading) / common, d = c - b.Derivative();
        while (b.Degree > 0)
        {
            RationalPolynomial part = Gcd(b, d);
            parts.Add(part);
            (b, c) = (b / part, d / part);
            d = c - b.Derivative();
        }

        return parts;
    }

    public static RationalPolynomial Gcd(RationalPolynomial a, RationalPolynomial b)
    {
        while (!b.IsZero)
            (a, b) = (b, a.DivRem(b).Remainder);

        return a.Monic();
    }

    // By the rational root theorem, on the coefficients made integers; each root once.
    public IEnumerable<Rational> RationalRoots()
    {
        Rational[] integers = Monic().Primitive()._coefficients;
        int lowest = Array.FindIndex(integers, c => !c.IsZero);
        if (lowest > 0)
            yield return Rational.Zero;
        if (Divisors(integers[lowest]) is not { } numerators || Divisors(integers[^1]) is not { } denominators)
            yield break;

        var candidates = from p in numerators from q in denominators from sign in new[] { 1, -1 } select new Rational(sign * p, q);
        foreach (Rational candidate in candidates.Distinct())
            if (Evaluate(candidate).IsZero)
                yield return candidate;
    }

    private static List<long>? Divisors(Rational integer)
    {
        var magnitude = integer.Abs().Numerator;
        if (magnitude > MaxDivided)
            return null;

        long n = (long)magnitude;
        var divisors = new List<long>();
        for (long d = 1; d * d <= n; d++)
            if (n % d == 0)
            {
                divisors.Add(d);
                if (d * d != n)
                    divisors.Add(n / d);
            }

        return divisors;
    }

    // By Kronecker's method: a factor with integer coefficients takes values at -1, 0 and 1 that
    // divide the polynomial's there, and three values fix a quadratic.
    private RationalPolynomial? QuadraticFactor()
    {
        RationalPolynomial integral = Monic().Primitive();
        if (Divisors(integral.Evaluate(-1)) is not { } below || Divisors(integral.Evaluate(0)) is not { } at
            || Divisors(integral.Evaluate(1)) is not { } above || 4L * below.Count * at.Count * above.Count > MaxCandidates)
            return null;

        foreach (long c in at)
            foreach (long m in below.SelectMany(v => new[] { v, -v }))
                foreach (long p in above.SelectMany(v => new[] { v, -v }))
                {
                    if ((m + p) % 2 != 0 || (m + p) / 2 == c)
                        continue;

                    var g = new RationalPolynomial(c, (p - m) / 2, (m + p) / 2 - c);
                    if (integral.DivRem(g).Remainder.IsZero)
                        return g.Monic();
                }

        return null;
    }
}
