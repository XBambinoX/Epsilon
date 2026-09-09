namespace Epsilon.Core;

public sealed class Min(Expr left, Expr right) : Expr
{
    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Min(Left.Evaluate(bindings), Right.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        throw new NotImplementedException("min(a,b) is undefined over C - the complex numbers are not ordered.");

    public override Expr Differentiate(string variable) =>
        throw new NotImplementedException(
            "min(a,b) has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    public override IReadOnlySet<string> GetVariables() =>
        new HashSet<string>(Left.GetVariables().Union(Right.GetVariables()));

    public override Expr Substitute(string variable, Expr replacement) =>
        new Min(Left.Substitute(variable, replacement), Right.Substitute(variable, replacement));

    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
    public override string ToString() => $"min({Left}, {Right})";
}