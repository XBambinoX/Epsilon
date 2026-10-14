namespace Epsilon.LinearAlgebra;

// The eigenvalues and eigenvectors of a symmetric matrix of doubles by the cyclic Jacobi method:
// plane rotations zero one entry off the diagonal after another, until all are negligible.
// Slower than tridiagonal QR for large matrices, but simple and accurate.
internal static class SymmetricEigenDecomposition
{
    private const double MachineEpsilon = 2.220446049250313E-16;

    // Each sweep squares the size of the entries off the diagonal once they are small; a handful suffices.
    private const int MaxSweeps = 100;

    public static (Vector<double> Values, Matrix<double> Vectors) Compute(Matrix<double> matrix)
    {
        matrix.CheckSquare("SymmetricEigen");
        LuDecomposition.CheckFinite(matrix);

        int n = matrix.Rows;
        double[] a = matrix.Entries.ToArray();
        double norm = NumericVectorExtensions.EuclideanNorm(a);
        CheckSymmetric(a, n, 10 * n * MachineEpsilon * norm);

        // Scaling by a power of two is exact and keeps the differences of the diagonal finite.
        double largest = 0;
        foreach (double entry in a)
            largest = Math.Max(largest, Math.Abs(entry));
        int exponent = largest == 0 ? 0 : Math.ILogB(largest);
        for (int i = 0; i < n; i++)
            for (int j = 0; j <= i; j++)
                a[i * n + j] = a[j * n + i] = Math.ScaleB(a[i * n + j], -exponent);

        var v = new double[n * n];
        for (int i = 0; i < n; i++)
            v[i * n + i] = 1;

        // Negligible: within rounding of its diagonal entries, or of the whole matrix.
        double floor = Math.ScaleB(norm, -exponent) / Math.Max(n, 1);
        for (int sweep = 0; sweep < MaxSweeps; sweep++)
        {
            bool rotated = false;
            for (int p = 0; p < n; p++)
                for (int q = p + 1; q < n; q++)
                {
                    double scale = Math.Max(Math.Sqrt(Math.Abs(a[p * n + p])) * Math.Sqrt(Math.Abs(a[q * n + q])), floor);
                    if (Math.Abs(a[p * n + q]) <= MachineEpsilon * scale)
                        continue;

                    Rotate(a, v, n, p, q);
                    rotated = true;
                }

            if (!rotated)
                break;
        }

        int[] order = Enumerable.Range(0, n).OrderBy(i => a[i * n + i]).ToArray();
        var values = new double[n];
        var vectors = new double[n * n];
        for (int column = 0; column < n; column++)
        {
            int source = order[column];
            values[column] = Math.ScaleB(a[source * n + source], exponent);

            // The sign of an eigenvector is free; the entry of largest magnitude is made positive.
            int top = 0;
            for (int k = 1; k < n; k++)
                if (Math.Abs(v[k * n + source]) > Math.Abs(v[top * n + source]))
                    top = k;
            double sign = v[top * n + source] < 0 ? -1 : 1;
            for (int k = 0; k < n; k++)
                vectors[k * n + column] = sign * v[k * n + source];
        }

        return (new Vector<double>(values), new Matrix<double>(n, n, vectors));
    }

    // Zeroes a[p, q] by a rotation in the plane of p and q (Numerical Recipes' jacobi), and turns
    // the columns p and q of v with it.
    private static void Rotate(double[] a, double[] v, int n, int p, int q)
    {
        double app = a[p * n + p], aqq = a[q * n + q], apq = a[p * n + q];
        double theta = (aqq - app) / (2 * apq);
        double t = (theta >= 0 ? 1 : -1) / (Math.Abs(theta) + double.Hypot(theta, 1));
        double c = 1 / Math.Sqrt(t * t + 1), s = t * c, tau = s / (1 + c);

        a[p * n + p] = app - t * apq;
        a[q * n + q] = aqq + t * apq;
        a[p * n + q] = a[q * n + p] = 0;
        for (int r = 0; r < n; r++)
        {
            if (r == p || r == q)
                continue;
            double g = a[r * n + p], h = a[r * n + q];
            a[r * n + p] = a[p * n + r] = g - s * (h + g * tau);
            a[r * n + q] = a[q * n + r] = h + s * (g - h * tau);
        }

        for (int k = 0; k < n; k++)
        {
            double g = v[k * n + p], h = v[k * n + q];
            v[k * n + p] = g - s * (h + g * tau);
            v[k * n + q] = h + s * (g - h * tau);
        }
    }

    // Rounding may leave a computed matrix a little asymmetric; an eigenvalue moves by at most the
    // size of a change of the matrix, so that is measured against the whole matrix.
    private static void CheckSymmetric(double[] a, int n, double tolerance)
    {
        for (int i = 0; i < n; i++)
            for (int j = 0; j < i; j++)
                if (Math.Abs(a[i * n + j] - a[j * n + i]) > tolerance)
                    throw new InvalidOperationException($"The matrix is not symmetric: the entries at ({i}, {j}) and ({j}, {i}) differ.");
    }
}
