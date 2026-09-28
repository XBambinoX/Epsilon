namespace Epsilon.Core;

/// <summary>The smaller of two values: <c>min(a, b)</c>.</summary>
/// <param name="left">The first value.</param>
/// <param name="right">The second value.</param>
public sealed class Min(Expr left, Expr right) : Expr
{
    private readonly ImmutableArray<Expr> _children = [left, right];

    /// <summary>The first value.</summary>
    public Expr Left { get; } = left;
    /// <summary>The second value.</summary>
    public Expr Right { get; } = right;

    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Min(Left.Evaluate(bindings), Right.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        throw new NotImplementedException("min(a,b) is undefined over C - the complex numbers are not ordered.");

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        throw new NotImplementedException(
            "min(a,b) has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => _children;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Min(a, b));

    /// <summary>Deconstructs the node for positional patterns: <c>case Min(var a, var b):</c>.</summary>
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
    /// <inheritdoc/>
    public override string ToString() => $"min({Left}, {Right})";
}