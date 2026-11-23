using Epsilon.Core;

namespace Epsilon.Solving;

/// <summary>
/// One solution of an equation, an exact value, or a family of them that repeats with a period,
/// as π / 6 + 2 * π * k for every integer k; and the conditions it holds under.
/// </summary>
public sealed class Solution
{
    internal Solution(Expr value, IReadOnlyList<Condition> conditions)
    {
        Value = value;
        Start = value;
        Conditions = conditions;
    }

    // The family start + period * index, with the assumptions it was solved under, which decide
    // its conditions at one index.
    internal Solution(Expr start, Expr period, string index, IReadOnlyList<Condition> conditions, Assumptions assumptions)
    {
        Term = (period * new Variable(index)).Simplify();
        Value = start is Constant { Value.IsZero: true } ? Term : new Add(start, Term);
        Start = start;
        Period = period;
        Index = index;
        Conditions = conditions;
        _assumptions = assumptions;
    }

    private readonly Assumptions? _assumptions;

    /// <summary>
    /// The value of the unknown, exact. It may contain the other variables, and for a family,
    /// <see cref="Index"/>: π / 6 + 2 * π * k.
    /// </summary>
    public Expr Value { get; }

    /// <summary>
    /// For a family, how far apart its values are: 2 * π for π / 6 + 2 * π * k, positive where it
    /// is a number; with other variables in it, as 2 * π / a, of either sign. Null for a single value.
    /// </summary>
    public Expr? Period { get; }

    /// <summary>
    /// For a family, the variable in <see cref="Value"/> that takes every integer value (and in
    /// <see cref="Conditions"/>, where only some do: <c>k &gt;= 0</c>). Null for a single value.
    /// </summary>
    public string? Index { get; }

    /// <summary>
    /// The conditions the value is a solution under, empty if it always is: <c>a != 0</c> for
    /// -b / a. Conditions the assumptions prove are left out.
    /// </summary>
    public IReadOnlyList<Condition> Conditions { get; }

    // The value at index 0.
    internal Expr Start { get; }

    private Expr? Term { get; }

    /// <summary>
    /// The value of a family at one value of <see cref="Index"/>, with the conditions that remain on
    /// the other variables; null where the conditions on the index leave it out: π * k with
    /// <c>k != 0</c> is π at 1 and nothing at 0. A single value is itself at every index.
    /// </summary>
    /// <param name="index">The value of <see cref="Index"/>.</param>
    public Solution? At(int index) => Index is null ? this : Solver.At(this, index, _assumptions!);

    /// <summary>The value, and its conditions after <c>if</c>: <c>-b / a if a != 0</c>.</summary>
    public string Print() => ValueText(value => value.Print()) + SolutionSet.ConditionsText(Conditions);

    /// <summary>The value and its conditions in LaTeX.</summary>
    public string ToLatex() => ValueText(value => value.ToLatex()) + SolutionSet.ConditionsLatex(Conditions);

    /// <inheritdoc/>
    public override string ToString() => Print();

    // A family prints its start first, as it is usually written: π / 6 + 2 * π * k.
    internal string ValueText(Func<Expr, string> print) =>
        Term is null || Start is Constant { Value.IsZero: true } ? print(Value) : print(Start) + " + " + print(Term);

    internal Solution With(IReadOnlyList<Condition> conditions) =>
        Index is null ? new Solution(Value, conditions) : new Solution(Start, Period!, Index, conditions, _assumptions!);

    internal bool SameAs(Solution other) =>
        Value.Equals(other.Value) && Conditions.SequenceEqual(other.Conditions);
}
