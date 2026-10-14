using Xunit;
using Epsilon.Core;
using Epsilon.LinearAlgebra;
// The alias that the README suggests against the clash with System.Numerics.Vector<T>.
using RealVector = Epsilon.LinearAlgebra.Vector<double>;
// The aliases of the fixed-size types in the README and the docs pages.
using Vec3 = Epsilon.LinearAlgebra.Vector3<double>;
using Mat4 = Epsilon.LinearAlgebra.Matrix4x4<double>;

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
    public void Least_squares_and_decompositions()
    {
        Assert.Equal("[1.5, 1]", Matrix<double>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]).LeastSquares([1, 3, 4, 4]).ToString());

        var (values, vectors) = Matrix<double>.FromRows([2, 1], [1, 2]).SymmetricEigen();
        Assert.Equal("[1, 3]", values.ToString());
        Assert.Equal("[6.708203932499369, 2.2360679774997894]", Matrix<double>.FromRows([3, 0], [4, 5]).Svd().S.ToString());
        Assert.Equal("[[0.04, 0.08], [0.08, 0.16]]", Matrix<double>.FromRows([1, 2], [2, 4]).PseudoInverse().ToString());
        Assert.Equal(14.933034373659256, Matrix<double>.FromRows([1, 2], [3, 4]).ConditionNumber());
        Assert.Equal("[1, 1, 1]", Matrix3x3<double>.Identity.SymmetricEigen().Values.ToString());
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
    public void Fixed_size_vectors_and_matrices()
    {
        var model = Mat4.Translation((10, 0, 5)) * Mat4.RotationY(Math.PI / 2) * Mat4.Scale(2);
        Vec3 tip = model.TransformPoint((1, 0, 0));
        Assert.Equal("[10, 0, 3]", tip.ToString());
        Assert.NotEqual(new Vec3(1, 0, 0), model.Inverse().TransformPoint(tip));   // "up to rounding"
        Assert.True(model.Inverse().TransformPoint(tip).IsApproximately((1, 0, 0)));
        Assert.Equal("[1, 1, 0]", new Vec3(1, -1, 0).Reflect(Vec3.UnitY).ToString());
        Assert.Equal(System.Numerics.Matrix4x4.CreateTranslation(1, 2, 3), Matrix4x4<float>.Translation((1, 2, 3)).ToSystemNumerics());

        var noisy = new Matrix3x3<double>(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9);
        Assert.Equal(1.734723475976807E-17, noisy.Determinant());
        Assert.False(noisy.TryInverse(out _));

        var turn = Quaternion<double>.RotationY(Math.PI / 2);
        Assert.True(Quaternion<double>.Slerp(Quaternion<double>.Identity, turn, 0.5).Angle(Quaternion<double>.RotationY(Math.PI / 4)) < 1e-15);
        var view = Mat4.LookAt((0, 2, 5), (0, 0, 0), Vec3.UnitY);
        var projection = Mat4.Perspective(Math.PI / 3, 16.0 / 9, 0.1, 100);
        Assert.Equal(0.9824128790613561, (projection * view).TransformPoint((0, 0, 0)).Z);
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
    public void From_formulas_to_fast_code()
    {
        static Matrix<Expr> Link(string angle, int length) =>
            Matrix<Expr>.Rotation(new Variable(angle)).ToHomogeneous() * Matrix<Expr>.Translation([length, 0]);

        Vector<Expr> tip = (Link("a", 3) * Link("b", 2) * Link("c", 1)).TransformPoint([0, 0]);
        string[] joints = ["a", "b", "c"];
        var position = tip.Compile(joints);
        var jacobian = tip.Jacobian(joints).Compile(joints);

        Assert.Equal("[6, 0]", position([0, 0, 0]).ToString());
        Assert.Equal("[6, 3, 1]", jacobian([0, 0, 0]).Row(1).ToString());
        Assert.StartsWith("public static void ArmTip(double a, ", tip.ToCSharp("ArmTip", joints));
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

// Every relative link in the pages of Epsilon.LinearAlgebra/docs and in its changelog leads to a
// file that exists, and an anchor to a heading of that file.
public class LinearAlgebraDocsLinksTests
{
    [Fact]
    public void Relative_links_resolve() =>
        Assert.Empty(MarkdownLinks.Broken(Path.Combine("Epsilon.LinearAlgebra", "docs"), Path.Combine("Epsilon.LinearAlgebra", "CHANGELOG.md")));
}

public static class MarkdownLinks
{
    // Links as "file#anchor: reason", for the Markdown files in the given directories and files,
    // which are relative to the repository root.
    public static List<string> Broken(params string[] paths)
    {
        string root = RepositoryRoot(paths[0]);
        var files = paths
            .Select(p => Path.Combine(root, p))
            .SelectMany(p => Directory.Exists(p) ? Directory.GetFiles(p, "*.md") : [p]);

        var broken = new List<string>();
        foreach (string file in files)
        {
            foreach (System.Text.RegularExpressions.Match link in System.Text.RegularExpressions.Regex.Matches(Prose(file), @"\]\(([^)\s]+)\)"))
            {
                string target = link.Groups[1].Value;
                if (target.StartsWith("http"))
                    continue;

                string[] parts = target.Split('#', 2);
                string linked = parts[0] == "" ? file : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, parts[0]));
                string name = $"{Path.GetFileName(file)}: {target}";
                if (!File.Exists(linked) && !Directory.Exists(linked))
                    broken.Add($"{name}: no such file");
                else if (parts.Length == 2 && !Anchors(linked).Contains(parts[1]))
                    broken.Add($"{name}: no such heading");
            }
        }
        return broken;
    }

    // The anchors GitHub gives the headings: lower case, punctuation dropped, spaces as hyphens.
    private static HashSet<string> Anchors(string file) =>
        Prose(file).Split('\n')
            .Where(line => line.StartsWith('#'))
            .Select(line => System.Text.RegularExpressions.Regex.Replace(line.TrimStart('#').Trim().ToLowerInvariant(), @"[^\w\- ]", "").Replace(' ', '-'))
            .ToHashSet();

    // The text outside fenced code blocks.
    private static string Prose(string file) =>
        System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(file).ReplaceLineEndings("\n"), @"^```.*?^```", "", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Singleline);

    private static string RepositoryRoot(string relativePath)
    {
        string dir = AppContext.BaseDirectory;
        while (!Path.Exists(Path.Combine(dir, relativePath)))
            dir = Path.GetDirectoryName(dir) ?? throw new FileNotFoundException($"{relativePath} not found");
        return dir;
    }
}

// Every example in Epsilon.LinearAlgebra/docs/getting-started.md.
public class LinearAlgebraGettingStartedDocTests
{
    [Fact]
    public void Install()
    {
        RealVector v = [1, 2];
        Assert.Equal(2, v.Length);
    }

    [Fact]
    public void A_first_example()
    {
        var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");
        var crossing = m.Solve(Vector<Expr>.Parse("[p, q]"));
        Assert.Equal("[(d * p - b * q) / (a * d - b * c), (a * q - c * p) / (a * d - b * c)]", crossing.Print());
        Assert.Equal("a * d - b * c", m.Determinant().Print());
        Assert.Empty(Vector<Expr>.Parse("[e, i]").GetVariables());

        var parallel = m.Substitute("c", ExprParser.Parse("2a")).Substitute("d", ExprParser.Parse("2b"));
        Assert.Equal("The matrix is singular: its determinant is 0.",
            Assert.Throws<InvalidOperationException>(() => parallel.Solve(Vector<Expr>.Parse("[p, q]"))).Message);

        Assert.Equal("[0.2, 0.4]", crossing.Evaluate(("a", 3), ("b", 1), ("c", 1), ("d", 2), ("p", 1), ("q", 1)).ToString());
        Assert.Equal("[0.19999999999999998, 0.4]", Matrix<double>.FromRows([3, 1], [1, 2]).Solve([1, 1]).ToString());
        Assert.Equal("[1/5, 2/5]", Matrix<Rational>.FromRows([3, 1], [1, 2]).Solve([1, 1]).ToString());

        var flat = Matrix<double>.FromRows([1, 2], [2, 4]);
        Assert.Equal(0, flat.Determinant());
        Assert.Equal("The matrix is singular: its rank is 1, not 2, to working precision.",
            Assert.Throws<InvalidOperationException>(() => flat.Solve([1, 1])).Message);

        Assert.Equal(@"\begin{bmatrix} a & b \\ c & d \end{bmatrix}", m.ToLatex());
    }

    [Fact]
    public void Which_entry_type()
    {
        Assert.Equal("[2, 3, 4]", Matrix4x4<double>.Translation((1, 2, 3)).TransformPoint((1, 1, 1)).ToString());

        var symbolic = Matrix<Expr>.Parse("[[x, 1], [2, x]]");
        Matrix<double> numbers = symbolic.Evaluate(3);
        Assert.Equal("[[3, 1], [2, 3]]", numbers.ToString());
        Matrix<Rational> exact = numbers.Map(Rational.FromDouble);
        Assert.Equal("[[3, 1], [2, 3]]", exact.ToString());
        Assert.Equal(new Rational(1, 10), Rational.FromDouble(0.1));
        Matrix<Expr> constants = exact.Map(r => (Expr)r);
        Assert.Equal("[[3/7, -1/7], [-2/7, 3/7]]", constants.Inverse().Print());
        Matrix2x2<double> small = (Matrix2x2<double>)numbers;
        Assert.True(small.ToMatrix() == numbers);
    }
}

