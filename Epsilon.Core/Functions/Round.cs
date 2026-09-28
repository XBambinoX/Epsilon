namespace Epsilon.Core;

public sealed class Round(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    // Halves round away from zero (round(2.5) = 3, round(-2.5) = -3), the same as
    // Rational.Round used by Simplify. Math.Round's default is banker's rounding
    // (to even), which would make Simplify and Evaluate disagree.
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Round(Argument.Evaluate(bindings), MidpointRounding.AwayFromZero);

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        throw new NotImplementedException("round(z) has no standard definition over the complex numbers.");

    protected override Expr DifferentiateCore(string variable) => 
        throw new NotImplementedException(
            "Round has a branch-dependent derivative and can't be represented " +
            "without a Piecewise/conditional Expr node.");

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();

    public override Expr Substitute(string variable, Expr replacement) =>
        new Round(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"round({Argument})";
}