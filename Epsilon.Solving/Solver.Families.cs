using Epsilon.Core;

namespace Epsilon.Solving;

// Families of solutions, start + period * k for every integer k, which inverting a periodic
// function gives: sin(x) = 1/2 at π / 6 + 2 * π * k and 5 * π / 6 + 2 * π * k.
internal sealed partial class Solver
{
    private static readonly string[] IndexNames = ["k", "n", "m", "j"];

    // The integer that numbers the values of a family, a name the equation doesn't use.
    private string _index = outer?._index ?? IndexNames[0];

    private static string FreeIndex(string variable, Expr left, Expr right)
    {
        bool Free(string name) => name != variable && !left.DependsOn(name) && !right.DependsOn(name);
        return IndexNames.FirstOrDefault(Free) ?? Enumerable.Range(1, int.MaxValue).Select(i => IndexNames[0] + i).First(Free);
    }

    private bool OnIndex(Condition condition) => condition.Left.DependsOn(_index) || condition.Right.DependsOn(_index);

    // A solution with the index in it as a parameter, as the family it stands for. Null where the
    // value isn't linear in the index with a number for the slope, as sqrt(π * k); empty where no
    // value of the index meets the conditions.
    private List<Solution>? Family(Expr value, IReadOnlyList<Condition> conditions)
    {
        Expr period = value.Differentiate(_index).Simplify(assumptions);
        if (period.GetVariables().Count > 0 || SignOf(period) is not int sign || sign == 0)
            return null;

        Expr start = value.Substitute(_index, 0).Simplify(assumptions);
        if (sign < 0)
            (period, conditions) = ((-period).Simplify(assumptions), Reindexed(conditions, -new Variable(_index)));
        return Normalized(start, period, conditions) is { } family ? [family] : [];
    }

    // The family start + period * k with its start moved into (-period / 2, period / 2] where
    // it is a number, and its conditions on k turned into bounds; null if no k meets them, a single
    // solution if one does.
    private Solution? Normalized(Expr start, Expr period, IReadOnlyList<Condition> conditions)
    {
        var k = new Variable(_index);
        start = start.Simplify(assumptions);
        period = period.Simplify(assumptions);
        if (start.GetVariables().Count == 0 && Shift(Ratio(start, period)) is { IsZero: false } shift)
        {
            start = (start - shift * period).Simplify(assumptions);
            conditions = Reindexed(conditions, k + shift);
        }

        var rest = new List<Condition>();
        Rational? lower = null, upper = null;
        var excluded = new List<Rational>();
        foreach (Condition condition in conditions)
        {
            Condition simplified = new(condition.Left.Simplify(assumptions), condition.Comparison, condition.Right.Simplify(assumptions));
            switch (Decide(simplified))
            {
                case false:
                    return null;
                case true:
                    continue;
            }

            Condition tidied = Tidied(simplified);
            if (tidied is { Left: Variable { Name: var name }, Right: Constant { Value: { IsInteger: true } n } } && name == _index)
            {
                if (tidied.Comparison is Comparison.GreaterOrEqual or Comparison.Equal && !(lower >= n))
                    lower = n;
                if (tidied.Comparison is Comparison.LessOrEqual or Comparison.Equal && !(upper <= n))
                    upper = n;
                if (tidied.Comparison is Comparison.NotEqual)
                    excluded.Add(n);
                if (tidied.Comparison is not Comparison.Less and not Comparison.Greater)
                    continue;
            }

            if (!rest.Contains(tidied))
                rest.Add(tidied);
        }

        if (lower > upper)
            return null;
        excluded = [.. excluded.Where(n => !(n < lower) && !(n > upper)).Distinct().Order()];
        if (lower is { } only && only == upper)
            return excluded.Count > 0 ? null : Single(start, period, only, rest);

        if (lower is { } low)
            rest.Add(new Condition(k, Comparison.GreaterOrEqual, low));
        if (upper is { } high)
            rest.Add(new Condition(k, Comparison.LessOrEqual, high));
        rest.AddRange(excluded.Select(n => new Condition(k, Comparison.NotEqual, n)));
        return new Solution(start, period, _index, rest, assumptions);
    }

    // The one value of a family its conditions leave.
    private Solution? Single(Expr start, Expr period, Rational index, IReadOnlyList<Condition> conditions)
    {
        var open = new List<Condition>();
        foreach (Condition condition in Reindexed(conditions, index))
        {
            switch (Decide(condition))
            {
                case false:
                    return null;
                case null:
                    open.Add(Tidied(condition));
                    break;
            }
        }

        return new Solution((start + index * period).Simplify(assumptions), open);
    }

