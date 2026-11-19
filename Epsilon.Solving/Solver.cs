using Epsilon.Core;

namespace Epsilon.Solving;

internal sealed class Solver(string variable, Assumptions assumptions)
{
    internal SolutionSet Solve(Expr left, Expr right)
    {
        // Strict, so that no step enlarges the domain: x/x = 1 isn't 1 = 1 at x = 0.
        left = left.Simplify(assumptions, SimplifyMode.Strict);
        right = right.Simplify(assumptions, SimplifyMode.Strict);
        Expr difference = (left - right).Simplify(assumptions, SimplifyMode.Strict);

        if (!difference.DependsOn(variable))
            return WithoutUnknown(difference, left, right);

        if (Polynomials.Coefficients(difference, variable, maxDegree: 1) is [var constant, var slope] && IsNonZero(slope))
            return SolutionSet.Of(variable, Admitted((-constant / slope).Simplify(assumptions)), []);

        return SolutionSet.Of(variable, [], [difference]);
    }

    private SolutionSet WithoutUnknown(Expr difference, Expr left, Expr right)
    {
        if (difference is Constant { Value.IsZero: true })
            return SolutionSet.EveryValue(variable, []);
        if (IsNonZero(difference))
            return SolutionSet.Empty(variable);
        return SolutionSet.EveryValue(variable, [new Condition(left, Comparison.Equal, right)]);
    }

    // The value as a solution with the condition that the assumptions about the unknown's sign
    // put on it, or nothing where they rule it out.
    private IEnumerable<Solution> Admitted(Expr value)
    {
        if (SignRequired() is not var (comparison, holds, fails))
        {
            yield return new Solution(value, []);
            yield break;
        }

        if (fails(value, assumptions))
            yield break;
        yield return new Solution(value, holds(value, assumptions) ? [] : [new Condition(value, comparison, 0)]);
    }

    private (Comparison, Func<Expr, Assumptions, bool> Holds, Func<Expr, Assumptions, bool> Fails)? SignRequired()
    {
        if (assumptions.IsPositive(variable))
            return (Comparison.Greater, AssumptionAnalysis.IsProvablyPositive, AssumptionAnalysis.IsProvablyNonPositive);
        if (assumptions.IsNegative(variable))
            return (Comparison.Less, AssumptionAnalysis.IsProvablyNegative, AssumptionAnalysis.IsProvablyNonNegative);
        if (assumptions.IsNonNegative(variable))
            return (Comparison.GreaterOrEqual, AssumptionAnalysis.IsProvablyNonNegative, AssumptionAnalysis.IsProvablyNegative);
        if (assumptions.IsNonPositive(variable))
            return (Comparison.LessOrEqual, AssumptionAnalysis.IsProvablyNonPositive, AssumptionAnalysis.IsProvablyPositive);
        if (assumptions.IsNonZero(variable))
            return (Comparison.NotEqual, AssumptionAnalysis.IsProvablyNonZero, (value, _) => value is Constant { Value.IsZero: true });
        return null;
    }

    // Numbers by their value too, as pi - 3, which the assumptions alone don't decide.
    private bool IsNonZero(Expr expr) =>
        expr.IsProvablyNonZero(assumptions) || new Sign(expr).Simplify(assumptions) is Constant { Value.IsZero: false };
}
