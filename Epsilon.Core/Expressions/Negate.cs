namespace Epsilon.Core;

public sealed class Negate(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        -Argument.Evaluate(bindings);

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        -Argument.EvaluateComplex(bindings);

    public override Expr Differentiate(string variable) =>
        new Negate(Argument.Differentiate(variable)).Simplify();

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();

    public override Expr Substitute(string variable, Expr replacement) =>
        new Negate(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"-{Argument}";
}