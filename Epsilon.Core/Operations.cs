namespace Epsilon.Core;

public sealed class Add(Expr left, Expr right) : Expr
{
    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(double x) => Left.Evaluate(x) + Right.Evaluate(x);
    public override Expr Differentiate() => new Add(Left.Differentiate(), Right.Differentiate()).Simplify();
    public override string ToString() => $"({Left} + {Right})";
    
    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

public sealed class Subtract(Expr left, Expr right) : Expr
{
    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(double x) => Left.Evaluate(x) - Right.Evaluate(x);
    public override Expr Differentiate() => new Subtract(Left.Differentiate(), Right.Differentiate()).Simplify();
    public override string ToString() => $"({Left} - {Right})";

    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

public sealed class Multiply(Expr left, Expr right) : Expr
{
    public Expr Left { get; } = left;
    public Expr Right { get; } = right;

    public override double Evaluate(double x) => Left.Evaluate(x) * Right.Evaluate(x);

    public override Expr Differentiate() =>
        new Add(
            new Multiply(Left.Differentiate(), Right),
            new Multiply(Left, Right.Differentiate())
        ).Simplify();

    public override string ToString() => $"({Left} * {Right})";

    public void Deconstruct(out Expr left, out Expr right) => (left, right) = (Left, Right);
}

public sealed class Divide(Expr numerator, Expr denominator) : Expr
{
    public Expr Numerator { get; } = numerator;
    public Expr Denominator { get; } = denominator;

    public override double Evaluate(double x) => Numerator.Evaluate(x) / Denominator.Evaluate(x);

    public override Expr Differentiate() =>
        new Divide(
            new Subtract(
                new Multiply(Numerator.Differentiate(), Denominator),
                new Multiply(Numerator, Denominator.Differentiate())
            ),
            new Power(Denominator, new Constant(2))
        ).Simplify();

    public override string ToString() => $"({Numerator} / {Denominator})";
    
    public void Deconstruct(out Expr numerator, out Expr denominator) => (numerator, denominator) = (Numerator, Denominator);
}

public sealed class Power(Expr baseExpr, Expr exponent) : Expr
{
    public Expr Base { get; } = baseExpr;
    public Expr Exponent { get; } = exponent;

    public override double Evaluate(double x) => Math.Pow(Base.Evaluate(x), Exponent.Evaluate(x));

    public override Expr Differentiate()
    {
        if (Exponent is Constant n)
        {
            return new Multiply(
                new Multiply(n, new Power(Base, new Constant(n.Value - 1))),
                Base.Differentiate()
            ).Simplify();
        }

        throw new NotImplementedException("Differentiation with non-constant exponent not yet supported.");
    }

    public override string ToString() => $"({Base} ^ {Exponent})";

    public void Deconstruct(out Expr baseExpr, out Expr exponent) => (baseExpr, exponent) = (Base, Exponent);
}