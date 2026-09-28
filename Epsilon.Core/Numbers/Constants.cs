namespace Epsilon.Core;

/// <summary>The constant pi = 3.14159... Printed as <c>pi</c>; the parser also accepts <c>π</c>.</summary>
public sealed class Pi : Expr
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.PI;
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => new ComplexNumber(Math.PI);
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => new Constant(0);
    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => NoChildren;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);
    /// <summary>Enables empty positional patterns such as <c>case Pi():</c>.</summary>
    public void Deconstruct() { }
    /// <inheritdoc/>
    public override string ToString() => "pi";
}

/// <summary>Euler's number e = 2.71828..., the base of the natural logarithm. Printed and parsed as <c>e</c>.</summary>
public sealed class EulerNumber : Expr
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.E;
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => new ComplexNumber(Math.E);
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => new Constant(0);
    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => NoChildren;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);
    /// <summary>Enables empty positional patterns such as <c>case Pi():</c>.</summary>
    public void Deconstruct() { }
    /// <inheritdoc/>
    public override string ToString() => "e";
}

/// <summary>
/// The imaginary unit i (i^2 = -1). Only has a value under <see cref="Expr.EvaluateComplex(IReadOnlyDictionary{string, ComplexNumber})"/>;
/// real <see cref="Expr.Evaluate(IReadOnlyDictionary{string, double})"/> throws <see cref="InvalidOperationException"/>.
/// </summary>
public sealed class ImaginaryUnit : Expr
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        throw new InvalidOperationException("The imaginary unit has no real value; use EvaluateComplex instead.");

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.ImaginaryUnit;

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => new Constant(0);

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => NoChildren;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);

    /// <summary>Enables empty positional patterns such as <c>case Pi():</c>.</summary>
    public void Deconstruct() { }

    /// <inheritdoc/>
    public override string ToString() => "i";
}