using Epsilon.Core;

namespace Epsilon.Calculus;

/// <summary>Limits of expressions of one variable at a point, from either side, and at inf and -inf.</summary>
public static class LimitExtensions
{
    extension(Expr expr)
    {
        /// <summary>
        /// The limit at <paramref name="point"/> by the expression's only variable. See
        /// <see cref="Limit(Expr, string, Expr, LimitDirection)"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The expression has more than one variable, or the limit doesn't exist, as for the
        /// overload with the variable.
        /// </exception>
        /// <exception cref="NotSupportedException">The limit isn't found.</exception>
        public Limit Limit(Expr point, LimitDirection direction = LimitDirection.Both)
        {
            ArgumentNullException.ThrowIfNull(expr);
            return expr.Limit(expr.GetVariables().Count == 0 ? "x" : expr.GetSingleVariable(), point, direction);
        }

        /// <summary>
        /// The limit at <paramref name="point"/>, inf or -inf among them, by the expression's
        /// only variable. See <see cref="Limit(Expr, string, double, LimitDirection)"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The point is NaN, or the side is beyond an infinite point.</exception>
        /// <exception cref="InvalidOperationException">
        /// The expression has more than one variable, or the limit doesn't exist, as for the
        /// overload with the variable.
        /// </exception>
        /// <exception cref="NotSupportedException">The limit isn't found.</exception>
        public Limit Limit(double point, LimitDirection direction = LimitDirection.Both)
        {
            ArgumentNullException.ThrowIfNull(expr);
            return expr.Limit(expr.GetVariables().Count == 0 ? "x" : expr.GetSingleVariable(), point, direction);
        }

        /// <summary>
        /// The limit of the expression as <paramref name="variable"/> tends to
        /// <paramref name="point"/>, exact: sin(x)/x at 0 is 1, x ln(x) at 0 is 0, and 1/x^2 at 0
        /// is inf. Other variables are constants, and the point may have them. From both sides,
        /// the limit exists where the two sides agree, 1/x at 0 has none, and at an end of where
        /// the expression is real it is the limit from the side it is real on: sqrt(x) at 0 is 0.
        /// </summary>
        /// <exception cref="ArgumentException">The point depends on the variable.</exception>
        /// <exception cref="InvalidOperationException">
        /// The limits from the two sides differ, or the expression isn't real on the side the
        /// limit is taken from.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The limit isn't found: a part of the expression oscillates there, as sin(1/x) at 0
        /// does, the limit depends on the sign of another variable, or the expression has a part
        /// the limits aren't found for.
        /// </exception>
        public Limit Limit(string variable, Expr point, LimitDirection direction = LimitDirection.Both)
        {
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(variable);
            ArgumentNullException.ThrowIfNull(point);
            if (point.DependsOn(variable))
                throw new ArgumentException($"The point {point.Print()} depends on {variable}.", nameof(point));

            return Find(expr.Simplify(), variable, point.Simplify(), direction switch
            {
                LimitDirection.Left => -1,
                LimitDirection.Right => 1,
                _ => 0,
            });
        }

        /// <summary>
        /// The limit as <paramref name="variable"/> tends to <paramref name="point"/>, which may be
        /// <see cref="double.PositiveInfinity"/> or <see cref="double.NegativeInfinity"/>:
        /// (1 + 1/x)^x at inf is e, and x exp(x) at -inf is 0. A finite point is the decimal it
        /// prints as; see <see cref="Limit(Expr, string, Expr, LimitDirection)"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The point is NaN, or the side is beyond an infinite point: <see cref="LimitDirection.Right"/>
        /// at inf or <see cref="LimitDirection.Left"/> at -inf.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The limits from the two sides of a finite point differ, or the expression isn't real
        /// on the side the limit is taken from.
        /// </exception>
        /// <exception cref="NotSupportedException">The limit isn't found.</exception>
        public Limit Limit(string variable, double point, LimitDirection direction = LimitDirection.Both)
        {
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(variable);
            if (double.IsNaN(point))
                throw new ArgumentException("The point is NaN.", nameof(point));
            if (double.IsFinite(point))
                return expr.Limit(variable, new Constant(point), direction);

            int sign = point > 0 ? 1 : -1;
            if (direction == (sign > 0 ? LimitDirection.Right : LimitDirection.Left))
                throw new ArgumentException($"{variable} can't tend to {Infinity(sign)} from the {(sign > 0 ? "right" : "left")}.", nameof(direction));

            return OneSided(expr.Simplify(), variable, null, sign);
        }
    }

