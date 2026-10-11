namespace Epsilon.LinearAlgebra;

/// <summary>
/// An immutable vector of three numbers, a value type for geometry in space: positions,
/// directions, velocities. The entries can be of any number type with the arithmetic operators,
/// such as float, double, Half, decimal or int; norms, angles and the other operations that need
/// square roots are in <see cref="FloatingPointVectorExtensions"/> for float, double and Half.
/// A tuple converts to it: <c>Vector3&lt;double&gt; v = (1, 2, 3);</c>
/// </summary>
/// <typeparam name="T">The type of the entries.</typeparam>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public readonly struct Vector3<T> : IEquatable<Vector3<T>>
    where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.ISubtractionOperators<T, T, T>,
              System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IDivisionOperators<T, T, T>,
              System.Numerics.IUnaryNegationOperators<T, T>,
              System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplicativeIdentity<T, T>
{
    /// <summary>The vector with the given entries.</summary>
    public Vector3(T x, T y, T z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>The vector with the entries of <paramref name="xy"/> followed by <paramref name="z"/>.</summary>
    public Vector3(Vector2<T> xy, T z) : this(xy.X, xy.Y, z)
    {
    }

    /// <summary>The first entry.</summary>
    public T X { get; init; }

    /// <summary>The second entry.</summary>
    public T Y { get; init; }

    /// <summary>The third entry.</summary>
    public T Z { get; init; }

    /// <summary>The zero vector.</summary>
    public static Vector3<T> Zero => new(T.AdditiveIdentity, T.AdditiveIdentity, T.AdditiveIdentity);

    /// <summary>The vector of ones, which scales nothing in <see cref="Hadamard"/>.</summary>
    public static Vector3<T> One => new(T.MultiplicativeIdentity, T.MultiplicativeIdentity, T.MultiplicativeIdentity);

    /// <summary>The unit vector (1, 0, 0) along the x axis.</summary>
    public static Vector3<T> UnitX => new(T.MultiplicativeIdentity, T.AdditiveIdentity, T.AdditiveIdentity);

    /// <summary>The unit vector (0, 1, 0) along the y axis.</summary>
    public static Vector3<T> UnitY => new(T.AdditiveIdentity, T.MultiplicativeIdentity, T.AdditiveIdentity);

    /// <summary>The unit vector (0, 0, 1) along the z axis.</summary>
    public static Vector3<T> UnitZ => new(T.AdditiveIdentity, T.AdditiveIdentity, T.MultiplicativeIdentity);

    /// <summary>The entry at the zero-based index: 0 is <see cref="X"/>, 1 is <see cref="Y"/>, 2 is <see cref="Z"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not 0, 1 or 2.</exception>
    public T this[int index] => index switch
    {
        0 => X,
        1 => Y,
        2 => Z,
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The vector has 3 entries."),
    };

    /// <summary>The first two entries.</summary>
    public Vector2<T> XY => new(X, Y);

    /// <summary>The dot product: the sum of the products of the entries at the same index.</summary>
    public T Dot(Vector3<T> other) => X * other.X + Y * other.Y + Z * other.Z;

    /// <summary>The squared norm v . v; it needs no square root, so every number type has it.</summary>
    public T NormSquared() => Dot(this);

    /// <summary>
    /// The cross product, perpendicular to both vectors, with the area of the parallelogram they
    /// span as its norm: <c>UnitX.Cross(UnitY)</c> is <see cref="UnitZ"/>.
    /// </summary>
    public Vector3<T> Cross(Vector3<T> other) =>
        new(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);

    /// <summary>The Hadamard product: the products of the entries at the same index, as when scaling by a vector.</summary>
    public Vector3<T> Hadamard(Vector3<T> other) => new(X * other.X, Y * other.Y, Z * other.Z);

    /// <summary>
    /// The point at <paramref name="t"/> on the line through two points: (1 - t) * start + t * end,
    /// so <paramref name="start"/> at t = 0 and <paramref name="end"/> at t = 1; other values extrapolate.
    /// </summary>
    public static Vector3<T> Lerp(Vector3<T> start, Vector3<T> end, T t)
    {
        T s = T.MultiplicativeIdentity - t;
        return new(s * start.X + t * end.X, s * start.Y + t * end.Y, s * start.Z + t * end.Z);
    }

    /// <summary>
    /// The vector with <paramref name="map"/> applied to every entry. It also converts the entries:
    /// <c>v.Map(float.CreateTruncating)</c> rounds doubles to floats, and
    /// <c>v.Map(x =&gt; (int)Math.Floor(x))</c> gives the grid cell of a point.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
    public Vector3<TResult> Map<TResult>(Func<T, TResult> map)
        where TResult : System.Numerics.IAdditionOperators<TResult, TResult, TResult>, System.Numerics.ISubtractionOperators<TResult, TResult, TResult>,
                        System.Numerics.IMultiplyOperators<TResult, TResult, TResult>, System.Numerics.IDivisionOperators<TResult, TResult, TResult>,
                        System.Numerics.IUnaryNegationOperators<TResult, TResult>,
                        System.Numerics.IAdditiveIdentity<TResult, TResult>, System.Numerics.IMultiplicativeIdentity<TResult, TResult>
    {
        ArgumentNullException.ThrowIfNull(map);
        return new(map(X), map(Y), map(Z));
    }

    /// <summary>The vector as a <see cref="Vector{T}"/> of length 3, for the general algorithms.</summary>
    public Vector<T> ToVector() => [X, Y, Z];

    /// <summary>The entries, for <c>var (x, y, z) = v;</c></summary>
    public void Deconstruct(out T x, out T y, out T z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    /// <summary>The vector with the entries of the tuple: <c>Vector3&lt;double&gt; v = (1, 2, 3);</c></summary>
    public static implicit operator Vector3<T>((T X, T Y, T Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);

    /// <summary>The vector with the entries of a <see cref="Vector{T}"/> of length 3.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="vector"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="vector"/> has not 3 entries.</exception>
    public static explicit operator Vector3<T>(Vector<T> vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.Length != 3)
            throw new ArgumentException($"A Vector3 needs 3 entries, not {vector.Length}.", nameof(vector));

        return new(vector[0], vector[1], vector[2]);
    }

    /// <summary>The entry-wise sum.</summary>
    public static Vector3<T> operator +(Vector3<T> left, Vector3<T> right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <summary>The entry-wise difference.</summary>
    public static Vector3<T> operator -(Vector3<T> left, Vector3<T> right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    /// <summary>Every entry negated.</summary>
    public static Vector3<T> operator -(Vector3<T> value) => new(-value.X, -value.Y, -value.Z);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Vector3<T> operator *(T scalar, Vector3<T> value) =>
        new(scalar * value.X, scalar * value.Y, scalar * value.Z);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Vector3<T> operator *(Vector3<T> value, T scalar) =>
        new(value.X * scalar, value.Y * scalar, value.Z * scalar);

    /// <summary>Every entry divided by the number; for integer entries the quotients are truncated, as in C#.</summary>
    public static Vector3<T> operator /(Vector3<T> value, T scalar) =>
        new(value.X / scalar, value.Y / scalar, value.Z / scalar);

    /// <summary>
    /// Whether the entries are equal. They are compared with their <c>Equals</c>, so NaN equals
    /// NaN, as for <see cref="Matrix{T}"/>, and equal vectors have equal hash codes.
    /// </summary>
    public bool Equals(Vector3<T> other)
    {
        var comparer = EqualityComparer<T>.Default;
        return comparer.Equals(X, other.X) && comparer.Equals(Y, other.Y) && comparer.Equals(Z, other.Z);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Vector3<T> other && Equals(other);

    /// <summary>A hash of the entries, consistent with <see cref="Equals(Vector3{T})"/>.</summary>
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>Whether the vectors are equal.</summary>
    public static bool operator ==(Vector3<T> left, Vector3<T> right) => left.Equals(right);

    /// <summary>Whether the vectors are not equal.</summary>
    public static bool operator !=(Vector3<T> left, Vector3<T> right) => !left.Equals(right);

    /// <summary>The entries in brackets, culture-invariant, as for <see cref="Vector{T}"/>: <c>[1, 2, 3]</c>.</summary>
    public override string ToString() => FormattableString.Invariant($"[{X}, {Y}, {Z}]");
}
