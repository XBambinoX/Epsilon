#if false
//TODO: port to the new immutable Matrix<T> of Epsilon.LinearAlgebra
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Calculus;

/// <summary>Hessian matrices (second partial derivatives) and Laplacians of multivariable expressions.</summary>
public static class HessianExtensions
{
    /// <summary>
    /// The symbolic Hessian: entry [i, j] is the second partial derivative by
    /// <paramref name="variables"/>[i] then [j]. Rows and columns are labelled by the variables. Simplified.
    /// </summary>
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

    /// <summary>The symbolic Hessian over every variable of the expression, in ordinal name order. Simplified.</summary>
    public static Matrix<Expr> Hessian(this Expr expr) =>
        expr.Hessian(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray());

    /// <summary>The Hessian with respect to <paramref name="variables"/>, evaluated at <paramref name="point"/>.</summary>
    /// <param name="expr">The function.</param>
    /// <param name="variables">The variables to differentiate by.</param>
    /// <param name="point">A value for every variable of the expression.</param>
    public static Matrix<double> HessianAt(this Expr expr, string[] variables, params (string Name, double Value)[] point) =>
        expr.Hessian(variables).EvaluateAt(ToDictionary(point));

    /// <summary>The Hessian over every variable of the expression, evaluated at <paramref name="point"/>.</summary>
    public static Matrix<double> HessianAt(this Expr expr, params (string Name, double Value)[] point) =>
        expr.HessianAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);

    /// <summary>The Hessian with respect to <paramref name="variables"/>, evaluated over the complex numbers at <paramref name="point"/>.</summary>
    /// <param name="expr">The function.</param>
    /// <param name="variables">The variables to differentiate by.</param>
    /// <param name="point">A value for every variable of the expression.</param>
    public static Matrix<ComplexNumber> HessianAt(this Expr expr, string[] variables, params (string Name, ComplexNumber Value)[] point)
    {
        int n = variables.Length;
        var values = new ComplexNumber[n, n];

        for (int i = 0; i < n; i++)
        {
            Expr firstDerivative = expr.Differentiate(variables[i]);
            for (int j = 0; j < n; j++)
                values[i, j] = firstDerivative.Differentiate(variables[j]).EvaluateComplex(point);
        }

        return new Matrix<ComplexNumber>(variables, values);
    }

    /// <summary>The Hessian over every variable, evaluated over the complex numbers at <paramref name="point"/>.</summary>
    public static Matrix<ComplexNumber> HessianAt(this Expr expr, params (string Name, ComplexNumber Value)[] point) =>
        expr.HessianAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);

    private static Dictionary<string, double> ToDictionary((string Name, double Value)[] bindings)
    {
        var dict = new Dictionary<string, double>(bindings.Length);
        foreach (var (name, value) in bindings)
            dict[name] = value;

        return dict;
    }

    /// <summary>The symbolic Laplacian: the sum of the second partial derivatives by each of <paramref name="variables"/>, simplified.</summary>
    public static Expr Laplacian(this Expr expr, params string[] variables) =>
        expr.Hessian(variables).TraceSymbolic();

    /// <summary>The symbolic Laplacian over every variable of the expression, simplified.</summary>
    public static Expr Laplacian(this Expr expr) =>
        expr.Laplacian(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray());

    /// <summary>The Laplacian with respect to <paramref name="variables"/>, evaluated at <paramref name="point"/>.</summary>
    /// <param name="expr">The function.</param>
    /// <param name="variables">The variables to differentiate by.</param>
    /// <param name="point">A value for every variable of the expression.</param>
    public static double LaplacianAt(this Expr expr, string[] variables, params (string Name, double Value)[] point) =>
        expr.Laplacian(variables).Evaluate(ToDictionary(point));

    /// <summary>The Laplacian over every variable of the expression, evaluated at <paramref name="point"/>.</summary>
    public static double LaplacianAt(this Expr expr, params (string Name, double Value)[] point) =>
        expr.LaplacianAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);

    /// <summary>The Laplacian with respect to <paramref name="variables"/>, evaluated over the complex numbers at <paramref name="point"/>.</summary>
    /// <param name="expr">The function.</param>
    /// <param name="variables">The variables to differentiate by.</param>
    /// <param name="point">A value for every variable of the expression.</param>
    public static ComplexNumber LaplacianAt(this Expr expr, string[] variables, params (string Name, ComplexNumber Value)[] point) =>
        expr.Laplacian(variables).EvaluateComplex(point);

    /// <summary>The Laplacian over every variable, evaluated over the complex numbers at <paramref name="point"/>.</summary>
    public static ComplexNumber LaplacianAt(this Expr expr, params (string Name, ComplexNumber Value)[] point) =>
        expr.LaplacianAt(expr.GetVariables().OrderBy(v => v, StringComparer.Ordinal).ToArray(), point);
}
#endif
