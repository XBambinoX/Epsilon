using Xunit;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests;

// Every example in the repository README.md, which gives a few examples per package.
public class RepositoryReadmeExamplesTests
{
    [Fact]
    public void Core()
    {
        Expr f = ExprParser.Parse("x^3 - 2x^2 + x", "x");

        Assert.Equal("3x^2 - 4x + 1", f.Differentiate("x").Print());
        Assert.Equal("(x - 1)^2 * x", f.TryFactorReal("x").Factored.Print());
        Assert.Equal([0.0, 1.0], f.FindRealRoots(-10, 10));
        Assert.Equal("-sqrt(2) / 2", ExprParser.Parse("cos(3pi/4)").Simplify().Print());
    }

    [Fact]
    public void Linear_algebra()
    {
        var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");

        Assert.Equal("a * d - b * c", m.Determinant().Print());
        Assert.Equal("[[-2, 1], [3/2, -1/2]]", Matrix<Expr>.Parse("[[1, 2], [3, 4]]").Inverse().Print());
        Assert.Equal(2, Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]).Rank());
    }
}
