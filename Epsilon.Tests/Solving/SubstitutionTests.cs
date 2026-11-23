using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class SubstitutionTests
{
    private static string Printed(string equation) => Equation.Parse(equation).Solve("x").Print();

    [Theory]
    [InlineData("ln(x)^2 = ln(x) + 2", "{exp(-1), exp(2)}")]
    [InlineData("exp(x)^2 - 3*exp(x) + 2 = 0", "{0, ln(2)}")]
    [InlineData("nthroot(x, 3)^2 + nthroot(x, 3) = 2", "{-8, 1}")]
    [InlineData("sqrt(x)^2 - 5*sqrt(x) + 6 = 0", "{4, 9}")]
    [InlineData("abs(x)^2 - 3*abs(x) + 2 = 0", "{-2, -1, 1, 2}")]
    [InlineData("cosh(x)^2 - 3*cosh(x) + 2 = 0", "{-acosh(2), 0, acosh(2)}")]
    [InlineData("atan(x)^2 = 1", "{-tan(1), tan(1)}")]
    public void Polynomials_in_one_function_of_the_unknown(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    // sin(x) = 2 has no solution, and families half a period apart are one.
    [Theory]
    [InlineData("sin(x)^2 - 3*sin(x) + 2 = 0", "{π / 2 + 2 * π * k | k in Z}")]
    [InlineData("sin(x)^2 = sin(x)", "{π * k, π / 2 + 2 * π * k | k in Z}")]
    [InlineData("2*cos(x)^2 - cos(x) - 1 = 0", "{-2 * π / 3 + 2 * π * k, 2 * π * k, 2 * π / 3 + 2 * π * k | k in Z}")]
    [InlineData("sin(x)^3 = sin(x)", "{π * k / 2 | k in Z}")]
    [InlineData("1/sin(x) = sin(x)", "{π / 2 + π * k | k in Z}")]
    public void Polynomials_in_a_periodic_function_give_families(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    [Fact]
    public void Parameters_in_the_polynomial_give_conditions()
    {
        Assert.Equal("{0, ln(a) if a > 0}", Equation.Parse("exp(x)^2 - (a + 1)*exp(x) + a = 0").Solve("x").Print());
    }

    // The unknown outside the part, or a polynomial in it without a solution in radicals.
    [Theory]
    [InlineData("sin(x)^2 + sin(x) = x")]
    [InlineData("exp(x)^5 - exp(x) - 1 = 0")]
    [InlineData("sin(x)^2 + cos(x) = 0")]
    public void Otherwise_unsolved(string equation) => Assert.Equal("{...}", Printed(equation));

    [Fact]
    public void Numerically_in_a_range_and_with_the_part_found_as_a_number()
    {
        Assert.Equal([Math.PI / 2, 5 * Math.PI / 2], Equation.Parse("sin(x)^2 - 3*sin(x) + 2 = 0").SolveNumerically(0, 10).Values);
        Assert.Equal([Math.Log(2)], Equation.Parse("exp(x)^2 - 3*exp(x) + 2 = 0").SolveNumerically(0.5, 1).Values);

        // exp(x) is a root of t^5 - t - 1, found as a number.
        double x = Assert.Single(Equation.Parse("exp(x)^5 - exp(x) - 1 = 0").SolveNumerically().Values);
        Assert.Equal(0, Math.Exp(5 * x) - Math.Exp(x) - 1, 15);
    }

    [Theory]
    [InlineData("2*cos(x)^2 - cos(x) - 1 = 0")]
    [InlineData("tan(x)^2 - 1 = 0")]
    [InlineData("ln(x)^2 = ln(x) + 2")]
    [InlineData("nthroot(x, 3)^2 + nthroot(x, 3) = 2")]
    public void Solutions_make_both_sides_equal(string text)
    {
        Equation equation = Equation.Parse(text);
        IEnumerable<Solution> values = equation.Solve("x").Solutions
            .SelectMany(solution => solution.Index is null ? [solution] : new[] { -2, 0, 3 }.Select(solution.At).OfType<Solution>());

        foreach (Solution value in values)
        {
            double x = value.Value.Evaluate();
            Assert.Equal(equation.Left.Evaluate(("x", x)), equation.Right.Evaluate(("x", x)), 9);
        }
    }
}
