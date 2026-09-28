namespace Epsilon.Core;

/// <summary>The absolute value <c>|x|</c>; the magnitude for complex x.</summary>
/// <param name="argument">The argument.</param>
public sealed class Abs(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Abs(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Argument.EvaluateComplex(bindings).Magnitude;

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable)
    {
        // d/dx |f(x)| = f(x) / |f(x)| * f'(x), for f(x) != 0
        return new Multiply(
            new Divide(Argument, new Abs(Argument)),
            Argument.Differentiate(variable)
        );
    }

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Abs(argument);

    /// <inheritdoc/>
    public override string ToString() =>
        $"abs({Argument})";
}