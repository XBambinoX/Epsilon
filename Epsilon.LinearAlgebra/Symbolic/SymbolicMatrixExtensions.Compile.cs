using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

// Compiled evaluation of whole matrices, built on Expr.Compile of all entries at once, so a
// subexpression shared by several entries is computed once per call; and the same as C# source.
public static partial class SymbolicMatrixExtensions
{
    extension(Matrix<Expr> matrix)
    {
        /// <summary>
        /// Compiles a matrix whose entries together have at most one variable to a delegate that
        /// returns exactly what <c>Evaluate(double x)</c> returns, entry for entry and bit for bit,
        /// many times faster: compile once, then call it as often as needed. A subexpression that
        /// occurs in several entries is computed once. For a matrix without variables the argument
        /// is ignored.
        /// </summary>
        /// <remarks>
        /// Like Evaluate, compiling doesn't simplify. Every call returns a new matrix, and the
        /// delegate is safe to call from many threads.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The entries have two or more variables, or an entry contains the imaginary unit.
        /// </exception>
        public Func<double, Matrix<double>> Compile() => matrix.Compile(SingleVariable(matrix.GetVariables()));

        /// <summary>
        /// Compiles to a delegate of the value of <paramref name="x"/>: <c>Compile("t")(2)</c>
        /// equals <c>Evaluate(("t", 2))</c>. See <see cref="Compile(Matrix{Expr})"/>.
        /// </summary>
        /// <exception cref="ArgumentException">An entry has a variable other than <paramref name="x"/>.</exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Func<double, Matrix<double>> Compile(string x)
        {
            var compiled = matrix.Compile([x]);
            return value => compiled([value]);
        }

        /// <summary>
        /// Compiles to a delegate of the values of <paramref name="x"/> and <paramref name="y"/>,
        /// in this order: <c>Compile("x", "y")(3, 4)</c> equals <c>Evaluate(("x", 3), ("y", 4))</c>.
        /// A variable the entries don't use is allowed and ignored. See <see cref="Compile(Matrix{Expr})"/>.
        /// </summary>
        /// <exception cref="ArgumentException">An entry has another variable, or the same name is given twice.</exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Func<double, double, Matrix<double>> Compile(string x, string y)
        {
            var compiled = matrix.Compile([x, y]);
            return (xValue, yValue) => compiled([xValue, yValue]);
        }

        /// <summary>
        /// Compiles to a delegate of the values of <paramref name="x"/>, <paramref name="y"/> and
        /// <paramref name="z"/>, in this order. See <see cref="Compile(Matrix{Expr}, string, string)"/>.
        /// </summary>
        /// <exception cref="ArgumentException">An entry has another variable, or the same name is given twice.</exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Func<double, double, double, Matrix<double>> Compile(string x, string y, string z)
        {
            var compiled = matrix.Compile([x, y, z]);
            return (xValue, yValue, zValue) => compiled([xValue, yValue, zValue]);
        }

        /// <summary>
        /// Compiles to a delegate of any number of variables, whose values come in an array in the
        /// order of <paramref name="variables"/>: <c>Compile(["a", "b"])([1, 2])</c>. See
        /// <see cref="Compile(Matrix{Expr}, string, string)"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// An entry has a variable that is not listed, or a name is listed twice. The delegate
        /// throws it when the array doesn't have one value per variable.
        /// </exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Func<double[], Matrix<double>> Compile(IReadOnlyList<string> variables)
        {
            var compiled = matrix.CompileEntries(variables);
            int rows = matrix.Rows, columns = matrix.Columns;

            return values =>
            {
                var entries = new double[rows * columns];
                compiled(values, entries);
                return new Matrix<double>(rows, columns, entries);
            };
        }

        /// <summary>
        /// The same without allocating, for hot loops: the delegate writes the values of the
        /// entries row by row to an array of <c>Rows * Columns</c> doubles that the caller
        /// provides and may reuse, so entry (i, j) lands at index <c>i * Columns + j</c>. All
        /// values are read before the first entry is written, so both arrays may be the same.
        /// See <see cref="Compile(Matrix{Expr}, IReadOnlyList{string})"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// An entry has a variable that is not listed, or a name is listed twice. The delegate
        /// throws it when an array has the wrong length.
        /// </exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Action<double[], double[]> CompileEntries(IReadOnlyList<string> variables) =>
            Expr.Compile(matrix.Entries.ToArray(), variables);

        /// <summary>
        /// The source of a C# method that computes the entries from the values of
        /// <paramref name="variables"/>, its parameters in this order, for code that should not
        /// depend on Epsilon or runs where delegates can't be compiled at run time. Entry (i, j)
        /// goes to <c>result[i * Columns + j]</c>; a subexpression that occurs more than once is
        /// computed once, into a local declared before its first use. <c>Matrix&lt;Expr&gt;.Rotation(t).ToCSharp("Rotation", ["t"])</c>
        /// gives
        /// <code>
        /// public static void Rotation(double t, Span&lt;double&gt; result)
        /// {
        ///     double t0 = Math.Cos(t);
        ///     result[0] = t0;
        ///     double t1 = Math.Sin(t);
        ///     result[1] = -t1;
        ///     result[2] = t1;
        ///     result[3] = t0;
        /// }
        /// </code>
        /// </summary>
        /// <remarks>
        /// The method returns exactly the values of Evaluate, bit for bit, as <c>Compile</c> does.
        /// It needs <c>using System;</c> and C# 8 or later. Names that are C# keywords are written
        /// with an <c>@</c>.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// A name is not a valid C# name, a variable is listed twice or is also the name of the
        /// method, or an entry has a variable that is not listed.
        /// </exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        /// <exception cref="NotSupportedException">An entry contains a node from outside the core.</exception>
        public string ToCSharp(string name, IReadOnlyList<string> variables) =>
            CSharpWriter.Write(matrix, name, variables);
    }

    // The variable of the parameterless Compile overloads; any name when every entry is constant.
    internal static string SingleVariable(IReadOnlySet<string> variables)
    {
        if (variables.Count > 1)
            throw new InvalidOperationException(
                $"Expected at most 1 variable, found {variables.Count}: [{string.Join(", ", variables.Order(StringComparer.Ordinal))}]. " +
                "Use an overload with the names of the variables.");

        return variables.Count == 0 ? "x" : variables.First();
    }
}
