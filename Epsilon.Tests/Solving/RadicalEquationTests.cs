using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class RadicalEquationTests
{
    private static string Printed(string equation, Assumptions? assumptions = null) =>
        Equation.Parse(equation).Solve("x", assumptions).Print();

    // Squared, sqrt(x + 1) = x - 1 is x^2 - 3x = 0, but at 0 the sides are 1 and -1.
    [Theory]
    [InlineData("sqrt(x + 1) = x - 1", "{3}")]
    [InlineData("sqrt(x) = x - 2", "{4}")]
    [InlineData("x + sqrt(x) = 6", "{4}")]
    [InlineData("sqrt(3x + 1) = x - 1", "{5}")]
    [InlineData("sqrt(5 - x) = x + 1", "{1}")]
    [InlineData("sqrt(x^2 + 1) = x + 1", "{0}")]
    [InlineData("sqrt(x) + x = 0", "{0}")]
    public void Squaring_adds_no_solutions(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("sqrt(x) + sqrt(x + 1) = 3", "{16/9}")]
    [InlineData("sqrt(x + 3) - sqrt(x) = 1", "{1}")]
    [InlineData("sqrt(x - 1) + sqrt(x + 4) = 5", "{5}")]
    [InlineData("sqrt(2x + 1) + sqrt(x - 3) = 2sqrt(x)", "{4}")]
    [InlineData("sqrt(x) = sqrt(2 - x)", "{1}")]
    [InlineData("1/sqrt(x) + sqrt(x) = 5/2", "{1/4, 4}")]
    [InlineData("nthroot(x, 4) + sqrt(x) = 6", "{16}")]
    [InlineData("x^(1/2) + x^(3/2) = 2", "{1}")]
    public void Roots_are_taken_out_one_at_a_time(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("nthroot(x, 3) = x", "{-1, 0, 1}")]
    [InlineData("nthroot(x + 1, 3) + nthroot(x - 1, 3) = 0", "{0}")]
    public void An_odd_root_has_a_sign_of_its_own(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("sqrt(x + sqrt(x)) = 2", "{9/2 - sqrt(17) / 2}")]
    [InlineData("ln(sqrt(x) + sqrt(x + 1)) = 0", "{0}")]
    [InlineData("(x - 1) / sqrt(x) = 0", "{1}")]
    public void Roots_inside_other_functions_and_roots(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("sqrt(x^2 - 1) = x", "{}")]
    [InlineData("sqrt(x - 5) + sqrt(1 - x) = 1", "{}")]
    [InlineData("sqrt(x) + sqrt(x + 1) = 0", "{}")]
    public void Where_squaring_leaves_nothing_there_is_nothing(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Fact]
    public void The_assumptions_apply() => Assert.Equal("{}", Printed("sqrt(x) = x - 2", Assumptions.None.AssumeNegative("x")));

    [Theory]
    [InlineData("sqrt(x) + sin(x) = 0")]
    [InlineData("nthroot(x, 3)^2 + nthroot(x, 3) = 2")]
    [InlineData("sqrt(x) + sqrt(x + 1) + sqrt(x + 2) = 5")]
    public void Roots_that_cant_be_taken_out_or_leave_an_unsolved_polynomial_stay_unsolved(string equation) =>
        Assert.Equal("{...}", Printed(equation));

    [Theory]
    [InlineData("sqrt(x) + sqrt(x + 1) = 3", "sqrt(x) + sqrt(x + 1) - 3")]
    [InlineData("sqrt(x) + sqrt(x + 1) + sqrt(x + 2) = 5", "sqrt(x) + sqrt(x + 1) + sqrt(x + 2) - 5")]
    [InlineData("sqrt(x) + sqrt(2x + 1) + sqrt(3x + 2) = 10", "sqrt(x) + sqrt(2x + 1) + sqrt(3x + 2) - 10")]
    public void Numerically_the_polynomial_left_is_solved_and_its_roots_checked(string equation, string difference)
    {
        double root = Assert.Single(Equation.Parse(equation).SolveNumerically().Values);

        Assert.Equal(0, ExprParser.Parse(difference).Evaluate(("x", root)), 1e-12);
    }

    [Fact]
    public void Each_value_solves_the_equation()
    {
        foreach (string equation in new[] { "sqrt(x + 3) - sqrt(x) = 1", "1/sqrt(x) + sqrt(x) = 5/2", "sqrt(x + sqrt(x)) = 2", "nthroot(x, 3) = x" })
        {
            Equation parsed = Equation.Parse(equation);
            foreach (Solution solution in parsed.Solve().Solutions)
            {
                double x = solution.Value.Evaluate();
                Assert.Equal(parsed.Right.Evaluate(x), parsed.Left.Evaluate(x), 1e-12);
            }
        }
    }
}
