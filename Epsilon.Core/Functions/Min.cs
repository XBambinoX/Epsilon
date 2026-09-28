namespace Epsilon.Core;

public sealed class Min(Expr left, Expr right) : Expr
{
    private readonly ImmutableArray<Expr> _children = [left, right];

    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Min(Left.Evaluate(bindings), Right.Evaluate(bindings));

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        throw new NotImplementedException("min(a,b) is undefined over C - the complex numbers are not ordered.");

    protected override Expr DifferentiateCore(string variable) =>
        throw new NotImplementedException(
            "min(a,b) has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    public override ImmutableArray<Expr> Children => _children;
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Min(a, b));

    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
    public override string ToString() => $"min({Left}, {Right})";
}