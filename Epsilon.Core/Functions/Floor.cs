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
        throw new NotSupportedException(
            "floor(z) is not supported over the complex numbers yet (planned for a future version). " +
            "Use Evaluate for real values.");

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => 
        throw new NotSupportedException(
            "The derivative of floor is not supported yet: it is 0 between the jumps but undefined " +
            "at them, which needs piecewise expressions (planned for a future version).");

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Floor(argument);

    /// <inheritdoc/>
    public override string ToString() => $"floor({Argument})";
}