// Every example in Epsilon.LinearAlgebra/docs/matrices-and-vectors.md, one test per section.
public class LinearAlgebraMatricesAndVectorsDocTests
{
    private static readonly Matrix<double> A = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]);

    [Fact]
    public void Creating_a_matrix()
    {
        var b = new Matrix<double>(new double[,] { { 1, 2 }, { 3, 4 } });
        Assert.Equal(Matrix<double>.FromRows([1, 2], [3, 4]), b);
        Assert.Equal("[[0, 1, 2], [10, 11, 12]]", Matrix<double>.Create(2, 3, (i, j) => 10 * i + j).ToString());
        Assert.Equal("[[1, 0], [0, 1]]", Matrix<double>.Identity(2).ToString());
        Assert.Equal("[[0, 0]]", Matrix<Expr>.Zero(1, 2).Print());
        Assert.Equal("[[1, 0], [0, 2]]", Matrix<double>.FromDiagonal([1, 2]).ToString());

        double[,] array = { { 1, 2 } };
        var copied = new Matrix<double>(array);
        array[0, 0] = 9;
        Assert.Equal(1, copied[0, 0]);
        Assert.StartsWith("Row 1 has 1 entries, but row 0 has 2.",
            Assert.Throws<ArgumentException>(() => Matrix<double>.FromRows([1, 2], [3])).Message);
        Assert.Equal(0, Matrix<double>.FromRows().Rows);

        var x = new Variable("x");
        Assert.Equal("[[x, 1], [0, x]]", Matrix<Expr>.FromRows([x, 1], [0, x]).Print());
        Assert.Equal("[[a, c], [b, d]]", Matrix<string>.FromRows(["a", "b"], ["c", "d"]).Transpose().ToString());
        Assert.Equal("[[1, 0, 0], [0, 1, 0], [0, 0, 1]]", Matrix<int>.Create(3, 3, (i, j) => i == j ? 1 : 0).ToString());
    }

    [Fact]
    public void Reading_a_matrix()
    {
        Assert.Equal(2, A.Rows);
        Assert.Equal(3, A.Columns);
        Assert.False(A.IsSquare);
        Assert.Equal(6, A[1, 2]);
        Assert.Equal("[4, 5, 6]", A.Row(1).ToString());
        Assert.Equal("[3, 6]", A.Column(2).ToString());
        Assert.Equal("[[1, 4], [2, 5], [3, 6]]", A.Transpose().ToString());
        Assert.Equal("[[1, 4, 9], [16, 25, 36]]", A.Map(v => v * v).ToString());
        Assert.Equal("[[False, False, True], [True, True, True]]", A.Map(v => v > 2).ToString());
        Assert.Equal(4, A.ToArray()[1, 0]);
        Assert.StartsWith("The matrix has 2 rows.", Assert.Throws<ArgumentOutOfRangeException>(() => A[2, 0]).Message);
    }

    [Fact]
    public void Submatrices_blocks_and_the_diagonal()
    {
        var g = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
        Assert.Equal("[[4, 5], [7, 8]]", g[1.., ..2].ToString());
        Assert.Equal("[[3], [6]]", g[..^1, 2..].ToString());
        Assert.Equal("[[2, 3], [5, 6], [8, 9]]", g[.., 1..].ToString());
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

        Assert.Equal("[[1, 0, 0], [0, 1, 0], [0, 0, 5]]", Matrix<double>.FromBlocks(
            [Matrix<double>.Identity(2), Matrix<double>.Zero(2, 1)],
            [Matrix<double>.Zero(1, 2), Matrix<double>.FromDiagonal([5])]).ToString());
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
        Assert.Equal("[2, 3]", v[1..].ToString());
        Matrix<double> column = v;
        Assert.Equal("[[1], [2], [3]]", column.ToString());
        Assert.Equal(column, v.ToMatrix());
        Assert.Equal("[1, 4, 9]", v.Map(e => e * e).ToString());
        Assert.StartsWith("The vector has 3 entries.", Assert.Throws<ArgumentOutOfRangeException>(() => v[3]).Message);

        Assert.Equal(6, v.Sum());
        Assert.Equal(3, v.Max());
        Assert.Equal("1 2 3", string.Join(" ", v));
        var seen = new List<double>();
        foreach (double entry in w)
            seen.Add(entry);
        Assert.Equal([4.0, 5, 6], seen);
    }

    [Fact]
    public void Equality_and_text()
    {
        Assert.Equal("[[1.5, 2]]", Matrix<double>.FromRows([1.5, 2]).ToString());
        Assert.False(Matrix<double>.FromRows([1, 2]) == Matrix<double>.FromRows([1], [2]));
        Assert.True(Matrix<double>.FromRows([double.NaN]) == Matrix<double>.FromRows([double.NaN]));
        Assert.False(Matrix<double>.FromRows([0.1 + 0.2]) == Matrix<double>.FromRows([0.3]));
        Assert.Equal(0.30000000000000004, 0.1 + 0.2);
        Assert.True(Matrix<Expr>.Parse("[[a, b], [c, d]]") == Matrix<Expr>.Parse("[[a, b], [c, d]]"));

        var keys = new HashSet<Matrix<double>> { Matrix<double>.Identity(2), Matrix<double>.FromRows([1, 0], [0, 1]) };
        Assert.Single(keys);
    }

    [Fact]
    public void In_the_debugger()
    {
        Assert.Equal("2 x 3, [[1, 2, 3], [4, 5, 6]]", A.DebuggerDisplay);
        Assert.Equal("Length = 3, [1, 2.5, 3]", Vector.Create(1, 2.5, 3).DebuggerDisplay);
        Assert.Equal("1 x 1, [[3x^2 - 4x + 1]]", Matrix<Expr>.Parse("[[3x^2 - 4x + 1]]").Simplify().DebuggerDisplay);
        Assert.Equal("5 x 5", Matrix<double>.Identity(5).DebuggerDisplay);
        Assert.Equal("[1, 2, 3]", new Vec3(1, 2, 3).ToString());
        Assert.Equal("{ToString(),nq}", typeof(Vec3).GetCustomAttributes(typeof(System.Diagnostics.DebuggerDisplayAttribute), false)
            .Cast<System.Diagnostics.DebuggerDisplayAttribute>().Single().Value);
    }
}

// Every example in Epsilon.LinearAlgebra/docs/arithmetic.md, one test per section.
public class LinearAlgebraArithmeticDocTests
{
    private static readonly Matrix<double> A = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]);
    private static readonly Matrix<Expr> M = Matrix<Expr>.Parse("[[a, b], [c, d]]");
    private static readonly Matrix<Rational> Q = Matrix<Rational>.FromRows([1, 2], [3, 4]);
    private static readonly Matrix<double> N = Matrix<double>.FromRows([2, 1], [4, 4]);

    [Fact]
    public void Operators()
    {
        Assert.Equal("[[14, 32], [32, 77]]", (A * A.Transpose()).ToString());
        Assert.Equal("[[4, 5], [10, 11]]", (A * Matrix<double>.FromRows([1, 0], [0, 1], [1, 1])).ToString());
        Assert.Equal("[-2, -2]", (A * Vector.Create(1.0, 0, -1)).ToString());
        Assert.Equal("[[2, 4, 6], [8, 10, 12]]", (2 * A).ToString());
        Assert.Equal("[[0.5, 1, 1.5], [2, 2.5, 3]]", (A / 2).ToString());

        Vector<double> v = [1, 2, 3], w = [4, 5, 6];
        Assert.Equal("[5, 7, 9]", (v + w).ToString());
        Assert.Equal(32, v.Dot(w));

        Assert.Equal("[[1/2, 1], [3/2, 2]]", (Q * new Rational(1, 2)).ToString());
        Assert.Equal("[[7, 10], [15, 22]]", (Q * Q).ToString());

        Assert.Equal("[[2a, 2b], [2c, 2d]]", (M + M).Print());
        Assert.Equal("[[a * x, b * x], [c * x, d * x]]", (M * new Variable("x")).Print());
        Assert.Equal("[a * x + b * y, c * x + d * y]", (M * Vector<Expr>.Parse("[x, y]")).Print());
        Assert.Equal("[[b * c + a^2, a * b + b * d], [a * c + c * d, b * c + d^2]]", (M * M).Print());

        var r = Matrix<Expr>.Parse("[[1, 2], [3, 4]]");
        Assert.Equal("[[7, 10], [15, 22]]", (r * r).Print());
        Assert.Equal("[[1/2, 1], [3/2, 2]]", (r / 2).Print());
        Assert.Equal("x^2 + y^2", Vector<Expr>.Parse("[x, y]").Dot(Vector<Expr>.Parse("[x, y]")).Print());

        var x = new Variable("x");
        Assert.Equal("[[1]]", (Matrix<Expr>.FromRows([x / x]) + Matrix<Expr>.Zero(1, 1)).Print());
    }

    [Fact]
    public void Powers()
    {
        Assert.Equal("[[89, 55], [55, 34]]", Matrix<double>.FromRows([1, 1], [1, 0]).Pow(10).ToString());
        Assert.Equal("[[1, 10x], [0, 1]]", Matrix<Expr>.Parse("[[1, x], [0, 1]]").Pow(10).Print());
        Assert.Equal("[[1, 0], [0, 1]]", Matrix<Expr>.Parse("[[0, -1], [1, 0]]").Pow(4).Print());
        Assert.Equal(Matrix<double>.Identity(2), Matrix<double>.FromRows([0, -1], [1, 0]).Pow(int.MaxValue - 3));

        Assert.Equal("[[1, -0.25], [-1, 0.5]]", N.Pow(-1).ToString());
        Assert.True(N.Pow(-1) == N.Inverse());
        Assert.Equal("[[11/2, -5/2], [-15/4, 7/4]]", Q.Pow(-2).ToString());
        Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2], [2, 4]).Pow(-1));
    }

    [Fact]
    public void Entry_wise_product_and_trace()
    {
        Assert.Equal("[[1, 4, 9], [16, 25, 36]]", A.Hadamard(A).ToString());
        Assert.Equal("[[x, 2y], [3z, 4w]]", Matrix<Expr>.Parse("[[1, 2], [3, 4]]").Hadamard(Matrix<Expr>.Parse("[[x, y], [z, w]]")).Print());
        Assert.Equal("a + d", M.Trace().Print());
        Assert.Equal(6, N.Trace());
    }

    [Fact]
    public void Sizes_that_dont_fit()
    {
        Assert.Equal("Cannot multiply a 2 x 3 matrix by a 2 x 3 matrix: 3 columns on the left, 2 rows on the right.",
            Assert.Throws<ArgumentException>(() => A * A).Message);
        Vector<double> v = [1, 2, 3];
        Assert.Equal("Cannot add vectors of different lengths: 3 and 2.",
            Assert.Throws<ArgumentException>(() => v + Vector.Create(1.0, 2.0)).Message);
        Assert.Equal("The trace needs a square matrix, not a 2 x 3 matrix.",
            Assert.Throws<InvalidOperationException>(() => A.Trace()).Message);
    }

    [Fact]
    public void Norms()
    {
        Assert.Equal(2, Matrix<double>.FromRows([1, 1], [1, 1]).FrobeniusNorm());
        Assert.Equal("sqrt(a^2 + b^2 + c^2 + d^2)", M.FrobeniusNorm().Print());
        Assert.Equal(5e300, Matrix<double>.FromRows([3e300], [4e300]).FrobeniusNorm(), 1e286);
    }

    [Fact]
    public void Approximate_equality()
    {
        double cos = Math.Cos(Math.PI / 2), sin = Math.Sin(Math.PI / 2);
        var turn = Matrix<double>.FromRows([cos, -sin], [sin, cos]);
        var exact = Matrix<double>.FromRows([0, -1], [1, 0]);

        Assert.Equal(6.123233995736766E-17, cos);
        Assert.False(turn == exact);
        Assert.True(turn.IsApproximately(exact));
        Assert.True(Vector.Create(0.1 + 0.2).IsApproximately([0.3]));

        var one = Matrix<double>.FromRows([1.0]);
        Assert.False(one.IsApproximately(Matrix<double>.FromRows([1.001])));
        Assert.True(one.IsApproximately(Matrix<double>.FromRows([1.001]), relativeTolerance: 1e-2));

        var h = Matrix<double>.FromRows([1, 2], [3, 4]);
        var residual = h * h.Inverse() - Matrix<double>.Identity(2);
        Assert.False(residual.IsApproximately(Matrix<double>.Zero(2, 2)));
        Assert.True(residual.IsApproximately(Matrix<double>.Zero(2, 2), absoluteTolerance: 1e-12));

        Assert.False(h.IsApproximately(Matrix<double>.Zero(2, 3)));
        Assert.True(Matrix<double>.FromRows([double.PositiveInfinity]).IsApproximately(Matrix<double>.FromRows([double.PositiveInfinity])));
        Assert.False(Matrix<double>.FromRows([double.NaN]).IsApproximately(Matrix<double>.FromRows([double.NaN])));
    }
}

