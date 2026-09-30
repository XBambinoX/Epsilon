using BenchmarkDotNet.Attributes;
using Epsilon.Core;

namespace Epsilon.Benchmarks;

// How Simplify scales with the size of a sum.
[MemoryDiagnoser]
public class LargeSumBenchmarks
{
    private Expr _distinctTerms = null!;
    private Expr _likeTerms = null!;

    [Params(10, 100, 300)]
    public int Terms { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // 1x^1 + 2x^2 + ... : nothing combines, so the result is as large as the input.
        _distinctTerms = ExprParser.Parse(
            string.Join(" + ", Enumerable.Range(1, Terms).Select(k => $"{k}x^{k}")));

        // 1x + 2x + ... : everything combines into a single term.
        _likeTerms = ExprParser.Parse(
            string.Join(" + ", Enumerable.Range(1, Terms).Select(k => $"{k}x")));
    }

    [Benchmark]
    public Expr DistinctTerms() => _distinctTerms.Simplify();

    [Benchmark]
    public Expr LikeTerms() => _likeTerms.Simplify();
}
