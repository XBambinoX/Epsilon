namespace Epsilon.Core;

public sealed class Add(Expr left, Expr right) : Expr
{
    private readonly ImmutableArray<Expr> _children = [left, right];

    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Left.Evaluate(bindings) + Right.Evaluate(bindings);

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Left.EvaluateComplex(bindings) + Right.EvaluateComplex(bindings);

    protected override Expr DifferentiateCore(string variable) =>
        new Add(Left.Differentiate(variable), Right.Differentiate(variable)).Simplify();

    public override ImmutableArray<Expr> Children => _children;
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Add(a, b));

    public override string ToString() => $"({Left} + {Right})";
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

public sealed class Subtract(Expr left, Expr right) : Expr
{
    private readonly ImmutableArray<Expr> _children = [left, right];

    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Left.Evaluate(bindings) - Right.Evaluate(bindings);

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Left.EvaluateComplex(bindings) - Right.EvaluateComplex(bindings);

    protected override Expr DifferentiateCore(string variable) =>
        new Subtract(Left.Differentiate(variable), Right.Differentiate(variable)).Simplify();

    public override ImmutableArray<Expr> Children => _children;
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Subtract(a, b));

    public override string ToString() => $"({Left} - {Right})";
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

public sealed class Multiply(Expr left, Expr right) : Expr
{
    private readonly ImmutableArray<Expr> _children = [left, right];

    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Left.Evaluate(bindings) * Right.Evaluate(bindings);

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Left.EvaluateComplex(bindings) * Right.EvaluateComplex(bindings);

    protected override Expr DifferentiateCore(string variable) =>
        new Add(
            new Multiply(Left.Differentiate(variable), Right),
            new Multiply(Left, Right.Differentiate(variable))
        ).Simplify();

    public override ImmutableArray<Expr> Children => _children;
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Multiply(a, b));

    public override string ToString() => $"({Left} * {Right})";
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

public sealed class Divide(Expr numerator, Expr denominator) : Expr
{
    private readonly ImmutableArray<Expr> _children = [numerator, denominator];

    public Expr Numerator { get; } = numerator;
    public Expr Denominator { get; } = denominator;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Numerator.Evaluate(bindings) / Denominator.Evaluate(bindings);

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Numerator.EvaluateComplex(bindings) / Denominator.EvaluateComplex(bindings);

    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            new Subtract(
                new Multiply(Numerator.Differentiate(variable), Denominator),
                new Multiply(Numerator, Denominator.Differentiate(variable))
            ),
            new Power(Denominator, new Constant(2))
        ).Simplify();

    public override ImmutableArray<Expr> Children => _children;
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Divide(a, b));

    public override string ToString() => $"({Numerator} / {Denominator})";
    public void Deconstruct(out Expr numerator, out Expr denominator) => (numerator, denominator) = (Numerator, Denominator);
}

public sealed class Power(Expr baseExpr, Expr exponent) : Expr
{
    private readonly ImmutableArray<Expr> _children = [baseExpr, exponent];

    public Expr Base { get; } = baseExpr;
    public Expr Exponent { get; } = exponent;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Pow(Base.Evaluate(bindings), Exponent.Evaluate(bindings));

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        ComplexNumber.Pow(Base.EvaluateComplex(bindings), Exponent.EvaluateComplex(bindings));

    protected override Expr DifferentiateCore(string variable)
    {
        if (Exponent is Constant n)
        {
            return new Multiply(
                new Multiply(n, new Power(Base, new Constant(n.Value - 1))),
                Base.Differentiate(variable)
            ).Simplify();
        }

        // d/dv (f^g) = f^g * (g' * ln(f) + g * f'/f)
        Expr fPrime = Base.Differentiate(variable);
        Expr gPrime = Exponent.Differentiate(variable);

        Expr term1 = new Multiply(gPrime, new Ln(Base));
        Expr term2 = new Multiply(Exponent, new Divide(fPrime, Base));

        return new Multiply(this, new Add(term1, term2)).Simplify();
    }

    public override ImmutableArray<Expr> Children => _children;
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Power(a, b));

    public override string ToString() => $"({Base} ^ {Exponent})";
    public void Deconstruct(out Expr baseExpr, out Expr exponent) => (baseExpr, exponent) = (Base, Exponent);
}