// Every example in Epsilon.LinearAlgebra/docs/vector-geometry.md, one test per section.
public class LinearAlgebraVectorGeometryDocTests
{
    [Fact]
    public void With_doubles()
    {
        Vector<double> u = [3, 4];
        Assert.Equal(5, u.Norm());
        Assert.Equal("[0.6, 0.8]", u.Normalize().ToString());
        Assert.Equal(5, Vector.Create<double>(1, 2).Distance([4, 6]));
        Assert.Equal("[0, 0, 1]", Vector.Create<double>(1, 0, 0).Cross([0, 1, 0]).ToString());
        Assert.Equal(0.7853981633974483, Vector.Create<double>(1, 1).Angle([1, 0]));
        Assert.Equal("[2.5, 2.5]", Vector.Create<double>(2, 3).ProjectOnto([1, 1]).ToString());
        Assert.Equal("[1, 1]", Vector.Create<double>(1, -1).Reflect([0, 1]).ToString());
        Assert.Equal("[2.5, 12.5]", Vector<double>.Lerp([0, 10], [10, 20], 0.25).ToString());
        Assert.Equal("[[3, 4], [6, 8]]", Vector.Create<double>(1, 2).Outer([3, 4]).ToString());
        Assert.Equal("[15, 25]", Vector<double>.Lerp([0, 10], [10, 20], 1.5).ToString());
        Assert.Throws<ArgumentException>(() => Vector.Create<double>(1, 2).Dot([1, 2, 3]));

        Vector<double> p1 = [1, 0, 0], p2 = [0, 2, 0], p3 = [0, 0, 3];
        var normal = (p2 - p1).Cross(p3 - p1);
        Assert.Equal("[6, 3, 2]", normal.ToString());
        Assert.Equal(3.5, normal.Norm() / 2);

        var e1 = Vector.Create<double>(1, 1, 0).Normalize();
        Vector<double> v = [1, 0, 1];
        var e2 = (v - v.ProjectOnto(e1)).Normalize();
        Assert.Equal("[0.4082482904638631, -0.4082482904638631, 0.8164965809277261]", e2.ToString());
        Assert.Equal(0, e1.Dot(e2));
    }

    [Fact]
    public void With_expressions()
    {
        Assert.Equal("[sqrt(2) / 2, sqrt(2) / 2]", Vector<Expr>.Parse("[1, 1]").Normalize().Print());
        Assert.Equal("π / 4", Vector<Expr>.Parse("[1, 0]").Angle(Vector<Expr>.Parse("[1, 1]")).Print());
        Assert.Equal("sqrt(a^2 + b^2)", Vector<Expr>.Parse("[a, b]").Norm().Print());
        Assert.Equal("[b * z - c * y, c * x - a * z, a * y - b * x]",
            Vector<Expr>.Parse("[a, b, c]").Cross(Vector<Expr>.Parse("[x, y, z]")).Print());
        Assert.Equal("[(x + y) / 2, (x + y) / 2]", Vector<Expr>.Parse("[x, y]").ProjectOnto(Vector<Expr>.Parse("[1, 1]")).Print());
        Assert.Equal("[x, -y]", Vector<Expr>.Parse("[x, y]").Reflect(Vector<Expr>.Parse("[0, 1]")).Print());
    }

    [Fact]
    public void With_rationals()
    {
        Vector<Rational> r = [1, 2];
        Assert.Equal(5, r.NormSquared());
        Assert.Equal("[3/2, 3/2]", r.ProjectOnto([1, 1]).ToString());
        Assert.Equal("[-2, -1]", r.Reflect([1, 1]).ToString());
        Assert.Equal("[1/3, 1]", Vector<Rational>.Lerp([0, 0], [1, 3], new Rational(1, 3)).ToString());
    }

    [Fact]
    public void At_the_extremes_of_floating_point()
    {
        Assert.Equal(5e300, Vector.Create(3e300, 4e300).Norm());
        Assert.Equal("[0.7071067811865476, 0.7071067811865476]", Vector.Create(1e-300, 1e-300).Normalize().ToString());
        Assert.Equal(1e-10, Vector.Create<double>(1, 0).Angle([1, 1e-10]));
        Assert.Equal(0, Math.Acos(1 / Math.Sqrt(1 + 1e-20)));
    }

    [Fact]
    public void The_zero_vector()
    {
        Vector<double> v = [1, 2];
        Assert.Equal("Cannot normalize the zero vector.",
            Assert.Throws<InvalidOperationException>(() => Vector<double>.Zero(2).Normalize()).Message);
        Assert.StartsWith("The angle with the zero vector is undefined.",
            Assert.Throws<ArgumentException>(() => v.Angle(Vector<double>.Zero(2))).Message);
        Assert.StartsWith("Cannot project onto the zero vector.",
            Assert.Throws<ArgumentException>(() => v.ProjectOnto(Vector<double>.Zero(2))).Message);
        Assert.StartsWith("The normal is the zero vector.",
            Assert.Throws<ArgumentException>(() => v.Reflect(Vector<double>.Zero(2))).Message);
        Assert.Equal("The cross product needs vectors of length 3, not 2.",
            Assert.Throws<InvalidOperationException>(() => v.Cross([0, 1])).Message);
    }
}

// Every example in Epsilon.LinearAlgebra/docs/symbolic-matrices.md, one test per section.
public class LinearAlgebraSymbolicMatricesDocTests
{
    private static readonly Matrix<Expr> M = Matrix<Expr>.Parse("[[a, b], [c, d]]");
    private static readonly Matrix<Expr> R = Matrix<Expr>.Parse("[[1, 2], [3, 4]]");
    private static readonly Matrix<Expr> Rotation = Matrix<Expr>.Parse("[[cos(x), -sin(x)], [sin(x), cos(x)]]");

    [Fact]
    public void Parsing_and_printing()
    {
        Assert.Equal("[[1 / 2, x + x]]", Matrix<Expr>.Parse("[[1/2, x + x]]").Print());
        Assert.Equal("[[1/2, 2x]]", Matrix<Expr>.Parse("[[1/2, x + x]]").Simplify().Print());
        Assert.Equal("[[theta, 2t]]", Matrix<Expr>.Parse("[[theta, 2t]]", ["theta", "t"]).Print());
        Assert.Equal(2, Vector<Expr>.Parse("[min(a, b), log(x, 2)]").Length);
        Assert.Equal(["x"], Vector<Expr>.Parse("[e * x, i, pi]").GetVariables());

        var simplified = Matrix<Expr>.Parse("[[1/2, x^2], [sin(x), -y]]").Simplify();
        Assert.Equal(simplified, Matrix<Expr>.Parse(simplified.Print()).Simplify());

        Assert.Equal(@"\begin{bmatrix} \cos\left(x\right) & -\sin\left(x\right) \\ \sin\left(x\right) & \cos\left(x\right) \end{bmatrix}",
            Rotation.ToLatex());
        Assert.Equal(@"\begin{bmatrix} x^{2} \\ \frac{1}{2} \end{bmatrix}", Vector<Expr>.Parse("[x^2, 1/2]").Simplify().ToLatex());
    }

