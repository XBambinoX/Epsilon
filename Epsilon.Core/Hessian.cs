namespace Epsilon.Core;

public static class HessianExtensions
{
    public static Matrix<Expr> Hessian(this Expr expr, IReadOnlyList<string> variables)
    {
        int n = variables.Count;
        var values = new Expr[n, n];

        for (int i = 0; i < n; i++)
        {
            Expr firstDerivative = expr.Differentiate(variables[i]);
            for (int j = 0; j < n; j++)
                values[i, j] = firstDerivative.Differentiate(variables[j]);
        }

        return new Matrix<Expr>(variables, values);
    }

    public static Matrix<Expr> Hessian(this Expr expr) =>
        expr.Hessian(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToList());

    public static Matrix<double> HessianAt(this Expr expr, IReadOnlyList<string> variables, IReadOnlyDictionary<string, double> point) =>
        expr.Hessian(variables).EvaluateAt(point);

    public static Matrix<double> HessianAt(this Expr expr, IReadOnlyDictionary<string, double> point) =>
        expr.HessianAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToList(), point);

    public static Expr Laplacian(this Expr expr, IReadOnlyList<string> variables) =>
        expr.Hessian(variables).TraceSymbolic();

    public static Expr Laplacian(this Expr expr) =>
        expr.Laplacian(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToList());
}