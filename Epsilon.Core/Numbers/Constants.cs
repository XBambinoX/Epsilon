namespace Epsilon.Core;

public sealed class Pi : Expr
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.PI;
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => new ComplexNumber(Math.PI);
    protected override Expr DifferentiateCore(string variable) => new Constant(0);
    public override ImmutableArray<Expr> Children => NoChildren;
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);
    public void Deconstruct() { }
    public override string ToString() => "pi";
}

public sealed class EulerNumber : Expr
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.E;
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => new ComplexNumber(Math.E);
    protected override Expr DifferentiateCore(string variable) => new Constant(0);
    public override ImmutableArray<Expr> Children => NoChildren;
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);
    public void Deconstruct() { }
    public override string ToString() => "e";
}

public sealed class ImaginaryUnit : Expr
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        throw new InvalidOperationException("The imaginary unit has no real value; use EvaluateComplex instead.");

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.ImaginaryUnit;

    protected override Expr DifferentiateCore(string variable) => new Constant(0);

    public override ImmutableArray<Expr> Children => NoChildren;
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);

    public void Deconstruct() { }

    public override string ToString() => "i";
}