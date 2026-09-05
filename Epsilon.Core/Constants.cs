namespace Epsilon.Core;

public sealed class Pi : Expr
{
    public override double Evaluate(double x) => Math.PI;
    public override Complex EvaluateComplex(Complex x) => new Complex(Math.PI);
    public override Expr Differentiate() => new Constant(0);
    public void Deconstruct() { }
    public override string ToString() => "pi";
}

public sealed class E : Expr
{
    public override double Evaluate(double x) => Math.E;
    public override Complex EvaluateComplex(Complex x) => new Complex(Math.E);
    public override Expr Differentiate() => new Constant(0);
    public void Deconstruct() { }
    public override string ToString() => "e";
}

public sealed class ImaginaryUnit : Expr
{
    public override double Evaluate(double x) =>
        throw new InvalidOperationException("The imaginary unit has no real value; use EvaluateComplex instead.");

    public override Complex EvaluateComplex(Complex x) => Complex.ImaginaryUnit;

    public override Expr Differentiate() => new Constant(0);

    public void Deconstruct() { }

    public override string ToString() => "i";
}