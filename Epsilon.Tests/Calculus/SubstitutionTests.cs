using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class SubstitutionTests
{
    private static readonly double[] Points = [-2.7, -1.3, -0.6, 0.35, 0.8, 1.7, 3.1];

    private static string Integral(string integrand) => ExprParser.Parse(integrand).Integrate("x").Print();

    [Theory]
    [InlineData("x*exp(x^2)")]
    [InlineData("1/(x*ln(x))")]
    [InlineData("2*x*(x^2 + 1)^5")]
    [InlineData("sin(x)/cos(x)^2")]
    [InlineData("tan(x)/cos(x)^2")]
    [InlineData("sec(x)^2*tan(x)^3")]
    [InlineData("exp(x)/(1 + exp(x))")]
    [InlineData("exp(2*x)/(exp(x) + 1)")]
    [InlineData("x^5/(1 + x^12)")]
    [InlineData("x^3*exp(x^4)")]
    [InlineData("exp(sqrt(x))/sqrt(x)")]
    [InlineData("sin(sqrt(x))/sqrt(x)")]
    [InlineData("asin(x)/sqrt(1 - x^2)")]
    [InlineData("exp(atan(x))/(1 + x^2)")]
    [InlineData("cos(ln(x))/x")]
    [InlineData("1/(x*(1 + ln(x)^2))")]
    [InlineData("x*sin(x^2)*cos(x^2)")]
    [InlineData("(2*x + 1)/(x^2 + x + 1)^2")]
    [InlineData("cos(x)/(1 + sin(x)^2)")]
    [InlineData("x*sqrt(1 - x^2)")]
    [InlineData("x^5*sqrt(x^3 + 2)")]
    [InlineData("x^3/sqrt(1 - x^2)")]
    [InlineData("x^3*(x^2 + 1)^(1/3)")]
    [InlineData("1/(1 + sqrt(x))")]
    [InlineData("x/sqrt(2*x + 3)")]
    [InlineData("1/(x*sqrt(x + 1))")]
    [InlineData("cos(x)^5")]
    [InlineData("sin(x)^3*cos(x)^2")]
    [InlineData("cos(2*x)^4")]
    [InlineData("sin(x)*cos(x)^7")]
    [InlineData("cosh(x)^2*sinh(x)")]
    [InlineData("sinh(x)^2*cosh(x)^2")]
    [InlineData("tan(x)^4")]
    [InlineData("cot(x)^3")]
    [InlineData("tanh(x)^3")]
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

    // A part of the integrand whose derivative is a factor of the rest.
    [Fact]
    public void The_derivative_of_a_part_divides_the_rest()
    {
        Assert.Equal("sin(x^2)", Integral("2*x*cos(x^2)"));
        Assert.Equal("ln(x)^2 / 2", Integral("ln(x)/x"));
        Assert.Equal("exp(sin(x))", Integral("cos(x)*exp(sin(x))"));
        Assert.Equal("(1/3) * ln(abs(x^3 + 1))", Integral("x^2/(x^3 + 1)"));
        Assert.Equal("atan(x)^2 / 2", Integral("atan(x)/(1 + x^2)"));
    }

    // x^4 is u^2 for u = x^2, exp(2x) is u^2 for u = exp(x), and the alternative derivative of
    // tan, sec^2, matches the integrand.
    [Fact]
    public void Powers_of_the_part()
    {
        Assert.Equal("(1/2) * atan(x^2)", Integral("x/(1 + x^4)"));
        Assert.Equal("atan(exp(x))", Integral("exp(x)/(1 + exp(2*x))"));
        Assert.Equal("tan(x)^4 / 4", Integral("sec(x)^2*tan(x)^3"));
    }

    // x^2 is u - 1 for u = x^2 + 1.
    [Fact]
    public void The_rest_written_in_the_part()
    {
        Assert.Equal("(1/5) * (x^2 + 1)^(5/2) - (1/3) * (x^2 + 1)^(3/2)", Integral("x^3*sqrt(x^2 + 1)"));
        Assert.Equal("(1/3) * (-x^2 + 1)^(3/2) - (-x^2 + 1)^(1/2)", Integral("x^3/sqrt(1 - x^2)"));
    }

    // x = u^2 - 1 for u = sqrt(x + 1), x = u^3 - 1 for u = nthroot(x + 1, 3).
    [Fact]
    public void x_written_in_a_root()
    {
        Assert.Equal("(2/5) * sqrt(x + 1)^5 - (2/3) * sqrt(x + 1)^3", Integral("x*sqrt(x + 1)"));
        Assert.Equal("-2atan(sqrt(x)) + 2sqrt(x)", Integral("sqrt(x)/(1 + x)"));
        Assert.Equal("(3/7) * nthroot(x + 1, 3)^7 - (3/4) * nthroot(x + 1, 3)^4", Integral("x*nthroot(x + 1, 3)"));
    }

    [Fact]
    public void Powers_of_sin_and_cos()
    {
        Assert.Equal("(1/3) * cos(x)^3 - cos(x)", Integral("sin(x)^3"));
        Assert.Equal("(1/8) * x - (1/32) * sin(4x)", Integral("sin(x)^2*cos(x)^2"));
        Assert.Equal("(3/8) * x + (1/32) * sin(4x) - (1/4) * sin(2x)", Integral("sin(x)^4"));
        Assert.Equal("(1/3) * cosh(x)^3 - cosh(x)", Integral("sinh(x)^3"));
        Assert.Equal("ln(abs(cos(x))) + (1/2) * sec(x)^2", Integral("tan(x)^3"));
    }

    // x * sqrt(x) used to stay a product, which Simplify doesn't merge.
    [Fact]
    public void Products_of_powers_of_one_base() =>
        Assert.Equal("2x^(5/2) / 5", Integral("x*sqrt(x)"));

    // Simplify leaves -(-1 / u), which prints as --1 / u.
    [Fact]
    public void No_double_minus() =>
        Assert.Equal("1 / cos(x)", Integral("sin(x)/cos(x)^2"));

    // g(0) of a part that is singular at 0 used to reach the core's Simplify, which throws for 0^(-1/2).
    [Fact]
    public void A_part_singular_at_0() =>
        Assert.Equal("(1/3) * (-x^2 + 1)^(3/2) - (-x^2 + 1)^(1/2)", Integral("x^3/sqrt(1 - x^2)"));
}
