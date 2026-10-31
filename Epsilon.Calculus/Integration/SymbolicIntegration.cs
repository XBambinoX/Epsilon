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
        /// <exception cref="NotSupportedException">
        /// No antiderivative was found, or the imaginary unit is inside a function whose real and
        /// imaginary parts aren't written.
        /// </exception>
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
        /// one; the elementary functions, and floor, ceiling and round, of a linear argument, the
        /// last three with continuous antiderivatives; squares and reciprocals of the
        /// trigonometric and hyperbolic functions, sec u tan u and its kin, products of sin and cos;
        /// linear over linear; and 1/q, 1/q^n, (a x + b)/q, sqrt(q), 1/sqrt(q) and (a x + b)/sqrt(q)
        /// for a quadratic q with numeric coefficients. A rational function with rational
        /// coefficients gets its rational part by Hermite's reduction, one fraction over the repeated
        /// factors of the denominator, and the rest by partial fractions over its linear and
        /// quadratic factors with rational coefficients (1/(x^3 + 1), x/(x^4 - 1)), with the
        /// logarithms of coefficients of the same size merged into one. What is left is integrated
        /// by substitution: by u = g for a part g whose derivative divides the rest (2x cos(x^2),
        /// ln(x)/x), with the powers of x written in u for g = p x^k + q; by x in terms of u for a
        /// root of a linear expression (x sqrt(x + 1)); and powers of sin and cos, sinh and cosh by
        /// their squares. A rational function of sin and cos is then one of t for t = cos x, sin x or
        /// tan x by Bioche's rules, with sin(k x) and cos(k x) by the multiple-angle formulas, and of
        /// those that apply the shortest result is taken; where none does, t = tan(x/2). An
        /// arctangent atan(p tan u + q) in the result, which jumps where tan u does, is written as
        /// u + atan(...) of sin 2u and cos 2u, continuous. A rational function of x and sqrt(q) for a
        /// quadratic q = p((x - h)^2 + k) is one of sin and cos, or sinh and cosh, by x - h = r sin(a)
        /// for p, k &lt; 0, r sinh(a) for p, k &gt; 0 and r cosh(a) on each side of the gap for
        /// p &gt; 0 &gt; k, with r = sqrt(|k|); where the sides differ, by Euler's t = x + sqrt(q/p)
        /// on both at once. After it, a factor of the denominator of
        /// degree 3 or more that divides some x^n - a, as x^3 - 2 and x^4 + 1, is split over the
        /// roots of that binomial, and any other of degree 3 or 4 over its roots in radicals, by
        /// Cardano's and Ferrari's formulas. Then the integrand is expanded once and tried again, so
        /// x (x + 1) is integrated as x^2 + x. A rational function of exp, sinh and cosh is one of
        /// t = exp(x). Last, it is integrated by parts, with u a logarithm or
        /// an inverse function of an algebraic argument, or a polynomial (x exp(x), x ln(x),
        /// ln(x)^2), solved for where it comes back to itself (exp(x) sin(x)), and sec^n and csc^n by
        /// their reduction formulas. Where that fails too, products and powers of sin and cos are
        /// written as sums of them by the product-to-sum formulas (exp(x) cos(x)^2). With the
        /// imaginary unit inside a function, the real and imaginary parts of the integrand, for a
        /// real variable, are integrated apart: 1/(x + i) is x/(x^2 + 1) - i/(x^2 + 1).
        /// </remarks>
        /// <exception cref="NotSupportedException">
        /// No antiderivative was found, or the imaginary unit is inside a function whose real and
        /// imaginary parts aren't written, as in sqrt(x + i).
        /// </exception>
        public Expr Integrate(string variable)
        {
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(variable);

            Expr simplified = expr.Simplify();
            if (ImaginaryInside(simplified, variable))
                return ComplexParts.Of(simplified) is var (re, im)
                    && IntegrationTable.Antiderivative(re.Simplify(), variable) is { } u
                    && IntegrationTable.Antiderivative(im.Simplify(), variable) is { } v
                    ? (u + new ImaginaryUnit() * v).Simplify()
                    : throw new NotSupportedException($"No antiderivative of {expr.Print()} by {variable} was found: the imaginary unit is integrated only where the integrand splits into real and imaginary parts.");

            return IntegrationTable.Antiderivative(simplified, variable)
                ?? throw new NotSupportedException($"No antiderivative of {expr.Print()} by {variable} was found.");
        }
    }

    // Outside sums and constant factors, numerators and denominators, a part with the variable has no i.
    private static bool ImaginaryInside(Expr e, string variable) => e.DependsOn(variable) && e switch
    {
        Add or Subtract or Negate => e.Children.Any(child => ImaginaryInside(child, variable)),
        Multiply product when !product.Left.DependsOn(variable) || !product.Right.DependsOn(variable) =>
            e.Children.Any(child => ImaginaryInside(child, variable)),
        Divide quotient when !quotient.Denominator.DependsOn(variable) => ImaginaryInside(quotient.Numerator, variable),
        Divide quotient when !quotient.Numerator.DependsOn(variable) => ComplexParts.HasImaginaryUnit(quotient.Denominator),
        _ => ComplexParts.HasImaginaryUnit(e),
    };
}
