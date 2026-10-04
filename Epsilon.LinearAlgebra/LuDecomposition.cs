namespace Epsilon.LinearAlgebra;

// Gaussian elimination with partial pivoting of a matrix of doubles: P * A = L * U. Works for any
// shape and brings the matrix to row echelon form, which gives the rank; Determinant, Solve and
// Inverse use it for square matrices of full rank.
//
// An entry counts as zero when it is not larger than the rounding error of the terms it was
// computed from: next to every entry, the elimination keeps the sum of the magnitudes of those
// terms. So [[1, 2, 3], [4, 5, 6], [7, 8, 9]], whose last pivot comes out as about 1e-16
// instead of 0, is singular, while diag(1e-20, 1), where nothing cancels, is not.
internal sealed class LuDecomposition
{
    // The spacing of doubles at 1, 2^-52.
    private const double MachineEpsilon = 2.220446049250313E-16;

    private readonly int _rows;
    private readonly int _columns;

    // Row-major. On and above the diagonal: U. Below it: the multipliers of L, whose diagonal is 1.
    private readonly double[] _lu;

    // _rowOrder[i] is the row of the original matrix that ended up as row i.
    private readonly int[] _rowOrder;

    private readonly bool _oddSwaps;

    // The number of pivots: the rank to working precision.
    public int Rank { get; }

    public LuDecomposition(Matrix<double> matrix)
    {
        CheckFinite(matrix);

        _rows = matrix.Rows;
        _columns = matrix.Columns;
        _lu = matrix.Entries.ToArray();
        _rowOrder = Enumerable.Range(0, _rows).ToArray();

        var magnitude = new double[_lu.Length];
        for (int k = 0; k < _lu.Length; k++)
            magnitude[k] = Math.Abs(_lu[k]);

        double tolerance = Math.Max(_rows, _columns) * MachineEpsilon;
        int r = 0;

        for (int c = 0; c < _columns && r < _rows; c++)
        {
            // The largest entry of the column that is not rounding noise; none means the column
            // is zero below row r, and it gets no pivot.
            int pivot = -1;
            double largest = 0;
            for (int i = r; i < _rows; i++)
            {
                double a = Math.Abs(_lu[i * _columns + c]);
                if (a > tolerance * magnitude[i * _columns + c] && a > largest)
                {
                    largest = a;
                    pivot = i;
                }
            }

            if (pivot < 0)
                continue;

            if (pivot != r)
            {
                SwapRows(_lu, r, pivot);
                SwapRows(magnitude, r, pivot);
                (_rowOrder[r], _rowOrder[pivot]) = (_rowOrder[pivot], _rowOrder[r]);
                _oddSwaps = !_oddSwaps;
            }

            double p = _lu[r * _columns + c];
            for (int i = r + 1; i < _rows; i++)
            {
                double l = _lu[i * _columns + c] / p;
                _lu[i * _columns + c] = l;

                for (int j = c + 1; j < _columns; j++)
                {
                    _lu[i * _columns + j] -= l * _lu[r * _columns + j];
                    magnitude[i * _columns + j] += Math.Abs(l) * magnitude[r * _columns + j];
                }
            }

            r++;
        }

        Rank = r;
    }

    public bool IsSingular => Rank < _columns;

    // The determinant of a square matrix; 0 if it is singular to working precision.
    public double Determinant()
    {
        if (IsSingular)
            return 0;

        double product = _oddSwaps ? -1 : 1;
        for (int i = 0; i < _rows; i++)
            product *= _lu[i * _columns + i];

        return product;
    }

    // The row-major solution X of A * X = B for a square A of full rank, where B has the given
    // number of columns and _rows rows.
    public double[] Solve(ReadOnlySpan<double> b, int bColumns)
    {
        if (IsSingular)
            throw new InvalidOperationException(
                $"The matrix is singular: its rank is {Rank}, not {_columns}, to working precision.");

        int n = _rows;
        var x = new double[n * bColumns];

        for (int q = 0; q < bColumns; q++)
        {
            // L * y = P * b, then U * x = y; y is stored in x.
            for (int i = 0; i < n; i++)
            {
                double sum = b[_rowOrder[i] * bColumns + q];
                for (int j = 0; j < i; j++)
                    sum -= _lu[i * n + j] * x[j * bColumns + q];
                x[i * bColumns + q] = sum;
            }

            for (int i = n - 1; i >= 0; i--)
            {
                double sum = x[i * bColumns + q];
                for (int j = i + 1; j < n; j++)
                    sum -= _lu[i * n + j] * x[j * bColumns + q];
                x[i * bColumns + q] = sum / _lu[i * n + i];
            }
        }

        return x;
    }

    private void SwapRows(double[] entries, int a, int b)
    {
        var rowA = entries.AsSpan(a * _columns, _columns);
        var rowB = entries.AsSpan(b * _columns, _columns);
        for (int j = 0; j < _columns; j++)
            (rowA[j], rowB[j]) = (rowB[j], rowA[j]);
    }

    private static void CheckFinite(Matrix<double> matrix)
    {
        ReadOnlySpan<double> entries = matrix.Entries;
        for (int k = 0; k < entries.Length; k++)
            if (!double.IsFinite(entries[k]))
                throw new InvalidOperationException(FormattableString.Invariant(
                    $"The matrix has a non-finite entry at ({k / matrix.Columns}, {k % matrix.Columns}): {entries[k]}."));
    }
}
