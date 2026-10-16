using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class PartialFractionsTests
{
    private static readonly double[] Points = [-2.7, -1.3, -0.6, 0.35, 0.8, 1.7, 3.1];

    private static string Integral(string integrand) => ExprParser.Parse(integrand).Integrate("x").Print();

    [Theory]
    [InlineData("1/(x^3 + 1)")]
    [InlineData("x^4/(x^3 + 1)")]
    [InlineData("(x + 1)^2/(x^3 + 1)")]
    [InlineData("1/(x^3 - x)")]
    [InlineData("1/(x^3 - 8)")]
    [InlineData("1/((x - 1)^3*(x + 1)^3)")]
    [InlineData("(3*x^2 + 2*x - 1)/(x*(x - 1)^2*(x^2 + 1)^2)")]
    [InlineData("1/(x^2 + 1)^3")]
    [InlineData("(x^5 + 1)/(x^6 + 3*x^4 + 3*x^2 + 1)")]
    [InlineData("1/(x^2*(x^2 + x + 1)^2)")]
    [InlineData("1/(x^4 + 4)")]
    [InlineData("1/(x^6 - 1)")]
    [InlineData("x^3/(x^4 + x^2 + 1)")]
    [InlineData("1/(x*(x^2 - 2))")]
    [InlineData("1/(4*x^2 - 1)^2")]
    [InlineData("pi/(x^3 - 1)")]
    [InlineData("-x/(2*(x^2 + x - 6))")]
    [InlineData("x*(x + 1)^(-2)")]
    [InlineData("atan(x)/x^2")]
    public void The_derivative_of_the_antiderivative_is_the_integrand(string integrand)
    {
        var f = ExprParser.Parse(integrand);
        var derivative = f.Integrate("x").Differentiate("x");

        int checkedPoints = 0;
        foreach (double x in Points)
        {
            double expected = f.Evaluate(("x", x));
            if (!double.IsFinite(expected))
                continue;

            Assert.Equal(expected, derivative.Evaluate(("x", x)), 1e-9 * Math.Max(1, Math.Abs(expected)));
            checkedPoints++;
        }

        Assert.True(checkedPoints >= 2);
    }

    [Fact]
    public void Linear_factors()
    {
        Assert.Equal("(1/3) * ln(abs(x - 1)) + (2/3) * ln(abs(x + 2))", Integral("x/((x - 1)*(x + 2))"));
        Assert.Equal("2ln(abs(x - 1)) - ln(abs(x + 1))", Integral("(x + 3)/(x^2 - 1)"));
        Assert.Equal("(1/2) * ln(abs(x^2 - 1)) - ln(abs(x))", Integral("1/(x^3 - x)"));
    }

    [Fact]
    public void Repeated_factors()
    {
        Assert.Equal("-1 / (x - 1) + ln(abs(x - 1))", Integral("x/(x - 1)^2"));
        Assert.Equal("-1 / (2x^2) + ln(abs(x))", Integral("(x^2 + 1)/x^3"));
        Assert.Equal("(atan(x) + x / (x^2 + 1)) / 2", Integral("1/(x^2 + 1)^2"));
        Assert.Equal("-1 / (2 * (x^2 + 1))", Integral("x/(x^2 + 1)^2"));
    }

    [Fact]
    public void Quadratic_factors()
    {
        Assert.Equal("atan((2x - 1) / sqrt(3)) / sqrt(3) + (1/3) * ln(abs(x + 1)) - (1/6) * ln(x^2 - x + 1)", Integral("1/(x^3 + 1)"));
        Assert.Equal("ln(abs(x)) - (1/2) * ln(x^2 + 1)", Integral("1/(x*(x^2 + 1))"));
        Assert.Equal("(-1/2) * atan(x) + (1/4) * ln(abs((x - 1) / (x + 1)))", Integral("1/(x^4 - 1)"));
        Assert.Equal("(-1/2) * ln(abs(x)) + (1/4) * ln(abs(x^2 - 2))", Integral("1/(x*(x^2 - 2))"));
    }

    // Logarithms with coefficients of the same size become one, of a product or a quotient.
    [Fact]
    public void Logarithms_are_merged()
    {
        Assert.Equal("(1/3) * ln(abs(x^3 + 1))", Integral("x^2/(x^3 + 1)"));
        Assert.Equal("(1/2) * ln(abs((x - 1) / (x + 1)))", Integral("1/(x^2 - 1)"));
        Assert.Equal("(1/3) * ln(abs((x - 1) / (2x + 1)))", Integral("1/((2*x + 1)*(x - 1))"));
        Assert.Equal("(1/4) * ln(abs((x^2 - 1) / (x^2 + 1)))", Integral("x/(x^4 - 1)"));
    }

    // Expanded denominators with no rational roots, split into quadratics.
    [Fact]
    public void Products_of_quadratics()
    {
        Assert.Equal("(-1/6) * atan(x / 2) + (1/3) * atan(x)", Integral("1/((x^2 + 1)*(x^2 + 4))"));
        Assert.Equal("(-1/6) * atan(x / 2) + (1/3) * atan(x)", Integral("1/(x^4 + 5*x^2 + 4)"));
        Assert.Equal("atan((2x + 1) / sqrt(3)) / (2sqrt(3)) + atan((2x - 1) / sqrt(3)) / (2sqrt(3)) + (1/4) * ln((x^2 + x + 1) / (x^2 - x + 1))",
            Integral("1/(x^4 + x^2 + 1)"));
        Assert.Equal("(1/16) * ln((x^2 + 2x + 2) / (x^2 - 2x + 2)) + (1/8) * atan(x + 1) + (1/8) * atan(x - 1)", Integral("1/(x^4 + 4)"));
    }

    [Fact]
    public void Constant_factors()
    {
        Assert.Equal("-atan((2x - 1) / sqrt(3)) / sqrt(3) + (1/6) * ln(x^2 - x + 1) - (1/3) * ln(abs(x + 1))", Integral("-1/(x^3 + 1)"));
        Assert.Equal("(1/2) * ln(abs((x - 1) / (x + 1))) * π", Integral("pi/(x^2 - 1)"));
    }

    [Fact]
    public void After_integration_by_parts() =>
        Assert.Equal("ln(abs(x)) - atan(x) / x - (1/2) * ln(x^2 + 1)", Integral("atan(x)/x^2"));

    // Factors of degree 3 or more over the rationals, and symbols in the denominator.
    [Theory]
    [InlineData("1/(x^4 + 1)")]
    [InlineData("1/(x^3 - 2)")]
    [InlineData("1/(x^5 + x)")]
    [InlineData("1/(x^3 + a)")]
    public void What_doesnt_split_isnt_integrated(string integrand) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));
}
