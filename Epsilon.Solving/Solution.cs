using Epsilon.Core;

namespace Epsilon.Solving;

/// <summary>One solution of an equation: an exact value, and the conditions it holds under.</summary>
public sealed class Solution
{
    internal Solution(Expr value, IReadOnlyList<Condition> conditions)
    {
        Value = value;
        Conditions = conditions;
    }

    /// <summary>The value of the unknown, exact. It may contain the other variables.</summary>
    public Expr Value { get; }

    /// <summary>
    /// The conditions the value is a solution under, empty if it always is: <c>a != 0</c> for
    /// -b / a. Conditions the assumptions prove are left out.
    /// </summary>
    public IReadOnlyList<Condition> Conditions { get; }

    /// <summary>The value, and its conditions after <c>if</c>: <c>-b / a if a != 0</c>.</summary>
    public string Print() => Value.Print() + SolutionSet.ConditionsText(Conditions);

    /// <summary>The value and its conditions in LaTeX.</summary>
    public string ToLatex() => Value.ToLatex() + SolutionSet.ConditionsLatex(Conditions);

    /// <inheritdoc/>
    public override string ToString() => Print();

    internal bool SameAs(Solution other) =>
        Value.Equals(other.Value) && Conditions.SequenceEqual(other.Conditions);
}
