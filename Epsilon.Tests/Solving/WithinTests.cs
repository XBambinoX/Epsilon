using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class WithinTests
{
    private static string Within(string equation, string from, string to) =>
        Equation.Parse(equation).Solve("x").Within(ExprParser.Parse(from), ExprParser.Parse(to)).Print();

    [Theory]
    [InlineData("sin(x) = 1/2", "0", "10", "{π / 6, 5 * π / 6, 13 * π / 6, 17 * π / 6}")]
    [InlineData("sin(x) = 0", "0", "2*pi", "{0, π, 2 * π}")]
    [InlineData("sin(x) = 0", "-pi", "pi", "{-π, 0, π}")]
    [InlineData("cos(x) = 0", "-pi", "pi", "{-π / 2, π / 2}")]
    [InlineData("sin(2x) = 1/2", "0", "pi", "{π / 12, 5 * π / 12}")]
    [InlineData("sin(x) = 1/2", "0.5236", "1", "{}")]
    public void The_values_of_a_family_in_the_range_are_listed(string equation, string from, string to, string expected) =>
        Assert.Equal(expected, Within(equation, from, to));

    [Theory]
    [InlineData("sin(x)/x = 0", "-10", "10", "{-3 * π, -2 * π, -π, π, 2 * π, 3 * π}")]
    [InlineData("sin(x)*ln(x) = 0", "0", "10", "{1, π, 2 * π, 3 * π}")]
    [InlineData("sin(x)/(1 + cos(x)) = 0", "0", "10", "{0, 2 * π}")]
    public void The_values_the_conditions_on_the_index_leave_out_are_left_out(string equation, string from, string to, string expected) =>
        Assert.Equal(expected, Within(equation, from, to));

    [Theory]
    [InlineData("x^2 = 4", "0", "10", "{2}")]
    [InlineData("x^2 = 4", "-2", "2", "{-2, 2}")]
    [InlineData("x = 5", "0", "1", "{}")]
    public void Single_values_are_kept_where_they_are_in_the_range(string equation, string from, string to, string expected) =>
        Assert.Equal(expected, Within(equation, from, to));

    // 2 * Math.PI is just below 2 * π, too close to tell apart.
    [Fact]
    public void A_value_too_close_to_an_end_to_tell_counts_as_in_the_range()
    {
        SolutionSet solutions = Equation.Parse("sin(x) = 0").Solve();

        Assert.Equal("{0, π, 2 * π}", solutions.Within(0, 2 * Math.PI).Print());
        Assert.Equal("{2 * π}", solutions.Within(2 * Math.PI, 7).Print());
    }

    [Fact]
    public void Too_many_values_stay_a_family_with_bounds_on_its_index() =>
        Assert.Equal("{π * k | k in Z, k >= 0, k <= 31830}", Equation.Parse("sin(x) = 0").Solve().Within(0, 100000).Print());

    [Fact]
    public void A_value_with_parameters_gets_the_range_as_conditions_on_them() =>
        Assert.Equal("{a} if a >= 0 and a <= 1", Within("x = a", "0", "1"));

    [Fact]
    public void Every_value_becomes_every_value_in_the_range() =>
        Assert.Equal("(-inf, inf) if x != 0 and x >= 0 and x <= 1", Within("x/x = 1", "0", "1"));

    [Fact]
    public void What_wasnt_solved_stays_unsolved() => Assert.Equal("{...}", Within("x - cos(x) = 0", "0", "1"));

    [Fact]
    public void The_assumptions_the_equation_was_solved_under_still_hold() =>
        Assert.Equal("{π, 2 * π, 3 * π}", Equation.Parse("sin(x) = 0").Solve("x", Assumptions.None.AssumePositive("x")).Within(-10, 10).Print());

    [Theory]
    [InlineData("sin(2x + 1) = 1/3")]
    [InlineData("cos(x/3) = -0.2")]
    [InlineData("tan(5x) = 7")]
    [InlineData("sin(x)/x = 0")]
    public void The_values_listed_are_those_found_numerically(string equation)
    {
        Equation parsed = Equation.Parse(equation);

        double[] exact = [.. parsed.Solve().Within(-10, 10).Solutions.Select(solution => solution.Value.Evaluate())];

        Assert.Equal(parsed.SolveNumerically(-10, 10).Values, exact, (a, b) => Math.Abs(a - b) < 1e-12);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 0)]
    public void The_lower_end_comes_first(int from, int to) =>
        Assert.Throws<ArgumentException>(() => Equation.Parse("sin(x) = 0").Solve().Within(from, to));

    [Fact]
    public void An_end_is_a_number() =>
        Assert.Throws<ArgumentException>(() => Equation.Parse("sin(x) = 0").Solve().Within(0, new Variable("a")));

    [Fact]
    public void Complex_solutions_have_no_range_of_the_real_line() =>
        Assert.Throws<InvalidOperationException>(() => Equation.Parse("x^2 = -1").SolveComplex().Within(0, 1));

    // Listed, not scanned for: the scan could miss values closer together than its steps.
    [Fact]
    public void Numerically_every_value_of_a_family_in_the_range_is_listed()
    {
        NumericSolutionSet<double> solutions = Equation.Parse("sin(x) = 0").SolveNumerically(0, 100000);

        Assert.Equal(31831, solutions.Values.Count);
        Assert.All(solutions.Values.Select((x, k) => (x, k)), pair => Assert.Equal(pair.k * Math.PI, pair.x, 1e-9 * Math.Max(1, pair.x)));
    }

    [Fact]
    public void Numerically_the_rest_is_still_scanned_for()
    {
        NumericSolutionSet<double> solutions = Equation.Parse("sin(x)*(x - cos(x)) = 0").SolveNumerically(0, 100);

        Assert.Equal(33, solutions.Values.Count);
        Assert.Contains(solutions.Values, x => Math.Abs(x - 0.7390851332151607) < 1e-12);
    }

    [Fact]
    public void Numerically_a_family_in_an_infinite_range_is_scanned_for() =>
        Assert.Contains(Equation.Parse("sin(x) = 0").SolveNumerically(0, double.PositiveInfinity).Values, x => Math.Abs(x - Math.PI) < 1e-12);
}
