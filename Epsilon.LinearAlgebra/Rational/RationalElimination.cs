using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

// Gaussian elimination over the rationals. The arithmetic is exact, so any pivot that is not 0
// will do and a matrix is singular exactly when it lacks one: no tolerance, unlike LuDecomposition.
internal static class RationalElimination
{
    // Brings the row-major entries of a rows x columns matrix, in place, to row echelon form and
    // applies the same row operations to the right-hand side (bColumns columns), if any. Returns
    // the rank and whether the rows were swapped an odd number of times.
    public static (int Rank, bool OddSwaps) Eliminate(Rational[] a, int rows, int columns, Rational[]? b = null, int bColumns = 0)
    {
        int rank = 0;
        bool oddSwaps = false;

        for (int column = 0; column < columns && rank < rows; column++)
        {
            int pivot = rank;
            while (pivot < rows && a[pivot * columns + column].IsZero)
                pivot++;
            if (pivot == rows)
                continue;

            if (pivot != rank)
            {
                SwapRows(a, columns, pivot, rank);
                if (b is not null)
                    SwapRows(b, bColumns, pivot, rank);
                oddSwaps = !oddSwaps;
            }

            Rational pivotValue = a[rank * columns + column];
            for (int r = rank + 1; r < rows; r++)
            {
                if (a[r * columns + column].IsZero)
                    continue;

                Rational factor = a[r * columns + column] / pivotValue;
                a[r * columns + column] = Rational.Zero;
                for (int c = column + 1; c < columns; c++)
                    a[r * columns + c] -= factor * a[rank * columns + c];

                if (b is not null)
                    for (int c = 0; c < bColumns; c++)
                        b[r * bColumns + c] -= factor * b[rank * bColumns + c];
            }

            rank++;
        }

        return (rank, oddSwaps);
    }

    // The determinant of an n x n matrix: the product of the pivots, with the sign of the swaps.
    public static Rational Determinant(Matrix<Rational> matrix)
    {
        int n = matrix.Rows;
        Rational[] a = matrix.Entries.ToArray();
        var (rank, oddSwaps) = Eliminate(a, n, n);
        if (rank < n)
            return Rational.Zero;

        Rational product = Rational.One;
        for (int i = 0; i < n; i++)
            product *= a[i * n + i];

        return oddSwaps ? -product : product;
    }

    public static int Rank(Matrix<Rational> matrix) =>
        Eliminate(matrix.Entries.ToArray(), matrix.Rows, matrix.Columns).Rank;

    // The row-major entries of X in A X = B for an n x n matrix A and an n x bColumns matrix B.
    public static Rational[] Solve(Matrix<Rational> matrix, ReadOnlySpan<Rational> rightHandSide, int bColumns)
    {
        int n = matrix.Rows;
        Rational[] a = matrix.Entries.ToArray();
        Rational[] x = rightHandSide.ToArray();

        int rank = Eliminate(a, n, n, x, bColumns).Rank;
        if (rank < n)
            throw new InvalidOperationException($"The matrix is singular: its rank is {rank}, not {n}.");

        // Back substitution, from the last row up; the pivots of a regular matrix are on the diagonal.
        for (int i = n - 1; i >= 0; i--)
            for (int c = 0; c < bColumns; c++)
            {
                Rational sum = x[i * bColumns + c];
                for (int j = i + 1; j < n; j++)
                    sum -= a[i * n + j] * x[j * bColumns + c];

                x[i * bColumns + c] = sum / a[i * n + i];
            }

        return x;
    }

    private static void SwapRows(Rational[] entries, int columns, int i, int j)
    {
        for (int c = 0; c < columns; c++)
            (entries[i * columns + c], entries[j * columns + c]) = (entries[j * columns + c], entries[i * columns + c]);
    }
}
