namespace Epsilon.Core;

public sealed class Sign(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Sign(Argument.Evaluate(bindings));

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        throw new NotImplementedException("sign(z) has no standard definition over the complex numbers.");

    // Piecewise-constant a.e.; derivative is 0 everywhere except at the root of
    // Argument, where it's a Dirac delta in the distributional sense - not
    // representable as a plain Expr.
    protected override Expr DifferentiateCore(string variable) => 
        throw new NotImplementedException(
            "Sign(x) has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    protected override Expr WithArgument(Expr argument) => new Sign(argument);

    public override string ToString() => $"sign({Argument})";
}