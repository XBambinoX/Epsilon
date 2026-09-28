namespace Epsilon.Core;

public readonly struct Complex : IEquatable<Complex>
{
    public double Real { get; }
    public double Imaginary { get; }

    public Complex(double real, double imaginary = 0)
    {
        Real = real == 0 ? 0.0 : real;
        Imaginary = imaginary == 0 ? 0.0 : imaginary;
    }

    public static readonly Complex Zero = new(0, 0);
    public static readonly Complex One = new(1, 0);
    public static readonly Complex ImaginaryUnit = new(0, 1);

    // Hypot avoids squaring the components, which overflows to infinity for values
    // around 1e155 and above even when the magnitude itself is perfectly representable.
    public double Magnitude => double.Hypot(Real, Imaginary);
    public double Phase => Math.Atan2(Imaginary, Real);
    public Complex Conjugate => new(Real, -Imaginary);

    public static Complex FromPolar(double magnitude, double phase) =>
        new(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));

    public static implicit operator Complex(double real) => new(real, 0);

    public static Complex operator +(Complex a, Complex b) => new(a.Real + b.Real, a.Imaginary + b.Imaginary);
    public static Complex operator -(Complex a, Complex b) => new(a.Real - b.Real, a.Imaginary - b.Imaginary);
    public static Complex operator -(Complex a) => new(-a.Real, -a.Imaginary);

    public static Complex operator *(Complex a, Complex b) =>
        new(a.Real * b.Real - a.Imaginary * b.Imaginary,
            a.Real * b.Imaginary + a.Imaginary * b.Real);

    // Smith's algorithm: divides through by the larger component of b first, so the
    // intermediate |b|^2 of the textbook formula (which overflows for |b| ~ 1e155) never appears.
    public static Complex operator /(Complex a, Complex b)
    {
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

    public static Complex Exp(Complex z) => FromPolar(Math.Exp(z.Real), z.Imaginary);
    public static Complex Log(Complex z) => new(Math.Log(z.Magnitude), z.Phase);
    public static Complex Sqrt(Complex z) => FromPolar(Math.Sqrt(z.Magnitude), z.Phase / 2);

    public static Complex Pow(Complex baseValue, Complex exponent)
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
                return new Complex(double.PositiveInfinity);  // 0^-1 = +inf, as Math.Pow(0, -1)
            return new Complex(double.NaN, double.NaN);       // e.g. 0^i: undefined
        }

        return Exp(exponent * Log(baseValue));
    }

    public bool Equals(Complex other) => Real.Equals(other.Real) && Imaginary.Equals(other.Imaginary);
    public override bool Equals(object? obj) => obj is Complex other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Real, Imaginary);
    public static bool operator ==(Complex a, Complex b) => a.Equals(b);
    public static bool operator !=(Complex a, Complex b) => !a.Equals(b);

    public override string ToString()
    {
        if (Imaginary == 0) return Real.ToString();
        if (Real == 0) return $"{Imaginary}i";
        return Imaginary > 0 ? $"{Real} + {Imaginary}i" : $"{Real} - {Math.Abs(Imaginary)}i";
    }
    
    //Trigonometry
    public static Complex Sin(Complex z) =>
    new(Math.Sin(z.Real) * Math.Cosh(z.Imaginary), Math.Cos(z.Real) * Math.Sinh(z.Imaginary));

    public static Complex Cos(Complex z) =>
        new(Math.Cos(z.Real) * Math.Cosh(z.Imaginary), -Math.Sin(z.Real) * Math.Sinh(z.Imaginary));

    public static Complex Tan(Complex z) => Sin(z) / Cos(z);

    public static Complex Sinh(Complex z) =>
        new(Math.Sinh(z.Real) * Math.Cos(z.Imaginary), Math.Cosh(z.Real) * Math.Sin(z.Imaginary));

    public static Complex Cosh(Complex z) =>
        new(Math.Cosh(z.Real) * Math.Cos(z.Imaginary), Math.Sinh(z.Real) * Math.Sin(z.Imaginary));

    public static Complex Tanh(Complex z) => Sinh(z) / Cosh(z);

    public static Complex Asin(Complex z) =>
        -ImaginaryUnit * Log(ImaginaryUnit * z + Sqrt(One - z * z));

    public static Complex Acos(Complex z) =>
        -ImaginaryUnit * Log(z + ImaginaryUnit * Sqrt(One - z * z));

    public static Complex Atan(Complex z) =>
        (ImaginaryUnit / new Complex(2)) * Log((One - ImaginaryUnit * z) / (One + ImaginaryUnit * z));

    // asinh(z) = ln(z + sqrt(z^2 + 1))
    public static Complex Asinh(Complex z) => Log(z + Sqrt(z * z + One));

    // acosh(z) = ln(z + sqrt(z^2 - 1))
    public static Complex Acosh(Complex z) => Log(z + Sqrt(z * z - One));

    // atanh(z) = (1/2) * ln((1+z) / (1-z))
    public static Complex Atanh(Complex z) => Log((One + z) / (One - z)) / new Complex(2);

    public static Complex Coth(Complex z) => Cosh(z) / Sinh(z);
    public static Complex Sech(Complex z) => One / Cosh(z);
    public static Complex Csch(Complex z) => One / Sinh(z);
}