using Epsilon.Core;

namespace Epsilon.Solving;

/// <summary>
/// The solutions of an equation in one unknown as numbers: <see cref="double"/> over the real
/// numbers, <see cref="ComplexNumber"/> over the complex ones. See <see cref="Equation.SolveNumerically(string, Assumptions?)"/>.
/// </summary>
/// <typeparam name="T"><see cref="double"/> or <see cref="ComplexNumber"/>.</typeparam>
public sealed class NumericSolutionSet<T> where T : struct
{
    internal NumericSolutionSet(string variable, IReadOnlyList<T> values, SolutionSet exact)
    {
        Variable = variable;
        Values = values;
        IsEveryValue = exact.IsEveryValue;
        Conditions = exact.Conditions;
        Unsolved = exact.Unsolved;
    }

    /// <summary>The unknown.</summary>
    public string Variable { get; }

    /// <summary>
    /// The solutions, each once and in ascending order (complex ones by the real part, then the
    /// imaginary part), each to about full double precision.
    /// </summary>
    public IReadOnlyList<T> Values { get; }

    /// <summary>
    /// Whether every value of the unknown is a solution, where the equation is defined:
    /// x/x = 1. <see cref="Values"/> is empty then, and <see cref="Conditions"/> says where the
    /// equation is defined, as <c>x != 0</c>.
    /// </summary>
    public bool IsEveryValue { get; }

    /// <summary>With <see cref="IsEveryValue"/>, the conditions under which every value is a solution.</summary>
    public IReadOnlyList<Condition> Conditions { get; }

    /// <summary>
    /// The expressions that must be 0 for the solutions not listed, and that weren't solved:
    /// empty if every solution is listed.
    /// </summary>
    public IReadOnlyList<Expr> Unsolved { get; }

    /// <summary>Whether every solution is listed, that is, nothing is <see cref="Unsolved"/>.</summary>
    public bool IsComplete => Unsolved.Count == 0;

    /// <summary>Whether the equation is known to have no solution.</summary>
    public bool IsEmpty => IsComplete && !IsEveryValue && Values.Count == 0;

    /// <summary>
    /// The set as text: <c>{1.1673039782614187}</c>, <c>{}</c> for no solution,
    /// <c>(-inf, inf)</c> for every real value (<c>C</c> for every complex one), and <c>...</c>
    /// for solutions that weren't found.
    /// </summary>
    public string Print()
    {
        if (IsEveryValue)
            return (typeof(T) == typeof(ComplexNumber) ? "C" : "(-inf, inf)") + SolutionSet.ConditionsText(Conditions);

        List<string> items = [.. Values.Select(value => value is double real
            ? real.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString()!)];
        if (!IsComplete)
            items.Add("...");
        return "{" + string.Join(", ", items) + "}";
    }

    /// <inheritdoc/>
    public override string ToString() => Print();
}

internal static class NumericSolutions
{
    internal static NumericSolutionSet<double> Real(SolutionSet exact, Solver solver) =>
        new(exact.Variable, [.. Values(exact, solver).Select(value => value.Real)], exact);

    internal static NumericSolutionSet<ComplexNumber> Complex(SolutionSet exact, Solver solver) =>
        new(exact.Variable, Values(exact, solver), exact);

    // In order, each once: next to each other after sorting, the same value twice is close.
    private static List<ComplexNumber> Values(SolutionSet exact, Solver solver)
    {
        var values = new List<ComplexNumber>();
        foreach (ComplexNumber value in exact.Solutions.Select(solution => solver.Number(solution.Value))
            .OrderBy(value => value.Real).ThenBy(value => value.Imaginary))
        {
            if (values.Count == 0 || !((values[^1] - value).Magnitude <= 1e-12 * Math.Max(1, value.Magnitude)))
                values.Add(value);
        }

        return values;
    }
}
