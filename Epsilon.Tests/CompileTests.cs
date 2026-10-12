using Xunit;
using Epsilon.Core;

namespace Epsilon.Tests.Core;

public class CompileTests
{
    // Every node type of the core, most of them at the edges of their domain below.
    private static readonly string[] Inputs =
    [
        "x + 2", "x - 3", "2.5 * x", "x / 3", "-x", "0.1 * x", "pi * x", "e * x",
        "x^2", "x^(1/3)", "2^x", "e^x", "exp(x)", "ln(x)", "log(x, 2)",
        "sqrt(x)", "nthroot(x, 3)", "nthroot(x, 2)", "nthroot(27, x)",
        "abs(x)", "floor(x)", "ceiling(x)", "round(x)", "sign(x)", "min(x, 1)", "max(x, 1)",
        "sin(x)", "cos(x)", "tan(x)", "cot(x)", "sec(x)", "csc(x)", "asin(x)", "acos(x)", "atan(x)",
        "sinh(x)", "cosh(x)", "tanh(x)", "coth(x)", "sech(x)", "csch(x)", "asinh(x)", "acosh(x)", "atanh(x)",
        "sin(x)^2 + cos(x)^2 * e^(-x/10) - x / (1 + x^2) + sin(x) * round(sin(x))",
    ];

    public static TheoryData<string> Corpus => new(Inputs);

    private static readonly double[] Points =
    [
        double.NegativeInfinity, -1e300, -27, -10, -2.5, -1, -0.5, -1e-300, -0.0,
        0, 1e-300, 0.5, 1, 1.5, 2.5, 3, 10, 27, 1e300, double.PositiveInfinity, double.NaN,
    ];

    // The same bits, or both NaN, or the same exception (sign(NaN) throws in Evaluate too).
    private static void AssertSameAsEvaluate(Expr expr, Func<double, double> compiled, double x)
    {
        double expected;
        try
        {
            expected = expr.Evaluate(x);
        }
        catch (Exception ex)
        {
            Assert.Throws(ex.GetType(), () => compiled(x));
            return;
        }

        double actual = compiled(x);
        bool same = double.IsNaN(expected)
            ? double.IsNaN(actual)
            : BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(actual);
        Assert.True(same, $"{expr.Print()} at x = {x}: Evaluate gives {expected}, Compile gives {actual}.");
    }

    [Theory]
    [MemberData(nameof(Corpus))]
    public void Returns_exactly_what_Evaluate_returns(string input)
    {
        Expr expr = ExprParser.Parse(input);
        Func<double, double> compiled = expr.Compile();

        foreach (double x in Points)
            AssertSameAsEvaluate(expr, compiled, x);
    }

