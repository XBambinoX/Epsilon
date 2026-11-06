using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class ExactIntegrationTests
{
    private const double Inf = double.PositiveInfinity;

    private static Expr Integral(string expr, string a, string b) =>
        ExprParser.Parse(expr).IntegrateExact("x", ExprParser.Parse(a), ExprParser.Parse(b));

    // "inf" and "-inf" as doubles, anything else as an expression, through the overload for the mix.
    private static Expr Improper(string expr, string a, string b)
    {
        Expr f = ExprParser.Parse(expr);
        double? x = Infinity(a), y = Infinity(b);
        return (x, y) switch
        {
            ({ } p, { } q) => f.IntegrateExact("x", p, q),
            ({ } p, null) => f.IntegrateExact("x", p, ExprParser.Parse(b)),
            (null, { } q) => f.IntegrateExact("x", ExprParser.Parse(a), q),
            _ => f.IntegrateExact("x", ExprParser.Parse(a), ExprParser.Parse(b)),
        };
    }

    private static double? Infinity(string limit) => limit switch
    {
        "inf" => double.PositiveInfinity,
        "-inf" => double.NegativeInfinity,
        _ => null,
    };

    [Theory]
    [InlineData("x^2", "0", "1", "1/3")]
    [InlineData("x^2", "1", "0", "-1/3")]
    [InlineData("1/(1 + x^2)", "0", "1", "(1/4) * π")]
    [InlineData("sin(x)", "0", "pi", "2")]
    [InlineData("1/x", "1", "e", "1")]
    [InlineData("1/x", "-e", "-1", "-1")]
    [InlineData("exp(-x)", "0", "ln(2)", "1/2")]
    [InlineData("x*exp(-x)", "0", "1", "-2exp(-1) + 1")]
    [InlineData("atan(x)", "0", "1", "(-1/2) * ln(2) + (1/4) * π")]
    [InlineData("asin(x)", "0", "1", "(1/2) * π - 1")]
    [InlineData("acos(x)", "-1", "1", "π")]
    [InlineData("1/(x^2 - 1)", "2", "3", "(1/2) * ln(3/2)")]
    [InlineData("1/(4 - x^2)", "-1", "1", "(1/2) * ln(3)")]
    [InlineData("1/(x^2 - 2)", "0", "1", "ln(-2sqrt(2) + 3) / (2sqrt(2))")]
    [InlineData("x/(x^4 - 1)", "2", "3", "(1/4) * ln(4/3)")]
    [InlineData("1/(x^3 + 1)", "0", "1", "(1/3) * ln(2) + (1/9) * π * sqrt(3)")]
    [InlineData("1/(x^2 + 4)", "-2", "2*sqrt(3)", "(7/24) * π")]
    [InlineData("1/(x*sqrt(x^2 - 2*x))", "2", "4", "sqrt(2) / 2")]
    [InlineData("1/(x*sqrt(x^2 - 1))", "1", "2", "(1/3) * π")]
    [InlineData("1/sqrt(x^2 - 1)", "1", "2", "ln(sqrt(3) + 2)")]
    [InlineData("sqrt(1 - x^2)", "-1", "1", "(1/2) * π")]
    [InlineData("ln(x)/x", "1", "e", "1/2")]
    [InlineData("1/(x*ln(x))", "e", "e^2", "ln(2)")]
    [InlineData("1/cosh(x)", "-1", "1", "2atan(sinh(1))")]
    [InlineData("x*sin(x)", "-pi", "pi", "2 * π")]
    public void Newton_Leibniz(string expr, string a, string b, string integral) =>
        Assert.Equal(integral, Integral(expr, a, b).Print());

    // A period of tan(x/2) and the other forms Bioche's rules and the table give is crossed whole.
    [Theory]
    [InlineData("1/(2 + cos(x))", "0", "2*pi", "2 * π / sqrt(3)")]
    [InlineData("1/(1 + cos(x)^2)", "0", "pi", "π / sqrt(2)")]
    [InlineData("1/(cos(x) + 2*sin(x) + 3)", "0", "2*pi", "π")]
    public void Over_a_period(string expr, string a, string b, string integral) =>
        Assert.Equal(integral, Integral(expr, a, b).Print());

    [Theory]
    [InlineData("ln(x)", "0", "1", "-1")]
    [InlineData("ln(x)^2", "0", "1", "2")]
    [InlineData("x*ln(x)", "0", "1", "-1/4")]
    [InlineData("sqrt(x)", "0", "1", "2/3")]
    [InlineData("1/sqrt(x)", "0", "4", "4")]
    [InlineData("x/sqrt(4 - x^2)", "0", "2", "2")]
    [InlineData("1/nthroot(x, 3)", "-1", "8", "9/2")]
    [InlineData("1/(1 + tan(x))", "0", "pi/2", "(1/4) * π")]
    [InlineData("x*atan(1/x)", "-1", "1", "1")]
    public void Where_the_antiderivative_is_undefined_its_limit_is_taken(string expr, string a, string b, string integral) =>
        Assert.Equal(integral, Integral(expr, a, b).Print());

    [Theory]
    [InlineData("sign(x)", "-1", "2", "1")]
    [InlineData("abs(x)", "-1", "2", "5/2")]
    [InlineData("floor(x)", "0", "5/2", "2")]
    [InlineData("ceiling(x)", "-3/2", "7/3", "7/2")]
    [InlineData("round(x)", "0", "3", "9/2")]
    public void Steps_and_kinks(string expr, string a, string b, string integral) =>
        Assert.Equal(integral, Integral(expr, a, b).Print());

    // sec(x)^2/(1 + tan(x)^2) is 1 wherever it is defined, and by u = tan(x) its antiderivative
    // is atan(tan(x)), which falls by pi at each pole of tan.
    [Theory]
    [InlineData("sec(x)^2/(1 + tan(x)^2)", "0", "10", "10")]
    [InlineData("sec(x)^2/(4 + tan(x)^2)", "0", "10", "(1/2) * atan(tan(10) / 2) + (3/2) * π")]
    public void The_jumps_of_the_antiderivative_are_taken_off(string expr, string a, string b, string integral)
    {
        Assert.Equal(integral, Integral(expr, a, b).Print());
        Assert.Equal(ExprParser.Parse(expr).Integrate("x", 0, 10), Integral(expr, a, b).Evaluate(), 1e-12);
    }

    [Theory]
    [InlineData("x^3*cos(x)", "-2", "2")]
    [InlineData("sin(x)^3", "-pi", "pi")]
    [InlineData("x*exp(x^4)", "-3", "3")]
    [InlineData("sign(x)*x^2", "-1", "1")]
    [InlineData("sin(x)/(1 + x^2)", "-1", "1")]
    [InlineData("atan(1/x)", "-1", "1")]
    [InlineData("x^3", "-c", "c")]
    public void An_odd_integrand_over_a_symmetric_interval_gives_0(string expr, string a, string b) =>
        Assert.Equal("0", Integral(expr, a, b).Print());

    [Theory]
    [InlineData("1/(2 + sin(x))", 0, 10)]
    [InlineData("1/(3 + sin(x))^2", 0, 10)]
    [InlineData("1/(2 + cos(x))^2", 0, 10)]
    [InlineData("sin(x)^2/(2 + cos(x))", 0, 10)]
    [InlineData("1/(5 + 4*cos(x))", -7, 9)]
    [InlineData("1/(1 + sin(x)^2)^2", 0, 10)]
    [InlineData("1/(2 - cos(x))^3", 0, 10)]
    [InlineData("1/(2 + tan(x)^2)", 0, 10)]
    [InlineData("1/(sin(x)^4 + cos(x)^4)", 0, 10)]
    [InlineData("1/(sin(x) + cos(x) + 2)", 0, 10)]
    [InlineData("sin(x)/(1 + sin(x)^2)", 0, 10)]
    [InlineData("1/(1 + sin(x))", 0, 4)]
    [InlineData("1/(2 + cos(x))", 0, 1000)]
    [InlineData("exp(x)*cos(x)^2", 0, 3)]
    [InlineData("1/(x^4 + 1)", -1, 2)]
    [InlineData("1/(x^2 + x + 1)^2", -3, 3)]
    public void Agrees_with_the_numeric_integral(string expr, double a, double b)
    {
        Expr f = ExprParser.Parse(expr);
        double numeric = f.Integrate("x", a, b);
        Assert.Equal(numeric, f.IntegrateExact("x", a, b).Evaluate(), 1e-10 * Math.Max(1, Math.Abs(numeric)));
    }

    [Theory]
    [InlineData("1/x", "-1", "1", "0")]
    [InlineData("1/x^2", "-1", "1", "0")]
    [InlineData("1/x^3", "-1", "1", "0")]
    [InlineData("exp(1/x)/x^2", "-1", "1", "0")]
    [InlineData("tan(x)", "0", "pi", "(1/2) * π")]
    [InlineData("cos(x)/(sin(x) - 1/2)", "0", "1", "(1/6) * π")]
    [InlineData("1/(1 + sin(x))^2", "0", "2*pi", "3 * π / 2")]
    [InlineData("1/(x - 1)", "1", "2", "1")]
    public void Diverges(string expr, string a, string b, string at)
    {
        var e = Assert.Throws<InvalidOperationException>(() => Integral(expr, a, b));
        Assert.EndsWith($"diverges at x = {at}.", e.Message);
    }

    [Theory]
    [InlineData("exp(-x)", "0", "inf", "1")]
    [InlineData("exp(-x)", "inf", "0", "-1")]
    [InlineData("exp(-x)", "pi", "inf", "exp(-π)")]
    [InlineData("1/(1 + x^2)", "-inf", "inf", "π")]
    [InlineData("1/(1 + x^2)", "sqrt(3)", "inf", "(1/6) * π")]
    [InlineData("1/(1 + x^2)", "-inf", "1", "(3/4) * π")]
    [InlineData("1/x^2", "1", "inf", "1")]
    [InlineData("x^3*exp(-x)", "0", "inf", "6")]
    [InlineData("x*exp(x)", "-inf", "0", "-1")]
    [InlineData("exp(-x)*sin(x)", "0", "inf", "1/2")]
    [InlineData("exp(-2*x)*cos(3*x)", "0", "inf", "2/13")]
    [InlineData("exp(-x)*cos(x)^2", "0", "inf", "3/5")]
    [InlineData("1/(x^4 + 1)", "0", "inf", "(1/4) * π * sqrt(2)")]
    [InlineData("1/(x^3 + 1)", "0", "inf", "(2/9) * π * sqrt(3)")]
    [InlineData("1/(1 + x^2)^2", "-inf", "inf", "(1/2) * π")]
    [InlineData("1/((x + 1)*(x + 2))", "0", "inf", "ln(2)")]
    [InlineData("1/(x^2 - 1)", "2", "inf", "(1/2) * ln(3)")]
    [InlineData("ln(x)/x^2", "1", "inf", "1")]
    [InlineData("1/(x*ln(x)^2)", "e", "inf", "1")]
    [InlineData("1/(1 + exp(x))", "0", "inf", "ln(2)")]
    [InlineData("1/cosh(x)", "-inf", "inf", "π")]
    [InlineData("atan(x)/(1 + x^2)", "0", "inf", "(1/8) * π^2")]
    [InlineData("1/(sqrt(x)*(1 + x))", "0", "inf", "π")]
    [InlineData("exp(-sqrt(x))/sqrt(x)", "0", "inf", "2")]
    [InlineData("1/(x*sqrt(x^2 - 1))", "-inf", "-1", "(-1/2) * π")]
    [InlineData("exp(x)", "-inf", "c", "exp(c)")]
    [InlineData("exp(-x)", "inf", "inf", "0")]
    public void Infinite_limits(string expr, string a, string b, string integral) =>
        Assert.Equal(integral, Improper(expr, a, b).Print());

    // |f| <= c/x^(3/2) far out: sin(x)/(1 + x^2) has no elementary antiderivative.
    [Theory]
    [InlineData("x/(x^4 + 1)")]
    [InlineData("x^3/(1 + x^6)")]
    [InlineData("x*exp(-x^2)")]
    [InlineData("sin(x)/(1 + x^2)")]
    public void An_odd_integrand_that_decays_fast_over_the_line_gives_0(string expr) =>
        Assert.Equal("0", ExprParser.Parse(expr).IntegrateExact("x", -Inf, Inf).Print());

    [Theory]
    [InlineData("1/x", "1", "inf", "inf")]
    [InlineData("1/sqrt(x)", "0", "inf", "inf")]
    [InlineData("1", "0", "inf", "inf")]
    [InlineData("x", "-inf", "inf", "inf")]
    [InlineData("x/(1 + x^2)", "-inf", "inf", "inf")]
    [InlineData("x^2*exp(-x)", "-inf", "0", "-inf")]
    [InlineData("1/(2 + cos(x))", "0", "inf", "inf")]
    [InlineData("1/(x^2 - 1)", "0", "inf", "x = 1")]
    public void Diverges_over_an_infinite_interval(string expr, string a, string b, string at)
    {
        var e = Assert.Throws<InvalidOperationException>(() => Improper(expr, a, b));
        Assert.EndsWith($"diverges at {at}.", e.Message);
    }

    // Integrands without an elementary antiderivative, over the intervals they are known on.
    [Theory]
    [InlineData("exp(-x^2)", "-inf", "inf", "sqrt(π)")]
    [InlineData("exp(-x^2)", "0", "inf", "sqrt(π) / 2")]
    [InlineData("exp(-x^2)", "-inf", "0", "sqrt(π) / 2")]
    [InlineData("exp(-x^2)", "inf", "0", "-sqrt(π) / 2")]
    [InlineData("exp(-x^2/2)/sqrt(2*pi)", "-inf", "inf", "1")]
    [InlineData("x^2*exp(-x^2/2)/sqrt(2*pi)", "-inf", "inf", "1")]
    [InlineData("x^2*exp(-x^2)", "-inf", "inf", "(1/2) * sqrt(π)")]
    [InlineData("x^4*exp(-x^2)", "0", "inf", "3sqrt(π) / 8")]
    [InlineData("(1 + x^2)*exp(-x^2)", "-inf", "inf", "(3/2) * sqrt(π)")]
    [InlineData("exp(-(x-1)^2)", "-inf", "inf", "sqrt(π)")]
    [InlineData("exp(-2*x^2 + 3*x + 1)", "-inf", "inf", "exp(17/8) * sqrt(π / 2)")]
    [InlineData("x*exp(-2*x^2 + 3*x + 1)", "-inf", "inf", "(3/4) * exp(17/8) * sqrt(π / 2)")]
    [InlineData("x^3*exp(-x^2 + x)", "-inf", "inf", "(7/8) * exp(1/4) * sqrt(π)")]
    [InlineData("cos(x)*exp(-x^2)", "-inf", "inf", "exp(-1/4) * sqrt(π)")]
    [InlineData("cos(x)*exp(-x^2)", "0", "inf", "exp(-1/4) * sqrt(π) / 2")]
    [InlineData("cos(2*x)*exp(-x^2 + x)", "-inf", "inf", "cos(1) * exp(-3/4) * sqrt(π)")]
    [InlineData("sin(2*x)*exp(-x^2 + x)", "-inf", "inf", "exp(-3/4) * sin(1) * sqrt(π)")]
    [InlineData("sqrt(x)*exp(-x)", "0", "inf", "(1/2) * sqrt(π)")]
    [InlineData("exp(-x)/sqrt(x)", "0", "inf", "sqrt(π)")]
    [InlineData("x^(3/2)*exp(-2*x)", "0", "inf", "3sqrt(π) / (16sqrt(2))")]
    [InlineData("sin(x)/x", "0", "inf", "π / 2")]
    [InlineData("sin(x)/x", "-inf", "inf", "π")]
    [InlineData("sin(-2*x)/x", "0", "inf", "-π / 2")]
    [InlineData("sin(x)^2/x^2", "0", "inf", "π / 2")]
    [InlineData("sin(2*x)^2/x^2", "-inf", "inf", "2 * π")]
    [InlineData("exp(-x)*sin(x)/x", "0", "inf", "π / 4")]
    [InlineData("exp(-2*x)*sin(3*x)/x", "0", "inf", "atan(3/2)")]
    [InlineData("sin(x^2)", "0", "inf", "sqrt(π / 2) / 2")]
    [InlineData("cos(x^2)", "-inf", "inf", "sqrt(π / 2)")]
    [InlineData("x/(exp(x) - 1)", "0", "inf", "(1/6) * π^2")]
    [InlineData("x^3/(exp(x) - 1)", "0", "inf", "(1/15) * π^4")]
    [InlineData("x^5/(exp(2*x) - 1)", "0", "inf", "π^6 / 504")]
    [InlineData("x/(exp(x) + 1)", "0", "inf", "(1/12) * π^2")]
    [InlineData("x^3/(exp(x) + 1)", "0", "inf", "(7/120) * π^4")]
    [InlineData("ln(x)/(1 - x)", "0", "1", "(-1/6) * π^2")]
    [InlineData("ln(x)/(x - 1)", "0", "1", "(1/6) * π^2")]
    [InlineData("ln(x)/(1 + x)", "0", "1", "(-1/12) * π^2")]
    [InlineData("ln(1 + x)/x", "0", "1", "(1/12) * π^2")]
    [InlineData("ln(1 - x)/x", "0", "1", "(-1/6) * π^2")]
    [InlineData("ln(sin(x))", "0", "pi/2", "-ln(2) * π / 2")]
    [InlineData("ln(cos(x))", "0", "pi/2", "-ln(2) * π / 2")]
    [InlineData("ln(sin(x))", "0", "pi", "-ln(2) * π")]
    [InlineData("exp(-x^2) + exp(-x)", "0", "inf", "(1/2) * sqrt(π) + 1")]
    public void Classic_integrals(string expr, string a, string b, string integral)
    {
        Expr value = Improper(expr, a, b);
        Assert.Equal(integral, value.Print());

        // The numeric integral doesn't take the oscillation of sin(x^2) apart.
        if (!expr.Contains("(x^2)"))
            Assert.Equal(ExprParser.Parse(expr).Integrate("x", Bound(a), Bound(b)), value.Evaluate(), 1e-9);
    }

    private static double Bound(string limit) => Infinity(limit) ?? ExprParser.Parse(limit).Evaluate();

    // Gamma(1/3), zeta(3), erf(1) and Si(1) have no closed form here.
    [Theory]
    [InlineData("exp(-x^3)", "0", "inf")]
    [InlineData("x^2/(exp(x) - 1)", "0", "inf")]
    [InlineData("exp(-x^2)", "0", "1")]
    [InlineData("sin(x)/x", "1", "inf")]
    [InlineData("exp(x^2)", "-inf", "inf")]
    [InlineData("exp(-x^2) + x^2", "0", "1")]
    public void An_integral_without_an_antiderivative_or_a_rule_throws(string expr, string a, string b) =>
        Assert.Throws<NotSupportedException>(() => Improper(expr, a, b));

    [Fact]
    public void A_classic_integral_with_a_symbol_depends_on_its_sign()
    {
        var e = Assert.Throws<NotSupportedException>(() => ExprParser.Parse("exp(-a*x^2)").IntegrateExact("x", -Inf, Inf));
        Assert.EndsWith("isn't found: it depends on the sign of a.", e.Message);
    }

    [Fact]
    public void An_antiderivative_that_oscillates_at_an_infinite_limit_isnt_taken()
    {
        var e = Assert.Throws<NotSupportedException>(() => ExprParser.Parse("cos(x)").IntegrateExact("x", 0, Inf));
        Assert.Equal("The integral of cos(x) from 0 to inf isn't found: the limit of its antiderivative sin(x) as x -> inf isn't found.", e.Message);
    }

    [Fact]
    public void Where_the_integral_depends_on_the_sign_of_another_variable_throws()
    {
        var e = Assert.Throws<NotSupportedException>(() => ExprParser.Parse("exp(-c*x)").IntegrateExact("x", 0, Inf));
        Assert.Equal("The integral of exp(-c * x) from 0 to inf isn't found: it depends on the sign of -c.", e.Message);
    }

    [Fact]
    public void Every_mix_of_infinite_and_exact_limits_has_an_overload()
    {
        Expr f = ExprParser.Parse("exp(-t)");
        Assert.Equal("1", f.IntegrateExact(0, Inf).Print());
        Assert.Equal("exp(-π)", f.IntegrateExact(new Pi(), Inf).Print());
        Assert.Equal("-exp(-π)", f.IntegrateExact(Inf, new Pi()).Print());
        Assert.Equal("-exp(-1) + 1", f.IntegrateExact(new Constant(0), new Constant(1)).Print());
        Assert.Equal("1/2", ExprParser.Parse("x").IntegrateExact("x", 0, 1).Print());
        Assert.Throws<ArgumentException>(() => f.IntegrateExact(0, double.NaN));
    }

    [Fact]
    public void An_integrand_that_isnt_real_on_a_part_of_the_interval_throws()
    {
        var e = Assert.Throws<InvalidOperationException>(() => Integral("sqrt(x)", "-1", "1"));
        Assert.Equal("The integral of sqrt(x) from -1 to 1 doesn't exist: the integrand isn't real at x = -0.5.", e.Message);
    }

    [Theory]
    [InlineData("exp(c*x)", "0", "1", "exp(c) / c - 1 / c")]
    [InlineData("c/(1 + x^2)", "0", "1", "(1/4) * π * c")]
    [InlineData("x^2", "0", "c", "(1/3) * c^3")]
    [InlineData("x^2 + c", "-1", "1", "2c + 2/3")]
    public void Other_variables_are_constants(string expr, string a, string b, string integral) =>
        Assert.Equal(integral, Integral(expr, a, b).Print());

    [Theory]
    [InlineData("1/(x + c)", "0", "1", "c")]
    [InlineData("1/x", "1", "c", "c")]
    public void Where_the_antiderivative_is_undefined_depends_on_another_variable_throws(string expr, string a, string b, string symbol)
    {
        var e = Assert.Throws<NotSupportedException>(() => Integral(expr, a, b));
        Assert.EndsWith($"is undefined or jumps between the limits depends on {symbol}.", e.Message);
    }

    [Theory]
    [InlineData("exp(i*x)", "0", "pi", "2 * i")]
    [InlineData("1/(x + i)", "-1", "1", "(-1/2) * i * π")]
    public void The_imaginary_unit(string expr, string a, string b, string integral) =>
        Assert.Equal(integral, Integral(expr, a, b).Print());

    [Fact]
    public void A_point_where_the_antiderivative_may_be_undefined_that_isnt_found_exactly_throws()
    {
        var e = Assert.Throws<NotSupportedException>(() => Integral("(1 - sin(x))/(x + cos(x))", "-1", "0"));
        Assert.Contains("may be undefined at x = -0.739085133215", e.Message);
    }

    [Fact]
    public void Without_an_antiderivative_throws() =>
        Assert.Throws<NotSupportedException>(() => Integral("sin(x)/x", "1", "2"));

    [Fact]
    public void Equal_limits_give_0() => Assert.Equal("0", Integral("1/x", "0", "0").Print());

    [Fact]
    public void The_only_variable_is_taken() => Assert.Equal("2", ExprParser.Parse("sin(t)").IntegrateExact(0, new Pi()).Print());

    [Fact]
    public void Limits_with_the_variable_or_that_arent_real_throw()
    {
        Expr f = ExprParser.Parse("x^2");
        Assert.Throws<ArgumentException>(() => f.IntegrateExact("x", 0, new Variable("x")));
        Assert.Throws<ArgumentException>(() => f.IntegrateExact("x", 0, new ImaginaryUnit()));
        Assert.Throws<ArgumentException>(() => f.IntegrateExact("x", 0, ExprParser.Parse("sqrt(-1)")));
        Assert.Throws<ArgumentNullException>(() => f.IntegrateExact("x", null!, 1));
    }
}
