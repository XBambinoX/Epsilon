namespace Epsilon.Core;

public sealed class Sqrt(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(double x) => Math.Sqrt(Argument.Evaluate(x));

    // d/dx sqrt(f(x)) = f'(x) / (2 * sqrt(f(x)))
    public override Expr Differentiate() =>
        new Divide(
            Argument.Differentiate(),
            new Multiply(new Constant(2), new Sqrt(Argument))
        );

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sqrt({Argument})";
}

public sealed class NthRoot(Expr argument, Expr degree) : Expr
{
    public Expr Argument { get; } = argument;
    public Expr Degree { get; } = degree;

    public override double Evaluate(double x) =>
        Math.Pow(Argument.Evaluate(x), 1.0 / Degree.Evaluate(x));

    public override Expr Differentiate()
    {
        if (Degree is Constant n)
        {
            Expr exponent = new Constant(1.0 / n.Value);
            return new Multiply(
                new Multiply(exponent, new Power(Argument, new Subtract(exponent, new Constant(1)))),
                Argument.Differentiate()
            );
        }

        throw new NotImplementedException("Differentiation with non-constant root degree not yet supported.");
    }

    public void Deconstruct(out Expr argument, out Expr degree) => (argument, degree) = (Argument, Degree);
    public override string ToString() => $"nthroot({Argument}, {Degree})";
}