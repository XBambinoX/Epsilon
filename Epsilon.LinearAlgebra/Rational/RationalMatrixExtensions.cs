using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

/// <summary>
/// Exact arithmetic, powers, determinant, inverse, solve and rank of matrices of
/// <see cref="Rational"/>: no rounding, so a singular matrix is always recognized. Faster than a
/// matrix of expressions with constant entries.
/// </summary>
public static class RationalMatrixExtensions
{
    extension(Matrix<Rational> matrix)
    {
        /// <summary>The <paramref name="size"/> x <paramref name="size"/> identity matrix.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
        public static Matrix<Rational> Identity(int size) =>
            Matrix<Rational>.Create(size, size, (i, j) => i == j ? Rational.One : Rational.Zero);

        /// <summary>The <paramref name="rows"/> x <paramref name="columns"/> matrix of zeros.</summary>
        /// <exception cref="ArgumentOutOfRangeException">A dimension is negative.</exception>
        public static Matrix<Rational> Zero(int rows, int columns) =>
            Matrix<Rational>.Create(rows, columns, (_, _) => Rational.Zero);

        /// <summary>
        /// The square matrix with the entries of <paramref name="diagonal"/> on the main diagonal
        /// and zeros elsewhere: <c>Matrix&lt;Rational&gt;.FromDiagonal([1, 2, 3])</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="diagonal"/> is null.</exception>
        public static Matrix<Rational> FromDiagonal(Vector<Rational> diagonal)
        {
            ArgumentNullException.ThrowIfNull(diagonal);
            return Matrix<Rational>.Create(diagonal.Length, diagonal.Length, (i, j) => i == j ? diagonal[i] : Rational.Zero);
        }

        /// <summary>The sum of the diagonal entries.</summary>
        /// <exception cref="InvalidOperationException">The matrix is not square.</exception>
        public Rational Trace()
        {
            matrix.CheckSquare("The trace");

            Rational sum = Rational.Zero;
            for (int i = 0; i < matrix.Rows; i++)
                sum += matrix[i, i];

            return sum;
        }

        /// <summary>The exact determinant, by Gaussian elimination.</summary>
        /// <exception cref="InvalidOperationException">The matrix is not square.</exception>
        public Rational Determinant()
        {
            matrix.CheckSquare("The determinant");
            return RationalElimination.Determinant(matrix);
        }

        /// <summary>The exact inverse, by Gaussian elimination.</summary>
        /// <exception cref="InvalidOperationException">The matrix is not square or is singular.</exception>
        public Matrix<Rational> Inverse()
        {
            matrix.CheckSquare("The inverse");

            int n = matrix.Rows;
            return new Matrix<Rational>(n, n, RationalElimination.Solve(matrix, Matrix<Rational>.Identity(n).Entries, n));
        }

        /// <summary>The exact solution x of A * x = b, by Gaussian elimination.</summary>
        /// <exception cref="ArgumentException"><paramref name="b"/> has not as many entries as the matrix has rows.</exception>
        /// <exception cref="InvalidOperationException">The matrix is not square or is singular.</exception>
        public Vector<Rational> Solve(Vector<Rational> b)
        {
            matrix.CheckSquare("Solve");
            if (b.Length != matrix.Rows)
                throw new ArgumentException($"The right-hand side has {b.Length} entries, but the matrix has {matrix.Rows} rows.", nameof(b));

            return new Vector<Rational>(RationalElimination.Solve(matrix, b.ToMatrix().Entries, 1));
        }

        /// <summary>The exact solution X of A * X = B: column j of X solves the system for column j of B.</summary>
        /// <exception cref="ArgumentException"><paramref name="b"/> has not as many rows as the matrix.</exception>
        /// <exception cref="InvalidOperationException">The matrix is not square or is singular.</exception>
        public Matrix<Rational> Solve(Matrix<Rational> b)
        {
            matrix.CheckSquare("Solve");
            if (b.Rows != matrix.Rows)
                throw new ArgumentException($"The right-hand side has {b.Rows} rows, but the matrix has {matrix.Rows}.", nameof(b));

            return new Matrix<Rational>(matrix.Rows, b.Columns, RationalElimination.Solve(matrix, b.Entries, b.Columns));
        }

