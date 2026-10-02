namespace Epsilon.Core;

/// <summary>
/// A complex number with <see cref="double"/> parts, the result type of
/// <see cref="Expr.EvaluateComplex(IReadOnlyDictionary{string, ComplexNumber})"/>. Multivalued
/// functions (Log, Sqrt, Pow, inverse trigonometry) return their principal value.
/// </summary>
/// <remarks>
/// Named ComplexNumber rather than Complex so it can be used next to
/// <c>using System.Numerics;</c> without an ambiguity error.
/// </remarks>
public readonly struct ComplexNumber : IEquatable<ComplexNumber>
{
    /// <summary>The real part.</summary>
    public double Real { get; }
    /// <summary>The imaginary part.</summary>
    public double Imaginary { get; }

    /// <summary>The number <paramref name="real"/> + <paramref name="imaginary"/>*i. A -0 part is stored as 0.</summary>
    public ComplexNumber(double real, double imaginary = 0)
    {
        Real = real == 0 ? 0.0 : real;
        Imaginary = imaginary == 0 ? 0.0 : imaginary;
    }

    /// <summary>0.</summary>
    public static readonly ComplexNumber Zero = new(0, 0);
    /// <summary>1.</summary>
    public static readonly ComplexNumber One = new(1, 0);
    /// <summary>i.</summary>
    public static readonly ComplexNumber ImaginaryUnit = new(0, 1);

    // Hypot avoids squaring the components, which overflows to infinity for values
    // around 1e155 and above even when the magnitude itself is perfectly representable.
    /// <summary>The absolute value |z|, computed without overflow for large parts.</summary>
    public double Magnitude => double.Hypot(Real, Imaginary);
    /// <summary>The argument of z in radians, in (-pi, pi].</summary>
    public double Phase => Math.Atan2(Imaginary, Real);
    /// <summary>The complex conjugate: a + bi becomes a - bi.</summary>
    public ComplexNumber Conjugate => new(Real, -Imaginary);

    /// <summary>The number with the given magnitude and phase (in radians).</summary>
    public static ComplexNumber FromPolar(double magnitude, double phase) =>
        new(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));

    /// <summary>A real number as a complex number with imaginary part 0.</summary>
    public static implicit operator ComplexNumber(double real) => new(real, 0);

    /// <summary>Sum.</summary>
    public static ComplexNumber operator +(ComplexNumber a, ComplexNumber b) => new(a.Real + b.Real, a.Imaginary + b.Imaginary);
    /// <summary>Difference.</summary>
    public static ComplexNumber operator -(ComplexNumber a, ComplexNumber b) => new(a.Real - b.Real, a.Imaginary - b.Imaginary);
    /// <summary>Negation.</summary>
    public static ComplexNumber operator -(ComplexNumber a) => new(-a.Real, -a.Imaginary);

    /// <summary>Product.</summary>
    public static ComplexNumber operator *(ComplexNumber a, ComplexNumber b) =>
        new(a.Real * b.Real - a.Imaginary * b.Imaginary,
            a.Real * b.Imaginary + a.Imaginary * b.Real);

    // Smith's algorithm: divides through by the larger component of b first, so the
    // intermediate |b|^2 of the textbook formula (which overflows for |b| ~ 1e155) never appears.
    /// <summary>
    /// Quotient, computed with Smith's algorithm to avoid overflow for large divisors. Division by
    /// zero follows real <see cref="double"/> division: z / 0 is infinite in the direction of z
    /// (1/0 = inf, -1/0 = -inf, i/0 = inf*i) and 0/0 is NaN.
    /// </summary>
    public static ComplexNumber operator /(ComplexNumber a, ComplexNumber b)
    {
        // A real or purely imaginary divisor is divided directly: Smith's formula would multiply
        // a part of a by the zero ratio, and infinity * 0 turns an infinite part into NaN.
        if (b.Imaginary == 0)
            return b.Real == 0 ? DivideByZero(a) : new(a.Real / b.Real, a.Imaginary / b.Real);
        if (b.Real == 0)
            return new(a.Imaginary / b.Imaginary, -a.Real / b.Imaginary);

        if (Math.Abs(b.Real) >= Math.Abs(b.Imaginary))
        {
            double ratio = b.Imaginary / b.Real;
            double denom = b.Real + b.Imaginary * ratio;
            return new(
                (a.Real + a.Imaginary * ratio) / denom,
                (a.Imaginary - a.Real * ratio) / denom);
        }
        else
        {
            double ratio = b.Real / b.Imaginary;
            double denom = b.Real * ratio + b.Imaginary;
            return new(
                (a.Real * ratio + a.Imaginary) / denom,
                (a.Imaginary * ratio - a.Real) / denom);
        }
    }

    // Zero is always stored as +0, so each part follows real division by +0, as Evaluate does:
    // x / +0 is inf or -inf, NaN for x = 0. A zero part of a non-zero numerator stays 0, so the
    // result points in the direction of a (i/0 = inf*i, not NaN + inf*i).
    private static ComplexNumber DivideByZero(ComplexNumber a)
    {
        if (a == Zero)
            return new(double.NaN, double.NaN);

        return new(a.Real == 0 ? 0 : a.Real / 0.0, a.Imaginary == 0 ? 0 : a.Imaginary / 0.0);
    }

    /// <summary>e^z.</summary>
    public static ComplexNumber Exp(ComplexNumber z) => FromPolar(Math.Exp(z.Real), z.Imaginary);
    /// <summary>The principal natural logarithm: ln|z| + i*Phase(z).</summary>
    public static ComplexNumber Log(ComplexNumber z) => new(Math.Log(z.Magnitude), z.Phase);
    /// <summary>The principal square root, with non-negative real part.</summary>
    public static ComplexNumber Sqrt(ComplexNumber z) => FromPolar(Math.Sqrt(z.Magnitude), z.Phase / 2);

    /// <summary>
    /// The principal power <paramref name="baseValue"/>^<paramref name="exponent"/> = exp(exponent * Log(base)).
    /// For base 0: 0^0 = 1, 0 for Re(exponent) &gt; 0, +infinity for a negative real exponent, NaN otherwise.
    /// </summary>
    public static ComplexNumber Pow(ComplexNumber baseValue, ComplexNumber exponent)
    {
        if (baseValue == Zero)
        {
            // |0^w| = 0^Re(w), so the real part of the exponent decides. The real-exponent
            // cases match Math.Pow, keeping EvaluateComplex consistent with Evaluate.
            if (exponent == Zero)
                return One;                                   // 0^0 = 1, as Math.Pow(0, 0)
            if (exponent.Real > 0)
                return Zero;
            if (exponent.Imaginary == 0)
                return new ComplexNumber(double.PositiveInfinity);  // 0^-1 = +inf, as Math.Pow(0, -1)
            return new ComplexNumber(double.NaN, double.NaN);       // e.g. 0^i: undefined
        }

        return Exp(exponent * Log(baseValue));
    }

    /// <summary>Exact equality of both parts.</summary>
    public bool Equals(ComplexNumber other) => Real.Equals(other.Real) && Imaginary.Equals(other.Imaginary);
    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ComplexNumber other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Real, Imaginary);
    /// <summary>Exact equality of both parts.</summary>
    public static bool operator ==(ComplexNumber a, ComplexNumber b) => a.Equals(b);
    /// <summary>Inequality.</summary>
    public static bool operator !=(ComplexNumber a, ComplexNumber b) => !a.Equals(b);

    // Invariant culture, like the parser: "1.5 + 2i" everywhere, never "1,5 + 2i".
    // Any NaN part makes the whole value undefined, so it prints as a single "NaN".
    /// <inheritdoc/>
    public override string ToString()
    {
        if (double.IsNaN(Real) || double.IsNaN(Imaginary)) return "NaN";

        string Format(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (Imaginary == 0) return Format(Real);
        if (Real == 0) return $"{Format(Imaginary)}i";
        return Imaginary > 0
            ? $"{Format(Real)} + {Format(Imaginary)}i"
            : $"{Format(Real)} - {Format(Math.Abs(Imaginary))}i";
    }
    
    //Trigonometry
    /// <summary>The sine.</summary>
    public static ComplexNumber Sin(ComplexNumber z) =>
    new(Math.Sin(z.Real) * Math.Cosh(z.Imaginary), Math.Cos(z.Real) * Math.Sinh(z.Imaginary));

    /// <summary>The cosine.</summary>
    public static ComplexNumber Cos(ComplexNumber z) =>
        new(Math.Cos(z.Real) * Math.Cosh(z.Imaginary), -Math.Sin(z.Real) * Math.Sinh(z.Imaginary));

    /// <summary>The tangent.</summary>
    public static ComplexNumber Tan(ComplexNumber z) => Sin(z) / Cos(z);

    /// <summary>The hyperbolic sine.</summary>
    public static ComplexNumber Sinh(ComplexNumber z) =>
        new(Math.Sinh(z.Real) * Math.Cos(z.Imaginary), Math.Cosh(z.Real) * Math.Sin(z.Imaginary));

    /// <summary>The hyperbolic cosine.</summary>
    public static ComplexNumber Cosh(ComplexNumber z) =>
        new(Math.Cosh(z.Real) * Math.Cos(z.Imaginary), Math.Sinh(z.Real) * Math.Sin(z.Imaginary));

    /// <summary>The hyperbolic tangent.</summary>
    public static ComplexNumber Tanh(ComplexNumber z) => Sinh(z) / Cosh(z);

    /// <summary>The principal arcsine.</summary>
    public static ComplexNumber Asin(ComplexNumber z) =>
        -ImaginaryUnit * Log(ImaginaryUnit * z + Sqrt(One - z * z));

    /// <summary>The principal arccosine.</summary>
    public static ComplexNumber Acos(ComplexNumber z) =>
        -ImaginaryUnit * Log(z + ImaginaryUnit * Sqrt(One - z * z));

    /// <summary>The principal arctangent.</summary>
    public static ComplexNumber Atan(ComplexNumber z) =>
        (ImaginaryUnit / new ComplexNumber(2)) * Log((One - ImaginaryUnit * z) / (One + ImaginaryUnit * z));

    // asinh(z) = ln(z + sqrt(z^2 + 1))
    /// <summary>The principal inverse hyperbolic sine.</summary>
    public static ComplexNumber Asinh(ComplexNumber z) => Log(z + Sqrt(z * z + One));

    // acosh(z) = ln(z + sqrt(z^2 - 1))
    /// <summary>The principal inverse hyperbolic cosine.</summary>
    public static ComplexNumber Acosh(ComplexNumber z) => Log(z + Sqrt(z * z - One));

    // atanh(z) = (1/2) * ln((1+z) / (1-z))
    /// <summary>The principal inverse hyperbolic tangent.</summary>
    public static ComplexNumber Atanh(ComplexNumber z) => Log((One + z) / (One - z)) / new ComplexNumber(2);

    /// <summary>The hyperbolic cotangent.</summary>
    public static ComplexNumber Coth(ComplexNumber z) => Cosh(z) / Sinh(z);
    /// <summary>The hyperbolic secant.</summary>
    public static ComplexNumber Sech(ComplexNumber z) => One / Cosh(z);
    /// <summary>The hyperbolic cosecant.</summary>
    public static ComplexNumber Csch(ComplexNumber z) => One / Sinh(z);
}