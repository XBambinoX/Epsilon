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

    // Neither odd nor even: the substitution t = tan(x/2), not there yet.
    [Theory]
    [InlineData("1/(2 + cos(x))")]
    [InlineData("sin(x)/(1 + sin(x))")]
    [InlineData("cos(2*x)/(cos(x) - sin(x))")]
    public void What_isnt_integrated_yet(string integrand) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));
}
