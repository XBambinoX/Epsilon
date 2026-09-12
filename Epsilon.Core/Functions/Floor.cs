namespace Epsilon.Core;

public sealed class Floor(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Floor(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        throw new NotImplementedException("floor(z) has no standard definition over the complex numbers.");

    public override Expr Differentiate(string variable) => 
        throw new NotImplementedException(
            "floor has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();

    public override Expr Substitute(string variable, Expr replacement) =>
        new Floor(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"floor({Argument})";
}