using Epsilon.Core;

namespace Epsilon.Calculus;

// A number a + b sqrt(d) for rationals a and b and a squarefree integer d > 1, kept exact, with
// division by the conjugate: the simplifier leaves 1/(sqrt(3) + 2) as it is, which is 2 - sqrt(3).
internal readonly record struct QuadraticSurd(Rational A, Rational B, long D)
{
    private const long MaxRadicand = 1_000_000_000_000;
    private const int MaxExponent = 16;

    public static QuadraticSurd? Of(Expr e) => e switch
    {
        Constant c => new QuadraticSurd(c.Value, 0, 1),
        Sqrt { Argument: Constant { Value: { Sign: > 0 } n } } => Root(n),
        Negate negate => Of(negate.Argument) is { } x ? -x : null,
        Add sum => Combine(sum.Left, sum.Right, (x, y) => x + y),
        Subtract difference => Combine(difference.Left, difference.Right, (x, y) => x - y),
        Multiply product => Combine(product.Left, product.Right, (x, y) => x * y),
        Divide quotient => Combine(quotient.Numerator, quotient.Denominator, (x, y) => y.IsZero ? null : x / y),
        Power { Exponent: Constant { Value: { IsInteger: true } k } } power when k.Abs() <= MaxExponent && Of(power.Base) is { } x =>
            Raise(x, (int)k.Numerator),
        _ => null,
    };

    private static QuadraticSurd One => new(1, 0, 1);

    private static QuadraticSurd? Raise(QuadraticSurd x, int k)
    {
        QuadraticSurd? result = One;
        for (int i = 0; i < Math.Abs(k) && result is { } product; i++)
            result = product * x;

        return k >= 0 ? result : result is { IsZero: false } positive ? One / positive : null;
    }

    public bool IsZero => A.IsZero && B.IsZero;

    // Over a common denominator, (40 - 4 sqrt(3))/97: the simplifier prints a sum with a
    // negative fraction before the root as -4 * (1/97) * sqrt(3) + 40/97.
    public Expr ToExpr()
    {
        if (B.IsZero)
            return new Constant(A);

        System.Numerics.BigInteger denominator = A.Denominator / System.Numerics.BigInteger.GreatestCommonDivisor(A.Denominator, B.Denominator) * B.Denominator;
        Expr numerator = new Constant(A * new Rational(denominator)) + new Constant(B * new Rational(denominator)) * new Sqrt(D);
        return (denominator.IsOne ? numerator : numerator / new Constant(new Rational(denominator))).Simplify();
    }

    public static QuadraticSurd operator -(QuadraticSurd x) => new(-x.A, -x.B, x.D);

    public static QuadraticSurd? operator +(QuadraticSurd x, QuadraticSurd y) =>
        Common(x, y) is { } d ? new QuadraticSurd(x.A + y.A, x.B + y.B, d) : null;

    public static QuadraticSurd? operator -(QuadraticSurd x, QuadraticSurd y) => x + -y;

    public static QuadraticSurd? operator *(QuadraticSurd x, QuadraticSurd y) =>
        Common(x, y) is { } d ? new QuadraticSurd(x.A * y.A + x.B * y.B * d, x.A * y.B + x.B * y.A, d) : null;

    // (a + b sqrt(d))/(c + e sqrt(d)) = (a + b sqrt(d))(c - e sqrt(d))/(c^2 - e^2 d).
    public static QuadraticSurd? operator /(QuadraticSurd x, QuadraticSurd y)
    {
        if (Common(x, y) is not { } d || x * new QuadraticSurd(y.A, -y.B, d) is not { } numerator)
            return null;

        Rational norm = y.A * y.A - y.B * y.B * d;
        return new QuadraticSurd(numerator.A / norm, numerator.B / norm, d);
    }

    // The radicand both share, where the one of a rational is any.
    private static long? Common(QuadraticSurd x, QuadraticSurd y) =>
        x.B.IsZero ? y.D : y.B.IsZero || x.D == y.D ? x.D : null;

    private static QuadraticSurd? Combine(Expr left, Expr right, Func<QuadraticSurd, QuadraticSurd, QuadraticSurd?> operation) =>
        Of(left) is { } x && Of(right) is { } y ? operation(x, y) : null;

    // sqrt(u/v) = sqrt(u v)/v, with the square factors of u v taken out.
    private static QuadraticSurd? Root(Rational n)
    {
        System.Numerics.BigInteger product = n.Numerator * n.Denominator;
        if (product > MaxRadicand)
            return null;

        long rest = (long)product, outside = 1;
        for (long k = 2; k * k <= rest; k++)
            while (rest % (k * k) == 0)
            {
                rest /= k * k;
                outside *= k;
            }

        var factor = new Rational(outside, n.Denominator);
        return rest == 1 ? new QuadraticSurd(factor, 0, 1) : new QuadraticSurd(0, factor, rest);
    }
}
