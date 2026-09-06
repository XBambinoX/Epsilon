namespace Epsilon.Core;

public static class HessianExtensions
{
    // Explicit variable order — callers who care about row/column meaning should pass this.
    public static Expr[,] Hessian(this Expr expr, IReadOnlyList<string> variables)
    {
        int n = variables.Count;
        var result = new Expr[n, n];

        for (int i = 0; i < n; i++)
        {
            Expr firstDerivative = expr.Differentiate(variables[i]);

            for (int j = 0; j < n; j++)
                result[i, j] = firstDerivative.Differentiate(variables[j]);
        }

        return result;
    }

    public static Expr[,] Hessian(this Expr expr) =>
        expr.Hessian(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToList());
}