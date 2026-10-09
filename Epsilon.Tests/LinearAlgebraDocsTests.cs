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

        Vector<double> u = [3, 4];
        Assert.Equal(5, u.Norm());
        Assert.Equal("[0, 0, 1]", Vector.Create<double>(1, 0, 0).Cross([0, 1, 0]).ToString());
    }

    [Fact]
    public void Matrices_and_vectors()
    {
        Assert.Equal("[[1, 4], [2, 5], [3, 6]]", Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]).Transpose().ToString());
        Assert.Equal("[[0, 1, 2], [10, 11, 12]]", Matrix<double>.Create(2, 3, (i, j) => 10 * i + j).ToString());

        Vector<double> v = [1, 2, 3];
        Assert.Equal(32, v.Dot([4, 5, 6]));
        Assert.Equal("[1, 2]", v[..2].ToString());

        var g = Matrix<double>.FromRows([1, 2], [3, 4]);
        Assert.Equal("[[3, 4]]", g[1.., ..].ToString());
        Assert.Equal("[[1, 2, 5], [3, 4, 6]]", Matrix<double>.FromBlocks([g, Vector.Create(5.0, 6.0)]).ToString());
    }

    [Fact]
    public void Arithmetic()
    {
        Assert.Equal("[[2a, 2b], [2c, 2d]]", (2 * M).Print());
        Assert.Equal("[a * x + b * y, c * x + d * y]", (M * Vector<Expr>.Parse("[x, y]")).Print());
        Assert.Equal("[[1, 10x], [0, 1]]", Matrix<Expr>.Parse("[[1, x], [0, 1]]").Pow(10).Print());
    }

    [Fact]
    public void Vector_geometry()
    {
        Assert.Equal("[1, 1]", Vector.Create<double>(1, -1).Reflect([0, 1]).ToString());
        Assert.Equal("5E+300", Vector.Create(3e300, 4e300).Norm().ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("1E-10", Vector.Create<double>(1, 0).Angle([1, 1e-10]).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(0, Math.Acos(1 / Math.Sqrt(1 + 1e-20)));
        Assert.Equal("π / 4", Vector<Expr>.Parse("[1, 0]").Angle(Vector<Expr>.Parse("[1, 1]")).Print());
    }

    [Fact]
    public void Exact_rational_matrices()
    {
        var hilbert = Matrix<Rational>.Create(8, 8, (i, j) => new Rational(1, i + j + 1));
        Assert.Equal(176679360, hilbert.Inverse()[7, 7]);
        Assert.True(hilbert * hilbert.Inverse() == Matrix<Rational>.Identity(8));
        Assert.Equal("[[-2, 1], [3/2, -1/2]]", Matrix<Rational>.FromRows([1, 2], [3, 4]).Inverse().ToString());
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

        var turn = Matrix<double>.FromRows([Math.Cos(Math.PI / 2), -1], [1, Math.Cos(Math.PI / 2)]);
        Assert.Equal(6.123233995736766E-17, turn[0, 0]);
        Assert.True(turn.IsApproximately(Matrix<double>.FromRows([0, -1], [1, 0])));
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

        Assert.NotEqual(Matrix<Rational>.Zero(2, 2), new Matrix<Rational>(new Rational[2, 2]));
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
        Assert.Equal("[[1, 0], [0, 2]]", Matrix<double>.FromDiagonal([1, 2]).ToString());

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
    public void Submatrices_blocks_and_the_diagonal()
    {
        var g = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
        Assert.Equal("[[4, 5], [7, 8]]", g[1.., ..2].ToString());
        Assert.Equal("[[3], [6]]", g[..^1, 2..].ToString());
        Assert.Equal("[1, 5, 9]", g.Diagonal().ToString());

        var a2 = Matrix<double>.FromRows([1, 2], [3, 4]);
        var i2 = Matrix<double>.Identity(2);
        Assert.Equal("[[1, 2, 1, 0], [3, 4, 0, 1]]", Matrix<double>.FromBlocks([a2, i2]).ToString());
        Assert.Equal("[[1, 2], [3, 4], [1, 0], [0, 1]]", Matrix<double>.FromBlocks([a2], [i2]).ToString());
        Assert.Equal("[[1, 2, 5], [3, 4, 6]]", Matrix<double>.FromBlocks([a2, Vector.Create(5.0, 6.0)]).ToString());

        Assert.StartsWith("Block (0, 1) has 3 rows, but block (0, 0) has 2.",
            Assert.Throws<ArgumentException>(() => Matrix<double>.FromBlocks([a2, Matrix<double>.Zero(3, 1)])).Message);
        Assert.Equal(2, A.Diagonal().Length);
        Assert.StartsWith("The range 1..4 is outside 0..3.",
            Assert.Throws<ArgumentOutOfRangeException>(() => g[1..4, ..]).Message);
    }

    [Fact]
    public void Vectors()
    {
        Vector<double> v = [1, 2, 3];
        var w = Vector.Create(4.0, 5.0, 6.0);

        Assert.Equal(3, v.Length);
        Assert.Equal(1, v[0]);
        Assert.Equal(3, v[^1]);
        Assert.Equal("[1, 2]", v[..2].ToString());
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
        Assert.Equal("[[1, 10x], [0, 1]]", Matrix<Expr>.Parse("[[1, x], [0, 1]]").Pow(10).Print());

        Assert.Equal("Cannot multiply a 2 x 3 matrix by a 2 x 3 matrix: 3 columns on the left, 2 rows on the right.",
            Assert.Throws<ArgumentException>(() => A * A).Message);
    }

    [Fact]
    public void Vector_geometry()
    {
        Vector<double> u = [3, 4];
        Assert.Equal(5, u.Norm());
        Assert.Equal("[0.6, 0.8]", u.Normalize().ToString());
        Assert.Equal("[0, 0, 1]", Vector.Create<double>(1, 0, 0).Cross([0, 1, 0]).ToString());
        Assert.Equal("[1, 1]", Vector.Create<double>(1, -1).Reflect([0, 1]).ToString());
        Assert.Equal("[2.5, 12.5]", Vector<double>.Lerp([0, 10], [10, 20], 0.25).ToString());

        Assert.Equal("[sqrt(2) / 2, sqrt(2) / 2]", Vector<Expr>.Parse("[1, 1]").Normalize().Print());
        Assert.Equal("π / 4", Vector<Expr>.Parse("[1, 0]").Angle(Vector<Expr>.Parse("[1, 1]")).Print());
        Assert.Equal("[b * z - c * y, c * x - a * z, a * y - b * x]",
            Vector<Expr>.Parse("[a, b, c]").Cross(Vector<Expr>.Parse("[x, y, z]")).Print());

        Assert.Equal(5e300, Vector.Create(3e300, 4e300).Norm());
        Assert.Equal(1e-10, Vector.Create<double>(1, 0).Angle([1, 1e-10]));
        Assert.Equal(0, Math.Acos(1 / Math.Sqrt(1 + 1e-20)));

        Assert.Throws<InvalidOperationException>(() => Vector<double>.Zero(2).Normalize());
    }

    [Fact]
    public void Norms_and_approximate_equality()
    {
        double cos = Math.Cos(Math.PI / 2), sin = Math.Sin(Math.PI / 2);
        var turn = Matrix<double>.FromRows([cos, -sin], [sin, cos]);
        var exact = Matrix<double>.FromRows([0, -1], [1, 0]);

        Assert.Equal(6.123233995736766E-17, cos);
        Assert.False(turn == exact);
        Assert.True(turn.IsApproximately(exact));

        var h = Matrix<double>.FromRows([1, 2], [3, 4]);
        var residual = h * h.Inverse() - Matrix<double>.Identity(2);
        Assert.False(residual.IsApproximately(Matrix<double>.Zero(2, 2)));
        Assert.True(residual.IsApproximately(Matrix<double>.Zero(2, 2), absoluteTolerance: 1e-12));

        Assert.False(h.IsApproximately(Matrix<double>.Zero(2, 3)));
        Assert.Equal(Math.Sqrt(30), h.FrobeniusNorm());
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
    public void Exact_rational_matrices()
    {
        var q = Matrix<Rational>.FromRows([1, 2], [3, 4]);
        Assert.Equal(-2, q.Determinant());
        Assert.Equal("[[-2, 1], [3/2, -1/2]]", q.Inverse().ToString());
        Assert.Equal("[-4, 9/2]", q.Solve([5, 6]).ToString());
        Assert.Equal("[[1/3, 2/3], [1, 4/3]]", (q / 3).ToString());

        var singular = Matrix<Rational>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
        Assert.Equal(2, singular.Rank());
        Assert.Equal("The matrix is singular: its rank is 2, not 3.",
            Assert.Throws<InvalidOperationException>(() => singular.Inverse()).Message);

        var hilbert = Matrix<Rational>.Create(8, 8, (i, j) => new Rational(1, i + j + 1));
        Assert.Equal(176679360, hilbert.Inverse()[7, 7]);
        Assert.True(hilbert * hilbert.Inverse() == Matrix<Rational>.Identity(8));

        // "doubles get right to only about eight digits"
        double inDoubles = Matrix<double>.Create(8, 8, (i, j) => 1.0 / (i + j + 1)).Inverse()[7, 7];
        double relativeError = Math.Abs(inDoubles - 176679360) / 176679360;
        Assert.InRange(relativeError, 1e-9, 1e-7);

        Assert.Throws<DivideByZeroException>(() => q / 0);
    }

    [Fact]
    public void In_the_debugger()
    {
        Assert.Equal("2 x 3, [[1, 2, 3], [4, 5, 6]]", A.DebuggerDisplay);
        Assert.Equal("Length = 3, [1, 2.5, 3]", Vector.Create(1, 2.5, 3).DebuggerDisplay);
        Assert.Equal("1 x 1, [[3x^2 - 4x + 1]]", Matrix<Expr>.Parse("[[3x^2 - 4x + 1]]").Simplify().DebuggerDisplay);
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

        // Fails once the core makes default(Rational) a valid 0: then drop the limitation.
        var defaults = new Matrix<Rational>(new Rational[2, 2]);
        Assert.Equal("[[0/0, 0/0], [0/0, 0/0]]", defaults.ToString());
        Assert.NotEqual(Matrix<Rational>.Zero(2, 2), defaults);
        Assert.Throws<DivideByZeroException>(() => defaults + defaults);
    }
}
