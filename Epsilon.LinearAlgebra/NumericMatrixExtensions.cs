namespace Epsilon.LinearAlgebra;

/// <summary>Arithmetic on matrices and vectors of <see cref="double"/>.</summary>
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
            matrix.CheckSquare("trace");

            double sum = 0;
            for (int i = 0; i < matrix.Rows; i++)
                sum += matrix[i, i];

            return sum;
        }

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

    extension(Vector<double> vector)
    {
        /// <summary>The vector of <paramref name="length"/> zeros.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        public static Vector<double> Zero(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            return new Vector<double>(new double[length]);
        }

        /// <summary>The dot product: the sum of the products of the entries at the same index.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public double Dot(Vector<double> other)
        {
            vector.CheckSameLength(other, "multiply");

            double sum = 0;
            for (int i = 0; i < vector.Length; i++)
                sum += vector[i] * other[i];

            return sum;
        }

        /// <summary>The entry-wise sum.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<double> operator +(Vector<double> left, Vector<double> right) =>
            left.Combine(right, (a, b) => a + b, "add");

        /// <summary>The entry-wise difference.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<double> operator -(Vector<double> left, Vector<double> right) =>
            left.Combine(right, (a, b) => a - b, "subtract");

        /// <summary>Every entry negated.</summary>
        public static Vector<double> operator -(Vector<double> value) => value.Map(a => -a);

        /// <summary>Every entry multiplied by the number.</summary>
        public static Vector<double> operator *(double scalar, Vector<double> value) => value.Map(a => scalar * a);

        /// <summary>Every entry multiplied by the number.</summary>
        public static Vector<double> operator *(Vector<double> value, double scalar) => value.Map(a => a * scalar);

        /// <summary>Every entry divided by the number.</summary>
        public static Vector<double> operator /(Vector<double> value, double scalar) => value.Map(a => a / scalar);
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
