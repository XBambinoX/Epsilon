namespace Epsilon.Core;

public sealed class Sqrt(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sqrt(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Sqrt(Argument.EvaluateComplex(bindings));

    // d/dx sqrt(f(x)) = f'(x) / (2 * sqrt(f(x)))
    public override Expr Differentiate(string variable) =>
        new Divide(
            Argument.Differentiate(variable),
            new Multiply(new Constant(2), new Sqrt(Argument))
        );

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Sqrt(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sqrt({Argument})";
}

public sealed class NthRoot(Expr argument, Expr degree) : Expr
{
    public Expr Argument { get; } = argument;
    public Expr Degree { get; } = degree;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Pow(Argument.Evaluate(bindings), 1.0 / Degree.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Pow(Argument.EvaluateComplex(bindings), Complex.One / Degree.EvaluateComplex(bindings));

    public override Expr Differentiate(string variable)
    {
        if (Degree is Constant n)
        {
            Expr exponent = new Constant(Rational.One / n.Value);
            return new Multiply(
                new Multiply(exponent, new Power(Argument, new Subtract(exponent, new Constant(1)))),
                Argument.Differentiate(variable)
            );
        }

        throw new NotImplementedException($"Differentiation with non-constant root degree not yet supported (variable: {variable}).");
    }

    public override IReadOnlySet<string> GetVariables() =>
        (IReadOnlySet<string>)new HashSet<string>(Argument.GetVariables().Union(Degree.GetVariables()));

    public override Expr Substitute(string variable, Expr replacement) =>
        new NthRoot(Argument.Substitute(variable, replacement), Degree.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument, out Expr degree) => (argument, degree) = (Argument, Degree);
    public override string ToString() => $"nthroot({Argument}, {Degree})";
}