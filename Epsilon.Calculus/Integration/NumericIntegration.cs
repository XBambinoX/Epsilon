using Epsilon.Core;

namespace Epsilon.Calculus;

/// <summary>Definite integrals computed numerically, over finite and infinite intervals.</summary>
public static class NumericIntegrationExtensions
{
    private const double DefaultTolerance = 1e-10;
    private const int MaxSplits = 30;

    extension(Expr expr)
    {
        /// <summary>
        /// The definite integral of an expression of at most one variable from <paramref name="a"/>
        /// to <paramref name="b"/>, computed numerically. See
        /// <see cref="Integrate(Expr, string, double, double, double)"/>.
        /// </summary>
        /// <exception cref="ArgumentException">A limit is NaN.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The tolerance is not between 0 and 1.</exception>
        /// <exception cref="InvalidOperationException">
        /// The expression has more than one variable, or the integral diverges or can't be
        /// computed, as for the overload with the variable.
        /// </exception>
        public double Integrate(double a, double b, double tolerance = DefaultTolerance)
        {
            ArgumentNullException.ThrowIfNull(expr);
            return expr.Integrate(expr.GetVariables().Count == 0 ? "x" : expr.GetSingleVariable(), a, b, tolerance);
        }

        /// <summary>
        /// The definite integral by <paramref name="variable"/> from <paramref name="a"/> to
        /// <paramref name="b"/>, computed numerically to about <paramref name="tolerance"/> times
        /// the integral of the absolute value. A limit may be infinite; for b &lt; a it is the
        /// negative of the integral from b to a. The integrand may be singular at the limits and
        /// inside as long as the integral converges: <c>ln(x)</c> from 0 to 1 is -1, and
        /// <c>1/sqrt(abs(x))</c> from -1 to 4 is 6.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The interval is split where the expression shows the integrand may be singular or not
        /// smooth: zeros of denominators, of the bases of fractional and negative powers and of the
        /// arguments of roots, logarithms and abs, poles of tan, edges of the domains of the
        /// inverse functions, kinks of min and max, jumps of floor, ceiling and round of linear
        /// arguments, and peaks of exponentials, so that a normal density far from 0 is found.
        /// Each piece is integrated by the tanh-sinh rule, or exp-sinh to an infinity, with the
        /// step halved until two estimates agree.
        /// </para>
        /// <para>
        /// Next to a singularity at a point that isn't a double, such as the zero of x^2 - 2, the
        /// accuracy is limited to about 1e-8. Another narrow feature far from 0 on an infinite
        /// interval, such as the peak of sech(10(x - 1000))^2, can be missed: split the interval there.
        /// Integrals that converge only by cancellation to an infinity, such as of sin(x)/x, are
        /// reported as divergent.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// A limit is NaN, or the expression has a variable besides <paramref name="variable"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">The tolerance is not between 0 and 1.</exception>
        /// <exception cref="InvalidOperationException">
        /// The integral diverges or converges too slowly for double precision, the integrand is not
        /// a real number or is infinite somewhere inside, or the estimates didn't agree.
        /// </exception>
        public double Integrate(string variable, double a, double b, double tolerance = DefaultTolerance)
        {
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(variable);
            if (double.IsNaN(a) || double.IsNaN(b))
                throw new ArgumentException("A limit of integration is NaN.");
            if (!(tolerance > 0 && tolerance < 1))
                throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "The tolerance must be between 0 and 1.");

            foreach (string name in expr.GetVariables())
                if (name != variable)
                    throw new ArgumentException(
                        $"The expression has the variable '{name}' besides '{variable}': substitute a value for it first.", nameof(variable));

            if (a == b)
                return 0;

            return a < b ? IntegrateAscending(expr, variable, a, b, tolerance) : -IntegrateAscending(expr, variable, b, a, tolerance);
        }
    }

    private static double IntegrateAscending(Expr expr, string variable, double a, double b, double tolerance)
    {
        // The ends of the pieces, null for an infinite limit.
        var ends = new List<IntegrationPoint?> { Limit(a) };
        foreach (IntegrationPoint point in Breakpoints.Find(expr, variable, a, b))
            ends.Add(point);
        ends.Add(Limit(b));
        if (ends is [null, null])
            ends.Insert(1, new IntegrationPoint(0, 0));

        var integrand = new ShiftedIntegrand(expr, variable);
        var pieces = new List<(DoubleExponential Rule, IntegrationPoint? P, IntegrationPoint? Q)>();
        for (int i = 0; i + 1 < ends.Count; i++)
            pieces.Add((Piece(integrand, ends[i], ends[i + 1]), ends[i], ends[i + 1]));

        // Each piece gets its share of tolerance * the integral of |f|, and at least an even share,
        // so a piece where the integrand is almost 0 isn't held to digits it doesn't have.
        double scale = pieces.Sum(piece => piece.Rule.AbsoluteIntegral);
        double reserve = scale;
        return pieces.Sum(piece => Converge(integrand, piece.Rule, piece.P, piece.Q, tolerance / 2, scale / pieces.Count, ref reserve, 0));
    }

    private static IntegrationPoint? Limit(double limit) =>
        double.IsFinite(limit) ? new IntegrationPoint(limit, new Constant(limit)) : null;

    private static DoubleExponential Piece(ShiftedIntegrand integrand, IntegrationPoint? p, IntegrationPoint? q)
    {
        if (p is { } from && q is { } to)
        {
            double halfWidth = to.Value / 2 - from.Value / 2;
            return DoubleExponential.Finite(integrand.Near(from, 1, halfWidth), integrand.Near(to, -1, halfWidth), from.Value, to.Value);
        }

        IntegrationPoint end = (p ?? q).GetValueOrDefault();
        int direction = p is null ? -1 : 1;
        return DoubleExponential.SemiInfinite(integrand.Near(end, direction, double.PositiveInfinity), end.Value, direction);
    }

    // A piece that doesn't converge, around a sharp peak say, is split in two, a finite one in
    // the middle and a half-line at its peak, so that the trouble ends up next to an end of a
    // piece, where the nodes crowd. The halves share its floor, and split again and again, as a
    // half-line out to a slowly decaying tail is, the last piece would have too little of it for
    // any error: the tail of sin(x)^2 exp(-x/100) beyond 3600 is 1e-16 of the integral. So a
    // piece that doesn't converge is taken as it is where all of its integral of |f| fits in the
    // reserve, as large as the floors together; its error can't be trusted, so all of it is charged.
    private static double Converge(
        ShiftedIntegrand integrand, DoubleExponential rule, IntegrationPoint? p, IntegrationPoint? q,
        double tolerance, double floor, ref double reserve, int depth)
    {
        if (rule.TryConverge(tolerance, floor, out double estimate))
            return estimate;

        if (rule.AbsoluteIntegral <= tolerance * reserve)
        {
            reserve -= rule.AbsoluteIntegral / tolerance;
            return estimate;
        }

        double split = p is { } from && q is { } to ? from.Value / 2 + to.Value / 2 : rule.Peak;
        double low = p?.Value ?? double.NegativeInfinity, high = q?.Value ?? double.PositiveInfinity;
        if (depth == MaxSplits || !(low < split && split < high) || !double.IsFinite(split))
            throw rule.NotConverged();

        var middle = new IntegrationPoint(split, new Constant(split));
        return Converge(integrand, Piece(integrand, p, middle), p, middle, tolerance, floor / 2, ref reserve, depth + 1)
            + Converge(integrand, Piece(integrand, middle, q), middle, q, tolerance, floor / 2, ref reserve, depth + 1);
    }
}
