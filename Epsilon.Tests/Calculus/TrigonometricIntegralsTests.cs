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
    [InlineData("1/(sqrt(2) + cos(x))")]
    [InlineData("1/(2 + sqrt(2)*cos(x))")]
    [InlineData("1/(pi + cos(x) - sin(x))")]
    [InlineData("1/(cos(x) + cosh(1))")]
    [InlineData("1/(cos(x) - sqrt(5))")]
    [InlineData("1/(sqrt(3) + cos(x/2))")]
    [InlineData("1/(1 + 2*cos(x))")]
    [InlineData("1/(1/2 + cos(x))")]
    [InlineData("1/(sqrt(2) + sin(x) + cos(x))")]
    [InlineData("(1 + sin(x))/(sqrt(2) + cos(x))")]
    [InlineData("(2 - 3*cos(x))/(sqrt(7) + sin(x) + cos(x))")]
    [InlineData("1/(1 + sin(x))^2")]
    [InlineData("x^2/sqrt(1 - x^2)")]
    [InlineData("sqrt(4 - x^2)/x^2")]
    [InlineData("(1 - x^2)^(3/2)")]
    [InlineData("1/(x*sqrt(1 - x^2))")]
    [InlineData("x*asin(x)")]
    [InlineData("x*acos(x)")]
    [InlineData("x^2*sqrt(x^2 + 1)")]
    [InlineData("1/(x^2*sqrt(4 + x^2))")]
    [InlineData("1/(x*sqrt(x^2 + 1))")]
    [InlineData("x^2/sqrt(x^2 + 2*x + 5)")]
    [InlineData("1/(1 + x^2)^(3/2)")]
    [InlineData("1/(x^2 + 1)^(5/2)")]
    [InlineData("sqrt(x^2 + 1)/x^2")]
    [InlineData("1/(x^2*sqrt(x^2 - 1))")]
    [InlineData("x^2*sqrt(x^2 - 1)")]
    [InlineData("1/(x^3*sqrt(x^2 - 1))")]
    [InlineData("sqrt(x^2 - 1)/x^2")]
    [InlineData("1/(x^2 - 1)^(3/2)")]
    [InlineData("x^2/sqrt(x^2 - 2*x)")]
    [InlineData("sqrt(x^2 - 4)")]
    [InlineData("exp(x)*cos(x)^2")]
    [InlineData("exp(x)*sin(x)*cos(x)")]
    [InlineData("exp(2*x)*sin(x)^3")]
    [InlineData("exp(x)*sin(x)^2*cos(x)^2")]
    [InlineData("exp(x)*cos(x)^2*sin(2*x)")]
    [InlineData("exp(-x)*cos(3*x)*sin(x + 1)")]
    [InlineData("2^x*sin(x)^2")]
    [InlineData("x*exp(x)*cos(x)^2")]
    [InlineData("cosh(x)*cos(x)^2")]
    [InlineData("cosh(x)^2*cos(x)")]
    [InlineData("cosh(x)^2*cos(x)^2")]
    [InlineData("exp(x)*cosh(x)*cos(x)")]
    [InlineData("exp(-x)*sinh(x)*sin(x)")]
    [InlineData("sinh(x)*cosh(x)*cos(x)")]
    [InlineData("sinh(x)^3*sin(2*x)")]
    [InlineData("1/(2 + cosh(x))")]
    [InlineData("1/(1 + sinh(x))")]
    [InlineData("1/(sinh(x)^2*cosh(x))")]
    [InlineData("cosh(x)^2/sinh(x)^2")]
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

    // Even in both together: t = tan x, with atan(tan(x)/2) written as the angle that goes on.
    [Fact]
    public void Even_in_both()
    {
        Assert.Equal("(1/3) * tan(x)^3", Integral("sin(x)^2/cos(x)^4"));
        Assert.Equal("(1/2) * (x - atan(sin(2x) / (cos(2x) + 3)))", Integral("1/(sin(x)^2 + 4*cos(x)^2)"));
        Assert.Equal("(1/2) * x - tan(x) / (2sec(x)^2)", Integral("tan(x)^2/(1 + tan(x)^2)"));
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
    public void The_half_angle() =>
        Assert.Equal("-2 * (3tan(x / 2)^2 + 3tan(x / 2) + 2) / (3(tan(x / 2) + 1)^3)", Integral("1/(1 + sin(x))^2"));

    // (p + q cos u + r sin u)/(a + b cos u + c sin u) with numbers, rational or not, from the table:
    // alpha u + beta ln|D| and gamma times the integral of 1/D, which is an arctangent with a
    // denominator that stays positive for a^2 > b^2 + c^2, a logarithm for a^2 < b^2 + c^2 and a
    // quotient between.
    [Fact]
    public void Linear_in_cos_and_sin_over_linear_in_cos_and_sin()
    {
        Assert.Equal("(x - 2atan(sin(x) / (cos(x) + sqrt(3) + 2))) / sqrt(3)", Integral("1/(2 + cos(x))"));
        Assert.Equal("(1/4) * (x + 2atan(sin(x) / (-cos(x) + 3)))", Integral("1/(5 - 3*cos(x))"));
        Assert.Equal("x - 2atan(sin(x) / (cos(x) + sqrt(2) + 1))", Integral("1/(sqrt(2) + cos(x))"));
        Assert.Equal("(x - 2atan(sin(x) / (cos(x) + exp(1)))) / sinh(1)", Integral("1/(cos(x) + cosh(1))"));
        Assert.Equal("10 * (x + 2atan((4cos(x) - 9sin(x)) / (4sin(x) + 9cos(x) + sqrt(3) + 10))) / sqrt(3)",
            Integral("1/(1 + 0.9*cos(x) + 0.4*sin(x))"));

        Assert.Equal("(1/4) * ln(abs((-4cos(x) + 3sin(x) + 5) / (5sin(x) + 3)))", Integral("1/(3 + 5*sin(x))"));
        Assert.Equal("ln(abs((-cos(x) + sin(x) + sqrt(2)) / (cos(x) + sin(x)))) / sqrt(2)", Integral("1/(sin(x) + cos(x))"));

        Assert.Equal("x + cos(x) / (sin(x) + 1)", Integral("sin(x)/(1 + sin(x))"));
        Assert.Equal("sin(x) / (cos(x) + 1) - ln(abs(cos(x) + 1))", Integral("(1 + sin(x))/(1 + cos(x))"));
        Assert.Equal("x - ln(abs(cos(x) + sqrt(2))) - 2atan(sin(x) / (cos(x) + sqrt(2) + 1))", Integral("(1 + sin(x))/(sqrt(2) + cos(x))"));
    }

    // atan(p tan(u) + q) jumped by pi where tan u does, and F(2 pi) - F(0) of 1/(2 + cos(x)) was 0.
    // Written as the angle that goes on, F(b) - F(a) is the integral across those points, also
    // where q^2 > 4p, as for sin^2 + 6 sin cos + 10 cos^2, and the denominator of the plain
    // u + atan(N/D) vanishes; and so it is with the table's 1/(a + b cos u + c sin u).
    [Theory]
    [InlineData("1/(2 + cos(x))", 0, 2 * Math.PI)]
    [InlineData("1/(1 + sin(x)^2)", -5, 12)]
    [InlineData("1/(sin(x)^2 + 6*sin(x)*cos(x) + 10*cos(x)^2)", -5, 9)]
    [InlineData("1/(1 + 0.9*cos(x) + 0.4*sin(x))", -7, 9)]
    [InlineData("1/(2 - sin(x))", 0, 20)]
    [InlineData("1/(2 + cos(3*x + 1))", 0, 5)]
    [InlineData("1/(2 + cos(x))^2", -4, 10)]
    [InlineData("sin(x)^2/(1 + cos(x)^2)", 0, 10)]
    [InlineData("1/(1 + sin(x)*cos(x))", -3, 8)]
    [InlineData("1/(5 - 4*cos(x))", 0, 30)]
    [InlineData("1/(sqrt(2) + cos(x))", -7, 20)]
    [InlineData("1/(pi + cos(x) - sin(x))", -10, 10)]
    [InlineData("1/(cos(x) + cosh(1))", 0, 40)]
    [InlineData("(2 - 3*cos(x))/(sqrt(7) + sin(x) + cos(x))", -6, 13)]
    public void The_antiderivative_goes_on_where_tan_is_undefined(string integrand, double a, double b)
    {
        var f = ExprParser.Parse(integrand);
        var antiderivative = f.Integrate("x");

        Assert.Equal(f.Integrate(a, b), antiderivative.Evaluate(("x", b)) - antiderivative.Evaluate(("x", a)), 1e-12);
    }

    [Fact]
    public void Over_a_period_the_antiderivative_gives_the_integral()
    {
        var antiderivative = ExprParser.Parse("1/(2 + cos(x))").Integrate("x");

        Assert.Equal(2 * Math.PI / Math.Sqrt(3), antiderivative.Evaluate(("x", 2 * Math.PI)) - antiderivative.Evaluate(("x", 0)), 1e-15);
    }

    [Fact]
    public void By_parts_with_the_half_angle() =>
        Assert.Equal("tan(x / 2) * x + 2ln(abs(cos(x / 2)))", Integral("x/(1 + cos(x))"));

    // sqrt(q) for q = p((x - h)^2 + k) with p, k < 0: x - h = sqrt(-k) sin(a), sqrt(q) a multiple of cos(a).
    [Fact]
    public void Square_roots_by_sin()
    {
        Assert.Equal("-(1/2) * sqrt(-x^2 + 1) * x + (1/2) * asin(x)", Integral("x^2/sqrt(1 - x^2)"));
        Assert.Equal("-sqrt(-x^2 + 4) / x - asin(x / 2)", Integral("sqrt(4 - x^2)/x^2"));
        Assert.Equal("(1/2) * asin(x) * x^2 + (1/4) * sqrt(-x^2 + 1) * x - (1/4) * asin(x)", Integral("x*asin(x)"));
    }

    // p, k > 0: x - h = sqrt(k) sinh(a), sqrt(q) a multiple of cosh(a).
    [Fact]
    public void Square_roots_by_sinh()
    {
        Assert.Equal("(1/4) * x^3 * sqrt(x^2 + 1) + (1/8) * sqrt(x^2 + 1) * x - (1/8) * asinh(x)", Integral("x^2*sqrt(x^2 + 1)"));
        Assert.Equal("-sqrt(x^2 + 4) / (4x)", Integral("1/(x^2*sqrt(4 + x^2))"));
        Assert.Equal("x / sqrt(x^2 + 1)", Integral("1/(1 + x^2)^(3/2)"));
        Assert.Equal("(1/2) * sqrt(x^2 + 2x + 5) * x - asinh((1/2) * x + 1/2) - (3/2) * sqrt(x^2 + 2x + 5)", Integral("x^2/sqrt(x^2 + 2*x + 5)"));
    }

    // p > 0 > k: x - h = s sqrt(-k) cosh(a) for the sign s of x - h, so the result holds on both
    // sides of the gap.
    [Fact]
    public void Square_roots_by_cosh()
    {
        Assert.Equal("sqrt(x^2 - 1) / x", Integral("1/(x^2*sqrt(x^2 - 1))"));
        Assert.Equal("sqrt(x^2 - 1) / (2x^2) + (1/2) * atan(sqrt(x^2 - 1))", Integral("1/(x^3*sqrt(x^2 - 1))"));
        Assert.Equal("(1/4) * x^3 * sqrt(x^2 - 1) - (1/8) * sqrt(x^2 - 1) * x - (1/8) * ln(abs(x + sqrt(x^2 - 1)))", Integral("x^2*sqrt(x^2 - 1)"));
        Assert.Equal("(1/2) * sqrt(x^2 - 2x) * x + (3/2) * ln(abs(x + sqrt(x^2 - 2x) - 1)) + (3/2) * sqrt(x^2 - 2x)", Integral("x^2/sqrt(x^2 - 2*x)"));
    }

    // Where the sides of the gap differ, Euler's t = x + sqrt(q/p) takes both at once. These were
    // joined by sign(x - h), which the core can't differentiate.
    [Theory]
    [InlineData("1/(x*sqrt(x^2 - 2*x))")]
    [InlineData("1/(x^2*sqrt(x^2 - 2*x))")]
    [InlineData("sqrt(x^2 - 2*x)/x")]
    [InlineData("1/((2*x + 1)*sqrt(x^2 - 2*x))")]
    [InlineData("x/((x + 5)*sqrt(x^2 - 2*x))")]
    [InlineData("1/(x*sqrt(2*x^2 - 4*x))")]
    [InlineData("1/(x*sqrt(3*x^2 - 2*x))")]
    public void Both_sides_of_the_gap_by_Euler_s_substitution(string integrand)
    {
        var f = ExprParser.Parse(integrand);
        var antiderivative = f.Integrate("x");
        Assert.DoesNotContain("sign", antiderivative.Print());

        var derivative = antiderivative.Differentiate("x");
        foreach (double x in new[] { -3.0, -1.7, 2.5, 4.0 })
            Assert.Equal(f.Evaluate(("x", x)), derivative.Evaluate(("x", x)), 1e-12);
    }

    // With an irrational coefficient Euler's substitution leaves no partial fractions, and the
    // sides are integrated apart and joined by sign(x - h): checked by difference quotients.
    [Fact]
    public void Both_sides_of_the_gap_apart()
    {
        var f = ExprParser.Parse("1/((x - pi)*sqrt(x^2 - 2*x))");
        var antiderivative = f.Integrate("x");
        Assert.Contains("sign(x - 1)", antiderivative.Print());

        foreach (double x in new[] { -3.0, -0.5, 2.5, 4.0 })
        {
            const double h = 1e-5;
            double slope = (antiderivative.Evaluate(("x", x + h)) - antiderivative.Evaluate(("x", x - h))) / (2 * h);
            Assert.Equal(f.Evaluate(("x", x)), slope, 1e-7);
        }
    }

    // Rational functions of exp, sinh and cosh by t = exp(x), and quotients of equal powers.
    [Fact]
    public void Hyperbolic_functions()
    {
        Assert.Equal("ln(abs((exp(x) - sqrt(3) + 2) / (exp(x) + sqrt(3) + 2))) / sqrt(3)", Integral("1/(2 + cosh(x))"));
        Assert.Equal("x - coth(x)", Integral("cosh(x)^2/sinh(x)^2"));
    }

    // x elsewhere than in sin and cos, and roots of a cubic and a quartic: non-elementary.
    [Theory]
    [InlineData("x/(2 + cos(x))")]
    [InlineData("sin(x)/x")]
    [InlineData("sqrt(1 + x^3)")]
    [InlineData("1/sqrt(x^4 + 1)")]
    public void What_isnt_integrated(string integrand) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));
}
