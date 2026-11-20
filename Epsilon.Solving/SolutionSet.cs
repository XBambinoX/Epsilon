using Epsilon.Core;

namespace Epsilon.Solving;

/// <summary>
/// The solutions of an equation in one unknown: exact values and families of them that repeat
/// with a period, each with the conditions it holds under, or every value; and the parts of the
/// equation that weren't solved, if any.
/// </summary>
public sealed class SolutionSet
{
    private readonly bool _complex;

    private SolutionSet(string variable, IReadOnlyList<Solution> solutions, bool isEveryValue, IReadOnlyList<Condition> conditions, IReadOnlyList<Expr> unsolved, bool complex)
    {
        _complex = complex;
        Variable = variable;
        Solutions = solutions;
        IsEveryValue = isEveryValue;
        Conditions = conditions;
        Unsolved = unsolved;
    }

    /// <summary>The unknown.</summary>
    public string Variable { get; }

    /// <summary>
    /// The exact solutions, each once: ordered by value where they are numbers (complex ones by
    /// the real part, then the imaginary part, families by their value at index 0), otherwise in
    /// the order they were found.
    /// </summary>
    public IReadOnlyList<Solution> Solutions { get; }

    /// <summary>
    /// Whether every value of the unknown is a solution, where both sides are defined and the
    /// assumptions allow it: x + 1 = 1 + x. <see cref="Solutions"/> is empty then, and
    /// <see cref="Conditions"/> says where the equation is defined, as <c>x != 0</c> for x/x = 1.
    /// </summary>
    public bool IsEveryValue { get; }

    /// <summary>
    /// With <see cref="IsEveryValue"/>, the conditions under which every value is a solution:
    /// <c>a = b</c> for a = b in x. Empty otherwise.
    /// </summary>
    public IReadOnlyList<Condition> Conditions { get; }

    /// <summary>
    /// The expressions that must be 0 for the solutions not listed, and that weren't solved:
    /// empty if every solution is listed.
    /// </summary>
    public IReadOnlyList<Expr> Unsolved { get; }

    /// <summary>Whether every solution is listed, that is, nothing is <see cref="Unsolved"/>.</summary>
    public bool IsComplete => Unsolved.Count == 0;

    /// <summary>Whether the equation is known to have no solution.</summary>
    public bool IsEmpty => IsComplete && !IsEveryValue && Solutions.Count == 0;

    /// <summary>
    /// The set as text: <c>{2, 3}</c>, <c>{-b / a} if a != 0</c>, <c>{}</c> for no solution,
    /// <c>(-inf, inf)</c> for every real value (<c>C</c> for every complex one), and <c>...</c> for solutions that weren't found:
    /// <c>{1, ...}</c>. Families of solutions name their index after a bar, with the conditions
    /// on it: <c>{π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z}</c>, <c>{π * k | k in Z, k != 0}</c>.
    /// </summary>
    public string Print()
    {
        if (IsEveryValue)
            return (_complex ? "C" : "(-inf, inf)") + ConditionsText(Conditions);

        var (items, shared) = Items(solution => solution.ValueText(value => value.Print()), solution => solution.Print());
        if (!IsComplete)
            items.Add("...");
        var (onIndex, rest) = ByIndex(shared);
        string index = Index is { } k ? " | " + k + " in Z" + string.Concat(onIndex.Select(condition => ", " + condition.Print())) : "";
        return "{" + string.Join(", ", items) + index + "}" + ConditionsText(rest);
    }

    /// <summary>The set in LaTeX: <c>\left\{2, 3\right\}</c>, <c>\emptyset</c> for no solution.</summary>
    public string ToLatex()
    {
        if (IsEveryValue)
            return (_complex ? "\\mathbb{C}" : "(-\\infty, \\infty)") + ConditionsLatex(Conditions);
        if (IsEmpty)
            return "\\emptyset";

        var (items, shared) = Items(solution => solution.ValueText(value => value.ToLatex()), solution => solution.ToLatex());
        if (!IsComplete)
            items.Add("\\ldots");
        var (onIndex, rest) = ByIndex(shared);
        string index = Index is { } k
            ? " \\mid " + k + " \\in \\mathbb{Z}" + string.Concat(onIndex.Select(condition => ", " + condition.ToLatex()))
            : "";
        return "\\left\\{" + string.Join(", ", items) + index + "\\right\\}" + ConditionsLatex(rest);
    }

    // The index of the families, the same for all of them.
    private string? Index => Solutions.Select(solution => solution.Index).FirstOrDefault(index => index is not null);

    // The conditions shared by all the solutions that are on the index go with it inside the braces.
    private (List<Condition> OnIndex, List<Condition> Others) ByIndex(IReadOnlyList<Condition> conditions)
    {
        if (Index is not { } k)
            return ([], [.. conditions]);
        bool On(Condition condition) => condition.Left.DependsOn(k) || condition.Right.DependsOn(k);
        return ([.. conditions.Where(On)], [.. conditions.Where(condition => !On(condition))]);
    }

    /// <inheritdoc/>
    public override string ToString() => Print();

    // With the same conditions for all, they are written once after the braces.
    private (List<string> Items, IReadOnlyList<Condition> Shared) Items(Func<Solution, string> value, Func<Solution, string> withConditions)
    {
        bool shared = Solutions.Count > 0 && Solutions.All(solution => solution.Conditions.SequenceEqual(Solutions[0].Conditions));
        return shared
            ? ([.. Solutions.Select(value)], Solutions[0].Conditions)
            : ([.. Solutions.Select(withConditions)], []);
    }

    internal static string ConditionsText(IReadOnlyList<Condition> conditions) =>
        conditions.Count == 0 ? "" : " if " + string.Join(" and ", conditions.Select(condition => condition.Print()));

    internal static string ConditionsLatex(IReadOnlyList<Condition> conditions) =>
        conditions.Count == 0 ? "" : " \\text{ if } " + string.Join(" \\text{ and } ", conditions.Select(condition => condition.ToLatex()));

    internal static SolutionSet Of(string variable, IEnumerable<Solution> solutions, IReadOnlyList<Expr> unsolved, bool complex = false)
    {
        var distinct = new List<Solution>();
        var byValue = new Dictionary<Expr, List<Solution>>();
        foreach (Solution solution in solutions)
        {
            if (!byValue.TryGetValue(solution.Value, out List<Solution>? same))
                byValue[solution.Value] = same = [];
            if (!same.Any(seen => seen.SameAs(solution)))
            {
                same.Add(solution);
                distinct.Add(solution);
            }
        }

        return new SolutionSet(variable, Ordered(distinct), false, [], unsolved, complex);
    }

    internal static SolutionSet EveryValue(string variable, IReadOnlyList<Condition> conditions, bool complex = false) =>
        new(variable, [], true, conditions, [], complex);

    internal static SolutionSet Empty(string variable, bool complex = false) => new(variable, [], false, [], [], complex);

    private static List<Solution> Ordered(List<Solution> solutions)
    {
        var values = new List<ComplexNumber>();
        foreach (Solution solution in solutions)
        {
            if (solution.Start.GetVariables().Count > 0)
                return solutions;
            ComplexNumber value = solution.Start.EvaluateComplex();
            if (!double.IsFinite(value.Magnitude))
                return solutions;
            values.Add(value);
        }

        return [.. solutions.Zip(values).OrderBy(pair => pair.Second.Real).ThenBy(pair => pair.Second.Imaginary).Select(pair => pair.First)];
    }
}
