namespace Epsilon.LinearAlgebra;

/// <summary>
/// Rotations, the inverse and approximate equality of <see cref="Matrix3x3{T}"/> with
/// floating-point entries: float, double or Half. Angles are in radians.
/// </summary>
public static class FloatingPointMatrix3x3Extensions
{
    extension<T>(Matrix3x3<T> matrix) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        /// <summary>The rotation about the x axis; a positive angle turns <c>UnitY</c> toward <c>UnitZ</c>.</summary>
        public static Matrix3x3<T> RotationX(T angle)
        {
            var (sin, cos) = T.SinCos(angle);
            return new(T.One, T.Zero, T.Zero, T.Zero, cos, -sin, T.Zero, sin, cos);
        }

        /// <summary>The rotation about the y axis; a positive angle turns <c>UnitZ</c> toward <c>UnitX</c>.</summary>
        public static Matrix3x3<T> RotationY(T angle)
        {
            var (sin, cos) = T.SinCos(angle);
            return new(cos, T.Zero, sin, T.Zero, T.One, T.Zero, -sin, T.Zero, cos);
        }

        /// <summary>
        /// The rotation about the z axis; a positive angle turns <c>UnitX</c> toward <c>UnitY</c>.
        /// In homogeneous coordinates of the plane it is the rotation about the origin.
        /// </summary>
        public static Matrix3x3<T> RotationZ(T angle)
        {
            var (sin, cos) = T.SinCos(angle);
            return new(cos, -sin, T.Zero, sin, cos, T.Zero, T.Zero, T.Zero, T.One);
        }

        /// <summary>
        /// The rotation about <paramref name="axis"/>, which need not have length 1, by Rodrigues'
        /// formula. A positive angle turns counterclockwise as seen from the tip of the axis
        /// (the right-hand rule).
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="axis"/> is zero.</exception>
        public static Matrix3x3<T> Rotation(Vector3<T> axis, T angle)
        {
            if (axis == Vector3<T>.Zero)
                throw new ArgumentException("The axis is the zero vector.", nameof(axis));

            var (x, y, z) = axis.Normalize();
            var (sin, cos) = T.SinCos(angle);
            T t = T.One - cos;
            return new(
                cos + x * x * t, x * y * t - z * sin, x * z * t + y * sin,
                x * y * t + z * sin, cos + y * y * t, y * z * t - x * sin,
                x * z * t - y * sin, y * z * t + x * sin, cos + z * z * t);
        }

        /// <summary>
        /// The affine map of the plane, in homogeneous coordinates, that scales by
        /// <paramref name="scale"/> along the axes, then turns by <paramref name="angle"/> about the
        /// origin, then moves by <paramref name="translation"/>: T R S, the transform of a sprite.
        /// </summary>
        public static Matrix3x3<T> Compose(Vector2<T> translation, T angle, Vector2<T> scale)
        {
            var (sin, cos) = T.SinCos(angle);
            return new(
                cos * scale.X, -sin * scale.Y, translation.X,
                sin * scale.X, cos * scale.Y, translation.Y,
                T.Zero, T.Zero, T.One);
        }

        /// <summary>
        /// The rotation by Euler angles: <c>angles.X</c> about x, <c>angles.Y</c> about y and
        /// <c>angles.Z</c> about z, applied in the given order about the fixed axes; see
        /// <see cref="EulerOrder"/>. For <see cref="EulerOrder.XYZ"/> it is Rz Ry Rx.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="order"/> is not an Euler order.</exception>
        public static Matrix3x3<T> FromEulerAngles(Vector3<T> angles, EulerOrder order)
        {
            var (a, b, c, _) = EulerAngles.Axes(order);
            return AxisRotation(c, angles[c]) * AxisRotation(b, angles[b]) * AxisRotation(a, angles[a]);
        }

        /// <summary>
        /// The Euler angles of a rotation matrix in the given order, so that
        /// <see cref="FromEulerAngles"/> gives the matrix back. The middle angle of the order is in
        /// [-pi/2, pi/2], the others in [-pi, pi]; at gimbal lock the last one is 0. The matrix
        /// must be a rotation: orthonormal, with determinant 1.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="order"/> is not an Euler order.</exception>
        public Vector3<T> ToEulerAngles(EulerOrder order) => EulerAngles.Of(matrix, order);

        /// <summary>
        /// The rotation of a quaternion, which need not have norm 1: the matrix of
        /// <c>rotation.Rotate</c>, so that <c>Rotation(p * q)</c> is <c>Rotation(p) * Rotation(q)</c>.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="rotation"/> is zero.</exception>
        public static Matrix3x3<T> Rotation(Quaternion<T> rotation)
        {
            if (rotation == default)
                throw new ArgumentException("The quaternion is zero.", nameof(rotation));

            var (x, y, z, w) = FloatingPointQuaternionExtensions.Rotation(rotation, out T normSquared);
            T s = FloatingPoint<T>.Two / normSquared;
            T xs = x * s, ys = y * s, zs = z * s;
            T wx = w * xs, wy = w * ys, wz = w * zs;
            T xx = x * xs, xy = x * ys, xz = x * zs, yy = y * ys, yz = y * zs, zz = z * zs;
            return new(
                T.One - (yy + zz), xy - wz, xz + wy,
                xy + wz, T.One - (xx + zz), yz - wx,
                xz - wy, yz + wx, T.One - (xx + yy));
        }

