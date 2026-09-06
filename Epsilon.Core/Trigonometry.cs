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