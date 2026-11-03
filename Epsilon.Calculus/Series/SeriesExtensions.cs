using Epsilon.Core;

namespace Epsilon.Calculus;

/// <summary>Series of expressions at a point: Taylor series, and Laurent and Puiseux series where the expression has a pole or a root there.</summary>
public static class SeriesExtensions
{
    extension(Expr expr)
    {
        /// <summary>
        /// The series at <paramref name="point"/> by the expression's only variable. See
        /// <see cref="Series(Expr, string, Expr, int)"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The expression doesn't have exactly one variable.</exception>
        /// <exception cref="NotSupportedException">The expression has no such series at the point.</exception>
        public Series Series(Expr point, int degree)
        {
            ArgumentNullException.ThrowIfNull(expr);
            return expr.Series(expr.GetSingleVariable(), point, degree);
        }

        /// <summary>
        /// The series of the expression at x = <paramref name="point"/> for x =
        /// <paramref name="variable"/>, the terms up to (x - a)^<paramref name="degree"/>, with
        /// exact coefficients: sin(x)/x at 0 to degree 4 is 1 - x^2/6 + x^4/120 + O(x^5). Other
        /// variables are constants, and the point may have them. Where the expression has a pole
        /// at the point, the series starts at a negative power, 1/sin(x) at 0 with 1/x, and where
        /// it has a root of a part that vanishes there, the powers are fractions, sqrt(x + x^2)
        /// with x^(1/2); such a series may hold only for x &gt; a (see <see cref="Series.RightSided"/>).
        /// </summary>
        /// <exception cref="ArgumentException">The point depends on the variable.</exception>
        /// <exception cref="NotSupportedException">
        /// The expression has no such series at the point: the series of ln(x) at 0 has ln(x) in
        /// it, exp(1/x) has an essential singularity at 0, and asin(x) isn't real for x &gt; 1, the
        /// side a series at a branch point is taken on; or a sign there isn't known.
        /// </exception>
        public Series Series(string variable, Expr point, int degree)
        {
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(variable);
            ArgumentNullException.ThrowIfNull(point);
            if (point.DependsOn(variable))
                throw new ArgumentException($"The point {point.Print()} depends on {variable}.", nameof(point));

            Expr at = point.Simplify();
            Expansion expansion = new SeriesExpander(variable, at).Expand(expr.Simplify(), degree + 1);
            var terms = expansion.Terms.Where(term => term.Key <= degree).Select(term => (term.Key, SeriesExpander.Tidy(term.Value))).ToList();
            Rational order = expansion.Terms.Keys.FirstOrDefault(exponent => exponent > degree, degree + 1);
            return new Series(variable, at, terms, order < degree + 1 ? order : degree + 1, expansion.RightSided);
        }
    }
}
