using Xunit;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class NumericQrTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 5)]
    [InlineData(10, 3)]
    [InlineData(3, 10)]
    [InlineData(40, 25)]
    [InlineData(25, 40)]
    public void Q_times_R_is_the_matrix_with_orthonormal_Q_and_triangular_R(int rows, int columns)
    {
        var random = new Random(rows * 100 + columns);
        for (int repeat = 0; repeat < 10; repeat++)
        {
            var a = Matrix<double>.Create(rows, columns, (_, _) => random.NextDouble() * 2 - 1);
            var (q, r) = a.QR();
            int k = Math.Min(rows, columns);

            Assert.Equal((rows, k), (q.Rows, q.Columns));
            Assert.Equal((k, columns), (r.Rows, r.Columns));
            Assert.True((q * r).IsApproximately(a, 1e-14));
            Assert.True((q.Transpose() * q).IsApproximately(Matrix<double>.Identity(k), 1e-14));
            for (int i = 0; i < k; i++)
            {
                Assert.True(r[i, i] >= 0);
                for (int j = 0; j < i; j++)
                    Assert.Equal(0, r[i, j]);
            }
        }
    }

    [Fact]
    public void Small_example()
    {
        var (q, r) = Matrix<double>.FromRows([3, 0], [4, 5]).QR();
        Assert.Equal("[[5, 4], [0, 3]]", r.ToString());
        Assert.True(q.IsApproximately(Matrix<double>.FromRows([0.6, -0.8], [0.8, 0.6])));
    }

    [Fact]
    public void Exists_for_dependent_columns_and_zero_matrices()
    {
        var a = Matrix<double>.FromRows([1, 2], [2, 4], [3, 6]);
        var (q, r) = a.QR();
        Assert.True((q * r).IsApproximately(a));
        Assert.True(Math.Abs(r[1, 1]) < 1e-14);

        var (zeroQ, zeroR) = Matrix<double>.Zero(2, 2).QR();
        Assert.Equal(Matrix<double>.Identity(2), zeroQ);
        Assert.Equal(Matrix<double>.Zero(2, 2), zeroR);
    }

    [Fact]
    public void Empty_matrices()
    {
        var (q, r) = Matrix<double>.Zero(0, 3).QR();
        Assert.Equal((0, 0, 0, 3), (q.Rows, q.Columns, r.Rows, r.Columns));

        (q, r) = Matrix<double>.Zero(3, 0).QR();
        Assert.Equal((3, 0, 0, 0), (q.Rows, q.Columns, r.Rows, r.Columns));
    }

    [Fact]
    public void Non_finite_entries_throw() =>
        Assert.Equal("The matrix has a non-finite entry at (1, 0): NaN.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2], [double.NaN, 4]).QR()).Message);
}