        /// <summary>
        /// The eigenvalues and eigenvectors of a symmetric matrix, such as an inertia tensor or the
        /// covariance of points, by the Jacobi method: A = V * diag(values) * V^T. The values are in
        /// ascending order, and column j of V is a unit eigenvector for value j; the columns form a
        /// rotation, with determinant 1, and the entry of largest magnitude of the last two is
        /// positive. Only the entries on and below the diagonal are used, once the matrix is known
        /// to be symmetric up to rounding.
        /// </summary>
        /// <exception cref="InvalidOperationException">The matrix is not symmetric or has an entry that is NaN or infinite.</exception>
        public (Vector3<T> Values, Matrix3x3<T> Vectors) SymmetricEigen() => SymmetricEigen3x3<T>.Compute(matrix);

        /// <summary>
        /// The inverse, the map that undoes this one; see <see cref="TryInverse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The matrix is singular to working precision or has an entry that is NaN or infinite.
        /// </exception>
        public Matrix3x3<T> Inverse() =>
            matrix.TryInverse(out Matrix3x3<T> inverse) ? inverse
            : throw new InvalidOperationException(T.IsFinite(Largest(matrix))
                ? "The matrix is singular to working precision."
                : "The matrix has an entry that is NaN or infinite.");

        /// <summary>
        /// The inverse by the closed formula, the adjugate over the determinant, if the matrix is
        /// invertible to working precision: its determinant must be larger than the bound on the
        /// rounding error of the formula. Rows of huge or tiny entries are scaled by powers of two
        /// first, which is exact, so neither overflow nor underflow spoils the result.
        /// </summary>
        /// <param name="inverse">The inverse, or the zero matrix if there is none.</param>
        /// <returns>Whether the matrix is invertible: false if it is singular to working precision or has an entry that is NaN or infinite.</returns>
        public bool TryInverse(out Matrix3x3<T> inverse)
        {
            T largest = Largest(matrix);
            if (!T.IsFinite(largest))
            {
                inverse = default;
                return false;
            }

            if (largest <= FloatingPoint<T>.SafeLargest)
            {
                bool invertible = Invert(matrix, out inverse, out T permanent);
                if (permanent >= FloatingPoint<T>.SafeSmallest)
                    return invertible;
            }

            // Rows scaled by powers of two: B = D A, so A^-1 = B^-1 D, the columns of B^-1 times
            // the factors.
            T d1 = FloatingPoint<T>.RowScale(T.MaxMagnitude(T.MaxMagnitude(matrix.M11, matrix.M12), matrix.M13));
            T d2 = FloatingPoint<T>.RowScale(T.MaxMagnitude(T.MaxMagnitude(matrix.M21, matrix.M22), matrix.M23));
            T d3 = FloatingPoint<T>.RowScale(T.MaxMagnitude(T.MaxMagnitude(matrix.M31, matrix.M32), matrix.M33));
            var scaled = new Matrix3x3<T>(
                d1 * matrix.M11, d1 * matrix.M12, d1 * matrix.M13,
                d2 * matrix.M21, d2 * matrix.M22, d2 * matrix.M23,
                d3 * matrix.M31, d3 * matrix.M32, d3 * matrix.M33);
            if (!Invert(scaled, out Matrix3x3<T> b, out _))
            {
                inverse = default;
                return false;
            }

            inverse = new(
                b.M11 * d1, b.M12 * d2, b.M13 * d3,
                b.M21 * d1, b.M22 * d2, b.M23 * d3,
                b.M31 * d1, b.M32 * d2, b.M33 * d3);
            return true;
        }

