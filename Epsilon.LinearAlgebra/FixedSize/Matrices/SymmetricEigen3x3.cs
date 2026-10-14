namespace Epsilon.LinearAlgebra;

// The cyclic Jacobi method of SymmetricEigenDecomposition for a symmetric 3 x 3 matrix, in locals.
internal static class SymmetricEigen3x3<T> where T : struct, System.Numerics.IFloatingPointIeee754<T>
{
    private const int MaxSweeps = 50;

    public static (Vector3<T> Values, Matrix3x3<T> Vectors) Compute(Matrix3x3<T> matrix)
    {
        var (m11, m12, m13, m21, m22, m23, m31, m32, m33) =
            (matrix.M11, matrix.M12, matrix.M13, matrix.M21, matrix.M22, matrix.M23, matrix.M31, matrix.M32, matrix.M33);
        T largest = T.Max(T.Max(T.Max(T.Abs(m11), T.Abs(m12)), T.Max(T.Abs(m13), T.Abs(m21))),
            T.Max(T.Max(T.Abs(m22), T.Abs(m23)), T.Max(T.Max(T.Abs(m31), T.Abs(m32)), T.Abs(m33))));
        if (!T.IsFinite(largest))
            throw new InvalidOperationException("The matrix has an entry that is NaN or infinite.");

        // Scaling by a power of two is exact and keeps the differences of the diagonal finite.
        int exponent = FloatingPoint<T>.ScaleExponent(largest);
        if (exponent != 0)
        {
            T factor = T.ScaleB(T.One, -exponent);
            (m11, m12, m13, m21, m22, m23, m31, m32, m33) =
                (m11 * factor, m12 * factor, m13 * factor, m21 * factor, m22 * factor, m23 * factor, m31 * factor, m32 * factor, m33 * factor);
        }

        T norm = T.Sqrt(m11 * m11 + m12 * m12 + m13 * m13 + m21 * m21 + m22 * m22 + m23 * m23 + m31 * m31 + m32 * m32 + m33 * m33);
        T tolerance = T.CreateTruncating(30) * FloatingPoint<T>.MachineEpsilon * norm;
        if (T.Abs(m21 - m12) > tolerance)
            throw NotSymmetric(1, 0);
        if (T.Abs(m31 - m13) > tolerance)
            throw NotSymmetric(2, 0);
        if (T.Abs(m32 - m23) > tolerance)
            throw NotSymmetric(2, 1);

        T d0 = m11, d1 = m22, d2 = m33, a01 = m21, a02 = m31, a12 = m32;
        Vector3<T> v0 = Vector3<T>.UnitX, v1 = Vector3<T>.UnitY, v2 = Vector3<T>.UnitZ;
        T floor = norm / T.CreateTruncating(3);

        for (int sweep = 0; sweep < MaxSweeps; sweep++)
        {
            bool rotated = Rotate(ref d0, ref d1, ref a01, ref a02, ref a12, ref v0, ref v1, floor);
            rotated |= Rotate(ref d0, ref d2, ref a02, ref a01, ref a12, ref v0, ref v2, floor);
            rotated |= Rotate(ref d1, ref d2, ref a12, ref a01, ref a02, ref v1, ref v2, floor);
            if (!rotated)
                break;
        }

        if (d1 < d0)
            (d0, d1, v0, v1) = (d1, d0, v1, v0);
        if (d2 < d1)
            (d1, d2, v1, v2) = (d2, d1, v2, v1);
        if (d1 < d0)
            (d0, d1, v0, v1) = (d1, d0, v1, v0);

        // As in the general case the largest entry of a vector is positive, for the two of the
        // largest values, such as the main axis of a cloud of points; the first one then follows
        // from them, so that the vectors form a rotation.
        v1 = WithPositiveLargest(v1);
        v2 = WithPositiveLargest(v2);
        v0 = v1.Cross(v2);

        if (exponent != 0)
            (d0, d1, d2) = (T.ScaleB(d0, exponent), T.ScaleB(d1, exponent), T.ScaleB(d2, exponent));
        return (new Vector3<T>(d0, d1, d2), Matrix3x3<T>.FromColumns(v0, v1, v2));
    }

    private static InvalidOperationException NotSymmetric(int i, int j) =>
        new($"The matrix is not symmetric: the entries at ({i}, {j}) and ({j}, {i}) differ.");

    // Zeroes apq by a rotation in the plane of p and q, as in SymmetricEigenDecomposition; arp
    // and arq are the entries of the third row r in the columns p and q.
    private static bool Rotate(ref T app, ref T aqq, ref T apq, ref T arp, ref T arq, ref Vector3<T> vp, ref Vector3<T> vq, T floor)
    {
        T scale = T.Max(T.Sqrt(T.Abs(app)) * T.Sqrt(T.Abs(aqq)), floor);
        if (T.Abs(apq) <= FloatingPoint<T>.MachineEpsilon * scale)
            return false;

        T theta = (aqq - app) / (FloatingPoint<T>.Two * apq);
        T t = (theta >= T.Zero ? T.One : -T.One) / (T.Abs(theta) + T.Hypot(theta, T.One));
        T c = T.One / T.Sqrt(t * t + T.One), s = t * c, tau = s / (T.One + c);

        app -= t * apq;
        aqq += t * apq;
        apq = T.Zero;
        (arp, arq) = (arp - s * (arq + arp * tau), arq + s * (arp - arq * tau));
        (vp, vq) = (vp - (vq + vp * tau) * s, vq + (vp - vq * tau) * s);
        return true;
    }

    private static Vector3<T> WithPositiveLargest(Vector3<T> v)
    {
        T largest = v.X;
        if (T.Abs(v.Y) > T.Abs(largest))
            largest = v.Y;
        if (T.Abs(v.Z) > T.Abs(largest))
            largest = v.Z;
        return largest < T.Zero ? -v : v;
    }
}
