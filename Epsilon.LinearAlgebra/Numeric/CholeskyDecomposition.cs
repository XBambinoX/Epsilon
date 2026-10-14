namespace Epsilon.LinearAlgebra;

// A = L L^T for a symmetric positive definite matrix of doubles. The tests are relative to the
// diagonal, so they don't change when the matrix is scaled to D A D, as by a change of units.
internal static class CholeskyDecomposition
{
    private const double MachineEpsilon = 2.220446049250313E-16;

    // The row-major L, or null with the reason in failure.
    public static double[]? TryFactor(Matrix<double> matrix, out string failure)
    {
        int n = matrix.Rows;
        ReadOnlySpan<double> a = matrix.Entries;
        double tolerance = 10 * n * MachineEpsilon;

        for (int k = 0; k < a.Length; k++)
            if (!double.IsFinite(a[k]))
            {
                failure = FormattableString.Invariant($"The matrix has a non-finite entry at ({k / n}, {k % n}): {a[k]}.");
                return null;
            }

        // Rounding may leave a computed covariance matrix a little asymmetric; for a positive
        // definite matrix |a_ij| < sqrt(a_ii a_jj), which sets the scale.
        for (int i = 0; i < n; i++)
            for (int j = 0; j < i; j++)
                if (Math.Abs(a[i * n + j] - a[j * n + i]) > tolerance * Math.Sqrt(Math.Abs(a[i * n + i])) * Math.Sqrt(Math.Abs(a[j * n + j])))
                {
                    failure = $"The matrix is not symmetric: the entries at ({i}, {j}) and ({j}, {i}) differ.";
                    return null;
                }

        var l = new double[n * n];
        for (int j = 0; j < n; j++)
        {
            double pivot = a[j * n + j];
            for (int k = 0; k < j; k++)
                pivot -= l[j * n + k] * l[j * n + k];

            // A pivot at the level of rounding, relative to a_jj, means a singular matrix:
            // positive semidefinite at best.
            if (!(pivot > tolerance * a[j * n + j]))
            {
                failure = "The matrix is not positive definite to working precision.";
                return null;
            }

            double diagonal = Math.Sqrt(pivot);
            l[j * n + j] = diagonal;
            for (int i = j + 1; i < n; i++)
            {
                double sum = a[i * n + j];
                for (int k = 0; k < j; k++)
                    sum -= l[i * n + k] * l[j * n + k];
                l[i * n + j] = sum / diagonal;
            }
        }

        if (SmallestEigenvalueEstimate(l, a, n) <= tolerance)
        {
            failure = "The matrix is not positive definite to working precision.";
            return null;
        }

        failure = "";
        return l;
    }

    // About the smallest eigenvalue of A scaled to a unit diagonal, D^-1 A D^-1 = (D^-1 L)(D^-1 L)^T:
    // 1 / ||(D^-1 L)^-1||^2 (Frobenius) lies between that value / n and it. The pivots alone miss
    // a singular matrix whose dependence spreads over several rows, as in a Hilbert matrix.
    private static double SmallestEigenvalueEstimate(double[] l, ReadOnlySpan<double> a, int n)
    {
        var scaled = new double[n * n];
        for (int i = 0; i < n; i++)
        {
            double rowScale = 1 / Math.Sqrt(a[i * n + i]);
            for (int k = 0; k <= i; k++)
                scaled[i * n + k] = l[i * n + k] * rowScale;
        }

        var inverse = new double[n];
        double sumOfSquares = 0;
        for (int j = 0; j < n; j++)
            for (int i = j; i < n; i++)
            {
                double sum = i == j ? 1 : 0;
                for (int k = j; k < i; k++)
                    sum -= scaled[i * n + k] * inverse[k];
                inverse[i] = sum / scaled[i * n + i];
                sumOfSquares += inverse[i] * inverse[i];
            }

        return double.IsFinite(sumOfSquares) ? 1 / sumOfSquares : 0;
    }
}
