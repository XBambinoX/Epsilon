namespace Epsilon.LinearAlgebra;

/// <summary>
/// The rotation, the inverse and approximate equality of <see cref="Matrix2x2{T}"/> with
/// floating-point entries: float, double or Half. Angles are in radians.
/// </summary>
public static class FloatingPointMatrix2x2Extensions
{
    extension<T>(Matrix2x2<T> matrix) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        /// <summary>The rotation of the plane about the origin; a positive angle turns <c>UnitX</c> toward <c>UnitY</c>.</summary>
        public static Matrix2x2<T> Rotation(T angle)
        {
            var (sin, cos) = T.SinCos(angle);
            return new(cos, -sin, sin, cos);
        }

        /// <summary>
        /// The inverse, the map that undoes this one; see <see cref="TryInverse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The matrix is singular to working precision or has an entry that is NaN or infinite.
        /// </exception>
        public Matrix2x2<T> Inverse() =>
            matrix.TryInverse(out Matrix2x2<T> inverse) ? inverse
            : throw new InvalidOperationException(T.IsFinite(Largest(matrix))
                ? "The matrix is singular to working precision."
                : "The matrix has an entry that is NaN or infinite.");

        /// <summary>
        /// The inverse [[M22, -M12], [-M21, M11]] / det, if the matrix is invertible to working
        /// precision: its determinant must be larger than the bound on its rounding error. Rows of
        /// huge or tiny entries are scaled by powers of two first, which is exact, so neither
        /// overflow nor underflow spoils the result.
        /// </summary>
        /// <param name="inverse">The inverse, or the zero matrix if there is none.</param>
        /// <returns>Whether the matrix is invertible: false if it is singular to working precision or has an entry that is NaN or infinite.</returns>
        public bool TryInverse(out Matrix2x2<T> inverse)
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
            T d1 = FloatingPoint<T>.RowScale(T.MaxMagnitude(matrix.M11, matrix.M12));
            T d2 = FloatingPoint<T>.RowScale(T.MaxMagnitude(matrix.M21, matrix.M22));
            var scaled = new Matrix2x2<T>(d1 * matrix.M11, d1 * matrix.M12, d2 * matrix.M21, d2 * matrix.M22);
            if (!Invert(scaled, out Matrix2x2<T> b, out _))
            {
                inverse = default;
                return false;
            }

            inverse = new(b.M11 * d1, b.M12 * d2, b.M21 * d1, b.M22 * d2);
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
        public bool IsApproximately(Matrix2x2<T> other, T? relativeTolerance = null, T? absoluteTolerance = null) =>
            FloatingPoint<T>.AreClose(
                [matrix.M11, matrix.M12, matrix.M21, matrix.M22], [other.M11, other.M12, other.M21, other.M22],
                relativeTolerance, absoluteTolerance);
    }

    // The adjugate over the determinant, unless the determinant is 0 to working precision. The
    // permanent of |A|, the sum of the magnitudes of the terms of the determinant, is the scale of
    // its rounding error.
    private static bool Invert<T>(Matrix2x2<T> m, out Matrix2x2<T> inverse, out T permanent)
        where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        T determinant = m.M11 * m.M22 - m.M12 * m.M21;
        permanent = T.Abs(m.M11 * m.M22) + T.Abs(m.M12 * m.M21);

        if (FloatingPoint<T>.IsNegligible(determinant, permanent, FloatingPoint<T>.DeterminantError2))
        {
            inverse = default;
            return false;
        }

        T r = T.One / determinant;
        inverse = new(m.M22 * r, -m.M12 * r, -m.M21 * r, m.M11 * r);
        return true;
    }

    // The largest magnitude of an entry; NaN if an entry is NaN.
    private static T Largest<T>(Matrix2x2<T> m) where T : struct, System.Numerics.IFloatingPointIeee754<T> =>
        T.Abs(T.MaxMagnitude(T.MaxMagnitude(m.M11, m.M12), T.MaxMagnitude(m.M21, m.M22)));
}
