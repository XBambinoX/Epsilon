namespace Epsilon.LinearAlgebra;

/// <summary>
/// An immutable quaternion x i + y j + z k + w, a value type for rotations of space: the unit
/// quaternion (sin(a/2) u, cos(a/2)) turns by the angle a about the unit axis u. The product
/// <c>p * q</c> applies q first, as for matrices. The entries can be of any number type with the
/// arithmetic operators; rotations, <c>Rotate</c>, <c>Slerp</c> and the other operations that need
/// square roots or trigonometry are in <see cref="FloatingPointQuaternionExtensions"/> for float,
/// double and Half.
/// </summary>
/// <typeparam name="T">The type of the entries.</typeparam>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public readonly struct Quaternion<T> : IEquatable<Quaternion<T>>
    where T : System.Numerics.IAdditionOperators<T, T, T>, System.Numerics.ISubtractionOperators<T, T, T>,
              System.Numerics.IMultiplyOperators<T, T, T>, System.Numerics.IDivisionOperators<T, T, T>,
              System.Numerics.IUnaryNegationOperators<T, T>,
              System.Numerics.IAdditiveIdentity<T, T>, System.Numerics.IMultiplicativeIdentity<T, T>
{
    /// <summary>The quaternion x i + y j + z k + w; the order of the entries is that of System.Numerics.</summary>
    public Quaternion(T x, T y, T z, T w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>The quaternion with the vector part <paramref name="xyz"/> and the scalar part <paramref name="w"/>.</summary>
    public Quaternion(Vector3<T> xyz, T w) : this(xyz.X, xyz.Y, xyz.Z, w)
    {
    }

    /// <summary>The coefficient of i.</summary>
    public T X { get; init; }

    /// <summary>The coefficient of j.</summary>
    public T Y { get; init; }

    /// <summary>The coefficient of k.</summary>
    public T Z { get; init; }

    /// <summary>The scalar part.</summary>
    public T W { get; init; }

    /// <summary>The identity 1, the rotation by 0.</summary>
    public static Quaternion<T> Identity => new(T.AdditiveIdentity, T.AdditiveIdentity, T.AdditiveIdentity, T.MultiplicativeIdentity);

    /// <summary>The vector part (x, y, z).</summary>
    public Vector3<T> XYZ => new(X, Y, Z);

    /// <summary>The conjugate -x i - y j - z k + w; for a unit quaternion it is the inverse rotation.</summary>
    public Quaternion<T> Conjugate() => new(-X, -Y, -Z, W);

    /// <summary>The dot product of the entries as vectors of four numbers.</summary>
    public T Dot(Quaternion<T> other) => X * other.X + Y * other.Y + Z * other.Z + W * other.W;

    /// <summary>The squared norm x^2 + y^2 + z^2 + w^2, which is 1 for a rotation.</summary>
    public T NormSquared() => Dot(this);

    /// <summary>
    /// The quaternion with <paramref name="map"/> applied to every entry; it also converts them:
    /// <c>q.Map(float.CreateTruncating)</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
    public Quaternion<TResult> Map<TResult>(Func<T, TResult> map)
        where TResult : System.Numerics.IAdditionOperators<TResult, TResult, TResult>, System.Numerics.ISubtractionOperators<TResult, TResult, TResult>,
                        System.Numerics.IMultiplyOperators<TResult, TResult, TResult>, System.Numerics.IDivisionOperators<TResult, TResult, TResult>,
                        System.Numerics.IUnaryNegationOperators<TResult, TResult>,
                        System.Numerics.IAdditiveIdentity<TResult, TResult>, System.Numerics.IMultiplicativeIdentity<TResult, TResult>
    {
        ArgumentNullException.ThrowIfNull(map);
        return new(map(X), map(Y), map(Z), map(W));
    }

    /// <summary>The entries, for <c>var (x, y, z, w) = q;</c></summary>
    public void Deconstruct(out T x, out T y, out T z, out T w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    /// <summary>
    /// The Hamilton product. As a rotation it applies <paramref name="right"/> first and then
    /// <paramref name="left"/>, like the product of their matrices; it is not commutative.
    /// </summary>
    public static Quaternion<T> operator *(Quaternion<T> left, Quaternion<T> right) => new(
        left.W * right.X + left.X * right.W + left.Y * right.Z - left.Z * right.Y,
        left.W * right.Y - left.X * right.Z + left.Y * right.W + left.Z * right.X,
        left.W * right.Z + left.X * right.Y - left.Y * right.X + left.Z * right.W,
        left.W * right.W - left.X * right.X - left.Y * right.Y - left.Z * right.Z);

    /// <summary>The entry-wise sum.</summary>
    public static Quaternion<T> operator +(Quaternion<T> left, Quaternion<T> right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);

    /// <summary>The entry-wise difference.</summary>
    public static Quaternion<T> operator -(Quaternion<T> left, Quaternion<T> right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);

    /// <summary>Every entry negated; as a rotation the same one.</summary>
    public static Quaternion<T> operator -(Quaternion<T> value) => new(-value.X, -value.Y, -value.Z, -value.W);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Quaternion<T> operator *(T scalar, Quaternion<T> value) =>
        new(scalar * value.X, scalar * value.Y, scalar * value.Z, scalar * value.W);

    /// <summary>Every entry multiplied by the number.</summary>
    public static Quaternion<T> operator *(Quaternion<T> value, T scalar) =>
        new(value.X * scalar, value.Y * scalar, value.Z * scalar, value.W * scalar);

    /// <summary>Every entry divided by the number; for integer entries the quotients are truncated, as in C#.</summary>
    public static Quaternion<T> operator /(Quaternion<T> value, T scalar) =>
        new(value.X / scalar, value.Y / scalar, value.Z / scalar, value.W / scalar);

    /// <summary>
    /// Whether the entries are equal. They are compared with their <c>Equals</c>, so NaN equals
    /// NaN. A quaternion and its negative are the same rotation but not equal.
    /// </summary>
    public bool Equals(Quaternion<T> other)
    {
        var comparer = EqualityComparer<T>.Default;
        return comparer.Equals(X, other.X) && comparer.Equals(Y, other.Y)
            && comparer.Equals(Z, other.Z) && comparer.Equals(W, other.W);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Quaternion<T> other && Equals(other);

    /// <summary>A hash of the entries, consistent with <see cref="Equals(Quaternion{T})"/>.</summary>
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    /// <summary>Whether the quaternions are equal.</summary>
    public static bool operator ==(Quaternion<T> left, Quaternion<T> right) => left.Equals(right);

    /// <summary>Whether the quaternions are not equal.</summary>
    public static bool operator !=(Quaternion<T> left, Quaternion<T> right) => !left.Equals(right);

    /// <summary>The entries x, y, z, w in brackets, culture-invariant: <c>[0, 0, 0, 1]</c> for the identity.</summary>
    public override string ToString() => FormattableString.Invariant($"[{X}, {Y}, {Z}, {W}]");
}
