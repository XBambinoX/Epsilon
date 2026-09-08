namespace Epsilon.Core;

public static class HessianExtensions
{
    public static Matrix<Expr> Hessian(this Expr expr, params string[] variables)
    {
        int n = variables.Length;
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
        expr.Hessian(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray());

    public static Matrix<double> HessianAt(this Expr expr, string[] variables, params (string Name, double Value)[] point) =>
        expr.Hessian(variables).EvaluateAt(ToDictionary(point));

    public static Matrix<double> HessianAt(this Expr expr, params (string Name, double Value)[] point) =>
        expr.HessianAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);

    public static Matrix<Complex> HessianAt(this Expr expr, string[] variables, params (string Name, Complex Value)[] point)
    {
        int n = variables.Length;
        var values = new Complex[n, n];

        for (int i = 0; i < n; i++)
        {
            Expr firstDerivative = expr.Differentiate(variables[i]);
            for (int j = 0; j < n; j++)
                values[i, j] = firstDerivative.Differentiate(variables[j]).EvaluateComplex(point);
        }

        return new Matrix<Complex>(variables, values);
    }

    public static Matrix<Complex> HessianAt(this Expr expr, params (string Name, Complex Value)[] point) =>
        expr.HessianAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);

    private static Dictionary<string, double> ToDictionary((string Name, double Value)[] bindings)
    {
        var dict = new Dictionary<string, double>(bindings.Length);
        foreach (var (name, value) in bindings)
            dict[name] = value;

        return dict;
    }

    public static Expr Laplacian(this Expr expr, params string[] variables) =>
        expr.Hessian(variables).TraceSymbolic();

    public static Expr Laplacian(this Expr expr) =>
        expr.Laplacian(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray());

    public static double LaplacianAt(this Expr expr, string[] variables, params (string Name, double Value)[] point) =>
        expr.Laplacian(variables).Evaluate(ToDictionary(point));

    public static double LaplacianAt(this Expr expr, params (string Name, double Value)[] point) =>
        expr.LaplacianAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);

    public static Complex LaplacianAt(this Expr expr, string[] variables, params (string Name, Complex Value)[] point) =>
        expr.Laplacian(variables).EvaluateComplex(point);

    public static Complex LaplacianAt(this Expr expr, params (string Name, Complex Value)[] point) =>
        expr.LaplacianAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);
}