using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class LimitTests
{
    private const double Inf = double.PositiveInfinity;

    private static string At(string expr, double point, LimitDirection direction = LimitDirection.Both) =>
        ExprParser.Parse(expr).Limit("x", point, direction).Print();

    private static string At(string expr, string point, LimitDirection direction = LimitDirection.Both) =>
        ExprParser.Parse(expr).Limit("x", ExprParser.Parse(point), direction).Print();

    [Theory]
    [InlineData("sin(x)/x", "0", "1")]
    [InlineData("(1 - cos(x))/x^2", "0", "1/2")]
    [InlineData("(tan(x) - x)/x^3", "0", "1/3")]
    [InlineData("(x - sin(x))/(x - tan(x))", "0", "-1/2")]
    [InlineData("1/x - 1/sin(x)", "0", "0")]
    [InlineData("ln(1 + x)/x", "0", "1")]
    [InlineData("(sqrt(1 + x) - 1)/x", "0", "1/2")]
    [InlineData("(x^3 - 8)/(x - 2)", "2", "12")]
    [InlineData("x^2", "3", "9")]
    [InlineData("sin(x)/(x - pi)", "pi", "-1")]
    [InlineData("(sin(x)/x)^(1/x^2)", "0", "exp(-1/6)")]
    [InlineData("cos(x)^(1/x^2)", "0", "exp(-1/2)")]
    [InlineData("1/(x - 2)^2", "2", "inf")]
    public void At_a_point(string expr, string point, string limit) => Assert.Equal(limit, At(expr, point));

    // ln(x) and exp(1/x), which have no series at 0, by the growth of what they are made of.
    [Theory]
    [InlineData("x*ln(x)", "0")]
    [InlineData("sqrt(x)*ln(x)", "0")]
    [InlineData("x^x", "1")]
    [InlineData("x^x^x", "0")]
    [InlineData("x^(1/x)", "0")]
    [InlineData("ln(x)", "-inf")]
    [InlineData("ln(x)^2/2", "inf")]
    [InlineData("ln(x)/ln(sin(x))", "1")]
    [InlineData("x^2*exp(-1/x^2)", "0")]
    [InlineData("exp(-1/x)", "0")]
    public void Logarithms_and_exponentials_at_0(string expr, string limit) => Assert.Equal(limit, At(expr, 0, LimitDirection.Right));

    [Theory]
    [InlineData("(2x^2 + 1)/(3x^2 - x)", "2/3")]
    [InlineData("(x^2 + 1)/(x + 1) - x", "-1")]
    [InlineData("sqrt(x^2 + x) - x", "1/2")]
    [InlineData("nthroot(x^3 + x^2, 3) - x", "1/3")]
    [InlineData("(1 + 1/x)^x", "e")]
    [InlineData("((x + 1)/x)^x", "e")]
    [InlineData("(1 + 1/x)^(x^2)*exp(-x)", "exp(-1/2)")]
    [InlineData("x*exp(-x)", "0")]
    [InlineData("x^7/exp(x)", "0")]
    [InlineData("exp(x)/x^100", "inf")]
    [InlineData("x - exp(x)", "-inf")]
    [InlineData("ln(x)/x", "0")]
    [InlineData("ln(ln(x))/ln(x)", "0")]
    [InlineData("x^(1/x)", "1")]
    [InlineData("x^ln(x)/exp(x)", "0")]
    [InlineData("ln(x)^ln(x)/x", "inf")]
    [InlineData("ln(1 + exp(x))/x", "1")]
    [InlineData("exp(x)/(exp(x) + 1)", "1")]
    [InlineData("atan(x)", "(1/2) * π")]
    [InlineData("x*(atan(x) - pi/2)", "-1")]
    [InlineData("atan(x)/2 + x/(2*(x^2 + 1))", "(1/4) * π")]
    [InlineData("-exp(-x)*(x^2 + 2*x + 2)", "0")]
    [InlineData("(exp(x) + x)^(1/x)", "e")]
    [InlineData("exp(2*ln(x))/x^2", "1")]
    public void At_infinity(string expr, string limit) => Assert.Equal(limit, At(expr, Inf));

    [Theory]
    [InlineData("x*exp(x)", "0")]
    [InlineData("x^2", "inf")]
    [InlineData("x^3", "-inf")]
    [InlineData("nthroot(x, 3)", "-inf")]
    [InlineData("atan(x)", "(-1/2) * π")]
    [InlineData("ln(1 + exp(x))/x", "0")]
    [InlineData("asinh(x)", "-inf")]
    public void At_minus_infinity(string expr, string limit) => Assert.Equal(limit, At(expr, double.NegativeInfinity));

    // From D. Gruntz, On Computing Limits in a Symbolic Manipulation System, 1996: exponentials
    // within exponentials, which cancel to the leading order.
    [Theory]
    [InlineData("exp(x)*(exp(1/x - exp(-x)) - exp(1/x))", "-1")]
    [InlineData("exp(x)*(exp(1/x + exp(-x) + exp(-x^2)) - exp(1/x - exp(-exp(x))))", "1")]
    [InlineData("exp(exp(x - exp(-x))/(1 - 1/x)) - exp(exp(x))", "inf")]
    [InlineData("(exp(4*x*exp(-x)/(1/exp(x) + 1/exp(2*x^2/(x + 1)))) - exp(x))/exp(x)^4", "1")]
    [InlineData("exp(x*exp(-x)/(exp(-x) + exp(-2*x^2/(x + 1))))/exp(x)", "1")]
    [InlineData("ln(ln(x*exp(x*exp(x)) + 1)) - exp(exp(ln(ln(x)) + 1/x))", "0")]
    [InlineData("x*ln(x)*ln(x*exp(x) - x^2)^2/ln(ln(x^2 + 2*exp(exp(3*x^3*ln(x)))))", "1/3")]
    public void Nested_exponentials(string expr, string limit) => Assert.Equal(limit, At(expr, Inf));

    // Rates of growth at an irrational ratio: in one exponential, factored out of a logarithm,
    // or one of them only inside the other.
    [Theory]
    [InlineData("2^x/3^x", "0")]
    [InlineData("(2^x + 3^x)^(1/x)", "3")]
    [InlineData("(3^x + 5^x)^(1/x)", "5")]
    [InlineData("x^pi", "inf")]
    [InlineData("x^(1 - pi)/(1 - pi)", "0")]
    [InlineData("x^pi*exp(-x)", "0")]
    public void Irrational_rates(string expr, string limit) => Assert.Equal(limit, At(expr, Inf));

    [Theory]
    [InlineData("tanh(x)", "1")]
    [InlineData("cosh(x)/exp(x)", "1/2")]
    [InlineData("asinh(x) - ln(x)", "ln(2)")]
    [InlineData("acosh(x) - ln(2x)", "0")]
    [InlineData("min(x, 1)", "1")]
    [InlineData("abs(x - 1)", "inf")]
    public void Hyperbolic_functions_and_others_at_infinity(string expr, string limit) => Assert.Equal(limit, At(expr, Inf));

    [Fact]
    public void From_one_side()
    {
        Assert.Equal("inf", At("1/x", 0, LimitDirection.Right));
        Assert.Equal("-inf", At("1/x", 0, LimitDirection.Left));
        Assert.Equal("inf", At("exp(-1/x)", 0, LimitDirection.Left));
        Assert.Equal("1", At("1/(1 + exp(1/x))", 0, LimitDirection.Left));
        Assert.Equal("1", At("abs(x)/x", 0, LimitDirection.Right));
        Assert.Equal("0", At("floor(x)", 1, LimitDirection.Left));
        Assert.Equal("1", At("floor(x)", 1, LimitDirection.Right));
        Assert.Equal("2", At("ceiling(x)", 2, LimitDirection.Left));
        Assert.Equal("0", At("round(x)", 0.5, LimitDirection.Left));
        Assert.Equal("1", At("round(x)", 0.5, LimitDirection.Right));
        Assert.Equal("inf", At("atanh(x)", 1, LimitDirection.Left));
        Assert.Equal("sqrt(2)", At("acos(x)/sqrt(1 - x)", 1, LimitDirection.Left));
        Assert.Equal("1", At("x*floor(1/x)", 0, LimitDirection.Right));
    }

    [Theory]
    [InlineData("1/x", "0", "-inf from the left, inf from the right")]
    [InlineData("sign(x)", "0", "-1 from the left, 1 from the right")]
    [InlineData("exp(1/x)", "0", "0 from the left, inf from the right")]
    [InlineData("floor(x)", "1", "0 from the left, 1 from the right")]
    [InlineData("tan(x)", "pi/2", "inf from the left, -inf from the right")]
    public void Sides_that_differ(string expr, string point, string sides)
    {
        var e = Assert.Throws<InvalidOperationException>(() => At(expr, point));
        Assert.EndsWith($"doesn't exist: {sides}.", e.Message);
    }

    // At an end of where the expression is real, the limit from both sides is that from the side
    // it is real on; from the other side there is none.
    [Fact]
    public void At_the_end_of_where_it_is_real()
    {
        Assert.Equal("0", At("sqrt(x)", 0));
        Assert.Equal("0", At("x*ln(x)", 0));
        Assert.Equal("-inf", At("ln(x)", 0));
        Assert.Equal("1", At("x^x", 0));
        Assert.Equal("0", At("sqrt(1 - x^2)", 1));
        Assert.Equal("The limit of sqrt(x) at x = 0 from the left doesn't exist: sqrt(x) isn't real there.",
            Assert.Throws<InvalidOperationException>(() => At("sqrt(x)", 0, LimitDirection.Left)).Message);
        Assert.Equal("The limit of ln(x) as x -> -inf doesn't exist: ln(x) isn't real there.",
            Assert.Throws<InvalidOperationException>(() => At("ln(x)", double.NegativeInfinity)).Message);
        Assert.Throws<InvalidOperationException>(() => At("sqrt(-1 - x^2)", 0));
    }

    // A bounded part that oscillates, where what it is next to settles the limit.
    [Theory]
    [InlineData("sin(x)/x", "0")]
    [InlineData("exp(-x)*(sin(x) + cos(x))", "0")]
    [InlineData("tanh(x)*sin(x)/x", "0")]
    [InlineData("sin(x)*exp(-x) + 1", "1")]
    [InlineData("x + sin(x)", "inf")]
    [InlineData("x/(x + sin(x))", "1")]
    [InlineData("sin(x)/(x + sin(x))", "0")]
    [InlineData("(2*x + sin(x))/(x + cos(x))", "2")]
    [InlineData("floor(x)/x", "1")]
    [InlineData("exp(-x)*floor(x)", "0")]
    [InlineData("floor(x)", "inf")]
    [InlineData("(1/5)*(exp(-x)*sin(2*x) - cos(2*x)*exp(-x)/2)", "0")]
    [InlineData("-(x + sin(x))/3", "-inf")]
    [InlineData("(x + 2*atan(-sin(x)/(cos(x) + sqrt(3) + 2)))/sqrt(3)", "inf")]
    public void Oscillations_that_are_squeezed(string expr, string limit) => Assert.Equal(limit, At(expr, Inf));

    [Fact]
    public void Oscillations_that_arent()
    {
        Assert.Equal("The limit of sin(x) as x -> inf isn't found: a part of it oscillates there.",
            Assert.Throws<NotSupportedException>(() => At("sin(x)", Inf)).Message);
        Assert.Equal("The limit of sin(1 / x) at x = 0 isn't found: a part of it oscillates there.",
            Assert.Throws<NotSupportedException>(() => At("sin(1/x)", "0")).Message);
        Assert.Equal("0", At("x*sin(1/x)", "0"));
        Assert.Throws<NotSupportedException>(() => At("x*sin(x)", Inf));
        Assert.Throws<NotSupportedException>(() => At("floor(x) - x", Inf));
    }

    // Other variables are constants, taken as other than 0; a sign the limit depends on isn't
    // known.
    [Fact]
    public void With_symbols()
    {
        Assert.Equal("a", At("sin(a*x)/x", "0"));
        Assert.Equal("a", At("(exp(a*x) - 1)/x", "0"));
        Assert.Equal("exp(a)", At("(1 + a/x)^x", Inf));
        Assert.Equal("(1/2) * a", At("sqrt(x^2 + a*x) - x", Inf));
        Assert.Equal("2a", At("(x^2 - a^2)/(x - a)", "a"));
        Assert.Equal("inf", At("1/(x - a)", "a", LimitDirection.Right));
        Assert.Equal("0", At("x^n*exp(-x)", Inf));
        Assert.Equal("The limit of a * x as x -> inf depends on the sign of a.",
            Assert.Throws<NotSupportedException>(() => At("a*x", Inf)).Message);
    }

    [Fact]
    public void The_result()
    {
        Limit limit = ExprParser.Parse("(1 + 1/x)^x").Limit("x", Inf);
        Assert.Equal(LimitKind.Finite, limit.Kind);
        Assert.Equal(Math.E, limit.Value.Evaluate());
        Assert.Equal("e", limit.ToString());

        Limit infinite = ExprParser.Parse("1/x^2").Limit("x", 0);
        Assert.Equal(LimitKind.PositiveInfinity, infinite.Kind);
        Assert.Equal("The limit is inf, not a value.", Assert.Throws<InvalidOperationException>(() => infinite.Value).Message);
        Assert.Equal(LimitKind.NegativeInfinity, ExprParser.Parse("ln(x)").Limit("x", 0).Kind);
    }

    [Fact]
    public void By_the_only_variable()
    {
        Assert.Equal("1", ExprParser.Parse("sin(t)/t").Limit(0).Print());
        Assert.Equal("0", ExprParser.Parse("exp(-t)").Limit(Inf).Print());
        Assert.Equal("inf", ExprParser.Parse("1/t").Limit(ExprParser.Parse("0"), LimitDirection.Right).Print());
        Assert.Equal("5", ExprParser.Parse("5").Limit(0).Print());
        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("x*y").Limit(0));
    }

    [Fact]
    public void Arguments_checked()
    {
        Expr f = ExprParser.Parse("1/x");
        Assert.Throws<ArgumentException>(() => f.Limit("x", double.NaN));
        Assert.Equal("x can't tend to inf from the right. (Parameter 'direction')",
            Assert.Throws<ArgumentException>(() => f.Limit("x", Inf, LimitDirection.Right)).Message);
        Assert.Throws<ArgumentException>(() => f.Limit("x", double.NegativeInfinity, LimitDirection.Left));
        Assert.Equal("0", f.Limit("x", Inf, LimitDirection.Left).Print());
        Assert.Throws<ArgumentException>(() => f.Limit("x", ExprParser.Parse("x + 1")));
        Assert.Throws<ArgumentNullException>(() => f.Limit(null!, 0));
        Assert.Throws<ArgumentNullException>(() => f.Limit("x", (Expr)null!));
    }

    // A double is the decimal it prints as, as for Series: tan(x) at Math.PI / 2 is finite.
    [Fact]
    public void A_double_point_is_its_decimal() =>
        Assert.Equal("tan(15707963267949/10000000000000)", At("tan(x)", Math.PI / 2));
}
