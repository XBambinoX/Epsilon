using System.Numerics;

namespace Epsilon.Core;

/// <summary>
/// An exact fraction of arbitrary-size integers, always kept in lowest terms with a positive
/// denominator (so 2/4 and 1/2 are the same value and compare equal). The number type behind
/// <see cref="Constant"/>.
/// </summary>
public readonly struct Rational : IEquatable<Rational>, IComparable<Rational>
{
    /// <summary>The numerator in lowest terms; carries the sign.</summary>
    public BigInteger Numerator { get; }
    /// <summary>The denominator in lowest terms; always positive.</summary>
    public BigInteger Denominator { get; }

    /// <summary>0.</summary>
    public static readonly Rational Zero = new(0, 1);
    /// <summary>1.</summary>
    public static readonly Rational One = new(1, 1);
    /// <summary>-1.</summary>
    public static readonly Rational MinusOne = new(-1, 1);

    /// <summary>The fraction <paramref name="numerator"/> / <paramref name="denominator"/>, reduced to lowest terms.</summary>
    /// <exception cref="DivideByZeroException"><paramref name="denominator"/> is 0.</exception>
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

    /// <summary>The integer <paramref name="integer"/>.</summary>
    public Rational(BigInteger integer) : this(integer, 1) { }

    /// <summary>Whether the value is 0.</summary>
    public bool IsZero => Numerator.IsZero;
    /// <summary>Whether the value is 1.</summary>
    public bool IsOne => Numerator == Denominator;
    /// <summary>Whether the value is a whole number.</summary>
    public bool IsInteger => Denominator.IsOne;
    /// <summary>-1, 0 or 1, the sign of the value.</summary>
    public int Sign => Numerator.Sign;

    // Round
    /// <summary>The largest integer not greater than the value: floor(-5/2) = -3.</summary>
    public Rational Floor()
    {
        BigInteger q = BigInteger.DivRem(Numerator, Denominator, out BigInteger r);
        if (r != 0 && Numerator.Sign < 0) q -= 1;
        return new Rational(q);
    }

    /// <summary>The smallest integer not less than the value: ceiling(-5/2) = -2.</summary>
    public Rational Ceiling()
    {
        BigInteger q = BigInteger.DivRem(Numerator, Denominator, out BigInteger r);
        if (r != 0 && Numerator.Sign > 0) q += 1;
        return new Rational(q);
    }

    // Halves round away from zero: 5/2 -> 3, -5/2 -> -3 (same as Round.Evaluate).
    /// <summary>The nearest integer, halves away from zero: round(5/2) = 3, round(-5/2) = -3.</summary>
    public Rational Round()
    {
        Rational doubled = new Rational(Numerator * 2, Denominator);
        BigInteger q = BigInteger.DivRem(doubled.Numerator, doubled.Denominator, out BigInteger r);
        BigInteger half = BigInteger.DivRem(q, 2, out BigInteger rem);
        if (rem != 0)
            half += Numerator.Sign >= 0 ? 1 : -1;
        return new Rational(half);
    }

    /// <summary>The absolute value.</summary>
    public Rational Abs() => Numerator.Sign < 0 ? new Rational(-Numerator, Denominator) : this;

    /// <summary>The exact power value^<paramref name="exponent"/>; 0^0 = 1.</summary>
    /// <exception cref="DivideByZeroException">The value is 0 and <paramref name="exponent"/> is negative.</exception>
    public Rational Pow(int exponent)
    {
        if (exponent == 0) return One;

        if (exponent > 0)
            return new Rational(BigInteger.Pow(Numerator, exponent), BigInteger.Pow(Denominator, exponent));

        if (IsZero)
            throw new DivideByZeroException("Cannot raise zero to a negative power.");

        return new Rational(BigInteger.Pow(Denominator, -exponent), BigInteger.Pow(Numerator, -exponent));
    }

    /// <summary>The nearest <see cref="double"/>; may lose precision or overflow to an infinity.</summary>
    public double ToDouble() => (double)Numerator / (double)Denominator;

    /// <summary>The integer as an exact rational.</summary>
    public static implicit operator Rational(int value) => new(value);
    /// <summary>The integer as an exact rational.</summary>
    public static implicit operator Rational(long value) => new(value);
    /// <summary>Same as <see cref="FromDouble"/>.</summary>
    /// <exception cref="ArgumentException">The value is NaN or infinite.</exception>
    public static explicit operator Rational(double value) => FromDouble(value);

    /// <summary>
    /// The exact value of the shortest decimal form of <paramref name="value"/> (15 significant
    /// digits): 0.1 becomes exactly 1/10, not the nearest binary fraction.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is NaN or infinite.</exception>
    public static Rational FromDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException($"Cannot represent {value} as a Rational.");

        string text = value.ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
        return FromDecimalString(text);
    }

    // Parses an exact decimal like "-12.5", optionally in scientific notation ("1.5E-20"),
    // which is what double.ToString("G15") produces for very large or very small values.
    /// <summary>
    /// Parses an exact decimal such as "-12.5", "0.001" or "1.5E-20" (invariant culture).
    /// "0.1" becomes exactly 1/10.
    /// </summary>
    /// <exception cref="FormatException"><paramref name="text"/> is not a decimal number.</exception>
    public static Rational FromDecimalString(string text)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        int exponent = 0;
        int expIndex = text.IndexOfAny(['e', 'E']);
        if (expIndex >= 0)
        {
            exponent = int.Parse(text[(expIndex + 1)..], System.Globalization.NumberStyles.AllowLeadingSign, culture);
            text = text[..expIndex];
        }

        bool negative = text.StartsWith('-');
        if (negative) text = text[1..];

        int dotIndex = text.IndexOf('.');
        string digits = dotIndex < 0 ? text : text.Remove(dotIndex, 1);
        int fractionalDigits = dotIndex < 0 ? 0 : text.Length - dotIndex - 1;

        BigInteger numerator = BigInteger.Parse(digits, System.Globalization.NumberStyles.None, culture);
        BigInteger denominator = BigInteger.One;

        // Shift the decimal point: the value is digits * 10^(exponent - fractionalDigits).
        int scale = exponent - fractionalDigits;
        if (scale >= 0)
            numerator *= BigInteger.Pow(10, scale);
        else
            denominator = BigInteger.Pow(10, -scale);

        if (negative) numerator = -numerator;

        return new Rational(numerator, denominator);
    }

    /// <summary>Exact sum.</summary>
    public static Rational operator +(Rational a, Rational b) =>
        new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);

    /// <summary>Exact difference.</summary>
    public static Rational operator -(Rational a, Rational b) =>
        new(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);

    /// <summary>Negation.</summary>
    public static Rational operator -(Rational a) => new(-a.Numerator, a.Denominator);

    /// <summary>Exact product.</summary>
    public static Rational operator *(Rational a, Rational b) =>
        new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);

    /// <summary>Exact quotient.</summary>
    /// <exception cref="DivideByZeroException"><paramref name="b"/> is 0.</exception>
    public static Rational operator /(Rational a, Rational b)
    {
        if (b.IsZero) throw new DivideByZeroException("Division by zero.");
        return new Rational(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
    }

    /// <summary>Value equality.</summary>
    public static bool operator ==(Rational a, Rational b) => a.Equals(b);
    /// <summary>Value inequality.</summary>
    public static bool operator !=(Rational a, Rational b) => !a.Equals(b);
    /// <summary>Exact comparison.</summary>
    public static bool operator <(Rational a, Rational b) => (a - b).Sign < 0;
    /// <summary>Exact comparison.</summary>
    public static bool operator >(Rational a, Rational b) => (a - b).Sign > 0;
    /// <summary>Exact comparison.</summary>
    public static bool operator <=(Rational a, Rational b) => (a - b).Sign <= 0;
    /// <summary>Exact comparison.</summary>
    public static bool operator >=(Rational a, Rational b) => (a - b).Sign >= 0;

    /// <summary>Value equality (both sides are in lowest terms, so this is exact).</summary>
    public bool Equals(Rational other) => Numerator == other.Numerator && Denominator == other.Denominator;
    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Rational other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);
    /// <summary>Exact ordering: negative, zero or positive as this value is less than, equal to or greater than <paramref name="other"/>.</summary>
    public int CompareTo(Rational other) => (this - other).Sign;

    /// <inheritdoc/>
    public override string ToString() => IsInteger ? Numerator.ToString() : $"{Numerator}/{Denominator}";
}