    [Theory]
    [InlineData("[[1, 2], [3]]", "Row 1 has 1 entries, but row 0 has 2.")]
    [InlineData("[[1, 2], [3, 4]", "Expected ']' at position 15, found the end of the text.")]
    [InlineData("[[1]] x", "Unexpected 'x' at position 6 after the closing ']'.")]
    [InlineData("[[1, sen(x)]]", "Invalid entry at (0, 1), starting at position 5: Unknown identifier 'sen' at position 0. Declared variables: x.")]
    public void Parse_errors(string text, string message) =>
        Assert.Equal(message, Assert.Throws<FormatException>(() => Matrix<Expr>.Parse(text, ["x"])).Message);

    [Fact]
    public void Determinant_and_characteristic_polynomial()
    {
        Assert.Equal("a * d - b * c", M.Determinant().Print());
        Assert.Equal("-4", Matrix<Expr>.Parse("[[x + 1, x - 1], [x - 1, x - 3]]").Determinant().Print());
        Assert.Equal("1", Rotation.Determinant().Print());

        var p = Matrix<Expr>.Parse("[[2, 1], [1, 2]]").CharacteristicPolynomial("t");
        Assert.Equal("t^2 - 4t + 3", p.Print());
        Assert.Equal([1.0, 3.0], p.FindRealRoots());
        Assert.Throws<ArgumentException>(() => M.CharacteristicPolynomial("a"));

        var s = Matrix<Expr>.Parse("[[2, 0, 0], [0, 3, 4], [0, 4, 9]]");
        var cp = s.CharacteristicPolynomial("t");
        Assert.Equal("t^3 - 14t^2 + 35t - 22", cp.Print());
        Assert.Equal("(t - 1) * (t - 2) * (t - 11)", cp.TryFactorReal("t").Factored.Print());
        Assert.Equal([1, 1.9999999999999993, 11], cp.FindRealRoots());
    }

    [Fact]
    public void Adjugate_inverse_and_solve()
    {
        Assert.Equal("[[d, -b], [-c, a]]", M.Adjugate().Print());
        Assert.Equal("[[-2, 1], [3/2, -1/2]]", R.Inverse().Print());
        Assert.Equal("[[cos(x), sin(x)], [-sin(x), cos(x)]]", Rotation.Inverse().Print());
        Assert.Equal("[(d * p - b * q) / (a * d - b * c), (a * q - c * p) / (a * d - b * c)]",
            M.Solve(Vector<Expr>.Parse("[p, q]")).Print());
        Assert.Equal(M.Inverse(), M.Solve(Matrix<Expr>.Identity(2)));

        var inverse = Matrix<Expr>.Parse("[[x, 1], [1, x]]").Inverse();
        Assert.Equal("[[x / (x^2 - 1), -1 / (x^2 - 1)], [-1 / (x^2 - 1), x / (x^2 - 1)]]", inverse.Print());
        Assert.Equal("[[0, 1], [1, 0]]", inverse.Substitute("x", 0).Simplify().Print());
        Assert.Equal("[[1 / 0, -1 / 0], [-1 / 0, 1 / 0]]", inverse.Substitute("x", 1).Simplify().Print());
        Assert.Equal("(x + 1) * (x - 1)", Matrix<Expr>.Parse("[[x, 1], [1, x]]").Determinant().TryFactorReal("x").Factored.Print());

        Assert.Equal("The matrix is singular: its determinant is 0.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[x, 2x], [1, 2]]").Inverse()).Message);
    }

    [Fact]
    public void Entry_wise_operations()
    {
        Assert.Equal("[[x^2 + 2x + 1, x^2 - 1]]", Matrix<Expr>.Parse("[[(x + 1)^2, (x - 1)*(x + 1)]]").Expand().Print());
        Assert.Equal("[[-sin(x), -cos(x)], [cos(x), -sin(x)]]", Rotation.Differentiate("x").Print());
        Assert.Equal("[[2, b], [c, d]]", M.Substitute("a", 2).Print());
        Assert.Equal("a, b, c, d", string.Join(", ", M.GetVariables().Order()));
        Assert.Equal("[[1, 2], [3, 4]]", M.Evaluate(("a", 1), ("b", 2), ("c", 3), ("d", 4)).ToString());
        Assert.Equal("[[3, 9]]", Matrix<Expr>.Parse("[[x, x^2]]").Evaluate(3).ToString());

        Assert.Equal("Expected at most 1 variable, found 2: [x, y]. Use the overload with named values for several variables.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[x, y]]").Evaluate(3)).Message);

        var roots = Matrix<Expr>.Parse("[[sqrt(x^2), abs(x)]]");
        Assert.Equal("[[abs(x), abs(x)]]", roots.Simplify().Print());
        Assert.Equal("[[x, x]]", roots.Simplify(Assumptions.None.AssumePositive("x")).Print());
    }
}

// Every example in Epsilon.LinearAlgebra/docs/numeric-matrices.md, one test per section.
public class LinearAlgebraNumericMatricesDocTests
{
    private static readonly Matrix<double> N = Matrix<double>.FromRows([2, 1], [4, 4]);

    [Fact]
    public void Determinant_inverse_and_systems()
    {
        Assert.Equal(4, N.Determinant());
        Assert.Equal("[1, 2]", N.Solve([4, 12]).ToString());
        Assert.Equal("[[1, 1], [2, -1]]", N.Solve(Matrix<double>.FromRows([4, 1], [12, 0])).ToString());
        Assert.Equal("[[1, -0.25], [-1, 0.5]]", N.Inverse().ToString());
        Assert.Equal(2, N.Rank());

        var powers = Matrix<double>.Create(3, 3, (i, j) => Math.Pow(i + 1, j));
        Assert.Equal("[[1, 1, 1], [1, 2, 4], [1, 3, 9]]", powers.ToString());
        Assert.Equal("[3, -2, 1]", powers.Solve([2, 3, 6]).ToString());
    }

    [Fact]
    public void Rank()
    {
        Assert.Equal(2, Matrix<double>.FromRows([1, 2], [3, 4], [5, 6]).Rank());
        Assert.Equal(2, Matrix<double>.FromRows([1, 2, 3], [2, 4, 6], [1, 1, 1]).Rank());
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

        var tiny = 1e-200 * Matrix<double>.Identity(2);
        Assert.Equal(0, tiny.Determinant());
        Assert.Equal(2, tiny.Rank());
        Assert.Equal("[[1E+200, 0], [0, 1E+200]]", tiny.Inverse().ToString());

        var hilbert = Matrix<double>.Create(12, 12, (i, j) => 1.0 / (i + j + 1));
        Assert.Equal(11, hilbert.Rank());
        Assert.Equal(1.8e16, hilbert.ConditionNumber(), 0.05e16);
        Assert.Equal("The matrix is singular: its rank is 11, not 12, to working precision.",
            Assert.Throws<InvalidOperationException>(() => hilbert.Inverse()).Message);
    }

    [Fact]
    public void Errors()
    {
        Assert.Equal("The inverse needs a square matrix, not a 2 x 3 matrix.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]).Inverse()).Message);
        Assert.StartsWith("The right-hand side has 3 entries, but the matrix has 2 rows.",
            Assert.Throws<ArgumentException>(() => N.Solve([1, 2, 3])).Message);
        Assert.Equal("The matrix has a non-finite entry at (0, 1): NaN.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, double.NaN], [3, 4]).Determinant()).Message);
        Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, double.PositiveInfinity]).Rank());
        Assert.Equal("Solve needs a square matrix, not a 2 x 3 matrix. LeastSquares takes any shape.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]).Solve([1, 2])).Message);
    }
}

// Every example in Epsilon.LinearAlgebra/docs/rational-matrices.md, one test per section.
public class LinearAlgebraRationalMatricesDocTests
{
    private static readonly Matrix<Rational> Q = Matrix<Rational>.FromRows([1, 2], [3, 4]);

    [Fact]
    public void Determinant_inverse_and_systems()
    {
        Assert.Equal(-2, Q.Determinant());
        Assert.Equal("[[-2, 1], [3/2, -1/2]]", Q.Inverse().ToString());
        Assert.Equal("[-4, 9/2]", Q.Solve([5, 6]).ToString());
        Assert.Equal("[[1/3, 2/3], [1, 4/3]]", (Q / 3).ToString());
        Assert.Equal("[[37, 54], [81, 118]]", Q.Pow(3).ToString());
        Assert.Equal(5, Q.Trace());

        var singular = Matrix<Rational>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
        Assert.Equal(2, singular.Rank());
        Assert.Equal("The matrix is singular: its rank is 2, not 3.",
            Assert.Throws<InvalidOperationException>(() => singular.Inverse()).Message);
        Assert.Equal("Cannot divide a matrix by 0.", Assert.Throws<DivideByZeroException>(() => Q / 0).Message);
    }

    [Fact]
    public void Ill_conditioned_matrices()
    {
        var hilbert = Matrix<Rational>.Create(8, 8, (i, j) => new Rational(1, i + j + 1));
        Assert.Equal(176679360, hilbert.Inverse()[7, 7]);
        Assert.True(hilbert * hilbert.Inverse() == Matrix<Rational>.Identity(8));
        Assert.Equal(176679357.49307457, hilbert.Map(r => r.ToDouble()).Inverse()[7, 7]);

        var hilbert12 = Matrix<Rational>.Create(12, 12, (i, j) => new Rational(1, i + j + 1));
        Assert.Equal(12, hilbert12.Rank());
        Assert.Equal(2.637780651253548E-78, hilbert12.Determinant().ToDouble());
        Assert.Equal(11445589052352, hilbert12.Inverse()[11, 11]);
    }

