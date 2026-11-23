using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class InverseFunctionTests
{
    private static string Printed(string equation, Assumptions? assumptions = null) =>
        Equation.Parse(equation).Solve("x", assumptions).Print();

    [Theory]
    [InlineData("exp(x) = 5", "{ln(5)}")]
    [InlineData("exp(x) = -1", "{}")]
    [InlineData("3ln(x - 1) + 2 = 5", "{1 + e}")]
    [InlineData("ln(x + 1) = ln(2)", "{1}")]
    [InlineData("1/ln(x) = 2", "{exp(1/2)}")]
    [InlineData("2^x = 8", "{3}")]
    [InlineData("10^x = 1000", "{3}")]
    [InlineData("2^x = 5", "{ln(5) / ln(2)}")]
    public void Exponentials_and_logarithms(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("sqrt(x) = 3", "{9}")]
    [InlineData("sqrt(x) = -1", "{}")]
    [InlineData("nthroot(x, 3) = -2", "{-8}")]
    [InlineData("ln(sqrt(x) - 1) = 0", "{4}")]
    [InlineData("abs(x - 1) = 2", "{-1, 3}")]
    [InlineData("abs(x) = 0", "{0}")]
    [InlineData("abs(x) = -1", "{}")]
    public void Roots_and_absolute_values(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("asin(x) = pi/6", "{1/2}")]
    [InlineData("asin(x) = 2", "{}")]
    [InlineData("acos(x) = pi/3", "{1/2}")]
    [InlineData("atan(x) = 1", "{tan(1)}")]
    [InlineData("sinh(x) = 1", "{asinh(1)}")]
    [InlineData("cosh(x) = 1", "{0}")]
    [InlineData("cosh(x) = 2", "{-acosh(2), acosh(2)}")]
    [InlineData("cosh(x) = 1/2", "{}")]
    [InlineData("tanh(x) = 1/2", "{atanh(1/2)}")]
    public void Inverse_and_hyperbolic_functions(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    // Powers of a function, and a polynomial inside one, which the polynomial solver takes over.
    [Theory]
    [InlineData("ln(x)^2 = 4", "{exp(-2), exp(2)}")]
    [InlineData("exp(x)^2 = 4", "{ln(2)}")]
    [InlineData("sqrt(x)^3 = 8", "{4}")]
    [InlineData("exp(x^2) = 4", "{-sqrt(2ln(2)), sqrt(2ln(2))}")]
    [InlineData("ln(x^2 - 1) = 0", "{-sqrt(2), sqrt(2)}")]
    [InlineData("exp(x^3 - x) = 1", "{-1, 0, 1}")]
    public void Layers_down_to_a_polynomial(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("exp(x) = a", "{ln(a)} if a > 0")]
    [InlineData("2^x = b", "{ln(b) / ln(2)} if b > 0")]
    [InlineData("sqrt(x + 1) = a", "{a^2 - 1} if a >= 0")]
    public void What_the_other_side_needs_becomes_a_condition(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    // The domain of the rest of the equation still decides: ln(x) is undefined at 0.
    [Theory]
    [InlineData("x^2*ln(x) = 0", "{1}")]
    [InlineData("exp(ln(x)) = -1", "{}")]
    [InlineData("ln(x)/(x - e) = 0", "{1}")]
    public void The_rest_of_the_equation_keeps_its_domain(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("x + sin(x) = 1", "x + sin(x) - 1")]
    [InlineData("x*exp(x) = 1", "x * exp(x) - 1")]
    public void The_unknown_on_both_sides_of_a_function_stays_unsolved(string equation, string unsolved)
    {
        SolutionSet solutions = Equation.Parse(equation).Solve("x");

        Assert.Equal("{...}", solutions.Print());
        Assert.Equal(unsolved, Assert.Single(solutions.Unsolved).Print());
    }

    [Fact]
    public void The_sign_assumed_for_the_unknown_selects_the_solutions() =>
        Assert.Equal("{}", Printed("exp(x) = 2", Assumptions.None.AssumeNegative("x")));

    [Fact]
    public void Numerically_too()
    {
        Assert.Equal([-1.1774100225154747, 1.1774100225154747], Equation.Parse("exp(x^2) = 4").SolveNumerically().Values);
        Assert.Equal([1.322165725569503], Equation.Parse("ln(x^5 - x) = 1").SolveNumerically().Values);
        Assert.Equal([0.5109734293885692], Equation.Parse("ln(x + sin(x)) = 0").SolveNumerically(-10, 10).Values);
    }
}
