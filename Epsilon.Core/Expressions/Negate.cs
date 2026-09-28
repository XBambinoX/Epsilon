namespace Epsilon.Core;

/// <summary>The negation <c>-argument</c>.</summary>
public sealed class Negate(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        -Argument.Evaluate(bindings);

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        -Argument.EvaluateComplex(bindings);

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Negate(Argument.Differentiate(variable)).Simplify();

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Negate(argument);

    /// <inheritdoc/>
    public override string ToString() => $"-{Argument}";
}