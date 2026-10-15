using Epsilon.Calculus;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.Calculus;

public class HessianTests
{
    private static readonly Expr F = ExprParser.Parse("x^2*y + sin(y)");
    private static readonly Expr Cubic = ExprParser.Parse("x^3 - 3*x + y^2");

    [Fact]
    public void Second_partial_derivatives_in_the_order_of_the_variables()
    {
        Assert.Equal("[[2y, 2x], [2x, -sin(y)]]", F.Hessian("x", "y").Print());
        Assert.Equal("[[-sin(y), 2x], [2x, 2y]]", F.Hessian("y", "x").Print());
        Assert.Equal("[[2y]]", F.Hessian("x").Print());
    }

    [Fact]
    public void Variables_as_arguments_or_as_a_list()
    {
        var expected = F.Hessian("x", "y");

        Assert.Equal(expected, F.Hessian(["x", "y"]));
        Assert.Equal(expected, F.Hessian(new List<string> { "x", "y" }));
    }

    [Fact]
    public void Entries_are_the_simplified_second_derivatives()
    {
        var f = ExprParser.Parse("exp(x*y)/(1 + z^2) + ln(x)*z");
        string[] names = ["x", "y", "z"];
        var hessian = f.Hessian(names);

        for (int i = 0; i < names.Length; i++)
            for (int j = 0; j <= i; j++)
                Assert.Equal(f.Differentiate(names[i]).Differentiate(names[j]), hessian[i, j]);
    }

    [Fact]
    public void Symmetric_and_equal_in_value_to_the_Jacobian_of_the_gradient()
    {
        foreach (string text in new[] { "x^y", "sin(x*y)*cos(x + y)", "atan(y/x)" })
        {
            var f = ExprParser.Parse(text);
            var hessian = f.Hessian("x", "y");
            var jacobian = f.Gradient("x", "y").Jacobian(["x", "y"]);

            Assert.Equal(hessian, hessian.Transpose());
            Assert.True(hessian.Evaluate(("x", 1.3), ("y", 0.7)).IsApproximately(jacobian.Evaluate(("x", 1.3), ("y", 0.7)), 1e-12));
        }
    }

    [Fact]
    public void A_variable_the_expression_does_not_have_gives_zeros() =>
        Assert.Equal("[[2y, 0], [0, 0]]", F.Hessian("x", "z").Print());

    [Fact]
    public void Without_a_list_every_variable_in_ordinal_order() =>
        Assert.Equal("[[0, 0, 0], [0, 0, 1], [0, 1, 0]]", ExprParser.Parse("b*a + B").Hessian().Print());

    [Fact]
    public void Empty_when_there_is_nothing_to_differentiate_by()
    {
        Assert.Equal(0, ExprParser.Parse("2*pi").Hessian().Rows);
        Assert.Equal(0, F.Hessian([]).Rows);
    }

    [Fact]
    public void Numbers_come_from_Evaluate_and_Compile_of_the_matrix()
    {
        var hessian = F.Hessian("x", "y");
        var at = hessian.Evaluate(("x", 1), ("y", 2));

        Assert.Equal(new Matrix<double>(new double[,] { { 4, 2 }, { 2, -Math.Sin(2) } }), at);
        Assert.Equal(at, hessian.Compile("x", "y")(1, 2));
    }

    [Fact]
    public void Compiled_Hessian_drives_Newtons_method()
    {
        var f = ExprParser.Parse("(1 - x)^2 + 100*(y - x^2)^2");
        var gradient = f.Gradient("x", "y").Compile("x", "y");
        var hessian = f.Hessian("x", "y").Compile("x", "y");

        Vector<double> p = [-1.2, 1.0];
        for (int step = 0; step < 20; step++)
            p -= hessian(p[0], p[1]).Solve(gradient(p[0], p[1]));

        Assert.True(p.IsApproximately([1.0, 1.0], 1e-12));
        Assert.Equal(CriticalPointKind.LocalMinimum, f.ClassifyCriticalPoint(("x", p[0]), ("y", p[1])));
    }

    [Fact]
    public void Laplacian_small_examples()
    {
        Assert.Equal("8y", ExprParser.Parse("x^2*y + y^3").Laplacian("x", "y").Print());
        Assert.Equal("6", ExprParser.Parse("x^2 + y^2 + z^2").Laplacian("x", "y", "z").Print());
        Assert.Equal("2y", F.Laplacian("x").Print());
    }

    [Fact]
    public void Laplacian_of_a_harmonic_function_is_zero()
    {
        Assert.Equal("0", ExprParser.Parse("exp(x)*sin(y)").Laplacian("x", "y").Print());
        Assert.Equal("0", ExprParser.Parse("x^3 - 3*x*y^2").Laplacian("x", "y").Print());
    }

    [Fact]
    public void Laplacian_is_zero_in_value_where_simplify_cannot_show_it()
    {
        var laplacian = ExprParser.Parse("ln(x^2 + y^2)").Laplacian("x", "y");

        foreach (var (x, y) in new[] { (0.3, -1.2), (1.0, 2.0), (-2.0, 0.1) })
            Assert.Equal(0, laplacian.Evaluate(("x", x), ("y", y)), 1e-12);
    }

    [Fact]
    public void Laplacian_is_the_divergence_of_the_gradient_and_the_trace_of_the_Hessian()
    {
        var f = ExprParser.Parse("exp(x*y)/(1 + z^2) + ln(x)*z");
        var laplacian = f.Laplacian("x", "y", "z");

        Assert.Equal(f.Gradient("x", "y", "z").Divergence("x", "y", "z"), laplacian);
        Assert.Equal(f.Hessian("x", "y", "z").Trace(), laplacian);
    }

