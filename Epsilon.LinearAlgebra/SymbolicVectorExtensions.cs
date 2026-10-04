using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

/// <summary>
/// Arithmetic and entry-wise operations on vectors of expressions. Unlike the operators of
/// <see cref="Expr"/>, which only build a tree, the arithmetic computes: every entry of the result is simplified.
/// </summary>
public static class SymbolicVectorExtensions
{
    extension(Vector<Expr> vector)
    {
        /// <summary>The vector of <paramref name="length"/> zeros.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        public static Vector<Expr> Zero(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            return new Vector<Expr>(Enumerable.Repeat(SymbolicMatrixExtensions.ZeroEntry, length).ToArray());
        }

        /// <summary>The simplified dot product: the sum of the products of the entries at the same index.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public Expr Dot(Vector<Expr> other)
        {
            vector.CheckSameLength(other, "multiply");
            return SymbolicMatrixExtensions.Sum(vector.Length, i => vector[i] * other[i]);
        }

        /// <summary>The entry-wise sum, simplified.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<Expr> operator +(Vector<Expr> left, Vector<Expr> right) =>
            left.Combine(right, (a, b) => (a + b).Simplify(), "add");

        /// <summary>The entry-wise difference, simplified.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<Expr> operator -(Vector<Expr> left, Vector<Expr> right) =>
            left.Combine(right, (a, b) => (a - b).Simplify(), "subtract");

        /// <summary>Every entry negated and simplified.</summary>
        public static Vector<Expr> operator -(Vector<Expr> value) => value.Map(a => (-a).Simplify());

        /// <summary>Every entry multiplied by the expression and simplified.</summary>
        public static Vector<Expr> operator *(Expr scalar, Vector<Expr> value) => value.Map(a => (scalar * a).Simplify());

        /// <summary>Every entry multiplied by the expression and simplified.</summary>
        public static Vector<Expr> operator *(Vector<Expr> value, Expr scalar) => value.Map(a => (a * scalar).Simplify());

        /// <summary>Every entry divided by the expression and simplified.</summary>
        public static Vector<Expr> operator /(Vector<Expr> value, Expr scalar) => value.Map(a => (a / scalar).Simplify());

        /// <summary>
        /// A vector from text such as <c>[a, b, c]</c>. Every entry is read by
        /// <see cref="ExprParser.Parse"/> and, as there, not simplified.
        /// </summary>
        /// <exception cref="FormatException">The brackets are malformed or an entry is not a valid expression.</exception>
        public static Vector<Expr> Parse(string text) => Vector<Expr>.Parse(text, []);

        /// <summary>
        /// The same with only <paramref name="variableNames"/> as variables, which may then have
        /// longer names: <c>Parse("[theta, 2t]", ["theta", "t"])</c>. See <see cref="ExprParser.Parse"/>.
        /// </summary>
        /// <exception cref="FormatException">The brackets are malformed or an entry is not a valid expression.</exception>
        public static Vector<Expr> Parse(string text, string[] variableNames)
        {
            var entries = MatrixParser.ParseVector(text);
            return Vector.Create(entries.Select((entry, i) => SymbolicMatrixExtensions.ParseEntry(entry, variableNames, $"index {i}")).ToArray());
        }

        /// <summary>Every entry simplified.</summary>
        public Vector<Expr> Simplify(SimplifyMode mode = SimplifyMode.Generic) =>
            vector.Map(entry => entry.Simplify(mode));

        /// <summary>Every entry simplified, using what is known about the variables.</summary>
        public Vector<Expr> Simplify(Assumptions assumptions, SimplifyMode mode = SimplifyMode.Generic) =>
            vector.Map(entry => entry.Simplify(assumptions, mode));

        /// <summary>Every entry expanded.</summary>
        public Vector<Expr> Expand(SimplifyMode mode = SimplifyMode.Generic) =>
            vector.Map(entry => entry.Expand(mode));

        /// <summary>The variable replaced in every entry; the entries are not simplified.</summary>
        public Vector<Expr> Substitute(string variable, Expr replacement) =>
            vector.Map(entry => entry.Substitute(variable, replacement));

        /// <summary>The simplified partial derivative of every entry.</summary>
        public Vector<Expr> Differentiate(string variable) =>
            vector.Map(entry => entry.Differentiate(variable));

        /// <summary>The names of the variables in all entries.</summary>
        public IReadOnlySet<string> GetVariables() => SymbolicMatrixExtensions.VariablesOf(vector.ToMatrix().Entries);

        /// <summary>The real value of every entry with the given variable values; NaN or an infinity where an entry is undefined.</summary>
        /// <exception cref="ArgumentException">A variable has no value.</exception>
        public Vector<double> Evaluate(IReadOnlyDictionary<string, double> bindings) =>
            vector.Map(entry => entry.Evaluate(bindings));

        /// <summary>The value of every entry when all of them together have at most one variable.</summary>
        /// <exception cref="InvalidOperationException">The entries have more than one variable.</exception>
        public Vector<double> Evaluate(double x) =>
            vector.Evaluate(SymbolicMatrixExtensions.SingleBinding(vector.GetVariables(), x));

        /// <summary>The complex value of every entry, principal branches.</summary>
        /// <exception cref="ArgumentException">A variable has no value.</exception>
        public Vector<ComplexNumber> EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
            vector.Map(entry => entry.EvaluateComplex(bindings));

        /// <summary>The complex value of every entry when all of them together have at most one variable.</summary>
        /// <exception cref="InvalidOperationException">The entries have more than one variable.</exception>
        public Vector<ComplexNumber> EvaluateComplex(ComplexNumber x) =>
            vector.EvaluateComplex(SymbolicMatrixExtensions.SingleBinding(vector.GetVariables(), x));

        /// <summary>The entries in brackets, printed, readable by <c>Parse</c>: <c>[x^2, 1/2]</c>.</summary>
        public string Print() => vector.Map(entry => entry.Print()).ToString();

        /// <summary>LaTeX of the column: <c>\begin{bmatrix} x^{2} \\ \frac{1}{2} \end{bmatrix}</c>.</summary>
        public string ToLatex() => SymbolicMatrixExtensions.Bmatrix(vector.Select(entry => entry.ToLatex()));
    }

    // Classic extension methods rather than members of the extension block: with params there,
    // the C# 14 compiler of SDK 10.0.1xx reports a false nullability warning (CS8620) at every
    // call. For the same reason Parse takes its variable names as an array without params.
    /// <summary>The real value of every entry, with values as pairs: <c>Evaluate(("x", 1), ("y", 2))</c>.</summary>
    /// <exception cref="ArgumentException">A variable has no value.</exception>
    public static Vector<double> Evaluate(this Vector<Expr> vector, params (string Name, double Value)[] bindings) =>
        vector.Evaluate(SymbolicMatrixExtensions.Bindings(bindings));

    /// <summary>The complex value of every entry, with values as pairs: <c>EvaluateComplex(("x", -1), ("y", 2))</c>.</summary>
    /// <exception cref="ArgumentException">A variable has no value.</exception>
    public static Vector<ComplexNumber> EvaluateComplex(this Vector<Expr> vector, params (string Name, ComplexNumber Value)[] bindings) =>
        vector.EvaluateComplex(SymbolicMatrixExtensions.Bindings(bindings));
}
