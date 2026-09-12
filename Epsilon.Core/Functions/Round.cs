namespace Epsilon.Core;

public sealed class Round(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Round(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        throw new NotImplementedException("round(z) has no standard definition over the complex numbers.");

    public override Expr Differentiate(string variable) => 
        throw new NotImplementedException(
            "Round has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();

    public override Expr Substitute(string variable, Expr replacement) =>
        new Round(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"round({Argument})";
}