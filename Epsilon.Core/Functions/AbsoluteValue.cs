namespace Epsilon.Core;

public sealed class Abs(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Abs(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Argument.EvaluateComplex(bindings).Magnitude;

    public override Expr Differentiate(string variable)
    {
        // d/dx |f(x)| = f(x) / |f(x)| * f'(x), for f(x) != 0
        return new Multiply(
            new Divide(Argument, new Abs(Argument)),
            Argument.Differentiate(variable)
        );
    }

    public override IReadOnlySet<string> GetVariables() =>
        Argument.GetVariables();

    public override Expr Substitute(string variable, Expr replacement) =>
        new Abs(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) =>
        argument = Argument;

    public override string ToString() =>
        $"abs({Argument})";
}