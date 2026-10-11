namespace Epsilon.LinearAlgebra;

/// <summary>
/// Norms, angles, projections and approximate equality of <see cref="Vector2{T}"/>,
/// <see cref="Vector3{T}"/> and <see cref="Vector4{T}"/> with floating-point entries: float,
/// double or Half. Each takes the fast naive formula where that one is accurate, and scales by a
/// power of two, which is exact, where squares would overflow or underflow.
/// </summary>
public static class FloatingPointVectorExtensions
{
    extension<T>(Vector2<T> vector) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        /// <summary>The Euclidean norm (length) sqrt(v . v); it neither overflows for huge entries nor underflows for tiny ones.</summary>
        public T Norm()
        {
            T squared = vector.NormSquared();
            if (FloatingPoint<T>.IsSafe(squared))
                return T.Sqrt(squared);

            Vector2<T> scaled = Scaled(vector, out int exponent);
            return T.ScaleB(T.Sqrt(scaled.NormSquared()), exponent);
        }

        /// <summary>The unit vector in the same direction, accurate for huge and tiny entries too.</summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        public Vector2<T> Normalize()
        {
            T squared = vector.NormSquared();
            if (FloatingPoint<T>.IsSafe(squared))
                return vector / T.Sqrt(squared);

            Vector2<T> scaled = Scaled(vector, out _);
            T norm = T.Sqrt(scaled.NormSquared());
            if (norm == T.Zero)
                throw new InvalidOperationException("Cannot normalize the zero vector.");

            return scaled / norm;
        }

        /// <summary>The Euclidean distance between two points: the norm of their difference.</summary>
        public T Distance(Vector2<T> other) => (vector - other).Norm();

        /// <summary>
        /// The angle between two vectors in radians, from 0 to pi. Kahan's formula
        /// 2 atan2(|a - b|, |a + b|) of the unit vectors a and b keeps it accurate at every angle,
        /// where the arccosine of the cosine loses small angles and those near pi.
        /// </summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="other"/> is zero.</exception>
        public T Angle(Vector2<T> other)
        {
            if (vector == Vector2<T>.Zero)
                throw new InvalidOperationException(NumericVectorExtensions.UndefinedAngle);
            if (other == Vector2<T>.Zero)
                throw new ArgumentException(NumericVectorExtensions.UndefinedAngle, nameof(other));

            Vector2<T> a = vector.Normalize(), b = other.Normalize();
            return FloatingPoint<T>.Two * T.Atan2((a - b).Norm(), (a + b).Norm());
        }

        /// <summary>
        /// The projection onto the line through <paramref name="onto"/>: (v . w / w . w) * w, the
        /// part of the vector in that direction.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="onto"/> is zero.</exception>
        public Vector2<T> ProjectOnto(Vector2<T> onto)
        {
            Vector2<T> direction = onto;
            T squared = direction.NormSquared();
            if (!FloatingPoint<T>.IsSafe(squared))
            {
                direction = Scaled(onto, out _);
                squared = direction.NormSquared();
                if (squared == T.Zero)
                    throw new ArgumentException("Cannot project onto the zero vector.", nameof(onto));
            }

            return vector.Dot(direction) / squared * direction;
        }

        /// <summary>
        /// The mirror image in the line through the origin perpendicular to <paramref name="normal"/>:
        /// v - 2 (v . n / n . n) * n, as a velocity bounces off a wall. The normal need not have length 1.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="normal"/> is zero.</exception>
        public Vector2<T> Reflect(Vector2<T> normal)
        {
            Vector2<T> direction = normal;
            T squared = direction.NormSquared();
            if (!FloatingPoint<T>.IsSafe(squared))
            {
                direction = Scaled(normal, out _);
                squared = direction.NormSquared();
                if (squared == T.Zero)
                    throw new ArgumentException("The normal is the zero vector.", nameof(normal));
            }

            return vector - FloatingPoint<T>.Two * vector.Dot(direction) / squared * direction;
        }

