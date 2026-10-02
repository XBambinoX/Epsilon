using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Epsilon.Core;

// Exact values of functions at special constant arguments, for Simplify: sin(pi/6) = 1/2,
// asin(1/2) = pi/6, sqrt(8) = 2sqrt(2), ln(1) = 0. Anything else stays symbolic - a value is
// never approximated - and so do points where a function is undefined (tan(pi/2), csc(0)).
internal static class ExactValues
{
    // c * sqrt(r) with r square-free (1 for a rational number): the values of the trigonometric
    // functions at multiples of pi/6 and pi/4.
    private readonly record struct Surd(Rational Coefficient, BigInteger Radicand);

    private static readonly Rational Half = new(1, 2);

    // sin(q*pi) for q in [0, 1/2].
    private static readonly (Rational Q, Surd Value)[] SinTable =
    [
        (0, new(0, 1)),
        (new(1, 6), new(Half, 1)),
        (new(1, 4), new(Half, 2)),
        (new(1, 3), new(Half, 3)),
        (Half, new(1, 1)),
    ];

    // tan(q*pi) for q in [0, 1/2); tan(pi/2) is undefined.
    private static readonly (Rational Q, Surd Value)[] TanTable =
    [
        (0, new(0, 1)),
        (new(1, 6), new(new Rational(1, 3), 3)),
        (new(1, 4), new(1, 1)),
        (new(1, 3), new(1, 3)),
    ];

    public static bool TryEvaluate(Expr expr, [NotNullWhen(true)] out Expr? value)
    {
        value = Evaluate(expr);
        return value is not null;
    }

    private static Expr? Evaluate(Expr expr) => expr switch
    {
        Sin(var a) when TryGetPiMultiple(a, out Rational q) => ToExpr(SinOfPiMultiple(q)),
        Cos(var a) when TryGetPiMultiple(a, out Rational q) => ToExpr(SinOfPiMultiple(q + Half)),
        Tan(var a) when TryGetPiMultiple(a, out Rational q) => ToExpr(TanOfPiMultiple(q)),
        Cot(var a) when TryGetPiMultiple(a, out Rational q) => ToExpr(TanOfPiMultiple(Half - q)),
        Sec(var a) when TryGetPiMultiple(a, out Rational q) => ToExpr(Reciprocal(SinOfPiMultiple(q + Half))),
        Csc(var a) when TryGetPiMultiple(a, out Rational q) => ToExpr(Reciprocal(SinOfPiMultiple(q))),

        Asin(var a) when TryGetSurd(a, out Surd v) => PiTimes(AngleOf(SinTable, v)),
        Acos(var a) when TryGetSurd(a, out Surd v) && AngleOf(SinTable, v) is Rational q => PiTimes(Half - q),
        Atan(var a) when TryGetSurd(a, out Surd v) => PiTimes(AngleOf(TanTable, v)),

        Sinh(Constant { Value.IsZero: true }) => new Constant(0),
        Tanh(Constant { Value.IsZero: true }) => new Constant(0),
        Asinh(Constant { Value.IsZero: true }) => new Constant(0),
        Atanh(Constant { Value.IsZero: true }) => new Constant(0),
        Cosh(Constant { Value.IsZero: true }) => new Constant(1),
        Sech(Constant { Value.IsZero: true }) => new Constant(1),
        Acosh(Constant { Value.IsOne: true }) => new Constant(0),
        Exp(Constant { Value.IsZero: true }) => new Constant(1),
        Ln(Constant { Value.IsOne: true }) => new Constant(0),
        Ln(EulerNumber) => new Constant(1),

        NthRoot(Constant c, Constant n) when IsRootDegree(n.Value, out int degree) => ReducedNthRoot(c.Value, degree),

        // Square roots of rationals and arithmetic with them, in one form: sqrt(8) = 2sqrt(2),
        // sqrt(1/2) = sqrt(2)/2, 2sqrt(3)/2 = sqrt(3), sqrt(2)*sqrt(3) = sqrt(6),
        // 1/(sqrt(2)/2) = sqrt(2), sin(pi/4)^2 = (sqrt(2)/2)^2 = 1/2.
        _ when TryGetSurd(expr, out Surd s) && ToExpr(s) is Expr normal && !normal.Equals(expr) => normal,

        _ => null
    };

    // ---- Trigonometric functions at q*pi ----

