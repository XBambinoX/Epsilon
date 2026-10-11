namespace Epsilon.LinearAlgebra;

/// <summary>
/// An immutable 4 x 4 matrix, a value type for affine and projective maps of space in
/// homogeneous coordinates: translations, rotations, scaling and their compositions. Vectors are
/// columns, as for <see cref="Matrix{T}"/>: <c>m * v</c> maps v, <c>a * b</c> applies b first, and
/// a translation is in the last column (<see cref="M14"/>, <see cref="M24"/>, <see cref="M34"/>).
/// The entries can be of any number type with the arithmetic operators; the inverse and
/// rotations are in <see cref="FloatingPointMatrix4x4Extensions"/> for float, double and Half.
/// </summary>
/// <typeparam name="T">The type of the entries.</typeparam>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public readonly struct Matrix4x4<T> : IEquatable<Matrix4x4<T>>
    where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.ISubtractionOperators<T, T, T>,
              System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IDivisionOperators<T, T, T>,
              System.Numerics.IUnaryNegationOperators<T, T>,
              System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplicativeIdentity<T, T>
{
    /// <summary>The matrix with the given entries, row by row: Mij is in row i and column j.</summary>
    public Matrix4x4(
        T m11, T m12, T m13, T m14,
        T m21, T m22, T m23, T m24,
        T m31, T m32, T m33, T m34,
        T m41, T m42, T m43, T m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }

    /// <summary>
    /// The map of space in homogeneous coordinates that applies the linear map
    /// <paramref name="linear"/>: it in the top left corner, 1 in the bottom right, zeros elsewhere.
    /// </summary>
    public Matrix4x4(Matrix3x3<T> linear) : this(
        linear.M11, linear.M12, linear.M13, T.AdditiveIdentity,
        linear.M21, linear.M22, linear.M23, T.AdditiveIdentity,
        linear.M31, linear.M32, linear.M33, T.AdditiveIdentity,
        T.AdditiveIdentity, T.AdditiveIdentity, T.AdditiveIdentity, T.MultiplicativeIdentity)
    {
    }

    /// <summary>The entry in row 1, column 1.</summary>
    public T M11 { get; init; }
    /// <summary>The entry in row 1, column 2.</summary>
    public T M12 { get; init; }
    /// <summary>The entry in row 1, column 3.</summary>
    public T M13 { get; init; }
    /// <summary>The entry in row 1, column 4: the x offset of a translation.</summary>
    public T M14 { get; init; }
    /// <summary>The entry in row 2, column 1.</summary>
    public T M21 { get; init; }
    /// <summary>The entry in row 2, column 2.</summary>
    public T M22 { get; init; }
    /// <summary>The entry in row 2, column 3.</summary>
    public T M23 { get; init; }
    /// <summary>The entry in row 2, column 4: the y offset of a translation.</summary>
    public T M24 { get; init; }
    /// <summary>The entry in row 3, column 1.</summary>
    public T M31 { get; init; }
    /// <summary>The entry in row 3, column 2.</summary>
    public T M32 { get; init; }
    /// <summary>The entry in row 3, column 3.</summary>
    public T M33 { get; init; }
    /// <summary>The entry in row 3, column 4: the z offset of a translation.</summary>
    public T M34 { get; init; }
    /// <summary>The entry in row 4, column 1.</summary>
    public T M41 { get; init; }
    /// <summary>The entry in row 4, column 2.</summary>
    public T M42 { get; init; }
    /// <summary>The entry in row 4, column 3.</summary>
    public T M43 { get; init; }
    /// <summary>The entry in row 4, column 4.</summary>
    public T M44 { get; init; }

    /// <summary>The identity matrix, which maps every vector to itself.</summary>
    public static Matrix4x4<T> Identity => new(Matrix3x3<T>.Identity);

    /// <summary>The zero matrix.</summary>
    public static Matrix4x4<T> Zero => FromRows(Vector4<T>.Zero, Vector4<T>.Zero, Vector4<T>.Zero, Vector4<T>.Zero);

    /// <summary>The matrix with the given rows.</summary>
    public static Matrix4x4<T> FromRows(Vector4<T> first, Vector4<T> second, Vector4<T> third, Vector4<T> fourth) => new(
        first.X, first.Y, first.Z, first.W,
        second.X, second.Y, second.Z, second.W,
        third.X, third.Y, third.Z, third.W,
        fourth.X, fourth.Y, fourth.Z, fourth.W);

    /// <summary>The matrix with the given columns, which are the images of the unit vectors.</summary>
    public static Matrix4x4<T> FromColumns(Vector4<T> first, Vector4<T> second, Vector4<T> third, Vector4<T> fourth) => new(
        first.X, second.X, third.X, fourth.X,
        first.Y, second.Y, third.Y, fourth.Y,
        first.Z, second.Z, third.Z, fourth.Z,
        first.W, second.W, third.W, fourth.W);

    /// <summary>The translation by <paramref name="offset"/>: the identity with the offset in the last column.</summary>
    public static Matrix4x4<T> Translation(Vector3<T> offset) =>
        Identity with { M14 = offset.X, M24 = offset.Y, M34 = offset.Z };

    /// <summary>The scaling by the factors along the axes: diag(x, y, z, 1).</summary>
    public static Matrix4x4<T> Scale(Vector3<T> factors) => new(Matrix3x3<T>.Scale(factors));

    /// <summary>The scaling by the same factor along every axis: diag(s, s, s, 1).</summary>
    public static Matrix4x4<T> Scale(T factor) => Scale(new Vector3<T>(factor, factor, factor));

    /// <summary>The entry at zero-based indices: <c>m[0, 3]</c> is <see cref="M14"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An index is not 0, 1, 2 or 3.</exception>
    public T this[int row, int column]
    {
        get
        {
            if ((uint)row >= 4)
                throw new ArgumentOutOfRangeException(nameof(row), row, "The matrix has 4 rows.");
            if ((uint)column >= 4)
                throw new ArgumentOutOfRangeException(nameof(column), column, "The matrix has 4 columns.");

            return Row(row)[column];
        }
    }

    /// <summary>The row at the zero-based index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not 0, 1, 2 or 3.</exception>
    public Vector4<T> Row(int index) => index switch
    {
        0 => new(M11, M12, M13, M14),
        1 => new(M21, M22, M23, M24),
        2 => new(M31, M32, M33, M34),
        3 => new(M41, M42, M43, M44),
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The matrix has 4 rows."),
    };

    /// <summary>The column at the zero-based index: the image of the unit vector along that axis.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not 0, 1, 2 or 3.</exception>
    public Vector4<T> Column(int index) => index switch
    {
        0 => new(M11, M21, M31, M41),
        1 => new(M12, M22, M32, M42),
        2 => new(M13, M23, M33, M43),
        3 => new(M14, M24, M34, M44),
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The matrix has 4 columns."),
    };

    /// <summary>The transpose: rows and columns swapped.</summary>
    public Matrix4x4<T> Transpose() => new(
        M11, M21, M31, M41,
        M12, M22, M32, M42,
        M13, M23, M33, M43,
        M14, M24, M34, M44);

    /// <summary>The sum of the diagonal entries.</summary>
    public T Trace() => M11 + M22 + M33 + M44;

    /// <summary>
    /// The determinant by the closed formula (Laplace expansion along the first two rows), exact
    /// for integers. For floating-point entries rounding can leave a tiny value instead of 0;
    /// <c>TryInverse</c> tells whether the matrix is singular to working precision.
    /// </summary>
    public T Determinant()
    {
        // The 2 x 2 minors of the first two rows (s) and of the last two (c).
        T s0 = M11 * M22 - M21 * M12, s1 = M11 * M23 - M21 * M13, s2 = M11 * M24 - M21 * M14;
        T s3 = M12 * M23 - M22 * M13, s4 = M12 * M24 - M22 * M14, s5 = M13 * M24 - M23 * M14;
        T c5 = M33 * M44 - M43 * M34, c4 = M32 * M44 - M42 * M34, c3 = M32 * M43 - M42 * M33;
        T c2 = M31 * M44 - M41 * M34, c1 = M31 * M43 - M41 * M33, c0 = M31 * M42 - M41 * M32;
        return s0 * c5 - s1 * c4 + s2 * c3 + s3 * c2 - s4 * c1 + s5 * c0;
    }

    /// <summary>
    /// The point mapped as in homogeneous coordinates: (x, y, z, 1) times the matrix, divided by
    /// the last entry w unless w is 1, as after a perspective projection. A translation moves it.
    /// </summary>
    public Vector3<T> TransformPoint(Vector3<T> point)
    {
        T x = M11 * point.X + M12 * point.Y + M13 * point.Z + M14;
        T y = M21 * point.X + M22 * point.Y + M23 * point.Z + M24;
        T z = M31 * point.X + M32 * point.Y + M33 * point.Z + M34;
        T w = M41 * point.X + M42 * point.Y + M43 * point.Z + M44;
        return EqualityComparer<T>.Default.Equals(w, T.MultiplicativeIdentity) ? new(x, y, z) : new(x / w, y / w, z / w);
    }

    /// <summary>
    /// The direction mapped as in homogeneous coordinates: (x, y, z, 0) times the matrix, so a
    /// translation leaves it alone. Normals need the inverse transpose instead once the map
    /// scales unevenly.
    /// </summary>
    public Vector3<T> TransformDirection(Vector3<T> direction) => new(
        M11 * direction.X + M12 * direction.Y + M13 * direction.Z,
        M21 * direction.X + M22 * direction.Y + M23 * direction.Z,
        M31 * direction.X + M32 * direction.Y + M33 * direction.Z);

    /// <summary>The matrix with <paramref name="map"/> applied to every entry; it also converts them: <c>m.Map(float.CreateTruncating)</c>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
    public Matrix4x4<TResult> Map<TResult>(Func<T, TResult> map)
        where TResult : System.Numerics.IAdditionOperators<TResult, TResult, TResult>, System.Numerics.ISubtractionOperators<TResult, TResult, TResult>,
                        System.Numerics.IMultiplyOperators<TResult, TResult, TResult>, System.Numerics.IDivisionOperators<TResult, TResult, TResult>,
                        System.Numerics.IUnaryNegationOperators<TResult, TResult>,
                        System.Numerics.IAdditiveIdentity<TResult, TResult>, System.Numerics.IMultiplicativeIdentity<TResult, TResult>
    {
        ArgumentNullException.ThrowIfNull(map);
        return Matrix4x4<TResult>.FromRows(Row(0).Map(map), Row(1).Map(map), Row(2).Map(map), Row(3).Map(map));
    }

    /// <summary>The entry-wise sum.</summary>
    public static Matrix4x4<T> operator +(Matrix4x4<T> left, Matrix4x4<T> right) => new(
        left.M11 + right.M11, left.M12 + right.M12, left.M13 + right.M13, left.M14 + right.M14,
        left.M21 + right.M21, left.M22 + right.M22, left.M23 + right.M23, left.M24 + right.M24,
        left.M31 + right.M31, left.M32 + right.M32, left.M33 + right.M33, left.M34 + right.M34,
        left.M41 + right.M41, left.M42 + right.M42, left.M43 + right.M43, left.M44 + right.M44);

    /// <summary>The entry-wise difference.</summary>
    public static Matrix4x4<T> operator -(Matrix4x4<T> left, Matrix4x4<T> right) => new(
        left.M11 - right.M11, left.M12 - right.M12, left.M13 - right.M13, left.M14 - right.M14,
        left.M21 - right.M21, left.M22 - right.M22, left.M23 - right.M23, left.M24 - right.M24,
        left.M31 - right.M31, left.M32 - right.M32, left.M33 - right.M33, left.M34 - right.M34,
        left.M41 - right.M41, left.M42 - right.M42, left.M43 - right.M43, left.M44 - right.M44);

    /// <summary>Every entry negated.</summary>
    public static Matrix4x4<T> operator -(Matrix4x4<T> value) => new(
        -value.M11, -value.M12, -value.M13, -value.M14,
        -value.M21, -value.M22, -value.M23, -value.M24,
        -value.M31, -value.M32, -value.M33, -value.M34,
        -value.M41, -value.M42, -value.M43, -value.M44);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Matrix4x4<T> operator *(T scalar, Matrix4x4<T> value) => new(
        scalar * value.M11, scalar * value.M12, scalar * value.M13, scalar * value.M14,
        scalar * value.M21, scalar * value.M22, scalar * value.M23, scalar * value.M24,
        scalar * value.M31, scalar * value.M32, scalar * value.M33, scalar * value.M34,
        scalar * value.M41, scalar * value.M42, scalar * value.M43, scalar * value.M44);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Matrix4x4<T> operator *(Matrix4x4<T> value, T scalar) => new(
        value.M11 * scalar, value.M12 * scalar, value.M13 * scalar, value.M14 * scalar,
        value.M21 * scalar, value.M22 * scalar, value.M23 * scalar, value.M24 * scalar,
        value.M31 * scalar, value.M32 * scalar, value.M33 * scalar, value.M34 * scalar,
        value.M41 * scalar, value.M42 * scalar, value.M43 * scalar, value.M44 * scalar);

    /// <summary>Every entry divided by the number; for integer entries the quotients are truncated, as in C#.</summary>
    public static Matrix4x4<T> operator /(Matrix4x4<T> value, T scalar) => new(
        value.M11 / scalar, value.M12 / scalar, value.M13 / scalar, value.M14 / scalar,
        value.M21 / scalar, value.M22 / scalar, value.M23 / scalar, value.M24 / scalar,
        value.M31 / scalar, value.M32 / scalar, value.M33 / scalar, value.M34 / scalar,
        value.M41 / scalar, value.M42 / scalar, value.M43 / scalar, value.M44 / scalar);

    /// <summary>The matrix product, the map that applies <paramref name="right"/> first and then <paramref name="left"/>.</summary>
    public static Matrix4x4<T> operator *(Matrix4x4<T> left, Matrix4x4<T> right) => new(
        left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31 + left.M14 * right.M41,
        left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32 + left.M14 * right.M42,
        left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33 + left.M14 * right.M43,
        left.M11 * right.M14 + left.M12 * right.M24 + left.M13 * right.M34 + left.M14 * right.M44,
        left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31 + left.M24 * right.M41,
        left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32 + left.M24 * right.M42,
        left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33 + left.M24 * right.M43,
        left.M21 * right.M14 + left.M22 * right.M24 + left.M23 * right.M34 + left.M24 * right.M44,
        left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31 + left.M34 * right.M41,
        left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32 + left.M34 * right.M42,
        left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33 + left.M34 * right.M43,
        left.M31 * right.M14 + left.M32 * right.M24 + left.M33 * right.M34 + left.M34 * right.M44,
        left.M41 * right.M11 + left.M42 * right.M21 + left.M43 * right.M31 + left.M44 * right.M41,
        left.M41 * right.M12 + left.M42 * right.M22 + left.M43 * right.M32 + left.M44 * right.M42,
        left.M41 * right.M13 + left.M42 * right.M23 + left.M43 * right.M33 + left.M44 * right.M43,
        left.M41 * right.M14 + left.M42 * right.M24 + left.M43 * right.M34 + left.M44 * right.M44);

    /// <summary>The vector mapped by the matrix: the product with the column vector.</summary>
    public static Vector4<T> operator *(Matrix4x4<T> matrix, Vector4<T> vector) => new(
        matrix.M11 * vector.X + matrix.M12 * vector.Y + matrix.M13 * vector.Z + matrix.M14 * vector.W,
        matrix.M21 * vector.X + matrix.M22 * vector.Y + matrix.M23 * vector.Z + matrix.M24 * vector.W,
        matrix.M31 * vector.X + matrix.M32 * vector.Y + matrix.M33 * vector.Z + matrix.M34 * vector.W,
        matrix.M41 * vector.X + matrix.M42 * vector.Y + matrix.M43 * vector.Z + matrix.M44 * vector.W);

    /// <summary>
    /// Whether the entries are equal. They are compared with their <c>Equals</c>, so NaN equals
    /// NaN, as for <see cref="Matrix{T}"/>, and equal matrices have equal hash codes.
    /// </summary>
    public bool Equals(Matrix4x4<T> other) =>
        Row(0) == other.Row(0) && Row(1) == other.Row(1) && Row(2) == other.Row(2) && Row(3) == other.Row(3);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Matrix4x4<T> other && Equals(other);

    /// <summary>A hash of the entries, consistent with <see cref="Equals(Matrix4x4{T})"/>.</summary>
    public override int GetHashCode() => HashCode.Combine(Row(0), Row(1), Row(2), Row(3));

    /// <summary>Whether the matrices are equal.</summary>
    public static bool operator ==(Matrix4x4<T> left, Matrix4x4<T> right) => left.Equals(right);

    /// <summary>Whether the matrices are not equal.</summary>
    public static bool operator !=(Matrix4x4<T> left, Matrix4x4<T> right) => !left.Equals(right);

    /// <summary>The rows in brackets, culture-invariant, as for <see cref="Matrix{T}"/>: <c>[[1, 0, 0, 0], [0, 1, 0, 0], ...]</c>.</summary>
    public override string ToString() => $"[{Row(0)}, {Row(1)}, {Row(2)}, {Row(3)}]";
}
