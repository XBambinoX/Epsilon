namespace Epsilon.Core;

/// <summary>The smallest integer not less than x: <c>ceiling(2.1) = 3</c>, <c>ceiling(-2.5) = -2</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Ceiling(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Ceiling(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        throw new NotSupportedException(
            "ceiling(z) is not supported over the complex numbers yet (planned for a future version). " +
            "Use Evaluate for real values.");

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => 
        throw new NotSupportedException(
            "The derivative of ceiling is not supported yet: it is 0 between the jumps but undefined " +
            "at them, which needs piecewise expressions (planned for a future version).");

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Ceiling(argument);

    /// <inheritdoc/>
    public override string ToString() => $"ceiling({Argument})";
}