using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Calculus;

/// <summary>
/// Hessians and Laplacians: the second partial derivatives of expressions, and the
/// second-derivative test at critical points.
/// </summary>
public static class HessianExtensions
{
    private const double MachineEpsilon = 2.220446049250313E-16;

    extension(Expr expr)
    {
        /// <summary>
        /// The Hessian by every variable of the expression, in ordinal order of the names
        /// (uppercase before lowercase); 0 x 0 for an expression without variables.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// The expression contains a function whose derivative is not supported, as for
        /// <see cref="Expr.Differentiate(string)"/>.
        /// </exception>
        public Matrix<Expr> Hessian()
        {
            ArgumentNullException.ThrowIfNull(expr);
            return expr.Hessian(OrderedVariables(expr));
        }

        /// <summary>
        /// The Laplacian by every variable of the expression; 0 for an expression without variables.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// The expression contains a function whose derivative is not supported, as for
        /// <see cref="Expr.Differentiate(string)"/>.
        /// </exception>
        public Expr Laplacian()
        {
            ArgumentNullException.ThrowIfNull(expr);
            return expr.Laplacian(OrderedVariables(expr));
        }

        /// <summary>
        /// The second-derivative test at a critical point, where the gradient is 0 (not checked):
        /// the signs of the eigenvalues of the Hessian there, by the variables of the point. All
        /// positive: a strict local minimum; all negative: a strict local maximum; both signs: a
        /// saddle; otherwise some are 0 to working precision and the test is inconclusive. For
        /// <c>x^2 - y^2</c> at (0, 0) it is a saddle.
        /// </summary>
        /// <exception cref="ArgumentException">The expression has a variable the point doesn't give.</exception>
        /// <exception cref="InvalidOperationException">A second derivative is NaN or infinite at the point.</exception>
        /// <exception cref="NotSupportedException">
        /// The expression contains a function whose derivative is not supported, as for
        /// <see cref="Expr.Differentiate(string)"/>.
        /// </exception>
        public CriticalPointKind ClassifyCriticalPoint(IReadOnlyDictionary<string, double> point)
        {
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(point);

            foreach (string name in expr.GetVariables())
                if (!point.ContainsKey(name))
                    throw new ArgumentException($"The point has no value for variable '{name}'.", nameof(point));

            string[] names = [.. point.Keys.Order(StringComparer.Ordinal)];
            if (names.Length == 0)
                return CriticalPointKind.Inconclusive;

            Matrix<double> hessian = expr.Hessian(names).Evaluate(point);
            for (int i = 0; i < names.Length; i++)
                for (int j = 0; j <= i; j++)
                    if (!double.IsFinite(hessian[i, j]))
                        throw new InvalidOperationException(FormattableString.Invariant(
                            $"The second derivative by {names[i]} and {names[j]} is {hessian[i, j]} at the point."));

            Vector<double> values = hessian.SymmetricEigen().Values;
            double zero = 10 * names.Length * MachineEpsilon * Math.Max(-values[0], values[^1]);

            if (values[0] < -zero && values[^1] > zero)
                return CriticalPointKind.Saddle;
            if (values[0] > zero)
                return CriticalPointKind.LocalMinimum;
            if (values[^1] < -zero)
                return CriticalPointKind.LocalMaximum;

            return CriticalPointKind.Inconclusive;
        }
    }

    /// <summary>
    /// The Hessian: entry (i, j) is the simplified second partial derivative by <c>variables[i]</c>
    /// and <c>variables[j]</c>, given as <c>Hessian("x", "y")</c> or as a list. The matrix is
    /// symmetric: each mixed derivative is taken once. A variable the expression doesn't have
    /// gives a row and a column of zeros. For <c>x^2 * y</c> by <c>x</c> and <c>y</c> it is
    /// <c>[[2y, 2x], [2x, 0]]</c>. Evaluate or Compile the matrix for numbers.
    /// </summary>
    /// <exception cref="ArgumentException">A name is listed twice.</exception>
    /// <exception cref="NotSupportedException">
    /// The expression contains a function whose derivative is not supported, as for
    /// <see cref="Expr.Differentiate(string)"/>.
    /// </exception>
    public static Matrix<Expr> Hessian(this Expr expr, params IReadOnlyList<string> variables)
    {
        ArgumentNullException.ThrowIfNull(expr);
        string[] names = VariableNames.Checked(variables);
        Vector<Expr> gradient = expr.Gradient(names);

        var entries = new Expr[names.Length, names.Length];
        for (int i = 0; i < names.Length; i++)
            for (int j = 0; j <= i; j++)
                entries[i, j] = entries[j, i] = gradient[i].Differentiate(names[j]);

        return new Matrix<Expr>(entries);
    }

    /// <summary>
    /// The Laplacian: the simplified sum of the second partial derivatives by each of
    /// <paramref name="variables"/>, which is the trace of the Hessian and the divergence of the
    /// gradient. For <c>x^2 * y + y^3</c> by <c>x</c> and <c>y</c> it is <c>8y</c>.
    /// </summary>
    /// <exception cref="ArgumentException">A name is listed twice.</exception>
    /// <exception cref="NotSupportedException">
    /// The expression contains a function whose derivative is not supported, as for
    /// <see cref="Expr.Differentiate(string)"/>.
    /// </exception>
    public static Expr Laplacian(this Expr expr, params IReadOnlyList<string> variables)
    {
        ArgumentNullException.ThrowIfNull(expr);
        return expr.Gradient(variables).Divergence(variables);
    }

    /// <summary>
    /// The second-derivative test with the point as pairs: <c>ClassifyCriticalPoint(("x", 0), ("y", 0))</c>.
    /// See <see cref="ClassifyCriticalPoint(Expr, IReadOnlyDictionary{string, double})"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// A variable is given twice, or the expression has a variable the point doesn't give.
    /// </exception>
    /// <exception cref="InvalidOperationException">A second derivative is NaN or infinite at the point.</exception>
    /// <exception cref="NotSupportedException">
    /// The expression contains a function whose derivative is not supported, as for
    /// <see cref="Expr.Differentiate(string)"/>.
    /// </exception>
    public static CriticalPointKind ClassifyCriticalPoint(this Expr expr, params (string Name, double Value)[] point)
    {
        ArgumentNullException.ThrowIfNull(point);

        var bindings = new Dictionary<string, double>(point.Length);
        foreach (var (name, value) in point)
            if (!bindings.TryAdd(name, value))
                throw new ArgumentException($"Variable '{name}' is given twice.", nameof(point));

        return expr.ClassifyCriticalPoint(bindings);
    }

    private static string[] OrderedVariables(Expr expr) => [.. expr.GetVariables().Order(StringComparer.Ordinal)];
}