public class NumericLeastSquaresTests
{
    // The line y = c0 + c1 x through (0, 1), (1, 3), (2, 4), (3, 4).
    private static readonly Matrix<double> Line = Matrix<double>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]);
    private static readonly Vector<double> Heights = [1, 3, 4, 4];

    [Fact]
    public void Fits_a_line()
    {
        var c = Line.LeastSquares(Heights);
        Assert.Equal("[1.5, 1]", c.ToString());

        // The residual is perpendicular to the columns.
        var residual = Heights - Line * c;
        Assert.True((Line.Transpose() * residual).IsApproximately(Vector<double>.Zero(2), absoluteTolerance: 1e-14));
    }

    [Fact]
    public void Agrees_with_the_exact_normal_equations()
    {
        var random = new Random(5);
        int compared = 0;
        for (int repeat = 0; repeat < 300; repeat++)
        {
            int rows = random.Next(1, 8), columns = random.Next(1, 8);
            var integers = Matrix<int>.Create(rows, columns, (_, _) => random.Next(-9, 10));
            var b = Vector.Create(Enumerable.Range(0, rows).Select(_ => random.Next(-9, 10)).ToArray());
            var exactMatrix = integers.Map(n => (Rational)n);
            if (exactMatrix.Rank() < Math.Min(rows, columns))
                continue;

            var exact = exactMatrix.LeastSquares(b.Map(n => (Rational)n)).Map(r => r.ToDouble());
            var numeric = integers.Map(n => (double)n).LeastSquares(b.Map(n => (double)n));
            Assert.True(numeric.IsApproximately(exact, 1e-12), $"{integers}: {numeric} vs {exact}");
            compared++;
        }

        Assert.True(compared > 200);
    }

    [Fact]
    public void A_square_matrix_gives_the_results_of_Solve()
    {
        var n = Matrix<double>.FromRows([2, 1], [4, 4]);
        Assert.Equal(n.Solve([4, 12]), n.LeastSquares([4, 12]));
        Assert.Equal("The matrix is singular: its rank is 1, not 2, to working precision.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2], [2, 4]).LeastSquares([1, 2])).Message);
    }

    [Fact]
    public void More_columns_than_rows_give_the_solution_of_smallest_norm()
    {
        Assert.True(Matrix<double>.FromRows([1, 1]).LeastSquares([2]).IsApproximately([1, 1]));

        var a = Matrix<double>.FromRows([1, 2, 2]);
        var x = a.LeastSquares([9]);
        Assert.True(x.IsApproximately([1, 2, 2], 1e-15));

        // Exact, and perpendicular to the solutions of A x = 0.
        var wide = Matrix<double>.FromRows([1, 2, 3, 4], [0, 1, -1, 2]);
        var y = wide.LeastSquares([3, 1]);
        Assert.True((wide * y).IsApproximately([3, 1], 1e-14));
        Assert.Equal(0, y.Dot([5, -1, -1, 0]), 1e-14);   // wide * [5, -1, -1, 0] = 0
    }

    [Fact]
    public void Solves_for_each_column_of_a_matrix()
    {
        var b = Matrix<double>.FromBlocks([Heights, 2 * Heights]);
        var x = Line.LeastSquares(b);
        Assert.Equal(Line.LeastSquares(Heights), x.Column(0));
        Assert.True(x.Column(1).IsApproximately(2 * Line.LeastSquares(Heights)));

        var wide = Matrix<double>.FromRows([1, 2, 2]);
        Assert.Equal((3, 2), (wide.LeastSquares(Matrix<double>.FromRows([9, 18])).Rows, wide.LeastSquares(Matrix<double>.FromRows([9, 18])).Columns));
    }

    [Fact]
    public void Dependent_columns_or_rows_throw()
    {
        Assert.Equal("The columns of the matrix are linearly dependent to working precision.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2], [2, 4], [3, 6]).LeastSquares([1, 2, 3])).Message);
        Assert.Equal("The columns of the matrix are linearly dependent to working precision.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([0.1, 0.3], [0.2, 0.6], [0.3, 0.9]).LeastSquares([1, 2, 3])).Message);
        Assert.Equal("The rows of the matrix are linearly dependent to working precision.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2, 3], [2, 4, 6]).LeastSquares([1, 2])).Message);
        Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 0], [0, 0], [0, 0]).LeastSquares([1, 2, 3]));
    }

    [Fact]
    public void Finds_every_dependence_whatever_its_coefficients()
    {
        var random = new Random(3);
        for (int repeat = 0; repeat < 300; repeat++)
        {
            int rows = random.Next(2, 30), columns = random.Next(2, Math.Min(rows, 12) + 1);
            var basis = Matrix<double>.Create(rows, columns - 1, (_, _) => random.NextDouble() * 2 - 1);
            var coefficients = Vector.Create(Enumerable.Range(0, columns - 1)
                .Select(_ => (random.NextDouble() * 2 - 1) * Math.Pow(10, random.Next(-8, 9))).ToArray());
            var a = Matrix<double>.FromBlocks([basis, basis * coefficients]);

            Assert.Throws<InvalidOperationException>(() => a.LeastSquares(Vector<double>.Zero(rows)));
            Assert.Throws<InvalidOperationException>(() => a.Transpose().LeastSquares(Vector<double>.Zero(columns)));
        }
    }

    [Fact]
    public void Scaling_the_columns_changes_nothing_but_the_solution_scale()
    {
        var random = new Random(4);
        for (int repeat = 0; repeat < 300; repeat++)
        {
            int rows = random.Next(1, 30), columns = random.Next(1, Math.Min(rows, 12) + 1);
            var a = Matrix<double>.Create(rows, columns, (_, _) => random.NextDouble() * 2 - 1);
            var scales = Vector.Create(Enumerable.Range(0, columns).Select(_ => Math.Pow(2, random.Next(-300, 301))).ToArray());
            var b = Vector.Create(Enumerable.Range(0, rows).Select(_ => random.NextDouble()).ToArray());

            var x = a.LeastSquares(b);
            var scaled = (a * Matrix<double>.FromDiagonal(scales)).LeastSquares(b);
            Assert.True(scaled.Hadamard(scales).IsApproximately(x, 1e-10));
        }

        Assert.Equal("[1, 1]", Matrix<double>.FromRows([1e-20, 0], [0, 1], [0, 0]).LeastSquares([1e-20, 1, 5]).ToString());
    }

    [Theory]
    [InlineData(1e-12, true)]
    [InlineData(1e-14, true)]
    [InlineData(1e-15, false)]
    public void Nearly_parallel_columns(double sine, bool independent)
    {
        var a = Matrix<double>.FromRows([1, 1], [0, sine], [0, 0]);
        if (independent)
            Assert.Equal("[1, 1]", a.LeastSquares([2, sine, 0]).ToString());
        else
            Assert.Throws<InvalidOperationException>(() => a.LeastSquares([2, sine, 0]));
    }

    [Fact]
    public void Ill_conditioned_columns()
    {
        var hilbert10 = Matrix<double>.Create(10, 10, (i, j) => 1.0 / (i + j + 1));
        var tall10 = Matrix<double>.FromBlocks([hilbert10], [Matrix<double>.Zero(1, 10)]);
        Assert.Equal(10, tall10.LeastSquares(Vector<double>.Zero(11)).Length);

        var hilbert12 = Matrix<double>.Create(12, 12, (i, j) => 1.0 / (i + j + 1));
        var tall12 = Matrix<double>.FromBlocks([hilbert12], [Matrix<double>.Zero(1, 12)]);
        Assert.Throws<InvalidOperationException>(() => tall12.LeastSquares(Vector<double>.Zero(13)));
    }

    [Fact]
    public void Wrong_sizes_and_non_finite_entries()
    {
        Assert.StartsWith("The right-hand side has 2 entries, but the matrix has 4 rows.",
            Assert.Throws<ArgumentException>(() => Line.LeastSquares([1, 2])).Message);
        Assert.StartsWith("The right-hand side has 1 rows, but the matrix has 4.",
            Assert.Throws<ArgumentException>(() => Line.LeastSquares(Matrix<double>.FromRows([1, 2]))).Message);

        // A wide matrix is decomposed as its transpose; the message still names the entry the right way round.
        Assert.Equal("The matrix has a non-finite entry at (0, 1): NaN.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, double.NaN, 3], [4, 5, 6]).LeastSquares([1, 2])).Message);
        Assert.Equal("The matrix has a non-finite entry at (2, 0): Infinity.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2], [3, 4], [double.PositiveInfinity, 6]).LeastSquares([1, 2, 3])).Message);
    }

    [Fact]
    public void Empty_matrices()
    {
        Assert.Equal(0, Matrix<double>.Zero(3, 0).LeastSquares([1, 2, 3]).Length);
        Assert.Equal("[0, 0]", Matrix<double>.Zero(0, 2).LeastSquares(Vector<double>.Zero(0)).ToString());
    }

    [Fact]
    public void Solve_of_a_non_square_matrix_points_to_LeastSquares()
    {
        const string message = "Solve needs a square matrix, not a 2 x 3 matrix. LeastSquares takes any shape.";
        Assert.Equal(message, Assert.Throws<InvalidOperationException>(() => Matrix<double>.Zero(2, 3).Solve([1, 2])).Message);
        Assert.Equal(message, Assert.Throws<InvalidOperationException>(() => Matrix<Rational>.Zero(2, 3).Solve([1, 2])).Message);
        Assert.Equal(message, Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Zero(2, 3).Solve(Vector<Expr>.Parse("[1, 2]"))).Message);
    }
}

public class RationalLeastSquaresTests
{
    private static readonly Matrix<Rational> Line = Matrix<Rational>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]);

    [Fact]
    public void Exact_for_every_shape()
    {
        Assert.Equal("[3/2, 1]", Line.LeastSquares([1, 3, 4, 4]).ToString());
        Assert.Equal("[1, 2, 2]", Matrix<Rational>.FromRows([1, 2, 2]).LeastSquares([9]).ToString());

        var square = Matrix<Rational>.FromRows([1, 2], [3, 4]);
        Assert.Equal(square.Solve([5, 6]), square.LeastSquares([5, 6]));
        Assert.Equal("[[3/2, 3], [1, 2]]", Line.LeastSquares(Matrix<Rational>.FromRows([1, 2], [3, 6], [4, 8], [4, 8])).ToString());
    }

    [Fact]
    public void Dependent_columns_or_rows_throw()
    {
        Assert.Equal("The columns of the matrix are linearly dependent.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Rational>.FromRows([1, 2], [2, 4], [3, 6]).LeastSquares([1, 2, 3])).Message);
        Assert.Equal("The rows of the matrix are linearly dependent.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Rational>.FromRows([1, 2, 3], [2, 4, 6]).LeastSquares([1, 2])).Message);
        Assert.Equal("The matrix is singular: its rank is 1, not 2.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Rational>.FromRows([1, 2], [2, 4]).LeastSquares([1, 2])).Message);
        Assert.Throws<ArgumentException>(() => Line.LeastSquares([1, 2]));
    }
}

public class SymbolicLeastSquaresTests
{
    [Fact]
    public void Constant_matrices_give_combinations_of_the_right_hand_side()
    {
        var line = Matrix<Expr>.Parse("[[1, 0], [1, 1], [1, 2]]");
        Assert.Equal("[(-1/6) * w + (1/3) * v + (5/6) * u, (-1/2) * u + (1/2) * w]",
            line.LeastSquares(Vector<Expr>.Parse("[u, v, w]")).Print());

        // A parabola through five points: the smoothing weights of Savitzky and Golay.
        var parabola = Matrix<Expr>.Create(5, 3, (i, j) => (Expr)(int)Math.Pow(i - 2, j));
        Assert.Equal("(12/35) * b + (12/35) * d + (17/35) * c - (3/35) * a - (3/35) * f",
            parabola.LeastSquares(Vector<Expr>.Parse("[a, b, c, d, f]"))[0].Print());

        Assert.Equal("[(1/2) * s, (1/2) * s]", Matrix<Expr>.Parse("[[1, 1]]").LeastSquares(Vector<Expr>.Parse("[s]")).Print());
    }

    [Fact]
    public void Symbols_in_the_matrix_give_one_fraction_per_entry()
    {
        Assert.Equal("[s / (x^2 + 1), s * x / (x^2 + 1)]", Matrix<Expr>.Parse("[[1, x]]").LeastSquares(Vector<Expr>.Parse("[s]")).Print());

        var line = Matrix<Expr>.Parse("[[1, p], [1, q], [1, r]]");
        var fit = line.LeastSquares(Vector<Expr>.Parse("[u, v, w]"));
        var values = new Dictionary<string, double> { ["p"] = 0, ["q"] = 1, ["r"] = 3, ["u"] = 1, ["v"] = 3, ["w"] = 4 };
        var numeric = Matrix<double>.FromRows([1, 0], [1, 1], [1, 3]).LeastSquares([1, 3, 4]);
        Assert.True(fit.Evaluate(values).IsApproximately(numeric, 1e-14));
    }

    [Fact]
    public void Square_matrices_give_the_results_of_Solve()
    {
        var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");
        Assert.Equal(m.Solve(Vector<Expr>.Parse("[p, q]")), m.LeastSquares(Vector<Expr>.Parse("[p, q]")));
    }

    [Fact]
    public void Dependent_columns_or_rows_throw()
    {
        Assert.Equal("The columns of the matrix are linearly dependent.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[1, 2], [2, 4], [3, 6]]").LeastSquares(Vector<Expr>.Parse("[1, 2, 3]"))).Message);
        Assert.Equal("The columns of the matrix are linearly dependent.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[1, x], [2, 2x], [3, 3x]]").LeastSquares(Vector<Expr>.Parse("[1, 2, 3]"))).Message);
        Assert.Equal("The rows of the matrix are linearly dependent.",
            Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[1, x, 1], [2, 2x, 2]]").LeastSquares(Vector<Expr>.Parse("[1, 2]"))).Message);
        Assert.Throws<ArgumentException>(() => Matrix<Expr>.Parse("[[1], [2]]").LeastSquares(Vector<Expr>.Parse("[1]")));
    }
}
