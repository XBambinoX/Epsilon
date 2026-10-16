using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class IntegrationByPartsTests
{
    private static readonly double[] Points = [-2.7, -1.3, -0.6, 0.35, 0.8, 1.7, 3.1];

    private static string Integral(string integrand) => ExprParser.Parse(integrand).Integrate("x").Print();

    [Theory]
    [InlineData("x^5*exp(x)")]
    [InlineData("x*exp(-2*x)")]
    [InlineData("x*sin(3*x + 1)")]
    [InlineData("x*sinh(x)")]
    [InlineData("x*2^x")]
    [InlineData("x*sec(x)^2")]
    [InlineData("x^2*exp(-x)")]
    [InlineData("x*cos(x)^2")]
    [InlineData("x*sec(x)*tan(x)")]
    [InlineData("x^2*ln(x)")]
    [InlineData("ln(x)/x^2")]
    [InlineData("ln(x)^3")]
    [InlineData("x*ln(x)^2")]
    [InlineData("sqrt(x)*ln(x)")]
    [InlineData("ln(2*x + 1)*x")]
    [InlineData("(x + 1)*ln(x)")]
    [InlineData("x^2*atan(x)")]
    [InlineData("exp(-x)*sin(x)")]
    [InlineData("cos(ln(x))")]
    [InlineData("2^x*cos(x)")]
    [InlineData("sec(x)^5")]
    [InlineData("sech(x)^3")]
    [InlineData("csch(x)^3")]
    [InlineData("sin(sqrt(x))")]
    [InlineData("atan(sqrt(x))")]
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

    // A polynomial for u loses a degree each step.
    [Fact]
    public void A_polynomial_times_an_exponential_or_a_sine()
    {
        Assert.Equal("exp(x) * x - exp(x)", Integral("x*exp(x)"));
        Assert.Equal("x^2 * sin(x) + 2cos(x) * x - 2sin(x)", Integral("x^2*cos(x)"));
        Assert.Equal("exp(x) * x", Integral("(x + 1)*exp(x)"));
    }

    // A logarithm or an inverse function for u, alone with dv = dx.
    [Fact]
    public void Logarithms_and_inverse_functions()
    {
        Assert.Equal("(1/2) * ln(x) * x^2 - (1/4) * x^2", Integral("x*ln(x)"));
        Assert.Equal("2x + ln(x)^2 * x - 2ln(x) * x", Integral("ln(x)^2"));
        Assert.Equal("ln(x^2 + 1) * x - 2x + 2atan(x)", Integral("ln(x^2 + 1)"));
        Assert.Equal("asin(x)^2 * x - 2x + 2asin(x) * (-x^2 + 1)^(1/2)", Integral("asin(x)^2"));
    }

    // Two steps come back to the integral, which is solved for.
    [Fact]
    public void Integrals_that_come_back_to_themselves()
    {
        Assert.Equal("(exp(x) * sin(x) - cos(x) * exp(x)) / 2", Integral("exp(x)*sin(x)"));
        Assert.Equal("(2/13) * cos(3x) * exp(2x) + (3/13) * exp(2x) * sin(3x)", Integral("exp(2*x)*cos(3*x)"));
        Assert.Equal("(sin(ln(x)) * x - cos(ln(x)) * x) / 2", Integral("sin(ln(x))"));
    }

    // With the same rate the two steps give I = A + I, so sinh is written in exponentials instead.
    [Fact]
    public void An_exponential_times_sinh() =>
        Assert.Equal("-(1/2) * x + (1/4) * exp(2x)", Integral("exp(x)*sinh(x)"));

    [Fact]
    public void Reduction_formulas_of_sec_and_csc()
    {
        Assert.Equal("(1/2) * ln(abs(sec(x) + tan(x))) + (1/2) * sec(x) * tan(x)", Integral("sec(x)^3"));
        Assert.Equal("(-1/2) * cot(x) * csc(x) - (1/2) * ln(abs(cot(x) + csc(x)))", Integral("csc(x)^3"));
    }

    [Fact]
    public void After_a_substitution()
    {
        Assert.Equal("2exp(sqrt(x)) * sqrt(x) - 2exp(sqrt(x))", Integral("exp(sqrt(x))"));
        Assert.Equal("(1/2) * exp(x^2) * x^2 - (1/2) * exp(x^2)", Integral("x^3*exp(x^2)"));
    }

    // Integration by parts made u = x - 1 a better choice than the root, and the expansion lost
    // to it in the substitution: these are the forms of before.
    [Fact]
    public void Substitution_keeps_its_forms()
    {
        Assert.Equal("(1/5) * (x^2 + 1)^(5/2) - (1/3) * (x^2 + 1)^(3/2)", Integral("x^3*sqrt(x^2 + 1)"));
        Assert.Equal("(1/3) * (-x^2 + 1)^(3/2) - (-x^2 + 1)^(1/2)", Integral("x^3/sqrt(1 - x^2)"));
    }

    // Non-elementary, or needing what isn't there yet: cos^2 lowered first.
    [Theory]
    [InlineData("x*tan(x)")]
    [InlineData("atan(x)^2")]
    [InlineData("exp(x)/x")]
    [InlineData("exp(x)*cos(x)^2")]
    [InlineData("x/(2 + cos(x))")]
    public void What_isnt_integrated_by_parts(string integrand) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));
}
