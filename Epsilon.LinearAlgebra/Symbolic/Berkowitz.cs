using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

// Berkowitz's algorithm for the characteristic polynomial of a matrix of expressions, and the
// adjugate from it. Neither divides, so the results hold wherever the entries are defined,
// without assuming that any entry is nonzero. Every intermediate value is expanded, so terms
// that cancel do so as early as possible.
internal static class Berkowitz
{
    // The coefficients c[0..n] of det(t I - A) = c[0] t^n + c[1] t^(n-1) + ... + c[n], with c[0] = 1.
    public static Expr[] Coefficients(Matrix<Expr> a)
    {
        int n = a.Rows;
        if (n == 0)
            return [1];

        // The coefficients for the trailing 1 x 1 submatrix, then for ever larger trailing
        // submatrices A_k = a[k.., k..], partitioned as [[a[k, k], R], [C, A_(k+1)]].
        Expr[] v = [1, (-a[n - 1, n - 1]).Expand()];

        for (int k = n - 2; k >= 0; k--)
        {
            int m = n - k;

            // The first column of a lower triangular Toeplitz matrix:
            // 1, -a[k, k], -R C, -R A_(k+1) C, ..., -R A_(k+1)^(m-2) C.
            var toeplitz = new Expr[m + 1];
            toeplitz[0] = 1;
            toeplitz[1] = (-a[k, k]).Expand();

            var w = new Expr[m - 1];
            for (int r = 0; r < m - 1; r++)
                w[r] = a[k + 1 + r, k];

            for (int i = 0; i < m - 1; i++)
            {
                if (i > 0)
                    w = Multiply(a, k + 1, w);

                toeplitz[i + 2] = (-Sum(m - 1, s => a[k, k + 1 + s] * w[s])).Expand();
            }

            // v = Toeplitz matrix ((m + 1) x m) times the previous v (length m).
            var next = new Expr[m + 1];
            for (int i = 0; i <= m; i++)
                next[i] = Sum(Math.Min(i, m - 1) + 1, j => toeplitz[i - j] * v[j]);

            v = next;
        }

        return v;
    }

    // The adjugate from the coefficients of the characteristic polynomial. By Cayley-Hamilton,
    // adj(A) = (-1)^(n+1) (A^(n-1) + c[1] A^(n-2) + ... + c[n-1] I), evaluated by Horner's scheme.
    public static Matrix<Expr> Adjugate(Matrix<Expr> a, Expr[] coefficients)
    {
        int n = a.Rows;
        var b = Matrix<Expr>.Identity(n);

        for (int k = 1; k < n; k++)
        {
            Matrix<Expr> previous = b;
            Expr c = coefficients[k];
            b = Matrix<Expr>.Create(n, n, (i, j) =>
            {
                Expr product = Sum(n, s => a[i, s] * previous[s, j]);
                return i == j ? (product + c).Expand() : product;
            });
        }

        return n % 2 == 1 ? b : b.Map(entry => (-entry).Expand());
    }

    // The expanded sum of term(0), ..., term(count - 1); 0 when there are no terms.
    public static Expr Sum(int count, Func<int, Expr> term)
    {
        if (count == 0)
            return 0;

        Expr sum = term(0);
        for (int i = 1; i < count; i++)
            sum += term(i);

        return sum.Expand();
    }

    // The trailing submatrix a[from.., from..] times the vector w, expanded.
    private static Expr[] Multiply(Matrix<Expr> a, int from, Expr[] w)
    {
        var result = new Expr[w.Length];
        for (int r = 0; r < w.Length; r++)
            result[r] = Sum(w.Length, s => a[from + r, from + s] * w[s]);

        return result;
    }
}
