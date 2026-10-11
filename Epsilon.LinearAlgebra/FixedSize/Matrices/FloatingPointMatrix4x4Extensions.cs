namespace Epsilon.LinearAlgebra;

/// <summary>
/// Rotations, the inverse and approximate equality of <see cref="Matrix4x4{T}"/> with
/// floating-point entries: float, double or Half. Angles are in radians.
/// </summary>
public static class FloatingPointMatrix4x4Extensions
{
    extension<T>(Matrix4x4<T> matrix) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        /// <summary>The rotation about the x axis; a positive angle turns <c>UnitY</c> toward <c>UnitZ</c>.</summary>
        public static Matrix4x4<T> RotationX(T angle) => new(Matrix3x3<T>.RotationX(angle));

        /// <summary>The rotation about the y axis; a positive angle turns <c>UnitZ</c> toward <c>UnitX</c>.</summary>
        public static Matrix4x4<T> RotationY(T angle) => new(Matrix3x3<T>.RotationY(angle));

        /// <summary>The rotation about the z axis; a positive angle turns <c>UnitX</c> toward <c>UnitY</c>.</summary>
        public static Matrix4x4<T> RotationZ(T angle) => new(Matrix3x3<T>.RotationZ(angle));

        /// <summary>
        /// The rotation about <paramref name="axis"/> through the origin, which need not have
        /// length 1. A positive angle turns counterclockwise as seen from the tip of the axis
        /// (the right-hand rule).
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="axis"/> is zero.</exception>
        public static Matrix4x4<T> Rotation(Vector3<T> axis, T angle) => new(Matrix3x3<T>.Rotation(axis, angle));

        /// <summary>
        /// The rotation by Euler angles about the fixed axes in the given order; see
        /// <c>Matrix3x3&lt;T&gt;.FromEulerAngles</c> and <see cref="EulerOrder"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="order"/> is not an Euler order.</exception>
        public static Matrix4x4<T> FromEulerAngles(Vector3<T> angles, EulerOrder order) =>
            new(Matrix3x3<T>.FromEulerAngles(angles, order));

        /// <summary>
        /// The Euler angles of the rotation in the top left 3 x 3 corner, which must be a rotation;
        /// see <c>Matrix3x3&lt;T&gt;.ToEulerAngles</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="order"/> is not an Euler order.</exception>
        public Vector3<T> ToEulerAngles(EulerOrder order) => new Matrix3x3<T>(
            matrix.M11, matrix.M12, matrix.M13,
            matrix.M21, matrix.M22, matrix.M23,
            matrix.M31, matrix.M32, matrix.M33).ToEulerAngles(order);

        /// <summary>
        /// The rotation of a quaternion, which need not have norm 1: the matrix of
        /// <c>rotation.Rotate</c>, so that <c>Rotation(p * q)</c> is <c>Rotation(p) * Rotation(q)</c>.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="rotation"/> is zero.</exception>
        public static Matrix4x4<T> Rotation(Quaternion<T> rotation) => new(Matrix3x3<T>.Rotation(rotation));

        /// <summary>
        /// The transform that scales by <paramref name="scale"/> along the axes, then turns by
        /// <paramref name="rotation"/>, then moves by <paramref name="translation"/>: T R S, the
        /// usual model matrix of an object. The quaternion need not have norm 1.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="rotation"/> is zero.</exception>
        public static Matrix4x4<T> Compose(Vector3<T> translation, Quaternion<T> rotation, Vector3<T> scale)
        {
            Matrix3x3<T> r = Matrix3x3<T>.Rotation(rotation);
            return new(
                r.M11 * scale.X, r.M12 * scale.Y, r.M13 * scale.Z, translation.X,
                r.M21 * scale.X, r.M22 * scale.Y, r.M23 * scale.Z, translation.Y,
                r.M31 * scale.X, r.M32 * scale.Y, r.M33 * scale.Z, translation.Z,
                T.Zero, T.Zero, T.Zero, T.One);
        }

