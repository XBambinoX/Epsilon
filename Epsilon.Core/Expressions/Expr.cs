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

    public override bool Equals(object? obj) => obj is Expr other && ToString() == other.ToString();
    public override int GetHashCode() => ToString().GetHashCode();
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