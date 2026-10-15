using Xunit;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class NumericSymmetricEigenTests
{
    [Fact]
    public void Small_examples()
    {
        var (values, vectors) = Matrix<double>.FromRows([2, 1], [1, 2]).SymmetricEigen();
        Assert.Equal("[1, 3]", values.ToString());
        Assert.Equal("[[0.7071067811865476, 0.7071067811865475], [-0.7071067811865475, 0.7071067811865476]]", vectors.ToString());

        (values, vectors) = Matrix<double>.FromRows([2, 0, 0], [0, 3, 4], [0, 4, 9]).SymmetricEigen();
        Assert.Equal("[1, 2, 11]", values.ToString());
        Assert.Equal("[[0, 1, 0], [0.8944271909999159, 0, 0.4472135954999579], [-0.4472135954999579, 0, 0.8944271909999159]]", vectors.ToString());

        Assert.Equal("[-1, 1]", Matrix<double>.FromRows([0, 1], [1, 0]).SymmetricEigen().Values.ToString());
        Assert.Equal(0, Matrix<double>.Zero(0, 0).SymmetricEigen().Values.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(40)]
    public void V_diag_V_transpose_is_the_matrix(int n)
    {
        var random = new Random(n);
        for (int repeat = 0; repeat < 10; repeat++)
        {
            var b = Matrix<double>.Create(n, n, (_, _) => random.NextDouble() * 2 - 1);
            var a = b + b.Transpose();
            var (values, vectors) = a.SymmetricEigen();

            Assert.True((vectors * Matrix<double>.FromDiagonal(values) * vectors.Transpose()).IsApproximately(a, 1e-13));
            Assert.True((vectors.Transpose() * vectors).IsApproximately(Matrix<double>.Identity(n), 1e-13));
            Assert.Equal(a.Trace(), values.Sum(), 1e-12);
            for (int j = 0; j < n; j++)
            {
                if (j > 0)
                    Assert.True(values[j - 1] <= values[j]);
                var column = vectors.Column(j);
                Assert.True(column.MaxBy(Math.Abs) > 0);
            }
        }
    }

    [Fact]
    public void Agrees_with_the_roots_of_the_characteristic_polynomial()
    {
        var random = new Random(7);
        for (int repeat = 0; repeat < 20; repeat++)
        {
            var entries = Matrix<int>.Create(3, 3, (_, _) => random.Next(-5, 6));
            var symmetric = Matrix<int>.Create(3, 3, (i, j) => entries[Math.Max(i, j), Math.Min(i, j)]);
            var roots = symmetric.Map(e => (Expr)e).CharacteristicPolynomial("t").FindRealRoots();
            var values = symmetric.Map(e => (double)e).SymmetricEigen().Values;
            if (roots.Count != 3)
                continue;   // a repeated root

            Assert.True(values.IsApproximately(Vector.Create(roots.ToArray()), 1e-9), $"{symmetric}: {values}");
        }
    }

    [Fact]
    public void Repeated_eigenvalues()
    {
        var (values, vectors) = Matrix<double>.Identity(3).SymmetricEigen();
        Assert.Equal("[1, 1, 1]", values.ToString());
        Assert.Equal(Matrix<double>.Identity(3), vectors);

        var ones = Matrix<double>.Create(3, 3, (_, _) => 1);
        (values, vectors) = ones.SymmetricEigen();
        Assert.True(values.IsApproximately([0, 0, 3], absoluteTolerance: 1e-15));
        Assert.True((vectors.Transpose() * vectors).IsApproximately(Matrix<double>.Identity(3)));
        Assert.True((ones * vectors.Column(2)).IsApproximately(3 * vectors.Column(2)));
    }

    [Fact]
    public void Huge_tiny_and_graded_entries()
    {
        var huge = Matrix<double>.FromRows([1e300, 1e300], [1e300, -1e300]).SymmetricEigen().Values;
        Assert.True(huge.IsApproximately([-Math.Sqrt(2) * 1e300, Math.Sqrt(2) * 1e300]));
        Assert.Equal("[1E-300, 2E-300]", Matrix<double>.FromRows([1e-300, 0], [0, 2e-300]).SymmetricEigen().Values.ToString());

        // Jacobi keeps the small eigenvalue of a graded matrix to full relative precision:
        // det / 1e20 = (1e20 - 1e18) / 1e20.
        Assert.Equal(0.99, Matrix<double>.FromRows([1e20, 1e9], [1e9, 1]).SymmetricEigen().Values[0], 1e-15);
    }

    [Fact]
    public void Symmetric_up_to_rounding()
    {
        // The entry below the diagonal is used.
        Assert.Equal("[-0.3, 0.3]",
            Matrix<double>.FromRows([0, 0.1 + 0.2], [0.3, 0]).SymmetricEigen().Values.ToString());
        Assert.Equal("The matrix is not symmetric: the entries at (1, 0) and (0, 1) differ.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2], [3, 4]).SymmetricEigen()).Message);
    }

    [Fact]
    public void Wrong_shapes_and_non_finite_entries()
    {
        Assert.Equal("SymmetricEigen needs a square matrix, not a 1 x 3 matrix.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2, 3]).SymmetricEigen()).Message);
        Assert.Equal("The matrix has a non-finite entry at (0, 1): NaN.",
            Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, double.NaN], [double.NaN, 1]).SymmetricEigen()).Message);
    }
}

