namespace Epsilon.LinearAlgebra;

/// <summary>
/// An immutable 2 x 2 matrix, a value type for linear maps of the plane: rotations, scaling,
/// shears. Vectors are columns, as for <see cref="Matrix{T}"/>: <c>m * v</c> maps v, and
/// <c>a * b</c> applies b first. For translations of the plane use <see cref="Matrix3x3{T}"/>.
/// The entries can be of any number type with the arithmetic operators; the inverse and the
/// rotation are in <see cref="FloatingPointMatrix2x2Extensions"/> for float, double and Half.
/// </summary>
/// <typeparam name="T">The type of the entries.</typeparam>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public readonly struct Matrix2x2<T> : IEquatable<Matrix2x2<T>>
    where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.ISubtractionOperators<T, T, T>,
              System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IDivisionOperators<T, T, T>,
              System.Numerics.IUnaryNegationOperators<T, T>,
              System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplicativeIdentity<T, T>
{
    /// <summary>The matrix with the given entries, row by row: Mij is in row i and column j.</summary>
    public Matrix2x2(T m11, T m12, T m21, T m22)
    {
        M11 = m11; M12 = m12;
        M21 = m21; M22 = m22;
    }

    /// <summary>The entry in row 1, column 1.</summary>
    public T M11 { get; init; }
    /// <summary>The entry in row 1, column 2.</summary>
    public T M12 { get; init; }
    /// <summary>The entry in row 2, column 1.</summary>
    public T M21 { get; init; }
    /// <summary>The entry in row 2, column 2.</summary>
    public T M22 { get; init; }

    /// <summary>The identity matrix, which maps every vector to itself.</summary>
    public static Matrix2x2<T> Identity => Scale(Vector2<T>.One);

    /// <summary>The zero matrix.</summary>
    public static Matrix2x2<T> Zero => FromRows(Vector2<T>.Zero, Vector2<T>.Zero);

    /// <summary>The matrix with the given rows.</summary>
    public static Matrix2x2<T> FromRows(Vector2<T> first, Vector2<T> second) => new(first.X, first.Y, second.X, second.Y);

    /// <summary>The matrix with the given columns, which are the images of the unit vectors.</summary>
    public static Matrix2x2<T> FromColumns(Vector2<T> first, Vector2<T> second) => new(first.X, second.X, first.Y, second.Y);

    /// <summary>The scaling of the plane by the factors along the axes: diag(x, y).</summary>
    public static Matrix2x2<T> Scale(Vector2<T> factors) => new(factors.X, T.AdditiveIdentity, T.AdditiveIdentity, factors.Y);

    /// <summary>The entry at zero-based indices: <c>m[0, 1]</c> is <see cref="M12"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An index is not 0 or 1.</exception>
    public T this[int row, int column]
    {
        get
        {
            if ((uint)row >= 2)
                throw new ArgumentOutOfRangeException(nameof(row), row, "The matrix has 2 rows.");
            if ((uint)column >= 2)
                throw new ArgumentOutOfRangeException(nameof(column), column, "The matrix has 2 columns.");

            return Row(row)[column];
        }
    }

    /// <summary>The row at the zero-based index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not 0 or 1.</exception>
    public Vector2<T> Row(int index) => index switch
    {
        0 => new(M11, M12),
        1 => new(M21, M22),
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The matrix has 2 rows."),
    };

    /// <summary>The column at the zero-based index: the image of the unit vector along that axis.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not 0 or 1.</exception>
    public Vector2<T> Column(int index) => index switch
    {
        0 => new(M11, M21),
        1 => new(M12, M22),
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The matrix has 2 columns."),
    };

    /// <summary>The transpose: rows and columns swapped. For a rotation it is the inverse.</summary>
    public Matrix2x2<T> Transpose() => new(M11, M21, M12, M22);

    /// <summary>The sum of the diagonal entries.</summary>
    public T Trace() => M11 + M22;

    /// <summary>
    /// The determinant M11 M22 - M12 M21, exact for integers: the factor by which the map scales
    /// areas, negative if it mirrors.
    /// </summary>
    public T Determinant() => M11 * M22 - M12 * M21;

    /// <summary>The matrix with <paramref name="map"/> applied to every entry; it also converts them: <c>m.Map(float.CreateTruncating)</c>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
    public Matrix2x2<TResult> Map<TResult>(Func<T, TResult> map)
        where TResult : System.Numerics.IAdditionOperators<TResult, TResult, TResult>, System.Numerics.ISubtractionOperators<TResult, TResult, TResult>,
                        System.Numerics.IMultiplyOperators<TResult, TResult, TResult>, System.Numerics.IDivisionOperators<TResult, TResult, TResult>,
                        System.Numerics.IUnaryNegationOperators<TResult, TResult>,
                        System.Numerics.IAdditiveIdentity<TResult, TResult>, System.Numerics.IMultiplicativeIdentity<TResult, TResult>
    {
        ArgumentNullException.ThrowIfNull(map);
        return new(map(M11), map(M12), map(M21), map(M22));
    }

    /// <summary>The entry-wise sum.</summary>
    public static Matrix2x2<T> operator +(Matrix2x2<T> left, Matrix2x2<T> right) =>
        new(left.M11 + right.M11, left.M12 + right.M12, left.M21 + right.M21, left.M22 + right.M22);

    /// <summary>The entry-wise difference.</summary>
    public static Matrix2x2<T> operator -(Matrix2x2<T> left, Matrix2x2<T> right) =>
        new(left.M11 - right.M11, left.M12 - right.M12, left.M21 - right.M21, left.M22 - right.M22);

    /// <summary>Every entry negated.</summary>
    public static Matrix2x2<T> operator -(Matrix2x2<T> value) => new(-value.M11, -value.M12, -value.M21, -value.M22);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Matrix2x2<T> operator *(T scalar, Matrix2x2<T> value) =>
        new(scalar * value.M11, scalar * value.M12, scalar * value.M21, scalar * value.M22);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Matrix2x2<T> operator *(Matrix2x2<T> value, T scalar) =>
        new(value.M11 * scalar, value.M12 * scalar, value.M21 * scalar, value.M22 * scalar);

    /// <summary>Every entry divided by the number; for integer entries the quotients are truncated, as in C#.</summary>
    public static Matrix2x2<T> operator /(Matrix2x2<T> value, T scalar) =>
        new(value.M11 / scalar, value.M12 / scalar, value.M21 / scalar, value.M22 / scalar);

    /// <summary>The matrix product, the map that applies <paramref name="right"/> first and then <paramref name="left"/>.</summary>
    public static Matrix2x2<T> operator *(Matrix2x2<T> left, Matrix2x2<T> right) => new(
        left.M11 * right.M11 + left.M12 * right.M21, left.M11 * right.M12 + left.M12 * right.M22,
        left.M21 * right.M11 + left.M22 * right.M21, left.M21 * right.M12 + left.M22 * right.M22);

    /// <summary>The vector mapped by the matrix: the product with the column vector.</summary>
    public static Vector2<T> operator *(Matrix2x2<T> matrix, Vector2<T> vector) => new(
        matrix.M11 * vector.X + matrix.M12 * vector.Y,
        matrix.M21 * vector.X + matrix.M22 * vector.Y);

    /// <summary>
    /// Whether the entries are equal. They are compared with their <c>Equals</c>, so NaN equals
    /// NaN, as for <see cref="Matrix{T}"/>, and equal matrices have equal hash codes.
    /// </summary>
    public bool Equals(Matrix2x2<T> other) => Row(0) == other.Row(0) && Row(1) == other.Row(1);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Matrix2x2<T> other && Equals(other);

    /// <summary>A hash of the entries, consistent with <see cref="Equals(Matrix2x2{T})"/>.</summary>
    public override int GetHashCode() => HashCode.Combine(M11, M12, M21, M22);

    /// <summary>Whether the matrices are equal.</summary>
    public static bool operator ==(Matrix2x2<T> left, Matrix2x2<T> right) => left.Equals(right);

    /// <summary>Whether the matrices are not equal.</summary>
    public static bool operator !=(Matrix2x2<T> left, Matrix2x2<T> right) => !left.Equals(right);

    /// <summary>The rows in brackets, culture-invariant, as for <see cref="Matrix{T}"/>: <c>[[1, 0], [0, 1]]</c>.</summary>
    public override string ToString() => $"[{Row(0)}, {Row(1)}]";
}
