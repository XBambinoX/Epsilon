namespace Epsilon;

public abstract class Expr
{
    public abstract double Evaluate(double x);
    public abstract Expr Differentiate();
}

public sealed class Constant(double value) : Expr
{
    public double Value { get; } = value;

    public override double Evaluate(double x) => Value;
    public override Expr Differentiate() => new Constant(0);
    public override string ToString() => Value.ToString();
}

public sealed class Variable : Expr
{
    public override double Evaluate(double x) => x;
    public override Expr Differentiate() => new Constant(1);
    public override string ToString() => "x";
}