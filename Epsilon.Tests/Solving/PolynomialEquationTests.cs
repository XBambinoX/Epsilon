using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class PolynomialEquationTests
{
    private static string Printed(string equation, Assumptions? assumptions = null) =>
        Equation.Parse(equation).Solve("x", assumptions).Print();

    private static string PrintedComplex(string equation) => Equation.Parse(equation).SolveComplex("x").Print();

    [Theory]
    [InlineData("x^2 - 5x + 6 = 0", "{2, 3}")]
    [InlineData("x^2 = 4", "{-2, 2}")]
    [InlineData("x^2 = 2", "{-sqrt(2), sqrt(2)}")]
    [InlineData("x^2 + 2x - 1 = 0", "{-sqrt(2) - 1, sqrt(2) - 1}")]
    [InlineData("x^2 - 2x + 1 = 0", "{1}")]
    [InlineData("x^2 = pi", "{-sqrt(π), sqrt(π)}")]
    [InlineData("x^2 + 1 = 0", "{}")]
    [InlineData("x^3 - x = 0", "{-1, 0, 1}")]
    [InlineData("x^3 = 1", "{1}")]
    [InlineData("2x^3 - 3x^2 - 3x + 2 = 0", "{-1, 1/2, 2}")]
    [InlineData("x^4 - 5x^2 + 6 = 0", "{-sqrt(3), -sqrt(2), sqrt(2), sqrt(3)}")]
    [InlineData("(x - 1)^2*(x + 2) = 0", "{-2, 1}")]
    public void Polynomial_equations(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("x^2 + 1 = 0", "{-i, i}")]
    [InlineData("x^2 + 2x + 5 = 0", "{-2 * i - 1, 2 * i - 1}")]
    [InlineData("x^3 = 1", "{-i * sqrt(3) / 2 - 1/2, i * sqrt(3) / 2 - 1/2, 1}")]
    [InlineData("x^4 = 1", "{-1, -i, i, 1}")]
    [InlineData("x^2 = 4", "{-2, 2}")]
    public void Complex_roots_of_polynomials(string equation, string expected) => Assert.Equal(expected, PrintedComplex(equation));

    // Over a common denominator, the numerator is 0 where the denominators aren't.
    [Theory]
    [InlineData("(x^2 - 1)/(x - 1) = 0", "{-1}")]
    [InlineData("x - 1/x = 0", "{-1, 1}")]
    [InlineData("1/x + 1/(x - 1) = 0", "{1/2}")]
    [InlineData("x^-2 = 4", "{-1/2, 1/2}")]
    [InlineData("1/x = 0", "{}")]
    [InlineData("(x + 1)/(x^2 - 1) = 0", "{}")]
    public void Rational_equations(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    // A product is 0 where a factor is and every factor is defined.
    [Theory]
    [InlineData("x*sin(x) = 0", "{π * k | k in Z}")]
    [InlineData("x*ln(x) = 0", "{1}")]
    [InlineData("sqrt(x)*(x + 1) = 0", "{0}")]
    [InlineData("tan(x)*(x - pi/2) = 0", "{π * k | k in Z}")]
    [InlineData("asin(x)*(x - 2) = 0", "{0}")]
    public void Products_are_zero_where_a_defined_factor_is(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Fact]
    public void Every_value_where_the_equation_is_defined()
    {
        Assert.Equal("(-inf, inf) if x != 0", Printed("x/x = 1"));
        Assert.Equal("C if x != 0", PrintedComplex("x/x = 1"));
        Assert.Equal("(-inf, inf) if a = 0 and x != 0", Printed("a/x = 0"));
    }

    [Fact]
    public void Solutions_with_parameters_keep_the_conditions_not_decided()
    {
        Assert.Equal("{a} if a != 1", Printed("(x - a)/(x - 1) = 0"));
        Assert.Equal("{2}", Printed("x^2 = 4", Assumptions.None.AssumePositive("x")));
    }

    [Fact]
    public void What_isnt_solved_yet_is_listed()
    {
        SolutionSet solutions = Equation.Parse("x*(x^3 + a*x + 1) = 0").Solve("x");

        Assert.Equal("{0, ...}", solutions.Print());
        Assert.Equal("x^3 + a * x + 1", Assert.Single(solutions.Unsolved).Print());
    }

    [Fact]
    public void Solves_for_the_only_variable_over_the_complex_numbers()
    {
        Assert.Equal("{-2 * i, 2 * i}", new Equation(ExprParser.Parse("x^2 + 4"), 0).SolveComplex().Print());
        Assert.Equal("{-i, i}", Equation.Parse("t^2 = -1").SolveComplex().Print());
    }

    [Fact]
    public void Domain_conditions()
    {
        static string Of(string expr, bool complex) => string.Join("; ", Domain.Conditions(ExprParser.Parse(expr), "x", complex)
            .Select(condition => condition.Print()).Order(StringComparer.Ordinal));

        Assert.Equal("cos(x) != 0; x != 0; x - 1 >= 0; x > 0; x >= 1", Of("ln(x) + sqrt(x - 1) + 1/x + tan(x) + acosh(x) + a/(b + 1)", complex: false));
        Assert.Equal("x != 0", Of("ln(x) + 1/x", complex: true));
    }
}
