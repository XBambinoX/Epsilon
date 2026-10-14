using Xunit;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class NumericCholeskyTests
{
    [Fact]
    public void Small_examples()
    {
        Assert.Equal("[[2, 0], [1, 1.4142135623730951]]", Matrix<double>.FromRows([4, 2], [2, 3]).Cholesky().ToString());
        Assert.Equal("[[2, 0, 0], [6, 1, 0], [-8, 5, 3]]",
            Matrix<double>.FromRows([4, 12, -16], [12, 37, -43], [-16, -43, 98]).Cholesky().ToString());
        Assert.Equal("[[3]]", Matrix<double>.FromRows([9]).Cholesky().ToString());
        Assert.Equal(0, Matrix<double>.Zero(0, 0).Cholesky().Rows);
    }

    [Fact]
    public void L_times_its_transpose_is_the_matrix_whatever_the_scale()
    {
        var random = new Random(1);
        for (int repeat = 0; repeat < 300; repeat++)
        {
            int n = random.Next(1, 30);
            var b = Matrix<double>.Create(n + random.Next(0, 4), n, (_, _) => random.NextDouble() * 2 - 1);
            var a = b.Transpose() * b;
            var d = Matrix<double>.FromDiagonal(Vector.Create(Enumerable.Range(0, n).Select(_ => Math.Pow(2, random.Next(-200, 201))).ToArray()));

            var l = a.Cholesky();
            Assert.True((l * l.Transpose()).IsApproximately(a, 1e-14));
            for (int i = 0; i < n; i++)
            {
                Assert.True(l[i, i] > 0);
                for (int j = i + 1; j < n; j++)
                    Assert.Equal(0, l[i, j]);
            }

            // D A D = (D L)(D L)^T, and powers of two scale exactly.
            Assert.True((d * a * d).TryCholesky(out var scaled));
            Assert.Equal(d * l, scaled);
        }
    }

    [Fact]
    public void Singular_matrices_are_not_positive_definite()
    {
        var random = new Random(2);
        for (int repeat = 0; repeat < 300; repeat++)
        {
            int n = random.Next(2, 30);
            var c = Matrix<double>.Create(n, random.Next(1, n), (_, _) => random.NextDouble() * 2 - 1);
            var d = Matrix<double>.FromDiagonal(Vector.Create(Enumerable.Range(0, n).Select(_ => Math.Pow(2, random.Next(-200, 201))).ToArray()));
            Assert.False((d * (c * c.Transpose()) * d).TryCholesky(out var lower));
            Assert.Null(lower);
        }

        Assert.Equal("The matrix is not positive definite to working precision.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 1], [1, 1]).Cholesky()).Message);
    }

    [Fact]
    public void Indefinite_and_negative_matrices()
    {
        Assert.False(Matrix<double>.FromRows([1, 2], [2, 1]).TryCholesky(out _));
        Assert.False(Matrix<double>.FromRows([-1, 0], [0, 1]).TryCholesky(out _));
        Assert.False(Matrix<double>.FromRows([-4]).TryCholesky(out _));
        Assert.False(Matrix<double>.Zero(2, 2).TryCholesky(out _));
    }

    [Fact]
    public void Nearly_singular_matrices()
    {
        Assert.True(Matrix<double>.FromRows([1, 1 - 1e-10], [1 - 1e-10, 1]).TryCholesky(out _));
        Assert.False(Matrix<double>.FromRows([1, 1 - 1e-16], [1 - 1e-16, 1]).TryCholesky(out _));

        // As for LU and QR: the 11 x 11 Hilbert matrix is positive definite to working precision, the 12 x 12 one is not.
        Assert.True(Matrix<double>.Create(11, 11, (i, j) => 1.0 / (i + j + 1)).TryCholesky(out _));
        Assert.False(Matrix<double>.Create(12, 12, (i, j) => 1.0 / (i + j + 1)).TryCholesky(out _));

        Assert.Equal("[[1E-10, 0], [0, 1]]", Matrix<double>.FromRows([1e-20, 0], [0, 1]).Cholesky().ToString());
        Assert.Equal("[[1E+150, 0], [0, 1E+150]]", Matrix<double>.FromRows([1e300, 0], [0, 1e300]).Cholesky().ToString());
        Assert.Equal("[[1E-150, 0], [0, 1E-150]]", Matrix<double>.FromRows([1e-300, 0], [0, 1e-300]).Cholesky().ToString());
    }

    [Fact]
    public void Symmetric_up_to_rounding()
    {
        // 0.1 + 0.2 is 0.30000000000000004; the entry below the diagonal is used.
        var l = Matrix<double>.FromRows([2, 0.1 + 0.2], [0.3, 2]).Cholesky();
        Assert.Equal(0.3 / Math.Sqrt(2), l[1, 0]);

        Assert.Equal("The matrix is not symmetric: the entries at (1, 0) and (0, 1) differ.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2], [3, 4]).Cholesky()).Message);
        Assert.False(Matrix<double>.FromRows([2, 1], [1.001, 2]).TryCholesky(out _));
    }

    [Fact]
    public void Wrong_shapes_and_non_finite_entries()
    {
        Assert.Equal("Cholesky needs a square matrix, not a 1 x 3 matrix.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2, 3]).Cholesky()).Message);
        Assert.False(Matrix<double>.FromRows([1, 2, 3]).TryCholesky(out _));

        Assert.Equal("The matrix has a non-finite entry at (0, 1): NaN.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, double.NaN], [double.NaN, 1]).Cholesky()).Message);
        Assert.False(Matrix<double>.FromRows([double.PositiveInfinity]).TryCholesky(out _));
    }
}
