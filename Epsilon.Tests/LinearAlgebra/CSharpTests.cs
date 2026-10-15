using Xunit;
using Epsilon.Core;
using Epsilon.LinearAlgebra;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Epsilon.Tests.LinearAlgebra;

// Compiles generated methods with Roslyn, as C# 8, and calls each through a bridge that takes
// the values in an array: a Span parameter can't be passed through reflection.
internal static class GeneratedCode
{
    private static readonly Lazy<MetadataReference[]> References = new(() =>
        [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))]);

    internal static Action<double[], double[]>[] Compile(params (string Source, string Name, int Parameters)[] methods)
    {
        var source = new System.Text.StringBuilder("using System;\n\npublic static class Generated\n{\n");
        foreach (var (method, name, parameters) in methods)
        {
            string arguments = string.Concat(Enumerable.Range(0, parameters).Select(i => $"values[{i}], "));
            source.Append(method).Append("\n\n");
            source.Append($"public static void Call_{name}(double[] values, double[] results) => {name}({arguments}results);\n\n");
        }
        source.Append('}');

        var compilation = CSharpCompilation.Create(
            "Generated" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source.ToString(), new CSharpParseOptions(LanguageVersion.CSharp8))],
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);
        var problems = emitted.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning && d.Location.IsInSource).ToList();
        Assert.True(emitted.Success && problems.Count == 0, string.Join("\n", problems) + "\n" + source);

        Type generated = System.Reflection.Assembly.Load(stream.ToArray()).GetType("Generated")!;
        return [.. methods.Select(m => generated.GetMethod("Call_" + m.Name)!.CreateDelegate<Action<double[], double[]>>())];
    }

    // The same bits, or both NaN, or the same exception (sign(NaN) throws in Evaluate too).
    internal static void AssertSameAsEvaluate(Expr expr, IReadOnlyDictionary<string, double> at, Func<double> generated)
    {
        double expected;
        try
        {
            expected = expr.Evaluate(at);
        }
        catch (Exception ex)
        {
            Assert.Throws(ex.GetType(), () => generated());
            return;
        }

        double actual = generated();
        bool same = double.IsNaN(expected)
            ? double.IsNaN(actual)
            : BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(actual);
        Assert.True(same, $"{expr.Print()} at [{string.Join(", ", at)}]: Evaluate gives {expected}, the generated code {actual}.");
    }
}

public class CSharpGenerationTests
{
    private static Expr P(string text) => ExprParser.Parse(text);

    private static readonly Variable X = new("x"), A = new("a"), B = new("b"), C = new("c");

    // Every node type of the core, as in the core's CompileTests.
    private static readonly string[] Corpus =
    [
        "x + 2", "x - 3", "2.5 * x", "x / 3", "-x", "0.1 * x", "pi * x", "e * x",
        "x^2", "x^(1/3)", "2^x", "e^x", "exp(x)", "ln(x)", "log(x, 2)",
        "sqrt(x)", "nthroot(x, 3)", "nthroot(x, 2)", "nthroot(27, x)",
        "abs(x)", "floor(x)", "ceiling(x)", "round(x)", "sign(x)", "min(x, 1)", "max(x, 1)",
        "sin(x)", "cos(x)", "tan(x)", "cot(x)", "sec(x)", "csc(x)", "asin(x)", "acos(x)", "atan(x)",
        "sinh(x)", "cosh(x)", "tanh(x)", "coth(x)", "sech(x)", "csch(x)", "asinh(x)", "acosh(x)", "atanh(x)",
        "sin(x)^2 + cos(x)^2 * e^(-x/10) - x / (1 + x^2) + sin(x) * round(sin(x))",
    ];

    private static readonly double[] Points =
    [
        double.NegativeInfinity, -1e300, -27, -10, -2.5, -1, -0.5, -1e-300, -0.0,
        0, 1e-300, 0.5, 1, 1.5, 2.5, 3, 10, 27, 1e300, double.PositiveInfinity, double.NaN,
    ];

    [Fact]
    public void Every_core_node_returns_exactly_what_Evaluate_returns()
    {
        Expr[] expressions = [.. Corpus.Select(P)];
        var methods = expressions.Select((expr, k) => (Vector.Create(expr).ToCSharp($"F{k}", ["x"]), $"F{k}", 1)).ToArray();
        var generated = GeneratedCode.Compile(methods);

        for (int k = 0; k < expressions.Length; k++)
            foreach (double x in Points)
            {
                double[] result = new double[1];
                GeneratedCode.AssertSameAsEvaluate(expressions[k], new Dictionary<string, double> { ["x"] = x },
                    () => { generated[k]([x], result); return result[0]; });
            }
    }

