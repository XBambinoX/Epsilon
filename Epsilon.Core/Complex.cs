namespace Epsilon.Core;

public readonly struct Complex : IEquatable<Complex>
{
    public double Real { get; }
    public double Imaginary { get; }

    public Complex(double real, double imaginary = 0)
    {
        Real = real;
        Imaginary = imaginary;
    }

    public static readonly Complex Zero = new(0, 0);
    public static readonly Complex One = new(1, 0);
    public static readonly Complex ImaginaryUnit = new(0, 1);

    public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);
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

    public static Complex operator /(Complex a, Complex b)
    {
        double denom = b.Real * b.Real + b.Imaginary * b.Imaginary;
        return new(
            (a.Real * b.Real + a.Imaginary * b.Imaginary) / denom,
            (a.Imaginary * b.Real - a.Real * b.Imaginary) / denom
        );
    }

    public static Complex Exp(Complex z) => FromPolar(Math.Exp(z.Real), z.Imaginary);
    public static Complex Log(Complex z) => new(Math.Log(z.Magnitude), z.Phase);
    public static Complex Sqrt(Complex z) => FromPolar(Math.Sqrt(z.Magnitude), z.Phase / 2);

    public static Complex Pow(Complex baseValue, Complex exponent)
    {
        if (baseValue == Zero)
            return exponent == Zero ? One : Zero;

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
}