namespace Epsilon.Core;

public sealed class Sin(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sin(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Sin(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) => new Multiply(new Cos(Argument), Argument.Differentiate(variable));
    protected override Expr WithArgument(Expr argument) => new Sin(argument);
    public override string ToString() => $"sin({Argument})";
}

public sealed class Cos(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Cos(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Cos(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(new Negate(new Sin(Argument)), Argument.Differentiate(variable));
    protected override Expr WithArgument(Expr argument) => new Cos(argument);
    public override string ToString() => $"cos({Argument})";
}

public sealed class Tan(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Tan(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Tan(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(Argument.Differentiate(variable), new Power(new Cos(Argument), new Constant(2)));
    protected override Expr WithArgument(Expr argument) => new Tan(argument);
    public override string ToString() => $"tan({Argument})";
}

public sealed class Cot(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 1.0 / Math.Tan(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.One / Complex.Tan(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            new Negate(Argument.Differentiate(variable)),
            new Power(new Sin(Argument), new Constant(2))
        );
    protected override Expr WithArgument(Expr argument) => new Cot(argument);
    public override string ToString() => $"cot({Argument})";
}

public sealed class Sec(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 1.0 / Math.Cos(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.One / Complex.Cos(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(new Multiply(new Sec(Argument), new Tan(Argument)), Argument.Differentiate(variable));
    protected override Expr WithArgument(Expr argument) => new Sec(argument);
    public override string ToString() => $"sec({Argument})";
}

public sealed class Csc(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 1.0 / Math.Sin(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.One / Complex.Sin(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(
            new Negate(new Multiply(new Csc(Argument), new Cot(Argument))),
            Argument.Differentiate(variable)
        );
    protected override Expr WithArgument(Expr argument) => new Csc(argument);
    public override string ToString() => $"csc({Argument})";
}

public sealed class Asin(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Asin(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Asin(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            Argument.Differentiate(variable),
            new Power(new Subtract(new Constant(1), new Power(Argument, new Constant(2))), new Constant(0.5))
        );
    protected override Expr WithArgument(Expr argument) => new Asin(argument);
    public override string ToString() => $"asin({Argument})";
}

public sealed class Acos(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Acos(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Acos(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            new Negate(Argument.Differentiate(variable)),
            new Power(new Subtract(new Constant(1), new Power(Argument, new Constant(2))), new Constant(0.5))
        );
    protected override Expr WithArgument(Expr argument) => new Acos(argument);
    public override string ToString() => $"acos({Argument})";
}

public sealed class Atan(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Atan(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Atan(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(Argument.Differentiate(variable), new Add(new Constant(1), new Power(Argument, new Constant(2))));
    protected override Expr WithArgument(Expr argument) => new Atan(argument);
    public override string ToString() => $"atan({Argument})";
}

public sealed class Sinh(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sinh(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Sinh(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) => new Multiply(new Cosh(Argument), Argument.Differentiate(variable));
    protected override Expr WithArgument(Expr argument) => new Sinh(argument);
    public override string ToString() => $"sinh({Argument})";
}

public sealed class Cosh(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Cosh(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Cosh(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) => new Multiply(new Sinh(Argument), Argument.Differentiate(variable));
    protected override Expr WithArgument(Expr argument) => new Cosh(argument);
    public override string ToString() => $"cosh({Argument})";
}

public sealed class Tanh(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Tanh(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Tanh(Argument.EvaluateComplex(bindings));
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(Argument.Differentiate(variable), new Power(new Cosh(Argument), new Constant(2)));
    protected override Expr WithArgument(Expr argument) => new Tanh(argument);
    public override string ToString() => $"tanh({Argument})";
}

public sealed class Asinh(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Asinh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Asinh(Argument.EvaluateComplex(bindings));

    // d/dx asinh(u) = u' / sqrt(u^2 + 1)
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            Argument.Differentiate(variable),
            new Power(new Add(new Power(Argument, new Constant(2)), new Constant(1)), new Constant(0.5))
        );

    protected override Expr WithArgument(Expr argument) => new Asinh(argument);

    public override string ToString() => $"asinh({Argument})";
}

public sealed class Acosh(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Acosh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Acosh(Argument.EvaluateComplex(bindings));

    // d/dx acosh(u) = u' / sqrt(u^2 - 1)   (domain: u > 1)
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            Argument.Differentiate(variable),
            new Power(new Subtract(new Power(Argument, new Constant(2)), new Constant(1)), new Constant(0.5))
        );

    protected override Expr WithArgument(Expr argument) => new Acosh(argument);

    public override string ToString() => $"acosh({Argument})";
}

public sealed class Atanh(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Atanh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Atanh(Argument.EvaluateComplex(bindings));

    // d/dx atanh(u) = u' / (1 - u^2)   (domain: |u| < 1)
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            Argument.Differentiate(variable),
            new Subtract(new Constant(1), new Power(Argument, new Constant(2)))
        );

    protected override Expr WithArgument(Expr argument) => new Atanh(argument);

    public override string ToString() => $"atanh({Argument})";
}

public sealed class Coth(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1.0 / Math.Tanh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Coth(Argument.EvaluateComplex(bindings));

    // d/dx coth(u) = -u' / sinh^2(u)
    protected override Expr DifferentiateCore(string variable) =>
        new Divide(
            new Negate(Argument.Differentiate(variable)),
            new Power(new Sinh(Argument), new Constant(2))
        );

    protected override Expr WithArgument(Expr argument) => new Coth(argument);

    public override string ToString() => $"coth({Argument})";
}

public sealed class Sech(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1.0 / Math.Cosh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Sech(Argument.EvaluateComplex(bindings));

    // d/dx sech(u) = -u' * sech(u) * tanh(u)
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(
            new Multiply(new Negate(Argument.Differentiate(variable)), new Sech(Argument)),
            new Tanh(Argument)
        );

    protected override Expr WithArgument(Expr argument) => new Sech(argument);

    public override string ToString() => $"sech({Argument})";
}

public sealed class Csch(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1.0 / Math.Sinh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Csch(Argument.EvaluateComplex(bindings));

    // d/dx csch(u) = -u' * csch(u) * coth(u)
    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(
            new Multiply(new Negate(Argument.Differentiate(variable)), new Csch(Argument)),
            new Coth(Argument)
        );

    protected override Expr WithArgument(Expr argument) => new Csch(argument);

    public override string ToString() => $"csch({Argument})";
}