    [Fact]
    public void Conversions()
    {
        var d = Matrix<double>.FromRows([0.1, 0.2], [0.3, 0.4]);
        Assert.Equal("[[-20.000000000000007, 10.000000000000004], [15.000000000000004, -5.000000000000002]]", d.Inverse().ToString());
        Assert.Equal("[[-20, 10], [15, -5]]", d.Map(Rational.FromDouble).Inverse().ToString());
        Assert.Equal("[[-2, 1], [1.5, -0.5]]", Q.Inverse().Map(r => r.ToDouble()).ToString());
        Assert.Equal("t^2 - 5t - 2", Q.Map(r => (Expr)r).CharacteristicPolynomial("t").Print());
    }

    [Fact]
    public void Fixed_size_types_of_rationals()
    {
        Vector3<Rational> a = (new Rational(1, 2), new Rational(1, 3), 1);
        Assert.Equal("25/6", a.Dot((1, 2, 3)).ToString());

        var m = new Matrix3x3<Rational>(2, 1, 0, 1, 2, 1, 0, 1, 2);
        Assert.Equal("[[3/4, -1/2, 1/4], [-1/2, 1, -1/2], [1/4, -1/2, 3/4]]", ((Matrix3x3<Rational>)m.ToMatrix().Inverse()).ToString());
    }
}

// Every example in Epsilon.LinearAlgebra/docs/fast-code.md, one test per section.
public class LinearAlgebraFastCodeDocTests
{
    [Fact]
    public void Compiled_evaluation()
    {
        var helix = Vector<Expr>.Parse("[cos(t), sin(t), t / 10]", ["t"]);
        Func<double, Vector<double>> point = helix.Compile();
        Assert.Equal("[-1, 1.2246467991473532E-16, 0.3141592653589793]", point(Math.PI).ToString());

        var table = Matrix<Expr>.Parse("[[x + y, x * y], [x - y, x / y]]");
        Assert.Equal("[[9, 18], [3, 2]]", table.Compile("x", "y")(6, 3).ToString());

        var swirl = Vector<Expr>.Parse("[-y, x, z]");
        Assert.Equal("[-2, 1, 3]", swirl.Compile("x", "y", "z")(1, 2, 3).ToString());

        var entries = new double[4];
        table.CompileEntries(["x", "y"])([6, 3], entries);
        Assert.Equal([9, 18, 3, 2], entries);

        Assert.Throws<ArgumentException>(() => Matrix<Expr>.Parse("[[x, y]]").Compile("x"));
    }

    [Fact]
    public void Jacobian()
    {
        var polar = Vector<Expr>.Parse("[r * cos(t), r * sin(t)]", ["r", "t"]);
        Assert.Equal("[[cos(t), -sin(t) * r], [sin(t), cos(t) * r]]", polar.Jacobian(["r", "t"]).Print());

        Assert.Equal("[[cos(t), -sin(t) * r, 0]]", Vector.Create(polar[0]).Jacobian(["r", "t", "z"]).Print());
    }

    [Fact]
    public void Newtons_method_for_a_system()
    {
        var equations = Vector<Expr>.Parse("[x^2 + y^2 - 4, x * y - 1]");
        string[] unknowns = ["x", "y"];
        Assert.Equal("[[2x, 2y], [y, x]]", equations.Jacobian(unknowns).Print());

        Func<double[], Vector<double>> f = equations.Compile(unknowns);
        Func<double[], Matrix<double>> jacobian = equations.Jacobian(unknowns).Compile(unknowns);

        Vector<double> guess = [2, 0];
        int steps = 0;
        for (int step = 0; step < 20; step++)
        {
            double[] values = guess.ToArray();
            Vector<double> residual = f(values);
            if (residual.Norm() < 1e-12)
                break;
            guess -= jacobian(values).Solve(residual);
            steps++;
        }

        Assert.Equal(5, steps);
        Assert.Equal("[1.9318516525781364, 0.5176380902050416]", guess.ToString());
        Assert.True(guess.IsApproximately([Math.Sqrt(2 + Math.Sqrt(3)), Math.Sqrt(2 - Math.Sqrt(3))], 1e-15));

        Assert.Throws<InvalidOperationException>(() => jacobian([0, 0]).Solve(f([0, 0])));
    }

    [Fact]
    public void Symbolic_transforms()
    {
        var quarterTurn = Matrix<Expr>.Rotation(ExprParser.Parse("pi / 2"));
        Assert.Equal("[[0, -1], [1, 0]]", quarterTurn.Print());
        var move = Matrix<Expr>.Translation(Vector<Expr>.Parse("[a, b]"));
        Assert.Equal("[[1, 0, a], [0, 1, b], [0, 0, 1]]", move.Print());

        var turnThenMove = move * quarterTurn.ToHomogeneous();
        Assert.Equal("[-y + a, b + x]", turnThenMove.TransformPoint(Vector<Expr>.Parse("[x, y]")).Print());
        Assert.Equal("[-y, x]", turnThenMove.TransformDirection(Vector<Expr>.Parse("[x, y]")).Print());

        Assert.Equal("[[s, 0, 0], [0, s, 0], [0, 0, 1]]", Matrix<Expr>.FromDiagonal(Vector<Expr>.Parse("[s, s]")).ToHomogeneous().Print());
    }

    [Fact]
    public void CSharp_source()
    {
        Assert.Equal(
            """
            public static void Rotation(double t, Span<double> result)
            {
                double t0 = Math.Cos(t);
                result[0] = t0;
                double t1 = Math.Sin(t);
                result[1] = -t1;
                result[2] = t1;
                result[3] = t0;
            }
            """.ReplaceLineEndings("\n"),
            Matrix<Expr>.Rotation(new Variable("t")).ToCSharp("Rotation", ["t"]));

        Assert.StartsWith("public static void F(double @in,", Vector.Create<Expr>(new Variable("in")).ToCSharp("F", ["in"]));
        Assert.Throws<ArgumentException>(() => Vector<Expr>.Parse("[x]").ToCSharp("2x", ["x"]));
        Assert.Throws<ArgumentException>(() => Vector<Expr>.Parse("[x, y]").ToCSharp("F", ["x"]));
    }

    [Fact]
    public void Inverse_kinematics()
    {
        static Matrix<Expr> Link(string angle, int length) =>
            Matrix<Expr>.Rotation(new Variable(angle)).ToHomogeneous() * Matrix<Expr>.Translation([length, 0]);

        Vector<Expr> tip = (Link("a", 3) * Link("b", 2) * Link("c", 1)).TransformPoint([0, 0]);
        string[] joints = ["a", "b", "c"];
        Func<double[], Vector<double>> position = tip.Compile(joints);
        Func<double[], Matrix<double>> jacobian = tip.Jacobian(joints).Compile(joints);

        Vector<double> angles = [0.3, 0.3, 0.3], target = [4, 2];
        int steps = 0;
        for (int step = 0; step < 100; step++)
        {
            double[] values = angles.ToArray();
            Vector<double> error = target - position(values);
            if (error.Norm() < 1e-9)
                break;

            Matrix<double> j = jacobian(values);
            Vector<double> change = j.Transpose() * (j * j.Transpose() + 0.01 * Matrix<double>.Identity(2)).Solve(error);
            angles += change.Norm() <= 0.5 ? change : change * (0.5 / change.Norm());
            steps++;
        }

        Assert.Equal(6, steps);
        Assert.True(angles.IsApproximately([-0.1828, 1.0812, 0.8694], 1e-4), angles.ToString());
        Assert.True((target - position(angles.ToArray())).Norm() < 1e-10);
        Assert.Equal((2, 3), (jacobian([0, 0, 0]).Rows, jacobian([0, 0, 0]).Columns));

        Assert.Equal(15, tip.ToCSharp("Position", joints).Split('\n').Length);
        Assert.Equal(29, tip.Jacobian(joints).ToCSharp("Jacobian", joints).Split('\n').Length);
    }
}

// Every example in Epsilon.LinearAlgebra/docs/fixed-size.md, one test per section.
public class LinearAlgebraFixedSizeDocTests
{
    private static readonly Mat4 Model = Mat4.Translation((10, 0, 5)) * Mat4.RotationY(Math.PI / 2) * Mat4.Scale(2);

    [Fact]
    public void Entry_types()
    {
        Vector3<int> cell = (7, 8, 9);
        Assert.Equal("[8, 8, 9]", (cell + Vector3<int>.UnitX).ToString());
        Assert.Equal("[3, 4, 4]", (cell / 2).ToString());
        Assert.Equal(25, new Vector2<int>(3, 4).NormSquared());
        Assert.Equal(6, new Matrix3x3<int>(2, 0, 0, 0, 3, 0, 0, 0, 1).Determinant());
    }