public class FixedSizeSymmetricEigenTests
{
    [Fact]
    public void Agrees_with_the_general_matrices_and_gives_a_rotation()
    {
        var random = new Random(3);
        double Entry() => random.NextDouble() * 2 - 1;
        for (int repeat = 0; repeat < 2000; repeat++)
        {
            var b = new Matrix3x3<double>(Entry(), Entry(), Entry(), Entry(), Entry(), Entry(), Entry(), Entry(), Entry());
            var a = repeat % 2 == 0 ? b + b.Transpose() : b * b.Transpose();
            var (values, vectors) = a.SymmetricEigen();

            Assert.True(values.ToVector().IsApproximately(a.ToMatrix().SymmetricEigen().Values, 1e-14));
            Assert.True(values.X <= values.Y && values.Y <= values.Z);
            Assert.Equal(1, vectors.Determinant(), 1e-14);
            Assert.True((vectors * new Matrix3x3<double>(values.X, 0, 0, 0, values.Y, 0, 0, 0, values.Z) * vectors.Transpose()).IsApproximately(a, 1e-14));
        }
    }

    [Fact]
    public void Float_and_Half()
    {
        Assert.Equal("[1, 3, 5]", new Matrix3x3<float>(2, 1, 0, 1, 2, 0, 0, 0, 5).SymmetricEigen().Values.ToString());

        var (values, vectors) = new Matrix3x3<Half>((Half)2, (Half)1, (Half)0, (Half)1, (Half)2, (Half)0, (Half)0, (Half)0, (Half)5).SymmetricEigen();
        Assert.Equal("[1, 3, 5]", values.ToString());
        Assert.True(vectors.Column(0).IsApproximately(((Half)0.7071, (Half)(-0.7071), (Half)0)));
    }

    [Fact]
    public void The_principal_axes_of_a_rotated_box()
    {
        // The inertia tensor of a box of 2 x 4 x 6 and mass 1, turned by some Euler angles.
        var rotation = Matrix3x3<double>.FromEulerAngles((0.3, 0.5, 0.7), EulerOrder.XYZ);
        var inertia = rotation * Matrix3x3<double>.Scale((52.0 / 12, 40.0 / 12, 20.0 / 12)) * rotation.Transpose();

        var (moments, axes) = inertia.SymmetricEigen();
        Assert.True(moments.IsApproximately((20.0 / 12, 40.0 / 12, 52.0 / 12)));
        for (int j = 0; j < 3; j++)
            Assert.Equal(1, Math.Abs(axes.Column(j).Dot(rotation.Column(2 - j))), 1e-14);   // the shortest side has the largest moment
    }

    [Fact]
    public void Repeated_and_zero_eigenvalues()
    {
        var (values, vectors) = Matrix3x3<double>.Identity.SymmetricEigen();
        Assert.Equal("[1, 1, 1]", values.ToString());
        Assert.Equal(Matrix3x3<double>.Identity, vectors);

        Assert.True(new Matrix3x3<double>(1, 1, 1, 1, 1, 1, 1, 1, 1).SymmetricEigen().Values.IsApproximately((0, 0, 3), absoluteTolerance: 1e-15));
        Assert.Equal("[0, 0, 0]", Matrix3x3<double>.Zero.SymmetricEigen().Values.ToString());
    }

    [Fact]
    public void Does_not_allocate()
    {
        var a = new Matrix3x3<double>(4, 1, 2, 1, 5, 3, 2, 3, 6);
        a.SymmetricEigen();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int k = 0; k < 100; k++)
            a.SymmetricEigen();
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void Errors()
    {
        Assert.Equal("The matrix is not symmetric: the entries at (1, 0) and (0, 1) differ.",
            Assert.Throws<InvalidOperationException>(() => new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9).SymmetricEigen()).Message);
        Assert.Equal("The matrix has an entry that is NaN or infinite.",
            Assert.Throws<InvalidOperationException>(() => (Matrix3x3<double>.Identity with { M12 = double.NaN }).SymmetricEigen()).Message);
    }
}
