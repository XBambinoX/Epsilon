using Epsilon.Core;

namespace Epsilon.Solving;

// The solutions in a range of the real line, the values of each family there listed one by one:
// sin(x) = 1/2 in [0, 10] at π / 6, 5 * π / 6, 13 * π / 6 and 17 * π / 6.
internal sealed partial class Solver
{
    // More values of a family than this in a range stay a family, with bounds on its index.
    private const int MaxListed = 10_000;

    // Numerically, more than this are left to the scan, as they are without a range.
    private const int MaxNumbers = 1_000_000;

    // A value too close to an end of the range to tell from it counts as in it: 2 * π is in
    // [0, 2 * Math.PI], whose end is just below 2 * π.
    internal SolutionSet SolutionsWithin(SolutionSet set, Expr from, Expr to)
    {
        var x = new Variable(variable);
        List<Condition> range = [new(x, Comparison.GreaterOrEqual, from), new(x, Comparison.LessOrEqual, to)];
        if (set.IsEveryValue)
            return SolutionSet.EveryValue(variable, [.. set.Conditions, .. range], complex, this);

        var solutions = new List<Solution>();
        foreach (Solution solution in set.Solutions)
        {
            if (solution.Start.GetVariables().Count > 0 || solution.Period?.GetVariables().Count > 0)
                solutions.AddRange(Admitted(solution, range));
            else if (solution.Index is not null)
                solutions.AddRange(Listed(solution, from, to));
            else if (Holds(solution.Value, Comparison.GreaterOrEqual, from) && Holds(solution.Value, Comparison.LessOrEqual, to))
                solutions.Add(solution);
        }

        return SolutionSet.Of(variable, solutions, set.Unsolved, complex, this);
    }

    private bool Holds(Expr value, Comparison comparison, Expr end) => Decide(new Condition(value, comparison, end)) != false;

    // The values of a family in [from, to], or the family with its index bounded to them where
    // there are more than MaxListed.
    private List<Solution> Listed(Solution family, Expr from, Expr to)
    {
        var k = new Variable(_index);
        List<Condition> conditions = [
            .. family.Conditions,
            new(k, Comparison.GreaterOrEqual, End(family, from, Comparison.GreaterOrEqual)),
            new(k, Comparison.LessOrEqual, End(family, to, Comparison.LessOrEqual)),
        ];
        if (Normalized(family.Start, family.Period!, conditions) is not { } bounded)
            return [];
        if (bounded.Index is null)
            return [bounded];

        Rational lower = Bound(bounded, Comparison.GreaterOrEqual), upper = Bound(bounded, Comparison.LessOrEqual);
        if (upper - lower >= MaxListed)
            return [bounded];

        var values = new List<Solution>();
        for (Rational n = lower; n <= upper; n += 1)
        {
            if (Single(bounded.Start, bounded.Period!, n, bounded.Conditions) is { } value)
                values.Add(value);
        }

        return values;
    }

    // The first index whose value is at least the end, or the last at most it. The nearest
    // integers to where the family meets the end are the only candidates, so an end far bigger
    // than the period, where neighbouring values can't be told apart, can't make it search long.
    private Rational End(Solution family, Expr end, Comparison comparison)
    {
        Expr quotient = Ratio(end - family.Start, family.Period!);
        Rational near = quotient is Constant { Value: var q } ? q.Floor() : (Rational)Math.Floor(quotient.Evaluate());
        Rational[] candidates = comparison == Comparison.GreaterOrEqual
            ? [near - 1, near, near + 1, near + 2]
            : [near + 2, near + 1, near, near - 1];
        return candidates.FirstOrDefault(n => Holds((family.Start + n * family.Period!).Simplify(assumptions), comparison, end), candidates[^1]);
    }

    // A bound Normalized wrote: k >= n or k <= n.
    private Rational Bound(Solution family, Comparison comparison) =>
        family.Conditions
            .Select(condition => condition is { Left: Variable { Name: var name }, Right: Constant { Value: { IsInteger: true } n } }
                && name == _index && condition.Comparison == comparison ? n : (Rational?)null)
            .First(n => n is not null)!.Value;

    // Numerically, the values of a family in [low, high] as numbers, polished on the equation;
    // null where there are more than MaxNumbers, or a condition on the index isn't a bound, for
    // the scan to find them.
    private List<Solution>? Numbers(Solution family, double low, double high, Expr difference)
    {
        double lower = double.NegativeInfinity, upper = double.PositiveInfinity;
        var excluded = new HashSet<double>();
        foreach (Condition condition in family.Conditions)
        {
            if (condition is not { Left: Variable { Name: var name }, Right: Constant { Value: { IsInteger: true } n } } || name != _index)
                return null;
            switch (condition.Comparison)
            {
                case Comparison.GreaterOrEqual:
                    lower = n.ToDouble();
                    break;
                case Comparison.LessOrEqual:
                    upper = n.ToDouble();
                    break;
                case Comparison.NotEqual:
                    excluded.Add(n.ToDouble());
                    break;
                default:
                    return null;
            }
        }

        double start = Number(family.Start).Real, period = Number(family.Period!).Real;
        double first = Math.Max(lower, Math.Ceiling((low - start) / period) - 1);
        double last = Math.Min(upper, Math.Floor((high - start) / period) + 1);
        if (!double.IsFinite(first) || !double.IsFinite(last) || last - first > MaxNumbers)
            return null;

        Func<double, double> polished = RealRoots.Polisher(difference, variable, many: last - first > 1000);

        var values = new List<Solution>();
        for (double k = first; k <= last; k++)
        {
            if (excluded.Contains(k))
                continue;
            double x = polished(Math.FusedMultiplyAdd(k, period, start));
            if (x < low || x > high)
                continue;

            Expr value = x;
            _numbers[value] = x;
            values.Add(new Solution(value, []));
        }

        return values;
    }
}