    [Fact]
    public void Fixed_size_vectors()
    {
        Vec3 p = (1, 2, 3);
        var q = new Vec3(4, 5, 6);
        var (x, y, z) = p;
        Assert.Equal((1.0, 2.0, 3.0), (x, y, z));
        Assert.Equal("[1, 0, 3]", (p with { Y = 0 }).ToString());
        Assert.Equal(3, p[2]);
        Assert.Equal("[1, 2]", p.XY.ToString());
        Assert.Equal(32, p.Dot(q));
        Assert.Equal("[-3, 6, -3]", p.Cross(q).ToString());
        Assert.Equal("[2, 4, 6]", p.Hadamard((2, 2, 2)).ToString());
        Assert.Equal("[2.5, 3.5, 4.5]", Vec3.Lerp(p, q, 0.5).ToString());
        Assert.Equal(1, new Vector2<double>(1, 0).Cross((0, 1)));
        Assert.Equal("[1, 2, 3]", new Vector4<double>(1, 2, 3, 4).XYZ.ToString());
        Assert.Equal("[1, 2, 3]", new Vec3(new Vector2<double>(1, 2), 3).ToString());

        Vec3 u = (3, 4, 0);
        Assert.Equal(5, u.Norm());
        Assert.Equal("[0.6, 0.8, 0]", u.Normalize().ToString());
        Assert.Equal("[1, 1, 0]", new Vec3(1, -1, 0).Reflect(Vec3.UnitY).ToString());
        Assert.Equal(1.5707963267948966, Vec3.UnitX.Angle(Vec3.UnitY));
        Assert.Equal(Math.PI / 2, Vec3.UnitX.Angle(Vec3.UnitY));
        Assert.Equal("5E+300", new Vec3(3e300, 4e300, 0).Norm().ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal((Half)500, new Vector2<Half>((Half)300, (Half)400).Norm());
        Assert.True(Half.IsPositiveInfinity((Half)300 * (Half)300));
        Assert.Equal("[0, 3, 10]", new Vec3(-5, 3, 12).Clamp(Vec3.Zero, (10, 10, 10)).ToString());

        Vec3[] points = [(1, 5, -2), (4, -1, 0), (-3, 2, 7)];
        Vec3 min = points[0], max = points[0];
        foreach (Vec3 point in points)
        {
            min = min.Min(point);
            max = max.Max(point);
        }
        Assert.Equal("[-3, -1, -2]", min.ToString());
        Assert.Equal("[4, 5, 7]", max.ToString());
        Assert.Equal("[0.5, 2, 2.5]", ((min + max) / 2).ToString());

        Vec3 turned = (Math.Cos(Math.PI / 2), 1, 0);
        Assert.False(turned == Vec3.UnitY);
        Assert.Equal(6.123233995736766E-17, turned.X);
        Assert.True(turned.IsApproximately(Vec3.UnitY));
        Assert.True(new Vector3<float>(1, 0, 0).IsApproximately((1.000001f, 0, 0)));
        Assert.False(new Vec3(1, 0, 0).IsApproximately((1.000001, 0, 0)));

        Assert.Throws<InvalidOperationException>(() => Vec3.Zero.Normalize());
        Assert.Throws<ArgumentException>(() => Vec3.UnitX.ProjectOnto(Vec3.Zero));
    }

    [Fact]
    public void Fixed_size_matrices_and_transforms()
    {
        Assert.Equal("[10, 0, 3]", Model.TransformPoint((1, 0, 0)).ToString());
        Assert.Equal("[2, 0, 1.2246467991473532E-16]", Model.TransformDirection((0, 0, 1)).ToString());
        Assert.Equal(2 * Math.Cos(Math.PI / 2), Model.TransformDirection((0, 0, 1)).Z);
        Assert.True((Mat4.RotationZ(1) * new Vector4<double>(1, 0, 0, 0)).Y > 0);   // UnitX turns toward UnitY

        var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        Assert.Equal(2, m.M12);
        Assert.Equal(6, m[1, 2]);
        Assert.Equal(m.M23, m[1, 2]);
        Assert.Equal("[3, 6, 9]", m.Column(2).ToString());
        Assert.Equal(0, m.Determinant());
        Assert.Equal("[1, 2, 3, 1]", Mat4.Translation((1, 2, 3)).Column(3).ToString());

        var project = Mat4.Identity with { M43 = 1, M44 = 0 };
        Assert.Equal("[1, 2, 1]", project.TransformPoint((2, 4, 2)).ToString());

        var sprite = Matrix3x3<double>.Translation((100, 50)) * Matrix3x3<double>.RotationZ(Math.PI / 2);
        Assert.True(sprite.TransformPoint((1, 0)).IsApproximately((100, 51)));

        var turn = Matrix2x2<double>.Rotation(Math.PI / 2);
        Assert.True((turn * new Vector2<double>(1, 0)).IsApproximately((0, 1)));
        Assert.True(new Matrix3x3<double>(turn) == Matrix3x3<double>.RotationZ(Math.PI / 2));
    }

    [Fact]
    public void The_inverse()
    {
        Assert.True(Model.Inverse().TransformPoint((10, 0, 3)).IsApproximately((1, 0, 0)));
        Assert.False(Mat4.Scale((1, 1, 0)).TryInverse(out _));

        var noisy = new Matrix3x3<double>(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9);
        Assert.Equal(1.734723475976807E-17, noisy.Determinant());
        Assert.False(noisy.TryInverse(out _));
        Assert.Equal("[[1E+20, 0, 0], [0, 1, 0], [0, 0, 1]]", Matrix3x3<double>.Scale((1e-20, 1, 1)).Inverse().ToString());

        Assert.Equal("The matrix is singular to working precision.",
            Assert.Throws<InvalidOperationException>(() => noisy.Inverse()).Message);
        Assert.False((Mat4.Identity with { M11 = double.NaN }).TryInverse(out _));
    }

    [Fact]
    public void Conversions_and_System_Numerics()
    {
        Vec3 p = (1, 2, 3);
        var q = new Vec3(4, 5, 6);

        Vector3<float> f = p.Map(float.CreateTruncating);
        Assert.Equal(new Vector3<float>(1, 2, 3), f);
        Vector3<int> c = new Vec3(1.5, -2.7, 3).Map(x => (int)Math.Floor(x));
        Assert.Equal("[1, -3, 3]", c.ToString());
        Assert.Equal(4, Model.ToMatrix().Rank());
        Assert.Equal((3, 3), (p.ToVector().Outer(q.ToVector()).Rows, p.ToVector().Outer(q.ToVector()).Columns));
        Assert.True((Mat4)Matrix<double>.Identity(4) == Mat4.Identity);

        var numerics = Matrix4x4<float>.Translation((1, 2, 3)).ToSystemNumerics();
        Assert.Equal(3, numerics.M43);
        Assert.True(Matrix4x4<float>.FromSystemNumerics(numerics) == Matrix4x4<float>.Translation((1, 2, 3)));

        Vec3[] corners = [(0, 0, 0), (1, 0, 0)];
        var moved = new Vec3[2];
        Mat4.Translation((1, 2, 3)).TransformPoints(corners, moved);
        Assert.Equal("[1, 2, 3], [2, 2, 3]", string.Join(", ", moved));
        Mat4.Translation((1, 2, 3)).TransformDirections(corners, moved);
        Assert.Equal("[0, 0, 0], [1, 0, 0]", string.Join(", ", moved));

        float[] buffer = [1, 2, 3, 4, 5, 6];
        var vertices = System.Runtime.InteropServices.MemoryMarshal.Cast<float, Vector3<float>>(buffer.AsSpan());
        Matrix4x4<float>.Translation((1, 0, 0)).TransformPoints(vertices, vertices);
        Assert.Equal(new float[] { 2, 2, 3, 5, 5, 6 }, buffer);
    }
}

// Every example in Epsilon.LinearAlgebra/docs/rotations.md, one test per section.
public class LinearAlgebraRotationsDocTests
{
    private static readonly Quaternion<double> Quarter = Quaternion<double>.RotationY(Math.PI / 2);

    [Fact]
    public void Quaternions()
    {
        Assert.True(Quarter.Rotate((1, 0, 0)).IsApproximately((0, 0, -1)));
        var half = Quarter * Quarter;
        var (axis, angle) = half.ToAxisAngle();
        Assert.Equal("[0, 1, 0]", axis.ToString());
        Assert.Equal(3.1415926535897927, angle);
        Assert.Equal(0, Quarter.Angle(-Quarter));
        Assert.Equal(new Mat4(Matrix3x3<double>.Rotation(Quarter)), Mat4.Rotation(Quarter));
        Assert.Equal(0, Quaternion<double>.FromRotationMatrix(Mat4.Rotation(Quarter)).Angle(Quarter));
        Assert.True(Quaternion<double>.FromToRotation(Vec3.UnitX, (0, 0, -1)).IsApproximately(Quarter));

        Assert.Equal("[0, 0, 0, 1]", (Quarter * Quarter.Inverse()).ToString());
        Assert.True(Quarter.Inverse().Rotate(Quarter.Rotate((1, 2, 3))).IsApproximately((1, 2, 3)));
        Assert.Equal(Quarter.Conjugate(), Quarter.Inverse());

        Assert.True((3 * Quarter).Rotate((1, 0, 0)).IsApproximately(Quarter.Rotate((1, 0, 0))));
        Assert.False(Quarter.IsApproximately(-Quarter));
    }

    [Fact]
    public void Interpolation()
    {
        var turn = Quaternion<double>.RotationZ(Math.PI / 2);
        var identity = Quaternion<double>.Identity;
        var (axis, angle) = Quaternion<double>.Slerp(identity, turn, 0.5).ToAxisAngle();
        Assert.Equal("[0, 0, 1]", axis.ToString());
        Assert.Equal(0.7853981633974483, angle);
        Assert.Equal(0.3926990816987241, Quaternion<double>.Slerp(identity, turn, 0.25).Angle(identity));
        Assert.Equal(Math.PI / 8, Quaternion<double>.Slerp(identity, turn, 0.25).Angle(identity), 1e-15);
        Assert.Equal(0.3769590215412104, Quaternion<double>.Nlerp(identity, turn, 0.25).Angle(identity));

        Assert.Equal(0, Quaternion<double>.Slerp(turn, -turn, 0.5).Angle(turn));
        Assert.Equal(0, Quaternion<double>.Nlerp(turn, -turn, 0.5).Angle(turn));
    }