        /// <summary>
        /// The inverse, the map that undoes this one; see <see cref="TryInverse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The matrix is singular to working precision or has an entry that is NaN or infinite.
        /// </exception>
        public Matrix4x4<T> Inverse() =>
            matrix.TryInverse(out Matrix4x4<T> inverse) ? inverse
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
        public bool TryInverse(out Matrix4x4<T> inverse)
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
            T d1 = RowScale(matrix.Row(0)), d2 = RowScale(matrix.Row(1)), d3 = RowScale(matrix.Row(2)), d4 = RowScale(matrix.Row(3));
            var scaled = Matrix4x4<T>.FromRows(d1 * matrix.Row(0), d2 * matrix.Row(1), d3 * matrix.Row(2), d4 * matrix.Row(3));
            if (!Invert(scaled, out Matrix4x4<T> b, out _))
            {
                inverse = default;
                return false;
            }

            Vector4<T> factors = (d1, d2, d3, d4);
            inverse = Matrix4x4<T>.FromRows(
                b.Row(0).Hadamard(factors), b.Row(1).Hadamard(factors), b.Row(2).Hadamard(factors), b.Row(3).Hadamard(factors));
            return true;
        }

        /// <summary>
        /// Splits an affine transform into T R S, as <see cref="Compose"/> builds it: the
        /// translation from the last column, the scale from the lengths of the other columns and
        /// the rotation from those columns divided by them. A mirror image shows as a negative
        /// <c>scale.X</c>. There is no such split, and the result is <c>false</c>, if the last row is
        /// not (0, 0, 0, 1), if a scale is 0, if an entry is NaN or infinite, or for a shear: when the
        /// columns are not perpendicular to within the square root of the machine epsilon.
        /// </summary>
        /// <param name="translation">The translation, or zero if there is no split.</param>
        /// <param name="rotation">The rotation as a unit quaternion, or zero if there is no split.</param>
        /// <param name="scale">The scale along the axes, or zero if there is no split.</param>
        /// <returns>Whether the matrix is T R S.</returns>
        public bool TryDecompose(out Vector3<T> translation, out Quaternion<T> rotation, out Vector3<T> scale)
        {
            translation = default;
            rotation = default;
            scale = default;
            if (matrix.M41 != T.Zero || matrix.M42 != T.Zero || matrix.M43 != T.Zero || matrix.M44 != T.One
                || !T.IsFinite(matrix.M14) || !T.IsFinite(matrix.M24) || !T.IsFinite(matrix.M34))
                return false;

            // The columns are the images of the unit vectors: the axes of R, times the scales.
            Vector3<T> x = (matrix.M11, matrix.M21, matrix.M31), y = (matrix.M12, matrix.M22, matrix.M32), z = (matrix.M13, matrix.M23, matrix.M33);
            T sx = x.Norm(), sy = y.Norm(), sz = z.Norm();
            if (!IsPositiveFinite(sx) || !IsPositiveFinite(sy) || !IsPositiveFinite(sz))
                return false;

            x /= sx;
            y /= sy;
            z /= sz;
            if (x.Cross(y).Dot(z) < T.Zero)
            {
                sx = -sx;   // a mirror image
                x = -x;
            }

            T tolerance = FloatingPoint<T>.SqrtMachineEpsilon;
            if (T.Abs(x.Dot(y)) > tolerance || T.Abs(x.Dot(z)) > tolerance || T.Abs(y.Dot(z)) > tolerance)
                return false;   // a shear

            translation = (matrix.M14, matrix.M24, matrix.M34);
            rotation = Quaternion<T>.FromRotationMatrix(Matrix3x3<T>.FromColumns(x, y, z));
            scale = (sx, sy, sz);
            return true;
        }

        /// <summary>
        /// The matrix that maps normals: the inverse transpose of the top left 3 x 3 corner. A
        /// normal must stay perpendicular to the surface, which <see cref="Matrix4x4{T}.TransformDirection"/>
        /// doesn't keep once the scale differs between the axes. Compute it once for many normals.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The top left corner is singular to working precision or has an entry that is NaN or infinite.
        /// </exception>
        public Matrix3x3<T> NormalMatrix()
        {
            var linear = new Matrix3x3<T>(
                matrix.M11, matrix.M12, matrix.M13,
                matrix.M21, matrix.M22, matrix.M23,
                matrix.M31, matrix.M32, matrix.M33);
            if (!linear.TryInverse(out Matrix3x3<T> inverse))
                throw new InvalidOperationException(
                    "The top left 3 x 3 corner is singular to working precision or has an entry that is NaN or infinite, so normals have no image.");

            return inverse.Transpose();
        }

        /// <summary>
        /// The normal of a surface mapped with it: the product with <see cref="NormalMatrix"/>, not
        /// normalized. Translations leave it alone.
        /// </summary>
        /// <exception cref="InvalidOperationException">The top left 3 x 3 corner has no inverse.</exception>
        public Vector3<T> TransformNormal(Vector3<T> normal) => matrix.NormalMatrix() * normal;

        /// <summary>
        /// <see cref="TransformNormal"/> of every vector in <paramref name="normals"/>, written to
        /// <paramref name="destination"/> at the same index, with the normal matrix computed once.
        /// The destination may be the source itself.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="destination"/> is shorter than <paramref name="normals"/>, or overlaps it at another position.
        /// </exception>
        /// <exception cref="InvalidOperationException">The top left 3 x 3 corner has no inverse.</exception>
        public void TransformNormals(ReadOnlySpan<Vector3<T>> normals, Span<Vector3<T>> destination)
        {
            Destination.Check(normals, destination);
            Matrix3x3<T> normalMatrix = matrix.NormalMatrix();
            for (int i = 0; i < normals.Length; i++)
                destination[i] = normalMatrix * normals[i];
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
        public bool IsApproximately(Matrix4x4<T> other, T? relativeTolerance = null, T? absoluteTolerance = null) =>
            FloatingPoint<T>.AreClose(
                [
                    matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                    matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44,
                ],
                [
                    other.M11, other.M12, other.M13, other.M14, other.M21, other.M22, other.M23, other.M24,
                    other.M31, other.M32, other.M33, other.M34, other.M41, other.M42, other.M43, other.M44,
                ],
                relativeTolerance, absoluteTolerance);
    }

    // The adjugate over the determinant by the Laplace expansion along the first two rows, unless
    // the determinant is 0 to working precision. The permanent of |A|, the sum of the magnitudes
    // of the terms of the determinant, is the scale of its rounding error.
    private static bool Invert<T>(Matrix4x4<T> m, out Matrix4x4<T> inverse, out T permanent)
        where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        // The 2 x 2 minors of the first two rows (s) and of the last two (c).
        T s0 = m.M11 * m.M22 - m.M21 * m.M12, s1 = m.M11 * m.M23 - m.M21 * m.M13, s2 = m.M11 * m.M24 - m.M21 * m.M14;
        T s3 = m.M12 * m.M23 - m.M22 * m.M13, s4 = m.M12 * m.M24 - m.M22 * m.M14, s5 = m.M13 * m.M24 - m.M23 * m.M14;
        T c5 = m.M33 * m.M44 - m.M43 * m.M34, c4 = m.M32 * m.M44 - m.M42 * m.M34, c3 = m.M32 * m.M43 - m.M42 * m.M33;
        T c2 = m.M31 * m.M44 - m.M41 * m.M34, c1 = m.M31 * m.M43 - m.M41 * m.M33, c0 = m.M31 * m.M42 - m.M41 * m.M32;
        T determinant = s0 * c5 - s1 * c4 + s2 * c3 + s3 * c2 - s4 * c1 + s5 * c0;
        permanent = Magnitude(m.M11, m.M22, m.M21, m.M12) * Magnitude(m.M33, m.M44, m.M43, m.M34)
            + Magnitude(m.M11, m.M23, m.M21, m.M13) * Magnitude(m.M32, m.M44, m.M42, m.M34)
            + Magnitude(m.M11, m.M24, m.M21, m.M14) * Magnitude(m.M32, m.M43, m.M42, m.M33)
            + Magnitude(m.M12, m.M23, m.M22, m.M13) * Magnitude(m.M31, m.M44, m.M41, m.M34)
            + Magnitude(m.M12, m.M24, m.M22, m.M14) * Magnitude(m.M31, m.M43, m.M41, m.M33)
            + Magnitude(m.M13, m.M24, m.M23, m.M14) * Magnitude(m.M31, m.M42, m.M41, m.M32);

        if (FloatingPoint<T>.IsNegligible(determinant, permanent, FloatingPoint<T>.DeterminantError4))
        {
            inverse = default;
            return false;
        }

        T r = T.One / determinant;
        inverse = new(
            (m.M22 * c5 - m.M23 * c4 + m.M24 * c3) * r,
            (-m.M12 * c5 + m.M13 * c4 - m.M14 * c3) * r,
            (m.M42 * s5 - m.M43 * s4 + m.M44 * s3) * r,
            (-m.M32 * s5 + m.M33 * s4 - m.M34 * s3) * r,

            (-m.M21 * c5 + m.M23 * c2 - m.M24 * c1) * r,
            (m.M11 * c5 - m.M13 * c2 + m.M14 * c1) * r,
            (-m.M41 * s5 + m.M43 * s2 - m.M44 * s1) * r,
            (m.M31 * s5 - m.M33 * s2 + m.M34 * s1) * r,

            (m.M21 * c4 - m.M22 * c2 + m.M24 * c0) * r,
            (-m.M11 * c4 + m.M12 * c2 - m.M14 * c0) * r,
            (m.M41 * s4 - m.M42 * s2 + m.M44 * s0) * r,
            (-m.M31 * s4 + m.M32 * s2 - m.M34 * s0) * r,

            (-m.M21 * c3 + m.M22 * c1 - m.M23 * c0) * r,
            (m.M11 * c3 - m.M12 * c1 + m.M13 * c0) * r,
            (-m.M41 * s3 + m.M42 * s1 - m.M43 * s0) * r,
            (m.M31 * s3 - m.M32 * s1 + m.M33 * s0) * r);
        return true;
    }

    private static bool IsPositiveFinite<T>(T value) where T : struct, System.Numerics.IFloatingPointIeee754<T> =>
        value > T.Zero && T.IsFinite(value);

    // |a b| + |c d|, the sum of the magnitudes of the terms of the minor a b - c d.
    private static T Magnitude<T>(T a, T b, T c, T d) where T : struct, System.Numerics.IFloatingPointIeee754<T> =>
        T.Abs(a * b) + T.Abs(c * d);

    private static T RowScale<T>(Vector4<T> row) where T : struct, System.Numerics.IFloatingPointIeee754<T> =>
        FloatingPoint<T>.RowScale(T.MaxMagnitude(T.MaxMagnitude(row.X, row.Y), T.MaxMagnitude(row.Z, row.W)));

    // The largest magnitude of an entry; NaN if an entry is NaN.
    private static T Largest<T>(Matrix4x4<T> m) where T : struct, System.Numerics.IFloatingPointIeee754<T> =>
        T.Abs(T.MaxMagnitude(
            T.MaxMagnitude(LargestEntry(m.Row(0)), LargestEntry(m.Row(1))),
            T.MaxMagnitude(LargestEntry(m.Row(2)), LargestEntry(m.Row(3)))));

    private static T LargestEntry<T>(Vector4<T> row) where T : struct, System.Numerics.IFloatingPointIeee754<T> =>
        T.MaxMagnitude(T.MaxMagnitude(row.X, row.Y), T.MaxMagnitude(row.Z, row.W));
}
