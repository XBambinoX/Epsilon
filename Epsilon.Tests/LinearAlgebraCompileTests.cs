using Xunit;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class SymbolicCompileTests
{
    private static Expr P(string text) => ExprParser.Parse(text).Simplify();

    private static void AssertBitwiseEqual(Matrix<double> expected, Matrix<double> actual)
    {
        Assert.Equal((expected.Rows, expected.Columns), (actual.Rows, actual.Columns));
        for (int i = 0; i < expected.Rows; i++)
            for (int j = 0; j < expected.Columns; j++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(expected[i, j]), BitConverter.DoubleToInt64Bits(actual[i, j]));
    }

    [Fact]
    public void A_compiled_matrix_returns_exactly_what_Evaluate_returns()
    {
        var m = Matrix<Expr>.Parse("[[sqrt(x) * ln(y), x / y, 1 / (x - y)], [atan(y / x), cot(x) * y^x, -x]]").Simplify();
        Func<double, double, Matrix<double>> compiled = m.Compile("x", "y");

        // Includes NaN (sqrt of a negative), an infinity (1/0) and -0.
        foreach (var (x, y) in new[] { (1.5, 2.0), (-4.0, 0.5), (3.0, 3.0), (0.0, 1e-300), (1e10, -7.25) })
            AssertBitwiseEqual(m.Evaluate(("x", x), ("y", y)), compiled(x, y));
    }

    [Fact]
    public void Values_come_in_the_order_of_the_names()
    {
        var m = Matrix<Expr>.Parse("[[x, y, z], [w, 2x, 0]]");

        Assert.Equal(Matrix<double>.FromRows([1, 2, 3], [4, 2, 0]), m.Compile(["x", "y", "z", "w"])([1, 2, 3, 4]));
        Assert.Equal(Matrix<double>.FromRows([3, 1, 2], [4, 6, 0]), m.Compile(["y", "z", "x", "w"])([1, 2, 3, 4]));

        var n = Matrix<Expr>.Parse("[[x - y, z]]");
        Assert.Equal(Matrix<double>.FromRows([-1, 3]), n.Compile("x", "y", "z")(1, 2, 3));
        Assert.Equal(Matrix<double>.FromRows([1, 1]), n.Compile("z", "y", "x")(1, 2, 3));
    }

    [Fact]
    public void Without_names_the_single_variable_is_used()
    {
        var rotation = Matrix<Expr>.Parse("[[cos(t), -sin(t)], [sin(t), cos(t)]]");
        Func<double, Matrix<double>> compiled = rotation.Compile();

        AssertBitwiseEqual(rotation.Evaluate(0.7), compiled(0.7));
        AssertBitwiseEqual(rotation.Evaluate(0.7), rotation.Compile("t")(0.7));
        // Unused names are allowed.
        AssertBitwiseEqual(rotation.Evaluate(0.7), rotation.Compile("s", "t")(123, 0.7));
    }

    [Fact]
    public void A_constant_matrix_ignores_the_argument()
    {
        var m = Matrix<Expr>.Parse("[[1, pi], [e, 2/3]]");

        AssertBitwiseEqual(m.Evaluate(0), m.Compile()(double.NaN));
        Assert.Equal(Math.PI, m.Compile([])([])[0, 1]);
    }

    [Fact]
    public void Shared_subexpressions_are_computed_once_for_the_whole_matrix()
    {
        int[] calls = [0];
        Expr g = new Counted(new Variable("x"), calls);
        var m = Matrix<Expr>.FromRows([g, g + 1], [new Sin(g), 2 * g]);
        Func<double, Matrix<double>> compiled = m.Compile();
        var expected = m.Evaluate(1.5);

        calls[0] = 0;
        Assert.Equal(expected, compiled(1.5));
        Assert.Equal(1, calls[0]);
    }

    [Fact]
    public void Every_call_returns_a_new_matrix()
    {
        Func<double, Matrix<double>> compiled = Matrix<Expr>.Parse("[[x, x^2]]").Compile();

        var first = compiled(2);
        var second = compiled(3);

        Assert.Equal(Matrix<double>.FromRows([2, 4]), first);
        Assert.Equal(Matrix<double>.FromRows([3, 9]), second);
    }

    [Fact]
    public void CompileEntries_writes_the_entries_row_by_row()
    {
        var m = Matrix<Expr>.Parse("[[x, y], [x + y, x * y], [1, -x]]");
        Action<double[], double[]> compiled = m.CompileEntries(["x", "y"]);
        double[] entries = new double[6];

        compiled([2, 5], entries);
        Assert.Equal([2, 5, 7, 10, 1, -2], entries);

        compiled([3, 4], entries);
        Assert.Equal([3, 4, 7, 12, 1, -3], entries);
    }

    [Fact]
    public void CompileEntries_may_write_over_the_values()
    {
        // 2 x 1: as many entries as variables, so one array can hold both.
        var swap = Matrix<Expr>.Parse("[[y], [x]]");
        double[] values = [1, 2];

        swap.CompileEntries(["x", "y"])(values, values);
        Assert.Equal([2, 1], values);
    }

    [Fact]
    public void Empty_matrices_compile()
    {
        var m = Matrix<Expr>.Parse("[[]]");

        var result = m.Compile()(1);
        Assert.Equal((1, 0), (result.Rows, result.Columns));
        m.CompileEntries([])([], []);
    }

    [Fact]
    public void Two_variables_need_their_names()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[y, x]]").Compile());
        Assert.Equal("Expected at most 1 variable, found 2: [x, y]. Use an overload with the names of the variables.", ex.Message);

        Assert.Throws<InvalidOperationException>(() => Vector<Expr>.Parse("[y, x]").Compile());
    }

    [Fact]
    public void Errors_at_compile_time()
    {
        var m = Matrix<Expr>.Parse("[[x, z]]");

        var unlisted = Assert.Throws<ArgumentException>(() => m.Compile("x", "y"));
        Assert.Equal("The expression has the variable 'z', which is not in the list of variables [x, y].", unlisted.Message);
        Assert.Throws<ArgumentException>(() => m.Compile("x", "x"));
        Assert.Throws<ArgumentException>(() => m.CompileEntries(["x", "z", "x"]));
        Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[1, i]]").Compile());
        Assert.Throws<ArgumentNullException>(() => m.Compile((IReadOnlyList<string>)null!));
    }

    [Fact]
    public void Errors_at_call_time()
    {
        var m = Matrix<Expr>.Parse("[[x, y]]");

        var values = Assert.Throws<ArgumentException>(() => m.Compile(["x", "y"])([1]));
        Assert.Equal("Expected 2 values, one per variable, got 1. (Parameter 'values')", values.Message);

        var results = Assert.Throws<ArgumentException>(() => m.CompileEntries(["x", "y"])([1, 2], new double[3]));
        Assert.StartsWith("Expected an array of 2 results", results.Message);
    }

    [Fact]
    public void The_delegate_works_from_many_threads()
    {
        var m = Matrix<Expr>.Parse("[[sin(x)^2, x * cos(x)], [exp(-x), sin(x)^2 + 1]]");
        Func<double, Matrix<double>> compiled = m.Compile();

        Parallel.For(0, 1000, i => AssertBitwiseEqual(m.Evaluate(i * 0.01), compiled(i * 0.01)));
    }

    [Fact]
    public void A_compiled_Jacobian_built_from_symbolic_derivatives()
    {
        // The use case: derive once symbolically, then evaluate fast many times.
        Vector<Expr> f = [P("x^2 * y"), P("sin(x) + y")];
        var jacobian = Matrix<Expr>.FromBlocks([f.Differentiate("x"), f.Differentiate("y")]);
        Func<double, double, Matrix<double>> compiled = jacobian.Compile("x", "y");

        AssertBitwiseEqual(jacobian.Evaluate(("x", 0.5), ("y", -2)), compiled(0.5, -2));
        Assert.Equal(Matrix<double>.FromRows([2 * 0.5 * -2, 0.25], [Math.Cos(0.5), 1]), compiled(0.5, -2));
    }

    // Counts its evaluations, to see how often the compiled code computes it.
    private sealed class Counted(Expr argument, int[] calls) : UnaryExpr(argument)
    {
        public override double Evaluate(IReadOnlyDictionary<string, double> bindings)
        {
            calls[0]++;
            return 2 * Argument.Evaluate(bindings);
        }

        protected override Expr DifferentiateCore(string variable) => throw new NotSupportedException();

        protected override Expr WithArgument(Expr argument) => new Counted(argument, calls);
    }
}

