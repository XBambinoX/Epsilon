namespace Epsilon.Core;

/// <summary>The largest integer not greater than x: <c>floor(2.7) = 2</c>, <c>floor(-2.5) = -3</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Floor(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Floor(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        throw new NotImplementedException("floor(z) has no standard definition over the complex numbers.");

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => 
        throw new NotImplementedException(
            "floor has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Floor(argument);

    /// <inheritdoc/>
    public override string ToString() => $"floor({Argument})";
}