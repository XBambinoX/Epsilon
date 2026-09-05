namespace Epsilon.Core;

public sealed class Sin(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => Math.Sin(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.Sin(Argument.EvaluateComplex(x));
    public override Expr Differentiate() => new Multiply(new Cos(Argument), Argument.Differentiate());
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sin({Argument})";
}

public sealed class Cos(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => Math.Cos(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.Cos(Argument.EvaluateComplex(x));
    public override Expr Differentiate() =>
        new Multiply(new Subtract(new Constant(0), new Sin(Argument)), Argument.Differentiate());
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"cos({Argument})";
}

public sealed class Tan(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => Math.Tan(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.Tan(Argument.EvaluateComplex(x));
    public override Expr Differentiate() =>
        new Divide(Argument.Differentiate(), new Power(new Cos(Argument), new Constant(2)));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"tan({Argument})";
}

public sealed class Cot(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => 1.0 / Math.Tan(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.One / Complex.Tan(Argument.EvaluateComplex(x));
    public override Expr Differentiate() =>
        new Divide(
            new Subtract(new Constant(0), Argument.Differentiate()),
            new Power(new Sin(Argument), new Constant(2))
        );
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"cot({Argument})";
}

public sealed class Sec(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => 1.0 / Math.Cos(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.One / Complex.Cos(Argument.EvaluateComplex(x));
    public override Expr Differentiate() =>
        new Multiply(new Multiply(new Sec(Argument), new Tan(Argument)), Argument.Differentiate());
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sec({Argument})";
}

public sealed class Csc(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => 1.0 / Math.Sin(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.One / Complex.Sin(Argument.EvaluateComplex(x));
    public override Expr Differentiate() =>
        new Multiply(
            new Subtract(new Constant(0), new Multiply(new Csc(Argument), new Cot(Argument))),
            Argument.Differentiate()
        );
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"csc({Argument})";
}

public sealed class Asin(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => Math.Asin(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.Asin(Argument.EvaluateComplex(x));
    public override Expr Differentiate() =>
        new Divide(
            Argument.Differentiate(),
            new Power(new Subtract(new Constant(1), new Power(Argument, new Constant(2))), new Constant(0.5))
        );
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"asin({Argument})";
}

public sealed class Acos(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => Math.Acos(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.Acos(Argument.EvaluateComplex(x));
    public override Expr Differentiate() =>
        new Divide(
            new Subtract(new Constant(0), Argument.Differentiate()),
            new Power(new Subtract(new Constant(1), new Power(Argument, new Constant(2))), new Constant(0.5))
        );
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"acos({Argument})";
}

public sealed class Atan(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => Math.Atan(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.Atan(Argument.EvaluateComplex(x));
    public override Expr Differentiate() =>
        new Divide(Argument.Differentiate(), new Add(new Constant(1), new Power(Argument, new Constant(2))));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"atan({Argument})";
}

public sealed class Sinh(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => Math.Sinh(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.Sinh(Argument.EvaluateComplex(x));
    public override Expr Differentiate() => new Multiply(new Cosh(Argument), Argument.Differentiate());
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"sinh({Argument})";
}

public sealed class Cosh(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => Math.Cosh(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.Cosh(Argument.EvaluateComplex(x));
    public override Expr Differentiate() => new Multiply(new Sinh(Argument), Argument.Differentiate());
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"cosh({Argument})";
}

public sealed class Tanh(Expr argument) : Expr
{
    public Expr Argument { get; } = argument;
    public override double Evaluate(double x) => Math.Tanh(Argument.Evaluate(x));
    public override Complex EvaluateComplex(Complex x) => Complex.Tanh(Argument.EvaluateComplex(x));
    public override Expr Differentiate() =>
        new Divide(Argument.Differentiate(), new Power(new Cosh(Argument), new Constant(2)));
    public void Deconstruct(out Expr argument) => argument = Argument;
    public override string ToString() => $"tanh({Argument})";
}