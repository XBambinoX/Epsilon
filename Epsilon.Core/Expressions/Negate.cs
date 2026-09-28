namespace Epsilon.Core;

public sealed class Negate(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        -Argument.Evaluate(bindings);

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        -Argument.EvaluateComplex(bindings);

    protected override Expr DifferentiateCore(string variable) =>
        new Negate(Argument.Differentiate(variable)).Simplify();

    protected override Expr WithArgument(Expr argument) => new Negate(argument);

    public override string ToString() => $"-{Argument}";
}