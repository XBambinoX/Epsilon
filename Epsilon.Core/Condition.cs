namespace Epsilon.Core;

/// <summary>How the two sides of a <see cref="Condition"/> compare.</summary>
public enum Comparison
{
    /// <summary><c>=</c></summary>
    Equal,

    /// <summary><c>!=</c></summary>
    NotEqual,

    /// <summary><c>&lt;</c></summary>
    Less,

    /// <summary><c>&lt;=</c></summary>
    LessOrEqual,

    /// <summary><c>&gt;</c></summary>
    Greater,

    /// <summary><c>&gt;=</c></summary>
    GreaterOrEqual,
}

/// <summary>
/// Two expressions compared, such as <c>a != 0</c> or <c>x &lt;= 1</c>: the condition a
/// solution of an equation holds under, as a != 0 for -b / a.
/// </summary>
public sealed record Condition
{
    /// <summary>The condition <paramref name="left"/> <paramref name="comparison"/> <paramref name="right"/>.</summary>
    public Condition(Expr left, Comparison comparison, Expr right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        Left = left;
        Comparison = comparison;
        Right = right;
    }

    /// <summary>The left side.</summary>
    public Expr Left { get; }

    /// <summary>How the sides compare.</summary>
    public Comparison Comparison { get; }

    /// <summary>The right side.</summary>
    public Expr Right { get; }

    /// <summary>The condition as text: <c>a != 0</c>, <c>E / m &gt;= 0</c>.</summary>
    public string Print() => $"{Left.Print()} {Symbol(Comparison)} {Right.Print()}";

    /// <summary>The condition in LaTeX: <c>a \neq 0</c>.</summary>
    public string ToLatex() => $"{Left.ToLatex()} {LatexSymbol(Comparison)} {Right.ToLatex()}";

    /// <inheritdoc/>
    public override string ToString() => Print();

    private static string Symbol(Comparison comparison) => comparison switch
    {
        Comparison.Equal => "=",
        Comparison.NotEqual => "!=",
        Comparison.Less => "<",
        Comparison.LessOrEqual => "<=",
        Comparison.Greater => ">",
        _ => ">=",
    };

    private static string LatexSymbol(Comparison comparison) => comparison switch
    {
        Comparison.Equal => "=",
        Comparison.NotEqual => "\\neq",
        Comparison.Less => "<",
        Comparison.LessOrEqual => "\\leq",
        Comparison.Greater => ">",
        _ => "\\geq",
    };
}