        /// <summary>
        /// Whether the vectors are equal up to rounding:
        /// |v - w| &lt;= max(relativeTolerance * max(|v|, |w|), absoluteTolerance). Without a
        /// relative tolerance it is 1e-9 for double, 1e-5 for float and 1e-2 for Half; against the
        /// zero vector only <paramref name="absoluteTolerance"/> helps. Infinite entries must be
        /// equal, and NaN is never close to anything.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A tolerance is negative or NaN.</exception>
        public bool IsApproximately(Vector2<T> other, T? relativeTolerance = null, T? absoluteTolerance = null) =>
            FloatingPoint<T>.AreClose([vector.X, vector.Y], [other.X, other.Y], relativeTolerance, absoluteTolerance);
    }

    extension<T>(Vector3<T> vector) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        /// <summary>The Euclidean norm (length) sqrt(v . v); it neither overflows for huge entries nor underflows for tiny ones.</summary>
        public T Norm()
        {
            T squared = vector.NormSquared();
            if (FloatingPoint<T>.IsSafe(squared))
                return T.Sqrt(squared);

            Vector3<T> scaled = Scaled(vector, out int exponent);
            return T.ScaleB(T.Sqrt(scaled.NormSquared()), exponent);
        }

        /// <summary>The unit vector in the same direction, accurate for huge and tiny entries too.</summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        public Vector3<T> Normalize()
        {
            T squared = vector.NormSquared();
            if (FloatingPoint<T>.IsSafe(squared))
                return vector / T.Sqrt(squared);

            Vector3<T> scaled = Scaled(vector, out _);
            T norm = T.Sqrt(scaled.NormSquared());
            if (norm == T.Zero)
                throw new InvalidOperationException("Cannot normalize the zero vector.");

            return scaled / norm;
        }

        /// <summary>The Euclidean distance between two points: the norm of their difference.</summary>
        public T Distance(Vector3<T> other) => (vector - other).Norm();

        /// <summary>
        /// The angle between two vectors in radians, from 0 to pi. Kahan's formula
        /// 2 atan2(|a - b|, |a + b|) of the unit vectors a and b keeps it accurate at every angle,
        /// where the arccosine of the cosine loses small angles and those near pi.
        /// </summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="other"/> is zero.</exception>
        public T Angle(Vector3<T> other)
        {
            if (vector == Vector3<T>.Zero)
                throw new InvalidOperationException(NumericVectorExtensions.UndefinedAngle);
            if (other == Vector3<T>.Zero)
                throw new ArgumentException(NumericVectorExtensions.UndefinedAngle, nameof(other));

            Vector3<T> a = vector.Normalize(), b = other.Normalize();
            return FloatingPoint<T>.Two * T.Atan2((a - b).Norm(), (a + b).Norm());
        }

        /// <summary>
        /// The projection onto the line through <paramref name="onto"/>: (v . w / w . w) * w, the
        /// part of the vector in that direction.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="onto"/> is zero.</exception>
        public Vector3<T> ProjectOnto(Vector3<T> onto)
        {
            Vector3<T> direction = onto;
            T squared = direction.NormSquared();
            if (!FloatingPoint<T>.IsSafe(squared))
            {
                direction = Scaled(onto, out _);
                squared = direction.NormSquared();
                if (squared == T.Zero)
                    throw new ArgumentException("Cannot project onto the zero vector.", nameof(onto));
            }

            return vector.Dot(direction) / squared * direction;
        }

        /// <summary>
        /// The mirror image in the plane through the origin perpendicular to <paramref name="normal"/>:
        /// v - 2 (v . n / n . n) * n, as a velocity bounces off a wall. The normal need not have length 1.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="normal"/> is zero.</exception>
        public Vector3<T> Reflect(Vector3<T> normal)
        {
            Vector3<T> direction = normal;
            T squared = direction.NormSquared();
            if (!FloatingPoint<T>.IsSafe(squared))
            {
                direction = Scaled(normal, out _);
                squared = direction.NormSquared();
                if (squared == T.Zero)
                    throw new ArgumentException("The normal is the zero vector.", nameof(normal));
            }

            return vector - FloatingPoint<T>.Two * vector.Dot(direction) / squared * direction;
        }

        /// <summary>
        /// Whether the vectors are equal up to rounding:
        /// |v - w| &lt;= max(relativeTolerance * max(|v|, |w|), absoluteTolerance). Without a
        /// relative tolerance it is 1e-9 for double, 1e-5 for float and 1e-2 for Half; against the
        /// zero vector only <paramref name="absoluteTolerance"/> helps. Infinite entries must be
        /// equal, and NaN is never close to anything.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A tolerance is negative or NaN.</exception>
        public bool IsApproximately(Vector3<T> other, T? relativeTolerance = null, T? absoluteTolerance = null) =>
            FloatingPoint<T>.AreClose([vector.X, vector.Y, vector.Z], [other.X, other.Y, other.Z], relativeTolerance, absoluteTolerance);
    }

    extension<T>(Vector4<T> vector) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        /// <summary>The Euclidean norm (length) sqrt(v . v); it neither overflows for huge entries nor underflows for tiny ones.</summary>
        public T Norm()
        {
            T squared = vector.NormSquared();
            if (FloatingPoint<T>.IsSafe(squared))
                return T.Sqrt(squared);

            Vector4<T> scaled = Scaled(vector, out int exponent);
            return T.ScaleB(T.Sqrt(scaled.NormSquared()), exponent);
        }

        /// <summary>The unit vector in the same direction, accurate for huge and tiny entries too.</summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        public Vector4<T> Normalize()
        {
            T squared = vector.NormSquared();
            if (FloatingPoint<T>.IsSafe(squared))
                return vector / T.Sqrt(squared);

            Vector4<T> scaled = Scaled(vector, out _);
            T norm = T.Sqrt(scaled.NormSquared());
            if (norm == T.Zero)
                throw new InvalidOperationException("Cannot normalize the zero vector.");

            return scaled / norm;
        }

        /// <summary>The Euclidean distance between two points: the norm of their difference.</summary>
        public T Distance(Vector4<T> other) => (vector - other).Norm();

        /// <summary>
        /// The angle between two vectors in radians, from 0 to pi. Kahan's formula
        /// 2 atan2(|a - b|, |a + b|) of the unit vectors a and b keeps it accurate at every angle,
        /// where the arccosine of the cosine loses small angles and those near pi.
        /// </summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="other"/> is zero.</exception>
        public T Angle(Vector4<T> other)
        {
            if (vector == Vector4<T>.Zero)
                throw new InvalidOperationException(NumericVectorExtensions.UndefinedAngle);
            if (other == Vector4<T>.Zero)
                throw new ArgumentException(NumericVectorExtensions.UndefinedAngle, nameof(other));

            Vector4<T> a = vector.Normalize(), b = other.Normalize();
            return FloatingPoint<T>.Two * T.Atan2((a - b).Norm(), (a + b).Norm());
        }

        /// <summary>
        /// The projection onto the line through <paramref name="onto"/>: (v . w / w . w) * w, the
        /// part of the vector in that direction.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="onto"/> is zero.</exception>
        public Vector4<T> ProjectOnto(Vector4<T> onto)
        {
            Vector4<T> direction = onto;
            T squared = direction.NormSquared();
            if (!FloatingPoint<T>.IsSafe(squared))
            {
                direction = Scaled(onto, out _);
                squared = direction.NormSquared();
                if (squared == T.Zero)
                    throw new ArgumentException("Cannot project onto the zero vector.", nameof(onto));
            }

            return vector.Dot(direction) / squared * direction;
        }

        /// <summary>
        /// The mirror image in the hyperplane through the origin perpendicular to <paramref name="normal"/>:
        /// v - 2 (v . n / n . n) * n. The normal need not have length 1.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="normal"/> is zero.</exception>
        public Vector4<T> Reflect(Vector4<T> normal)
        {
            Vector4<T> direction = normal;
            T squared = direction.NormSquared();
            if (!FloatingPoint<T>.IsSafe(squared))
            {
                direction = Scaled(normal, out _);
                squared = direction.NormSquared();
                if (squared == T.Zero)
                    throw new ArgumentException("The normal is the zero vector.", nameof(normal));
            }

            return vector - FloatingPoint<T>.Two * vector.Dot(direction) / squared * direction;
        }

        /// <summary>
        /// Whether the vectors are equal up to rounding:
        /// |v - w| &lt;= max(relativeTolerance * max(|v|, |w|), absoluteTolerance). Without a
        /// relative tolerance it is 1e-9 for double, 1e-5 for float and 1e-2 for Half; against the
        /// zero vector only <paramref name="absoluteTolerance"/> helps. Infinite entries must be
        /// equal, and NaN is never close to anything.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A tolerance is negative or NaN.</exception>
        public bool IsApproximately(Vector4<T> other, T? relativeTolerance = null, T? absoluteTolerance = null) =>
            FloatingPoint<T>.AreClose([vector.X, vector.Y, vector.Z, vector.W], [other.X, other.Y, other.Z, other.W], relativeTolerance, absoluteTolerance);
    }

    // The vector divided by a power of two that brings its largest magnitude into [1, 2): exact,
    // so the direction stays the same, while the sum of squares can neither overflow nor underflow.
    private static Vector2<T> Scaled<T>(Vector2<T> v, out int exponent) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        exponent = FloatingPoint<T>.ScaleExponent(T.MaxMagnitude(v.X, v.Y));
        return new(T.ScaleB(v.X, -exponent), T.ScaleB(v.Y, -exponent));
    }

    private static Vector3<T> Scaled<T>(Vector3<T> v, out int exponent) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        exponent = FloatingPoint<T>.ScaleExponent(T.MaxMagnitude(T.MaxMagnitude(v.X, v.Y), v.Z));
        return new(T.ScaleB(v.X, -exponent), T.ScaleB(v.Y, -exponent), T.ScaleB(v.Z, -exponent));
    }

    private static Vector4<T> Scaled<T>(Vector4<T> v, out int exponent) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        exponent = FloatingPoint<T>.ScaleExponent(T.MaxMagnitude(T.MaxMagnitude(v.X, v.Y), T.MaxMagnitude(v.Z, v.W)));
        return new(T.ScaleB(v.X, -exponent), T.ScaleB(v.Y, -exponent), T.ScaleB(v.Z, -exponent), T.ScaleB(v.W, -exponent));
    }
}
