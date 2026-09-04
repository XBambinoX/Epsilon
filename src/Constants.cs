namespace Epsilon;

public sealed class Pi : Expr
{
    public override double Evaluate(double x) => Math.PI;
    public override Expr Differentiate() => new Constant(0);
    public void Deconstruct() { }
    public override string ToString() => "pi";
}

public sealed class E : Expr
{
    public override double Evaluate(double x) => Math.E;
    public override Expr Differentiate() => new Constant(0);
    public void Deconstruct() { }
    public override string ToString() => "e";
}