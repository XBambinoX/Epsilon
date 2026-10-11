namespace Epsilon.LinearAlgebra;

/// <summary>
/// An immutable vector of two numbers, a value type for geometry in the plane. The entries can
/// be of any number type with the arithmetic operators, such as float, double, Half, decimal or
/// int; norms, angles and the other operations that need square roots are in
/// <see cref="FloatingPointVectorExtensions"/> for float, double and Half. A tuple converts to it:
/// <c>Vector2&lt;double&gt; v = (1, 2);</c>
/// </summary>
/// <typeparam name="T">The type of the entries.</typeparam>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public readonly struct Vector2<T> : IEquatable<Vector2<T>>
    where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.ISubtractionOperators<T, T, T>,
              System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IDivisionOperators<T, T, T>,
              System.Numerics.IUnaryNegationOperators<T, T>,
              System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplicativeIdentity<T, T>
{
    /// <summary>The vector with the given entries.</summary>
    public Vector2(T x, T y)
    {
        X = x;
        Y = y;
    }

    /// <summary>The first entry.</summary>
    public T X { get; init; }

    /// <summary>The second entry.</summary>
    public T Y { get; init; }

    /// <summary>The zero vector.</summary>
    public static Vector2<T> Zero => new(T.AdditiveIdentity, T.AdditiveIdentity);

    /// <summary>The vector of ones, which scales nothing in <see cref="Hadamard"/>.</summary>
    public static Vector2<T> One => new(T.MultiplicativeIdentity, T.MultiplicativeIdentity);

    /// <summary>The unit vector (1, 0) along the x axis.</summary>
    public static Vector2<T> UnitX => new(T.MultiplicativeIdentity, T.AdditiveIdentity);

    /// <summary>The unit vector (0, 1) along the y axis.</summary>
    public static Vector2<T> UnitY => new(T.AdditiveIdentity, T.MultiplicativeIdentity);

    /// <summary>The entry at the zero-based index: 0 is <see cref="X"/>, 1 is <see cref="Y"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is not 0 or 1.</exception>
    public T this[int index] => index switch
    {
        0 => X,
        1 => Y,
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The vector has 2 entries."),
    };

    /// <summary>The dot product: the sum of the products of the entries at the same index.</summary>
    public T Dot(Vector2<T> other) => X * other.X + Y * other.Y;

    /// <summary>The squared norm v . v; it needs no square root, so every number type has it.</summary>
    public T NormSquared() => Dot(this);

    /// <summary>
    /// The cross product of the vectors in the plane z = 0, which has only a z entry: x1 y2 - y1 x2.
    /// It is positive if <paramref name="other"/> points counterclockwise from this vector,
    /// negative if clockwise and 0 if they are parallel; its magnitude is the area of the
    /// parallelogram they span.
    /// </summary>
    public T Cross(Vector2<T> other) => X * other.Y - Y * other.X;

    /// <summary>The Hadamard product: the products of the entries at the same index, as when scaling by a vector.</summary>
    public Vector2<T> Hadamard(Vector2<T> other) => new(X * other.X, Y * other.Y);

    /// <summary>
    /// The point at <paramref name="t"/> on the line through two points: (1 - t) * start + t * end,
    /// so <paramref name="start"/> at t = 0 and <paramref name="end"/> at t = 1; other values extrapolate.
    /// </summary>
    public static Vector2<T> Lerp(Vector2<T> start, Vector2<T> end, T t)
    {
        T s = T.MultiplicativeIdentity - t;
        return new(s * start.X + t * end.X, s * start.Y + t * end.Y);
    }

    /// <summary>
    /// The vector with <paramref name="map"/> applied to every entry. It also converts the entries:
    /// <c>v.Map(float.CreateTruncating)</c> rounds doubles to floats, and
    /// <c>v.Map(x =&gt; (int)Math.Floor(x))</c> gives the grid cell of a point.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
    public Vector2<TResult> Map<TResult>(Func<T, TResult> map)
        where TResult : System.Numerics.IAdditionOperators<TResult, TResult, TResult>, System.Numerics.ISubtractionOperators<TResult, TResult, TResult>,
                        System.Numerics.IMultiplyOperators<TResult, TResult, TResult>, System.Numerics.IDivisionOperators<TResult, TResult, TResult>,
                        System.Numerics.IUnaryNegationOperators<TResult, TResult>,
                        System.Numerics.IAdditiveIdentity<TResult, TResult>, System.Numerics.IMultiplicativeIdentity<TResult, TResult>
    {
        ArgumentNullException.ThrowIfNull(map);
        return new(map(X), map(Y));
    }

    /// <summary>The vector as a <see cref="Vector{T}"/> of length 2, for the general algorithms.</summary>
    public Vector<T> ToVector() => [X, Y];

    /// <summary>The entries, for <c>var (x, y) = v;</c></summary>
    public void Deconstruct(out T x, out T y)
    {
        x = X;
        y = Y;
    }

    /// <summary>The vector with the entries of the tuple: <c>Vector2&lt;double&gt; v = (1, 2);</c></summary>
    public static implicit operator Vector2<T>((T X, T Y) tuple) => new(tuple.X, tuple.Y);

    /// <summary>The vector with the entries of a <see cref="Vector{T}"/> of length 2.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="vector"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="vector"/> has not 2 entries.</exception>
    public static explicit operator Vector2<T>(Vector<T> vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.Length != 2)
            throw new ArgumentException($"A Vector2 needs 2 entries, not {vector.Length}.", nameof(vector));

        return new(vector[0], vector[1]);
    }

    /// <summary>The entry-wise sum.</summary>
    public static Vector2<T> operator +(Vector2<T> left, Vector2<T> right) => new(left.X + right.X, left.Y + right.Y);

    /// <summary>The entry-wise difference.</summary>
    public static Vector2<T> operator -(Vector2<T> left, Vector2<T> right) => new(left.X - right.X, left.Y - right.Y);

    /// <summary>Every entry negated.</summary>
    public static Vector2<T> operator -(Vector2<T> value) => new(-value.X, -value.Y);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Vector2<T> operator *(T scalar, Vector2<T> value) => new(scalar * value.X, scalar * value.Y);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Vector2<T> operator *(Vector2<T> value, T scalar) => new(value.X * scalar, value.Y * scalar);

    /// <summary>Every entry divided by the number; for integer entries the quotients are truncated, as in C#.</summary>
    public static Vector2<T> operator /(Vector2<T> value, T scalar) => new(value.X / scalar, value.Y / scalar);

    /// <summary>
    /// Whether the entries are equal. They are compared with their <c>Equals</c>, so NaN equals
    /// NaN, as for <see cref="Matrix{T}"/>, and equal vectors have equal hash codes.
    /// </summary>
    public bool Equals(Vector2<T> other)
    {
        var comparer = EqualityComparer<T>.Default;
        return comparer.Equals(X, other.X) && comparer.Equals(Y, other.Y);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Vector2<T> other && Equals(other);

    /// <summary>A hash of the entries, consistent with <see cref="Equals(Vector2{T})"/>.</summary>
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>Whether the vectors are equal.</summary>
    public static bool operator ==(Vector2<T> left, Vector2<T> right) => left.Equals(right);

    /// <summary>Whether the vectors are not equal.</summary>
    public static bool operator !=(Vector2<T> left, Vector2<T> right) => !left.Equals(right);

    /// <summary>The entries in brackets, culture-invariant, as for <see cref="Vector{T}"/>: <c>[1, 2]</c>.</summary>
    public override string ToString() => FormattableString.Invariant($"[{X}, {Y}]");
}
