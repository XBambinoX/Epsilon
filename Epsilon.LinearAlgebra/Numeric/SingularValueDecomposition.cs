namespace Epsilon.LinearAlgebra;

// The thin singular value decomposition A = U diag(S) V^T of a matrix of doubles by one-sided
// Jacobi (Hestenes): plane rotations of pairs of columns until all columns are orthogonal; their
// norms are then the singular values. Accurate for the small singular values too, as the test of
// orthogonality is relative to the norms of the two columns.
internal static class SingularValueDecomposition
{
    private const double MachineEpsilon = 2.220446049250313E-16;
    private const int MaxSweeps = 100;

    // A sum of squares from here on hasn't lost accuracy to subnormal terms.
    private const double SafeSquare = 1e-290;

    public static (Matrix<double> U, Vector<double> S, Matrix<double> V) Compute(Matrix<double> matrix)
    {
        LuDecomposition.CheckFinite(matrix);

        bool wide = matrix.Rows < matrix.Columns;
        var (u, s, v) = Tall(wide ? matrix.Transpose() : matrix, vectors: true);
        if (wide)
            (u, v) = (v!, u!);

        // The signs of a pair of singular vectors are free; the entry of largest magnitude of each
        // right one is made positive.
        int k = s.Length;
        for (int j = 0; j < k; j++)
        {
            int top = 0;
            for (int i = 1; i < v!.Length / k; i++)
                if (Math.Abs(v[i * k + j]) > Math.Abs(v[top * k + j]))
                    top = i;
            if (v[top * k + j] >= 0)
                continue;
            for (int i = 0; i < v.Length / k; i++)
                v[i * k + j] = -v[i * k + j];
            for (int i = 0; i < u!.Length / k; i++)
                u[i * k + j] = -u[i * k + j];
        }

        return (new Matrix<double>(matrix.Rows, k, u!), new Vector<double>(s), new Matrix<double>(matrix.Columns, k, v!));
    }

    // The singular values, in descending order.
    public static double[] Values(Matrix<double> matrix)
    {
        LuDecomposition.CheckFinite(matrix);
        return Tall(matrix.Rows < matrix.Columns ? matrix.Transpose() : matrix, vectors: false).S;
    }

    // For m >= n: U (m x n) and V (n x n), row-major, and S, or only S.
    private static (double[]? U, double[] S, double[]? V) Tall(Matrix<double> a, bool vectors)
    {
        int m = a.Rows, n = a.Columns;

        // The columns of A as rows, so that the rotations walk them in memory order.
        var w = new double[n * m];
        for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                w[j * m + i] = a[i, j];

        double[]? vt = null;
        if (vectors)
        {
            vt = new double[n * n];
            for (int j = 0; j < n; j++)
                vt[j * n + j] = 1;
        }

        // As in LAPACK's dgesvj: orthogonal when |a_p . a_q| <= sqrt(m) eps ||a_p|| ||a_q||.
        double tolerance = Math.Sqrt(Math.Max(m, 1)) * MachineEpsilon;
        for (int sweep = 0; sweep < MaxSweeps; sweep++)
        {
            bool rotated = false;
            for (int p = 0; p < n; p++)
                for (int q = p + 1; q < n; q++)
                {
                    var columnP = w.AsSpan(p * m, m);
                    var columnQ = w.AsSpan(q * m, m);
                    if (Zeta(columnP, columnQ, tolerance) is not double zeta)
                        continue;

                    double t = (zeta >= 0 ? 1 : -1) / (Math.Abs(zeta) + double.Hypot(zeta, 1));
                    double c = 1 / Math.Sqrt(1 + t * t), s = c * t;
                    Rotate(columnP, columnQ, c, s);
                    if (vt is not null)
                        Rotate(vt.AsSpan(p * n, n), vt.AsSpan(q * n, n), c, s);
                    rotated = true;
                }

            if (!rotated)
                break;
        }

        var norms = new double[n];
        for (int j = 0; j < n; j++)
            norms[j] = NumericVectorExtensions.EuclideanNorm(w.AsSpan(j * m, m));
        int[] order = Enumerable.Range(0, n).OrderByDescending(j => norms[j]).ToArray();
        double[] values = order.Select(j => norms[j]).ToArray();
        if (vt is null)
            return (null, values, null);

        var v = new double[n * n];
        for (int column = 0; column < n; column++)
            for (int i = 0; i < n; i++)
                v[i * n + column] = vt[order[column] * n + i];

        return (LeftVectors(w, norms, order, m, n), values, v);
    }

