namespace Epsilon.LinearAlgebra;

/// <summary>
/// An immutable vector of four numbers, a value type for homogeneous coordinates (x, y, z, w)
/// and colors with alpha. The entries can be of any number type with the arithmetic operators,
/// such as float, double, Half, decimal or int; norms, angles and the other operations that need
/// square roots are in <see cref="FloatingPointVectorExtensions"/> for float, double and Half.
/// A tuple converts to it: <c>Vector4&lt;double&gt; v = (1, 2, 3, 1);</c>
/// </summary>
/// <typeparam name="T">The type of the entries.</typeparam>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public readonly struct Vector4<T> : IEquatable<Vector4<T>>
    where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.ISubtractionOperators<T, T, T>,
              System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IDivisionOperators<T, T, T>,
              System.Numerics.IUnaryNegationOperators<T, T>,
              System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplicativeIdentity<T, T>
{
    /// <summary>The vector with the given entries.</summary>
    public Vector4(T x, T y, T z, T w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// The vector with the entries of <paramref name="xyz"/> followed by <paramref name="w"/>:
    /// <c>new Vector4&lt;double&gt;(point, 1)</c> is a point in homogeneous coordinates.
    /// </summary>
    public Vector4(Vector3<T> xyz, T w) : this(xyz.X, xyz.Y, xyz.Z, w)
    {
    }

    /// <summary>The first entry.</summary>
    public T X { get; init; }

    /// <summary>The second entry.</summary>
    public T Y { get; init; }

    /// <summary>The third entry.</summary>
    public T Z { get; init; }

    /// <summary>The fourth entry.</summary>
    public T W { get; init; }

    /// <summary>The zero vector.</summary>
    public static Vector4<T> Zero => new(T.AdditiveIdentity, T.AdditiveIdentity, T.AdditiveIdentity, T.AdditiveIdentity);

    /// <summary>The vector of ones, which scales nothing in <see cref="Hadamard"/>.</summary>
    public static Vector4<T> One =>
        new(T.MultiplicativeIdentity, T.MultiplicativeIdentity, T.MultiplicativeIdentity, T.MultiplicativeIdentity);

    /// <summary>The unit vector (1, 0, 0, 0).</summary>
    public static Vector4<T> UnitX => new(T.MultiplicativeIdentity, T.AdditiveIdentity, T.AdditiveIdentity, T.AdditiveIdentity);

    /// <summary>The unit vector (0, 1, 0, 0).</summary>
    public static Vector4<T> UnitY => new(T.AdditiveIdentity, T.MultiplicativeIdentity, T.AdditiveIdentity, T.AdditiveIdentity);

    /// <summary>The unit vector (0, 0, 1, 0).</summary>
    public static Vector4<T> UnitZ => new(T.AdditiveIdentity, T.AdditiveIdentity, T.MultiplicativeIdentity, T.AdditiveIdentity);

    /// <summary>The unit vector (0, 0, 0, 1).</summary>
    public static Vector4<T> UnitW => new(T.AdditiveIdentity, T.AdditiveIdentity, T.AdditiveIdentity, T.MultiplicativeIdentity);

    /// <summary>
    /// The entry at the zero-based index: 0 is <see cref="X"/>, 1 is <see cref="Y"/>, 2 is
    /// <see cref="Z"/>, 3 is <see cref="W"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not 0, 1, 2 or 3.</exception>
    public T this[int index] => index switch
    {
        0 => X,
        1 => Y,
        2 => Z,
        3 => W,
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The vector has 4 entries."),
    };

    /// <summary>The first two entries.</summary>
    public Vector2<T> XY => new(X, Y);

    /// <summary>The first three entries.</summary>
    public Vector3<T> XYZ => new(X, Y, Z);

    /// <summary>The dot product: the sum of the products of the entries at the same index.</summary>
    public T Dot(Vector4<T> other) => X * other.X + Y * other.Y + Z * other.Z + W * other.W;

    /// <summary>The squared norm v . v; it needs no square root, so every number type has it.</summary>
    public T NormSquared() => Dot(this);

    /// <summary>The Hadamard product: the products of the entries at the same index, as when scaling by a vector.</summary>
    public Vector4<T> Hadamard(Vector4<T> other) => new(X * other.X, Y * other.Y, Z * other.Z, W * other.W);

    /// <summary>
    /// The point at <paramref name="t"/> on the line through two points: (1 - t) * start + t * end,
    /// so <paramref name="start"/> at t = 0 and <paramref name="end"/> at t = 1; other values extrapolate.
    /// </summary>
    public static Vector4<T> Lerp(Vector4<T> start, Vector4<T> end, T t)
    {
        T s = T.MultiplicativeIdentity - t;
        return new(s * start.X + t * end.X, s * start.Y + t * end.Y, s * start.Z + t * end.Z, s * start.W + t * end.W);
    }

    /// <summary>
    /// The vector with <paramref name="map"/> applied to every entry. It also converts the entries:
    /// <c>v.Map(float.CreateTruncating)</c> rounds doubles to floats, and
    /// <c>v.Map(x =&gt; (int)Math.Floor(x))</c> gives the grid cell of a point.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
    public Vector4<TResult> Map<TResult>(Func<T, TResult> map)
        where TResult : System.Numerics.IAdditionOperators<TResult, TResult, TResult>, System.Numerics.ISubtractionOperators<TResult, TResult, TResult>,
                        System.Numerics.IMultiplyOperators<TResult, TResult, TResult>, System.Numerics.IDivisionOperators<TResult, TResult, TResult>,
                        System.Numerics.IUnaryNegationOperators<TResult, TResult>,
                        System.Numerics.IAdditiveIdentity<TResult, TResult>, System.Numerics.IMultiplicativeIdentity<TResult, TResult>
    {
        ArgumentNullException.ThrowIfNull(map);
        return new(map(X), map(Y), map(Z), map(W));
    }

    /// <summary>The entries, for <c>var (x, y, z, w) = v;</c></summary>
    public void Deconstruct(out T x, out T y, out T z, out T w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    /// <summary>The vector with the entries of the tuple: <c>Vector4&lt;double&gt; v = (1, 2, 3, 1);</c></summary>
    public static implicit operator Vector4<T>((T X, T Y, T Z, T W) tuple) => new(tuple.X, tuple.Y, tuple.Z, tuple.W);

    /// <summary>The entry-wise sum.</summary>
    public static Vector4<T> operator +(Vector4<T> left, Vector4<T> right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    /// <summary>The entry-wise difference.</summary>
    public static Vector4<T> operator -(Vector4<T> left, Vector4<T> right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

    /// <summary>Every entry negated.</summary>
    public static Vector4<T> operator -(Vector4<T> value) => new(-value.X, -value.Y, -value.Z, -value.W);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Vector4<T> operator *(T scalar, Vector4<T> value) =>
        new(scalar * value.X, scalar * value.Y, scalar * value.Z, scalar * value.W);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Vector4<T> operator *(Vector4<T> value, T scalar) =>
        new(value.X * scalar, value.Y * scalar, value.Z * scalar, value.W * scalar);

    /// <summary>Every entry divided by the number; for integer entries the quotients are truncated, as in C#.</summary>
    public static Vector4<T> operator /(Vector4<T> value, T scalar) =>
        new(value.X / scalar, value.Y / scalar, value.Z / scalar, value.W / scalar);

    /// <summary>
    /// Whether the entries are equal. They are compared with their <c>Equals</c>, so NaN equals
    /// NaN, as for <see cref="Matrix{T}"/>, and equal vectors have equal hash codes.
    /// </summary>
    public bool Equals(Vector4<T> other)
    {
        var comparer = EqualityComparer<T>.Default;
        return comparer.Equals(X, other.X) && comparer.Equals(Y, other.Y)
            && comparer.Equals(Z, other.Z) && comparer.Equals(W, other.W);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Vector4<T> other && Equals(other);

    /// <summary>A hash of the entries, consistent with <see cref="Equals(Vector4{T})"/>.</summary>
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    /// <summary>Whether the vectors are equal.</summary>
    public static bool operator ==(Vector4<T> left, Vector4<T> right) => left.Equals(right);

    /// <summary>Whether the vectors are not equal.</summary>
    public static bool operator !=(Vector4<T> left, Vector4<T> right) => !left.Equals(right);

    /// <summary>The entries in brackets, culture-invariant, as for <see cref="Vector{T}"/>: <c>[1, 2, 3, 4]</c>.</summary>
    public override string ToString() => FormattableString.Invariant($"[{X}, {Y}, {Z}, {W}]");
}
