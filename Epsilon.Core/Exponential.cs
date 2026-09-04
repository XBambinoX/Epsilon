namespace Epsilon.Core;

public sealed class Exp(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(double x) => Math.Exp(Argument.Evaluate(x));

    // d/dx e^f(x) = e^f(x) * f'(x)
    public override Expr Differentiate() =>
        new Multiply(new Exp(Argument), Argument.Differentiate()).Simplify();

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"exp({Argument})";
}

public sealed class Ln(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(double x) => Math.Log(Argument.Evaluate(x));

    // d/dx ln(f(x)) = f'(x) / f(x)
    public override Expr Differentiate() =>
        new Divide(Argument.Differentiate(), Argument).Simplify();

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"ln({Argument})";
}