    // 0, pi, -pi/6, 2*pi/3: the forms Simplify gives rational multiples of pi.
    private static bool TryGetPiMultiple(Expr expr, out Rational q)
    {
        switch (expr)
        {
            case Constant c when c.Value.IsZero:
                q = Rational.Zero;
                return true;
            case Pi:
                q = Rational.One;
                return true;
            case Negate(var a) when TryGetPiMultiple(a, out q):
                q = -q;
                return true;
            case Multiply(Constant c, var a) when TryGetPiMultiple(a, out q):
                q *= c.Value;
                return true;
            case Divide(var a, Constant c) when !c.Value.IsZero && TryGetPiMultiple(a, out q):
                q /= c.Value;
                return true;
            default:
                q = Rational.Zero;
                return false;
        }
    }

    private static Surd? SinOfPiMultiple(Rational q)
    {
        q = Mod(q, 2);
        bool negate = q >= 1;           // sin(x + pi) = -sin(x)
        if (negate)
            q -= 1;
        if (q > Half)                   // sin(pi - x) = sin(x)
            q = 1 - q;

        return Negated(ValueAt(SinTable, q), negate);
    }

    private static Surd? TanOfPiMultiple(Rational q)
    {
        q = Mod(q, 1);
        bool negate = q > Half;         // tan(pi - x) = -tan(x)
        if (negate)
            q = 1 - q;

        return Negated(ValueAt(TanTable, q), negate);
    }

    private static Rational Mod(Rational q, int period) => q - period * (q / period).Floor();

    private static Surd? ValueAt((Rational Q, Surd Value)[] table, Rational q)
    {
        foreach (var entry in table)
            if (entry.Q == q)
                return entry.Value;
        return null;
    }

    // The inverse lookup, for asin, acos and atan; odd like asin and atan.
    private static Rational? AngleOf((Rational Q, Surd Value)[] table, Surd value)
    {
        Rational magnitude = value.Coefficient.Abs();
        foreach (var (q, entry) in table)
            if (entry.Coefficient == magnitude && entry.Radicand == value.Radicand)
                return value.Coefficient.Sign < 0 ? -q : q;
        return null;
    }

    private static Surd? Negated(Surd? value, bool negate) =>
        negate && value is Surd v ? v with { Coefficient = -v.Coefficient } : value;

    // 1 / (c*sqrt(r)) = sqrt(r) / (c*r); null for 0, where sec and csc are undefined.
    private static Surd? Reciprocal(Surd? value) =>
        value is Surd v && !v.Coefficient.IsZero
            ? v with { Coefficient = Rational.One / (v.Coefficient * new Rational(v.Radicand)) }
            : null;

    // c1*sqrt(r1) * c2*sqrt(r2) = c1*c2*sqrt(r1*r2), with the square factors of r1*r2 taken out.
    private static Surd Product(Surd a, Surd b)
    {
        Surd root = SqrtOf(new Rational(a.Radicand * b.Radicand));
        return root with { Coefficient = a.Coefficient * b.Coefficient * root.Coefficient };
    }

    // (c*sqrt(r))^n = c^n * r^(n/2), times sqrt(r) for an odd n; null for 1/0.
    private static Surd? Pow(Surd value, int n)
    {
        if (n < 0)
            return Reciprocal(Pow(value, -n));

        Rational coefficient = value.Coefficient.Pow(n) * new Rational(BigInteger.Pow(value.Radicand, n / 2));
        return new Surd(coefficient, n % 2 == 0 ? BigInteger.One : value.Radicand);
    }

    // sqrt(p/q) = sqrt(p*q)/q with the square factors of p*q taken out: sqrt(8) = 2sqrt(2),
    // sqrt(1/2) = sqrt(2)/2, sqrt(9/4) = 3/2.
    private static Surd SqrtOf(Rational value)
    {
        var (outside, radicand) = ExtractPowers(value.Numerator * value.Denominator, 2);
        return new Surd(new Rational(outside, value.Denominator), radicand);
    }

