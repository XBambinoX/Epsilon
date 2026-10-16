using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class TrigonometricIntegralsTests
{
    private static readonly double[] Points = [-2.7, -1.3, -0.6, 0.35, 0.8, 1.7, 3.1];

    private static string Integral(string integrand) => ExprParser.Parse(integrand).Integrate("x").Print();

    [Theory]
    [InlineData("sin(x)^3/(1 + cos(x)^2)")]
    [InlineData("sin(3*x)/(2 + cos(x))")]
    [InlineData("tan(x)/(1 + cos(x))")]
    [InlineData("1/(sin(2*x) + 2*sin(x))")]
    [InlineData("sin(x)*cos(2*x)/(1 + cos(x))")]
    [InlineData("cos(x)^5/sin(x)^2")]
    [InlineData("sin(5*x)*cos(x)/(1 + cos(x)^2)")]
    [InlineData("1/(sin(x)*cos(x))")]
    [InlineData("1/(1 + sin(x)^2)")]
    [InlineData("1/(sin(x)^2 + 4*cos(x)^2)")]
    [InlineData("sin(x)^2/cos(x)^4")]
    [InlineData("sin(x)^4/cos(x)^2")]
    [InlineData("1/(sin(x)^4*cos(x)^2)")]
    [InlineData("1/(1 + tan(x))")]
    [InlineData("sec(x)^3*tan(x)^3")]
    [InlineData("sin(2*x)/(1 + cos(x)^2)")]
    [InlineData("1/(sin(2*x + 1)*cos(2*x + 1))")]
    [InlineData("sin(x/2)^3/(1 + cos(x/2)^2)")]
    [InlineData("1/(2 + cos(x))")]
    [InlineData("1/(1 + sin(x))")]
    [InlineData("sin(x)/(1 + sin(x))")]
    [InlineData("cos(2*x)/(cos(x) - sin(x))")]
    [InlineData("1/(3 + 5*sin(x))")]
    [InlineData("1/(sin(x) + cos(x))")]
    [InlineData("1/(1 + sin(x) + cos(x))")]
    [InlineData("(1 + sin(x))/(1 + cos(x))")]
    [InlineData("1/(2 + cos(3*x + 1))")]
    [InlineData("1/(2 + cos(x))^2")]
    [InlineData("x/(1 + cos(x))")]
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

    // Odd in sin: t = cos x.
    [Fact]
    public void Odd_in_sin()
    {
        Assert.Equal("cos(x) - 2atan(cos(x))", Integral("sin(x)^3/(1 + cos(x)^2)"));
        Assert.Equal("8cos(x) - 2cos(x)^2 - 15ln(abs(cos(x) + 2))", Integral("sin(3*x)/(2 + cos(x))"));
    }

    // Odd in cos: t = sin x.
    [Fact]
    public void Odd_in_cos() =>
        Assert.Equal("(1/3) * sin(x)^3 - 1 / sin(x) - 2sin(x)", Integral("cos(x)^5/sin(x)^2"));

    // Even in both together: t = tan x, with atan(tan(x)) written as x.
    [Fact]
    public void Even_in_both()
    {
        Assert.Equal("(1/3) * tan(x)^3", Integral("sin(x)^2/cos(x)^4"));
        Assert.Equal("(1/2) * atan(tan(x) / 2)", Integral("1/(sin(x)^2 + 4*cos(x)^2)"));
        Assert.Equal("(1/2) * x - tan(x) / (2tan(x)^2 + 2)", Integral("tan(x)^2/(1 + tan(x)^2)"));
    }

    // Where several rules apply, the shortest result: t = tan x here.
    [Fact]
    public void The_shortest_of_the_rules() =>
        Assert.Equal("ln(abs(tan(x)))", Integral("1/(sin(x)*cos(x))"));

    // sin(k x) and cos(k x) by the multiple-angle formulas, and any linear argument.
    [Fact]
    public void Multiple_angles_and_linear_arguments()
    {
        Assert.Equal("-ln(cos(x)^2 + 1)", Integral("sin(2*x)/(1 + cos(x)^2)"));
        Assert.Equal("ln(abs(tan(2x + 1))) / 2", Integral("1/(sin(2*x + 1)*cos(2*x + 1))"));
        Assert.Equal("ln(abs(tan(a * x))) / a", Integral("1/(sin(a*x)*cos(a*x))"));
    }

    // Neither odd nor even: t = tan(x/2), with sin x = 2t/(1 + t^2) and cos x = (1 - t^2)/(1 + t^2).
    [Fact]
    public void The_half_angle()
    {
        Assert.Equal("2atan(tan(x / 2) / sqrt(3)) / sqrt(3)", Integral("1/(2 + cos(x))"));
        Assert.Equal("(1/2) * atan(2tan(x / 2))", Integral("1/(5 - 3*cos(x))"));
        Assert.Equal("x + 2 / (tan(x / 2) + 1)", Integral("sin(x)/(1 + sin(x))"));
        Assert.Equal("ln(tan(x / 2)^2 + 1) + tan(x / 2)", Integral("(1 + sin(x))/(1 + cos(x))"));
    }

    // tan(x/2) jumps at pi, and with it the antiderivative: F(2 pi) - F(0) isn't the integral.
    [Fact]
    public void The_antiderivative_jumps_with_the_half_angle()
    {
        var f = ExprParser.Parse("1/(2 + cos(x))");
        var antiderivative = f.Integrate("x");

        Assert.Equal(0, antiderivative.Evaluate(("x", 2 * Math.PI)) - antiderivative.Evaluate(("x", 0)), 1e-15);
        Assert.Equal(3.627598728468432, f.Integrate(0, 2 * Math.PI));
    }

    [Fact]
    public void By_parts_with_the_half_angle() =>
        Assert.Equal("tan(x / 2) * x + 2ln(abs(cos(x / 2)))", Integral("x/(1 + cos(x))"));

    // x elsewhere than in sin and cos: non-elementary, or wanting an identity first.
    [Theory]
    [InlineData("x/(2 + cos(x))")]
    [InlineData("sin(x)/x")]
    [InlineData("exp(x)*cos(x)^2")]
    public void What_isnt_integrated(string integrand) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));
}
