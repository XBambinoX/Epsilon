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
        throw new NotImplementedException("ceiling(z) has no standard definition over the complex numbers.");

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => 
        throw new NotImplementedException(
            "Ceiling has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Ceiling(argument);

    /// <inheritdoc/>
    public override string ToString() => $"ceiling({Argument})";
}