namespace Epsilon.Core;

/// <summary>The sine <c>sin(x)</c>, x in radians.</summary>
/// <param name="argument">The angle in radians.</param>
public sealed class Sin(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sin(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Sin(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => new Multiply(new Cos(Argument), DerivativeOf(Argument, variable));
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Sin(argument);
    /// <inheritdoc/>
    public override string ToString() => $"sin({Argument})";
}

/// <summary>The cosine <c>cos(x)</c>, x in radians.</summary>
/// <param name="argument">The angle in radians.</param>
public sealed class Cos(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Cos(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Cos(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(new Negate(new Sin(Argument)), DerivativeOf(Argument, variable));
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Cos(argument);
    /// <inheritdoc/>
    public override string ToString() => $"cos({Argument})";
}

/// <summary>The tangent <c>tan(x)</c>, x in radians.</summary>
/// <param name="argument">The angle in radians.</param>
public sealed class Tan(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Tan(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Tan(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(DerivativeOf(Argument, variable), new Power(new Cos(Argument), new Constant(2)));
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Tan(argument);
    /// <inheritdoc/>
    public override string ToString() => $"tan({Argument})";
}

/// <summary>The cotangent <c>cot(x) = 1 / tan(x)</c>, x in radians.</summary>
/// <param name="argument">The angle in radians.</param>
public sealed class Cot(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 1.0 / Math.Tan(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.One / ComplexNumber.Tan(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            new Negate(DerivativeOf(Argument, variable)),
            new Power(new Sin(Argument), new Constant(2))
        );
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Cot(argument);
    /// <inheritdoc/>
    public override string ToString() => $"cot({Argument})";
}

/// <summary>The secant <c>sec(x) = 1 / cos(x)</c>, x in radians.</summary>
/// <param name="argument">The angle in radians.</param>
public sealed class Sec(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 1.0 / Math.Cos(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.One / ComplexNumber.Cos(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(new Multiply(new Sec(Argument), new Tan(Argument)), DerivativeOf(Argument, variable));
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Sec(argument);
    /// <inheritdoc/>
    public override string ToString() => $"sec({Argument})";
}

/// <summary>The cosecant <c>csc(x) = 1 / sin(x)</c>, x in radians.</summary>
/// <param name="argument">The angle in radians.</param>
public sealed class Csc(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 1.0 / Math.Sin(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.One / ComplexNumber.Sin(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(
            new Negate(new Multiply(new Csc(Argument), new Cot(Argument))),
            DerivativeOf(Argument, variable)
        );
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Csc(argument);
    /// <inheritdoc/>
    public override string ToString() => $"csc({Argument})";
}

/// <summary>The arcsine <c>asin(x)</c> in radians, in [-pi/2, pi/2]; NaN over the reals for |x| &gt; 1.</summary>
/// <param name="argument">The argument.</param>
public sealed class Asin(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Asin(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Asin(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            DerivativeOf(Argument, variable),
            new Power(new Subtract(new Constant(1), new Power(Argument, new Constant(2))), new Constant(0.5))
        );
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Asin(argument);
    /// <inheritdoc/>
    public override string ToString() => $"asin({Argument})";
}

/// <summary>The arccosine <c>acos(x)</c> in radians, in [0, pi]; NaN over the reals for |x| &gt; 1.</summary>
/// <param name="argument">The argument.</param>
public sealed class Acos(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Acos(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Acos(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            new Negate(DerivativeOf(Argument, variable)),
            new Power(new Subtract(new Constant(1), new Power(Argument, new Constant(2))), new Constant(0.5))
        );
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Acos(argument);
    /// <inheritdoc/>
    public override string ToString() => $"acos({Argument})";
}

/// <summary>The arctangent <c>atan(x)</c> in radians, in (-pi/2, pi/2).</summary>
/// <param name="argument">The argument.</param>
public sealed class Atan(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Atan(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Atan(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(DerivativeOf(Argument, variable), new Add(new Constant(1), new Power(Argument, new Constant(2))));
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Atan(argument);
    /// <inheritdoc/>
    public override string ToString() => $"atan({Argument})";
}

/// <summary>The hyperbolic sine <c>sinh(x)</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Sinh(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sinh(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Sinh(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => new Multiply(new Cosh(Argument), DerivativeOf(Argument, variable));
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Sinh(argument);
    /// <inheritdoc/>
    public override string ToString() => $"sinh({Argument})";
}

/// <summary>The hyperbolic cosine <c>cosh(x)</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Cosh(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Cosh(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Cosh(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => new Multiply(new Sinh(Argument), DerivativeOf(Argument, variable));
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Cosh(argument);
    /// <inheritdoc/>
    public override string ToString() => $"cosh({Argument})";
}

/// <summary>The hyperbolic tangent <c>tanh(x)</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Tanh(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Tanh(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Tanh(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(DerivativeOf(Argument, variable), new Power(new Cosh(Argument), new Constant(2)));
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Tanh(argument);
    /// <inheritdoc/>
    public override string ToString() => $"tanh({Argument})";
}

/// <summary>The inverse hyperbolic sine <c>asinh(x)</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Asinh(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Asinh(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        ComplexNumber.Asinh(Argument.EvaluateComplex(bindings));

    // d/dx asinh(u) = u' / sqrt(u^2 + 1)
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            DerivativeOf(Argument, variable),
            new Power(new Add(new Power(Argument, new Constant(2)), new Constant(1)), new Constant(0.5))
        );

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Asinh(argument);

    /// <inheritdoc/>
    public override string ToString() => $"asinh({Argument})";
}

/// <summary>The inverse hyperbolic cosine <c>acosh(x)</c>; NaN over the reals for x &lt; 1.</summary>
/// <param name="argument">The argument.</param>
public sealed class Acosh(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Acosh(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        ComplexNumber.Acosh(Argument.EvaluateComplex(bindings));

    // d/dx acosh(u) = u' / sqrt(u^2 - 1)   (domain: u > 1)
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            DerivativeOf(Argument, variable),
            new Power(new Subtract(new Power(Argument, new Constant(2)), new Constant(1)), new Constant(0.5))
        );

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Acosh(argument);

    /// <inheritdoc/>
    public override string ToString() => $"acosh({Argument})";
}

/// <summary>The inverse hyperbolic tangent <c>atanh(x)</c>; NaN over the reals for |x| &gt; 1.</summary>
/// <param name="argument">The argument.</param>
public sealed class Atanh(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Atanh(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        ComplexNumber.Atanh(Argument.EvaluateComplex(bindings));

    // d/dx atanh(u) = u' / (1 - u^2)   (domain: |u| < 1)
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            DerivativeOf(Argument, variable),
            new Subtract(new Constant(1), new Power(Argument, new Constant(2)))
        );

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Atanh(argument);

    /// <inheritdoc/>
    public override string ToString() => $"atanh({Argument})";
}

/// <summary>The hyperbolic cotangent <c>coth(x) = 1 / tanh(x)</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Coth(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1.0 / Math.Tanh(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        ComplexNumber.Coth(Argument.EvaluateComplex(bindings));

    // d/dx coth(u) = -u' / sinh^2(u)
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            new Negate(DerivativeOf(Argument, variable)),
            new Power(new Sinh(Argument), new Constant(2))
        );

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Coth(argument);

    /// <inheritdoc/>
    public override string ToString() => $"coth({Argument})";
}

/// <summary>The hyperbolic secant <c>sech(x) = 1 / cosh(x)</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Sech(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1.0 / Math.Cosh(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        ComplexNumber.Sech(Argument.EvaluateComplex(bindings));

    // d/dx sech(u) = -u' * sech(u) * tanh(u)
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(
            new Multiply(new Negate(DerivativeOf(Argument, variable)), new Sech(Argument)),
            new Tanh(Argument)
        );

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Sech(argument);

    /// <inheritdoc/>
    public override string ToString() => $"sech({Argument})";
}

/// <summary>The hyperbolic cosecant <c>csch(x) = 1 / sinh(x)</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Csch(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1.0 / Math.Sinh(Argument.Evaluate(bindings));

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        ComplexNumber.Csch(Argument.EvaluateComplex(bindings));

    // d/dx csch(u) = -u' * csch(u) * coth(u)
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(
            new Multiply(new Negate(DerivativeOf(Argument, variable)), new Csch(Argument)),
            new Coth(Argument)
        );

    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Csch(argument);

    /// <inheritdoc/>
    public override string ToString() => $"csch({Argument})";
}