using Epsilon.Core;

namespace Epsilon.Calculus;

/// <summary>Gradients (vectors of first partial derivatives) of multivariable expressions.</summary>
public static class GradientExtensions
{
    /// <summary>The symbolic partial derivatives with respect to each of <paramref name="variables"/>, keyed by variable. Not simplified.</summary>
    public static Dictionary<string, Expr> Gradient(this Expr expr, params string[] variables)
    {
        var result = new Dictionary<string, Expr>(variables.Length);

        foreach (var variable in variables)
            result[variable] = expr.Differentiate(variable);

        return result;
    }

    /// <summary>The symbolic partial derivatives with respect to every variable of the expression. Not simplified.</summary>
    public static Dictionary<string, Expr> Gradient(this Expr expr) =>
        expr.Gradient(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray());

    /// <summary>The gradient with respect to <paramref name="variables"/>, evaluated at <paramref name="point"/>.</summary>
    /// <param name="expr">The function.</param>
    /// <param name="variables">The variables to differentiate by.</param>
    /// <param name="point">A value for every variable of the expression.</param>
    public static Dictionary<string, double> GradientAt(this Expr expr, string[] variables, params (string Name, double Value)[] point)
    {
        var gradient = expr.Gradient(variables);
        var result = new Dictionary<string, double>(variables.Length);

        foreach (var variable in variables)
            result[variable] = gradient[variable].Evaluate(point);

        return result;
    }

    /// <summary>The gradient with respect to every variable of the expression, evaluated at <paramref name="point"/>.</summary>
    public static Dictionary<string, double> GradientAt(this Expr expr, params (string Name, double Value)[] point) =>
        expr.GradientAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);

    /// <summary>The gradient with respect to <paramref name="variables"/>, evaluated over the complex numbers at <paramref name="point"/>.</summary>
    /// <param name="expr">The function.</param>
    /// <param name="variables">The variables to differentiate by.</param>
    /// <param name="point">A value for every variable of the expression.</param>
    public static Dictionary<string, ComplexNumber> GradientAt(this Expr expr, string[] variables, params (string Name, ComplexNumber Value)[] point)
    {
        var gradient = expr.Gradient(variables);
        var result = new Dictionary<string, ComplexNumber>(variables.Length);

        foreach (var variable in variables)
            result[variable] = gradient[variable].EvaluateComplex(point);

        return result;
    }

    /// <summary>The gradient with respect to every variable, evaluated over the complex numbers at <paramref name="point"/>.</summary>
    public static Dictionary<string, ComplexNumber> GradientAt(this Expr expr, params (string Name, ComplexNumber Value)[] point) =>
        expr.GradientAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);
}