        /// <summary>
        /// Splits an affine map of the plane into T R S, as <see cref="Compose"/> builds it: the
        /// translation from the last column, the scale from the lengths of the first two columns and
        /// the angle, in [-pi, pi], from the first of them. A mirror image shows as a negative
        /// <c>scale.X</c>. There is no such split, and the result is <c>false</c>, if the last row is
        /// not (0, 0, 1), if a scale is 0, if an entry is NaN or infinite, or for a shear: when the
        /// columns are not perpendicular to within the square root of the machine epsilon.
        /// </summary>
        /// <param name="translation">The translation, or zero if there is no split.</param>
        /// <param name="angle">The angle of the rotation in radians, or 0 if there is no split.</param>
        /// <param name="scale">The scale along the axes, or zero if there is no split.</param>
        /// <returns>Whether the matrix is T R S.</returns>
        public bool TryDecompose(out Vector2<T> translation, out T angle, out Vector2<T> scale)
        {
            translation = default;
            angle = T.Zero;
            scale = default;
            if (matrix.M31 != T.Zero || matrix.M32 != T.Zero || matrix.M33 != T.One
                || !T.IsFinite(matrix.M13) || !T.IsFinite(matrix.M23))
                return false;

            Vector2<T> x = (matrix.M11, matrix.M21), y = (matrix.M12, matrix.M22);
            T sx = x.Norm(), sy = y.Norm();
            if (!(sx > T.Zero) || !T.IsFinite(sx) || !(sy > T.Zero) || !T.IsFinite(sy))
                return false;

            x /= sx;
            y /= sy;
            if (x.Cross(y) < T.Zero)
            {
                sx = -sx;   // a mirror image
                x = -x;
            }

            if (T.Abs(x.Dot(y)) > FloatingPoint<T>.SqrtMachineEpsilon)
                return false;   // a shear

            translation = (matrix.M13, matrix.M23);
            angle = T.Atan2(x.Y, x.X);
            scale = (sx, sy);
            return true;
        }

        /// <summary>
        /// Whether the matrices are equal up to rounding:
        /// ||A - B|| &lt;= max(relativeTolerance * max(||A||, ||B||), absoluteTolerance) in the
        /// Frobenius norm, as for <see cref="Matrix{T}"/> of double. Without a relative tolerance it
        /// is 1e-9 for double, 1e-5 for float and 1e-2 for Half; against the zero matrix only
        /// <paramref name="absoluteTolerance"/> helps. Infinite entries must be equal, and NaN is
        /// never close to anything.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A tolerance is negative or NaN.</exception>
        public bool IsApproximately(Matrix3x3<T> other, T? relativeTolerance = null, T? absoluteTolerance = null) =>
            FloatingPoint<T>.AreClose(
                [matrix.M11, matrix.M12, matrix.M13, matrix.M21, matrix.M22, matrix.M23, matrix.M31, matrix.M32, matrix.M33],
                [other.M11, other.M12, other.M13, other.M21, other.M22, other.M23, other.M31, other.M32, other.M33],
                relativeTolerance, absoluteTolerance);
    }

    private static Matrix3x3<T> AxisRotation<T>(int axis, T angle) where T : struct, System.Numerics.IFloatingPointIeee754<T> =>
        axis switch
        {
            0 => Matrix3x3<T>.RotationX(angle),
            1 => Matrix3x3<T>.RotationY(angle),
            _ => Matrix3x3<T>.RotationZ(angle),
        };

    // The adjugate over the determinant, unless the determinant is 0 to working precision. The
    // permanent of |A|, the sum of the magnitudes of the terms of the determinant, is the scale of
    // its rounding error.
    private static bool Invert<T>(Matrix3x3<T> m, out Matrix3x3<T> inverse, out T permanent)
        where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        T c11 = m.M22 * m.M33 - m.M23 * m.M32;
        T c12 = m.M23 * m.M31 - m.M21 * m.M33;
        T c13 = m.M21 * m.M32 - m.M22 * m.M31;
        T determinant = m.M11 * c11 + m.M12 * c12 + m.M13 * c13;
        permanent = T.Abs(m.M11) * (T.Abs(m.M22 * m.M33) + T.Abs(m.M23 * m.M32))
            + T.Abs(m.M12) * (T.Abs(m.M23 * m.M31) + T.Abs(m.M21 * m.M33))
            + T.Abs(m.M13) * (T.Abs(m.M21 * m.M32) + T.Abs(m.M22 * m.M31));

        if (FloatingPoint<T>.IsNegligible(determinant, permanent, FloatingPoint<T>.DeterminantError3))
        {
            inverse = default;
            return false;
        }

        T r = T.One / determinant;
        inverse = new(
            c11 * r, (m.M13 * m.M32 - m.M12 * m.M33) * r, (m.M12 * m.M23 - m.M13 * m.M22) * r,
            c12 * r, (m.M11 * m.M33 - m.M13 * m.M31) * r, (m.M13 * m.M21 - m.M11 * m.M23) * r,
            c13 * r, (m.M12 * m.M31 - m.M11 * m.M32) * r, (m.M11 * m.M22 - m.M12 * m.M21) * r);
        return true;
    }

    // The largest magnitude of an entry; NaN if an entry is NaN.
    private static T Largest<T>(Matrix3x3<T> m) where T : struct, System.Numerics.IFloatingPointIeee754<T> =>
        T.Abs(T.MaxMagnitude(
            T.MaxMagnitude(T.MaxMagnitude(m.M11, m.M12), T.MaxMagnitude(m.M13, m.M21)),
            T.MaxMagnitude(T.MaxMagnitude(T.MaxMagnitude(m.M22, m.M23), T.MaxMagnitude(m.M31, m.M32)), m.M33)));
}
