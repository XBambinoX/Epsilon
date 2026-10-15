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
        double at = end.Value;
        if (double.IsFinite(_plain(at)))
            return new IntegrandEnd(d => _plain(at + direction * d), Unresolved(at, direction, halfWidth));

        Expr distance = new Variable(variable);
        Expr shifted = expr.Substitute(variable, direction > 0 ? end.Exact + distance : end.Exact - distance)
            .Simplify(SimplifyMode.Strict);
        Func<double, double> far = shifted.Compile(variable);

        Expr expanded = ExpandVanishingSums(shifted);
        if (ReferenceEquals(expanded, shifted))
            return new IntegrandEnd(far, 0);

        // Expanded, 1 - (1 - d)^2 is 2d - d^2 next to the end, but away from it an expanded
        // power such as (1 - d)^50 loses more to cancellation than it saves.
        Func<double, double> near = expanded.Compile(variable);
        double reach = 1e-6 * Math.Max(1, Math.Abs(at));
        return new IntegrandEnd(d => d < reach ? near(d) : far(d), 0);
    }

    // Next to a nonzero end x can't come closer than its rounding, so a singularity at a point
    // that isn't a double (tan at the double nearest pi/2) looks finite there. Where |f| grows
    // like d^-alpha towards the end, the rounded x misses the integral up to a few roundings less
    // the value it stalls at, alpha / (1 - alpha) of that value times the width; an alpha near 1
    // or more means the integral diverges.
    private double Unresolved(double at, int direction, double halfWidth)
    {
        double rounding = Math.Abs(at) * MachineEpsilon;
        double near = Math.ScaleB(rounding, 16), middle = Math.ScaleB(rounding, 24), far = Math.ScaleB(rounding, 32);
        if (rounding == 0 || far > halfWidth)
            return 0;

        double atNear = Math.Abs(_plain(at + direction * near));
        double atMiddle = Math.Abs(_plain(at + direction * middle));
        double atFar = Math.Abs(_plain(at + direction * far));
        if (!(atNear > 0 && atMiddle > 0 && atFar > 0 && double.IsFinite(atNear)))
            return 0;

        double alpha = Math.Min(Math.Log(atNear / atMiddle), Math.Log(atMiddle / atFar)) / Math.Log(256);
        if (alpha <= 0)
            return 0;
        if (alpha >= 0.99)
            return double.PositiveInfinity;

        double reach = 16 * rounding;
        return atNear * reach * Math.Pow(reach / near, -alpha) * alpha / (1 - alpha);
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
