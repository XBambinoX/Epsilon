using Xunit;
using Epsilon.Core;
using Epsilon.LinearAlgebra;
// The alias that the README suggests against the clash with System.Numerics.Vector<T>.
using RealVector = Epsilon.LinearAlgebra.Vector<double>;

namespace Epsilon.Tests.LinearAlgebra;

// Epsilon.LinearAlgebra/docs/api-reference.md must list every public type and member. A new
// public member fails this test until it is documented there.
public class LinearAlgebraApiReferenceDocTests
{
    [Fact]
    public void Every_public_type_and_member_is_listed() =>
        Assert.Empty(Core.ApiReferencePages.Missing(typeof(Matrix<>).Assembly, "Epsilon.LinearAlgebra", "docs", "api-reference.md"));
}

// Every example in Epsilon.LinearAlgebra/README.md, with the result its comment shows.
public class LinearAlgebraReadmeExamplesTests
{
    private static readonly Matrix<Expr> M = Matrix<Expr>.Parse("[[a, b], [c, d]]");

    [Fact]
    public void Header()
    {
        Assert.Equal("a * d - b * c", M.Determinant().Print());
        Assert.Equal("[[d, -b], [-c, a]]", M.Adjugate().Print());
    }

    [Fact]
    public void Quick_start()
    {
        Assert.Equal("a * d - b * c", M.Determinant().Print());
        Assert.Equal("a + d", M.Trace().Print());

        var r = Matrix<Expr>.Parse("[[1, 2], [3, 4]]");
        Assert.Equal("[[-2, 1], [3/2, -1/2]]", r.Inverse().Print());
        Assert.Equal("t^2 - 5t - 2", r.CharacteristicPolynomial("t").Print());

        var n = Matrix<double>.FromRows([2, 1], [4, 4]);
        Assert.Equal(4, n.Determinant());
        Assert.Equal("[1, 2]", n.Solve([4, 12]).ToString());
    }

    [Fact]
    public void Matrices_and_vectors()
    {
        Assert.Equal("[[1, 4], [2, 5], [3, 6]]", Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]).Transpose().ToString());
        Assert.Equal("[[0, 1, 2], [10, 11, 12]]", Matrix<double>.Create(2, 3, (i, j) => 10 * i + j).ToString());

        Vector<double> v = [1, 2, 3];
        Assert.Equal(32, v.Dot([4, 5, 6]));
    }

    [Fact]
    public void Arithmetic()
    {
        Assert.Equal("[[2a, 2b], [2c, 2d]]", (2 * M).Print());
        Assert.Equal("[a * x + b * y, c * x + d * y]", (M * Vector<Expr>.Parse("[x, y]")).Print());
    }

    [Fact]
    public void Symbolic_results()
    {
        var inverse = Matrix<Expr>.Parse("[[x, 1], [1, x]]").Inverse();
        Assert.Equal("[[x / (x^2 - 1), -1 / (x^2 - 1)], [-1 / (x^2 - 1), x / (x^2 - 1)]]", inverse.Print());
        Assert.Equal("[[0, 1], [1, 0]]", inverse.Substitute("x", 0).Simplify().Print());

        Assert.Equal("-4", Matrix<Expr>.Parse("[[x + 1, x - 1], [x - 1, x - 3]]").Determinant().Print());
        Assert.Equal("[[cos(x), sin(x)], [-sin(x), cos(x)]]",
            Matrix<Expr>.Parse("[[cos(x), -sin(x)], [sin(x), cos(x)]]").Inverse().Print());
    }

    [Fact]
    public void Numeric_results()
    {
        var s = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);

        Assert.Equal(0, s.Determinant());
        Assert.Equal(2, s.Rank());
        Assert.Equal("The matrix is singular: its rank is 2, not 3, to working precision.",
            Assert.Throws<InvalidOperationException>(() => s.Inverse()).Message);
        Assert.Equal(2, Matrix<double>.FromRows([1e-20, 0], [0, 1]).Rank());
    }

    [Fact]
    public void Entry_wise_text_and_latex()
    {
        var rotation = Matrix<Expr>.Parse("[[cos(x), -sin(x)], [sin(x), cos(x)]]");

        Assert.Equal("[[-sin(x), -cos(x)], [cos(x), -sin(x)]]", rotation.Differentiate("x").Print());
        Assert.Equal("[[1, 2], [3, 4]]", M.Evaluate(("a", 1), ("b", 2), ("c", 3), ("d", 4)).ToString());
        Assert.Equal(@"\begin{bmatrix} x^{2} \\ \frac{1}{2} \end{bmatrix}", Vector<Expr>.Parse("[x^2, 1/2]").Simplify().ToLatex());
    }

    [Fact]
    public void Good_to_know()
    {
        RealVector v = [1, 2];
        Assert.Equal(2, v.Length);

        Assert.Equal("[[1 / 2, x + x]]", Matrix<Expr>.Parse("[[1/2, x + x]]").Print());
        Assert.Equal("[[1]]", (Matrix<Expr>.FromRows([new Variable("x") / new Variable("x")]) + Matrix<Expr>.Zero(1, 1)).Print());
    }
}

