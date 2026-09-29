using Xunit;
using Epsilon.Core;

namespace Epsilon.Tests.Core;

// Every example in docs/limitations.md, with the result it shows. When a limitation is lifted,
// one of these fails: update or remove the section in the docs together with the test.
public class LimitationsDocTests
{
    [Fact]
    public void No_expansion()
    {
        Assert.Equal("-x^2 + (x + 1)^2", ExprParser.Parse("(x + 1)^2 - x^2").Simplify().Print());
        Assert.Equal("(x + 1) * (x - 1)", ExprParser.Parse("(x + 1)*(x - 1)").Simplify().Print());
        Assert.False(ExprParser.Parse("(x - 2)^4").TryFactorReal("x").Success);
    }

    [Fact]
    public void Factoring_is_limited()
    {
        Assert.False(ExprParser.Parse("x^4 + 1").TryFactorReal("x").Success);
        Assert.False(ExprParser.Parse("x^3 + x + 1").TryFactorReal("x").Success);
        Assert.False(ExprParser.Parse("x^2 - y^2").TryFactorReal("x").Success);
    }

    [Fact]
    public void No_exact_function_values()
    {
        Assert.Equal("sin(π / 6)", ExprParser.Parse("sin(pi/6)").Simplify().Print());
        Assert.Equal("asin(1/2)", ExprParser.Parse("asin(1/2)").Simplify().Print());
        Assert.Equal("sqrt(8)", ExprParser.Parse("sqrt(8)").Simplify().Print());
    }

    [Fact]
    public void Few_identities()
    {
        Assert.Equal("1", ExprParser.Parse("sin(x)^2 + cos(x)^2").Simplify().Print());
        Assert.Equal("tan(x)", ExprParser.Parse("sin(x)/cos(x)").Simplify().Print());
        Assert.Equal("x", ExprParser.Parse("ln(exp(x))").Simplify().Print());

        Assert.Equal("2cos(x) * sin(x)", ExprParser.Parse("2sin(x)cos(x)").Simplify().Print());
        Assert.Equal("ln(x * y) - ln(x)", ExprParser.Parse("ln(x*y) - ln(x)").Simplify().Print());
    }

    [Fact]
    public void Equality_is_structural()
    {
        Assert.False(ExprParser.Parse("x*x").Equals(ExprParser.Parse("x^2")));
        Assert.True(ExprParser.Parse("x*x").Simplify().Equals(ExprParser.Parse("x^2")));
    }

    [Theory]
    [InlineData("floor(x)")]
    [InlineData("ceiling(x)")]
    [InlineData("round(x)")]
    [InlineData("sign(x)")]
    [InlineData("min(x, 1)")]
    [InlineData("max(x, 1)")]
    [InlineData("nthroot(2, x)")]
    public void Unsupported_derivatives(string input)
    {
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(input).Differentiate("x"));
    }

    [Fact]
    public void Abs_derivative()
    {
        Assert.Equal("x / abs(x)", ExprParser.Parse("abs(x)").Differentiate("x").Print());
    }

    [Theory]
    [InlineData("floor(x)")]
    [InlineData("ceiling(x)")]
    [InlineData("round(x)")]
    [InlineData("sign(x)")]
    [InlineData("min(x, 1)")]
    [InlineData("max(x, 1)")]
    public void Unsupported_complex_functions(string input)
    {
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(input).EvaluateComplex(new ComplexNumber(1.5, 1)));
    }

    [Fact]
    public void Division_by_zero_real_vs_complex()
    {
        Assert.Equal(double.PositiveInfinity, ExprParser.Parse("1/x").Evaluate(0));
        ComplexNumber z = ExprParser.Parse("1/x").EvaluateComplex(0);
        Assert.True(double.IsNaN(z.Real) || double.IsNaN(z.Imaginary));
    }

    [Fact]
    public void Real_roots_grid()
    {
        Assert.Equal(11, ExprParser.Parse("sin(1/x)").FindRealRoots(0.01, 1).Count);
        Assert.Equal(31, ExprParser.Parse("sin(1/x)").FindRealRoots(0.01, 1, 5000).Count);
        Assert.Equal([1.0], ExprParser.Parse("(x - 1)*(x - 1.0000001)").FindRealRoots(-10, 10));
    }

    [Fact]
    public void Number_as_right_hand_side_binds_to_the_range_overload()
    {
        Expr s = ExprParser.Parse("sin(x)");

        Assert.Equal([0.0], s.FindRealRoots(-1, 2, 3));
        Assert.Empty(s.FindRealRoots((Expr)(-1), 2, 3));
    }

    [Fact]
    public void Complex_roots_repeated()
    {
        Assert.Equal(98, ExprParser.Parse("x^2 - 2x + 1").FindComplexRoots(-2, 2, -2, 2).Count);
        Assert.Equal(3, ExprParser.Parse("x^3 - 1").FindComplexRoots(-2, 2, -2, 2).Count);
    }
}
