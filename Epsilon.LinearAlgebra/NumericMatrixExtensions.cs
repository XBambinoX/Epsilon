namespace Epsilon.LinearAlgebra;

/// <summary>Arithmetic, determinant, inverse, solve and rank of matrices of <see cref="double"/>.</summary>
public static class NumericMatrixExtensions
{
    extension(Matrix<double> matrix)
    {
        /// <summary>The <paramref name="size"/> x <paramref name="size"/> identity matrix.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
        public static Matrix<double> Identity(int size) =>
            Matrix<double>.Create(size, size, (i, j) => i == j ? 1 : 0);

        /// <summary>The <paramref name="rows"/> x <paramref name="columns"/> matrix of zeros.</summary>
        /// <exception cref="ArgumentOutOfRangeException">A dimension is negative.</exception>
        public static Matrix<double> Zero(int rows, int columns) =>
            Matrix<double>.Create(rows, columns, (_, _) => 0);

        /// <summary>The sum of the diagonal entries.</summary>
        /// <exception cref="InvalidOperationException">The matrix is not square.</exception>
        public double Trace()
        {
            matrix.CheckSquare("The trace");

            double sum = 0;
            for (int i = 0; i < matrix.Rows; i++)
                sum += matrix[i, i];

            return sum;
        }

        /// <summary>
        /// The determinant, by LU decomposition with partial pivoting; 0 if the matrix is singular
        /// to working precision.
        /// </summary>
        /// <exception cref="InvalidOperationException">The matrix is not square or has an entry that is NaN or infinite.</exception>
        public double Determinant()
        {
            matrix.CheckSquare("The determinant");
            return new LuDecomposition(matrix).Determinant();
        }

        /// <summary>The inverse, by LU decomposition with partial pivoting.</summary>
        /// <exception cref="InvalidOperationException">
        /// The matrix is not square, is singular to working precision or has an entry that is NaN or infinite.
        /// </exception>
        public Matrix<double> Inverse()
        {
            matrix.CheckSquare("The inverse");

            int n = matrix.Rows;
            var identity = Matrix<double>.Identity(n);
            return new Matrix<double>(n, n, new LuDecomposition(matrix).Solve(identity.Entries, n));
        }

        /// <summary>The solution x of A * x = b, by LU decomposition with partial pivoting.</summary>
        /// <exception cref="ArgumentException"><paramref name="b"/> has not as many entries as the matrix has rows.</exception>
        /// <exception cref="InvalidOperationException">
        /// The matrix is not square, is singular to working precision or has an entry that is NaN or infinite.
        /// </exception>
        public Vector<double> Solve(Vector<double> b)
        {
            matrix.CheckSquare("Solve");
            if (b.Length != matrix.Rows)
                throw new ArgumentException($"The right-hand side has {b.Length} entries, but the matrix has {matrix.Rows} rows.", nameof(b));

            return new Vector<double>(new LuDecomposition(matrix).Solve(b.ToMatrix().Entries, 1));
        }

        /// <summary>The solution X of A * X = B: column j of X solves the system for column j of B.</summary>
        /// <exception cref="ArgumentException"><paramref name="b"/> has not as many rows as the matrix.</exception>
        /// <exception cref="InvalidOperationException">
        /// The matrix is not square, is singular to working precision or has an entry that is NaN or infinite.
        /// </exception>
        public Matrix<double> Solve(Matrix<double> b)
        {
            matrix.CheckSquare("Solve");
            if (b.Rows != matrix.Rows)
                throw new ArgumentException($"The right-hand side has {b.Rows} rows, but the matrix has {matrix.Rows}.", nameof(b));

            return new Matrix<double>(matrix.Rows, b.Columns, new LuDecomposition(matrix).Solve(b.Entries, b.Columns));
        }

        /// <summary>
        /// The rank to working precision: the number of linearly independent rows. An entry counts
        /// as zero when elimination reduces it to the rounding error of the terms it came from.
        /// </summary>
        /// <exception cref="InvalidOperationException">The matrix has an entry that is NaN or infinite.</exception>
        public int Rank() => new LuDecomposition(matrix).Rank;

        /// <summary>The entry-wise sum.</summary>
        /// <exception cref="ArgumentException">The matrices have different sizes.</exception>
        public static Matrix<double> operator +(Matrix<double> left, Matrix<double> right) =>
            left.Combine(right, (a, b) => a + b, "add");

        /// <summary>The entry-wise difference.</summary>
        /// <exception cref="ArgumentException">The matrices have different sizes.</exception>
        public static Matrix<double> operator -(Matrix<double> left, Matrix<double> right) =>
            left.Combine(right, (a, b) => a - b, "subtract");

        /// <summary>Every entry negated.</summary>
        public static Matrix<double> operator -(Matrix<double> value) => value.Map(a => -a);

        /// <summary>The matrix product.</summary>
        /// <exception cref="ArgumentException">The left matrix has not as many columns as the right one has rows.</exception>
        public static Matrix<double> operator *(Matrix<double> left, Matrix<double> right)
        {
            Matrix<double>.CheckProduct(left, right);
            return new Matrix<double>(left.Rows, right.Columns, Product(left, right.Entries, right.Columns));
        }

        /// <summary>The product of a matrix and a column vector.</summary>
        /// <exception cref="ArgumentException">The vector's length differs from the number of columns.</exception>
        public static Vector<double> operator *(Matrix<double> left, Vector<double> right)
        {
            Matrix<double>.CheckProduct(left, right);
            return new Vector<double>(Product(left, right.ToMatrix().Entries, 1));
        }

        /// <summary>Every entry multiplied by the number.</summary>
        public static Matrix<double> operator *(double scalar, Matrix<double> value) => value.Map(a => scalar * a);

        /// <summary>Every entry multiplied by the number.</summary>
        public static Matrix<double> operator *(Matrix<double> value, double scalar) => value.Map(a => a * scalar);

        /// <summary>Every entry divided by the number.</summary>
        public static Matrix<double> operator /(Matrix<double> value, double scalar) => value.Map(a => a / scalar);
    }

    // The row-major entries of left * right, where right has the given number of columns.
    private static double[] Product(Matrix<double> left, ReadOnlySpan<double> right, int rightColumns)
    {
        ReadOnlySpan<double> l = left.Entries;
        int inner = left.Columns;
        var result = new double[left.Rows * rightColumns];

        // i-k-j order walks both right and result along their rows.
        for (int i = 0; i < left.Rows; i++)
            for (int k = 0; k < inner; k++)
            {
                double a = l[i * inner + k];
                for (int j = 0; j < rightColumns; j++)
                    result[i * rightColumns + j] += a * right[k * rightColumns + j];
            }

        return result;
    }
}
