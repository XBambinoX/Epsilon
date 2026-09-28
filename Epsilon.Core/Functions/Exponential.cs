namespace Epsilon.Core;

public sealed class Exp(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Exp(Argument.Evaluate(bindings));
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Exp(Argument.EvaluateComplex(bindings));

    // d/dx e^f(x) = e^f(x) * f'(x)
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(new Exp(Argument), Argument.Differentiate(variable)).Simplify();

    protected override Expr WithArgument(Expr argument) => new Exp(argument);

    public override string ToString() => $"exp({Argument})";
}

public sealed class Ln(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Log(Argument.Evaluate(bindings));
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Log(Argument.EvaluateComplex(bindings));

    // d/dx ln(f(x)) = f'(x) / f(x)
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(Argument.Differentiate(variable), Argument).Simplify();

    protected override Expr WithArgument(Expr argument) => new Ln(argument);

    public override string ToString() => $"ln({Argument})";
}