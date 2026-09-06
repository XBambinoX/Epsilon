namespace Epsilon.Core;

public sealed class Add(Expr left, Expr right) : Expr
{
    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Left.Evaluate(bindings) + Right.Evaluate(bindings);

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Left.EvaluateComplex(bindings) + Right.EvaluateComplex(bindings);

    public override Expr Differentiate(string variable) =>
        new Add(Left.Differentiate(variable), Right.Differentiate(variable)).Simplify();

    public override IReadOnlySet<string> GetVariables() =>
        (IReadOnlySet<string>)new HashSet<string>(Left.GetVariables().Union(Right.GetVariables()));

    public override Expr Substitute(string variable, Expr replacement) =>
        new Add(Left.Substitute(variable, replacement), Right.Substitute(variable, replacement));

    public override string ToString() => $"({Left} + {Right})";
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

public sealed class Subtract(Expr left, Expr right) : Expr
{
    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Left.Evaluate(bindings) - Right.Evaluate(bindings);

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Left.EvaluateComplex(bindings) - Right.EvaluateComplex(bindings);

    public override Expr Differentiate(string variable) =>
        new Subtract(Left.Differentiate(variable), Right.Differentiate(variable)).Simplify();

    public override IReadOnlySet<string> GetVariables() =>
        (IReadOnlySet<string>)new HashSet<string>(Left.GetVariables().Union(Right.GetVariables()));

    public override Expr Substitute(string variable, Expr replacement) =>
        new Subtract(Left.Substitute(variable, replacement), Right.Substitute(variable, replacement));

    public override string ToString() => $"({Left} - {Right})";
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

public sealed class Multiply(Expr left, Expr right) : Expr
{
    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Left.Evaluate(bindings) * Right.Evaluate(bindings);

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Left.EvaluateComplex(bindings) * Right.EvaluateComplex(bindings);

    public override Expr Differentiate(string variable) =>
        new Add(
            new Multiply(Left.Differentiate(variable), Right),
            new Multiply(Left, Right.Differentiate(variable))
        ).Simplify();

    public override IReadOnlySet<string> GetVariables() =>
        (IReadOnlySet<string>)new HashSet<string>(Left.GetVariables().Union(Right.GetVariables()));

    public override Expr Substitute(string variable, Expr replacement) =>
        new Multiply(Left.Substitute(variable, replacement), Right.Substitute(variable, replacement));

    public override string ToString() => $"({Left} * {Right})";
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

public sealed class Divide(Expr numerator, Expr denominator) : Expr
{
    public Expr Numerator { get; } = numerator;
    public Expr Denominator { get; } = denominator;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Numerator.Evaluate(bindings) / Denominator.Evaluate(bindings);

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Numerator.EvaluateComplex(bindings) / Denominator.EvaluateComplex(bindings);

    public override Expr Differentiate(string variable) =>
        new Divide(
            new Subtract(
                new Multiply(Numerator.Differentiate(variable), Denominator),
                new Multiply(Numerator, Denominator.Differentiate(variable))
            ),
            new Power(Denominator, new Constant(2))
        ).Simplify();

    public override IReadOnlySet<string> GetVariables() =>
        (IReadOnlySet<string>)new HashSet<string>(Numerator.GetVariables().Union(Denominator.GetVariables()));

    public override Expr Substitute(string variable, Expr replacement) =>
        new Divide(Numerator.Substitute(variable, replacement), Denominator.Substitute(variable, replacement));

    public override string ToString() => $"({Numerator} / {Denominator})";
    public void Deconstruct(out Expr numerator, out Expr denominator) => (numerator, denominator) = (Numerator, Denominator);
}

public sealed class Power(Expr baseExpr, Expr exponent) : Expr
{
    public Expr Base { get; } = baseExpr;
    public Expr Exponent { get; } = exponent;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Pow(Base.Evaluate(bindings), Exponent.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Pow(Base.EvaluateComplex(bindings), Exponent.EvaluateComplex(bindings));

    public override Expr Differentiate(string variable)
    {
        if (Exponent is Constant n)
        {
            return new Multiply(
                new Multiply(n, new Power(Base, new Constant(n.Value - 1))),
                Base.Differentiate(variable)
            ).Simplify();
        }

        throw new NotImplementedException(
            $"Differentiation with non-constant exponent not yet supported (variable: {variable}).");
    }

    public override IReadOnlySet<string> GetVariables() =>
        (IReadOnlySet<string>)new HashSet<string>(Base.GetVariables().Union(Exponent.GetVariables()));

    public override Expr Substitute(string variable, Expr replacement) =>
        new Power(Base.Substitute(variable, replacement), Exponent.Substitute(variable, replacement));

    public override string ToString() => $"({Base} ^ {Exponent})";
    public void Deconstruct(out Expr baseExpr, out Expr exponent) => (baseExpr, exponent) = (Base, Exponent);
}