    [Fact]
    public void Corpus_covers_every_node_type_of_the_core()
    {
        var used = new HashSet<Type>();
        void Collect(Expr expr)
        {
            used.Add(expr.GetType());
            foreach (Expr child in expr.Children)
                Collect(child);
        }
        foreach (string input in Inputs)
            Collect(ExprParser.Parse(input));

        var missing = typeof(Expr).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Expr)) && !type.IsAbstract && type != typeof(ImaginaryUnit))
            .Where(type => !used.Contains(type))
            .Select(type => type.Name);
        Assert.Empty(missing);
    }

    [Fact]
    public void Several_variables_in_the_given_order()
    {
        Expr f = ExprParser.Parse("x - 2y + 3z");

        Assert.Equal(12, ExprParser.Parse("x * y").Compile("x", "y")(3, 4));
        Assert.Equal(f.Evaluate(("x", 1), ("y", 2), ("z", 3)), f.Compile("x", "y", "z")(1, 2, 3));
        Assert.Equal(f.Evaluate(("x", 1), ("y", 2), ("z", 3)), f.Compile("z", "y", "x")(3, 2, 1));
        Assert.Equal(1.25, ExprParser.Parse("a * b - c / d").Compile(["a", "b", "c", "d"])([1, 2, 3, 4]));
        Assert.Equal(-1, ExprParser.Parse("x - y").Compile("y", "x")(2, 1));
    }

    [Fact]
    public void Unused_variables_and_arguments_are_ignored()
    {
        Assert.Equal(9, ExprParser.Parse("x^2").Compile("x", "y")(3, 99));
        Assert.Equal(9, ExprParser.Parse("t^2").Compile()(3));
        Assert.Equal(2 * Math.PI, ExprParser.Parse("2pi").Compile()(123));
        Assert.Equal(7, ExprParser.Parse("7").Compile([])([]));
    }

    [Fact]
    public void Errors_are_found_when_compiling()
    {
        Expr xy = ExprParser.Parse("x * y");

        var several = Assert.Throws<InvalidOperationException>(() => xy.Compile());
        Assert.Equal(Assert.Throws<InvalidOperationException>(() => xy.Evaluate(1)).Message, several.Message);

        var missing = Assert.Throws<ArgumentException>(() => xy.Compile("x"));
        Assert.Equal("The expression has the variable 'y', which is not in the list of variables [x].", missing.Message);
        Assert.Throws<ArgumentException>(() => xy.Compile(["y", "z"]));
        Assert.Throws<ArgumentException>(() => Expr.Compile([xy, ExprParser.Parse("z")], ["x", "y"]));

        var twice = Assert.Throws<ArgumentException>(() => xy.Compile("x", "y", "x"));
        Assert.Equal("Variable 'x' is listed twice.", twice.Message);

        Expr complex = ExprParser.Parse("x + 2i");
        var imaginary = Assert.Throws<InvalidOperationException>(() => complex.Compile());
        Assert.Equal(Assert.Throws<InvalidOperationException>(() => complex.Evaluate(1)).Message, imaginary.Message);

        Assert.Throws<ArgumentNullException>(() => xy.Compile((IReadOnlyList<string>)null!));
        Assert.Throws<ArgumentNullException>(() => xy.Compile("x", null!));
        Assert.Throws<ArgumentNullException>(() => Expr.Compile([xy, null!], ["x", "y"]));
    }

    [Fact]
    public void Array_lengths_are_checked_at_each_call()
    {
        Func<double[], double> f = ExprParser.Parse("x + y").Compile(["x", "y"]);
        Action<double[], double[]> both = Expr.Compile([ExprParser.Parse("x + y"), ExprParser.Parse("x - y")], ["x", "y"]);

        var tooFew = Assert.Throws<ArgumentException>(() => f([1]));
        Assert.StartsWith("Expected 2 values, one per variable, got 1.", tooFew.Message);
        Assert.Throws<ArgumentException>(() => f([1, 2, 3]));
        Assert.Throws<ArgumentNullException>(() => f(null!));

        var room = Assert.Throws<ArgumentException>(() => both([1, 2], new double[3]));
        Assert.StartsWith("Expected an array of 2 results, one per expression, got 3.", room.Message);
        Assert.Throws<ArgumentNullException>(() => both([1, 2], null!));
    }

    [Fact]
    public void Several_expressions_at_once()
    {
        Expr f = ExprParser.Parse("x^2 * y + sin(x * y)");
        Expr[] gradient = [f.Differentiate("x"), f.Differentiate("y")];
        Action<double[], double[]> compiled = Expr.Compile(gradient, ["x", "y"]);

        var results = new double[2];
        compiled([0.7, -1.3], results);

        Assert.Equal(gradient[0].Evaluate(("x", 0.7), ("y", -1.3)), results[0]);
        Assert.Equal(gradient[1].Evaluate(("x", 0.7), ("y", -1.3)), results[1]);

        // All values are read before the first write: swap in place.
        double[] point = [1, 2];
        Expr.Compile([new Variable("y"), new Variable("x")], ["x", "y"])(point, point);
        Assert.Equal([2.0, 1.0], point);

        Expr.Compile([], ["x"])([5], []);
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

    [Fact]
    public void Shared_subexpressions_are_computed_once()
    {
        int[] calls = [0];
        Expr g = new Counted(new Variable("x"), calls);
        Expr f = g + g * new Sin(g);

        Assert.Equal(f.Evaluate(1.5), f.Compile()(1.5));
        calls[0] = 0;
        f.Compile()(1.5);
        Assert.Equal(1, calls[0]);

        calls[0] = 0;
        Expr.Compile([g + 1, g * 2, new Counted(new Variable("y"), calls)], ["x", "y"])([1, 2], new double[3]);
        Assert.Equal(2, calls[0]);
    }

    [Fact]
    public void Nodes_from_outside_the_core_use_their_own_Evaluate()
    {
        Expr f = new Epsilon.Tests.Other.Sin(new Variable("x")) * new Variable("y") + 1;
        Func<double, double, double> compiled = f.Compile("x", "y");

        Assert.Equal(f.Evaluate(("x", 0.3), ("y", 4)), compiled(0.3, 4));
        Assert.Throws<ArgumentException>(() => new Epsilon.Tests.Other.Sin(new Variable("z")).Compile("x"));
    }

    [Fact]
    public void The_delegate_works_from_many_threads()
    {
        Expr f = ExprParser.Parse("sin(x)^2 + cos(x)^2 * x");
        Func<double, double> compiled = f.Compile();

        Parallel.For(0, 1000, i => Assert.Equal(f.Evaluate(i * 0.01), compiled(i * 0.01)));
    }
}
