using Epsilon.Core;

namespace Epsilon.Solving;

/// <summary>
/// One solution of an equation: its value, exact or approximate, and the conditions it holds under.
/// </summary>
public sealed class Solution
{
    internal Solution(Expr value, IReadOnlyList<Condition> conditions, bool isApproximate = false)
    {
        Value = value;
        Conditions = conditions;
        IsApproximate = isApproximate;
    }

    /// <summary>
    /// The value of the unknown, exact unless <see cref="IsApproximate"/>. It may contain the other
    /// variables.
    /// </summary>
    public Expr Value { get; }

    /// <summary>
    /// Whether <see cref="Value"/> is a number found numerically, to 15 significant digits, where
    /// the exact one has no closed form (a root of x^5 - x - 1) or wasn't found.
    /// </summary>
    public bool IsApproximate { get; }

    /// <summary>
    /// The conditions the value is a solution under, empty if it always is: <c>a != 0</c> for
    /// -b / a. Conditions the assumptions prove are left out.
    /// </summary>
    public IReadOnlyList<Condition> Conditions { get; }

    /// <summary>
    /// The value, and its conditions after <c>if</c>: <c>-b / a if a != 0</c>; an approximate one
    /// as a decimal after <c>~</c>: <c>~1.16730397826142</c>.
    /// </summary>
    public string Print() => ValueText() + SolutionSet.ConditionsText(Conditions);

    /// <summary>The value and its conditions in LaTeX; an approximate one after <c>\approx</c>.</summary>
    public string ToLatex() => ValueLatex() + SolutionSet.ConditionsLatex(Conditions);

    /// <inheritdoc/>
    public override string ToString() => Print();

    internal string ValueText() => IsApproximate ? "~" + Decimal(latex: false) : Value.Print();

    internal string ValueLatex() => IsApproximate ? "\\approx " + Decimal(latex: true) : Value.ToLatex();

    private string Decimal(bool latex)
    {
        ComplexNumber value = Value.EvaluateComplex();
        string real = Text(value.Real), imaginary = Text(Math.Abs(value.Imaginary)) + (latex ? " i" : " * i");
        if (value.Imaginary == 0)
            return real;
        if (value.Real == 0)
            return (value.Imaginary < 0 ? "-" : "") + imaginary;
        return real + (value.Imaginary < 0 ? " - " : " + ") + imaginary;
    }

    internal static string Text(double value) => value.ToString("G15", System.Globalization.CultureInfo.InvariantCulture);

    internal bool SameAs(Solution other) =>
        Value.Equals(other.Value) && Conditions.SequenceEqual(other.Conditions);
}
