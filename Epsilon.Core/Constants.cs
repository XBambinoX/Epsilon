namespace Epsilon.Core;

public sealed class Pi : Expr
{
    private static readonly IReadOnlySet<string> NoVariables = new HashSet<string>();

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.PI;
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => new Complex(Math.PI);
    public override Expr Differentiate(string variable) => new Constant(0);
    public override IReadOnlySet<string> GetVariables() => NoVariables;
    public override Expr Substitute(string variable, Expr replacement) => this;
    public void Deconstruct() { }
    public override string ToString() => "pi";
}

public sealed class E : Expr
{
    private static readonly IReadOnlySet<string> NoVariables = new HashSet<string>();

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.E;
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => new Complex(Math.E);
    public override Expr Differentiate(string variable) => new Constant(0);
    public override IReadOnlySet<string> GetVariables() => NoVariables;
    public override Expr Substitute(string variable, Expr replacement) => this;
    public void Deconstruct() { }
    public override string ToString() => "e";
}

public sealed class ImaginaryUnit : Expr
{
    private static readonly IReadOnlySet<string> NoVariables = new HashSet<string>();

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        throw new InvalidOperationException("The imaginary unit has no real value; use EvaluateComplex instead.");

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.ImaginaryUnit;

    public override Expr Differentiate(string variable) => new Constant(0);

    public override IReadOnlySet<string> GetVariables() => NoVariables;

    public override Expr Substitute(string variable, Expr replacement) => this;

    public void Deconstruct() { }

    public override string ToString() => "i";
}