public class SymbolicVectorCompileTests
{
    private static void AssertBitwiseEqual(Vector<double> expected, Vector<double> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(BitConverter.DoubleToInt64Bits(expected[i]), BitConverter.DoubleToInt64Bits(actual[i]));
    }

    [Fact]
    public void A_curve_compiles_to_a_function_of_its_parameter()
    {
        var helix = Vector<Expr>.Parse("[cos(t), sin(t), t / (2pi)]", ["t"]);
        Func<double, Vector<double>> compiled = helix.Compile();

        foreach (double t in new[] { 0, 0.3, Math.PI, -10 })
        {
            AssertBitwiseEqual(helix.Evaluate(t), compiled(t));
            AssertBitwiseEqual(helix.Evaluate(t), helix.Compile("t")(t));
        }
    }

    [Fact]
    public void Surfaces_fields_and_any_number_of_variables()
    {
        var surface = Vector<Expr>.Parse("[u * cos(v), u * sin(v), v]", ["u", "v"]);
        AssertBitwiseEqual(surface.Evaluate(("u", 2), ("v", 0.4)), surface.Compile("u", "v")(2, 0.4));

        var field = Vector<Expr>.Parse("[y * z, -x * z, x * y]");
        AssertBitwiseEqual(field.Evaluate(("x", 1), ("y", 2), ("z", 3)), field.Compile("x", "y", "z")(1, 2, 3));
        Assert.Equal(Vector.Create(6.0, -3, 2), field.Compile(["x", "y", "z"])([1, 2, 3]));
    }

