namespace Epsilon.Core;

/// <summary>Rounds to the nearest integer, halves away from zero: <c>round(2.5) = 3</c>, <c>round(-2.5) = -3</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Round(Expr argument) : UnaryExpr(argument)
{
    // Halves round away from zero (round(2.5) = 3, round(-2.5) = -3), the same as
    // Rational.Round used by Simplify. Math.Round's default is banker's rounding
    // (to even), which would make Simplify and Evaluate disagree.
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Round(Argument.Evaluate(bindings), MidpointRounding.AwayFromZero);

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        throw new NotImplementedException("round(z) has no standard definition over the complex numbers.");

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => 
        throw new NotImplementedException(
            "Round has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Round(argument);

    /// <inheritdoc/>
    public override string ToString() => $"round({Argument})";
}