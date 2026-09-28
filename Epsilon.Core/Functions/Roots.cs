namespace Epsilon.Core;

/// <summary>The principal square root <c>sqrt(x)</c>; NaN over the reals for x &lt; 0, <c>i*sqrt(-x)</c> over the complex numbers.</summary>
/// <param name="argument">The argument.</param>
public sealed class Sqrt(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sqrt(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Sqrt(Argument.EvaluateComplex(bindings));

    // d/dx sqrt(f(x)) = f'(x) / (2 * sqrt(f(x)))
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            DerivativeOf(Argument, variable),
            new Multiply(new Constant(2), new Sqrt(Argument))
        );

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Sqrt(argument);

    /// <inheritdoc/>
    public override string ToString() => $"sqrt({Argument})";
}

/// <summary>
/// The real n-th root <c>nthroot(x, n)</c>. For an odd integer degree a negative argument gives
/// the negative real root (<c>nthroot(-8, 3) = -2</c>), unlike <c>x^(1/3)</c> (see <see cref="Power"/>).
/// </summary>
/// <param name="argument">The radicand.</param>
/// <param name="degree">The root degree n.</param>
public sealed class NthRoot(Expr argument, Expr degree) : Expr
{
    private readonly ImmutableArray<Expr> _children = [argument, degree];

    /// <summary>The radicand.</summary>
    public Expr Argument { get; } = argument;
    /// <summary>The root degree n.</summary>
    public Expr Degree { get; } = degree;

    // nthroot is the REAL root: for an odd integer degree, a negative argument gives the
    // negative real root (nthroot(-8, 3) = -2), matching Simplify. This deliberately differs
    // from Power(x, 1/n), which is the principal value (undefined over the reals for x < 0).
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        RealRoot(Argument.Evaluate(bindings), Degree.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings)
    {
        ComplexNumber argument = Argument.EvaluateComplex(bindings);
        ComplexNumber degree = Degree.EvaluateComplex(bindings);

        // Stay consistent with Evaluate on the real line; elsewhere use the principal value.
        if (argument.Imaginary == 0 && degree.Imaginary == 0 && IsOddInteger(degree.Real))
            return new ComplexNumber(RealRoot(argument.Real, degree.Real));

        return ComplexNumber.Pow(argument, ComplexNumber.One / degree);
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

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable)
    {
        if (Degree is not Constant n)
            throw new NotSupportedException(
                $"The derivative of nthroot with a degree that depends on '{variable}' is not supported yet " +
                "(planned for a future version). Use a constant degree.");

        Expr fPrime = DerivativeOf(Argument, variable);

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

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => _children;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) =>
        WithTwoChildren(children, static (a, b) => new NthRoot(a, b));

    /// <summary>Deconstructs the node for positional patterns: <c>case NthRoot(var x, var n):</c>.</summary>
    public void Deconstruct(out Expr argument, out Expr degree) => (argument, degree) = (Argument, Degree);
    /// <inheritdoc/>
    public override string ToString() => $"nthroot({Argument}, {Degree})";
}