    [Fact]
    public void Euler_angles()
    {
        Vec3 angles = (0.1, 0.5, 0);
        var q = Quaternion<double>.FromEulerAngles(angles, EulerOrder.ZXY);
        Assert.True(q.ToEulerAngles(EulerOrder.ZXY).IsApproximately(angles));
        Assert.True(Mat4.FromEulerAngles(angles, EulerOrder.ZXY).IsApproximately(Mat4.Rotation(q)));
        Assert.True(Matrix3x3<double>.FromEulerAngles((0.1, 0.2, 0.3), EulerOrder.XYZ)
            == Matrix3x3<double>.RotationZ(0.3) * Matrix3x3<double>.RotationY(0.2) * Matrix3x3<double>.RotationX(0.1));
        Assert.Equal(0, Mat4.FromEulerAngles((0.4, Math.PI / 2, 1.3), EulerOrder.XYZ).ToEulerAngles(EulerOrder.XYZ).Z);
        Assert.True(Quaternion<float>.FromSystemNumerics(System.Numerics.Quaternion.CreateFromYawPitchRoll(0.5f, 0.1f, 0.2f))
            .IsApproximately(Quaternion<float>.FromEulerAngles((0.1f, 0.5f, 0.2f), EulerOrder.ZXY)));
    }

    [Fact]
    public void Translation_rotation_and_scale()
    {
        var model = Mat4.Compose((10, 0, 5), Quarter, (2, 2, 2));
        Assert.True(model.TryDecompose(out var position, out var rotation, out var scale));
        Assert.Equal("[10, 0, 5]", position.ToString());
        Assert.Equal("[2, 2, 2]", scale.ToString());
        Assert.Equal(0, rotation.Angle(Quarter));

        Assert.True(Mat4.Scale((-1, 1, 1)).TryDecompose(out _, out _, out var mirrored));
        Assert.Equal("[-1, 1, 1]", mirrored.ToString());
        Assert.False((Mat4.Identity with { M12 = 0.5 }).TryDecompose(out _, out _, out _));

        var sprite = Matrix3x3<double>.Compose((100, 50), Math.PI / 6, (2, 2));
        Assert.True(sprite.TryDecompose(out var at, out var angle, out var size));
        Assert.Equal("[100, 50]", at.ToString());
        Assert.Equal(0.5235987755982988, angle);
        Assert.Equal(Math.PI / 6, angle);
        Assert.Equal("[2, 2]", size.ToString());
    }

    [Fact]
    public void Normals()
    {
        var stretch = Mat4.Scale((2, 1, 1));
        Assert.Equal("[0.5, -1, 0]", stretch.TransformNormal((1, -1, 0)).ToString());
        Assert.Equal("[2, -1, 0]", stretch.TransformDirection((1, -1, 0)).ToString());
        Assert.Equal(0, stretch.TransformDirection((1, 1, 0)).Dot(stretch.TransformNormal((1, -1, 0))));
        Assert.NotEqual(0, stretch.TransformDirection((1, 1, 0)).Dot(stretch.TransformDirection((1, -1, 0))));
        Assert.Equal("[[0.5, 0, 0], [0, 1, 0], [0, 0, 1]]", stretch.NormalMatrix().ToString());

        Vec3[] normals = [(1, -1, 0), (0, 0, 1)];
        stretch.TransformNormals(normals, normals);
        Assert.Equal("[0.5, -1, 0], [0, 0, 1]", string.Join(", ", normals));
    }
}

// Every example in Epsilon.LinearAlgebra/docs/camera.md, one test per section.
public class LinearAlgebraCameraDocTests
{
    private static readonly Mat4 View = Mat4.LookAt((0, 2, 5), (0, 0, 0), Vec3.UnitY);
    private static readonly Mat4 Projection = Mat4.Perspective(Math.PI / 3, 16.0 / 9, 0.1, 100);

    [Fact]
    public void View_and_perspective()
    {
        Assert.Equal("[0, 0, 0]", View.TransformPoint((0, 2, 5)).ToString());
        Assert.Equal("[0, 0, 0]", Projection.TransformPoint((0, 0, -0.1)).ToString());
        Assert.Equal(0.9824128790613561, (Projection * View).TransformPoint((0, 0, 0)).Z);

        var leftHanded = Mat4.PerspectiveLeftHanded(Math.PI / 3, 16.0 / 9, 0.1, 100);
        Assert.Equal("[0, 0, 0]", leftHanded.TransformPoint((0, 0, 0.1)).ToString());

        Assert.Throws<ArgumentException>(() => Mat4.LookAt((1, 2, 3), (1, 2, 3), Vec3.UnitY));
        Assert.Throws<ArgumentException>(() => Mat4.LookAt((0, 5, 0), (0, 0, 0), Vec3.UnitY));

        var openGL = Mat4.Perspective(Math.PI / 3, 16.0 / 9, 0.1, 100, DepthRange.MinusOneToOne);
        Assert.Equal(1, openGL.TransformPoint((0, 0, -100)).Z);
        Assert.Equal(-1, openGL.TransformPoint((0, 0, -0.1)).Z, 1e-15);

        var reversed = Mat4.Perspective(Math.PI / 3, 16.0 / 9, 0.1, double.PositiveInfinity, DepthRange.OneToZero);
        Assert.Equal(0.0001, reversed.TransformPoint((0, 0, -1000)).Z);
    }

    [Fact]
    public void Orthographic_projection()
    {
        var map = Mat4.Orthographic(20, 10, 0.1, 100);
        Assert.Equal("[1, 1, 0]", map.TransformPoint((10, 5, -0.1)).ToString());

        var ui = Mat4.OrthographicOffCenter(0, 1920, 1080, 0, -1, 1);
        Assert.Equal("[-1, 1, 0.5]", ui.TransformPoint((0, 0, 0)).ToString());
        Assert.Equal("[1, -1, 0.5]", ui.TransformPoint((1920, 1080, 0)).ToString());
    }

    [Fact]
    public void A_first_person_camera()
    {
        // The example with a frame's input: the mouse a little to the left and up, W held.
        double yaw = 0, pitch = 0, sensitivity = 0.002, speed = 5, deltaTime = 0.1, aspect = 16.0 / 9;
        double mouseDeltaX = -785.398, mouseDeltaY = -100, moveForward = 1, moveRight = 0;
        Vec3 position = (0, 1.7, 5);

        yaw -= mouseDeltaX * sensitivity;
        pitch = Math.Clamp(pitch - mouseDeltaY * sensitivity, -1.55, 1.55);
        var orientation = Quaternion<double>.RotationY(yaw) * Quaternion<double>.RotationX(pitch);
        Vec3 forward = orientation.Rotate(-Vec3.UnitZ), right = orientation.Rotate(Vec3.UnitX);
        position += (forward * moveForward + right * moveRight) * (speed * deltaTime);

        var viewProjection = Mat4.Perspective(Math.PI / 3, aspect, 0.1, 1000)
                           * Mat4.LookAt(position, position + forward, Vec3.UnitY);
        System.Numerics.Matrix4x4 forTheGpu = viewProjection.Map(float.CreateTruncating).ToSystemNumerics();

        Assert.Equal(1.570796, yaw, 1e-6);         // a quarter turn to the left
        Assert.Equal(0.2, pitch, 1e-12);           // looking up a little
        Assert.True(forward.X < -0.9 && forward.Y > 0.1);
        Assert.True(position.Distance((0, 1.7, 5)) - 0.5 is < 1e-12 and > -1e-12);

        // A point straight ahead lands in the middle of the screen.
        Vec3 ahead = position + 10 * forward;
        Vector3<float> screen = Matrix4x4<float>.FromSystemNumerics(forTheGpu).TransformPoint(ahead.Map(float.CreateTruncating));
        Assert.Equal(0, screen.X, 1e-5f);
        Assert.Equal(0, screen.Y, 1e-5f);
    }

    [Fact]
    public void An_orbit_camera()
    {
        double yaw = Math.PI / 2, pitch = 0.5, distance = 10;
        Vec3 target = (0, 1, 0);
        var orbit = Quaternion<double>.RotationY(yaw) * Quaternion<double>.RotationX(-pitch);
        Vec3 eye = target + orbit.Rotate((0, 0, distance));
        Assert.True(eye.Y > target.Y);
        Assert.Equal(10, eye.Distance(target), 1e-12);
        Assert.Equal("[0, 0, -10]", Mat4.LookAt(eye, target, Vec3.UnitY).TransformPoint(target).ToString());
    }

    [Theory]
    [InlineData(960, 540, 0, 0)]
    [InlineData(1920, 1080, 2.26, 3.43)]
    public void Picking_with_the_mouse(double mouseX, double mouseY, double hitX, double hitZ)
    {
        double width = 1920, height = 1080;
        var viewProjection = Projection * View;
        double ndcX = 2 * mouseX / width - 1, ndcY = 1 - 2 * mouseY / height;
        var unproject = viewProjection.Inverse();
        Vec3 near = unproject.TransformPoint((ndcX, ndcY, 0)), far = unproject.TransformPoint((ndcX, ndcY, 1));
        Vec3 ray = (far - near).Normalize();

        Vec3 hit = near + (-near.Y / ray.Y) * ray;

        Assert.True(hit.IsApproximately((hitX, 0, hitZ), absoluteTolerance: 0.005), hit.ToString());
        Assert.True(viewProjection.TransformPoint(hit).XY.IsApproximately((ndcX, ndcY), absoluteTolerance: 1e-9));
    }
}

// Every example in Epsilon.LinearAlgebra/docs/limitations.md, with the result it shows. When a
// limitation is lifted, one of these fails: update the page together with the test.
public class LinearAlgebraLimitationsDocTests
{
    [Fact]
    public void Symbolic_matrices()
    {
        Assert.Equal("a * d + t^2 - b * c + (-a - d) * t", Matrix<Expr>.Parse("[[a, b], [c, d]]").CharacteristicPolynomial("t").Print());

        var polar = Vector<Expr>.Parse("[r * cos(t), r * sin(t)]", ["r", "t"]);
        Assert.Equal("cos(t)^2 * r + sin(t)^2 * r", polar.Jacobian(["r", "t"]).Determinant().Print());
        Assert.Equal("2", ExprParser.Parse("2sin(x)^2 + 2cos(x)^2").Simplify().Print());

        Assert.Equal("cos(a) * cos(b) - sin(a) * sin(b)",
            (Matrix<Expr>.Rotation(new Variable("a")) * Matrix<Expr>.Rotation(new Variable("b")))[0, 0].Print());
    }
}

// Every example in Epsilon.LinearAlgebra/docs/least-squares.md, one test per section.
public class LinearAlgebraLeastSquaresDocTests
{
    private static readonly double[] Xs = [0, 1, 2, 3, 4], Ys = [1.1, 1.9, 5.2, 9.8, 17.1];

