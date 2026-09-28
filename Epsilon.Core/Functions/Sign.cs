namespace Epsilon.Core;

/// <summary>The sign of x: -1, 0 or 1.</summary>
/// <param name="argument">The argument.</param>
public sealed class Sign(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Sign(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        throw new NotSupportedException(
            "sign(z) is not supported over the complex numbers yet (planned for a future version). " +
            "Use Evaluate for real values.");

    // Piecewise-constant a.e.; derivative is 0 everywhere except at the root of
    // Argument, where it's a Dirac delta in the distributional sense - not
    // representable as a plain Expr.
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => 
        throw new NotSupportedException(
            "The derivative of sign is not supported yet: it is 0 between the jumps but undefined " +
            "at them, which needs piecewise expressions (planned for a future version).");

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Sign(argument);

    /// <inheritdoc/>
    public override string ToString() => $"sign({Argument})";
}