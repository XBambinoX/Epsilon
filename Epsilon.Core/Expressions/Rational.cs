using System.Numerics;

namespace Epsilon.Core;

public readonly struct Rational : IEquatable<Rational>, IComparable<Rational>
{
    public BigInteger Numerator { get; }
    public BigInteger Denominator { get; }

    public static readonly Rational Zero = new(0, 1);
    public static readonly Rational One = new(1, 1);
    public static readonly Rational MinusOne = new(-1, 1);

    public Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
            throw new DivideByZeroException("Rational denominator cannot be zero.");

        // Normalization
        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        BigInteger gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        if (gcd.IsZero) gcd = 1;

        Numerator = numerator / gcd;
        Denominator = denominator / gcd;
    }

    public Rational(BigInteger integer) : this(integer, 1) { }

    public bool IsZero => Numerator.IsZero;
    public bool IsOne => Numerator == Denominator;
    public bool IsInteger => Denominator.IsOne;
    public int Sign => Numerator.Sign;

    // Round
    public Rational Floor()
    {
        BigInteger q = BigInteger.DivRem(Numerator, Denominator, out BigInteger r);
        if (r != 0 && Numerator.Sign < 0) q -= 1;
        return new Rational(q);
    }

    public Rational Ceiling()
    {
        BigInteger q = BigInteger.DivRem(Numerator, Denominator, out BigInteger r);
        if (r != 0 && Numerator.Sign > 0) q += 1;
        return new Rational(q);
    }

    public Rational Round()
    {
        Rational doubled = new Rational(Numerator * 2, Denominator);
        BigInteger q = BigInteger.DivRem(doubled.Numerator, doubled.Denominator, out BigInteger r);
        BigInteger half = BigInteger.DivRem(q, 2, out BigInteger rem);
        if (rem != 0)
            half += Numerator.Sign >= 0 ? 1 : -1;
        return new Rational(half);
    }

    public Rational Abs() => Numerator.Sign < 0 ? new Rational(-Numerator, Denominator) : this;

    public Rational Pow(int exponent)
    {
        if (exponent == 0) return One;

        if (exponent > 0)
            return new Rational(BigInteger.Pow(Numerator, exponent), BigInteger.Pow(Denominator, exponent));

        if (IsZero)
            throw new DivideByZeroException("Cannot raise zero to a negative power.");

        return new Rational(BigInteger.Pow(Denominator, -exponent), BigInteger.Pow(Numerator, -exponent));
    }

    public double ToDouble() => (double)Numerator / (double)Denominator;

    public static implicit operator Rational(int value) => new(value);
    public static implicit operator Rational(long value) => new(value);
    public static explicit operator Rational(double value) => FromDouble(value);

    public static Rational FromDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException($"Cannot represent {value} as a Rational.");

        string text = value.ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
        return FromDecimalString(text);
    }

    public static Rational FromDecimalString(string text)
    {
        bool negative = text.StartsWith('-');
        if (negative) text = text[1..];

        int dotIndex = text.IndexOf('.');
        if (dotIndex < 0)
        {
            BigInteger intPart = BigInteger.Parse(text);
            return new Rational(negative ? -intPart : intPart);
        }

        string digits = text.Remove(dotIndex, 1);
        int fractionalDigits = text.Length - dotIndex - 1;

        BigInteger numerator = BigInteger.Parse(digits);
        BigInteger denominator = BigInteger.Pow(10, fractionalDigits);

        if (negative) numerator = -numerator;

        return new Rational(numerator, denominator);
    }

    public static Rational operator +(Rational a, Rational b) =>
        new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);

    public static Rational operator -(Rational a, Rational b) =>
        new(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);

    public static Rational operator -(Rational a) => new(-a.Numerator, a.Denominator);

    public static Rational operator *(Rational a, Rational b) =>
        new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);

    public static Rational operator /(Rational a, Rational b)
    {
        if (b.IsZero) throw new DivideByZeroException("Division by zero.");
        return new Rational(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
    }

    public static bool operator ==(Rational a, Rational b) => a.Equals(b);
    public static bool operator !=(Rational a, Rational b) => !a.Equals(b);
    public static bool operator <(Rational a, Rational b) => (a - b).Sign < 0;
    public static bool operator >(Rational a, Rational b) => (a - b).Sign > 0;
    public static bool operator <=(Rational a, Rational b) => (a - b).Sign <= 0;
    public static bool operator >=(Rational a, Rational b) => (a - b).Sign >= 0;

    public bool Equals(Rational other) => Numerator == other.Numerator && Denominator == other.Denominator;
    public override bool Equals(object? obj) => obj is Rational other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);
    public int CompareTo(Rational other) => (this - other).Sign;

    public override string ToString() => IsInteger ? Numerator.ToString() : $"{Numerator}/{Denominator}";
}