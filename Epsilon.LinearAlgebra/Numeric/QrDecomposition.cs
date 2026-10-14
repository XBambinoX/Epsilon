namespace Epsilon.LinearAlgebra;

// Householder QR of a matrix of doubles, A = Q * R, as in LAPACK's dgeqrf: the reflections are
// stored below the diagonal of R, and Q is formed only when asked for. Works for any shape;
// LeastSquares uses it for tall matrices and, on the transpose, for wide ones.
internal sealed class QrDecomposition
{
    private const double MachineEpsilon = 2.220446049250313E-16;

    private readonly int _rows;
    private readonly int _columns;

    // Row-major. On and above the diagonal: R. Below it: the reflection vectors, whose first entry 1 is implied.
    private readonly double[] _qr;
    private readonly double[] _tau;

    // Whether the columns are linearly dependent to working precision.
    public bool HasDependentColumns { get; }

    public QrDecomposition(Matrix<double> matrix)
    {
        LuDecomposition.CheckFinite(matrix);

        _rows = matrix.Rows;
        _columns = matrix.Columns;
        _qr = matrix.Entries.ToArray();
        _tau = new double[Math.Min(_rows, _columns)];

        var scratch = new double[_rows];
        for (int j = 0; j < _tau.Length; j++)
            Reflect(j, scratch);

        HasDependentColumns = _columns > _rows || SmallestUnitCombination() <= 10 * Math.Max(_rows, _columns) * MachineEpsilon;
    }

    // About the smallest singular value of A with its columns scaled to length 1, which R scaled the
    // same way shares: 1 / ||(R D^-1)^-1|| (Frobenius) lies between that value / sqrt(n) and it.
    // Unlike a test of the diagonal of R alone, it finds a dependence whatever its coefficients.
    // Exactly dependent random columns stay below max(m, n) eps; the factor 10 is the margin.
    private double SmallestUnitCombination()
    {
        int n = _columns;
        var scaled = new double[n * n];
        for (int j = 0; j < n; j++)
        {
            double norm = 0;
            for (int i = 0; i <= j; i++)
                norm = double.Hypot(norm, _qr[i * n + j]);
            if (norm == 0)
                return 0;
            for (int i = 0; i <= j; i++)
                scaled[i * n + j] = _qr[i * n + j] / norm;
        }

        // The inverse of the triangle, column by column, and the sum of the squares of its entries.
        var inverse = new double[n];
        double sumOfSquares = 0;
        for (int j = 0; j < n; j++)
        {
            for (int i = j; i >= 0; i--)
            {
                double sum = i == j ? 1 : 0;
                for (int k = i + 1; k <= j; k++)
                    sum -= scaled[i * n + k] * inverse[k];
                inverse[i] = sum / scaled[i * n + i];
                sumOfSquares += inverse[i] * inverse[i];
            }
        }

        return double.IsFinite(sumOfSquares) ? 1 / Math.Sqrt(sumOfSquares) : 0;
    }

    // The thin factors: Q with min(rows, columns) orthonormal columns and R upper triangular
    // (trapezoidal for a wide matrix), with the signs chosen so that the diagonal of R is >= 0.
    public (Matrix<double> Q, Matrix<double> R) Factors()
    {
        int k = _tau.Length;

        var r = new double[k * _columns];
        for (int i = 0; i < k; i++)
            for (int j = i; j < _columns; j++)
                r[i * _columns + j] = _qr[i * _columns + j];

        var q = new double[_rows * k];
        for (int j = 0; j < k; j++)
            q[j * k + j] = 1;
        for (int j = k - 1; j >= 0; j--)
            for (int c = j; c < k; c++)
                ApplyReflection(j, q, k, c);

        for (int i = 0; i < k; i++)
        {
            if (r[i * _columns + i] >= 0)
                continue;
            for (int j = i; j < _columns; j++)
                r[i * _columns + j] = -r[i * _columns + j];
            for (int row = 0; row < _rows; row++)
                q[row * k + i] = -q[row * k + i];
        }

        return (new Matrix<double>(_rows, k, q), new Matrix<double>(k, _columns, r));
    }

    // The x minimizing ||A x - b|| column by column, for a tall matrix with independent columns:
    // R x = Q^T b. b is row-major with _rows rows.
    public double[] LeastSquares(ReadOnlySpan<double> b, int bColumns)
    {
        var y = b.ToArray();
        for (int j = 0; j < _tau.Length; j++)
            for (int c = 0; c < bColumns; c++)
                ApplyReflection(j, y, bColumns, c);

        int n = _columns;
        var x = new double[n * bColumns];
        for (int c = 0; c < bColumns; c++)
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = y[i * bColumns + c];
                for (int j = i + 1; j < n; j++)
                    sum -= _qr[i * _columns + j] * x[j * bColumns + c];
                x[i * bColumns + c] = sum / _qr[i * _columns + i];
            }

        return x;
    }

    // For the decomposition of A^T, where A is wide with independent rows: the solution of
    // A x = b with the smallest norm, x = Q z with R^T z = b. b is row-major with _columns rows.
    public double[] MinimumNormSolution(ReadOnlySpan<double> b, int bColumns)
    {
        int m = _columns;
        var x = new double[_rows * bColumns];
        for (int c = 0; c < bColumns; c++)
            for (int i = 0; i < m; i++)
            {
                double sum = b[i * bColumns + c];
                for (int j = 0; j < i; j++)
                    sum -= _qr[j * _columns + i] * x[j * bColumns + c];
                x[i * bColumns + c] = sum / _qr[i * _columns + i];
            }

        for (int j = _tau.Length - 1; j >= 0; j--)
            for (int c = 0; c < bColumns; c++)
                ApplyReflection(j, x, bColumns, c);

        return x;
    }

    // The reflection H = I - tau v v^T that zeroes column j below the diagonal (LAPACK's dlarfg),
    // applied to the columns on its right. No reflection where the column is already zero there.
    private void Reflect(int j, double[] scratch)
    {
        int below = _rows - j - 1;
        for (int i = 0; i < below; i++)
            scratch[i] = _qr[(j + 1 + i) * _columns + j];
        double belowNorm = NumericVectorExtensions.EuclideanNorm(scratch.AsSpan(0, below));
        if (belowNorm == 0)
            return;

        double alpha = _qr[j * _columns + j];
        double beta = -double.CopySign(double.Hypot(alpha, belowNorm), alpha);
        _tau[j] = (beta - alpha) / beta;
        double scale = 1 / (alpha - beta);
        for (int i = j + 1; i < _rows; i++)
            _qr[i * _columns + j] *= scale;
        _qr[j * _columns + j] = beta;

        for (int c = j + 1; c < _columns; c++)
            ApplyReflection(j, _qr, _columns, c);
    }

    // Applies reflection j to column c of a row-major array with the given number of columns and _rows rows.
    private void ApplyReflection(int j, double[] target, int targetColumns, int c)
    {
        double tau = _tau[j];
        if (tau == 0)
            return;

        double w = target[j * targetColumns + c];
        for (int i = j + 1; i < _rows; i++)
            w += _qr[i * _columns + j] * target[i * targetColumns + c];

        w *= tau;
        target[j * targetColumns + c] -= w;
        for (int i = j + 1; i < _rows; i++)
            target[i * targetColumns + c] -= w * _qr[i * _columns + j];
    }
}
