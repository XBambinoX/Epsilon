using Epsilon.Core;

namespace Epsilon.Calculus;

/// <summary>Antiderivatives of expressions, from the rules of a table of integrals, by substitution and by parts.</summary>
public static class SymbolicIntegrationExtensions
{
    extension(Expr expr)
    {
        /// <summary>
        /// An antiderivative by the expression's only variable. See <see cref="Integrate(Expr, string)"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The expression doesn't have exactly one variable.</exception>
        /// <exception cref="NotSupportedException">No antiderivative was found.</exception>
        public Expr Integrate()
        {
            ArgumentNullException.ThrowIfNull(expr);
            return expr.Integrate(expr.GetSingleVariable());
        }

        /// <summary>
        /// An antiderivative by <paramref name="variable"/>, simplified, without the constant of
        /// integration: its derivative is the expression wherever that is defined. Other variables
        /// are constants. Exponents stay exact, 1/x gives <c>ln(abs(x))</c>, and the integral of
        /// <c>nthroot(x, 3)</c> is <c>3/4 x nthroot(x, 3)</c>, real for x &lt; 0 as well.
        /// </summary>
        /// <remarks>
        /// The rules: sums and constant factors; powers of a linear expression, and real roots of
        /// one; the elementary functions of a linear argument; squares and reciprocals of the
        /// trigonometric and hyperbolic functions, sec u tan u and its kin, products of sin and cos;
        /// linear over linear; and 1/q, (a x + b)/q, sqrt(q), 1/sqrt(q) and (a x + b)/sqrt(q) for a
        /// quadratic q with numeric coefficients. What no rule fits is integrated by substitution:
        /// by u = g for a part g whose derivative divides the rest (2x cos(x^2), ln(x)/x), with the
        /// powers of x written in u for g = p x^k + q; by x in terms of u for a root of a linear
        /// expression (x sqrt(x + 1)); and powers of sin and cos, sinh and cosh by their squares.
        /// Then it is expanded once and tried again, so x (x + 1) is integrated as x^2 + x. Last, it
        /// is integrated by parts, with u a logarithm, an inverse function or a polynomial
        /// (x exp(x), x ln(x), ln(x)^2), solved for where it comes back to itself (exp(x) sin(x)),
        /// and sec^n and csc^n by their reduction formulas.
        /// </remarks>
        /// <exception cref="NotSupportedException">No antiderivative was found.</exception>
        public Expr Integrate(string variable)
        {
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(variable);

            return IntegrationTable.Antiderivative(expr, variable)
                ?? throw new NotSupportedException($"No antiderivative of {expr.Print()} by {variable} was found.");
        }
    }
}
