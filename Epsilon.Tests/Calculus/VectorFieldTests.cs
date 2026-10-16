using Epsilon.Calculus;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.Calculus;

public class VectorFieldTests
{
    private static Vector<Expr> Field(params string[] entries) =>
        Vector.Create(entries.Select(entry => ExprParser.Parse(entry)).ToArray());

    [Fact]
    public void Divergence_small_examples()
    {
        Assert.Equal("3y", Field("x*y", "y^2").Divergence("x", "y").Print());
        Assert.Equal("3", Field("x", "y", "z").Divergence("x", "y", "z").Print());
        Assert.Equal("0", Field("-y", "x").Divergence("x", "y").Print());
    }

    [Fact]
    public void Divergence_pairs_entry_i_with_variable_i() =>
        Assert.Equal("x", Field("x*y", "y^2").Divergence("y", "x").Print());

    [Fact]
    public void Divergence_of_a_gradient_is_the_sum_of_the_second_derivatives() =>
        Assert.Equal("8y", ExprParser.Parse("x^2*y + y^3").Gradient("x", "y").Divergence("x", "y").Print());

    [Fact]
    public void Curl_small_examples()
    {
        Assert.Equal("[0, 0, 2]", Field("-y", "x", "0").Curl("x", "y", "z").Print());
        Assert.Equal("[0, 0, y^2 - x^2]", Field("x^2*y", "x*y^2", "0").Curl("x", "y", "z").Print());
    }

    [Fact]
    public void Curl_of_a_gradient_and_divergence_of_a_curl_are_zero()
    {
        var f = ExprParser.Parse("x*y*z + exp(x)*sin(y) + z^2");
        Assert.Equal("[0, 0, 0]", f.Gradient("x", "y", "z").Curl("x", "y", "z").Print());

        Assert.Equal("0", Field("x*y*z", "x^2 + z", "sin(x*y)").Curl("x", "y", "z").Divergence("x", "y", "z").Print());
    }

    // Simplify doesn't bring rational expressions to a common denominator, so the mixed partial
    // derivatives of atan(y/z) cancel only in value.
    [Fact]
    public void Curl_of_a_gradient_is_zero_in_value_where_simplify_cannot_show_it()
    {
        var curl = ExprParser.Parse("exp(x*y)*cos(z) + atan(y/z)").Gradient("x", "y", "z").Curl("x", "y", "z");

        foreach (var (x, y, z) in new[] { (0.3, -1.2, 2.5), (1.0, 2.0, -0.5), (-2.0, 0.1, 1.0) })
            foreach (double entry in curl.Evaluate(("x", x), ("y", y), ("z", z)))
                Assert.Equal(0, entry, 1e-12);
    }

    [Fact]
    public void Lengths_must_match()
    {
        Assert.Throws<ArgumentException>(() => Field("x", "y").Divergence("x"));
        Assert.Throws<ArgumentException>(() => Field("x", "y", "z").Curl("x", "y"));
        Assert.Throws<ArgumentException>(() => Field("x", "y").Curl("x", "y"));
    }

    [Fact]
    public void A_name_listed_twice_throws()
    {
        Assert.Throws<ArgumentException>(() => Field("x", "y").Divergence("x", "x"));
        Assert.Throws<ArgumentException>(() => Field("x", "y", "z").Curl("x", "y", "x"));
    }

    [Fact]
    public void Null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() => ((Vector<Expr>)null!).Divergence("x"));
        Assert.Throws<ArgumentNullException>(() => Field("x").Divergence(null!));
        Assert.Throws<ArgumentNullException>(() => ((Vector<Expr>)null!).Curl("x", "y", "z"));
        Assert.Throws<ArgumentNullException>(() => Field("x", "y", "z").Curl("x", null!, "z"));
    }

    [Fact]
    public void Unsupported_derivatives_throw()
    {
        Assert.Throws<NotSupportedException>(() => Field("floor(x)", "y").Divergence("x", "y"));
        Assert.Throws<NotSupportedException>(() => Field("x", "floor(x)", "z").Curl("x", "y", "z"));
    }
}