// Every example in Epsilon.LinearAlgebra/docs/guide.md, one test per section.
public class LinearAlgebraGuideTests
{
    private static readonly Matrix<double> A = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]);
    private static readonly Matrix<Expr> M = Matrix<Expr>.Parse("[[a, b], [c, d]]");
    private static readonly Matrix<Expr> R = Matrix<Expr>.Parse("[[1, 2], [3, 4]]");
    private static readonly Matrix<Expr> Rotation = Matrix<Expr>.Parse("[[cos(x), -sin(x)], [sin(x), cos(x)]]");

    [Fact]
    public void Creating_a_matrix()
    {
        var b = new Matrix<double>(new double[,] { { 1, 2 }, { 3, 4 } });
        Assert.Equal(Matrix<double>.FromRows([1, 2], [3, 4]), b);
        Assert.Equal("[[0, 1, 2], [10, 11, 12]]", Matrix<double>.Create(2, 3, (i, j) => 10 * i + j).ToString());
        Assert.Equal("[[1, 0], [0, 1]]", Matrix<double>.Identity(2).ToString());
        Assert.Equal("[[0, 0]]", Matrix<Expr>.Zero(1, 2).Print());

        Assert.StartsWith("Row 1 has 1 entries, but row 0 has 2.",
            Assert.Throws<ArgumentException>(() => Matrix<double>.FromRows([1, 2], [3])).Message);
        Assert.Equal(0, Matrix<double>.FromRows().Rows);
    }

    [Fact]
    public void Reading_a_matrix()
    {
        Assert.Equal(2, A.Rows);
        Assert.Equal(3, A.Columns);
        Assert.Equal(6, A[1, 2]);
        Assert.Equal("[4, 5, 6]", A.Row(1).ToString());
        Assert.Equal("[3, 6]", A.Column(2).ToString());
        Assert.Equal("[[1, 4], [2, 5], [3, 6]]", A.Transpose().ToString());
        Assert.Equal("[[1, 4, 9], [16, 25, 36]]", A.Map(v => v * v).ToString());
        Assert.Throws<ArgumentOutOfRangeException>(() => A[2, 0]);
    }

    [Fact]
    public void Vectors()
    {
        Vector<double> v = [1, 2, 3];
        var w = Vector.Create(4.0, 5.0, 6.0);

        Assert.Equal(3, v.Length);
        Assert.Equal(1, v[0]);
        Matrix<double> column = v;
        Assert.Equal("[[1], [2], [3]]", column.ToString());
        Assert.Equal(32, v.Dot(w));
        Assert.Equal(6, v.Sum());
    }

    [Fact]
    public void Equality_and_text()
    {
        Assert.Equal("[[1.5, 2]]", Matrix<double>.FromRows([1.5, 2]).ToString());
        Assert.True(M == Matrix<Expr>.Parse("[[a, b], [c, d]]"));
    }

    [Fact]
    public void Arithmetic()
    {
        Assert.Equal("[[2a, 2b], [2c, 2d]]", (M + M).Print());
        Assert.Equal("[[a * x, b * x], [c * x, d * x]]", (M * new Variable("x")).Print());
        Assert.Equal("[a * x + b * y, c * x + d * y]", (M * Vector<Expr>.Parse("[x, y]")).Print());
        Assert.Equal("a + d", M.Trace().Print());
        Assert.Equal("[[7, 10], [15, 22]]", (R * R).Print());
        Assert.Equal("[[1/2, 1], [3/2, 2]]", (R / 2).Print());

        Assert.Equal("Cannot multiply a 2 x 3 matrix by a 2 x 3 matrix: 3 columns on the left, 2 rows on the right.",
            Assert.Throws<ArgumentException>(() => A * A).Message);
    }

    [Fact]
    public void Determinant_and_characteristic_polynomial()
    {
        Assert.Equal("a * d - b * c", M.Determinant().Print());
        Assert.Equal("-4", Matrix<Expr>.Parse("[[x + 1, x - 1], [x - 1, x - 3]]").Determinant().Print());

        var p = Matrix<Expr>.Parse("[[2, 1], [1, 2]]").CharacteristicPolynomial("t");
        Assert.Equal("t^2 - 4t + 3", p.Print());
        Assert.Equal([1.0, 3.0], p.FindRealRoots());

        Assert.Throws<ArgumentException>(() => M.CharacteristicPolynomial("a"));
    }

    [Fact]
    public void Adjugate_inverse_and_solve()
    {
        Assert.Equal("[[d, -b], [-c, a]]", M.Adjugate().Print());
        Assert.Equal("[[-2, 1], [3/2, -1/2]]", R.Inverse().Print());
        Assert.Equal("[(-b * f + e * d) / (a * d - b * c), (a * f - e * c) / (a * d - b * c)]",
            M.Solve(Vector<Expr>.Parse("[e, f]")).Print());

        var inverse = Matrix<Expr>.Parse("[[x, 1], [1, x]]").Inverse();
        Assert.Equal("[[x / (x^2 - 1), -1 / (x^2 - 1)], [-1 / (x^2 - 1), x / (x^2 - 1)]]", inverse.Print());
        Assert.Equal("[[0, 1], [1, 0]]", inverse.Substitute("x", 0).Simplify().Print());
        Assert.Equal("[[1 / 0, -1 / 0], [-1 / 0, 1 / 0]]", inverse.Substitute("x", 1).Simplify().Print());

        Assert.Equal("The matrix is singular: its determinant is 0.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[x, 2x], [1, 2]]").Inverse()).Message);
    }

    [Fact]
    public void Numeric_methods()
    {
        var n = Matrix<double>.FromRows([2, 1], [4, 4]);

        Assert.Equal(4, n.Determinant());
        Assert.Equal("[1, 2]", n.Solve([4, 12]).ToString());
        Assert.Equal("[[1, 1], [2, -1]]", n.Solve(Matrix<double>.FromRows([4, 1], [12, 0])).ToString());
        Assert.Equal("[[1, -0.25], [-1, 0.5]]", n.Inverse().ToString());
        Assert.Equal(2, n.Rank());
    }

    [Fact]
    public void When_is_a_matrix_singular()
    {
        var s = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
        Assert.Equal(0, s.Determinant());
        Assert.Equal(2, s.Rank());
        Assert.Equal("The matrix is singular: its rank is 2, not 3, to working precision.",
            Assert.Throws<InvalidOperationException>(() => s.Inverse()).Message);

        var scaled = Matrix<double>.FromRows([1e-20, 0], [0, 1]);
        Assert.Equal("1E-20", scaled.Determinant().ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("[[1E+20, 0], [0, 1]]", scaled.Inverse().ToString());

        Assert.Equal("The matrix has a non-finite entry at (0, 1): NaN.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, double.NaN]).Rank()).Message);
    }

    [Fact]
    public void Entry_wise_operations()
    {
        Assert.Equal("[[x^2 + 2x + 1, x^2 - 1]]", Matrix<Expr>.Parse("[[(x + 1)^2, (x - 1)*(x + 1)]]").Expand().Print());
        Assert.Equal("[[-sin(x), -cos(x)], [cos(x), -sin(x)]]", Rotation.Differentiate("x").Print());
        Assert.Equal("1", Rotation.Determinant().Print());
        Assert.Equal("[[1, 2], [3, 4]]", M.Evaluate(("a", 1), ("b", 2), ("c", 3), ("d", 4)).ToString());
        Assert.Equal("[[3, 9]]", Matrix<Expr>.Parse("[[x, x^2]]").Evaluate(3).ToString());

        Assert.Equal("Expected at most 1 variable, found 2: [x, y]. Use the overload with named values for several variables.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[x, y]]").Evaluate(3)).Message);
    }

    [Theory]
    [InlineData("[[1, 2], [3]]", "Row 1 has 1 entries, but row 0 has 2.")]
    [InlineData("[[1, 2], [3, 4]", "Expected ']' at position 15, found the end of the text.")]
    [InlineData("[[1]] x", "Unexpected 'x' at position 6 after the closing ']'.")]
    [InlineData("[[1, sen(x)]]", "Invalid entry at (0, 1), starting at position 5: Unknown identifier 'sen' at position 0. Declared variables: x.")]
    public void Parse_errors(string text, string message) =>
        Assert.Equal(message, Assert.Throws<FormatException>(() => Matrix<Expr>.Parse(text, ["x"])).Message);

    [Fact]
    public void Text_and_latex()
    {
        Assert.Equal("[[1 / 2, x + x]]", Matrix<Expr>.Parse("[[1/2, x + x]]").Print());
        Assert.Equal("[[1/2, 2x]]", Matrix<Expr>.Parse("[[1/2, x + x]]").Simplify().Print());
        Assert.Equal("[[theta, 2t]]", Matrix<Expr>.Parse("[[theta, 2t]]", ["theta", "t"]).Print());

        Assert.Equal(@"\begin{bmatrix} \cos\left(x\right) & -\sin\left(x\right) \\ \sin\left(x\right) & \cos\left(x\right) \end{bmatrix}",
            Rotation.ToLatex());
        Assert.Equal(@"\begin{bmatrix} x^{2} \\ \frac{1}{2} \end{bmatrix}", Vector<Expr>.Parse("[x^2, 1/2]").Simplify().ToLatex());
    }

    [Fact]
    public void Limitations()
    {
        Assert.Throws<InvalidOperationException>(() => Matrix<double>.Zero(2, 3).Solve([1, 2]));
        Assert.Equal("a * d + t^2 - b * c + (-a - d) * t", M.CharacteristicPolynomial("t").Print());
    }
}