    [Fact]
    public void Shared_subexpressions_become_locals_declared_before_their_first_use()
    {
        Assert.Equal(
            """
            public static void Rotation(double t, Span<double> result)
            {
                double t0 = Math.Cos(t);
                result[0] = t0;
                double t1 = Math.Sin(t);
                result[1] = -t1;
                result[2] = t1;
                result[3] = t0;
            }
            """.ReplaceLineEndings("\n"),
            Matrix<Expr>.Rotation(P("t")).ToCSharp("Rotation", ["t"]));
    }

    // Built from the nodes, since the parser puts sums and products in its own order.
    private static readonly (Expr Tree, string Code)[] Groupings =
    [
        // Floating-point addition is not associative: (0.1 + 0.2) + 0.3 != 0.1 + (0.2 + 0.3).
        (new Add(new Add(A, B), C), "a + b + c"),
        (new Add(A, new Add(B, C)), "a + (b + c)"),
        (new Subtract(new Subtract(A, B), C), "a - b - c"),
        (new Subtract(A, new Subtract(B, C)), "a - (b - c)"),
        (new Divide(A, new Multiply(B, C)), "a / (b * c)"),
        (new Multiply(new Add(A, B), C), "(a + b) * c"),
        (new Negate(new Add(A, B)), "-(a + b)"),
        (new Multiply(A, new Negate(B)), "a * (-b)"),
        (new Multiply(new Negate(A), B), "-a * b"),
        (new Negate(new Negate(A)), "-(-a)"),
        (new Power(A, new Power(B, C)), "Math.Pow(a, Math.Pow(b, c))"),
        (new Divide(new Cot(A), B), "1.0 / Math.Tan(a) / b"),
        (new Divide(A, new Cot(B)), "a / (1.0 / Math.Tan(b))"),
        (new Divide(A, new Sign(B)), "a / ((double)Math.Sign(b))"),
        (new Multiply(new Sign(A), B), "(double)Math.Sign(a) * b"),
    ];

    [Fact]
    public void Parentheses_keep_the_order_of_evaluation()
    {
        var methods = Groupings.Select((g, k) => (Vector.Create(g.Tree).ToCSharp($"F{k}", ["a", "b", "c"]), $"F{k}", 3)).ToArray();
        for (int k = 0; k < Groupings.Length; k++)
            Assert.Contains($"    result[0] = {Groupings[k].Code};\n", methods[k].Item1);

        var generated = GeneratedCode.Compile(methods);
        for (int k = 0; k < Groupings.Length; k++)
            foreach (var (a, b, c) in new[] { (0.1, 0.2, 0.3), (1e16, -1e16, 1), (-2.5, 3, 0.7) })
            {
                double[] result = new double[1];
                var at = new Dictionary<string, double> { ["a"] = a, ["b"] = b, ["c"] = c };
                GeneratedCode.AssertSameAsEvaluate(Groupings[k].Tree, at, () => { generated[k]([a, b, c], result); return result[0]; });
            }
    }

    [Fact]
    public void Numbers_are_written_so_that_they_read_back_as_the_same_double()
    {
        Vector<Expr> v =
        [
            new Constant(2) * X, X * new Constant(-2.5), new Constant(0.1) + X, new Constant(1e20) * X,
            new Constant(new Rational(System.Numerics.BigInteger.Pow(10, 400))) * X, new Pi() + new EulerNumber(),
            new Divide(new Constant(1), new Constant(3)),
        ];
        string source = v.ToCSharp("F", ["x"]);

        Assert.Contains("result[0] = 2.0 * x;", source);
        Assert.Contains("result[1] = x * (-2.5);", source);
        Assert.Contains("result[2] = 0.1 + x;", source);
        Assert.Contains("result[3] = 1E+20 * x;", source);
        Assert.Contains("result[4] = double.PositiveInfinity * x;", source);
        Assert.Contains("result[5] = Math.PI + Math.E;", source);
        Assert.Contains("result[6] = 1.0 / 3.0;", source);

        var generated = GeneratedCode.Compile((source, "F", 1))[0];
        double[] result = new double[v.Length];
        generated([0.7], result);
        Assert.Equal(v.Evaluate(0.7), Vector.Create(result));
    }

