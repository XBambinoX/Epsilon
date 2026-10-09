using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

/// <summary>
/// Arithmetic, norms, angles, projections and entry-wise operations on vectors of expressions.
/// Unlike the operators of <see cref="Expr"/>, which only build a tree, the arithmetic computes:
/// every entry of the result is simplified.
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

        /// <summary>
        /// The point at <paramref name="t"/> on the line through two points: (1 - t) * start + t * end,
        /// every entry simplified.
        /// </summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<Expr> Lerp(Vector<Expr> start, Vector<Expr> end, Expr t) =>
            start.Combine(end, (a, b) => ((1 - t) * a + t * b).Simplify(), "interpolate between");

        /// <summary>The simplified dot product: the sum of the products of the entries at the same index.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public Expr Dot(Vector<Expr> other)
        {
            vector.CheckSameLength(other, "multiply");
            return SymbolicMatrixExtensions.Sum(vector.Length, i => vector[i] * other[i]);
        }

        /// <summary>The Euclidean norm sqrt(v . v), simplified: <c>[3, 4]</c> gives 5, <c>[x, 0]</c> gives abs(x).</summary>
        public Expr Norm() => new Sqrt(vector.NormSquared()).Simplify();

        /// <summary>The squared norm v . v, simplified: <c>[a, b]</c> gives a^2 + b^2.</summary>
        public Expr NormSquared() => vector.Dot(vector);

        /// <summary>
        /// The unit vector in the same direction, every entry simplified. With symbols it is
        /// undefined exactly where the vector is zero.
        /// </summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        public Vector<Expr> Normalize()
        {
            Expr norm = vector.Norm();
            if (IsZero(norm))
                throw new InvalidOperationException("Cannot normalize the zero vector.");

            return vector / norm;
        }

        /// <summary>The Euclidean distance between two points: the norm of their difference.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public Expr Distance(Vector<Expr> other)
        {
            vector.CheckSameLength(other, "measure the distance between");
            return (vector - other).Norm();
        }

        /// <summary>The cross product of two vectors of length 3, every entry simplified.</summary>
        /// <exception cref="InvalidOperationException">The vector has not 3 entries.</exception>
        /// <exception cref="ArgumentException"><paramref name="other"/> has not 3 entries.</exception>
        public Vector<Expr> Cross(Vector<Expr> other)
        {
            NumericVectorExtensions.CheckCrossLengths(vector.Length, other.Length);
            return
            [
                (vector[1] * other[2] - vector[2] * other[1]).Simplify(),
                (vector[2] * other[0] - vector[0] * other[2]).Simplify(),
                (vector[0] * other[1] - vector[1] * other[0]).Simplify(),
            ];
        }

        /// <summary>
        /// The angle acos(v . w / (|v| |w|)) between two vectors, simplified: <c>[1, 0]</c> and
        /// <c>[1, 1]</c> give pi/4. With symbols it is undefined exactly where a vector is zero.
        /// </summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        /// <exception cref="ArgumentException">The vectors have different lengths, or <paramref name="other"/> is zero.</exception>
        public Expr Angle(Vector<Expr> other)
        {
            vector.CheckSameLength(other, "measure the angle between");
            Expr norm = vector.Norm(), otherNorm = other.Norm();
            if (IsZero(norm))
                throw new InvalidOperationException(NumericVectorExtensions.UndefinedAngle);
            if (IsZero(otherNorm))
                throw new ArgumentException(NumericVectorExtensions.UndefinedAngle, nameof(other));

            return new Acos(vector.Dot(other) / (norm * otherNorm)).Simplify();
        }

        /// <summary>The projection (v . w / w . w) * w onto the line through <paramref name="onto"/>, every entry simplified.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths, or <paramref name="onto"/> is zero.</exception>
        public Vector<Expr> ProjectOnto(Vector<Expr> onto)
        {
            vector.CheckSameLength(onto, "project");

            Expr squared = onto.NormSquared();
            if (IsZero(squared))
                throw new ArgumentException("Cannot project onto the zero vector.", nameof(onto));

            return vector.Dot(onto) / squared * onto;
        }

        /// <summary>
        /// The mirror image v - 2 (v . n / n . n) * n in the plane through the origin perpendicular
        /// to <paramref name="normal"/>, every entry simplified. The normal need not have length 1.
        /// </summary>
        /// <exception cref="ArgumentException">The vectors have different lengths, or <paramref name="normal"/> is zero.</exception>
        public Vector<Expr> Reflect(Vector<Expr> normal)
        {
            vector.CheckSameLength(normal, "reflect");

            Expr squared = normal.NormSquared();
            if (IsZero(squared))
                throw new ArgumentException("The normal is the zero vector.", nameof(normal));

            return vector - 2 * vector.Dot(normal) / squared * normal;
        }

        /// <summary>The outer product v w^T: the matrix whose entry at (i, j) is v[i] * w[j], simplified.</summary>
        public Matrix<Expr> Outer(Vector<Expr> other) =>
            Matrix<Expr>.Create(vector.Length, other.Length, (i, j) => (vector[i] * other[j]).Simplify());

        /// <summary>The Hadamard product: the simplified products of the entries at the same index.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public Vector<Expr> Hadamard(Vector<Expr> other) =>
            vector.Combine(other, (a, b) => (a * b).Simplify(), "take the Hadamard product of");

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

    private static bool IsZero(Expr value) => value is Constant { Value.IsZero: true };
}
