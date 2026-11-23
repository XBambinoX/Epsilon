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

    // exp(2x) is exp(x)^2, 4^x is (2^x)^2, 2^(x + 1) is 2 * 2^x and exp(-x) is 1 / exp(x).
    [Theory]
    [InlineData("exp(2*x) - 3*exp(x) + 2 = 0", "{0, ln(2)}")]
    [InlineData("exp(2*x) = exp(x) + 1", "{ln(sqrt(5) / 2 + 1/2)}")]
    [InlineData("exp(x) - exp(x/2) - 2 = 0", "{2ln(2)}")]
    [InlineData("4^x - 3*2^x + 2 = 0", "{0, 1}")]
    [InlineData("9^x - 4*3^x + 3 = 0", "{0, 1}")]
    [InlineData("2^(x + 1) + 2^x = 12", "{2}")]
    [InlineData("8^x = 4^(x + 1)", "{2}")]
    [InlineData("2^x + 2^(-x) = 5/2", "{-1, 1}")]
    [InlineData("(1/2)^x + 2^x = 5/2", "{-1, 1}")]
    public void Exponentials_of_one_base_are_powers_of_one(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    // In t = nthroot(x, 3), x^(2/3) is t^2 and x is t^3; x^(2/3) is defined for x >= 0 only,
    // which keeps out -8 of t = -2.
    [Theory]
    [InlineData("x - 3*sqrt(x) + 2 = 0", "{1, 4}")]
    [InlineData("x^(2/3) + x^(1/3) = 2", "{1}")]
    [InlineData("x^(2/3) - 3*x^(1/3) + 2 = 0", "{1, 8}")]
    [InlineData("x^(-1/2) + x^(1/2) = 5/2", "{1/4, 4}")]
    [InlineData("sqrt(x) + nthroot(x, 4) = 6", "{16}")]
    [InlineData("nthroot(x, 3)^2 + nthroot(x, 3) = x", "{2 - sqrt(5), 0, sqrt(5) + 2}")]
    [InlineData("sqrt(x + 1) + x = 5", "{3}")]
    [InlineData("x^2 + sqrt(x) = 18", "{4}")]
    public void Roots_of_one_radicand_are_powers_of_one(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    // sqrt(x + a) is t at x = t^2 - a, which is where it is defined.
    [Theory]
    [InlineData("sqrt(x + a) = x", "{-sqrt(a + 1/4) + 1/2 if a <= 0 and a >= -1/4, sqrt(a + 1/4) + 1/2 if a >= -1/4}")]
    [InlineData("a^(2*x) - 3*a^x + 2 = 0", "{0 if a > 0 and a != 1, ln(2) / ln(a) if a != 1 and a > 0}")]
    public void With_parameters(string equation, string expected) => Assert.Equal(expected, Printed(equation));

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
    [InlineData("exp(x) = 2^x")]
    [InlineData("x*exp(x) = 1")]
    [InlineData("sqrt(x) + nthroot(x, 3) = x")]
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
    [InlineData("exp(x) + exp(-x) = 4")]
    [InlineData("nthroot(x, 3)^2 + nthroot(x, 3) = x")]
    [InlineData("x^(-1/2) + x^(1/2) = 5/2")]
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
