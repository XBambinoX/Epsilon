namespace Epsilon.Core;

public abstract class Expr
{
    public abstract double Evaluate(IReadOnlyDictionary<string, double> bindings);

    public double Evaluate(double x) => Evaluate(SingleBinding(GetSingleVariable(), x));

    public double Evaluate(params (string Name, double Value)[] bindings)
    {
        var dict = new Dictionary<string, double>(bindings.Length);
        foreach (var (name, value) in bindings)
            dict[name] = value;

        return Evaluate(dict);
    }

    public abstract Expr Differentiate(string variable);

    public Expr Differentiate()
    {
        var vars = GetVariables();
        if (vars.Count == 0)
            return new Constant(0);

        if (vars.Count == 1)
            return Differentiate(vars.First());

        throw new InvalidOperationException(
            $"Expected exactly 1 variable, found {vars.Count}: [{string.Join(", ", vars)}]. " +
            "Use the explicit-variable overload for multivariable expressions.");
    }

    public virtual Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        throw new NotImplementedException($"{GetType().Name} does not yet support complex evaluation.");

    public Complex EvaluateComplex(Complex x) => EvaluateComplex(SingleBinding(GetSingleVariable(), x));

    public Complex EvaluateComplex(params (string Name, Complex Value)[] bindings)
    {
        var dict = new Dictionary<string, Complex>(bindings.Length);
        foreach (var (name, value) in bindings)
            dict[name] = value;

        return EvaluateComplex(dict);
    }

    public abstract IReadOnlySet<string> GetVariables();

    public virtual bool DependsOn(string variable) => GetVariables().Contains(variable);

    public abstract Expr Substitute(string variable, Expr replacement);

    public string GetSingleVariable()
    {
        var vars = GetVariables();

        if (vars.Count != 1)
            throw new InvalidOperationException(
                $"Expected exactly 1 variable, found {vars.Count}: [{string.Join(", ", vars)}]. " +
                "Use the explicit-variable overload for multivariable expressions.");

        return vars.First();
    }

    private static IReadOnlyDictionary<string, T> SingleBinding<T>(string variable, T value) =>
        new Dictionary<string, T> { [variable] = value };

    public override bool Equals(object? obj) =>
            obj is Expr other && StructurallyEquals(this, other);

    public override int GetHashCode() => StructuralHash(this);

    private static bool StructurallyEquals(Expr a, Expr b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.GetType() != b.GetType()) return false;

