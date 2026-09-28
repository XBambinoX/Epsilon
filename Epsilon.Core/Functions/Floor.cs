namespace Epsilon.Core;

public sealed class Floor(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Floor(Argument.Evaluate(bindings));

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        throw new NotImplementedException("floor(z) has no standard definition over the complex numbers.");

    protected override Expr DifferentiateCore(string variable) => 
        throw new NotImplementedException(
            "floor has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    protected override Expr WithArgument(Expr argument) => new Floor(argument);

    public override string ToString() => $"floor({Argument})";
}