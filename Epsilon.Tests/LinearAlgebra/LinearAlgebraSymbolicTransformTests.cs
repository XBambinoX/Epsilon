using Xunit;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class SymbolicTransformTests
{
    private static Expr P(string text) => ExprParser.Parse(text);

    private static Vector<Expr> V(string text) => Vector<Expr>.Parse(text);

    [Fact]
    public void Rotations_with_a_symbolic_angle()
    {
        Assert.Equal("[[cos(t), -sin(t)], [sin(t), cos(t)]]", Matrix<Expr>.Rotation(P("t")).Print());
        Assert.Equal("[[1, 0, 0], [0, cos(t), -sin(t)], [0, sin(t), cos(t)]]", Matrix<Expr>.RotationX(P("t")).Print());
        Assert.Equal("[[cos(t), 0, sin(t)], [0, 1, 0], [-sin(t), 0, cos(t)]]", Matrix<Expr>.RotationY(P("t")).Print());
        Assert.Equal("[[cos(t), -sin(t), 0], [sin(t), cos(t), 0], [0, 0, 1]]", Matrix<Expr>.RotationZ(P("t")).Print());
    }

    [Fact]
    public void Known_angles_give_exact_entries()
    {
        Assert.Equal("[[0, -1], [1, 0]]", Matrix<Expr>.Rotation(P("pi / 2")).Print());
        Assert.Equal("[[0, -1, 0], [1, 0, 0], [0, 0, 1]]", Matrix<Expr>.RotationZ(P("pi / 2")).Print());
        Assert.Equal("[[1, 0, 0], [0, 1/2, -sqrt(3) / 2], [0, sqrt(3) / 2, 1/2]]", Matrix<Expr>.RotationX(P("pi / 3")).Print());
    }

    [Theory]
    [InlineData(0.7)]
    [InlineData(-2.5)]
    public void Rotations_agree_with_the_fixed_size_ones(double angle)
    {
        var t = new Variable("t");

        Assert.Equal(Matrix2x2<double>.Rotation(angle).ToMatrix(), Matrix<Expr>.Rotation(t).Evaluate(angle));
        Assert.Equal(Matrix3x3<double>.RotationX(angle).ToMatrix(), Matrix<Expr>.RotationX(t).Evaluate(angle));
        Assert.Equal(Matrix3x3<double>.RotationY(angle).ToMatrix(), Matrix<Expr>.RotationY(t).Evaluate(angle));
        Assert.Equal(Matrix3x3<double>.RotationZ(angle).ToMatrix(), Matrix<Expr>.RotationZ(t).Evaluate(angle));
    }

    [Fact]
    public void A_product_of_rotations_is_a_rotation_from_Euler_angles()
    {
        var r = Matrix<Expr>.RotationZ(P("c")) * Matrix<Expr>.RotationY(P("b")) * Matrix<Expr>.RotationX(P("a"));
        var expected = Matrix3x3<double>.FromEulerAngles((0.3, -0.5, 1.2), EulerOrder.XYZ).ToMatrix();

        Assert.True(r.Evaluate(("a", 0.3), ("b", -0.5), ("c", 1.2)).IsApproximately(expected, 1e-15));
    }

    [Fact]
    public void Translation_in_homogeneous_coordinates()
    {
        Assert.Equal("[[1, 0, 0, a], [0, 1, 0, b], [0, 0, 1, c], [0, 0, 0, 1]]", Matrix<Expr>.Translation(V("[a, b, c]")).Print());
        Assert.Equal("[[1, 0, x], [0, 1, 2], [0, 0, 1]]", Matrix<Expr>.Translation(V("[x, 2]")).Print());
        Assert.Equal("[[1]]", Matrix<Expr>.Translation(V("[]")).Print());

        Assert.Equal(
            Matrix4x4<double>.Translation((1, -2, 3)).ToMatrix(),
            Matrix<Expr>.Translation(V("[a, b, c]")).Evaluate(("a", 1), ("b", -2), ("c", 3)));
    }

    [Fact]
    public void ToHomogeneous_adds_a_row_and_a_column_of_the_identity()
    {
        Assert.Equal(Matrix<Expr>.RotationZ(P("t")), Matrix<Expr>.Rotation(P("t")).ToHomogeneous());
        Assert.Equal("[[a, b, c, 0], [d, e, f, 0], [0, 0, 0, 1]]", Matrix<Expr>.Parse("[[a, b, c], [d, e, f]]").ToHomogeneous().Print());
        Assert.Equal("[[s, 0, 0], [0, s, 0], [0, 0, 1]]", Matrix<Expr>.FromDiagonal(V("[s, s]")).ToHomogeneous().Print());
    }

    [Fact]
    public void TransformPoint_gives_the_end_of_a_kinematic_chain()
    {
        // A planar arm: turn by a, go l along the first link, turn by b, go m along the second.
        var arm = Matrix<Expr>.Rotation(P("a")).ToHomogeneous() * Matrix<Expr>.Translation(V("[l, 0]"))
            * Matrix<Expr>.Rotation(P("b")).ToHomogeneous() * Matrix<Expr>.Translation(V("[m, 0]"));
        var tip = arm.TransformPoint(V("[0, 0]"));

        double a = 0.4, b = 1.1, l = 2, m = 1.5;
        var expected = Vector.Create(l * Math.Cos(a) + m * Math.Cos(a + b), l * Math.Sin(a) + m * Math.Sin(a + b));
        var at = new Dictionary<string, double> { ["a"] = a, ["b"] = b, ["l"] = l, ["m"] = m };

        Assert.True(tip.Evaluate(at).IsApproximately(expected, 1e-15));
        Assert.Equal((2, 2), (tip.Jacobian(["a", "b"]).Rows, tip.Jacobian(["a", "b"]).Columns));
    }

    [Fact]
    public void TransformPoint_divides_by_w_unless_it_is_1()
    {
        var projective = Matrix<Expr>.Parse("[[1, 0, 0], [0, 1, 0], [0, 1, 1]]");

        Assert.Equal("[x / (y + 1), y / (y + 1)]", projective.TransformPoint(V("[x, y]")).Print());
        Assert.Equal("[a + x, b + y]", Matrix<Expr>.Translation(V("[a, b]")).TransformPoint(V("[x, y]")).Print());

        var numeric = Matrix3x3<double>.FromRows((1, 0, 0), (0, 1, 0), (0, 1, 1));
        Assert.Equal(numeric.TransformPoint((3, 0.5)).ToVector(), projective.TransformPoint(V("[x, y]")).Evaluate(("x", 3), ("y", 0.5)));
    }

    [Fact]
    public void TransformDirection_ignores_the_translation()
    {
        var turnAndMove = Matrix<Expr>.Translation(V("[a, b]")) * Matrix<Expr>.Rotation(P("pi / 2")).ToHomogeneous();

        Assert.Equal("[-y, x]", turnAndMove.TransformDirection(V("[x, y]")).Print());
        Assert.Equal("[-y + a, b + x]", turnAndMove.TransformPoint(V("[x, y]")).Print());
    }

    [Fact]
    public void Errors()
    {
        var rotation = Matrix<Expr>.RotationZ(P("t"));

        var ex = Assert.Throws<ArgumentException>(() => rotation.TransformPoint(V("[x, y, z]")));
        Assert.Equal("Transforming a vector of length 3 needs a 4 x 4 matrix in homogeneous coordinates, not 3 x 3. (Parameter 'point')", ex.Message);
        Assert.Throws<ArgumentException>(() => Matrix<Expr>.Parse("[[1, 0, 0], [0, 1, 0]]").TransformDirection(V("[x, y]")));
        Assert.Throws<ArgumentNullException>(() => rotation.TransformPoint(null!));
        Assert.Throws<ArgumentNullException>(() => Matrix<Expr>.RotationX(null!));
        Assert.Throws<ArgumentNullException>(() => Matrix<Expr>.Translation(null!));
    }
}
