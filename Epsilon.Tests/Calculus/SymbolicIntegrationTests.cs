using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class SymbolicIntegrationTests
{
    private static readonly double[] Points = [-2.7, -1.3, -0.6, 0.35, 0.8, 1.7, 3.1];

    private static string Integral(string integrand) => ExprParser.Parse(integrand).Integrate("x").Print();
    [Theory]
    [InlineData("5")]
    [InlineData("3*x^2 - 2*x + 1")]
    [InlineData("x^pi")]
    [InlineData("(3 - x)^(-1)")]
    [InlineData("x^(-3/2)")]
    [InlineData("nthroot(x, 3)^2")]
    [InlineData("1/nthroot(x, 3)")]
    [InlineData("nthroot(2*x + 1, 5)")]
    [InlineData("exp(x)^2")]
    [InlineData("1/exp(x)")]
    [InlineData("ln(2*x)")]
    [InlineData("ln(abs(x))")]
    [InlineData("tan(x)")]
    [InlineData("cot(x)")]
    [InlineData("sec(x)")]
    [InlineData("csc(x)")]
    [InlineData("tanh(x)")]
    [InlineData("coth(x)")]
    [InlineData("sech(x)")]
    [InlineData("csch(x)")]
    [InlineData("acos(x/2)")]
    [InlineData("atan(x)")]
    [InlineData("asinh(x)")]
    [InlineData("acosh(x)")]
    [InlineData("atanh(x/4)")]
    [InlineData("abs(2*x - 1)")]
    [InlineData("sign(x)")]
    [InlineData("min(x, 1)")]
    [InlineData("max(x, -x)")]
    [InlineData("cos(2*x)^2")]
    [InlineData("tan(x)^2")]
    [InlineData("csc(x)^2")]
    [InlineData("1/sin(x)")]
    [InlineData("sinh(x)^2")]
    [InlineData("sech(x)^2")]
    [InlineData("csc(x)*cot(x)")]
    [InlineData("sin(3*x)*cos(2*x)")]
    [InlineData("sin(x)*sin(2*x)")]
    [InlineData("cos(x)*cos(x + 1)")]
    [InlineData("(x + 1)^2*(x - 1)")]
    [InlineData("x^3/(x - 1)")]
    [InlineData("(x^4 + 1)/(x^2 + 2*x + 5)")]
    [InlineData("(x^2 + 1)/(x^2 - 1)")]
    [InlineData("1/(4 - x^2)")]
    [InlineData("(2*x + 3)/(x^2 + x + 1)")]
    [InlineData("1/(2*x^2 + 3)")]
    [InlineData("1/sqrt(4 - x^2)")]
    [InlineData("sqrt(x^2 - 1)")]
    [InlineData("(x + 2)/sqrt(9 - x^2)")]
    [InlineData("1/sqrt(x^2 + 2*x + 2)")]
    [InlineData("1/sqrt(x^2 + 2*x + 1)")]
    public void The_derivative_of_the_antiderivative_is_the_integrand(string integrand)
    {
        var f = ExprParser.Parse(integrand);
        var antiderivative = f.Integrate("x");

        int checkedPoints = 0;
        foreach (double x in Points)
        {
            double expected = f.Evaluate(("x", x));
            if (!double.IsFinite(expected))
                continue;

            Assert.Equal(expected, Derivative(antiderivative, x), 1e-6 * Math.Max(1, Math.Abs(expected)));
            checkedPoints++;
        }

        Assert.True(checkedPoints >= 2);
    }

    private static double Derivative(Expr antiderivative, double x)
    {
        try
        {
            return antiderivative.Differentiate("x").Evaluate(("x", x));
        }
        catch (NotSupportedException)
        {
            const double H = 1e-6;
            return (antiderivative.Evaluate(("x", x + H)) - antiderivative.Evaluate(("x", x - H))) / (2 * H);
        }
    }

    [Fact]
    public void Powers_with_exact_exponents_and_the_logarithm_of_abs()
    {
        Assert.Equal("x^2 / 2", Integral("x"));
        Assert.Equal("x^3 - x^2 + x", Integral("3*x^2 - 2*x + 1"));
        Assert.Equal("2x^(3/2) / 3", Integral("sqrt(x)"));
        Assert.Equal("-1 / x", Integral("1/x^2"));
        Assert.Equal("(2x + 1)^6 / 12", Integral("(2*x + 1)^5"));
        Assert.Equal("ln(abs(x))", Integral("1/x"));
        Assert.Equal("ln(abs(2x - 1)) / 2", Integral("1/(2*x - 1)"));
    }

    [Fact]
    public void Real_roots_stay_real_for_negative_arguments()
    {
        var antiderivative = ExprParser.Parse("nthroot(x, 3)").Integrate("x");

        Assert.Equal("3nthroot(x, 3) * x / 4", antiderivative.Print());
        Assert.Equal(12, antiderivative.Evaluate(("x", -8)), 1e-14);
        Assert.Equal(-2, antiderivative.Differentiate("x").Evaluate(("x", -8)), 1e-14);
        Assert.Equal("3nthroot(x, 3)^2 / 2", Integral("1/nthroot(x, 3)"));
    }

    [Fact]
    public void Elementary_functions_of_a_linear_argument()
    {
        Assert.Equal("-exp(-3x) / 3", Integral("exp(-3*x)"));
        Assert.Equal("e^(2x) / 2", Integral("e^(2*x)"));
        Assert.Equal("2^x / ln(2)", Integral("2^x"));
        Assert.Equal("ln(x) * x - x", Integral("ln(x)"));
        Assert.Equal("sin(3x + 1) / 3", Integral("cos(3*x + 1)"));
        Assert.Equal("-ln(abs(cos(x)))", Integral("tan(x)"));
        Assert.Equal("ln(abs(sec(x) + tan(x)))", Integral("sec(x)"));
        Assert.Equal("atan(sinh(x))", Integral("sech(x)"));
        Assert.Equal("asin(x) * x + sqrt(-x^2 + 1)", Integral("asin(x)"));
        Assert.Equal("abs(x) * x / 2", Integral("abs(x)"));
        Assert.Equal("abs(x)", Integral("sign(x)"));
    }

    [Fact]
    public void Squares_reciprocals_and_products_of_trigonometric_functions()
    {
        Assert.Equal("(1/2) * x - (1/4) * sin(2x)", Integral("sin(x)^2"));
        Assert.Equal("tan(x)", Integral("sec(x)^2"));
        Assert.Equal("tan(x)", Integral("1/cos(x)^2"));
        Assert.Equal("sec(x)", Integral("sec(x)*tan(x)"));
        Assert.Equal("-cos(2x) / 4", Integral("sin(x)*cos(x)"));
    }

    [Fact]
    public void Quadratic_denominators()
    {
        Assert.Equal("atan(x)", Integral("1/(1 + x^2)"));
        Assert.Equal("atan(x / 2) / 2", Integral("1/(x^2 + 4)"));
        Assert.Equal("atan((x + 1) / 2) / 2", Integral("1/(x^2 + 2*x + 5)"));
        Assert.Equal("(1/2) * ln(abs((x - 1) / (x + 1)))", Integral("1/(x^2 - 1)"));
        Assert.Equal("-1 / (x + 1)", Integral("1/(x^2 + 2*x + 1)"));
        Assert.Equal("(1/2) * ln(x^2 + 1)", Integral("x/(x^2 + 1)"));
        Assert.Equal("ln(abs((x - sqrt(2)) / (x + sqrt(2)))) / (2sqrt(2))", Integral("1/(x^2 - 2)"));
    }

    // Around the vertex, with the fraction of the shift cleared.
    [Fact]
    public void Quadratics_with_a_fractional_vertex()
    {
        Assert.Equal("2atan((2x + 1) / sqrt(3)) / sqrt(3)", Integral("1/(x^2 + x + 1)"));
        Assert.Equal("ln(abs((2x - sqrt(5) + 1) / (2x + sqrt(5) + 1))) / sqrt(5)", Integral("1/(x^2 + x - 1)"));
        Assert.Equal("asinh((2x + 1) / sqrt(3))", Integral("1/sqrt(x^2 + x + 1)"));
        Assert.Equal("asin((2x + 1) / sqrt(5))", Integral("1/sqrt(1 - x - x^2)"));
    }

    [Fact]
    public void Square_roots_of_quadratics()
    {
        Assert.Equal("asin(x)", Integral("1/sqrt(1 - x^2)"));
        Assert.Equal("asinh(x)", Integral("1/sqrt(x^2 + 1)"));
        Assert.Equal("ln(abs(x + sqrt(x^2 - 1)))", Integral("1/sqrt(x^2 - 1)"));
        Assert.Equal("(1/2) * sqrt(-x^2 + 1) * x + (1/2) * asin(x)", Integral("sqrt(1 - x^2)"));
        Assert.Equal("sqrt(x^2 + 1)", Integral("x/sqrt(x^2 + 1)"));
    }

    [Fact]
    public void Polynomials_are_divided_and_products_expanded()
    {
        Assert.Equal("(1/3) * x^3 + (1/2) * x^2", Integral("x*(x + 1)"));
        Assert.Equal("(1/2) * x^2 + ln(abs(x))", Integral("(x^2 + 1)/x"));
        Assert.Equal("x + 2ln(abs(x - 1))", Integral("(x + 1)/(x - 1)"));
        Assert.Equal("(1/2) * x - (3/4) * ln(abs(2x + 3))", Integral("x/(2*x + 3)"));
        Assert.Equal("x - atan(x)", Integral("x^2/(x^2 + 1)"));
    }

    [Fact]
    public void Agrees_with_the_numeric_integral()
    {
        foreach (var (integrand, a, b) in new[] { ("sqrt(1 - x^2)", -1.0, 1.0), ("1/(x^2 + 2*x + 5)", -3.0, 2.0), ("x^3/(x - 1)", 2.0, 4.0), ("atan(x)", 0.0, 1.0) })
        {
            var f = ExprParser.Parse(integrand);
            var antiderivative = f.Integrate("x");
            double exact = antiderivative.Evaluate(("x", b)) - antiderivative.Evaluate(("x", a));

            Assert.Equal(f.Integrate(a, b), exact, 1e-12);
        }
    }

    [Fact]
    public void Other_variables_are_constants()
    {
        Assert.Equal("x^2 * a / 2", Integral("a*x"));
        Assert.Equal("-cos(a * x) / a", Integral("sin(a*x)"));
        Assert.Equal("y^2 * x / 2", ExprParser.Parse("x*y").Integrate("y").Print());
        Assert.Equal("sin(x) * t", ExprParser.Parse("sin(x)").Integrate("t").Print());
    }

    [Fact]
    public void Without_a_name_the_only_variable()
    {
        Assert.Equal("t^3 / 3", ExprParser.Parse("t^2").Integrate().Print());
        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("x*y").Integrate());
        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("5").Integrate());
    }

    // Non-elementary, a denominator whose roots can't be written, a step, a quadratic whose sign
    // isn't known, the double angle, and the root of a cubic.
    [Theory]
    [InlineData("exp(x^2)")]
    [InlineData("1/(x^5 - x - 1)")]
    [InlineData("floor(x)")]
    [InlineData("1/(x^2 + a^2)")]
    [InlineData("exp(x)*cos(x)^2")]
    [InlineData("sqrt(1 + x^3)")]
    public void What_no_rule_fits_throws(string integrand) =>
        Assert.Equal($"No antiderivative of {ExprParser.Parse(integrand).Print()} by x was found.",
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x")).Message);

    // The rules are those of real functions: ln|x + i| isn't an antiderivative of 1/(x + i).
    [Theory]
    [InlineData("1/(x + i)")]
    [InlineData("1/(x^2 + i)")]
    [InlineData("exp(i*x)")]
    [InlineData("x^i")]
    public void The_imaginary_unit_elsewhere_throws(string integrand) =>
        Assert.Equal($"No antiderivative of {ExprParser.Parse(integrand).Print()} by x was found: the imaginary unit is integrated only as a constant factor or term.",
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x")).Message);

    [Fact]
    public void The_imaginary_unit_as_a_constant_factor_or_term()
    {
        Assert.Equal("i * x^2 / 2", Integral("i*x"));
        Assert.Equal("(1/2) * x^2 + i * x", Integral("x + i"));
        Assert.Equal("i * ln(abs(x))", Integral("i/x"));
        Assert.Equal("-x^2 / 2", Integral("i^2*x"));
    }

    [Fact]
    public void Null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Integrate("x"));
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Integrate());
        Assert.Throws<ArgumentNullException>(() => ExprParser.Parse("x").Integrate(null!));
    }
}
