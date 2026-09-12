namespace Epsilon.Core;

public sealed class Sign(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Sign(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        throw new NotImplementedException("sign(z) has no standard definition over the complex numbers.");

    // Piecewise-constant a.e.; derivative is 0 everywhere except at the root of
    // Argument, where it's a Dirac delta in the distributional sense - not
    // representable as a plain Expr.
    public override Expr Differentiate(string variable) => 
        throw new NotImplementedException(
            "Sign(x) has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();

    public override Expr Substitute(string variable, Expr replacement) =>
        new Sign(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sign({Argument})";
}