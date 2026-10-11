namespace Epsilon.LinearAlgebra;

/// <summary>
/// An immutable 3 x 3 matrix, a value type for linear maps of space (rotations, scaling) and for
/// affine maps of the plane in homogeneous coordinates. Vectors are columns, as for
/// <see cref="Matrix{T}"/>: <c>m * v</c> maps v, and <c>a * b</c> applies b first. The entries
/// can be of any number type with the arithmetic operators; the inverse and rotations are in
/// <see cref="FloatingPointMatrix3x3Extensions"/> for float, double and Half.
/// </summary>
/// <typeparam name="T">The type of the entries.</typeparam>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public readonly struct Matrix3x3<T> : IEquatable<Matrix3x3<T>>
    where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.ISubtractionOperators<T, T, T>,
              System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IDivisionOperators<T, T, T>,
              System.Numerics.IUnaryNegationOperators<T, T>,
              System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplicativeIdentity<T, T>
{
    /// <summary>The matrix with the given entries, row by row: Mij is in row i and column j.</summary>
    public Matrix3x3(T m11, T m12, T m13, T m21, T m22, T m23, T m31, T m32, T m33)
    {
        M11 = m11; M12 = m12; M13 = m13;
        M21 = m21; M22 = m22; M23 = m23;
        M31 = m31; M32 = m32; M33 = m33;
    }

    /// <summary>The entry in row 1, column 1.</summary>
    public T M11 { get; init; }
    /// <summary>The entry in row 1, column 2.</summary>
    public T M12 { get; init; }
    /// <summary>The entry in row 1, column 3.</summary>
    public T M13 { get; init; }
    /// <summary>The entry in row 2, column 1.</summary>
    public T M21 { get; init; }
    /// <summary>The entry in row 2, column 2.</summary>
    public T M22 { get; init; }
    /// <summary>The entry in row 2, column 3.</summary>
    public T M23 { get; init; }
    /// <summary>The entry in row 3, column 1.</summary>
    public T M31 { get; init; }
    /// <summary>The entry in row 3, column 2.</summary>
    public T M32 { get; init; }
    /// <summary>The entry in row 3, column 3.</summary>
    public T M33 { get; init; }

    /// <summary>The identity matrix, which maps every vector to itself.</summary>
    public static Matrix3x3<T> Identity => Scale(Vector3<T>.One);

    /// <summary>The zero matrix.</summary>
    public static Matrix3x3<T> Zero => FromRows(Vector3<T>.Zero, Vector3<T>.Zero, Vector3<T>.Zero);

    /// <summary>The matrix with the given rows.</summary>
    public static Matrix3x3<T> FromRows(Vector3<T> first, Vector3<T> second, Vector3<T> third) =>
        new(first.X, first.Y, first.Z, second.X, second.Y, second.Z, third.X, third.Y, third.Z);

    /// <summary>
    /// The matrix with the given columns, which are the images of the unit vectors: a basis
    /// <c>FromColumns(right, up, forward)</c> maps <c>UnitX</c> to <c>right</c>.
    /// </summary>
    public static Matrix3x3<T> FromColumns(Vector3<T> first, Vector3<T> second, Vector3<T> third) =>
        new(first.X, second.X, third.X, first.Y, second.Y, third.Y, first.Z, second.Z, third.Z);

    /// <summary>The scaling of space by the factors along the axes: diag(x, y, z).</summary>
    public static Matrix3x3<T> Scale(Vector3<T> factors)
    {
        T zero = T.AdditiveIdentity;
        return new(factors.X, zero, zero, zero, factors.Y, zero, zero, zero, factors.Z);
    }

    /// <summary>
    /// The scaling of the plane by the factors along the axes in homogeneous coordinates:
    /// diag(x, y, 1), for <see cref="TransformPoint"/>.
    /// </summary>
    public static Matrix3x3<T> Scale(Vector2<T> factors) => Scale(new Vector3<T>(factors, T.MultiplicativeIdentity));

    /// <summary>
    /// The translation of the plane by <paramref name="offset"/> in homogeneous coordinates: the
    /// identity with the offset in the last column, for <see cref="TransformPoint"/>.
    /// </summary>
    public static Matrix3x3<T> Translation(Vector2<T> offset)
    {
        T zero = T.AdditiveIdentity, one = T.MultiplicativeIdentity;
        return new(one, zero, offset.X, zero, one, offset.Y, zero, zero, one);
    }

    /// <summary>The entry at zero-based indices: <c>m[0, 1]</c> is <see cref="M12"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An index is not 0, 1 or 2.</exception>
    public T this[int row, int column]
    {
        get
        {
            if ((uint)row >= 3)
                throw new ArgumentOutOfRangeException(nameof(row), row, "The matrix has 3 rows.");
            if ((uint)column >= 3)
                throw new ArgumentOutOfRangeException(nameof(column), column, "The matrix has 3 columns.");

            return Row(row)[column];
        }
    }

    /// <summary>The row at the zero-based index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not 0, 1 or 2.</exception>
    public Vector3<T> Row(int index) => index switch
    {
        0 => new(M11, M12, M13),
        1 => new(M21, M22, M23),
        2 => new(M31, M32, M33),
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The matrix has 3 rows."),
    };

    /// <summary>The column at the zero-based index: the image of the unit vector along that axis.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not 0, 1 or 2.</exception>
    public Vector3<T> Column(int index) => index switch
    {
        0 => new(M11, M21, M31),
        1 => new(M12, M22, M32),
        2 => new(M13, M23, M33),
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The matrix has 3 columns."),
    };

    /// <summary>The transpose: rows and columns swapped. For a rotation it is the inverse.</summary>
    public Matrix3x3<T> Transpose() => new(M11, M21, M31, M12, M22, M32, M13, M23, M33);

    /// <summary>The sum of the diagonal entries.</summary>
    public T Trace() => M11 + M22 + M33;

    /// <summary>
    /// The determinant by the closed formula, exact for integers. For floating-point entries
    /// rounding can leave a tiny value instead of 0; <c>TryInverse</c> tells whether the matrix
    /// is singular to working precision.
    /// </summary>
    public T Determinant() =>
        M11 * (M22 * M33 - M23 * M32) - M12 * (M21 * M33 - M23 * M31) + M13 * (M21 * M32 - M22 * M31);

    /// <summary>
    /// The point of the plane mapped as in homogeneous coordinates: (x, y, 1) times the matrix,
    /// divided by the last entry w unless w is 1, as after a projective map. With a translation
    /// it moves the point.
    /// </summary>
    public Vector2<T> TransformPoint(Vector2<T> point)
    {
        T x = M11 * point.X + M12 * point.Y + M13;
        T y = M21 * point.X + M22 * point.Y + M23;
        T w = M31 * point.X + M32 * point.Y + M33;
        return EqualityComparer<T>.Default.Equals(w, T.MultiplicativeIdentity) ? new(x, y) : new(x / w, y / w);
    }

    /// <summary>
    /// The direction of the plane mapped as in homogeneous coordinates: (x, y, 0) times the
    /// matrix, so a translation leaves it alone.
    /// </summary>
    public Vector2<T> TransformDirection(Vector2<T> direction) =>
        new(M11 * direction.X + M12 * direction.Y, M21 * direction.X + M22 * direction.Y);

    /// <summary>The matrix with <paramref name="map"/> applied to every entry; it also converts them: <c>m.Map(float.CreateTruncating)</c>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
    public Matrix3x3<TResult> Map<TResult>(Func<T, TResult> map)
        where TResult : System.Numerics.IAdditionOperators<TResult, TResult, TResult>, System.Numerics.ISubtractionOperators<TResult, TResult, TResult>,
                        System.Numerics.IMultiplyOperators<TResult, TResult, TResult>, System.Numerics.IDivisionOperators<TResult, TResult, TResult>,
                        System.Numerics.IUnaryNegationOperators<TResult, TResult>,
                        System.Numerics.IAdditiveIdentity<TResult, TResult>, System.Numerics.IMultiplicativeIdentity<TResult, TResult>
    {
        ArgumentNullException.ThrowIfNull(map);
        return new(map(M11), map(M12), map(M13), map(M21), map(M22), map(M23), map(M31), map(M32), map(M33));
    }

    /// <summary>The entry-wise sum.</summary>
    public static Matrix3x3<T> operator +(Matrix3x3<T> left, Matrix3x3<T> right) => new(
        left.M11 + right.M11, left.M12 + right.M12, left.M13 + right.M13,
        left.M21 + right.M21, left.M22 + right.M22, left.M23 + right.M23,
        left.M31 + right.M31, left.M32 + right.M32, left.M33 + right.M33);

    /// <summary>The entry-wise difference.</summary>
    public static Matrix3x3<T> operator -(Matrix3x3<T> left, Matrix3x3<T> right) => new(
        left.M11 - right.M11, left.M12 - right.M12, left.M13 - right.M13,
        left.M21 - right.M21, left.M22 - right.M22, left.M23 - right.M23,
        left.M31 - right.M31, left.M32 - right.M32, left.M33 - right.M33);

    /// <summary>Every entry negated.</summary>
    public static Matrix3x3<T> operator -(Matrix3x3<T> value) => new(
        -value.M11, -value.M12, -value.M13,
        -value.M21, -value.M22, -value.M23,
        -value.M31, -value.M32, -value.M33);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Matrix3x3<T> operator *(T scalar, Matrix3x3<T> value) => new(
        scalar * value.M11, scalar * value.M12, scalar * value.M13,
        scalar * value.M21, scalar * value.M22, scalar * value.M23,
        scalar * value.M31, scalar * value.M32, scalar * value.M33);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Matrix3x3<T> operator *(Matrix3x3<T> value, T scalar) => new(
        value.M11 * scalar, value.M12 * scalar, value.M13 * scalar,
        value.M21 * scalar, value.M22 * scalar, value.M23 * scalar,
        value.M31 * scalar, value.M32 * scalar, value.M33 * scalar);

    /// <summary>Every entry divided by the number; for integer entries the quotients are truncated, as in C#.</summary>
    public static Matrix3x3<T> operator /(Matrix3x3<T> value, T scalar) => new(
        value.M11 / scalar, value.M12 / scalar, value.M13 / scalar,
        value.M21 / scalar, value.M22 / scalar, value.M23 / scalar,
        value.M31 / scalar, value.M32 / scalar, value.M33 / scalar);

    /// <summary>The matrix product, the map that applies <paramref name="right"/> first and then <paramref name="left"/>.</summary>
    public static Matrix3x3<T> operator *(Matrix3x3<T> left, Matrix3x3<T> right) => new(
        left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31,
        left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32,
        left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33,
        left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31,
        left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32,
        left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33,
        left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31,
        left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32,
        left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33);

    /// <summary>The vector mapped by the matrix: the product with the column vector.</summary>
    public static Vector3<T> operator *(Matrix3x3<T> matrix, Vector3<T> vector) => new(
        matrix.M11 * vector.X + matrix.M12 * vector.Y + matrix.M13 * vector.Z,
        matrix.M21 * vector.X + matrix.M22 * vector.Y + matrix.M23 * vector.Z,
        matrix.M31 * vector.X + matrix.M32 * vector.Y + matrix.M33 * vector.Z);

    /// <summary>
    /// Whether the entries are equal. They are compared with their <c>Equals</c>, so NaN equals
    /// NaN, as for <see cref="Matrix{T}"/>, and equal matrices have equal hash codes.
    /// </summary>
    public bool Equals(Matrix3x3<T> other) => Row(0) == other.Row(0) && Row(1) == other.Row(1) && Row(2) == other.Row(2);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Matrix3x3<T> other && Equals(other);

    /// <summary>A hash of the entries, consistent with <see cref="Equals(Matrix3x3{T})"/>.</summary>
    public override int GetHashCode() => HashCode.Combine(Row(0), Row(1), Row(2));

    /// <summary>Whether the matrices are equal.</summary>
    public static bool operator ==(Matrix3x3<T> left, Matrix3x3<T> right) => left.Equals(right);

    /// <summary>Whether the matrices are not equal.</summary>
    public static bool operator !=(Matrix3x3<T> left, Matrix3x3<T> right) => !left.Equals(right);

    /// <summary>The rows in brackets, culture-invariant, as for <see cref="Matrix{T}"/>: <c>[[1, 0, 0], [0, 1, 0], [0, 0, 1]]</c>.</summary>
    public override string ToString() => $"[{Row(0)}, {Row(1)}, {Row(2)}]";
}
