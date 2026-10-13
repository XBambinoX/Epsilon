using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

// Compiled evaluation of whole vectors: the compiled one-column matrix, wrapped.
public static partial class SymbolicVectorExtensions
{
    extension(Vector<Expr> vector)
    {
        /// <summary>
        /// Compiles a vector whose entries together have at most one variable, such as a curve
        /// <c>[cos(t), sin(t), t]</c>, to a delegate that returns exactly what
        /// <c>Evaluate(double x)</c> returns, entry for entry and bit for bit, many times faster.
        /// A subexpression that occurs in several entries is computed once. For a vector without
        /// variables the argument is ignored.
        /// </summary>
        /// <remarks>
        /// Like Evaluate, compiling doesn't simplify. Every call returns a new vector, and the
        /// delegate is safe to call from many threads.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The entries have two or more variables, or an entry contains the imaginary unit.
        /// </exception>
        public Func<double, Vector<double>> Compile()
        {
            var compiled = vector.ToMatrix().Compile();
            return x => new Vector<double>(compiled(x));
        }

        /// <summary>
        /// Compiles to a delegate of the value of <paramref name="x"/>: <c>Compile("t")(2)</c>
        /// equals <c>Evaluate(("t", 2))</c>. See <see cref="Compile(Vector{Expr})"/>.
        /// </summary>
        /// <exception cref="ArgumentException">An entry has a variable other than <paramref name="x"/>.</exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Func<double, Vector<double>> Compile(string x)
        {
            var compiled = vector.ToMatrix().Compile(x);
            return value => new Vector<double>(compiled(value));
        }

        /// <summary>
        /// Compiles to a delegate of the values of <paramref name="x"/> and <paramref name="y"/>,
        /// in this order, such as a surface <c>[u cos(v), u sin(v), v]</c>. A variable the entries
        /// don't use is allowed and ignored. See <see cref="Compile(Vector{Expr})"/>.
        /// </summary>
        /// <exception cref="ArgumentException">An entry has another variable, or the same name is given twice.</exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Func<double, double, Vector<double>> Compile(string x, string y)
        {
            var compiled = vector.ToMatrix().Compile(x, y);
            return (xValue, yValue) => new Vector<double>(compiled(xValue, yValue));
        }

        /// <summary>
        /// Compiles to a delegate of the values of <paramref name="x"/>, <paramref name="y"/> and
        /// <paramref name="z"/>, in this order, such as a vector field. See
        /// <see cref="Compile(Vector{Expr}, string, string)"/>.
        /// </summary>
        /// <exception cref="ArgumentException">An entry has another variable, or the same name is given twice.</exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Func<double, double, double, Vector<double>> Compile(string x, string y, string z)
        {
            var compiled = vector.ToMatrix().Compile(x, y, z);
            return (xValue, yValue, zValue) => new Vector<double>(compiled(xValue, yValue, zValue));
        }

        /// <summary>
        /// Compiles to a delegate of any number of variables, whose values come in an array in the
        /// order of <paramref name="variables"/>: <c>Compile(["a", "b"])([1, 2])</c>. See
        /// <see cref="Compile(Vector{Expr}, string, string)"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// An entry has a variable that is not listed, or a name is listed twice. The delegate
        /// throws it when the array doesn't have one value per variable.
        /// </exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Func<double[], Vector<double>> Compile(IReadOnlyList<string> variables)
        {
            var compiled = vector.ToMatrix().Compile(variables);
            return values => new Vector<double>(compiled(values));
        }

        /// <summary>
        /// The same without allocating, for hot loops: the delegate writes the values of the
        /// entries in order to an array of <c>Length</c> doubles that the caller provides and may
        /// reuse. All values are read before the first entry is written, so both arrays may be
        /// the same. See <see cref="Compile(Vector{Expr}, IReadOnlyList{string})"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// An entry has a variable that is not listed, or a name is listed twice. The delegate
        /// throws it when an array has the wrong length.
        /// </exception>
        /// <exception cref="InvalidOperationException">An entry contains the imaginary unit.</exception>
        public Action<double[], double[]> CompileEntries(IReadOnlyList<string> variables) =>
            vector.ToMatrix().CompileEntries(variables);
    }
}