    // The family at one index. Deciding conditions doesn't need the unknown, so the index stands in.
    internal static Solution? At(Solution family, Rational index, Assumptions assumptions) =>
        new Solver(family.Index!, assumptions, complex: false) { _index = family.Index! }
            .Single(family.Start, family.Period!, index, family.Conditions);

    // The conditions with the index replaced: k + 1 for k where the start moved back a period.
    private List<Condition> Reindexed(IReadOnlyList<Condition> conditions, Expr replacement) =>
        [.. conditions.Select(condition => OnIndex(condition)
            ? new Condition(
                condition.Left.Substitute(_index, replacement).Simplify(assumptions),
                condition.Comparison,
                condition.Right.Substitute(_index, replacement).Simplify(assumptions))
            : condition)];

    // The whole number of periods that moves start / period = q into (-1/2, 1/2]; null if q isn't
    // a number.
    private static Rational? Shift(Expr quotient)
    {
        if (quotient is Constant { Value: var q })
            return (q - new Rational(1, 2)).Ceiling();
        if (quotient.GetVariables().Count > 0)
            return null;

        // An irrational quotient is never half way, so rounding can't decide wrongly.
        double value = quotient.Evaluate();
        return double.IsFinite(value) ? (Rational)Math.Ceiling(value - 0.5) : null;
    }

    // A condition linear in the index with numbers for the coefficients, as a bound on it, the
    // index being an integer: 2 * π * k + π / 6 > 0 is k >= 0, and π * k != 1 always holds.
    // Neither where the condition isn't such.
    private (bool? Holds, Condition? Bound) IndexBound(Condition condition)
    {
        Expr difference = (condition.Left - condition.Right).Simplify(assumptions);
        if (!difference.DependsOn(_index))
            return (null, null);
        Expr slope = difference.Differentiate(_index).Simplify(assumptions);
        Expr at = difference.Substitute(_index, 0).Simplify(assumptions);
        if (slope.GetVariables().Count > 0 || at.GetVariables().Count > 0 || SignOf(slope) is not int sign || sign == 0)
            return (null, null);

        // k compared with the root of the difference, rounded to the integers on either side.
        Expr root = Ratio(-at, slope);
        Rational floor, ceiling;
        if (root is Constant { Value: var exact })
            (floor, ceiling) = (exact.Floor(), exact.Ceiling());
        else
        {
            // An irrational root is no integer, but one this close can't be told from one.
            double value = root.Evaluate();
            if (!double.IsFinite(value) || Math.Abs(value - Math.Round(value)) < 1e-9)
                return (null, null);
            (floor, ceiling) = ((Rational)Math.Floor(value), (Rational)Math.Ceiling(value));
        }

        bool integer = floor == ceiling;
        var k = new Variable(_index);
        return (sign < 0 ? Flipped(condition.Comparison) : condition.Comparison) switch
        {
            Comparison.Equal => integer ? (null, new Condition(k, Comparison.Equal, floor)) : (false, null),
            Comparison.NotEqual => integer ? (null, new Condition(k, Comparison.NotEqual, floor)) : (true, null),
            Comparison.Greater => (null, new Condition(k, Comparison.GreaterOrEqual, floor + 1)),
            Comparison.GreaterOrEqual => (null, new Condition(k, Comparison.GreaterOrEqual, ceiling)),
            Comparison.Less => (null, new Condition(k, Comparison.LessOrEqual, ceiling - 1)),
            _ => (null, new Condition(k, Comparison.LessOrEqual, floor)),
        };
    }

