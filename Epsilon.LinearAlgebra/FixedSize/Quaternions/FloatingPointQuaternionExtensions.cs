namespace Epsilon.LinearAlgebra;

/// <summary>
/// Rotations as quaternions of float, double or Half: building them, applying them, turning them
/// into an axis and an angle and back from a matrix, and interpolating between them. Angles are
/// in radians. Wherever a quaternion stands for a rotation it need not have norm 1: q and every
/// nonzero multiple of it are the same rotation.
/// </summary>
public static class FloatingPointQuaternionExtensions
{
    extension<T>(Quaternion<T> quaternion) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        /// <summary>
        /// The rotation about <paramref name="axis"/>, which need not have length 1, by
        /// <paramref name="angle"/>: (sin(a/2) u, cos(a/2)) for the unit axis u. A positive angle
        /// turns counterclockwise as seen from the tip of the axis (the right-hand rule), as
        /// <c>Matrix3x3&lt;T&gt;.Rotation</c> does.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="axis"/> is zero.</exception>
        public static Quaternion<T> Rotation(Vector3<T> axis, T angle)
        {
            if (axis == Vector3<T>.Zero)
                throw new ArgumentException("The axis is the zero vector.", nameof(axis));

            var (sin, cos) = T.SinCos(angle / FloatingPoint<T>.Two);
            return new(axis.Normalize() * sin, cos);
        }

        /// <summary>The rotation about the x axis; a positive angle turns <c>UnitY</c> toward <c>UnitZ</c>.</summary>
        public static Quaternion<T> RotationX(T angle)
        {
            var (sin, cos) = T.SinCos(angle / FloatingPoint<T>.Two);
            return new(sin, T.Zero, T.Zero, cos);
        }

        /// <summary>The rotation about the y axis; a positive angle turns <c>UnitZ</c> toward <c>UnitX</c>.</summary>
        public static Quaternion<T> RotationY(T angle)
        {
            var (sin, cos) = T.SinCos(angle / FloatingPoint<T>.Two);
            return new(T.Zero, sin, T.Zero, cos);
        }

        /// <summary>The rotation about the z axis; a positive angle turns <c>UnitX</c> toward <c>UnitY</c>.</summary>
        public static Quaternion<T> RotationZ(T angle)
        {
            var (sin, cos) = T.SinCos(angle / FloatingPoint<T>.Two);
            return new(T.Zero, T.Zero, sin, cos);
        }

        /// <summary>
        /// The unit quaternion of a rotation matrix, by Shepperd's method, which takes the square
        /// root of the largest of 1 + trace and the 1 + 2 Mii - trace and so stays accurate for
        /// every angle. The matrix must be a rotation: orthonormal, with determinant 1.
        /// </summary>
        public static Quaternion<T> FromRotationMatrix(Matrix3x3<T> matrix)
        {
            T one = T.One, two = FloatingPoint<T>.Two;
            T m11 = matrix.M11, m22 = matrix.M22, m33 = matrix.M33;
            T trace = m11 + m22 + m33;

            Quaternion<T> q;
            if (trace > T.Zero)
            {
                T s = two * T.Sqrt(one + trace);   // 4 w
                q = new((matrix.M32 - matrix.M23) / s, (matrix.M13 - matrix.M31) / s, (matrix.M21 - matrix.M12) / s, s / (two * two));
            }
            else if (m11 > m22 && m11 > m33)
            {
                T s = two * T.Sqrt(one + m11 - m22 - m33);   // 4 x
                q = new(s / (two * two), (matrix.M12 + matrix.M21) / s, (matrix.M13 + matrix.M31) / s, (matrix.M32 - matrix.M23) / s);
            }
            else if (m22 > m33)
            {
                T s = two * T.Sqrt(one + m22 - m11 - m33);   // 4 y
                q = new((matrix.M12 + matrix.M21) / s, s / (two * two), (matrix.M23 + matrix.M32) / s, (matrix.M13 - matrix.M31) / s);
            }
            else
            {
                T s = two * T.Sqrt(one + m33 - m11 - m22);   // 4 z
                q = new((matrix.M13 + matrix.M31) / s, (matrix.M23 + matrix.M32) / s, s / (two * two), (matrix.M21 - matrix.M12) / s);
            }

            return q.Normalize();
        }

        /// <summary>The unit quaternion of the rotation in the top left 3 x 3 corner; see the overload for a <see cref="Matrix3x3{T}"/>.</summary>
        public static Quaternion<T> FromRotationMatrix(Matrix4x4<T> matrix) => FromRotationMatrix(new Matrix3x3<T>(
            matrix.M11, matrix.M12, matrix.M13,
            matrix.M21, matrix.M22, matrix.M23,
            matrix.M31, matrix.M32, matrix.M33));

        /// <summary>The norm sqrt(x^2 + y^2 + z^2 + w^2); it neither overflows nor underflows.</summary>
        public T Norm() => new Vector4<T>(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W).Norm();

        /// <summary>The unit quaternion in the same direction, the same rotation with norm 1.</summary>
        /// <exception cref="InvalidOperationException">The quaternion is zero.</exception>
        public Quaternion<T> Normalize()
        {
            if (quaternion == default)
                throw new InvalidOperationException("Cannot normalize the zero quaternion.");

            var (x, y, z, w) = new Vector4<T>(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W).Normalize();
            return new(x, y, z, w);
        }

