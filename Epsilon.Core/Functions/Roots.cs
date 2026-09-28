namespace Epsilon.Core;

public sealed class Sqrt(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sqrt(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Sqrt(Argument.EvaluateComplex(bindings));

    // d/dx sqrt(f(x)) = f'(x) / (2 * sqrt(f(x)))
    protected override Expr DifferentiateCore(string variable) =>
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

    // nthroot is the REAL root: for an odd integer degree, a negative argument gives the
    // negative real root (nthroot(-8, 3) = -2), matching Simplify. This deliberately differs
    // from Power(x, 1/n), which is the principal value (undefined over the reals for x < 0).
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        RealRoot(Argument.Evaluate(bindings), Degree.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings)
    {
        Complex argument = Argument.EvaluateComplex(bindings);
        Complex degree = Degree.EvaluateComplex(bindings);

        // Stay consistent with Evaluate on the real line; elsewhere use the principal value.
        if (argument.Imaginary == 0 && degree.Imaginary == 0 && IsOddInteger(degree.Real))
            return new Complex(RealRoot(argument.Real, degree.Real));

        return Complex.Pow(argument, Complex.One / degree);
    }

    private static bool IsOddInteger(double value) =>
        double.IsInteger(value) && Math.Abs(value % 2) == 1;

    private static double RealRoot(double argument, double degree)
    {
        if (argument < 0 && IsOddInteger(degree))
            return -RealRoot(-argument, degree);

        double root = Math.Pow(argument, 1.0 / degree);

        // Neither Math.Pow(27, 1.0 / 3) nor even Math.Cbrt(27) is guaranteed to return
        // exactly 3 (glibc gives 3.0000000000000004). Snap to the nearest integer when it
        // is provably exact, so perfect powers give exact roots on every platform. Works for
        // non-integer degrees too: nthroot(8, 3/2) = 8^(2/3) snaps to 4 because 4^1.5 == 8.
        double nearest = Math.Round(root);
        if (Math.Pow(nearest, degree) == argument)
            return nearest;

        return root;
    }

    protected override Expr DifferentiateCore(string variable)
    {
        if (Degree is not Constant n)
            throw new NotImplementedException($"Differentiation with non-constant root degree not yet supported (variable: {variable}).");

        Expr fPrime = Argument.Differentiate(variable);

        // Written in terms of nthroot itself, so the derivative is real wherever the
        // function is (e.g. at negative arguments for odd degrees). A Power form such as
        // x^(1/3 - 1) would evaluate to NaN there.
        if (n.Value.IsInteger && n.Value.Sign > 0)
        {
            // d/dx nthroot(f, n) = f' / (n * nthroot(f, n)^(n-1))
            Expr root = new NthRoot(Argument, n);
            Expr rootPower = n.Value == 2 ? root : new Power(root, new Constant(n.Value - 1));
            return new Divide(fPrime, new Multiply(n, rootPower));
        }

        if (n.Value.IsInteger)
        {
            // Negative integer degree: d/dx nthroot(f, n) = f' * nthroot(f, n) / (n * f)
            return new Divide(new Multiply(fPrime, new NthRoot(Argument, n)), new Multiply(n, Argument));
        }

        // Non-integer degree: the real root coincides with the principal power.
        Expr exponent = new Constant(Rational.One / n.Value);
        return new Multiply(
            new Multiply(exponent, new Power(Argument, new Subtract(exponent, new Constant(1)))),
            fPrime);
    }

    public override IReadOnlySet<string> GetVariables() =>
        (IReadOnlySet<string>)new HashSet<string>(Argument.GetVariables().Union(Degree.GetVariables()));

    public override Expr Substitute(string variable, Expr replacement) =>
        new NthRoot(Argument.Substitute(variable, replacement), Degree.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument, out Expr degree) => (argument, degree) = (Argument, Degree);
    public override string ToString() => $"nthroot({Argument}, {Degree})";
}