    // A condition the values of a family meet or not by where they fall in the periods of the
    // trigonometric functions in it, as cos(x) != 1 for π * k: the family as one for each value of
    // k modulo the length that does, 2 * π * k + π here. Null where the unknown is anywhere but in
    // trigonometric functions of a multiple of it, or a period isn't a whole number of steps of the
    // family in a few periods.
    private List<Solution>? Residues(Condition condition, Solution family, List<Condition> open)
    {
        Expr period = family.Period!;
        if (family.Start.GetVariables().Count > 0 || period.GetVariables().Count > 0)
            return null;

        System.Numerics.BigInteger length = 1;
        bool Periodic(Expr expr)
        {
            switch (expr)
            {
                case Variable unknown when unknown.Name == variable:
                    return false;
                case Sin or Cos or Tan or Cot or Sec or Csc when expr.Children[0].DependsOn(variable):
                    Expr slope = expr.Children[0].Differentiate(variable).Simplify(assumptions);
                    Expr cycle = expr is Tan or Cot ? Expr.PI : 2 * Expr.PI;
                    if (slope.GetVariables().Count > 0 || (slope * period / cycle).Simplify(assumptions) is not Constant { Value: var steps })
                        return false;
                    length = length / System.Numerics.BigInteger.GreatestCommonDivisor(length, steps.Denominator) * steps.Denominator;
                    return length <= 64;
                default:
                    return expr.Children.All(Periodic);
            }
        }

        if (!Periodic(condition.Left) || !Periodic(condition.Right))
            return null;

        var holding = new List<int>();
        for (int residue = 0; residue < length; residue++)
        {
            Expr value = (family.Start + residue * period).Simplify(assumptions);
            var at = new Condition(
                condition.Left.Substitute(variable, value).Simplify(assumptions),
                condition.Comparison,
                condition.Right.Substitute(variable, value).Simplify(assumptions));
            switch (Decide(at))
            {
                case null:
                    return null;
                case true:
                    holding.Add(residue);
                    break;
            }
        }

        if (holding.Count == length)
            return Normalized(family.Start, period, open) is { } whole ? [whole] : [];

        var k = new Variable(_index);
        var parts = new List<Solution>();
        foreach (int residue in holding)
        {
            if (Normalized(family.Start + residue * period, (int)length * period, Reindexed(open, (int)length * k + residue)) is { } part)
                parts.Add(part);
        }

        return parts;
    }

    // A condition polynomial in the unknown on a family: between the real roots of the polynomial,
    // and at each, it holds or fails throughout, so on runs of k. Those it holds on as families with
    // bounds, or the few values in a short run: sqrt(1 - x^2) is defined at π * k only for k = 0.
    // Null where the condition isn't such, or its roots aren't all found.
    private List<Solution>? Partitioned(Condition condition, Solution family, List<Condition> open)
    {
        Expr start = family.Start, period = family.Period!;
        Expr difference = (condition.Left - condition.Right).Simplify(assumptions);
        if (start.GetVariables().Count > 0 || period.GetVariables().Count > 0
            || Polynomials.Coefficients(difference, variable, MaxDegree) is not { } coefficients
            || coefficients.Any(coefficient => coefficient.GetVariables().Count > 0))
            return null;

        SolutionSet roots = new Solver(variable, Assumptions.None, complex: false).Solve(difference, 0);
        if (!roots.IsComplete || roots.IsEveryValue || roots.Solutions.Any(root => root.Index is not null || root.Conditions.Count > 0))
            return null;

        // Each root as the number of periods it is from the start, exact where that is rational.
        var at = new List<(double Steps, Rational? Exact, double X)>();
        foreach (Solution root in roots.Solutions)
        {
            double x = root.Value.Evaluate();
            if (Ratio(root.Value - start, period) is Constant { Value: var exact })
                at.Add((exact.ToDouble(), exact, x));
            else
            {
                // An irrational number of periods is no whole number, but one this close can't be told from one.
                double steps = (x - start.Evaluate()) / period.Evaluate();
                if (!double.IsFinite(steps) || Math.Abs(steps - Math.Round(steps)) < 1e-9)
                    return null;
                at.Add((steps, null, x));
            }
        }

        at.Sort((a, b) => a.Steps.CompareTo(b.Steps));
        var k = new Variable(_index);

        // Only the roots fail it: the family keeps the rest, k != 1 and k != -1 for x^2 != π^2.
        if (condition.Comparison == Comparison.NotEqual)
        {
            List<Condition> excluding = [.. open, .. at.Where(root => root.Exact is { IsInteger: true })
                .Select(root => new Condition(k, Comparison.NotEqual, root.Exact!.Value))];
            return Normalized(start, period, excluding) is { } rest ? [rest] : [];
        }

        bool Holds(int sign) => condition.Comparison switch
        {
            Comparison.Equal => sign == 0,
            Comparison.NotEqual => sign != 0,
            Comparison.Less => sign < 0,
            Comparison.LessOrEqual => sign <= 0,
            Comparison.Greater => sign > 0,
            _ => sign >= 0,
        };

        // The runs of k in order, null for no end, with whether the condition holds on each;
        // neighbours that agree are one run.
        var runs = new List<(Rational? From, Rational? To, bool Holds)>();
        void Run(Rational? from, Rational? to, bool holds)
        {
            if (from > to)
                return;
            if (runs.Count > 0 && runs[^1].Holds == holds)
                runs[^1] = (runs[^1].From, to, holds);
            else
                runs.Add((from, to, holds));
        }

        for (int i = 0; i <= at.Count; i++)
        {
            Rational? from = i == 0 ? null : at[i - 1].Exact is { } low ? (low.IsInteger ? low + 1 : low.Floor() + 1) : (Rational)Math.Floor(at[i - 1].Steps) + 1;
            Rational? to = i == at.Count ? null : at[i].Exact is { } high ? (high.IsInteger ? high - 1 : high.Ceiling() - 1) : (Rational)Math.Ceiling(at[i].Steps) - 1;
            double x = at.Count == 0 ? 0 : i == 0 ? at[0].X - 1 : i == at.Count ? at[^1].X + 1 : (at[i - 1].X + at[i].X) / 2;
            Run(from, to, Holds(Math.Sign(difference.Evaluate((variable, x)))));
            if (i < at.Count && at[i].Exact is { IsInteger: true } index)
                Run(index, index, Holds(0));
        }

        var parts = new List<Solution>();
        foreach (var (from, to, _) in runs.Where(run => run.Holds))
        {
            if (from is { } first && to is { } last && last - first < 100)
            {
                for (Rational n = first; n <= last; n += 1)
                {
                    if (Single(start, period, n, open) is { } single)
                        parts.Add(single);
                }

                continue;
            }

            List<Condition> bounded = [.. open];
            if (from is { } lower)
                bounded.Add(new Condition(k, Comparison.GreaterOrEqual, lower));
            if (to is { } upper)
                bounded.Add(new Condition(k, Comparison.LessOrEqual, upper));
            if (Normalized(start, period, bounded) is { } part)
                parts.Add(part);
        }

        return parts;
    }

