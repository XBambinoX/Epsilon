namespace Epsilon.Core;

/// <summary>The exponential <c>exp(x) = e^x</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Exp(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Exp(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Exp(Argument.EvaluateComplex(bindings));

    // d/dx e^f(x) = e^f(x) * f'(x)
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(new Exp(Argument), Argument.Differentiate(variable)).Simplify();

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Exp(argument);

    /// <inheritdoc/>
    public override string ToString() => $"exp({Argument})";
}

/// <summary>The natural logarithm <c>ln(x)</c>; NaN over the reals for x &lt; 0 and -infinity at 0.</summary>
/// <param name="argument">The argument.</param>
public sealed class Ln(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Log(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Log(Argument.EvaluateComplex(bindings));

    // d/dx ln(f(x)) = f'(x) / f(x)
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(Argument.Differentiate(variable), Argument).Simplify();

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Ln(argument);

    /// <inheritdoc/>
    public override string ToString() => $"ln({Argument})";
}