using System.Numerics;

namespace Epsilon.Core;

// A number a + b sqrt(d) for rationals a, b and an integer d > 1 without square factors, kept
// exact: 1/(sqrt(3) + 2) = 2 - sqrt(3) by the conjugate, and the sign of 1 - sqrt(2) is known.
internal readonly record struct QuadraticSurd(Rational A, Rational B, BigInteger D)
{
    private const int MaxExponent = 16;

    // Null for anything that isn't such a number, or has square roots of two different numbers.
    public static QuadraticSurd? Of(Expr e) => e switch
    {
        Constant c => new QuadraticSurd(c.Value, 0, 1),
        Sqrt(Constant { Value.Sign: > 0 } c) => Root(c.Value),
        Negate(var a) => Of(a) is { } x ? -x : null,
        Add(var l, var r) => Combine(l, r, (x, y) => x + y),
        Subtract(var l, var r) => Combine(l, r, (x, y) => x - y),
        Multiply(var l, var r) => Combine(l, r, (x, y) => x * y),
        Divide(var n, var d) => Combine(n, d, (x, y) => x / y),
        Power(var b, Constant { Value: { IsInteger: true } k }) when k.Abs() <= MaxExponent && Of(b) is { } x =>
            Raise(x, (int)k.Numerator),
        _ => null,
    };

    public bool IsZero => A.IsZero && B.IsZero;

    // Exact: a and b sqrt(d) of opposite signs compare as a^2 and b^2 d, which are never equal.
    public int Sign =>
        B.IsZero ? A.Sign
        : A.IsZero || A.Sign == B.Sign ? B.Sign
        : A * A > B * B * new Rational(D) ? A.Sign : B.Sign;

    public QuadraticSurd Abs => Sign < 0 ? -this : this;

    // Over a common denominator, (40 - 4sqrt(3))/97, as Simplify writes it.
    public Expr ToExpr()
    {
        if (B.IsZero)
            return new Constant(A);

        BigInteger denominator = A.Denominator / BigInteger.GreatestCommonDivisor(A.Denominator, B.Denominator) * B.Denominator;
        var scale = new Rational(denominator);
        Rational b = B * scale;
        Expr sqrt = new Sqrt(new Constant(new Rational(D)));
        Expr root = b.IsOne ? sqrt : b == Rational.MinusOne ? new Negate(sqrt) : new Multiply(new Constant(b), sqrt);
        Expr numerator = A.IsZero ? root : new Add(new Constant(A * scale), root);
        return denominator.IsOne ? numerator : new Divide(numerator, new Constant(scale));
    }

    public QuadraticSurd Times(Rational c) => new(A * c, B * c, D);

    public static QuadraticSurd operator -(QuadraticSurd x) => new(-x.A, -x.B, x.D);

    public static QuadraticSurd? operator +(QuadraticSurd x, QuadraticSurd y) =>
        Common(x, y) is { } d ? new QuadraticSurd(x.A + y.A, x.B + y.B, d) : null;

    public static QuadraticSurd? operator -(QuadraticSurd x, QuadraticSurd y) => x + -y;

    public static QuadraticSurd? operator *(QuadraticSurd x, QuadraticSurd y) =>
        Common(x, y) is { } d ? new QuadraticSurd(x.A * y.A + x.B * y.B * new Rational(d), x.A * y.B + x.B * y.A, d) : null;

    // (a + b sqrt(d))/(c + e sqrt(d)) = (a + b sqrt(d))(c - e sqrt(d))/(c^2 - e^2 d).
    public static QuadraticSurd? operator /(QuadraticSurd x, QuadraticSurd y)
    {
        if (y.IsZero || Common(x, y) is not { } d || x * new QuadraticSurd(y.A, -y.B, d) is not { } numerator)
            return null;

        Rational norm = y.A * y.A - y.B * y.B * new Rational(d);
        return new QuadraticSurd(numerator.A / norm, numerator.B / norm, d);
    }

    private static QuadraticSurd? Raise(QuadraticSurd x, int k)
    {
        QuadraticSurd? result = new QuadraticSurd(1, 0, 1);
        for (int i = 0; i < Math.Abs(k) && result is { } product; i++)
            result = product * x;

        return k >= 0 ? result : result is { } power ? new QuadraticSurd(1, 0, 1) / power : null;
    }

    // The radicand both share, where that of a rational number is any.
    private static BigInteger? Common(QuadraticSurd x, QuadraticSurd y) =>
        x.B.IsZero ? y.D : y.B.IsZero || x.D == y.D ? x.D : null;

    // The right side first: in a long sum it is one term, so a variable there ends the walk at once.
    private static QuadraticSurd? Combine(Expr left, Expr right, Func<QuadraticSurd, QuadraticSurd, QuadraticSurd?> operation) =>
        Of(right) is { } y && Of(left) is { } x ? operation(x, y) : null;

    // sqrt(u/v) = sqrt(u v)/v, with the square factors of u v taken out.
    private static QuadraticSurd Root(Rational n)
    {
        var (outside, rest) = ExactValues.ExtractPowers(n.Numerator * n.Denominator, 2);
        var factor = new Rational(outside, n.Denominator);
        return rest.IsOne ? new QuadraticSurd(factor, 0, 1) : new QuadraticSurd(0, factor, rest);
    }
}
