using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Epsilon.Core;

namespace Epsilon.Benchmarks;

// Evaluate against the compiled delegate, over many points as in a plot or a root scan,
// and the one-time cost of compiling. Run: --filter '*Compile*' --job short.
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CompileBenchmarks
{
    private const int Points = 1000;

    // The formula of FormulaBenchmarks, and a derivative, which repeats subexpressions.
    private readonly Expr _formula = ExprParser.Parse("3x^2 * sin(x) + exp(-x/2) * cos(2x) - sqrt(x^2 + 1) / (1 + x)");
    private readonly Expr _derivative;
    // Only + - * /: no function calls, so the tree walk is nearly all of Evaluate's cost.
    private readonly Expr _arithmetic = ExprParser.Parse("(x + 1) * (x - 2) / (x * x + 3) - 4x / (x + 5)");
    private readonly Func<double, double> _compiledArithmetic;
    private readonly Func<double, double> _compiledFormula;
    private readonly Func<double, double> _compiledDerivative;
    private readonly double[] _xs = [.. Enumerable.Range(0, Points).Select(i => 0.01 * i)];

    public CompileBenchmarks()
    {
        _derivative = _formula.Differentiate("x");
        _compiledFormula = _formula.Compile();
        _compiledDerivative = _derivative.Compile();
        _compiledArithmetic = _arithmetic.Compile();
    }

    private double EvaluateAll(Expr expr)
    {
        var bindings = new Dictionary<string, double> { ["x"] = 0 };
        double sum = 0;
        foreach (double x in _xs)
        {
            bindings["x"] = x;
            sum += expr.Evaluate(bindings);
        }
        return sum;
    }

    private double CallAll(Func<double, double> f)
    {
        double sum = 0;
        foreach (double x in _xs)
            sum += f(x);
        return sum;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Formula1000")]
    public double EvaluateFormula() => EvaluateAll(_formula);

    [Benchmark, BenchmarkCategory("Formula1000")]
    public double CompiledFormula() => CallAll(_compiledFormula);

    [Benchmark(Baseline = true), BenchmarkCategory("Derivative1000")]
    public double EvaluateDerivative() => EvaluateAll(_derivative);

    [Benchmark, BenchmarkCategory("Derivative1000")]
    public double CompiledDerivative() => CallAll(_compiledDerivative);

    [Benchmark(Baseline = true), BenchmarkCategory("Arithmetic1000")]
    public double EvaluateArithmetic() => EvaluateAll(_arithmetic);

    [Benchmark, BenchmarkCategory("Arithmetic1000")]
    public double CompiledArithmetic() => CallAll(_compiledArithmetic);

    [Benchmark, BenchmarkCategory("CompileOnce")]
    public Func<double, double> CompileFormula() => _formula.Compile();

    [Benchmark, BenchmarkCategory("CompileOnce")]
    public Func<double, double> CompileDerivative() => _derivative.Compile();
}