    [Fact]
    public void CompileEntries_writes_the_entries_in_order()
    {
        var v = Vector<Expr>.Parse("[x + y, x - y, x * y]");
        double[] entries = new double[3];

        v.CompileEntries(["x", "y"])([5, 3], entries);
        Assert.Equal([8, 2, 15], entries);
    }

    [Fact]
    public void Errors_are_those_of_matrices()
    {
        var v = Vector<Expr>.Parse("[x, z]");

        Assert.Throws<ArgumentException>(() => v.Compile("x"));
        Assert.Throws<ArgumentException>(() => v.Compile(["x", "z"])([1]));
        Assert.Throws<InvalidOperationException>(() => Vector<Expr>.Parse("[i]").Compile());
    }
}

public class SymbolicJacobianTests
{
    [Fact]
    public void Polar_coordinates()
    {
        var polar = Vector<Expr>.Parse("[r * cos(t), r * sin(t)]", ["r", "t"]);

        Assert.Equal("[[cos(t), -sin(t) * r], [sin(t), cos(t) * r]]", polar.Jacobian(["r", "t"]).Print());
    }

    [Fact]
    public void One_row_per_entry_and_one_column_per_variable_in_the_given_order()
    {
        var f = Vector<Expr>.Parse("[x^2 * y, sin(x) + y]");

        var j = f.Jacobian(["x", "y", "z"]);
        Assert.Equal((2, 3), (j.Rows, j.Columns));
        // z doesn't occur: a column of zeros.
        Assert.Equal("[[2x * y, x^2, 0], [cos(x), 1, 0]]", j.Print());

        Assert.Equal("[[x^2, 2x * y], [1, cos(x)]]", f.Jacobian(["y", "x"]).Print());
        Assert.Equal(f.Differentiate("y"), f.Jacobian(["x", "y"]).Column(1));
    }

    [Fact]
    public void A_single_entry_gives_the_gradient_as_a_row()
    {
        Vector<Expr> f = [ExprParser.Parse("x * y^2")];

        Assert.Equal("[[y^2, 2x * y]]", f.Jacobian(["x", "y"]).Print());
    }

    [Fact]
    public void The_compiled_Jacobian_matches_finite_differences()
    {
        // A two-link planar arm with unit links: the position of its tip.
        var tip = Vector<Expr>.Parse("[cos(a) + cos(a + b), sin(a) + sin(a + b)]", ["a", "b"]);
        Func<double, double, Matrix<double>> jacobian = tip.Jacobian(["a", "b"]).Compile("a", "b");
        Func<double, double, Vector<double>> position = tip.Compile("a", "b");

        const double a = 0.4, b = 1.1, h = 1e-6;
        var j = jacobian(a, b);
        var byA = (position(a + h, b) - position(a - h, b)) / (2 * h);
        var byB = (position(a, b + h) - position(a, b - h)) / (2 * h);

        Assert.True(j.Column(0).IsApproximately(byA, 1e-8), $"{j.Column(0)} vs {byA}");
        Assert.True(j.Column(1).IsApproximately(byB, 1e-8), $"{j.Column(1)} vs {byB}");
    }

    [Fact]
    public void Empty_lists()
    {
        var f = Vector<Expr>.Parse("[x, y]");

        var noVariables = f.Jacobian([]);
        Assert.Equal((2, 0), (noVariables.Rows, noVariables.Columns));

        var noEntries = Vector<Expr>.Parse("[]").Jacobian(["x"]);
        Assert.Equal((0, 1), (noEntries.Rows, noEntries.Columns));
    }

    [Fact]
    public void Errors()
    {
        var f = Vector<Expr>.Parse("[x, y]");

        var twice = Assert.Throws<ArgumentException>(() => f.Jacobian(["x", "y", "x"]));
        Assert.Equal("Variable 'x' is listed twice. (Parameter 'variables')", twice.Message);
        Assert.Throws<ArgumentNullException>(() => f.Jacobian(null!));
        Assert.Throws<ArgumentNullException>(() => f.Jacobian(["x", null!]));
        Assert.Throws<NotSupportedException>(() => Vector<Expr>.Parse("[floor(x)]").Jacobian(["x"]));
    }
}
