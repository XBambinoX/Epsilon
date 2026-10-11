namespace Epsilon.LinearAlgebra;

/// <summary>
/// Conversions between the fixed-size types of float and those of System.Numerics. System.Numerics
/// multiplies row vectors from the left (<c>v * M</c>), so its matrices are the transposes of these:
/// the translation of a <see cref="System.Numerics.Matrix4x4"/> is in its last row. The conversions
/// transpose, so a map stays the same map, and <c>a * b</c> here is <c>b * a</c> there.
/// </summary>
public static class SystemNumericsExtensions
{
    extension(Vector2<float> vector)
    {
        /// <summary>The same vector as a <see cref="System.Numerics.Vector2"/>.</summary>
        public System.Numerics.Vector2 ToSystemNumerics() => new(vector.X, vector.Y);

        /// <summary>The vector with the entries of a <see cref="System.Numerics.Vector2"/>.</summary>
        public static Vector2<float> FromSystemNumerics(System.Numerics.Vector2 value) => new(value.X, value.Y);
    }

    extension(Vector3<float> vector)
    {
        /// <summary>The same vector as a <see cref="System.Numerics.Vector3"/>.</summary>
        public System.Numerics.Vector3 ToSystemNumerics() => new(vector.X, vector.Y, vector.Z);

        /// <summary>The vector with the entries of a <see cref="System.Numerics.Vector3"/>.</summary>
        public static Vector3<float> FromSystemNumerics(System.Numerics.Vector3 value) => new(value.X, value.Y, value.Z);
    }

    extension(Vector4<float> vector)
    {
        /// <summary>The same vector as a <see cref="System.Numerics.Vector4"/>.</summary>
        public System.Numerics.Vector4 ToSystemNumerics() => new(vector.X, vector.Y, vector.Z, vector.W);

        /// <summary>The vector with the entries of a <see cref="System.Numerics.Vector4"/>.</summary>
        public static Vector4<float> FromSystemNumerics(System.Numerics.Vector4 value) => new(value.X, value.Y, value.Z, value.W);
    }

    extension(Quaternion<float> quaternion)
    {
        /// <summary>The same quaternion as a <see cref="System.Numerics.Quaternion"/>, with the entries in the same order.</summary>
        public System.Numerics.Quaternion ToSystemNumerics() => new(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

        /// <summary>The quaternion with the entries of a <see cref="System.Numerics.Quaternion"/>.</summary>
        public static Quaternion<float> FromSystemNumerics(System.Numerics.Quaternion value) => new(value.X, value.Y, value.Z, value.W);
    }

    extension(Matrix3x3<float> matrix)
    {
        /// <summary>
        /// The same affine map of the plane as a <see cref="System.Numerics.Matrix3x2"/>, which
        /// holds the transposed first two rows: the translation (M13, M23) becomes its last row.
        /// </summary>
        /// <exception cref="InvalidOperationException">The last row is not (0, 0, 1), so the map is not affine.</exception>
        public System.Numerics.Matrix3x2 ToSystemNumerics()
        {
            if (matrix.M31 != 0 || matrix.M32 != 0 || matrix.M33 != 1)
                throw new InvalidOperationException(FormattableString.Invariant(
                    $"Only a matrix with the last row [0, 0, 1] converts to a Matrix3x2, not {matrix.Row(2)}."));

            return new(matrix.M11, matrix.M21, matrix.M12, matrix.M22, matrix.M13, matrix.M23);
        }

        /// <summary>
        /// The matrix of the affine map of the plane that a <see cref="System.Numerics.Matrix3x2"/>
        /// holds, in homogeneous coordinates: its transpose with the last row (0, 0, 1).
        /// </summary>
        public static Matrix3x3<float> FromSystemNumerics(System.Numerics.Matrix3x2 value) => new(
            value.M11, value.M21, value.M31,
            value.M12, value.M22, value.M32,
            0, 0, 1);
    }

    extension(Matrix4x4<float> matrix)
    {
        /// <summary>The same map as a <see cref="System.Numerics.Matrix4x4"/>: the transpose.</summary>
        public System.Numerics.Matrix4x4 ToSystemNumerics() => new(
            matrix.M11, matrix.M21, matrix.M31, matrix.M41,
            matrix.M12, matrix.M22, matrix.M32, matrix.M42,
            matrix.M13, matrix.M23, matrix.M33, matrix.M43,
            matrix.M14, matrix.M24, matrix.M34, matrix.M44);

        /// <summary>The matrix of the map that a <see cref="System.Numerics.Matrix4x4"/> holds: its transpose.</summary>
        public static Matrix4x4<float> FromSystemNumerics(System.Numerics.Matrix4x4 value) => new(
            value.M11, value.M21, value.M31, value.M41,
            value.M12, value.M22, value.M32, value.M42,
            value.M13, value.M23, value.M33, value.M43,
            value.M14, value.M24, value.M34, value.M44);
    }
}
