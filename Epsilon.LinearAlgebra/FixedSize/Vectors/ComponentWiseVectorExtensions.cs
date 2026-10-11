namespace Epsilon.LinearAlgebra;

/// <summary>
/// Entry-by-entry minimum, maximum, absolute value and clamping of <see cref="Vector2{T}"/>,
/// <see cref="Vector3{T}"/> and <see cref="Vector4{T}"/> with entries of an ordered number type:
/// float, double, Half, decimal, int and the like.
/// </summary>
public static class ComponentWiseVectorExtensions
{
    extension<T>(Vector2<T> vector) where T : System.Numerics.INumber<T>
    {
        /// <summary>The smaller entry at each index; with <c>Max</c> it gives the corners of a bounding box.</summary>
        public Vector2<T> Min(Vector2<T> other) => new(T.Min(vector.X, other.X), T.Min(vector.Y, other.Y));

        /// <summary>The larger entry at each index.</summary>
        public Vector2<T> Max(Vector2<T> other) => new(T.Max(vector.X, other.X), T.Max(vector.Y, other.Y));

        /// <summary>The absolute value of every entry.</summary>
        public Vector2<T> Abs() => new(T.Abs(vector.X), T.Abs(vector.Y));

        /// <summary>Every entry limited to the range between the entries of <paramref name="min"/> and <paramref name="max"/> at its index.</summary>
        /// <exception cref="ArgumentException">An entry of <paramref name="min"/> is greater than that of <paramref name="max"/>.</exception>
        public Vector2<T> Clamp(Vector2<T> min, Vector2<T> max) =>
            new(T.Clamp(vector.X, min.X, max.X), T.Clamp(vector.Y, min.Y, max.Y));
    }

    extension<T>(Vector3<T> vector) where T : System.Numerics.INumber<T>
    {
        /// <summary>The smaller entry at each index; with <c>Max</c> it gives the corners of a bounding box.</summary>
        public Vector3<T> Min(Vector3<T> other) =>
            new(T.Min(vector.X, other.X), T.Min(vector.Y, other.Y), T.Min(vector.Z, other.Z));

        /// <summary>The larger entry at each index.</summary>
        public Vector3<T> Max(Vector3<T> other) =>
            new(T.Max(vector.X, other.X), T.Max(vector.Y, other.Y), T.Max(vector.Z, other.Z));

        /// <summary>The absolute value of every entry.</summary>
        public Vector3<T> Abs() => new(T.Abs(vector.X), T.Abs(vector.Y), T.Abs(vector.Z));

        /// <summary>Every entry limited to the range between the entries of <paramref name="min"/> and <paramref name="max"/> at its index.</summary>
        /// <exception cref="ArgumentException">An entry of <paramref name="min"/> is greater than that of <paramref name="max"/>.</exception>
        public Vector3<T> Clamp(Vector3<T> min, Vector3<T> max) =>
            new(T.Clamp(vector.X, min.X, max.X), T.Clamp(vector.Y, min.Y, max.Y), T.Clamp(vector.Z, min.Z, max.Z));
    }

    extension<T>(Vector4<T> vector) where T : System.Numerics.INumber<T>
    {
        /// <summary>The smaller entry at each index; with <c>Max</c> it gives the corners of a bounding box.</summary>
        public Vector4<T> Min(Vector4<T> other) =>
            new(T.Min(vector.X, other.X), T.Min(vector.Y, other.Y), T.Min(vector.Z, other.Z), T.Min(vector.W, other.W));

        /// <summary>The larger entry at each index.</summary>
        public Vector4<T> Max(Vector4<T> other) =>
            new(T.Max(vector.X, other.X), T.Max(vector.Y, other.Y), T.Max(vector.Z, other.Z), T.Max(vector.W, other.W));

        /// <summary>The absolute value of every entry.</summary>
        public Vector4<T> Abs() => new(T.Abs(vector.X), T.Abs(vector.Y), T.Abs(vector.Z), T.Abs(vector.W));

        /// <summary>Every entry limited to the range between the entries of <paramref name="min"/> and <paramref name="max"/> at its index.</summary>
        /// <exception cref="ArgumentException">An entry of <paramref name="min"/> is greater than that of <paramref name="max"/>.</exception>
        public Vector4<T> Clamp(Vector4<T> min, Vector4<T> max) =>
            new(T.Clamp(vector.X, min.X, max.X), T.Clamp(vector.Y, min.Y, max.Y),
                T.Clamp(vector.Z, min.Z, max.Z), T.Clamp(vector.W, min.W, max.W));
    }
}
