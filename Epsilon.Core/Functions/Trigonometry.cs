namespace Epsilon.Core;

public sealed class Sin(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sin(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Sin(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) => new Multiply(new Cos(Argument), Argument.Differentiate(variable));
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Sin(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sin({Argument})";
}

public sealed class Cos(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Cos(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Cos(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) =>
        new Multiply(new Subtract(new Constant(0), new Sin(Argument)), Argument.Differentiate(variable));
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Cos(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"cos({Argument})";
}

public sealed class Tan(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Tan(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Tan(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) =>
        new Divide(Argument.Differentiate(variable), new Power(new Cos(Argument), new Constant(2)));
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Tan(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"tan({Argument})";
}

public sealed class Cot(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 1.0 / Math.Tan(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.One / Complex.Tan(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) =>
        new Divide(
            new Subtract(new Constant(0), Argument.Differentiate(variable)),
            new Power(new Sin(Argument), new Constant(2))
        );
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Cot(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"cot({Argument})";
}

public sealed class Sec(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 1.0 / Math.Cos(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.One / Complex.Cos(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) =>
        new Multiply(new Multiply(new Sec(Argument), new Tan(Argument)), Argument.Differentiate(variable));
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Sec(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sec({Argument})";
}

public sealed class Csc(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 1.0 / Math.Sin(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.One / Complex.Sin(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) =>
        new Multiply(
            new Subtract(new Constant(0), new Multiply(new Csc(Argument), new Cot(Argument))),
            Argument.Differentiate(variable)
        );
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Csc(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"csc({Argument})";
}

public sealed class Asin(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Asin(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Asin(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) =>
        new Divide(
            Argument.Differentiate(variable),
            new Power(new Subtract(new Constant(1), new Power(Argument, new Constant(2))), new Constant(0.5))
        );
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Asin(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"asin({Argument})";
}

public sealed class Acos(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Acos(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Acos(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) =>
        new Divide(
            new Subtract(new Constant(0), Argument.Differentiate(variable)),
            new Power(new Subtract(new Constant(1), new Power(Argument, new Constant(2))), new Constant(0.5))
        );
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Acos(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"acos({Argument})";
}

public sealed class Atan(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Atan(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Atan(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) =>
        new Divide(Argument.Differentiate(variable), new Add(new Constant(1), new Power(Argument, new Constant(2))));
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Atan(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"atan({Argument})";
}

public sealed class Sinh(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sinh(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Sinh(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) => new Multiply(new Cosh(Argument), Argument.Differentiate(variable));
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Sinh(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sinh({Argument})";
}

public sealed class Cosh(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Cosh(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Cosh(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) => new Multiply(new Sinh(Argument), Argument.Differentiate(variable));
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Cosh(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"cosh({Argument})";
}

public sealed class Tanh(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Tanh(Argument.Evaluate(bindings));
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => Complex.Tanh(Argument.EvaluateComplex(bindings));
    public override Expr Differentiate(string variable) =>
        new Divide(Argument.Differentiate(variable), new Power(new Cosh(Argument), new Constant(2)));
    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) => new Tanh(Argument.Substitute(variable, replacement));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"tanh({Argument})";
}

public sealed class Asinh(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Asinh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Asinh(Argument.EvaluateComplex(bindings));

    // d/dx asinh(u) = u' / sqrt(u^2 + 1)
    public override Expr Differentiate(string variable) =>
        new Divide(
            Argument.Differentiate(variable),
            new Power(new Add(new Power(Argument, new Constant(2)), new Constant(1)), new Constant(0.5))
        );

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) =>
        new Asinh(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"asinh({Argument})";
}

public sealed class Acosh(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Acosh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Acosh(Argument.EvaluateComplex(bindings));

    // d/dx acosh(u) = u' / sqrt(u^2 - 1)   (domain: u > 1)
    public override Expr Differentiate(string variable) =>
        new Divide(
            Argument.Differentiate(variable),
            new Power(new Subtract(new Power(Argument, new Constant(2)), new Constant(1)), new Constant(0.5))
        );

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) =>
        new Acosh(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"acosh({Argument})";
}

public sealed class Atanh(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Atanh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Atanh(Argument.EvaluateComplex(bindings));

    // d/dx atanh(u) = u' / (1 - u^2)   (domain: |u| < 1)
    public override Expr Differentiate(string variable) =>
        new Divide(
            Argument.Differentiate(variable),
            new Subtract(new Constant(1), new Power(Argument, new Constant(2)))
        );

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) =>
        new Atanh(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"atanh({Argument})";
}

public sealed class Coth(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1.0 / Math.Tanh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Coth(Argument.EvaluateComplex(bindings));

    // d/dx coth(u) = -u' / sinh^2(u)
    public override Expr Differentiate(string variable) =>
        new Divide(
            new Subtract(new Constant(0), Argument.Differentiate(variable)),
            new Power(new Sinh(Argument), new Constant(2))
        );

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) =>
        new Coth(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"coth({Argument})";
}

public sealed class Sech(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1.0 / Math.Cosh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Sech(Argument.EvaluateComplex(bindings));

    // d/dx sech(u) = -u' * sech(u) * tanh(u)
    public override Expr Differentiate(string variable) =>
        new Multiply(
            new Multiply(new Subtract(new Constant(0), Argument.Differentiate(variable)), new Sech(Argument)),
            new Tanh(Argument)
        );

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) =>
        new Sech(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sech({Argument})";
}

public sealed class Csch(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1.0 / Math.Sinh(Argument.Evaluate(bindings));

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        Complex.Csch(Argument.EvaluateComplex(bindings));

    // d/dx csch(u) = -u' * csch(u) * coth(u)
    public override Expr Differentiate(string variable) =>
        new Multiply(
            new Multiply(new Subtract(new Constant(0), Argument.Differentiate(variable)), new Csch(Argument)),
            new Coth(Argument)
        );

    public override IReadOnlySet<string> GetVariables() => Argument.GetVariables();
    public override Expr Substitute(string variable, Expr replacement) =>
        new Csch(Argument.Substitute(variable, replacement));

    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"csch({Argument})";
}