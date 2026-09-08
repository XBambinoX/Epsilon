namespace Epsilon.Core;

public static class SymbolicMatrixExtensions
{
    /// <summary>
    /// Evaluates every cell of a symbolic matrix at the given point, producing a numeric matrix.
    /// </summary>
    public static Matrix<double> EvaluateAt(this Matrix<Expr> matrix, IReadOnlyDictionary<string, double> point)
    {
        int n = matrix.Size;
        var values = new double[n, n];

        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                values[i, j] = matrix[i, j].Evaluate(point);

        return new Matrix<double>(matrix.Variables, values);
    }

    /// <summary>The symbolic trace (sum of diagonal expressions) — e.g. for building a Laplacian.</summary>
    public static Expr TraceSymbolic(this Matrix<Expr> matrix)
    {
        Expr result = new Constant(0);
        for (int i = 0; i < matrix.Size; i++)
            result = new Add(result, matrix[i, i]);
        return result.Simplify();
    }
}

public static class NumericMatrixExtensions
{
    public static double Trace(this Matrix<double> matrix)
    {
        double sum = 0;
        for (int i = 0; i < matrix.Size; i++)
            sum += matrix[i, i];
        return sum;
    }
}