    // Products, quotients and integer powers of rational numbers and square roots of positive
    // rational numbers, such as sqrt(3)/2, -sqrt(2)/2, 1/sqrt(3) or (2sqrt(2))^3.
    private static bool TryGetSurd(Expr expr, out Surd value)
    {
        switch (expr)
        {
            case Constant c:
                value = new Surd(c.Value, BigInteger.One);
                return true;
            case Sqrt(Constant c) when c.Value.Sign > 0:
                value = SqrtOf(c.Value);
                return true;
            case Negate(var a) when TryGetSurd(a, out value):
                value = value with { Coefficient = -value.Coefficient };
                return true;
            case Multiply(var a, var b) when TryGetSurd(a, out Surd x) && TryGetSurd(b, out Surd y):
                value = Product(x, y);
                return true;
            case Divide(var a, var b) when TryGetSurd(a, out Surd x) && TryGetSurd(b, out Surd y) && Reciprocal(y) is Surd inverse:
                value = Product(x, inverse);
                return true;
            case Power(var b, Constant k) when IsInt(k.Value, out int n) && TryGetSurd(b, out Surd x) && Pow(x, n) is Surd power:
                value = power;
                return true;
            default:
                value = default;
                return false;
        }
    }

    private static Expr? ToExpr(Surd? value) =>
        value is Surd v
            ? Times(v.Coefficient, v.Radicand.IsOne ? null : new Sqrt(new Constant(new Rational(v.Radicand))))
            : null;

    private static Expr? PiTimes(Rational? q) => q is Rational value ? Times(value, new Pi()) : null;

    // c * atom written as Simplify writes it: 2sqrt(2), sqrt(2) / 2, -sqrt(3) / 2, 2 * pi / 3.
    private static Expr Times(Rational coefficient, Expr? atom)
    {
        if (atom is null || coefficient.IsZero)
            return new Constant(coefficient);

        BigInteger numerator = coefficient.Numerator;
        Expr top = numerator.IsOne ? atom
            : numerator == BigInteger.MinusOne ? new Negate(atom)
            : new Multiply(new Constant(new Rational(numerator)), atom);

        return coefficient.IsInteger ? top : new Divide(top, new Constant(new Rational(coefficient.Denominator)));
    }

    // ---- Roots of rational numbers ----

    // Root degrees taken apart: integers from 2 up to this. Higher ones are left alone.
    private const int MaxRootDegree = 64;

    // Square and higher factors are found by trial division up to this, so a factor p^n with a
    // larger prime p stays inside the root. The result is exact either way.
    private const int MaxTrialDivisor = 100_000;

    // nthroot(a/b, n) with the n-th powers in a and b taken out: nthroot(16, 3) = 2nthroot(2, 3),
    // nthroot(-16, 3) = -2nthroot(2, 3). Unlike sqrt, the denominator is not moved into the root,
    // which would make its number n - 1 times longer. Null if there is nothing to take out.
    private static Expr? ReducedNthRoot(Rational value, int degree)
    {
        if (value.IsZero || (value.Sign < 0 && degree % 2 == 0))
            return null; // nthroot(0, n) is folded elsewhere; an even root of a negative number is undefined

        var (numeratorOutside, numeratorRest) = ExtractPowers(BigInteger.Abs(value.Numerator), degree);
        var (denominatorOutside, denominatorRest) = ExtractPowers(value.Denominator, degree);
        if (numeratorOutside.IsOne && denominatorOutside.IsOne)
            return null;

        Rational radicand = new(numeratorRest, denominatorRest);
        Rational outside = new Rational(numeratorOutside, denominatorOutside) * value.Sign;
        Expr? root = radicand.IsOne ? null : new NthRoot(new Constant(radicand), new Constant(degree));
        return Times(outside, root);
    }

    private static bool IsRootDegree(Rational value, out int degree) =>
        IsInt(value, out degree) && degree >= 2 && degree <= MaxRootDegree;

    private static bool IsInt(Rational value, out int n)
    {
        bool fits = value.IsInteger && value.Numerator >= int.MinValue + 1 && value.Numerator <= int.MaxValue;
        n = fits ? (int)value.Numerator : 0;
        return fits;
    }

    // m = outside^n * rest, with the n-th powers of primes up to MaxTrialDivisor moved to Outside.
    private static (BigInteger Outside, BigInteger Remainder) ExtractPowers(BigInteger m, int n)
    {
        BigInteger outside = BigInteger.One;

        for (int p = 2; p <= MaxTrialDivisor; p = p == 2 ? 3 : p + 2)
        {
            BigInteger power = BigInteger.Pow(p, n);
            if (power > m)
                break;

            while (m % power == 0)
            {
                m /= power;
                outside *= p;
            }
        }

        return (outside, m);
    }
}