        /// <summary>The exact rank: the number of linearly independent rows, for any shape.</summary>
        public int Rank() => RationalElimination.Rank(matrix);

        /// <summary>
        /// The integer power A^n by repeated squaring: the identity for n = 0, and a power of the
        /// inverse for negative n.
        /// </summary>
        /// <exception cref="InvalidOperationException">The matrix is not square, or n is negative and the matrix is singular.</exception>
        public Matrix<Rational> Pow(int exponent)
        {
            matrix.CheckSquare("Pow");
            return Matrix<Rational>.Power(exponent < 0 ? matrix.Inverse() : matrix, exponent,
                Matrix<Rational>.Identity(matrix.Rows), (a, b) => a * b);
        }

        /// <summary>The Hadamard product: the products of the entries at the same position.</summary>
        /// <exception cref="ArgumentException">The matrices have different sizes.</exception>
        public Matrix<Rational> Hadamard(Matrix<Rational> other) =>
            matrix.Combine(other, (a, b) => a * b, "take the Hadamard product of");

        /// <summary>The entry-wise sum.</summary>
        /// <exception cref="ArgumentException">The matrices have different sizes.</exception>
        public static Matrix<Rational> operator +(Matrix<Rational> left, Matrix<Rational> right) =>
            left.Combine(right, (a, b) => a + b, "add");

        /// <summary>The entry-wise difference.</summary>
        /// <exception cref="ArgumentException">The matrices have different sizes.</exception>
        public static Matrix<Rational> operator -(Matrix<Rational> left, Matrix<Rational> right) =>
            left.Combine(right, (a, b) => a - b, "subtract");

        /// <summary>Every entry negated.</summary>
        public static Matrix<Rational> operator -(Matrix<Rational> value) => value.Map(a => -a);

        /// <summary>The matrix product.</summary>
        /// <exception cref="ArgumentException">The left matrix has not as many columns as the right one has rows.</exception>
        public static Matrix<Rational> operator *(Matrix<Rational> left, Matrix<Rational> right)
        {
            Matrix<Rational>.CheckProduct(left, right);
            return new Matrix<Rational>(left.Rows, right.Columns, Product(left, right.Entries, right.Columns));
        }

        /// <summary>The product of a matrix and a column vector.</summary>
        /// <exception cref="ArgumentException">The vector's length differs from the number of columns.</exception>
        public static Vector<Rational> operator *(Matrix<Rational> left, Vector<Rational> right)
        {
            Matrix<Rational>.CheckProduct(left, right);
            return new Vector<Rational>(Product(left, right.ToMatrix().Entries, 1));
        }

        /// <summary>Every entry multiplied by the number.</summary>
        public static Matrix<Rational> operator *(Rational scalar, Matrix<Rational> value) => value.Map(a => scalar * a);

        /// <summary>Every entry multiplied by the number.</summary>
        public static Matrix<Rational> operator *(Matrix<Rational> value, Rational scalar) => value.Map(a => a * scalar);

        /// <summary>Every entry divided by the number.</summary>
        /// <exception cref="DivideByZeroException"><paramref name="scalar"/> is 0.</exception>
        public static Matrix<Rational> operator /(Matrix<Rational> value, Rational scalar)
        {
            if (scalar.IsZero)
                throw new DivideByZeroException("Cannot divide a matrix by 0.");

            return value.Map(a => a / scalar);
        }
    }

    // The row-major entries of left * right, where right has the given number of columns.
    private static Rational[] Product(Matrix<Rational> left, ReadOnlySpan<Rational> right, int rightColumns)
    {
        ReadOnlySpan<Rational> l = left.Entries;
        int inner = left.Columns;

        // Filled explicitly: default(Rational) is not a valid zero.
        var result = new Rational[left.Rows * rightColumns];
        Array.Fill(result, Rational.Zero);

        // i-k-j order walks both right and result along their rows.
        for (int i = 0; i < left.Rows; i++)
            for (int k = 0; k < inner; k++)
            {
                Rational a = l[i * inner + k];
                if (a.IsZero)
                    continue;

                for (int j = 0; j < rightColumns; j++)
                    result[i * rightColumns + j] += a * right[k * rightColumns + j];
            }

        return result;
    }
}
