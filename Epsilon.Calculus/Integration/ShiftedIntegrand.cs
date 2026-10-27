using Epsilon.Core;

namespace Epsilon.Calculus;

// The integrand next to an end as a function of the distance from it, and the integral of |f|
// over the distances too small to tell from the end (infinite when it diverges there).
internal readonly record struct IntegrandEnd(Func<double, double> At, double Unresolved);

// Where the integrand is singular at an end, the expression is shifted there symbolically:
// (1 - x)^(-1/2) next to 1 becomes d^(-1/2), exact down to the smallest double, where
// 1 - (1 - d) is 0 for every d below 1e-16.
internal sealed class ShiftedIntegrand(Expr expr, string variable)
{
    private const double MachineEpsilon = 2.220446049250313E-16;

    private readonly Func<double, double> _plain = expr.Compile(variable);

    public IntegrandEnd Near(IntegrationPoint end, int direction, double halfWidth)
    {
        double at = end.Value, unresolved = 0;
        if (double.IsFinite(_plain(at)))
        {
            unresolved = Unresolved(at, direction, halfWidth, out bool fast);
            if (!fast && unresolved == 0)
                return new IntegrandEnd(d => _plain(at + direction * d), 0);
        }

        IntegrandEnd shifted = Shifted(end, direction);

        // Next to a singularity at a point that isn't a double, the shift removes the rounding
        // only where the point is exact, as sqrt(2) and pi are, and the integrand is singular at
        // 0. A limit such as Math.PI is kept to 15 digits, which are short of pi.
        return unresolved > 0 && !double.IsInfinity(shifted.At(0))
            ? new IntegrandEnd(d => _plain(at + direction * d), unresolved)
            : shifted;
    }

    private IntegrandEnd Shifted(IntegrationPoint end, int direction)
    {
        Expr distance = new Variable(variable);
        Expr shifted = ReduceAngles(expr.Substitute(variable, direction > 0 ? end.Exact + distance : end.Exact - distance)
            .Simplify(SimplifyMode.Strict));
        Func<double, double> far = shifted.Compile(variable);

        Expr expanded = ExpandVanishingSums(shifted);
        if (ReferenceEquals(expanded, shifted))
            return new IntegrandEnd(far, 0);

        // Expanded, 1 - (1 - d)^2 is 2d - d^2 next to the end, but away from it an expanded
        // power such as (1 - d)^50 loses more to cancellation than it saves.
        Func<double, double> near = expanded.Compile(variable);
        double reach = 1e-6 * Math.Max(1, Math.Abs(end.Value));
        return new IntegrandEnd(d => d < reach ? near(d) : far(d), 0);
    }

    // sin(k pi/2 + r) and the others in sin r and cos r: next to pi, sin(pi + d) is -sin(d),
    // where pi + d would be rounded first.
    private Expr ReduceAngles(Expr e)
    {
        var children = e.Children;
        Expr[]? changed = null;
        for (int i = 0; i < children.Length; i++)
        {
            Expr child = ReduceAngles(children[i]);
            if (!ReferenceEquals(child, children[i]))
                (changed ??= [.. children])[i] = child;
        }
        if (changed is not null)
            e = e.WithChildren(changed);

        if (e is not (Sin or Cos or Tan or Cot or Sec or Csc) || !e.DependsOn(variable))
            return e;

        var (turn, rest) = Split(((UnaryExpr)e).Argument.Expand(SimplifyMode.Strict));
        turn = turn.Simplify(SimplifyMode.Strict);
        if (IntegrationTable.PiMultiple(turn) is not { IsZero: false } multiple || !(2 * multiple).IsInteger)
            return e;

        Expr sin = (new Sin(rest) * new Cos(turn) + new Cos(rest) * new Sin(turn)).Simplify(SimplifyMode.Strict);
        Expr cos = (new Cos(rest) * new Cos(turn) - new Sin(rest) * new Sin(turn)).Simplify(SimplifyMode.Strict);
        return e switch
        {
            Sin => sin,
            Cos => cos,
            Tan => sin / cos,
            Cot => cos / sin,
            Sec => 1 / cos,
            _ => 1 / sin,
        };
    }

    // The terms of a sum that don't depend on the variable, and the others.
    private (Expr Constant, Expr Others) Split(Expr e) => e switch
    {
        _ when !e.DependsOn(variable) => (e, 0),
        Add sum when (Split(sum.Left), Split(sum.Right)) is var ((c1, r1), (c2, r2)) => (c1 + c2, r1 + r2),
        Subtract difference when (Split(difference.Left), Split(difference.Right)) is var ((c1, r1), (c2, r2)) => (c1 - c2, r1 - r2),
        Negate negate when Split(negate.Argument) is var (c, r) => (-c, -r),
        _ => (0, e),
    };

    // Next to a nonzero end x can't come closer than its rounding, so a singularity at a point
    // that isn't a double (tan at the double nearest pi/2) looks finite there. Where |f| grows
    // like d^-alpha towards the end, and is larger at the end than that gives a few roundings
    // from it, the rounded x misses the integral up to there less the value it stalls at,
    // alpha / (1 - alpha) of that value times the width; an alpha near 1 or more means the
    // integral diverges. Where |f| levels off before, but halves within the farthest of those
    // distances all the same, it is shifted to the end: the peak of 1/(1 + 1e10 (x - 1000)^2) is
    // 1e-5 wide where x is rounded to 2e-13, which would cost its integral 3e-10, and its
    // curvature looked like a singularity.
    private double Unresolved(double at, int direction, double halfWidth, out bool fast)
    {
        fast = false;
        double rounding = Math.Abs(at) * MachineEpsilon;
        double near = Math.ScaleB(rounding, 16), middle = Math.ScaleB(rounding, 24), far = Math.ScaleB(rounding, 32);
        if (rounding == 0 || far > halfWidth)
            return 0;

        double atEnd = Math.Abs(_plain(at));
        double atNear = Math.Abs(_plain(at + direction * near));
        double atMiddle = Math.Abs(_plain(at + direction * middle));
        double atFar = Math.Abs(_plain(at + direction * far));
        if (atNear > 0 && atMiddle > 0 && atFar > 0 && double.IsFinite(atNear))
        {
            double alpha = Math.Min(Math.Log(atNear / atMiddle), Math.Log(atMiddle / atFar)) / Math.Log(256);
            double reach = 16 * rounding;
            double atReach = atNear * Math.Pow(near / reach, alpha);
            if (alpha > 0 && atEnd > atReach)
                return alpha >= 0.99 ? double.PositiveInfinity : atReach * reach * alpha / (1 - alpha);
        }

        fast = atEnd > 2 * atFar;
        return 0;
    }

    private Expr ExpandVanishingSums(Expr e)
    {
        if (e is Add or Subtract && VanishesAtEnd(e))
            return e.Expand(SimplifyMode.Strict);

        var children = e.Children;
        Expr[]? changed = null;
        for (int i = 0; i < children.Length; i++)
        {
            Expr child = ExpandVanishingSums(children[i]);
            if (!ReferenceEquals(child, children[i]))
                (changed ??= [.. children])[i] = child;
        }

        return changed is null ? e : e.WithChildren(changed);
    }

    private bool VanishesAtEnd(Expr sum)
    {
        var end = new Dictionary<string, double> { [variable] = 0 };
        double scale = sum.Children.Sum(term => Math.Abs(term.Evaluate(end)));
        return double.IsFinite(scale) && scale > 0 && Math.Abs(sum.Evaluate(end)) <= 1e-12 * scale;
    }
}
