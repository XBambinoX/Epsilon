using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class SeriesTests
{
    private static Series Of(string expr, string point, int degree) => ExprParser.Parse(expr).Series("x", ExprParser.Parse(point), degree);

    private static string Printed(string expr, string point, int degree) => Of(expr, point, degree).Print();

    [Fact]
    public void Taylor_series()
    {
        Assert.Equal("1 - (1/6) * x^2 + (1/120) * x^4 + O(x^5)", Printed("sin(x)/x", "0", 4));
        Assert.Equal("1 + x + (1/2) * x^2 + (1/6) * x^3 + O(x^4)", Printed("exp(x)", "0", 3));
        Assert.Equal("x + (1/3) * x^3 + (2/15) * x^5 + (17/315) * x^7 + O(x^8)", Printed("tan(x)", "0", 7));
        Assert.Equal("x - (1/3) * x^3 + (1/5) * x^5 + O(x^6)", Printed("atan(x)", "0", 5));
        Assert.Equal("x + (1/6) * x^3 + (3/40) * x^5 + O(x^6)", Printed("asin(x)", "0", 5));
        Assert.Equal("1 + (1/2) * x - (1/8) * x^2 + (1/16) * x^3 + O(x^4)", Printed("sqrt(1 + x)", "0", 3));
        Assert.Equal("1 + x - (1/8) * x^4 + O(x^5)", Printed("exp(sin(x)) - x^2/2", "0", 4));
    }

    // In powers of x - a, with the point exact.
    [Fact]
    public void At_other_points()
    {
        Assert.Equal("(x - 1) - (1/2) * (x - 1)^2 + (1/3) * (x - 1)^3 + O((x - 1)^4)", Printed("ln(x)", "1", 3));
        Assert.Equal("1 + (x - 1) + (x - 1)^2 + (1/2) * (x - 1)^3 + O((x - 1)^4)", Printed("x^x", "1", 3));
        Assert.Equal("π / 6 + (2sqrt(3) / 3) * (x - 1/2) + (2sqrt(3) / 9) * (x - 1/2)^2 + O((x - 1/2)^3)", Printed("asin(x)", "1/2", 2));
        Assert.Equal("1 - (1/2) * (x - (1/2) * π)^2 + O((x - (1/2) * π)^3)", Printed("sin(x)", "pi/2", 2));
    }

    // Other variables are constants, in the expression and in the point.
    [Fact]
    public void With_symbols()
    {
        Assert.Equal("1 + a * x + (1/2) * a^2 * x^2 + O(x^3)", Printed("exp(a*x)", "0", 2));
        Assert.Equal("sin(a) + cos(a) * (x - a) - (sin(a) / 2) * (x - a)^2 + O((x - a)^3)", Printed("sin(x)", "a", 2));
        Assert.Equal("-1 / a - (1 / a^2) * x - (1 / a^3) * x^2 + O(x^3)", Printed("1/(x - a)", "0", 2));
    }

    // A pole: the series starts at a negative power.
    [Fact]
    public void Laurent_series()
    {
        Assert.Equal("x^-1 + (1/6) * x + (7/360) * x^3 + O(x^4)", Printed("1/sin(x)", "0", 3));
        Assert.Equal("x^-1 - 1/2 + (1/12) * x - (1/720) * x^3 + O(x^4)", Printed("1/(exp(x) - 1)", "0", 3));
        Assert.Equal("-(x - (1/2) * π)^-1 + (1/3) * (x - (1/2) * π) + O((x - (1/2) * π)^2)", Printed("tan(x)", "pi/2", 1));
        Assert.Equal("x^-2 - x^-1 + 1 + O(x)", Printed("x^10/(x^12 + x^13)", "0", 0));
        Assert.False(Of("1/sin(x)", "0", 3).RightSided);
    }

    // A root of a part that vanishes at the point: powers that are fractions, for x > a.
    [Fact]
    public void Puiseux_series()
    {
        var root = Of("sqrt(x + x^2)", "0", 2);
        Assert.Equal("x^(1/2) + (1/2) * x^(3/2) + O(x^(5/2))", root.Print());
        Assert.Equal(new Rational(5, 2), root.Order);
        Assert.True(root.RightSided);

        Assert.Equal("(sqrt(2) / 2) * x - (sqrt(2) / 48) * x^3 + O(x^4)", Printed("sqrt(1 - cos(x))", "0", 3));
        Assert.Equal("x^(-1/2) + O(x^2)", Printed("1/sqrt(x)", "0", 1));
    }

    // An inverse function from the integral of its derivative, which has a root at a branch point.
    [Fact]
    public void Inverse_functions_at_branch_points()
    {
        var acosh = Of("acosh(x)", "1", 2);
        Assert.Equal("sqrt(2) * (x - 1)^(1/2) - (sqrt(2) / 12) * (x - 1)^(3/2) + O((x - 1)^(5/2))", acosh.Print());
        Assert.True(acosh.RightSided);
        Assert.Equal("-π / 2 + sqrt(2) * (x + 1)^(1/2) + (sqrt(2) / 12) * (x + 1)^(3/2) + O((x + 1)^(5/2))", Printed("asin(x)", "-1", 2));
        Assert.Equal("π / 4 + (1/2) * (x - 1) - (1/4) * (x - 1)^2 + (1/12) * (x - 1)^3 + O((x - 1)^4)", Printed("atan(x)", "1", 3));
        Assert.Equal("x^2 - (1/3) * x^6 + O(x^7)", Printed("atan(x^2)", "0", 6));
    }

    // sqrt(x^2) is x for x > 0 only; the real cube root of x^3 is x on both sides.
    [Fact]
    public void Which_side_a_series_holds_on()
    {
        Assert.Equal("x + O(x^3)", Printed("sqrt(x^2)", "0", 2));
        Assert.True(Of("sqrt(x^2)", "0", 2).RightSided);
        Assert.True(Of("abs(x)", "0", 2).RightSided);
        Assert.False(Of("abs(x^2 - x^3)", "0", 3).RightSided);
        Assert.False(Of("abs(x - 2)", "0", 2).RightSided);

        var cube = Of("nthroot(x^3 + x^4, 3)", "0", 2);
        Assert.Equal("x + (1/3) * x^2 + O(x^3)", cube.Print());
        Assert.False(cube.RightSided);
    }

    // The difference from the expression shrinks at least as (x - a)^Order: halving x - a divides
    // it by 2^Order or more, unless it is down to rounding.
    [Theory]
    [InlineData("exp(sin(x))", "0", 5)]
    [InlineData("ln(cos(x))", "0", 6)]
    [InlineData("x/sin(x)", "0", 4)]
    [InlineData("1/(exp(x) - 1)", "0", 3)]
    [InlineData("x^x", "1", 3)]
    [InlineData("asin(x)", "1/2", 3)]
    [InlineData("(1 + x)^(1/3)", "0", 4)]
    [InlineData("atanh(x) + acos(x)", "0", 5)]
    [InlineData("tan(x)", "pi/2", 2)]
    [InlineData("sqrt(1 - cos(x))", "0", 4)]
    [InlineData("acosh(x)", "1", 3)]
    [InlineData("atan(x)", "1", 8)]
    [InlineData("sqrt(x + x^2)", "0", 2)]
    [InlineData("cosh(x)/(1 + x^2)", "2", 4)]
    [InlineData("2^x*sec(x)", "0", 4)]
    public void The_remainder_is_of_the_order(string expr, string point, int degree)
    {
        var f = ExprParser.Parse(expr);
        var series = Of(expr, point, degree);
        double a = ExprParser.Parse(point).Evaluate();
        double Error(double h) => Math.Abs(f.Evaluate(("x", a + h)) - series.ToExpr().Evaluate(("x", a + h)));

        double order = Math.Log2(Error(0.1) / Error(0.05));
        Assert.True(Error(0.1) < 1e-12 || order > series.Order.ToDouble() - 0.4, $"{order} for the order {series.Order}");
    }

    [Fact]
    public void Coefficients()
    {
        var series = Of("cos(x)", "0", 4);
        Assert.Equal("-1/2", series.Coefficient(2).Print());
        Assert.Equal("0", series.Coefficient(3).Print());
        Assert.Equal("0", series.Coefficient(new Rational(1, 2)).Print());
        Assert.Equal(new Rational(5), series.Order);
        Assert.Equal([0, 2, 4], series.Terms.Select(term => (int)term.Exponent.Numerator));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Coefficient(5));
    }

    [Fact]
    public void As_an_expression()
    {
        var polynomial = Of("sin(x)/x", "0", 4).ToExpr();
        Assert.Equal(1 - 0.25 / 6 + 0.0625 / 120, polynomial.Evaluate(("x", 0.5)), 1e-15);
        Assert.Equal("0", Of("floor(x)", "1/2", 2).ToExpr().Print());
        Assert.Equal("O((x - 1/2)^3)", Printed("floor(x)", "1/2", 2));
    }

    [Fact]
    public void Without_a_name_the_only_variable()
    {
        Assert.Equal("t - (1/6) * t^3 + O(t^4)", ExprParser.Parse("sin(t)").Series(0, 3).Print());
        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("x*y").Series(0, 3));
    }

    // A logarithm of x, an essential singularity, a branch point with nothing real on the right,
    // a pole of atanh, atan of a pole, a root that isn't real, a jump, a denominator that is 0,
    // a vanishing base raised to an irrational power, and a sign that depends on a symbol.
    [Theory]
    [InlineData("ln(x)", "0")]
    [InlineData("x*ln(x)", "0")]
    [InlineData("exp(1/x)", "0")]
    [InlineData("sin(1/x)", "0")]
    [InlineData("asin(x)", "1")]
    [InlineData("atanh(x)", "1")]
    [InlineData("atan(1/x)", "0")]
    [InlineData("sqrt(x - 2)", "0")]
    [InlineData("floor(x)", "1")]
    [InlineData("1/(sin(x)^2 + cos(x)^2 - 1)", "0")]
    [InlineData("x^pi", "0")]
    [InlineData("abs(a*x)", "0")]
    [InlineData("floor(x)", "a")]
    [InlineData("x^a", "0")]
    public void What_has_no_such_series_throws(string expr, string point) =>
        Assert.Throws<NotSupportedException>(() => Of(expr, point, 3));

    [Fact]
    public void A_point_with_the_variable_throws() =>
        Assert.Throws<ArgumentException>(() => ExprParser.Parse("sin(x)").Series("x", ExprParser.Parse("2*x"), 3));

    [Fact]
    public void Null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Series("x", 0, 3));
        Assert.Throws<ArgumentNullException>(() => ExprParser.Parse("x").Series(null!, 0, 3));
        Assert.Throws<ArgumentNullException>(() => ExprParser.Parse("x").Series("x", null!, 3));
    }
}