    private static string Infinity(int sign) => sign > 0 ? "inf" : "-inf";

    private static Limit Find(Expr f, string variable, Expr point, int side)
    {
        if (side != 0)
            return OneSided(f, variable, point, side);

        // A side the expression isn't real on is left out, unless both are.
        Limit? right = null, left;
        try
        {
            right = Side(f, variable, point, 1, bothSides: true);
        }
        catch (NotRealThere)
        {
        }

        try
        {
            left = Side(f, variable, point, -1, bothSides: true);
        }
        catch (NotRealThere e)
        {
            return right ?? throw new InvalidOperationException(e.Message, e.InnerException);
        }

        if (right is null)
            return left;
        if (Same(left, right))
            return right;

        string where = $"of {f.Print()} at {variable} = {point.Print()}";
        if (left.Kind == right.Kind && (left.Value.GetVariables().Count > 0 || right.Value.GetVariables().Count > 0))
            throw new NotSupportedException($"The limits {where} from the left, {left.Print()}, and from the right, {right.Print()}, aren't known to be the same.");

        throw new InvalidOperationException($"The limit {where} doesn't exist: {left.Print()} from the left, {right.Print()} from the right.");
    }

    private static bool Same(Limit a, Limit b)
    {
        if (a.Kind != b.Kind)
            return false;
        if (a.Kind != LimitKind.Finite || (a.Value - b.Value).Simplify() is Constant { Value.IsZero: true })
            return true;
        if (a.Value.GetVariables().Count > 0 || b.Value.GetVariables().Count > 0)
            return false;

        double x = a.Value.Evaluate(), y = b.Value.Evaluate();
        return Math.Abs(x - y) <= 1e-12 * Math.Max(1, Math.Abs(x));
    }

    private static Limit OneSided(Expr f, string variable, Expr? point, int side)
    {
        try
        {
            return Side(f, variable, point, side, bothSides: false);
        }
        catch (NotRealThere e)
        {
            throw new InvalidOperationException(e.Message, e.InnerException);
        }
    }

    // The limit from one side, of f(a + side/x) or of f(side x) for an infinite point, as x -> inf.
    private static Limit Side(Expr f, string variable, Expr? point, int side, bool bothSides)
    {
        var x = new Variable(variable);
        Expr at = point is null ? side > 0 ? x : -x : side > 0 ? point + 1 / x : point - 1 / x;
        string where = point is null
            ? $"as {variable} -> {Infinity(side)}"
            : $"at {variable} = {point.Print()}{(bothSides ? "" : side > 0 ? " from the right" : " from the left")}";
        try
        {
            return new Gruntz(variable).AtInfinity(f.Substitute(variable, at).Simplify());
        }
        catch (Gruntz.UnknownSign e)
        {
            throw new NotSupportedException($"The limit of {f.Print()} {where} depends on the sign of {e.Constant.Print()}.", e);
        }
        catch (NotSupportedException e) when (NotReal(f, variable, point, side))
        {
            throw new NotRealThere($"The limit of {f.Print()} {where} doesn't exist: {f.Print()} isn't real there.", e);
        }
        catch (Gruntz.Oscillation e)
        {
            throw new NotSupportedException($"The limit of {f.Print()} {where} isn't found: a part of it oscillates there.", e);
        }
        catch (NotSupportedException e)
        {
            throw new NotSupportedException($"The limit of {f.Print()} {where} isn't found.", e);
        }
    }

    // Whether f is NaN all the way along the side, at x = a + side 2^-k (|a| + 1) or side 2^k.
    private static bool NotReal(Expr f, string variable, Expr? point, int side)
    {
        if (f.GetVariables().Any(name => name != variable) || point is not null && point.GetVariables().Count > 0)
            return false;

        try
        {
            double a = point?.Evaluate() ?? 0;
            int[] steps = point is null ? [3, 5, 7] : [16, 24, 32, 40];
            return steps.All(k => double.IsNaN(f.Evaluate((variable, point is null ? side * Math.Pow(2, k) : a + side * Math.Pow(2, -k) * (Math.Abs(a) + 1)))));
        }
        catch (Exception e) when (e is ArithmeticException or InvalidOperationException or NotSupportedException)
        {
            return false;
        }
    }

    private sealed class NotRealThere(string message, Exception inner) : InvalidOperationException(message, inner);
}