    [Fact]
    public void Fitting_a_line()
    {
        var a = Matrix<double>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]);
        Vector<double> y = [1, 3, 4, 4];

        var c = a.LeastSquares(y);
        Assert.Equal("[1.5, 1]", c.ToString());
        Assert.Equal("[-0.5, 0.5, 0.5, -0.5]", (y - a * c).ToString());
        Assert.Equal("[0, 0]", (a.Transpose() * (y - a * c)).ToString());
    }

    [Fact]
    public void Any_function_with_linear_coefficients()
    {
        var powers = Matrix<double>.Create(5, 3, (i, j) => Math.Pow(Xs[i], j));
        Assert.StartsWith("[[1, 0, 0], [1, 1, 1], [1, 2, 4], ", powers.ToString());
        Assert.Equal("[1.082857142857144, -0.0957142857142874, 1.021428571428572]", powers.LeastSquares(Vector.Create(Ys)).ToString());
    }

    [Fact]
    public void Exact_and_symbolic_fits()
    {
        Assert.Equal("[3/2, 1]", Matrix<Rational>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]).LeastSquares([1, 3, 4, 4]).ToString());

        var exactPowers = Matrix<Rational>.Create(5, 3, (i, j) => new Rational((int)Math.Pow(Xs[i], j)));
        Assert.Equal("[379/350, -67/700, 143/140]", exactPowers.LeastSquares(Vector.Create(Ys).Map(Rational.FromDouble)).ToString());

        var parabola = Matrix<Expr>.Create(5, 3, (i, j) => (Expr)(int)Math.Pow(i - 2, j));
        Assert.Equal("[[1, -2, 4], [1, -1, 1], [1, 0, 0], [1, 1, 1], [1, 2, 4]]", parabola.Print());
        Assert.Equal("(12/35) * b + (12/35) * d + (17/35) * c - (3/35) * a - (3/35) * f",
            parabola.LeastSquares(Vector<Expr>.Parse("[a, b, c, d, f]"))[0].Print());

        Assert.Equal("[(p * u + q * v + r * w) / (p^2 + q^2 + r^2)]",
            Matrix<Expr>.Parse("[[p], [q], [r]]").LeastSquares(Vector<Expr>.Parse("[u, v, w]")).Print());
    }

    [Fact]
    public void More_unknowns_than_equations()
    {
        Assert.Equal("[1, 2, 2]", Matrix<Rational>.FromRows([1, 2, 2]).LeastSquares([9]).ToString());
        Assert.Equal("[0.9999999999999991, 1.9999999999999993, 1.9999999999999993]", Matrix<double>.FromRows([1, 2, 2]).LeastSquares([9]).ToString());
        Assert.Equal("Solve needs a square matrix, not a 2 x 3 matrix. LeastSquares takes any shape.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.Zero(2, 3).Solve([1, 2])).Message);
    }

    [Fact]
    public void Dependent_columns()
    {
        var dependent = Matrix<double>.FromRows([1, 2], [2, 4], [3, 6]);
        Assert.Equal("The columns of the matrix are linearly dependent to working precision. PseudoInverse handles any rank.",
            Assert.Throws<InvalidOperationException>(() => dependent.LeastSquares([1, 2, 3])).Message);
        Assert.Equal("[0.2, 0.4]", (dependent.PseudoInverse() * Vector.Create<double>(1, 2, 3)).ToString());
    }

    [Fact]
    public void Why_QR_for_doubles()
    {
        double eps = 1e-8;
        var lauchli = Matrix<double>.FromRows([1, 1], [eps, 0], [0, eps]);
        Vector<double> b = [2, eps, eps];
        Assert.Equal("[0.9999999999999998, 1.0000000000000002]", lauchli.LeastSquares(b).ToString());
        Assert.Equal("The matrix is singular: its rank is 1, not 2, to working precision.",
            Assert.Throws<InvalidOperationException>(() => (lauchli.Transpose() * lauchli).Solve(lauchli.Transpose() * b)).Message);
    }
}

// Every example in Epsilon.LinearAlgebra/docs/decompositions.md, one test per section.
public class LinearAlgebraDecompositionsDocTests
{
    [Fact]
    public void QR()
    {
        var a = Matrix<double>.FromRows([3, 0], [4, 5]);
        var (q, r) = a.QR();
        Assert.Equal("[[5, 4], [0, 3]]", r.ToString());
        Assert.True(q.IsApproximately(Matrix<double>.FromRows([0.6, -0.8], [0.8, 0.6])));
        Assert.True((q * r).IsApproximately(a));
    }

    [Fact]
    public void Cholesky()
    {
        var covariance = Matrix<double>.FromRows([4, 2], [2, 3]);
        var l = covariance.Cholesky();
        Assert.Equal("[[2, 0], [1, 1.4142135623730951]]", l.ToString());
        Assert.True(Matrix<double>.FromRows([2, -1], [-1, 2]).TryCholesky(out _));
        Assert.False(Matrix<double>.FromRows([1, 2], [2, 1]).TryCholesky(out _));

        // mean + l z has the covariance l l^T.
        Assert.True((l * l.Transpose()).IsApproximately(covariance));
        Assert.Equal("The matrix is not positive definite to working precision.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 1], [1, 1]).Cholesky()).Message);
    }

    [Fact]
    public void Symmetric_eigenvalues()
    {
        var ellipse = Matrix<double>.FromRows([1, 0.5], [0.5, 1]);
        var (values, vectors) = ellipse.SymmetricEigen();
        Assert.Equal("[0.5, 1.5]", values.ToString());
        Assert.Equal("[[0.7071067811865476, 0.7071067811865475], [-0.7071067811865475, 0.7071067811865476]]", vectors.ToString());
        Assert.Equal("[1.414213562373095, 0.8164965809277261]", values.Map(v => 1 / Math.Sqrt(v)).ToString());
        Assert.Equal("[1, 2, 11]", Matrix<double>.FromRows([2, 0, 0], [0, 3, 4], [0, 4, 9]).SymmetricEigen().Values.ToString());

        Vec3[] points = [(0, 0, 0), (2, 2, 0), (4, 4, 1), (6, 6, 0)];
        Vec3 mean = points.Aggregate(Vec3.Zero, (sum, p) => sum + p) / points.Length;
        Assert.Equal("[3, 3, 0.25]", mean.ToString());
        var spread = Matrix3x3<double>.Zero;
        foreach (Vec3 p in points)
        {
            Vec3 d = p - mean;
            spread += Matrix3x3<double>.FromColumns(d * d.X, d * d.Y, d * d.Z);
        }

        var (variances, axes) = (spread / points.Length).SymmetricEigen();
        Assert.Equal("[0, 0.17477764162066062, 10.012722358379339]", variances.ToString());
        Assert.Equal("[0.7066494206079106, 0.7066494206079107, 0.035960988710105474]", axes.Column(2).ToString());
        Assert.Equal(1, axes.Determinant(), 1e-15);

        // In the frame of the box the points are flat.
        foreach (Vec3 p in points)
            Assert.Equal(0, (axes.Transpose() * (p - mean)).X, 1e-14);
        Assert.True(Mat4.Rotation(Quaternion<double>.FromRotationMatrix(axes)).IsApproximately(new Mat4(axes)));
    }

    [Fact]
    public void Singular_values()
    {
        var a = Matrix<double>.FromRows([3, 0], [4, 5]);
        var (u, s, v) = a.Svd();
        Assert.Equal("[6.708203932499369, 2.2360679774997894]", s.ToString());
        Assert.True(s.IsApproximately([3 * Math.Sqrt(5), Math.Sqrt(5)]));
        Assert.True((u * Matrix<double>.FromDiagonal(s) * v.Transpose()).IsApproximately(a));

        var table = Matrix<double>.FromRows([1, 2, 3], [2, 4, 6.1], [3, 6, 9]);
        var (u3, s3, v3) = table.Svd();
        Assert.Equal("[14.043057523168343, 0.05035276541593254, 0]", s3.ToString());
        var nearest = s3[0] * u3.Column(0).Outer(v3.Column(0));
        Assert.Equal(0.0503527654159321, (table - nearest).SpectralNorm());

        Assert.Equal("[1E+300, 1E-300]", Matrix<double>.FromRows([1e300, 0], [0, 1e-300]).Svd().S.ToString());
        Assert.Equal(0.99, Matrix<double>.FromRows([1e20, 1e9], [1e9, 1]).Svd().S[1], 1e-15);
    }

    [Fact]
    public void Pseudo_inverse_norm_and_condition_number()
    {
        Assert.Equal("[[0.04, 0.08], [0.08, 0.16]]", Matrix<double>.FromRows([1, 2], [2, 4]).PseudoInverse().ToString());

        var m = Matrix<double>.FromRows([1, 2], [3, 4]);
        Assert.Equal(5.4649857042190435, m.SpectralNorm());
        Assert.Equal(14.933034373659256, m.ConditionNumber());
        Assert.Equal(15257577275.315193, Matrix<double>.Create(8, 8, (i, j) => 1.0 / (i + j + 1)).ConditionNumber());
        Assert.Equal(double.PositiveInfinity, Matrix<double>.FromRows([1, 2], [2, 4]).ConditionNumber());
        Assert.Equal(1e20, Matrix<double>.FromRows([1e-20, 0], [0, 1]).ConditionNumber());
    }
}
