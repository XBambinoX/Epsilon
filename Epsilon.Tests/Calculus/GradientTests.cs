using Epsilon.Calculus;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.Calculus;

public class GradientTests
{
    private static readonly Expr F = ExprParser.Parse("x^2*y + sin(y)");

    [Fact]
    public void Partial_derivatives_in_the_order_of_the_variables()
    {
        Assert.Equal("[2x * y, x^2 + cos(y)]", F.Gradient(["x", "y"]).Print());
        Assert.Equal("[x^2 + cos(y), 2x * y]", F.Gradient(["y", "x"]).Print());
        Assert.Equal("[2x * y]", F.Gradient(["x"]).Print());
    }

    [Fact]
    public void Variables_as_arguments_or_as_a_list()
    {
        var f = ExprParser.Parse("omega*sigma^2", "omega", "sigma");
        var expected = f.Gradient(["omega", "sigma"]);

        Assert.Equal("[sigma^2, 2omega * sigma]", expected.Print());
        Assert.Equal(expected, f.Gradient("omega", "sigma"));
        Assert.Equal(expected, f.Gradient(new List<string> { "omega", "sigma" }));
        Assert.Equal(expected, f.Gradient(new[] { "omega", "sigma" }));
        Assert.Equal("[sigma^2]", f.Gradient("omega").Print());
    }

    [Fact]
    public void Entries_are_the_simplified_partial_derivatives()
    {
        var f = ExprParser.Parse("exp(x*y)/(1 + z^2) + ln(x)*z");
        var gradient = f.Gradient(["x", "y", "z"]);

        Assert.Equal(f.Differentiate("x"), gradient[0]);
        Assert.Equal(f.Differentiate("y"), gradient[1]);
        Assert.Equal(f.Differentiate("z"), gradient[2]);
    }

    [Fact]
    public void A_variable_the_expression_does_not_have_gives_zero() =>
        Assert.Equal("[2x * y, 0]", F.Gradient(["x", "z"]).Print());

    [Fact]
    public void Without_a_list_every_variable_in_ordinal_order() =>
        Assert.Equal("[1, b, a]", ExprParser.Parse("b*a + B").Gradient().Print());

    [Fact]
    public void Empty_when_there_is_nothing_to_differentiate_by()
    {
        Assert.Equal(0, ExprParser.Parse("2*pi").Gradient().Length);
        Assert.Equal(0, F.Gradient([]).Length);
    }

    [Fact]
    public void Numbers_come_from_Evaluate_and_Compile_of_the_vector()
    {
        var gradient = F.Gradient(["x", "y"]);
        var at = gradient.Evaluate(("x", 1), ("y", 2));

        Assert.Equal([4, 1 + Math.Cos(2)], at);
        Assert.Equal(at, gradient.Compile("x", "y")(1, 2));
    }

    [Fact]
    public void Compiled_gradient_drives_gradient_descent()
    {
        var gradient = ExprParser.Parse("(x - 1)^2 + 2*(y + 2)^2").Gradient(["x", "y"]).Compile("x", "y");

        Vector<double> p = [0.0, 0.0];
        for (int step = 0; step < 200; step++)
            p -= 0.1 * gradient(p[0], p[1]);

        Assert.True(p.IsApproximately([1.0, -2.0], 1e-12));
    }

    [Fact]
    public void Directional_derivative_along_an_axis_is_the_partial_derivative()
    {
        Assert.Equal(F.Differentiate("x"), F.DirectionalDerivative([1, 0], "x", "y"));
        Assert.Equal(F.Differentiate("y"), F.DirectionalDerivative([0, 1], "x", "y"));
    }

    [Fact]
    public void Directional_derivative_is_the_gradient_dotted_with_the_direction()
    {
        Assert.Equal("2x * y + x^2", ExprParser.Parse("x^2*y").DirectionalDerivative([1, 1], "x", "y").Print());

        Vector<Expr> direction = [ExprParser.Parse("cos(t)"), ExprParser.Parse("sin(t)")];
        Assert.Equal(F.Gradient("x", "y").Dot(direction), F.DirectionalDerivative(direction, "x", "y"));
    }

    [Fact]
    public void Directional_derivative_is_not_normalized()
    {
        double unit = F.DirectionalDerivative([0.6, 0.8], "x", "y").Evaluate(("x", 1), ("y", 2));
        double longer = F.DirectionalDerivative([3, 4], "x", "y").Evaluate(("x", 1), ("y", 2));

        Assert.Equal(5 * unit, longer, 1e-12);
    }

    [Fact]
    public void Directional_derivative_checks_its_arguments()
    {
        Assert.Throws<ArgumentException>(() => F.DirectionalDerivative([1, 0, 0], "x", "y"));
        Assert.Throws<ArgumentException>(() => F.DirectionalDerivative([1, 0], "x", "x"));
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).DirectionalDerivative([1], "x"));
        Assert.Throws<ArgumentNullException>(() => F.DirectionalDerivative(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => F.DirectionalDerivative([1], null!));
    }

    [Fact]
    public void A_name_listed_twice_throws() =>
        Assert.Throws<ArgumentException>(() => F.Gradient("x", "y", "x"));

    [Fact]
    public void Null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Gradient(["x"]));
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Gradient());
        Assert.Throws<ArgumentNullException>(() => F.Gradient(null!));
        Assert.Throws<ArgumentNullException>(() => F.Gradient(["x", null!]));
    }

    [Fact]
    public void Unsupported_derivatives_throw() =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("floor(x)*y").Gradient());
}
