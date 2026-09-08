namespace Epsilon.Core;

public static class GradientExtensions
{
    public static Dictionary<string, Expr> Gradient(this Expr expr, params string[] variables)
    {
        var result = new Dictionary<string, Expr>(variables.Length);

        foreach (var variable in variables)
            result[variable] = expr.Differentiate(variable);

        return result;
    }

    public static Dictionary<string, Expr> Gradient(this Expr expr) =>
        expr.Gradient(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray());

    public static Dictionary<string, double> GradientAt(this Expr expr, string[] variables, params (string Name, double Value)[] point)
    {
        var gradient = expr.Gradient(variables);
        var result = new Dictionary<string, double>(variables.Length);

        foreach (var variable in variables)
            result[variable] = gradient[variable].Evaluate(point);

        return result;
    }

    public static Dictionary<string, double> GradientAt(this Expr expr, params (string Name, double Value)[] point) =>
        expr.GradientAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);

    public static Dictionary<string, Complex> GradientAt(this Expr expr, string[] variables, params (string Name, Complex Value)[] point)
    {
        var gradient = expr.Gradient(variables);
        var result = new Dictionary<string, Complex>(variables.Length);

        foreach (var variable in variables)
            result[variable] = gradient[variable].EvaluateComplex(point);

        return result;
    }

    public static Dictionary<string, Complex> GradientAt(this Expr expr, params (string Name, Complex Value)[] point) =>
        expr.GradientAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);
}