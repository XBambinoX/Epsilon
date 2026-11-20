using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class PeriodicEquationTests
{
    private static string Printed(string equation, Assumptions? assumptions = null) =>
        Equation.Parse(equation).Solve("x", assumptions).Print();

    [Theory]
    [InlineData("sin(x) = 1/2", "{π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z}")]
    [InlineData("sin(x) = -1/2", "{-5 * π / 6 + 2 * π * k, -π / 6 + 2 * π * k | k in Z}")]
    [InlineData("sin(x) = 1/3", "{asin(1/3) + 2 * π * k, π - asin(1/3) + 2 * π * k | k in Z}")]
    [InlineData("cos(x) = 1/2", "{-π / 3 + 2 * π * k, π / 3 + 2 * π * k | k in Z}")]
    [InlineData("cos(x) = -1", "{π + 2 * π * k | k in Z}")]
    [InlineData("tan(x) = 1", "{π / 4 + π * k | k in Z}")]
    [InlineData("tan(x) = -sqrt(3)", "{-π / 3 + π * k | k in Z}")]
    [InlineData("cot(x) = 1", "{π / 4 + π * k | k in Z}")]
    [InlineData("cot(x) = 0", "{π / 2 + π * k | k in Z}")]
    [InlineData("sec(x) = 2", "{-π / 3 + 2 * π * k, π / 3 + 2 * π * k | k in Z}")]
    [InlineData("csc(x) = 2", "{π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z}")]
    public void Trigonometric_functions_give_a_family_for_each_value_in_a_period(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("sin(x) = 2", "{}")]
    [InlineData("cos(x) = -3/2", "{}")]
    [InlineData("sec(x) = 1/2", "{}")]
    [InlineData("csc(x) = 0", "{}")]
    public void A_value_out_of_range_has_none(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    // Families half a period apart are one: 2 * π * k and π + 2 * π * k are π * k.
    [Theory]
    [InlineData("sin(x) = 0", "{π * k | k in Z}")]
    [InlineData("sin(x) = 1", "{π / 2 + 2 * π * k | k in Z}")]
    [InlineData("cos(x) = 0", "{π / 2 + π * k | k in Z}")]
    [InlineData("sin(x)^2 = 1/4", "{-π / 6 + π * k, π / 6 + π * k | k in Z}")]
    [InlineData("sin(x)*cos(x) = 0", "{π * k / 2 | k in Z}")]
    [InlineData("abs(sin(x)) = 1/2", "{-π / 6 + π * k, π / 6 + π * k | k in Z}")]
    public void Families_that_make_one_merge(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("sin(2x) = 1/2", "{π / 12 + π * k, 5 * π / 12 + π * k | k in Z}")]
    [InlineData("sin(3x) = 0", "{π * k / 3 | k in Z}")]
    [InlineData("sin(x/2) = 1", "{π + 4 * π * k | k in Z}")]
    [InlineData("sin(pi*x) = 0", "{k | k in Z}")]
    [InlineData("sin(x - 1) = 0", "{1 + π * k | k in Z}")]
    [InlineData("2sin(x) + 1 = 0", "{-5 * π / 6 + 2 * π * k, -π / 6 + 2 * π * k | k in Z}")]
    [InlineData("ln(sin(x)) = 0", "{π / 2 + 2 * π * k | k in Z}")]
    [InlineData("exp(sin(x)) = 1", "{π * k | k in Z}")]
    [InlineData("1/sin(x) = 2", "{π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z}")]
    public void Layers_around_and_a_linear_argument_inside(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("sin(x) = a", "{asin(a) + 2 * π * k, -asin(a) + π + 2 * π * k | k in Z} if a >= -1 and a <= 1")]
    [InlineData("cos(x) = a", "{acos(a) + 2 * π * k, -acos(a) + 2 * π * k | k in Z} if a >= -1 and a <= 1")]
    [InlineData("tan(x) = a", "{atan(a) + π * k | k in Z}")]
    [InlineData("sec(x) = a", "{acos(1 / a) + 2 * π * k, -acos(1 / a) + 2 * π * k | k in Z} if abs(a) >= 1")]
    public void What_the_other_side_needs_becomes_a_condition(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    // The rest of the equation leaves the values of k it is defined at.
    [Theory]
    [InlineData("sin(x)/x = 0", "{π * k | k in Z, k != 0}")]
    [InlineData("sin(x)/(x^2 - pi^2) = 0", "{π * k | k in Z, k != -1, k != 1}")]
    [InlineData("sin(x)/(x^2 - 1) = 0", "{π * k | k in Z}")]
    [InlineData("sin(x)*sqrt(x) = 0", "{π * k | k in Z, k >= 0}")]
    [InlineData("sin(x)*ln(x) = 0", "{π * k if k >= 1, 1 | k in Z}")]
    [InlineData("cos(x)*sqrt(-x) = 0", "{0, π / 2 + π * k if k <= -1 | k in Z}")]
    [InlineData("sin(x)*sqrt(x^2 - 4) = 0", "{-2, π * k if k <= -1, π * k if k >= 1, 2 | k in Z}")]
    [InlineData("sin(x)*sqrt(1 - x^2) = 0", "{-1, 0, 1}")]
    [InlineData("sin(x)*sqrt(10 - x^2) = 0", "{-sqrt(10), -π, 0, π, sqrt(10)}")]
    [InlineData("sin(pi*x)*sqrt(4 - x^2) = 0", "{-2, -1, 0, 1, 2}")]
    [InlineData("x*sin(x) = 0", "{π * k | k in Z}")]
    public void The_rest_of_the_equation_bounds_k(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    // Where a periodic condition holds depends on k modulo a few: cos(π * k) is 1 for even k only.
    [Theory]
    [InlineData("sin(x)/(1 + cos(x)) = 0", "{2 * π * k | k in Z}")]
    [InlineData("sin(x)/(cos(x) - 1) = 0", "{π + 2 * π * k | k in Z}")]
    [InlineData("cos(2x)/sin(x) = 0", "{π / 4 + π * k / 2 | k in Z}")]
    [InlineData("tan(x)*sin(x) = 0", "{π * k | k in Z}")]
    public void A_periodic_condition_keeps_some_values_of_k_in_each_period(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("sin(x) = 1/2", "{π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z, k >= 0}")]
    [InlineData("sin(x) = 0", "{π * k | k in Z, k >= 1}")]
    [InlineData("cos(x) = 0", "{π / 2 + π * k | k in Z, k >= 0}")]
    public void A_positive_unknown_bounds_k(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation, Assumptions.None.AssumePositive("x")));

    [Fact]
    public void A_negative_unknown_bounds_k() =>
        Assert.Equal("{π / 4 + π * k | k in Z, k <= -1}", Printed("tan(x) = 1", Assumptions.None.AssumeNegative("x")));

    [Fact]
    public void The_index_is_a_name_the_equation_doesnt_use() =>
        Assert.Equal("{-asin(k) + 2 * π * n, asin(k) + π + 2 * π * n | n in Z} if k <= 1 and k >= -1", Printed("sin(x) + k = 0"));

    [Theory]
    [InlineData("sin(x^2) = 0", "x")]
    [InlineData("sin(1/x) = 0", "x")]
    [InlineData("sin(sin(x)) = 0", "x")]
    [InlineData("sin(exp(x)) = 0", "x")]
    [InlineData("sin(x) = cos(x)", "x")]
    public void Families_not_linear_in_k_stay_unsolved(string equation, string variable)
    {
        SolutionSet solutions = Equation.Parse(equation).Solve(variable);

        Assert.Equal("{...}", solutions.Print());
        Assert.Single(solutions.Unsolved);
    }

    [Fact]
    public void A_family_is_its_value_with_the_index_in_it_and_its_period()
    {
        Solution family = Equation.Parse("sin(x) = 1/2").Solve().Solutions[0];

        Assert.Equal("k", family.Index);
        Assert.Equal("2 * π", family.Period!.Print());
        Assert.Equal("13 * π / 6", family.Value.Substitute("k", 1).Simplify().Print());
        Assert.Null(Equation.Parse("x = 1").Solve().Solutions[0].Period);
    }

    [Theory]
    [InlineData("sin(x)/x = 0", 1, "π")]
    [InlineData("sin(x)/x = 0", -2, "-2 * π")]
    [InlineData("sin(x) = 1/2", 2, "25 * π / 6")]
    [InlineData("sin(x)*ln(x) = 0", 3, "3 * π")]
    [InlineData("sin(x) = a", 1, "asin(a) + 2 * π if a >= -1 and a <= 1")]
    public void At_gives_one_value_of_a_family(string equation, int index, string expected) =>
        Assert.Equal(expected, Equation.Parse(equation).Solve("x").Solutions[0].At(index)!.Print());

    [Theory]
    [InlineData("sin(x)/x = 0", 0)]
    [InlineData("sin(x)/(x^2 - pi^2) = 0", -1)]
    [InlineData("sin(x)*ln(x) = 0", 0)]
    [InlineData("sin(x)*sqrt(x) = 0", -1)]
    public void At_an_index_the_conditions_leave_out_there_is_none(string equation, int index) =>
        Assert.Null(Equation.Parse(equation).Solve().Solutions[0].At(index));

    [Fact]
    public void At_decides_by_the_assumptions_the_equation_was_solved_under()
    {
        Solution family = Equation.Parse("sin(x) = 0").Solve("x", Assumptions.None.AssumePositive("x")).Solutions[0];

        Assert.Null(family.At(0));
        Assert.Equal("3 * π", family.At(3)!.Print());
    }

    [Fact]
    public void A_single_value_is_itself_at_every_index()
    {
        Solution one = Equation.Parse("x = 1").Solve().Solutions[0];

        Assert.Same(one, one.At(5));
    }

    [Theory]
    [InlineData("sin(2x + 1) = 1/3")]
    [InlineData("cos(x/3) = -0.2")]
    [InlineData("tan(5x) = 7")]
    [InlineData("sin(x)^2 = 1/4")]
    public void Every_value_of_a_family_solves_the_equation(string equation)
    {
        Equation parsed = Equation.Parse(equation);
        SolutionSet solutions = parsed.Solve();

        Assert.NotEmpty(solutions.Solutions);
        foreach (Solution family in solutions.Solutions)
        {
            for (int k = -5; k <= 5; k++)
            {
                double x = family.Value.Evaluate((family.Index!, k));
                Assert.Equal(parsed.Right.Evaluate(x), parsed.Left.Evaluate(x), 1e-12);
            }
        }
    }

    [Fact]
    public void Latex_names_the_index_after_a_bar() =>
        Assert.Equal("\\left\\{\\pi \\cdot k \\mid k \\in \\mathbb{Z}, k \\neq 0\\right\\}", Equation.Parse("sin(x)/x = 0").Solve().ToLatex());

    // A family has too many values to list as numbers, but in a range it has a few.
    [Fact]
    public void Numerically_a_family_is_listed_in_a_range()
    {
        Assert.Equal("{...}", Equation.Parse("sin(x) = 1/2").SolveNumerically().Print());
        Assert.Equal([Math.PI / 6, 5 * Math.PI / 6, 13 * Math.PI / 6, 17 * Math.PI / 6],
            Equation.Parse("sin(x) = 1/2").SolveNumerically(0, 10).Values, (a, b) => Math.Abs(a - b) < 1e-12);
    }
}