    // (||a_q||^2 - ||a_p||^2) / (2 a_p . a_q), which fixes the rotation; null if the columns are
    // orthogonal to working precision. Where the squares overflow or lose bits to underflow, the
    // same from the norms and the cosine of the angle, which can't.
    private static double? Zeta(ReadOnlySpan<double> p, ReadOnlySpan<double> q, double tolerance)
    {
        double alpha = 0, beta = 0, gamma = 0;
        for (int i = 0; i < p.Length; i++)
        {
            alpha += p[i] * p[i];
            beta += q[i] * q[i];
            gamma += p[i] * q[i];
        }

        if (alpha >= SafeSquare && beta >= SafeSquare && double.IsFinite(alpha) && double.IsFinite(beta))
            return Math.Abs(gamma) <= tolerance * Math.Sqrt(alpha) * Math.Sqrt(beta) ? null : (beta - alpha) / (2 * gamma);

        double normP = NumericVectorExtensions.EuclideanNorm(p), normQ = NumericVectorExtensions.EuclideanNorm(q);
        if (normP == 0 || normQ == 0)
            return null;

        double cosine = 0;
        for (int i = 0; i < p.Length; i++)
            cosine += p[i] / normP * (q[i] / normQ);
        return Math.Abs(cosine) <= tolerance ? null : (normQ / normP - normP / normQ) / (2 * cosine);
    }

    // (c a_p - s a_q, s a_p + c a_q), which makes the two orthogonal for the c and s above.
    private static void Rotate(Span<double> p, Span<double> q, double c, double s)
    {
        for (int i = 0; i < p.Length; i++)
            (p[i], q[i]) = (c * p[i] - s * q[i], s * p[i] + c * q[i]);
    }

    // The columns of A, normalized, in the order of the singular values. They are orthogonal
    // already, and orthogonalized again against the earlier ones; for a singular value 0 a unit
    // vector of the axes, orthogonalized, takes the place of the zero column. With fewer than m
    // columns so far, the squares of what is left of the m axes sum to at least 1, so one of
    // them keeps more than 1 / sqrt(2m) of its length.
    private static double[] LeftVectors(double[] w, double[] norms, int[] order, int m, int n)
    {
        var columns = new double[n][];
        int nextAxis = 0;
        for (int column = 0; column < n; column++)
        {
            int source = order[column];
            double[]? u = norms[source] > 0 ? Orthonormal(w.AsSpan(source * m, m).ToArray(), columns, column, 0.5) : null;
            while (u is null)
            {
                var axis = new double[m];
                axis[nextAxis++] = 1;
                u = Orthonormal(axis, columns, column, 1 / Math.Sqrt(2 * m));
            }

            columns[column] = u;
        }

        var result = new double[m * n];
        for (int column = 0; column < n; column++)
            for (int i = 0; i < m; i++)
                result[i * n + column] = columns[column][i];

        return result;
    }

    // The vector with its components along the first count columns removed twice (Gram-Schmidt
    // with reorthogonalization), normalized; null if less than the fraction of its length is left.
    private static double[]? Orthonormal(double[] u, double[][] columns, int count, double fraction)
    {
        double before = NumericVectorExtensions.EuclideanNorm(u);
        for (int pass = 0; pass < 2; pass++)
            for (int k = 0; k < count; k++)
            {
                double dot = 0;
                for (int i = 0; i < u.Length; i++)
                    dot += columns[k][i] * u[i];
                for (int i = 0; i < u.Length; i++)
                    u[i] -= dot * columns[k][i];
            }

        double after = NumericVectorExtensions.EuclideanNorm(u);
        if (!(after > fraction * before))
            return null;

        for (int i = 0; i < u.Length; i++)
            u[i] /= after;
        return u;
    }
}
