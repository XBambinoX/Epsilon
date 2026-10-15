using Xunit;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class NumericSvdTests
{
    [Fact]
    public void Small_examples()
    {
        var (u, s, v) = Matrix<double>.FromRows([3, 0], [4, 5]).Svd();
        Assert.True(s.IsApproximately([3 * Math.Sqrt(5), Math.Sqrt(5)]));
        Assert.True((u * Matrix<double>.FromDiagonal(s) * v.Transpose()).IsApproximately(Matrix<double>.FromRows([3, 0], [4, 5])));

        (u, s, v) = Matrix<double>.FromRows([1, 2], [2, 4]).Svd();
        Assert.Equal("[5, 0]", s.ToString());
        Assert.True((u.Transpose() * u).IsApproximately(Matrix<double>.Identity(2)));
        Assert.True(v.Column(0).IsApproximately(Vector.Create(1, 2.0).Normalize()));

        (u, s, v) = Matrix<double>.FromRows([1, 2, 3]).Svd();
        Assert.Equal("[[1]]", u.ToString());
        Assert.Equal(Math.Sqrt(14), s[0], 1e-15);
        Assert.True(v.Column(0).IsApproximately(Vector.Create(1, 2, 3.0).Normalize()));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 2)]
    [InlineData(2, 3)]
    [InlineData(6, 6)]
    [InlineData(20, 7)]
    [InlineData(7, 20)]
    [InlineData(40, 25)]
    public void U_diag_S_V_transpose_is_the_matrix(int rows, int columns)
    {
        var random = new Random(rows * 100 + columns);
        int k = Math.Min(rows, columns);
        for (int repeat = 0; repeat < 10; repeat++)
        {
            var a = Matrix<double>.Create(rows, columns, (_, _) => random.NextDouble() * 2 - 1);
            var (u, s, v) = a.Svd();

            Assert.Equal((rows, k, columns, k, k), (u.Rows, u.Columns, v.Rows, v.Columns, s.Length));
            Assert.True((u * Matrix<double>.FromDiagonal(s) * v.Transpose()).IsApproximately(a, 1e-14));
            Assert.True((u.Transpose() * u).IsApproximately(Matrix<double>.Identity(k), 1e-13));
            Assert.True((v.Transpose() * v).IsApproximately(Matrix<double>.Identity(k), 1e-13));
            for (int j = 0; j < k; j++)
            {
                if (j > 0)
                    Assert.True(s[j - 1] >= s[j]);
                Assert.True(v.Column(j).MaxBy(Math.Abs) > 0);
            }

            // The singular values are the square roots of the eigenvalues of A^T A.
            var squares = (a.Transpose() * a).SymmetricEigen().Values.Reverse().Take(k).Select(e => Math.Sqrt(Math.Max(e, 0))).ToArray();
            Assert.True(s.IsApproximately(Vector.Create(squares), 1e-13));
        }
    }

    [Fact]
    public void Rank_deficient_and_zero_matrices_still_have_orthonormal_U_and_V()
    {
        var random = new Random(9);
        var a = Matrix<double>.Create(6, 1, (_, _) => random.NextDouble()) * Matrix<double>.Create(1, 4, (_, _) => random.NextDouble());
        var (u, s, v) = a.Svd();
        Assert.True(s[1..].IsApproximately(Vector<double>.Zero(3), absoluteTolerance: 1e-15 * s[0]));
        Assert.True((u.Transpose() * u).IsApproximately(Matrix<double>.Identity(4), 1e-14));
        Assert.True((v.Transpose() * v).IsApproximately(Matrix<double>.Identity(4), 1e-14));

        (u, s, v) = Matrix<double>.Zero(3, 2).Svd();
        Assert.Equal("[0, 0]", s.ToString());
        Assert.Equal("[[1, 0], [0, 1], [0, 0]]", u.ToString());
        Assert.Equal(Matrix<double>.Identity(2), v);

        Assert.Equal(0, Matrix<double>.Zero(0, 3).Svd().S.Length);
    }

    [Fact]
    public void Huge_tiny_and_graded_entries()
    {
        Assert.Equal("[1E+300, 1E-300]", Matrix<double>.FromRows([1e300, 0], [0, 1e-300]).Svd().S.ToString());
        Assert.True(Matrix<double>.FromRows([1e300, 1e300], [1e300, -1e300]).Svd().S.IsApproximately([Math.Sqrt(2) * 1e300, Math.Sqrt(2) * 1e300]));
        Assert.True((1e300 * Matrix<double>.FromRows([1e-300, 2e-300], [3e-300, 4e-300]).Svd().S)
            .IsApproximately(Matrix<double>.FromRows([1, 2], [3, 4]).Svd().S, 1e-14));

        // One-sided Jacobi keeps the small singular value of a graded matrix to full relative precision.
        Assert.Equal(0.99, Matrix<double>.FromRows([1e20, 1e9], [1e9, 1]).Svd().S[1], 1e-15);
    }

    [Fact]
    public void Non_finite_entries_throw() =>
        Assert.Equal("The matrix has a non-finite entry at (0, 1): NaN.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, double.NaN]).Svd()).Message);
}