        /// <summary>The inverse, the conjugate over the squared norm: <c>q * q.Inverse()</c> is 1.</summary>
        /// <exception cref="InvalidOperationException">The quaternion is zero.</exception>
        public Quaternion<T> Inverse()
        {
            if (quaternion == default)
                throw new InvalidOperationException("The zero quaternion has no inverse.");

            // The conjugate of the unit quaternion over the norm, which neither overflows nor underflows.
            return quaternion.Normalize().Conjugate() / quaternion.Norm();
        }

        /// <summary>
        /// The vector turned by the rotation: q v q^-1, by v + 2 (w c + u x c) / |q|^2 with the
        /// vector part u and c = u x v.
        /// </summary>
        /// <exception cref="InvalidOperationException">The quaternion is zero.</exception>
        public Vector3<T> Rotate(Vector3<T> vector)
        {
            Quaternion<T> q = Rotation(quaternion, out T normSquared);
            Vector3<T> u = q.XYZ, c = u.Cross(vector);
            return vector + (q.W * c + u.Cross(c)) * (FloatingPoint<T>.Two / normSquared);
        }

        /// <summary>
        /// The axis, of length 1, and the angle, from 0 to pi, of the rotation. For the identity,
        /// whose axis is undefined, the axis is <c>UnitX</c> and the angle 0.
        /// </summary>
        /// <exception cref="InvalidOperationException">The quaternion is zero.</exception>
        public (Vector3<T> Axis, T Angle) ToAxisAngle()
        {
            Quaternion<T> q = quaternion.Normalize();
            if (q.W < T.Zero)
                q = -q;   // the same rotation, the other way round the axis

            T sine = q.XYZ.Norm();
            T angle = FloatingPoint<T>.Two * T.Atan2(sine, q.W);
            return (sine == T.Zero ? Vector3<T>.UnitX : q.XYZ / sine, angle);
        }

        /// <summary>
        /// The angle of the rotation from this one to <paramref name="other"/>, from 0 to pi; 0 for
        /// a quaternion and its negative. Kahan's formula keeps it accurate for small angles too.
        /// </summary>
        /// <exception cref="InvalidOperationException">A quaternion is zero.</exception>
        public T Angle(Quaternion<T> other)
        {
            Quaternion<T> a = quaternion.Normalize(), b = other.Normalize();
            if (a.Dot(b) < T.Zero)
                b = -b;

            // Half the angle between them as vectors of four numbers is a quarter of the turn.
            T two = FloatingPoint<T>.Two;
            return two * two * T.Atan2((a - b).Norm(), (a + b).Norm());
        }

        /// <summary>
        /// The spherical linear interpolation: the rotation at <paramref name="t"/> of the way
        /// from <paramref name="start"/> to <paramref name="end"/> at constant angular speed, the
        /// shorter way round. At t = 0 it is <paramref name="start"/> with norm 1, at t = 1
        /// <paramref name="end"/> or its negative, the same rotation; other values extrapolate.
        /// </summary>
        /// <exception cref="InvalidOperationException">A quaternion is zero.</exception>
        public static Quaternion<T> Slerp(Quaternion<T> start, Quaternion<T> end, T t)
        {
            Quaternion<T> a = start.Normalize(), b = end.Normalize();
            if (a.Dot(b) < T.Zero)
                b = -b;   // the shorter way

            // The angle between them as vectors, by Kahan's formula: accurate when it is small.
            T angle = FloatingPoint<T>.Two * T.Atan2((a - b).Norm(), (a + b).Norm());
            T sin = T.Sin(angle);
            if (sin == T.Zero)
                return a;

            return a * (T.Sin((T.One - t) * angle) / sin) + b * (T.Sin(t * angle) / sin);
        }

        /// <summary>
        /// The normalized linear interpolation, the shorter way round: faster than
        /// <see cref="Slerp"/> and the same path, but at a speed that varies a little. Meant for
        /// quaternions of the same norm, such as unit ones.
        /// </summary>
        /// <exception cref="InvalidOperationException">The result is zero, as for zero quaternions.</exception>
        public static Quaternion<T> Nlerp(Quaternion<T> start, Quaternion<T> end, T t)
        {
            if (start.Dot(end) < T.Zero)
                end = -end;

            return (start * (T.One - t) + end * t).Normalize();
        }

        /// <summary>
        /// Whether the quaternions are equal up to rounding, entry by entry as vectors of four
        /// numbers, as for <see cref="Vector4{T}"/>. A quaternion and its negative are the same
        /// rotation but not close; compare rotations with <see cref="Angle"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A tolerance is negative or NaN.</exception>
        public bool IsApproximately(Quaternion<T> other, T? relativeTolerance = null, T? absoluteTolerance = null) =>
            FloatingPoint<T>.AreClose(
                [quaternion.X, quaternion.Y, quaternion.Z, quaternion.W], [other.X, other.Y, other.Z, other.W],
                relativeTolerance, absoluteTolerance);
    }

    // The quaternion of a rotation and its squared norm, which is safe to divide by: normalized
    // first where the squared norm would overflow or underflow.
    internal static Quaternion<T> Rotation<T>(Quaternion<T> quaternion, out T normSquared)
        where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        if (quaternion == default)
            throw new InvalidOperationException("The zero quaternion is no rotation.");

        normSquared = quaternion.NormSquared();
        if (FloatingPoint<T>.IsSafe(normSquared))
            return quaternion;

        Quaternion<T> unit = quaternion.Normalize();
        normSquared = unit.NormSquared();
        return unit;
    }
}