    [Fact]
    public void A_kinematic_chain_gives_the_same_values_as_Compile()
    {
        var arm = Matrix<Expr>.Rotation(A).ToHomogeneous() * Matrix<Expr>.Translation(Vector<Expr>.Parse("[l, 0]"))
            * Matrix<Expr>.Rotation(B).ToHomogeneous() * Matrix<Expr>.Translation(Vector<Expr>.Parse("[m, 0]"));
        var jacobian = arm.TransformPoint(Vector<Expr>.Parse("[0, 0]")).Jacobian(["a", "b"]);
        string[] variables = ["a", "b", "l", "m"];

        var generated = GeneratedCode.Compile((jacobian.ToCSharp("ArmJacobian", variables), "ArmJacobian", 4))[0];
        Action<double[], double[]> compiled = jacobian.CompileEntries(variables);

        foreach (double[] values in new double[][] { [0.4, 1.1, 2, 1.5], [-3, 0.01, 1, 1], [100, -7, 0.5, 2] })
        {
            double[] expected = new double[4], actual = new double[4];
            compiled(values, expected);
            generated(values, actual);
            Assert.Equal(expected.Select(BitConverter.DoubleToInt64Bits), actual.Select(BitConverter.DoubleToInt64Bits));
        }
    }

    [Fact]
    public void Generated_names_never_clash_with_the_given_ones()
    {
        // Keywords get an @; the locals, the result and RealRoot skip names that are taken.
        Variable keyword = new("in"), t0 = new("t0"), result = new("result"), realRoot = new("RealRoot");
        Vector<Expr> v = [keyword + t0, new Sin(keyword) * new Sin(keyword), new NthRoot(result, new Constant(3)), realRoot];
        string source = v.ToCSharp("t1", ["in", "t0", "result", "RealRoot"]);

        Assert.StartsWith("public static void t1(double @in, double t0, double result, double RealRoot, Span<double> result1)", source);
        Assert.Contains("double t2 = Math.Sin(@in);", source);
        Assert.Contains("result1[2] = RealRoot1(result, 3.0);", source);
        Assert.Contains("static double RealRoot1(double argument, double degree)", source);

        var generated = GeneratedCode.Compile((source, "t1", 4))[0];
        double[] entries = new double[4];
        generated([0.5, 2, -8, 7], entries);
        Assert.Equal(v.Evaluate(("in", 0.5), ("t0", 2), ("result", -8), ("RealRoot", 7)), Vector.Create(entries));
    }

    [Fact]
    public void Constant_and_empty_matrices()
    {
        Assert.Equal("public static void F(Span<double> result)\n{\n    result[0] = 1.0 / 2.0;\n}", Vector<Expr>.Parse("[1/2]").ToCSharp("F", []));
        Assert.Equal("public static void F(double x, Span<double> result)\n{\n}", Matrix<Expr>.Parse("[[]]").ToCSharp("F", ["x"]));
    }

    [Fact]
    public void Errors()
    {
        var m = Matrix<Expr>.Parse("[[x, y]]");

        foreach (string bad in new[] { "", "2x", "a b", "x-y", "@x" })
        {
            Assert.Throws<ArgumentException>(() => m.ToCSharp(bad, ["x", "y"]));
            Assert.Throws<ArgumentException>(() => m.ToCSharp("F", ["x", "y", bad]));
        }

        var invalid = Assert.Throws<ArgumentException>(() => m.ToCSharp("2x", ["x", "y"]));
        Assert.Equal("'2x' is not a valid C# name: use letters, digits and underscores, starting with a letter or an underscore. (Parameter 'name')", invalid.Message);

        var unlisted = Assert.Throws<ArgumentException>(() => m.ToCSharp("F", ["x"]));
        Assert.Equal("An entry has the variable 'y', which is not in the list of variables [x]. (Parameter 'variables')", unlisted.Message);

        var twice = Assert.Throws<ArgumentException>(() => m.ToCSharp("F", ["x", "y", "x"]));
        Assert.Equal("Variable 'x' is listed twice. (Parameter 'variables')", twice.Message);

        var same = Assert.Throws<ArgumentException>(() => m.ToCSharp("x", ["x", "y"]));
        Assert.Equal("'x' is both the name of the method and a variable. (Parameter 'variables')", same.Message);

        Assert.Throws<ArgumentNullException>(() => m.ToCSharp(null!, ["x", "y"]));
        Assert.Throws<ArgumentNullException>(() => m.ToCSharp("F", null!));
        Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[1, i]]").ToCSharp("F", []));
        Assert.Throws<NotSupportedException>(() => Vector.Create<Expr>(new Epsilon.Tests.Other.Sin(X)).ToCSharp("F", ["x"]));
    }

    [Theory]
    [MemberData(nameof(Epsilon.Tests.Core.NestingLimitTests.InputsAtTheLimit), MemberType = typeof(Epsilon.Tests.Core.NestingLimitTests))]
    public void Expressions_as_deep_as_the_parser_allows_are_written_on_a_small_stack(string input)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { Vector.Create(P(input)).ToCSharp("F", ["x"]); }
            catch (Exception ex) { error = ex; }
        }, maxStackSize: 1024 * 1024);

        thread.Start();
        thread.Join();
        Assert.Null(error);
    }
}
