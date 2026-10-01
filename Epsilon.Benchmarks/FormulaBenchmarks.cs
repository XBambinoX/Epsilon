using BenchmarkDotNet.Attributes;
using Epsilon.Core;

namespace Epsilon.Benchmarks;

// Everyday-sized formulas: the cost of one call in typical use.
[MemoryDiagnoser]
public class FormulaBenchmarks
{
    private const string Formula = "3x^2 * sin(x) + exp(-x/2) * cos(2x) - sqrt(x^2 + 1) / (1 + x)";

    private readonly Expr _formula = ExprParser.Parse(Formula);
    private readonly Dictionary<string, double> _bindings = new() { ["x"] = 0.5 };

    // Exercises the main rule families: identities, like factors, cancelling, constants.
    private readonly Expr _simplifiable =
        ExprParser.Parse("sin(x)^2 + cos(x)^2 + 2x * 3x - x/x + (x^2 * y) / (x * y) + 2^3 * x");

    [Benchmark]
    public Expr Parse() => ExprParser.Parse(Formula);

    [Benchmark]
    public double Evaluate() => _formula.Evaluate(_bindings);

    [Benchmark]
    public string Print() => _formula.Print();

    [Benchmark]
    public Expr Simplify() => _simplifiable.Simplify();
}
