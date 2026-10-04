using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

/// <summary>
/// Arithmetic on matrices and vectors of expressions. Unlike the operators of <see cref="Expr"/>,
/// which only build a tree, these compute: every entry of the result is simplified.
/// </summary>
public static class SymbolicMatrixExtensions
{
    private static readonly Expr ZeroEntry = new Constant(0);
    private static readonly Expr OneEntry = new Constant(1);

    extension(Matrix<Expr> matrix)
    {
        /// <summary>The <paramref name="size"/> x <paramref name="size"/> identity matrix.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
        public static Matrix<Expr> Identity(int size) =>
            Matrix<Expr>.Create(size, size, (i, j) => i == j ? OneEntry : ZeroEntry);

        /// <summary>The <paramref name="rows"/> x <paramref name="columns"/> matrix of zeros.</summary>
        /// <exception cref="ArgumentOutOfRangeException">A dimension is negative.</exception>
        public static Matrix<Expr> Zero(int rows, int columns) =>
            Matrix<Expr>.Create(rows, columns, (_, _) => ZeroEntry);

        /// <summary>The simplified sum of the diagonal entries.</summary>
        /// <exception cref="InvalidOperationException">The matrix is not square.</exception>
        public Expr Trace()
        {
            matrix.CheckSquare("trace");
            return Sum(matrix.Rows, i => matrix[i, i]);
        }

        /// <summary>The entry-wise sum, simplified.</summary>
        /// <exception cref="ArgumentException">The matrices have different sizes.</exception>
        public static Matrix<Expr> operator +(Matrix<Expr> left, Matrix<Expr> right) =>
            left.Combine(right, (a, b) => (a + b).Simplify(), "add");

        /// <summary>The entry-wise difference, simplified.</summary>
        /// <exception cref="ArgumentException">The matrices have different sizes.</exception>
        public static Matrix<Expr> operator -(Matrix<Expr> left, Matrix<Expr> right) =>
            left.Combine(right, (a, b) => (a - b).Simplify(), "subtract");

        /// <summary>Every entry negated and simplified.</summary>
        public static Matrix<Expr> operator -(Matrix<Expr> value) => value.Map(a => (-a).Simplify());

        /// <summary>The matrix product, simplified.</summary>
        /// <exception cref="ArgumentException">The left matrix has not as many columns as the right one has rows.</exception>
        public static Matrix<Expr> operator *(Matrix<Expr> left, Matrix<Expr> right)
        {
            Matrix<Expr>.CheckProduct(left, right);
            return Matrix<Expr>.Create(left.Rows, right.Columns,
                (i, j) => Sum(left.Columns, k => left[i, k] * right[k, j]));
        }

        /// <summary>The product of a matrix and a column vector, simplified.</summary>
        /// <exception cref="ArgumentException">The vector's length differs from the number of columns.</exception>
        public static Vector<Expr> operator *(Matrix<Expr> left, Vector<Expr> right)
        {
            Matrix<Expr>.CheckProduct(left, right);
            return new Vector<Expr>(left * right.ToMatrix());
        }

        /// <summary>Every entry multiplied by the expression and simplified.</summary>
        public static Matrix<Expr> operator *(Expr scalar, Matrix<Expr> value) => value.Map(a => (scalar * a).Simplify());

        /// <summary>Every entry multiplied by the expression and simplified.</summary>
        public static Matrix<Expr> operator *(Matrix<Expr> value, Expr scalar) => value.Map(a => (a * scalar).Simplify());

        /// <summary>Every entry divided by the expression and simplified.</summary>
        public static Matrix<Expr> operator /(Matrix<Expr> value, Expr scalar) => value.Map(a => (a / scalar).Simplify());
    }

    extension(Vector<Expr> vector)
    {
        /// <summary>The vector of <paramref name="length"/> zeros.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        public static Vector<Expr> Zero(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            return new Vector<Expr>(Enumerable.Repeat(ZeroEntry, length).ToArray());
        }

        /// <summary>The simplified dot product: the sum of the products of the entries at the same index.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public Expr Dot(Vector<Expr> other)
        {
            vector.CheckSameLength(other, "multiply");
            return Sum(vector.Length, i => vector[i] * other[i]);
        }

        /// <summary>The entry-wise sum, simplified.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<Expr> operator +(Vector<Expr> left, Vector<Expr> right) =>
            left.Combine(right, (a, b) => (a + b).Simplify(), "add");

        /// <summary>The entry-wise difference, simplified.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<Expr> operator -(Vector<Expr> left, Vector<Expr> right) =>
            left.Combine(right, (a, b) => (a - b).Simplify(), "subtract");

        /// <summary>Every entry negated and simplified.</summary>
        public static Vector<Expr> operator -(Vector<Expr> value) => value.Map(a => (-a).Simplify());

        /// <summary>Every entry multiplied by the expression and simplified.</summary>
        public static Vector<Expr> operator *(Expr scalar, Vector<Expr> value) => value.Map(a => (scalar * a).Simplify());

        /// <summary>Every entry multiplied by the expression and simplified.</summary>
        public static Vector<Expr> operator *(Vector<Expr> value, Expr scalar) => value.Map(a => (a * scalar).Simplify());

        /// <summary>Every entry divided by the expression and simplified.</summary>
        public static Vector<Expr> operator /(Vector<Expr> value, Expr scalar) => value.Map(a => (a / scalar).Simplify());
    }

    // The simplified sum of term(0), ..., term(count - 1); 0 when there are no terms.
    private static Expr Sum(int count, Func<int, Expr> term)
    {
        if (count == 0)
            return ZeroEntry;

        Expr sum = term(0);
        for (int k = 1; k < count; k++)
            sum += term(k);

        return sum.Simplify();
    }
}
