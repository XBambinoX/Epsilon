namespace Epsilon.Core;

public sealed class Abs(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Abs(Argument.Evaluate(bindings));

    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        Argument.EvaluateComplex(bindings).Magnitude;

    protected override Expr DifferentiateCore(string variable)
    {
        // d/dx |f(x)| = f(x) / |f(x)| * f'(x), for f(x) != 0
        return new Multiply(
            new Divide(Argument, new Abs(Argument)),
            Argument.Differentiate(variable)
        );
    }

    protected override Expr WithArgument(Expr argument) => new Abs(argument);

    public override string ToString() =>
        $"abs({Argument})";
}