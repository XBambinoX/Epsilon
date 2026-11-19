using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class RealRootTests
{
    private static IReadOnlyList<double> Roots(string equation, double from = double.NegativeInfinity, double to = double.PositiveInfinity) =>
        Equation.Parse(equation).SolveNumerically(from, to).Values;

    [Fact]
    public void Solutions_in_a_range()
    {
        Assert.Equal([-7 * Math.PI / 6, Math.PI / 6, 5 * Math.PI / 6], Roots("sin(x) = 1/2", -4, 4), new Close(12));
        Assert.Equal([-Math.PI, 0, Math.PI], Roots("x*sin(x) = 0", -4, 4), new Close(12));
        Assert.Empty(Roots("sin(x) = -1", 2, 3));
        Assert.Equal(-Math.PI / 2, Assert.Single(Roots("sin(x) = -1", -2, -1)), 8);
    }

    [Fact]
    public void Exact_solutions_outside_the_range_are_left_out()
    {
        Assert.Empty(Roots("x^5 - x - 1 = 0", 0, 1));
        Assert.Equal([1.1673039782614187], Roots("x^5 - x - 1 = 0", 1, 2));
        Assert.Equal([2.0], Roots("x^2 = 4", 0));
        Assert.Equal([5.0], Roots("x = 5", 5, 10));
    }

    [Fact]
    public void The_whole_line()
    {
        Assert.Equal([0.7390851332151607], Roots("x = cos(x)"));
        Assert.Equal([-2.0, 2.0], Roots("x^2 = 4"));
        Assert.Empty(Roots("x^2 + 1 = 0", -100, 100));
    }

    [Fact]
    public void Every_value_in_the_range()
    {
        Assert.Equal("(-inf, inf) if x != 0 and x >= -5 and x <= 5", Equation.Parse("x/x = 1").SolveNumerically(-5, 5).Print());
        Assert.Equal("(-inf, inf) if x >= 0", Equation.Parse("1 = 1").SolveNumerically("x", 0, double.PositiveInfinity).Print());
    }

    [Fact]
    public void Scans_finer_where_the_equation_oscillates()
    {
        // 200 steps found 82 of the zeros 0, pi, ..., 318 pi of sin(x) on [0, 1000].
        Assert.Equal(319, Roots("sin(x) = 0", 0, 1000).Count);
    }

    [Theory]
    [InlineData("1/x = 0")]         // a sign change at a pole
    [InlineData("exp(-x) = 0")]     // tends to 0 without reaching it
    [InlineData("sin(x)/sin(x) = 0")]
    public void No_false_solutions(string equation) => Assert.Empty(Roots(equation, -5, 50));

    [Fact]
    public void Poles_and_touching_roots()
    {
        Assert.Equal(0.0, Assert.Single(Roots("tan(x) = 0", -2, 2)), 12);
        Assert.Equal([-2 * Math.PI, -Math.PI, 0, Math.PI, 2 * Math.PI], Roots("sin(x)^2 = 0", -7, 7), new Close(9));
    }

    [Theory]
    [InlineData(double.NegativeInfinity, double.PositiveInfinity)]
    [InlineData(-1, 1)]
    [InlineData(-0.37, 5.1)]
    public void Solutions_at_the_edge_of_the_domain(double from, double to)
    {
        Assert.Equal([0.0], Roots("sqrt(x) = 0", from, to));
        Assert.Empty(Roots("x^2 / x = 0", from, to));
    }

    [Fact]
    public void Edges_of_the_domain()
    {
        Assert.Equal([-1.0, 1.0], Roots("sqrt(1 - x^2) = 0"));
        Assert.Equal([-1.0, 1.0], Roots("sqrt(1 - x^2) = 0", -2.3, 1.7));
        Assert.Equal([-Math.Sqrt(2), Math.Sqrt(2)], Roots("sqrt(2 - x^2) = 0"), new Close(15));
        Assert.Equal([1.0], Roots("acos(x) = 0"));
    }

    // Each tends to 0 at x = 0, where it is undefined.
    [Theory]
    [InlineData("x * ln(x) = 0")]
    [InlineData("sqrt(x) * ln(x) = 0")]
    [InlineData("ln(x) = 0")]
    public void A_limit_at_an_undefined_edge_isnt_a_solution(string equation) => Assert.Equal([1.0], Roots(equation));

    [Fact]
    public void Undefined_points_are_not_solutions() =>
        Assert.Equal([1.0], Roots("x^2/x = x^2", -10, 10));

    [Fact]
    public void Functions_without_a_derivative()
    {
        // round(x) - 2 is 0 on [1.5, 2.5): the scan reports grid points there without needing f'.
        IReadOnlyList<double> roots = Roots("round(x) - 2 = 0", -5, 5);

        Assert.NotEmpty(roots);
        Assert.All(roots, root => Assert.Equal(2, Math.Round(root, MidpointRounding.AwayFromZero)));
    }

    [Fact]
    public void The_sign_assumed_for_the_unknown_selects_the_solutions() =>
        Assert.Equal([Math.PI / 6, 5 * Math.PI / 6],
            Equation.Parse("sin(x) = 1/2").SolveNumerically("x", -4, 4, Assumptions.None.AssumePositive("x")).Values, new Close(12));

    [Fact]
    public void Ranges_must_be_numbers_in_order()
    {
        Equation equation = Equation.Parse("x^2 = 4");

        Assert.Equal("Expected the lower limit below the upper one, got [5, 5].", Assert.Throws<ArgumentException>(() => equation.SolveNumerically(5, 5)).Message);
        Assert.Throws<ArgumentException>(() => equation.SolveNumerically(10, -10));
        Assert.Equal("A limit of the range is NaN.", Assert.Throws<ArgumentException>(() => equation.SolveNumerically(double.NaN, 10)).Message);
        Assert.Throws<InvalidOperationException>(() => Equation.Parse("x = a").SolveNumerically("x", 0, 1));
    }

    [Fact]
    public void A_solution_near_a_guess()
    {
        Assert.Equal(2.0, Equation.Parse("x^2 = 4").FindRoot(near: 3));
        Assert.Equal(1.4142135623730951, Equation.Parse("x^2 = 2").FindRoot(near: 1));
        Assert.Equal(0.7390851332151607, Equation.Parse("cos(x) = x").FindRoot(near: 1));
        Assert.Equal(-2.0, Equation.Parse("t^2 = 4").FindRoot("t", near: -1));
    }

    [Fact]
    public void No_solution_where_Newtons_method_stops()
    {
        Assert.Null(Equation.Parse("x^2 + 5 = 0").FindRoot(near: 0)); // a flat slope at 0
        Assert.Throws<InvalidOperationException>(() => Equation.Parse("x = a").FindRoot("x", near: 0));
    }

    // The scan itself, which polynomials don't reach through an equation.
    [Theory]
    [InlineData("x^2", -5, 5, new[] { 0.0 })]              // the grid hits 0
    [InlineData("x^2", -5, 5.025, new[] { 0.0 })]          // the grid misses 0
    [InlineData("x^2 - 2x + 1", -3, 3, new[] { 1.0 })]
    [InlineData("x^2 - 2x + 1", -5, 5, new[] { 1.0 })]
    [InlineData("(x - 1)^3", -5, 5, new[] { 1.0 })]
    [InlineData("x^4", -5, 5, new[] { 0.0 })]
    [InlineData("(x - 1)^2 * (x + 2)", -5, 5, new[] { -2.0, 1.0 })]
    [InlineData("(x - 0.3)^2", -5, 5.025, new[] { 0.3 })]
    [InlineData("x^3 - x", -5, 5, new[] { -1.0, 0.0, 1.0 })]
    [InlineData("x - 5", 5, 10, new[] { 5.0 })]            // a root on the lower limit
    [InlineData("x - 10", 5, 10, new[] { 10.0 })]          // a root on the upper limit
    [InlineData("x^2", double.NegativeInfinity, double.PositiveInfinity, new[] { 0.0 })]
    [InlineData("x^3", -0.37, 3, new[] { 0.0 })]           // x^3 underflows to 0 on a plateau of tiny x
    public void The_scan_finds_each_root_once(string f, double from, double to, double[] expected) =>
        Assert.Equal(expected, RealRoots.InRange(ExprParser.Parse(f), "x", from, to), new Close(9));

    [Theory]
    [InlineData("x^2 + 0.0000001")] // a small positive minimum
    [InlineData("x^2 + 1")]
    [InlineData("x^2 / x")]         // 0 next to 0, undefined at it
    public void The_scan_finds_no_false_roots(string f) =>
        Assert.Empty(RealRoots.InRange(ExprParser.Parse(f), "x", -5, 50));

    private sealed class Close(int digits) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) <= Math.Pow(10, -digits) * Math.Max(1, Math.Abs(y));

        public int GetHashCode(double value) => 0;
    }
}
