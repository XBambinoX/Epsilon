using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

// Rotations and homogeneous transforms with symbolic angles and offsets, to build kinematic
// chains whose positions and Jacobians are then derived and compiled. The conventions are those
// of the fixed-size matrices: column vectors, right-handed, angles in radians.
public static partial class SymbolicMatrixExtensions
{
    extension(Matrix<Expr> matrix)
    {
        /// <summary>
        /// The 2 x 2 rotation of the plane by <paramref name="angle"/>, counterclockwise:
        /// <c>[[cos(t), -sin(t)], [sin(t), cos(t)]]</c>. The entries are simplified, so known
        /// angles give exact values: <c>Rotation(pi / 2)</c> is <c>[[0, -1], [1, 0]]</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="angle"/> is null.</exception>
        public static Matrix<Expr> Rotation(Expr angle)
        {
            var (sin, cos) = SinCos(angle);
            return Matrix<Expr>.FromRows([cos, Negated(sin)], [sin, cos]);
        }

        /// <summary>
        /// The 3 x 3 rotation about the x axis; a positive angle turns the y axis toward the z
        /// axis. Simplified entries, as for <see cref="Rotation(Expr)"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="angle"/> is null.</exception>
        public static Matrix<Expr> RotationX(Expr angle)
        {
            var (sin, cos) = SinCos(angle);
            return Matrix<Expr>.FromRows([OneEntry, ZeroEntry, ZeroEntry], [ZeroEntry, cos, Negated(sin)], [ZeroEntry, sin, cos]);
        }

        /// <summary>
        /// The 3 x 3 rotation about the y axis; a positive angle turns the z axis toward the x
        /// axis. Simplified entries, as for <see cref="Rotation(Expr)"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="angle"/> is null.</exception>
        public static Matrix<Expr> RotationY(Expr angle)
        {
            var (sin, cos) = SinCos(angle);
            return Matrix<Expr>.FromRows([cos, ZeroEntry, sin], [ZeroEntry, OneEntry, ZeroEntry], [Negated(sin), ZeroEntry, cos]);
        }

        /// <summary>
        /// The 3 x 3 rotation about the z axis; a positive angle turns the x axis toward the y
        /// axis. It is also <see cref="Rotation(Expr)"/> in homogeneous coordinates of the plane.
        /// Simplified entries.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="angle"/> is null.</exception>
        public static Matrix<Expr> RotationZ(Expr angle)
        {
            var (sin, cos) = SinCos(angle);
            return Matrix<Expr>.FromRows([cos, Negated(sin), ZeroEntry], [sin, cos, ZeroEntry], [ZeroEntry, ZeroEntry, OneEntry]);
        }

        /// <summary>
        /// The translation by <paramref name="offset"/> in homogeneous coordinates: the identity
        /// one size larger than the offset, with the offset in its last column. An offset of
        /// length 3 gives a 4 x 4 matrix, as <c>Matrix4x4.Translation</c> does; the entries of the
        /// offset are used as they are.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="offset"/> is null.</exception>
        public static Matrix<Expr> Translation(Vector<Expr> offset)
        {
            ArgumentNullException.ThrowIfNull(offset);

            int n = offset.Length;
            return Matrix<Expr>.Create(n + 1, n + 1, (i, j) =>
                j == n && i < n ? offset[i] : i == j ? OneEntry : ZeroEntry);
        }

        /// <summary>
        /// The same linear map in homogeneous coordinates, to combine with translations:
        /// <c>[[A, 0], [0, 1]]</c>, one row and one column larger. A scaling is
        /// <c>FromDiagonal(factors).ToHomogeneous()</c>.
        /// </summary>
        public Matrix<Expr> ToHomogeneous() =>
            Matrix<Expr>.Create(matrix.Rows + 1, matrix.Columns + 1, (i, j) =>
                i < matrix.Rows && j < matrix.Columns ? matrix[i, j]
                : i == matrix.Rows && j == matrix.Columns ? OneEntry
                : ZeroEntry);

        /// <summary>
        /// The image of a point under a transform in homogeneous coordinates: the point extended
        /// by a 1, multiplied, and divided by the last entry of the product unless that is exactly
        /// 1. Simplified. With a chain of rotations and translations this gives the position of
        /// its end, ready for <c>Jacobian</c> and <c>Compile</c>.
        /// </summary>
        /// <exception cref="ArgumentException">The matrix is not one size larger than the point in both dimensions.</exception>
        public Vector<Expr> TransformPoint(Vector<Expr> point)
        {
            CheckHomogeneous(matrix, point, nameof(point));

            int n = point.Length;
            Vector<Expr> image = matrix * Vector.Create<Expr>([.. point, OneEntry]);
            Expr w = image[n];

            return w.Equals(OneEntry)
                ? image[..n]
                : image[..n].Map(entry => (entry / w).Simplify());
        }

        /// <summary>
        /// The image of a direction under a transform in homogeneous coordinates: only the linear
        /// part acts, so translations don't move it. Simplified.
        /// </summary>
        /// <exception cref="ArgumentException">The matrix is not one size larger than the direction in both dimensions.</exception>
        public Vector<Expr> TransformDirection(Vector<Expr> direction)
        {
            CheckHomogeneous(matrix, direction, nameof(direction));

            int n = direction.Length;
            return matrix[..n, ..n] * direction;
        }
    }

    private static (Expr Sin, Expr Cos) SinCos(Expr angle)
    {
        ArgumentNullException.ThrowIfNull(angle);
        return (new Sin(angle).Simplify(), new Cos(angle).Simplify());
    }

    private static Expr Negated(Expr value) => (-value).Simplify();

    private static void CheckHomogeneous(Matrix<Expr> matrix, Vector<Expr> vector, string paramName)
    {
        ArgumentNullException.ThrowIfNull(vector, paramName);

        int size = vector.Length + 1;
        if (matrix.Rows != size || matrix.Columns != size)
            throw new ArgumentException(
                $"Transforming a vector of length {vector.Length} needs a {size} x {size} matrix in homogeneous coordinates, not {matrix.Size}.",
                paramName);
    }
}