    // The solutions without those another one already gives, and with the families that make one
    // with half the period merged: 2 * π * k and π + 2 * π * k are π * k.
    private List<Solution> Reduced(List<Solution> solutions)
    {
        if (numeric)
            return solutions;

        var kept = new List<Solution>(solutions);
        for (bool merged = true; merged;)
        {
            merged = false;
            for (int i = 0; i < kept.Count && !merged; i++)
            {
                for (int j = i + 1; j < kept.Count && !merged; j++)
                {
                    if (Merged(kept[i], kept[j]) is { } family)
                    {
                        kept[i] = family;
                        kept.RemoveAt(j);
                        merged = true;
                    }
                }
            }
        }

        for (int i = kept.Count - 1; i >= 0; i--)
        {
            Solution solution = kept[i];
            if (kept.Any(other => !ReferenceEquals(other, solution) && Covers(other, solution)))
                kept.RemoveAt(i);
        }

        return kept;
    }

    private Solution? Merged(Solution a, Solution b)
    {
        if (a.Index is null || b.Index is null || !a.Period!.Equals(b.Period) || a.Conditions.Any(OnIndex) || b.Conditions.Any(OnIndex)
            || !a.Conditions.ToHashSet().SetEquals(b.Conditions))
            return null;
        if (Ratio(b.Start - a.Start, a.Period) is not Constant { Value: var steps } || !(steps - new Rational(1, 2)).IsInteger)
            return null;
        return Normalized(a.Start, (a.Period / 2).Simplify(assumptions), a.Conditions);
    }

    // Whether every value of the inner solution is one of the outer, under fewer conditions: 0 is
    // π * k for k = 0, and π * k with k >= 0 has it.
    private bool Covers(Solution outer, Solution inner)
    {
        List<Condition> onIndex = [.. outer.Conditions.Where(OnIndex)];
        if (!outer.Conditions.Where(condition => !OnIndex(condition)).All(inner.Conditions.Contains))
            return false;
        if (outer.Index is null)
            return inner.Index is null && outer.Value.Equals(inner.Value);

        bool Steps(Expr distance) => Ratio(distance, outer.Period!) is Constant { Value.IsInteger: true };
        if (inner.Index is not null)
            return onIndex.Count == 0 && Steps(inner.Period!) && Steps(inner.Start - outer.Start);
        return Ratio(inner.Value - outer.Start, outer.Period!) is Constant { Value: { IsInteger: true } index }
            && Reindexed(onIndex, index).All(condition => Decide(condition) == true);
    }

    // Expanded, as Simplify leaves 1/2 + (π - 1) / 2 a sum.
    private static Expr Ratio(Expr a, Expr b) => (a / b).Expand();

    private static Comparison Flipped(Comparison comparison) => comparison switch
    {
        Comparison.Less => Comparison.Greater,
        Comparison.LessOrEqual => Comparison.GreaterOrEqual,
        Comparison.Greater => Comparison.Less,
        Comparison.GreaterOrEqual => Comparison.LessOrEqual,
        var same => same,
    };
}
