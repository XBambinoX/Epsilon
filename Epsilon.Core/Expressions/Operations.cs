namespace Epsilon.Core;

/// <summary>The sum <c>left + right</c>.</summary>
public sealed class Add(Expr left, Expr right) : Expr
{
    private readonly ImmutableArray<Expr> _children = [left, right];

    /// <summary>The left operand.</summary>
    public Expr Left { get; } = left;
    /// <summary>The right operand.</summary>
    public Expr Right { get; } = right;

    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Left.Evaluate(bindings) + Right.Evaluate(bindings);

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Left.EvaluateComplex(bindings) + Right.EvaluateComplex(bindings);

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Add(Left.Differentiate(variable), Right.Differentiate(variable)).Simplify();

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => _children;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Add(a, b));

    /// <inheritdoc/>
    public override string ToString() => $"({Left} + {Right})";
    /// <summary>Deconstructs the node for positional patterns, e.g. <c>case Add(var l, var r):</c>.</summary>
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

/// <summary>The difference <c>left - right</c>.</summary>
public sealed class Subtract(Expr left, Expr right) : Expr
{
    private readonly ImmutableArray<Expr> _children = [left, right];

    /// <summary>The left operand.</summary>
    public Expr Left { get; } = left;
    /// <summary>The right operand.</summary>
    public Expr Right { get; } = right;

    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Left.Evaluate(bindings) - Right.Evaluate(bindings);

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Left.EvaluateComplex(bindings) - Right.EvaluateComplex(bindings);

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Subtract(Left.Differentiate(variable), Right.Differentiate(variable)).Simplify();

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => _children;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Subtract(a, b));

    /// <inheritdoc/>
    public override string ToString() => $"({Left} - {Right})";
    /// <summary>Deconstructs the node for positional patterns, e.g. <c>case Add(var l, var r):</c>.</summary>
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

/// <summary>The product <c>left * right</c>.</summary>
public sealed class Multiply(Expr left, Expr right) : Expr
{
    private readonly ImmutableArray<Expr> _children = [left, right];

    /// <summary>The left operand.</summary>
    public Expr Left { get; } = left;
    /// <summary>The right operand.</summary>
    public Expr Right { get; } = right;

    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Left.Evaluate(bindings) * Right.Evaluate(bindings);

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Left.EvaluateComplex(bindings) * Right.EvaluateComplex(bindings);

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Add(
            new Multiply(Left.Differentiate(variable), Right),
            new Multiply(Left, Right.Differentiate(variable))
        ).Simplify();

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => _children;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Multiply(a, b));

    /// <inheritdoc/>
    public override string ToString() => $"({Left} * {Right})";
    /// <summary>Deconstructs the node for positional patterns, e.g. <c>case Add(var l, var r):</c>.</summary>
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

/// <summary>The quotient <c>numerator / denominator</c>. Evaluates to an infinity or NaN where the denominator is 0.</summary>
public sealed class Divide(Expr numerator, Expr denominator) : Expr
{
    private readonly ImmutableArray<Expr> _children = [numerator, denominator];

    /// <summary>The dividend.</summary>
    public Expr Numerator { get; } = numerator;
    /// <summary>The divisor.</summary>
    public Expr Denominator { get; } = denominator;

    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Numerator.Evaluate(bindings) / Denominator.Evaluate(bindings);

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Numerator.EvaluateComplex(bindings) / Denominator.EvaluateComplex(bindings);

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            new Subtract(
                new Multiply(Numerator.Differentiate(variable), Denominator),
                new Multiply(Numerator, Denominator.Differentiate(variable))
            ),
            new Power(Denominator, new Constant(2))
        ).Simplify();

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => _children;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Divide(a, b));

    /// <inheritdoc/>
    public override string ToString() => $"({Numerator} / {Denominator})";
    /// <summary>Deconstructs the node for positional patterns, e.g. <c>case Add(var l, var r):</c>.</summary>
    public void Deconstruct(out Expr numerator, out Expr denominator) => (numerator, denominator) = (Numerator, Denominator);
}

/// <summary>
/// <c>base ^ exponent</c>, as a principal value: a negative base with a non-integer exponent is
/// undefined over the reals (<c>(-8)^(1/3)</c> is NaN; use <see cref="NthRoot"/> for the real cube root).
/// </summary>
public sealed class Power(Expr baseExpr, Expr exponent) : Expr
{
    private readonly ImmutableArray<Expr> _children = [baseExpr, exponent];

    /// <summary>The base.</summary>
    public Expr Base { get; } = baseExpr;
    /// <summary>The exponent.</summary>
    public Expr Exponent { get; } = exponent;

    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Pow(Base.Evaluate(bindings), Exponent.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        ComplexNumber.Pow(Base.EvaluateComplex(bindings), Exponent.EvaluateComplex(bindings));

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => _children;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new Power(a, b));

    /// <inheritdoc/>
    public override string ToString() => $"({Base} ^ {Exponent})";
    /// <summary>Deconstructs the node for positional patterns, e.g. <c>case Add(var l, var r):</c>.</summary>
    public void Deconstruct(out Expr baseExpr, out Expr exponent) => (baseExpr, exponent) = (Base, Exponent);
}