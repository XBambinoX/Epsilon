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
            matrix.CheckSquare("The trace");
            return Sum(matrix.Rows, i => matrix[i, i]);
        }

        /// <summary>
        /// The determinant, expanded. Berkowitz's algorithm computes it without dividing, so it
        /// holds wherever the entries are defined.
        /// </summary>
        /// <exception cref="InvalidOperationException">The matrix is not square.</exception>
        public Expr Determinant()
        {
            matrix.CheckSquare("The determinant");
            return Determinant(matrix, Berkowitz.Coefficients(matrix));
        }

        /// <summary>
        /// The characteristic polynomial det(t I - A) in the variable <paramref name="variable"/>:
        /// t^n, then the lower powers with expanded coefficients. Its roots are the eigenvalues.
        /// </summary>
        /// <exception cref="InvalidOperationException">The matrix is not square.</exception>
        /// <exception cref="ArgumentException">The variable occurs in an entry of the matrix.</exception>
        public Expr CharacteristicPolynomial(string variable)
        {
            matrix.CheckSquare("The characteristic polynomial");
            ArgumentException.ThrowIfNullOrEmpty(variable);
            for (int i = 0; i < matrix.Rows; i++)
                for (int j = 0; j < matrix.Columns; j++)
                    if (matrix[i, j].DependsOn(variable))
                        throw new ArgumentException($"The variable '{variable}' occurs in the entry at ({i}, {j}).", nameof(variable));

            Expr[] coefficients = Berkowitz.Coefficients(matrix);
            var t = new Variable(variable);
            int n = matrix.Rows;

            Expr polynomial = coefficients[n];
            for (int k = n - 1; k >= 0; k--)
                polynomial = coefficients[k] * t.Pow(n - k) + polynomial;

            return polynomial.Simplify();
        }

        /// <summary>
        /// The adjugate (classical adjoint), the transposed matrix of cofactors: adj(A) * A =
        /// det(A) * I. Expanded and computed without dividing, so it exists for singular matrices too.
        /// </summary>
        /// <exception cref="InvalidOperationException">The matrix is not square.</exception>
        public Matrix<Expr> Adjugate()
        {
            matrix.CheckSquare("The adjugate");
            return Berkowitz.Adjugate(matrix, Berkowitz.Coefficients(matrix));
        }

        /// <summary>
        /// The inverse adj(A) / det(A), every entry simplified. With symbols it is undefined exactly
        /// where the matrix is singular: [[x, 1], [1, x]] has an inverse at x = 0 as well.
        /// </summary>
        /// <exception cref="InvalidOperationException">The matrix is not square, or its determinant is 0.</exception>
        public Matrix<Expr> Inverse()
        {
            matrix.CheckSquare("The inverse");

            var (adjugate, determinant) = AdjugateAndDeterminant(matrix);
            return adjugate.Map(entry => (entry / determinant).Simplify());
        }

        /// <summary>
        /// The solution x of A * x = b by Cramer's rule, x = adj(A) * b / det(A), every entry
        /// simplified. Like <see cref="Inverse"/>, it is undefined exactly where A is singular.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="b"/> has not as many entries as the matrix has rows.</exception>
        /// <exception cref="InvalidOperationException">The matrix is not square, or its determinant is 0.</exception>
        public Vector<Expr> Solve(Vector<Expr> b)
        {
            matrix.CheckSquare("Solve");
            if (b.Length != matrix.Rows)
                throw new ArgumentException($"The right-hand side has {b.Length} entries, but the matrix has {matrix.Rows} rows.", nameof(b));

            return new Vector<Expr>(SolveCramer(matrix, b.ToMatrix()));
        }

        /// <summary>The solution X of A * X = B: column j of X solves the system for column j of B.</summary>
        /// <exception cref="ArgumentException"><paramref name="b"/> has not as many rows as the matrix.</exception>
        /// <exception cref="InvalidOperationException">The matrix is not square, or its determinant is 0.</exception>
        public Matrix<Expr> Solve(Matrix<Expr> b)
        {
            matrix.CheckSquare("Solve");
            if (b.Rows != matrix.Rows)
                throw new ArgumentException($"The right-hand side has {b.Rows} rows, but the matrix has {matrix.Rows}.", nameof(b));

            return SolveCramer(matrix, b);
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

    // det(A) = (-1)^n c[n] for det(t I - A) = t^n + c[1] t^(n-1) + ... + c[n].
    private static Expr Determinant(Matrix<Expr> matrix, Expr[] coefficients) =>
        matrix.Rows % 2 == 0 ? coefficients[^1] : (-coefficients[^1]).Expand();

    // Both from one characteristic polynomial; the determinant must not be 0.
    private static (Matrix<Expr> Adjugate, Expr Determinant) AdjugateAndDeterminant(Matrix<Expr> matrix)
    {
        Expr[] coefficients = Berkowitz.Coefficients(matrix);
        Expr determinant = Determinant(matrix, coefficients);
        if (determinant is Constant { Value.IsZero: true })
            throw new InvalidOperationException("The matrix is singular: its determinant is 0.");

        return (Berkowitz.Adjugate(matrix, coefficients), determinant);
    }

    private static Matrix<Expr> SolveCramer(Matrix<Expr> matrix, Matrix<Expr> b)
    {
        var (adjugate, determinant) = AdjugateAndDeterminant(matrix);
        return Matrix<Expr>.Create(matrix.Rows, b.Columns,
            (i, j) => (Berkowitz.Sum(matrix.Rows, k => adjugate[i, k] * b[k, j]) / determinant).Simplify());
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
