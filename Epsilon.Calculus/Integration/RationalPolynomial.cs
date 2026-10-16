using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed class RationalPolynomial
{
    public const int MaxDegree = 64;
    private const long MaxDivided = 1_000_000;
    private const long MaxCandidates = 20_000;

    private readonly Rational[] _coefficients;

    public RationalPolynomial(params Rational[] coefficients)
    {
        int length = coefficients.Length;
        while (length > 1 && coefficients[length - 1].IsZero)
            length--;
        _coefficients = length == 0 ? [Rational.Zero] : coefficients[..length];
    }

    public static RationalPolynomial Zero { get; } = new(Rational.Zero);
    public static RationalPolynomial One { get; } = new(Rational.One);
    private static RationalPolynomial X { get; } = new(0, 1);

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

    // A rational function as n/d in lowest terms with a monic d: 1/(t^2/(1 + t^2) + 1) is (t^2 + 1)/(2t^2 + 1).
    public static (RationalPolynomial N, RationalPolynomial D)? Fraction(Expr e, string variable)
    {
        if (Of(e, variable) is { } p)
            return (p, One);

        (RationalPolynomial N, RationalPolynomial D)? Part(Expr child) => Fraction(child, variable);
        (RationalPolynomial N, RationalPolynomial D)? fraction = e switch
        {
            Add sum when Part(sum.Left) is var (n1, d1) && Part(sum.Right) is var (n2, d2) => (n1 * d2 + n2 * d1, d1 * d2),
            Subtract difference when Part(difference.Left) is var (n1, d1) && Part(difference.Right) is var (n2, d2) => (n1 * d2 - n2 * d1, d1 * d2),
            Multiply product when Part(product.Left) is var (n1, d1) && Part(product.Right) is var (n2, d2) => (n1 * n2, d1 * d2),
            Divide quotient when Part(quotient.Numerator) is var (n1, d1) && Part(quotient.Denominator) is var (n2, d2) && !n2.IsZero => (n1 * d2, d1 * n2),
            Negate negate when Part(negate.Argument) is var (n, d) => (-n, d),
            Power { Exponent: Constant { Value: { IsInteger: true } k } } power when k.Abs() <= MaxDegree && Part(power.Base) is var (n, d) && (k.Sign >= 0 || !n.IsZero) =>
                k.Sign >= 0 ? (n.Pow((int)k.Numerator), d.Pow((int)k.Numerator)) : (d.Pow((int)-k.Numerator), n.Pow((int)-k.Numerator)),
            _ => null,
        };
        if (fraction is not var (numerator, denominator) || numerator.Degree > MaxDegree || denominator.Degree > MaxDegree)
            return null;

        RationalPolynomial common = Gcd(numerator, denominator);
        (numerator, denominator) = (numerator / common, denominator / common);
        return (numerator * (1 / denominator.Leading), denominator.Monic());
    }

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

    public (double Re, double Im) Evaluate(double re, double im)
    {
        double valueRe = 0, valueIm = 0;
        for (int k = Degree; k >= 0; k--)
            (valueRe, valueIm) = (valueRe * re - valueIm * im + _coefficients[k].ToDouble(), valueRe * im + valueIm * re);

        return (valueRe, valueIm);
    }

    // p(x - h).
    public RationalPolynomial Shift(Rational h) => Enumerable.Reverse(_coefficients).Aggregate(Zero, (value, c) => value * Linear(h) + new RationalPolynomial(c));

    // The least m from the degree up to which x^m is a nonzero constant a modulo this monic
    // polynomial, which then divides x^m - a.
    public (int Exponent, Rational Value)? BinomialMultiple()
    {
        RationalPolynomial power = X.Pow(Degree).DivRem(this).Remainder;
        for (int m = Degree; m <= 6 * Degree; m++)
        {
            if (power.Degree == 0 && !power.IsZero)
                return (m, power[0]);
            power = (power * X).DivRem(this).Remainder;
        }

        return null;
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
                factors.AddRange(rest.InPowers().Select(piece => (piece, multiplicity)));
        }

        return factors;
    }

    // g(x^k) splits as g does, each factor in x^k: x^6 - 5x^3 + 6 is (x^3 - 2)(x^3 - 3).
    private IEnumerable<RationalPolynomial> InPowers()
    {
        int k = Enumerable.Range(1, Degree).Where(i => !_coefficients[i].IsZero).Aggregate(0, Gcd);
        if (k < 2)
            return [this];

        var g = new RationalPolynomial([.. _coefficients.Where((_, i) => i % k == 0)]);
        return g.Factor().Select(factor => new RationalPolynomial([.. Enumerable.Range(0, factor.Factor.Degree * k + 1)
            .Select(i => i % k == 0 ? factor.Factor[i / k] : Rational.Zero)]));

        static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
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
