using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

// Entry-wise operations, parsing, text and LaTeX for matrices of expressions. Each
// entry-wise method does what the method of the same name does to an Expr.
public static partial class SymbolicMatrixExtensions
{
    extension(Matrix<Expr> matrix)
    {
        /// <summary>
        /// A matrix from text such as <c>[[a, b], [c, d]]</c>. Every entry is read by
        /// <see cref="ExprParser.Parse"/> and, as there, not simplified.
        /// </summary>
        /// <exception cref="FormatException">
        /// The brackets are malformed, the rows have different lengths, or an entry is not a valid expression.
        /// </exception>
        public static Matrix<Expr> Parse(string text) => Matrix<Expr>.Parse(text, []);

        /// <summary>
        /// The same with only <paramref name="variableNames"/> as variables, which may then have
        /// longer names: <c>Parse("[[theta, 2t]]", ["theta", "t"])</c>. See <see cref="ExprParser.Parse"/>.
        /// </summary>
        /// <exception cref="FormatException">
        /// The brackets are malformed, the rows have different lengths, or an entry is not a valid expression.
        /// </exception>
        public static Matrix<Expr> Parse(string text, string[] variableNames)
        {
            var rows = MatrixParser.ParseRows(text);

            int columns = rows.Count == 0 ? 0 : rows[0].Count;
            for (int i = 1; i < rows.Count; i++)
                if (rows[i].Count != columns)
                    throw new FormatException($"Row {i} has {rows[i].Count} entries, but row 0 has {columns}.");

            return Matrix<Expr>.Create(rows.Count, columns,
                (i, j) => ParseEntry(rows[i][j], variableNames, $"({i}, {j})"));
        }

        /// <summary>Every entry simplified.</summary>
        public Matrix<Expr> Simplify(SimplifyMode mode = SimplifyMode.Generic) =>
            matrix.Map(entry => entry.Simplify(mode));

        /// <summary>Every entry simplified, using what is known about the variables.</summary>
        public Matrix<Expr> Simplify(Assumptions assumptions, SimplifyMode mode = SimplifyMode.Generic) =>
            matrix.Map(entry => entry.Simplify(assumptions, mode));

        /// <summary>Every entry expanded.</summary>
        public Matrix<Expr> Expand(SimplifyMode mode = SimplifyMode.Generic) =>
            matrix.Map(entry => entry.Expand(mode));

        /// <summary>The variable replaced in every entry; the entries are not simplified.</summary>
        public Matrix<Expr> Substitute(string variable, Expr replacement) =>
            matrix.Map(entry => entry.Substitute(variable, replacement));

        /// <summary>The simplified partial derivative of every entry.</summary>
        public Matrix<Expr> Differentiate(string variable) =>
            matrix.Map(entry => entry.Differentiate(variable));

        /// <summary>The names of the variables in all entries.</summary>
        public IReadOnlySet<string> GetVariables() => VariablesOf(matrix.Entries);

        /// <summary>The real value of every entry with the given variable values; NaN or an infinity where an entry is undefined.</summary>
        /// <exception cref="ArgumentException">A variable has no value.</exception>
        public Matrix<double> Evaluate(IReadOnlyDictionary<string, double> bindings) =>
            matrix.Map(entry => entry.Evaluate(bindings));

        /// <summary>The value of every entry when all of them together have at most one variable.</summary>
        /// <exception cref="InvalidOperationException">The entries have more than one variable.</exception>
        public Matrix<double> Evaluate(double x) =>
            matrix.Evaluate(SingleBinding(matrix.GetVariables(), x));

        /// <summary>The complex value of every entry, principal branches.</summary>
        /// <exception cref="ArgumentException">A variable has no value.</exception>
        public Matrix<ComplexNumber> EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
            matrix.Map(entry => entry.EvaluateComplex(bindings));

        /// <summary>The complex value of every entry when all of them together have at most one variable.</summary>
        /// <exception cref="InvalidOperationException">The entries have more than one variable.</exception>
        public Matrix<ComplexNumber> EvaluateComplex(ComplexNumber x) =>
            matrix.EvaluateComplex(SingleBinding(matrix.GetVariables(), x));

        /// <summary>The rows in brackets with every entry printed, readable by <c>Parse</c>: <c>[[x^2, 1/2], [0, 1]]</c>.</summary>
        public string Print() => matrix.Map(entry => entry.Print()).ToString();

        /// <summary>LaTeX: <c>\begin{bmatrix} x^{2} &amp; \frac{1}{2} \\ 0 &amp; 1 \end{bmatrix}</c>.</summary>
        public string ToLatex() =>
            Bmatrix(Enumerable.Range(0, matrix.Rows).Select(i =>
                string.Join(" & ", Enumerable.Range(0, matrix.Columns).Select(j => matrix[i, j].ToLatex()))));
    }

    // Classic extension methods rather than members of an extension block: with params there,
    // the C# 14 compiler of SDK 10.0.1xx reports a false nullability warning (CS8620) at every
    // call. For the same reason Parse takes its variable names as an array without params.
    /// <summary>The real value of every entry, with values as pairs: <c>Evaluate(("x", 1), ("y", 2))</c>.</summary>
    /// <exception cref="ArgumentException">A variable has no value.</exception>
    public static Matrix<double> Evaluate(this Matrix<Expr> matrix, params (string Name, double Value)[] bindings) =>
        matrix.Evaluate(Bindings(bindings));

    /// <summary>The complex value of every entry, with values as pairs: <c>EvaluateComplex(("x", -1), ("y", 2))</c>.</summary>
    /// <exception cref="ArgumentException">A variable has no value.</exception>
    public static Matrix<ComplexNumber> EvaluateComplex(this Matrix<Expr> matrix, params (string Name, ComplexNumber Value)[] bindings) =>
        matrix.EvaluateComplex(Bindings(bindings));

    // Parses one entry; an error names the entry and where it starts in the whole text.
    internal static Expr ParseEntry((string Text, int Position) entry, string[] variableNames, string where)
    {
        string text = entry.Text.TrimStart();
        int position = entry.Position + entry.Text.Length - text.Length;

        try
        {
            return ExprParser.Parse(text, variableNames);
        }
        catch (FormatException e)
        {
            throw new FormatException($"Invalid entry at {where}, starting at position {position}: {e.Message}", e);
        }
    }

    internal static HashSet<string> VariablesOf(ReadOnlySpan<Expr> entries)
    {
        var names = new HashSet<string>();
        foreach (Expr entry in entries)
            names.UnionWith(entry.GetVariables());

        return names;
    }

    internal static Dictionary<string, T> Bindings<T>((string Name, T Value)[] pairs)
    {
        var bindings = new Dictionary<string, T>(pairs.Length);
        foreach (var (name, value) in pairs)
            bindings[name] = value;

        return bindings;
    }

    // The binding for the single-value Evaluate overloads; none when every entry is constant.
    internal static Dictionary<string, T> SingleBinding<T>(IReadOnlySet<string> variables, T value)
    {
        if (variables.Count > 1)
            throw new InvalidOperationException(
                $"Expected at most 1 variable, found {variables.Count}: [{string.Join(", ", variables.Order(StringComparer.Ordinal))}]. " +
                "Use the overload with named values for several variables.");

        var bindings = new Dictionary<string, T>();
        foreach (string name in variables)
            bindings[name] = value;

        return bindings;
    }

    internal static string Bmatrix(IEnumerable<string> rows) =>
        @"\begin{bmatrix} " + string.Join(@" \\ ", rows) + @" \end{bmatrix}";
}
