using BenchmarkDotNet.Attributes;
using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Benchmarks;

// What the packages spend their time on, mostly Simplify on the trees they build. A short run is
// enough to compare two versions: ... -- --filter '*Workload*' --job short
[MemoryDiagnoser]
public class WorkloadBenchmarks
{
    private static readonly string[] Antiderivatives =
        ["x^2*cos(x)*exp(-x)", "1/(x^4 + 1)", "x^2/sqrt(x^2 - 2*x)", "exp(x)*cos(x)^2", "1/(2 + sin(x))", "ln(x)^2", "x*asin(x)", "1/(x^3 - x - 1)"];

    private static readonly (string F, string A, string B)[] Definite =
        [("1/(x^3 + 1)", "0", "1"), ("1/(2 + cos(x))", "0", "pi"), ("x*exp(-x)", "0", "1"), ("1/(x^2 - 2)", "0", "1"), ("sqrt(1 - x^2)", "-1", "1")];

    private static readonly (string F, double Point)[] Limits =
    [
        ("(exp(x) + x)^(1/x)", double.PositiveInfinity), ("(1 + 1/x)^x", double.PositiveInfinity), ("(tan(x) - x)/x^3", 0),
        ("x - sqrt(x^2 - x)", double.PositiveInfinity), ("exp(x)*(exp(1/x - exp(-x)) - exp(1/x))", double.PositiveInfinity),
    ];

    private static readonly (string F, string Point, int Degree)[] SeriesCases =
        [("exp(-(x + 1)^2)*sin(x)", "0", 8), ("tan(x)", "0", 9), ("sqrt(1 + x)/(1 - x)", "0", 6), ("sin(x)", "a", 4), ("x^x", "1", 4)];

    private readonly Expr _mixed = ExprParser.Parse("exp(-x*y^2)*sin(x + y)/(1 + x^2 + y^2)");
    private readonly Expr _dipole = ExprParser.Parse("x/(x^2 + y^2 + z^2)^(3/2)");
    private readonly Expr _powers = ExprParser.Parse("(x + y - 1)^8 - (x - y + 1)^8");
    private Expr _mixedSum = null!;

    // Built with operators: the parser takes at most 500 levels, and this sum is 1200 deep.
    [GlobalSetup]
    public void Setup()
    {
        var x = new Variable("x");
        Expr sum = 0;
        for (int k = 1; k <= 600; k++)
            sum += k * x.Pow(k) + new Sin(k * x);
        _mixedSum = sum;
    }

    [Benchmark]
    public List<Expr> Integrals() => [.. Antiderivatives.Select(f => ExprParser.Parse(f).Integrate("x"))];

    [Benchmark]
    public List<Expr> ExactIntegrals() =>
        [.. Definite.Select(d => ExprParser.Parse(d.F).IntegrateExact("x", ExprParser.Parse(d.A), ExprParser.Parse(d.B)))];

    [Benchmark]
    public List<Limit> LimitsAtPoints() => [.. Limits.Select(l => ExprParser.Parse(l.F).Limit("x", l.Point))];

    [Benchmark]
    public List<Series> TaylorSeries() => [.. SeriesCases.Select(s => ExprParser.Parse(s.F).Series("x", ExprParser.Parse(s.Point), s.Degree))];

    [Benchmark]
    public Expr MixedSecondDerivative() => _mixed.Differentiate("x").Differentiate("y");

    [Benchmark]
    public Expr DipoleLaplacian() => _dipole.Laplacian("x", "y", "z");

    [Benchmark]
    public Expr ExpandPowers() => _powers.Expand();

    [Benchmark]
    public Expr MixedSum() => _mixedSum.Simplify();
}