public class NumericPseudoInverseTests
{
    [Fact]
    public void Small_examples()
    {
        Assert.Equal("[[0.04, 0.08], [0.08, 0.16]]", Matrix<double>.FromRows([1, 2], [2, 4]).PseudoInverse().ToString());
        Assert.True(Matrix<double>.FromRows([1, 1]).PseudoInverse().IsApproximately(Matrix<double>.FromRows([0.5], [0.5])));
        Assert.True(Matrix<double>.FromRows([1, 2], [3, 4]).PseudoInverse().IsApproximately(Matrix<double>.FromRows([1, 2], [3, 4]).Inverse(), 1e-14));
        Assert.Equal((3, 0), (Matrix<double>.Zero(0, 3).PseudoInverse().Rows, Matrix<double>.Zero(0, 3).PseudoInverse().Columns));
        Assert.Equal(Matrix<double>.Zero(3, 2), Matrix<double>.Zero(2, 3).PseudoInverse());
    }

    [Fact]
    public void Satisfies_the_Penrose_conditions_for_any_rank()
    {
        var random = new Random(4);
        for (int repeat = 0; repeat < 100; repeat++)
        {
            int rows = random.Next(1, 12), columns = random.Next(1, 12), rank = random.Next(1, Math.Min(rows, columns) + 1);
            var a = Matrix<double>.Create(rows, rank, (_, _) => random.NextDouble() * 2 - 1)
                  * Matrix<double>.Create(rank, columns, (_, _) => random.NextDouble() * 2 - 1);
            var x = a.PseudoInverse();

            Assert.True((a * x * a).IsApproximately(a, 1e-10));
            Assert.True((x * a * x).IsApproximately(x, 1e-10));
            Assert.True((a * x).IsApproximately((a * x).Transpose(), 1e-10));
            Assert.True((x * a).IsApproximately((x * a).Transpose(), 1e-10));
        }
    }

    [Fact]
    public void Gives_the_least_squares_solution_of_least_norm()
    {
        var line = Matrix<double>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]);
        Assert.True((line.PseudoInverse() * Vector.Create<double>(1, 3, 4, 4)).IsApproximately(line.LeastSquares([1, 3, 4, 4]), 1e-14));

        // Where LeastSquares gives up: the columns are dependent.
        var dependent = Matrix<double>.FromRows([1, 2], [2, 4], [3, 6]);
        Assert.Throws<InvalidOperationException>(() => dependent.LeastSquares([1, 2, 3]));
        Assert.True((dependent.PseudoInverse() * Vector.Create<double>(1, 2, 3)).IsApproximately([0.2, 0.4]));
    }

    [Fact]
    public void LeastSquares_points_to_it() =>
        Assert.Equal("The columns of the matrix are linearly dependent to working precision. PseudoInverse handles any rank.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2], [2, 4], [3, 6]).LeastSquares([1, 2, 3])).Message);
}

public class NumericSpectralNormAndConditionTests
{
    [Fact]
    public void Small_examples()
    {
        var a = Matrix<double>.FromRows([1, 2], [3, 4]);
        Assert.Equal(5.4649857042190435, a.SpectralNorm(), 1e-14);
        Assert.Equal(14.933034373659256, a.ConditionNumber(), 1e-12);
        Assert.Equal(1, Matrix<double>.Identity(3).ConditionNumber());
        Assert.Equal(double.PositiveInfinity, Matrix<double>.FromRows([1, 2], [2, 4]).ConditionNumber());
        Assert.Equal(3, Matrix<double>.FromRows([1, 2, 2]).SpectralNorm(), 1e-15);
    }

    [Fact]
    public void The_spectral_norm_is_at_most_the_Frobenius_norm()
    {
        var random = new Random(6);
        for (int repeat = 0; repeat < 50; repeat++)
        {
            var a = Matrix<double>.Create(random.Next(1, 8), random.Next(1, 8), (_, _) => random.NextDouble() * 2 - 1);
            double spectral = a.SpectralNorm();
            Assert.InRange(spectral, a.FrobeniusNorm() / Math.Sqrt(Math.Min(a.Rows, a.Columns)) * (1 - 1e-14), a.FrobeniusNorm() * (1 + 1e-14));
            Assert.Equal(a.Svd().S[0], spectral);
        }
    }

    [Fact]
    public void The_Hilbert_matrix()
    {
        // cond(H8) is about 1.5258e10.
        Assert.Equal(1.5258e10, Matrix<double>.Create(8, 8, (i, j) => 1.0 / (i + j + 1)).ConditionNumber(), 0.0001e10);
    }

    [Fact]
    public void Empty_matrices()
    {
        Assert.Equal(0, Matrix<double>.Zero(0, 0).SpectralNorm());
        Assert.Equal("An empty matrix has no condition number.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.Zero(0, 0).ConditionNumber()).Message);
    }
}
