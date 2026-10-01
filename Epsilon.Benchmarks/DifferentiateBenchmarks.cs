using BenchmarkDotNet.Attributes;
using Epsilon.Core;

namespace Epsilon.Benchmarks;

// Differentiate simplifies its result, so these mostly measure Simplify on the
// larger trees that the product, quotient and chain rules build.
[MemoryDiagnoser]
public class DifferentiateBenchmarks
{
    private readonly Expr _function = ExprParser.Parse("x^3 * sin(x) / (1 + x^2)");

    [Benchmark]
    public Expr FirstDerivative() => _function.Differentiate("x");

    [Benchmark]
    public Expr SecondDerivative() => _function.Differentiate("x").Differentiate("x");
}
