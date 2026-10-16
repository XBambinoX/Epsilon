using Xunit;
using Epsilon.Calculus;
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
        Assert.Equal("[1.5, 1]", Matrix<double>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]).LeastSquares([1, 3, 4, 4]).ToString());
        Assert.Equal("[2, 3, 4]", Matrix4x4<double>.Translation((1, 2, 3)).TransformPoint((1, 1, 1)).ToString());
    }

    [Fact]
    public void Calculus()
    {
        var f = ExprParser.Parse("x^3 - 3*x + y^2");

        Assert.Equal("[3x^2 - 3, 2y]", f.Gradient("x", "y").Print());
        Assert.Equal(CriticalPointKind.LocalMinimum, f.ClassifyCriticalPoint(("x", 1), ("y", 0)));
        Assert.Equal("exp(x) * x - exp(x)", ExprParser.Parse("x*exp(x)").Integrate("x").Print());
        Assert.Equal("(1/2) * ln(abs((x - 1) / (x + 1)))", ExprParser.Parse("1/(x^2 - 1)").Integrate("x").Print());
        Assert.Equal(-4, ExprParser.Parse("ln(x)/sqrt(x)").Integrate(0, 1));
    }
}