        return (a, b) switch
        {
            (Constant x, Constant y) => x.Value == y.Value,
            (Variable x, Variable y) => x.Name == y.Name,
            (Pi, Pi) => true,
            (E, E) => true,
            (ImaginaryUnit, ImaginaryUnit) => true,

            (Add(var l1, var r1), Add(var l2, var r2)) => l1.Equals(l2) && r1.Equals(r2),
            (Subtract(var l1, var r1), Subtract(var l2, var r2)) => l1.Equals(l2) && r1.Equals(r2),
            (Multiply(var l1, var r1), Multiply(var l2, var r2)) => l1.Equals(l2) && r1.Equals(r2),
            (Divide(var n1, var d1), Divide(var n2, var d2)) => n1.Equals(n2) && d1.Equals(d2),
            (Power(var b1, var e1), Power(var b2, var e2)) => b1.Equals(b2) && e1.Equals(e2),
            (Negate(var a1), Negate(var a2)) => a1.Equals(a2),

            (Sin(var a1), Sin(var a2)) => a1.Equals(a2),
            (Cos(var a1), Cos(var a2)) => a1.Equals(a2),
            (Tan(var a1), Tan(var a2)) => a1.Equals(a2),
            (Cot(var a1), Cot(var a2)) => a1.Equals(a2),
            (Sec(var a1), Sec(var a2)) => a1.Equals(a2),
            (Csc(var a1), Csc(var a2)) => a1.Equals(a2),
            (Asin(var a1), Asin(var a2)) => a1.Equals(a2),
            (Acos(var a1), Acos(var a2)) => a1.Equals(a2),
            (Atan(var a1), Atan(var a2)) => a1.Equals(a2),
            (Sinh(var a1), Sinh(var a2)) => a1.Equals(a2),
            (Cosh(var a1), Cosh(var a2)) => a1.Equals(a2),
            (Tanh(var a1), Tanh(var a2)) => a1.Equals(a2),
            (Asinh(var a1), Asinh(var a2)) => a1.Equals(a2),
            (Acosh(var a1), Acosh(var a2)) => a1.Equals(a2),
            (Atanh(var a1), Atanh(var a2)) => a1.Equals(a2),
            (Coth(var a1), Coth(var a2)) => a1.Equals(a2),
            (Sech(var a1), Sech(var a2)) => a1.Equals(a2),
            (Csch(var a1), Csch(var a2)) => a1.Equals(a2),
            (Exp(var a1), Exp(var a2)) => a1.Equals(a2),
            (Ln(var a1), Ln(var a2)) => a1.Equals(a2),
            (Sqrt(var a1), Sqrt(var a2)) => a1.Equals(a2),
            (Abs(var a1), Abs(var a2)) => a1.Equals(a2),
            (Sign(var a1), Sign(var a2)) => a1.Equals(a2),
            (Floor(var a1), Floor(var a2)) => a1.Equals(a2),
            (Ceiling(var a1), Ceiling(var a2)) => a1.Equals(a2),
            (Round(var a1), Round(var a2)) => a1.Equals(a2),

            (NthRoot(var a1, var n1), NthRoot(var a2, var n2)) => a1.Equals(a2) && n1.Equals(n2),
            (Min(var l1, var r1), Min(var l2, var r2)) => l1.Equals(l2) && r1.Equals(r2),
            (Max(var l1, var r1), Max(var l2, var r2)) => l1.Equals(l2) && r1.Equals(r2),

            _ => throw new NotSupportedException(
                $"Equals is not implemented for {a.GetType().Name} — add a case to StructurallyEquals.")
        };
    }

    private static int StructuralHash(Expr expr) => expr switch
    {
        Constant c => HashCode.Combine(nameof(Constant), c.Value),
        Variable v => HashCode.Combine(nameof(Variable), v.Name),
        Pi => nameof(Pi).GetHashCode(),
        E => nameof(E).GetHashCode(),
        ImaginaryUnit => nameof(ImaginaryUnit).GetHashCode(),

        Add(var l, var r) => HashCode.Combine(nameof(Add), l, r),
        Subtract(var l, var r) => HashCode.Combine(nameof(Subtract), l, r),
        Multiply(var l, var r) => HashCode.Combine(nameof(Multiply), l, r),
        Divide(var n, var d) => HashCode.Combine(nameof(Divide), n, d),
        Power(var b, var e) => HashCode.Combine(nameof(Power), b, e),
        Negate(var a) => HashCode.Combine(nameof(Negate), a),

        Sin(var a) => HashCode.Combine(nameof(Sin), a),
        Cos(var a) => HashCode.Combine(nameof(Cos), a),
        Tan(var a) => HashCode.Combine(nameof(Tan), a),
        Cot(var a) => HashCode.Combine(nameof(Cot), a),
        Sec(var a) => HashCode.Combine(nameof(Sec), a),
        Csc(var a) => HashCode.Combine(nameof(Csc), a),
        Asin(var a) => HashCode.Combine(nameof(Asin), a),
        Acos(var a) => HashCode.Combine(nameof(Acos), a),
        Atan(var a) => HashCode.Combine(nameof(Atan), a),
        Sinh(var a) => HashCode.Combine(nameof(Sinh), a),
        Cosh(var a) => HashCode.Combine(nameof(Cosh), a),
        Tanh(var a) => HashCode.Combine(nameof(Tanh), a),
        Asinh(var a) => HashCode.Combine(nameof(Asinh), a),
        Acosh(var a) => HashCode.Combine(nameof(Acosh), a),
        Atanh(var a) => HashCode.Combine(nameof(Atanh), a),
        Coth(var a) => HashCode.Combine(nameof(Coth), a),
        Sech(var a) => HashCode.Combine(nameof(Sech), a),
        Csch(var a) => HashCode.Combine(nameof(Csch), a),
        Exp(var a) => HashCode.Combine(nameof(Exp), a),
        Ln(var a) => HashCode.Combine(nameof(Ln), a),
        Sqrt(var a) => HashCode.Combine(nameof(Sqrt), a),
        Abs(var a) => HashCode.Combine(nameof(Abs), a),
        Sign(var a) => HashCode.Combine(nameof(Sign), a),
        Floor(var a) => HashCode.Combine(nameof(Floor), a),
        Ceiling(var a) => HashCode.Combine(nameof(Ceiling), a),
        Round(var a) => HashCode.Combine(nameof(Round), a),

        NthRoot(var a, var n) => HashCode.Combine(nameof(NthRoot), a, n),
        Min(var l, var r) => HashCode.Combine(nameof(Min), l, r),
        Max(var l, var r) => HashCode.Combine(nameof(Max), l, r),

        _ => throw new NotSupportedException(
            $"GetHashCode is not implemented for {expr.GetType().Name} — add a case to StructuralHash.")
    };
}

public sealed class Constant : Expr
{
    public Rational Value { get; }

    private static readonly IReadOnlySet<string> NoVariables = new HashSet<string>();

    public Constant(Rational value) => Value = value;
    public Constant(double value) : this(Rational.FromDouble(value)) { }
    public Constant(int value) : this(new Rational(value)) { }

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Value.ToDouble();
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => new Complex(Value.ToDouble());
    public override Expr Differentiate(string variable) => new Constant(0);
    public override IReadOnlySet<string> GetVariables() => NoVariables;
    public override Expr Substitute(string variable, Expr replacement) => this;
    public override string ToString() => Value.ToString();
}

public sealed class Variable(string name) : Expr
{
    public string Name { get; } = name;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings)
    {
        if (!bindings.TryGetValue(Name, out double value))
            throw new ArgumentException($"No binding provided for variable '{Name}'.");

        return value;
    }

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings)
    {
        if (!bindings.TryGetValue(Name, out Complex value))
            throw new ArgumentException($"No binding provided for variable '{Name}'.");

        return value;
    }

    public override Expr Differentiate(string variable) => new Constant(variable == Name ? 1 : 0);

    public override IReadOnlySet<string> GetVariables() => new HashSet<string> { Name };

    public override Expr Substitute(string variable, Expr replacement) => variable == Name ? replacement : this;

    public override string ToString() => Name;
}