namespace Epsilon.Core;

public sealed class Exp(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Exp(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Exp(Argument.EvaluateComplex(bindings));

    // d/dx e^f(x) = e^f(x) * f'(x)
    public override Expr Differentiate(string variable) =>
        new Multiply(new Exp(Argument), Argument.Differentiate(variable)).Simplify();

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Exp(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"exp({Argument})";
}

public sealed class Ln(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Log(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Log(Argument.EvaluateComplex(bindings));

    // d/dx ln(f(x)) = f'(x) / f(x)
    public override Expr Differentiate(string variable) =>
        new Divide(Argument.Differentiate(variable), Argument).Simplify();

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Ln(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"ln({Argument})";
}