    [Fact]
    public void Laplacian_without_a_list_uses_every_variable()
    {
        Assert.Equal("8y", ExprParser.Parse("x^2*y + y^3").Laplacian().Print());
        Assert.Equal("0", ExprParser.Parse("2*pi").Laplacian().Print());
    }

    [Fact]
    public void Classify_by_the_signs_of_the_eigenvalues()
    {
        Assert.Equal(CriticalPointKind.LocalMinimum, ExprParser.Parse("x^2 + y^2").ClassifyCriticalPoint(("x", 0), ("y", 0)));
        Assert.Equal(CriticalPointKind.LocalMaximum, ExprParser.Parse("-x^2 - 3*y^2 + x*y").ClassifyCriticalPoint(("x", 0), ("y", 0)));
        Assert.Equal(CriticalPointKind.Saddle, ExprParser.Parse("x^2 - y^2").ClassifyCriticalPoint(("x", 0), ("y", 0)));
    }

    // The gradient [3x^2 - 3, 2y] is 0 at (1, 0) and (-1, 0).
    [Fact]
    public void Classify_the_critical_points_of_a_cubic()
    {
        Assert.Equal(CriticalPointKind.LocalMinimum, Cubic.ClassifyCriticalPoint(("x", 1), ("y", 0)));
        Assert.Equal(CriticalPointKind.Saddle, Cubic.ClassifyCriticalPoint(("x", -1), ("y", 0)));
    }

    [Fact]
    public void Inconclusive_with_a_zero_eigenvalue_and_none_of_opposite_signs()
    {
        foreach (string text in new[] { "x^4 + y^4", "-x^4 - y^2", "x^3 + y^2", "x^3 - 3*x*y^2", "(x + y)^2" })
            Assert.Equal(CriticalPointKind.Inconclusive, ExprParser.Parse(text).ClassifyCriticalPoint(("x", 0), ("y", 0)));
    }

    [Fact]
    public void Opposite_signs_decide_even_with_a_zero_eigenvalue() =>
        Assert.Equal(CriticalPointKind.Saddle, ExprParser.Parse("x^2 - y^2 + z^4").ClassifyCriticalPoint(("x", 0), ("y", 0), ("z", 0)));

    [Fact]
    public void Classification_does_not_depend_on_the_scale()
    {
        foreach (string scale in new[] { "10^-30", "10^30" })
        {
            Assert.Equal(CriticalPointKind.LocalMinimum, ExprParser.Parse($"{scale}*(x^2 + y^2)").ClassifyCriticalPoint(("x", 0), ("y", 0)));
            Assert.Equal(CriticalPointKind.Saddle, ExprParser.Parse($"{scale}*(x^2 - y^2)").ClassifyCriticalPoint(("x", 0), ("y", 0)));
            Assert.Equal(CriticalPointKind.Inconclusive, ExprParser.Parse($"{scale}*(x + y)^2").ClassifyCriticalPoint(("x", 0), ("y", 0)));
        }
    }

    // As a function of x and y, x^2 is constant along y: not a strict minimum.
    [Fact]
    public void The_variables_are_those_of_the_point()
    {
        Assert.Equal(CriticalPointKind.LocalMinimum, ExprParser.Parse("x^2").ClassifyCriticalPoint(("x", 0)));
        Assert.Equal(CriticalPointKind.Inconclusive, ExprParser.Parse("x^2").ClassifyCriticalPoint(("x", 0), ("y", 0)));
        Assert.Equal(CriticalPointKind.Inconclusive, ExprParser.Parse("2*pi").ClassifyCriticalPoint());
    }

    [Fact]
    public void Point_as_pairs_or_as_a_dictionary() =>
        Assert.Equal(CriticalPointKind.Saddle, Cubic.ClassifyCriticalPoint(new Dictionary<string, double> { ["x"] = -1, ["y"] = 0 }));

    [Fact]
    public void Classify_checks_its_arguments()
    {
        Assert.Throws<ArgumentException>(() => Cubic.ClassifyCriticalPoint(("x", 1)));
        Assert.Throws<ArgumentException>(() => Cubic.ClassifyCriticalPoint(("x", 1), ("y", 0), ("x", -1)));
        Assert.Equal("The second derivative by x and x is Infinity at the point.",
            Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("x*ln(x)").ClassifyCriticalPoint(("x", 0))).Message);
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).ClassifyCriticalPoint(("x", 0)));
        Assert.Throws<ArgumentNullException>(() => Cubic.ClassifyCriticalPoint((IReadOnlyDictionary<string, double>)null!));
        Assert.Throws<ArgumentNullException>(() => Cubic.ClassifyCriticalPoint(((string, double)[])null!));
    }

    [Fact]
    public void A_name_listed_twice_throws()
    {
        Assert.Throws<ArgumentException>(() => F.Hessian("x", "y", "x"));
        Assert.Throws<ArgumentException>(() => F.Laplacian("x", "x"));
    }

    [Fact]
    public void Null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Hessian("x"));
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Hessian());
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Laplacian("x"));
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Laplacian());
        Assert.Throws<ArgumentNullException>(() => F.Hessian(null!));
        Assert.Throws<ArgumentNullException>(() => F.Laplacian(["x", null!]));
    }

    [Fact]
    public void Unsupported_derivatives_throw()
    {
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("floor(x)*y").Hessian());
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("floor(x)*y").Laplacian());
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("floor(x)*y").ClassifyCriticalPoint(("x", 0.5), ("y", 0)));
    }
}
