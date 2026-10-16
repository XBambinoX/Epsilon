using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Calculus;

/// <summary>Gradients: the vectors of first partial derivatives of expressions.</summary>
public static class GradientExtensions
{
    extension(Expr expr)
    {
        /// <summary>
        /// The gradient by every variable of the expression, in ordinal order of the names
        /// (uppercase before lowercase); empty for an expression without variables.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// The expression contains a function whose derivative is not supported, as for
        /// <see cref="Expr.Differentiate(string)"/>.
        /// </exception>
        public Vector<Expr> Gradient()
        {
            ArgumentNullException.ThrowIfNull(expr);
            return expr.Gradient([.. expr.GetVariables().Order(StringComparer.Ordinal)]);
        }
    }

    // A classic extension method rather than a member of the extension block: with params there,
    // the C# 14 compiler of SDK 10.0.1xx reports a false nullability warning (CS8620) at every call.
    /// <summary>
    /// The gradient: the simplified partial derivatives by <paramref name="variables"/>, in
    /// that order, given as <c>Gradient("x", "y")</c> or as a list. A variable the expression
    /// doesn't have gives 0. For <c>x^2 * y</c> by <c>x</c> and <c>y</c> it is <c>[2x * y, x^2]</c>.
    /// Evaluate or Compile the vector for numbers.
    /// </summary>
    /// <exception cref="ArgumentException">A name is listed twice.</exception>
    /// <exception cref="NotSupportedException">
    /// The expression contains a function whose derivative is not supported, as for
    /// <see cref="Expr.Differentiate(string)"/>.
    /// </exception>
    public static Vector<Expr> Gradient(this Expr expr, params IReadOnlyList<string> variables)
    {
        ArgumentNullException.ThrowIfNull(expr);
        return Vector.Create(expr).Jacobian(variables).Row(0);
    }

    /// <summary>
    /// The directional derivative: the gradient by <paramref name="variables"/> dotted with
    /// <paramref name="direction"/>, whose entry i goes with <c>variables[i]</c>; simplified.
    /// The direction is not normalized: along a unit vector it is the rate of change, along a
    /// longer one that rate times its length. For <c>x^2 * y</c> along <c>[1, 1]</c> by <c>x</c>
    /// and <c>y</c> it is <c>2x * y + x^2</c>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The direction and the variables have different lengths, or a name is listed twice.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The expression contains a function whose derivative is not supported, as for
    /// <see cref="Expr.Differentiate(string)"/>.
    /// </exception>
    public static Expr DirectionalDerivative(this Expr expr, Vector<Expr> direction, params IReadOnlyList<string> variables)
    {
        ArgumentNullException.ThrowIfNull(expr);
        ArgumentNullException.ThrowIfNull(direction);
        string[] names = VariableNames.Checked(variables);

        if (direction.Length != names.Length)
            throw new ArgumentException(
                $"The direction has {direction.Length} entries but there are {names.Length} variables.", nameof(direction));

        return expr.Gradient(names).Dot(direction);
    }
}
