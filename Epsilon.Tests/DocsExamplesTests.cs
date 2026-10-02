using Xunit;
using Epsilon.Core;

namespace Epsilon.Tests.Core;

// Every example in docs/limitations.md, with the result it shows. When a limitation is lifted,
// one of these fails: update or remove the section in the docs together with the test.
public class LimitationsDocTests
{
    [Fact]
    public void No_expansion()
    {
        Assert.Equal("-x^2 + (x + 1)^2", ExprParser.Parse("(x + 1)^2 - x^2").Simplify().Print());
        Assert.Equal("(x + 1) * (x - 1)", ExprParser.Parse("(x + 1)*(x - 1)").Simplify().Print());
        Assert.False(ExprParser.Parse("(x - 2)^4").TryFactorReal("x").Success);
    }

    [Fact]
    public void Factoring_is_limited()
    {
        Assert.False(ExprParser.Parse("x^4 + 1").TryFactorReal("x").Success);
        Assert.False(ExprParser.Parse("x^3 + x + 1").TryFactorReal("x").Success);
        Assert.False(ExprParser.Parse("x^2 - y^2").TryFactorReal("x").Success);
    }

    [Fact]
    public void No_exact_function_values()
    {
        Assert.Equal("sin(π / 6)", ExprParser.Parse("sin(pi/6)").Simplify().Print());
        Assert.Equal("asin(1/2)", ExprParser.Parse("asin(1/2)").Simplify().Print());
        Assert.Equal("sqrt(8)", ExprParser.Parse("sqrt(8)").Simplify().Print());
    }

    [Fact]
    public void Few_identities()
    {
        Assert.Equal("1", ExprParser.Parse("sin(x)^2 + cos(x)^2").Simplify().Print());
        Assert.Equal("tan(x)", ExprParser.Parse("sin(x)/cos(x)").Simplify().Print());
        Assert.Equal("x", ExprParser.Parse("ln(exp(x))").Simplify().Print());

        Assert.Equal("2cos(x) * sin(x)", ExprParser.Parse("2sin(x)cos(x)").Simplify().Print());
        Assert.Equal("ln(x * y) - ln(x)", ExprParser.Parse("ln(x*y) - ln(x)").Simplify().Print());
    }

    [Fact]
    public void Equality_is_structural()
    {
        Assert.False(ExprParser.Parse("x*x").Equals(ExprParser.Parse("x^2")));
        Assert.True(ExprParser.Parse("x*x").Simplify().Equals(ExprParser.Parse("x^2")));
    }

    [Theory]
    [InlineData("floor(x)")]
    [InlineData("ceiling(x)")]
    [InlineData("round(x)")]
    [InlineData("sign(x)")]
    [InlineData("min(x, 1)")]
    [InlineData("max(x, 1)")]
    [InlineData("nthroot(2, x)")]
    public void Unsupported_derivatives(string input)
    {
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(input).Differentiate("x"));
    }

    [Fact]
    public void Abs_derivative()
    {
        Assert.Equal("x / abs(x)", ExprParser.Parse("abs(x)").Differentiate("x").Print());
    }

    [Theory]
    [InlineData("floor(x)")]
    [InlineData("ceiling(x)")]
    [InlineData("round(x)")]
    [InlineData("sign(x)")]
    [InlineData("min(x, 1)")]
    [InlineData("max(x, 1)")]
    public void Unsupported_complex_functions(string input)
    {
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(input).EvaluateComplex(new ComplexNumber(1.5, 1)));
    }

    [Fact]
    public void Real_roots_grid()
    {
        Assert.Equal(11, ExprParser.Parse("sin(1/x)").FindRealRoots(0.01, 1).Count);
        Assert.Equal(31, ExprParser.Parse("sin(1/x)").FindRealRoots(0.01, 1, 5000).Count);
        Assert.Equal([1.0], ExprParser.Parse("(x - 1)*(x - 1.0000001)").FindRealRoots(-10, 10));
    }

    [Fact]
    public void Number_as_right_hand_side_binds_to_the_range_overload()
    {
        Expr s = ExprParser.Parse("sin(x)");

        Assert.Equal([0.0], s.FindRealRoots(-1, 2, 3));
        Assert.Empty(s.FindRealRoots((Expr)(-1), 2, 3));
    }
}

// Additions to docs/limitations.md found while writing the guides.
public class LimitationsDocAdditionsTests
{
    [Fact]
    public void Underflow_is_reported_as_a_root()
    {
        Assert.Equal([-500000013.6409661], ExprParser.Parse("exp(x)").FindRealRoots());
        Assert.Equal(4, ExprParser.Parse("exp(-x^2)").FindRealRoots().Count);
    }

    [Fact]
    public void Variable_names_are_letters_only()
    {
        Assert.Throws<FormatException>(() => ExprParser.Parse("v0 + 1", "v0"));
    }
}

// Every example in the guides under docs/, one class per page, with the result it shows.
// If one of these fails, update the page together with the code.
public class GettingStartedDocTests
{
    [Fact]
    public void First_example()
    {
        Expr h = ExprParser.Parse("20t - 5t^2", "t");
        Assert.Equal("-5t^2 + 20t", h.Print());
        Assert.Equal(15, h.Evaluate(1));

        Expr v = h.Differentiate("t");
        Assert.Equal("-10t + 20", v.Print());
        Assert.Equal([2.0], v.FindRealRoots());
        Assert.Equal(20, h.Evaluate(2));

        Assert.Equal([0.0, 4.0], h.FindRealRoots(-10, 10));
        Assert.Equal("-5 * (t - 4) * t", h.TryFactorReal("t").Factored.Print());
        Assert.Equal("-5t^{2} + 20t", h.ToLatex());
    }

    [Fact]
    public void Building_in_code()
    {
        var t = new Variable("t");
        Expr height = 20 * t - 5 * t.Pow(2);

        Assert.Equal("-5t^2 + 20t", height.Print());
        Assert.Equal("-10t + 20", height.Differentiate("t").Print());

        Assert.Equal("t + t", (t + t).Print());
        Assert.Equal("2t", (t + t).Simplify().Print());
    }
}

public sealed class DocSigmoid(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1 / (1 + Math.Exp(-Argument.Evaluate(bindings)));

    protected override Expr DifferentiateCore(string variable) =>
        this * (1 - this) * DerivativeOf(Argument, variable);

    protected override Expr WithArgument(Expr argument) => new DocSigmoid(argument);

    public override string ToString() => $"sigmoid({Argument.Print()})";
}

public class ExpressionsDocTests
{
    private static readonly Variable X = new("x");
    private static readonly Variable Y = new("y");

    [Fact]
    public void Operators()
    {
        Expr e = 3 * X.Pow(2) - X / Y + 1;
        Assert.Equal("3x^2 - x / y + 1", e.Print());

        Assert.Equal("x + 0", (X + 0).Print());
        Assert.Equal("x", (X + 0).Simplify().Print());
    }

    [Fact]
    public void Numbers()
    {
        Assert.Equal("(1/10) * x", (0.1 * X).Print());
        Assert.Equal("(1/3) * x", (new Rational(1, 3) * X).Print());
        Assert.Throws<ArgumentException>(() => (Expr)double.NaN);
    }

    [Fact]
    public void Node_types()
    {
        Assert.Equal("sin(x) + π", (new Sin(X) + new Pi()).Print());
    }

    [Fact]
    public void Inspecting()
    {
        Expr g = ExprParser.Parse("x*y + sin(z)");
        Assert.Equal(["x", "y", "z"], g.GetVariables().Order());
        Assert.False(g.DependsOn("w"));

        Expr p = ExprParser.Parse("sin(x)^2");
        Assert.True(p is Power(Sin(Variable { Name: "x" }), Constant { Value.IsInteger: true } c) && c.Value == 2);
    }

    private static Expr SinToCos(Expr e)
    {
        Expr mapped = e.MapChildren(SinToCos);
        return mapped is Sin(var a) ? new Cos(a) : mapped;
    }

    [Fact]
    public void Transforming()
    {
        Assert.Equal("2cos(y) + cos(x)", SinToCos(ExprParser.Parse("sin(x) + 2sin(y)")).Print());
    }

    [Fact]
    public void Substitution()
    {
        Expr q = ExprParser.Parse("x^2 + 1");
        Assert.Equal("(y + 1)^2 + 1", q.Substitute("x", ExprParser.Parse("y + 1")).Print());
        Assert.Equal("10", q.Substitute("x", 3).Simplify().Print());
    }

    [Fact]
    public void Equality()
    {
        Assert.False((1 + X).Equals(X + 1));
        Assert.True((1 + X).Canonicalize().Equals((X + 1).Canonicalize()));
        Assert.True(ExprParser.Parse("1 + x").Equals(ExprParser.Parse("x + 1")));
    }

    [Fact]
    public void Custom_node()
    {
        var s = new DocSigmoid(2 * X);
        Assert.Equal(0.5, s.Evaluate(0));
        Assert.Equal("2 * (-sigmoid(2x) + 1) * sigmoid(2x)", s.Differentiate("x").Print());
    }
}

public class ParsingDocTests
{
    [Fact]
    public void Canonical_not_simplified()
    {
        Assert.Equal("x + 1", ExprParser.Parse("1 + x").Print());
        Assert.Equal("x + x", ExprParser.Parse("x + x").Print());
    }

    [Fact]
    public void Numbers_and_log()
    {
        Assert.Equal("1/100000", ExprParser.Parse("1e-5").Print());
        Assert.Equal("2500", ExprParser.Parse("2.5E3").Print());
        Assert.Equal("ln(8) / ln(2)", ExprParser.Parse("log(8, 2)").Print());
        Assert.Equal(3, ExprParser.Parse("log(8, 2)").Evaluate(0), precision: 12);
        Assert.Equal("abs(x - 1)", ExprParser.Parse("|x - 1|").Print());
    }

    [Fact]
    public void Precedence()
    {
        Assert.Equal(-9, ExprParser.Parse("-x^2").Evaluate(3));
        Assert.Equal(512, ExprParser.Parse("2^3^2").Evaluate(0));
        Assert.Equal("(x / 2) * y", ExprParser.Parse("x/2y").Print());
    }

    [Fact]
    public void Variables()
    {
        Assert.Equal("x * y", ExprParser.Parse("xy").Print());
        Assert.Equal("a * b", ExprParser.Parse("ab").Print());
        Assert.Equal("e * a * h * t * t", ExprParser.Parse("theta").Print());
        Assert.Equal("theta^2 + 2t", ExprParser.Parse("theta^2 + 2t", "theta", "t").Print());
        Assert.Equal("second + 1", ExprParser.Parse("second + 1", "second").Print());

        var ex = Assert.Throws<FormatException>(() => ExprParser.Parse("sen(x)", "x"));
        Assert.Equal("Unknown identifier 'sen' at position 0. Declared variables: x.", ex.Message);
    }

    [Theory]
    [InlineData("2 3", "Missing operator between numbers '2' and '3'.")]
    [InlineData("1,5", "Unexpected token ','.")]
    [InlineData("x # 2", "Unexpected character '#' at position 2.")]
    [InlineData("(x+1", "Expected closing ')'.")]
    [InlineData("x +", "Unexpected end of expression.")]
    [InlineData("sinx", "Expected '(' after function name 'sin'.")]
    [InlineData("sin(x, y)", "Function 'sin' takes exactly 1 argument.")]
    [InlineData("min(x)", "min requires exactly 2 arguments: min(a, b).")]
    public void Errors(string input, string message)
    {
        var ex = Assert.Throws<FormatException>(() => ExprParser.Parse(input));
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Errors_for_too_deep_input()
    {
        var nested = Assert.Throws<FormatException>(() =>
            ExprParser.Parse(new string('(', 257) + "x" + new string(')', 257)));
        Assert.Equal(
            "Expression is nested too deeply (at most 256 levels of parentheses, functions, signs and powers).",
            nested.Message);

        var chain = Assert.Throws<FormatException>(() =>
            ExprParser.Parse(string.Join(" + ", Enumerable.Repeat("x", 501))));
        Assert.Equal("Expression is too deep (at most 500 levels); split it into smaller parts.", chain.Message);
    }

    [Fact]
    public void Round_trip()
    {
        Expr e = ExprParser.Parse("x^(1/2) + (-2)^x - sin(x)/3");
        Assert.True(ExprParser.Parse(e.Print()).Equals(e));
    }
}

public class SimplificationDocTests
{
    [Theory]
    [InlineData("sqrt(x^2)", "abs(x)")]
    [InlineData("(x^2)^(1/2)", "(x^2)^(1/2)")]
    [InlineData("1/3 + 1/6", "1/2")]
    [InlineData("2^-2", "1/4")]
    [InlineData("sqrt(9/4)", "3/2")]
    [InlineData("nthroot(-8, 3)", "-2")]
    [InlineData("round(5/2)", "3")]
    [InlineData("2x + 3x", "5x")]
    [InlineData("x*y*x*y", "x^2 * y^2")]
    [InlineData("2*x*3*x", "6x^2")]
    [InlineData("x*(-y)*x", "-x^2 * y")]
    [InlineData("(x + 1) - 1", "x")]
    [InlineData("(2x)/(4x)", "1/2")]
    [InlineData("(x^2*y)/(x*y)", "x")]
    [InlineData("x^3/x", "x^2")]
    [InlineData("2sin(x)^2 + 2cos(x)^2 + 1", "3")]
    [InlineData("1 - sin(x)^2", "cos(x)^2")]
    [InlineData("sec(x)^2 - tan(x)^2", "1")]
    [InlineData("sin(x)/cos(x)", "tan(x)")]
    [InlineData("ln(exp(x))", "x")]
    [InlineData("exp(ln(x))", "x")]
    [InlineData("sqrt(x)^2", "x")]
    [InlineData("tan(x)*cot(x)", "1")]
    [InlineData("0/0", "0 / 0")]
    public void Generic(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).Simplify().Print());
    }

    [Theory]
    [InlineData("x/x", "1", "x / x")]
    [InlineData("x^3/x", "x^2", "x^3 / x")]
    [InlineData("0/x", "0", "0 / x")]
    [InlineData("sqrt(x)^2", "x", "sqrt(x)^2")]
    [InlineData("exp(ln(x))", "x", "exp(ln(x))")]
    [InlineData("tan(x)*cot(x)", "1", "cot(x) * tan(x)")]
    [InlineData("sin(x)/cos(x)", "tan(x)", "tan(x)")]
    public void Generic_vs_strict(string input, string generic, string strict)
    {
        Expr e = ExprParser.Parse(input);
        Assert.Equal(generic, e.Simplify().Print());
        Assert.Equal(strict, e.Simplify(SimplifyMode.Strict).Print());
    }

    [Fact]
    public void Strict_with_assumptions()
    {
        Assert.Equal("1", ExprParser.Parse("x/x").Simplify(Assumptions.None.AssumeNonZero("x"), SimplifyMode.Strict).Print());
    }
}

public class AssumptionsDocTests
{
    [Fact]
    public void Intro()
    {
        var a = Assumptions.None.AssumePositive("x");
        Assert.Equal("x", ExprParser.Parse("sqrt(x^2)").Simplify(a).Print());

        var b = Assumptions.None.AssumePositive("x").AssumeNegative("y").AssumeInteger("n");
        Assert.True(b.IsPositive("x") && b.IsNegative("y") && b.IsInteger("n"));
    }

    [Fact]
    public void Combining()
    {
        Assert.Equal(Signing.Positive, Assumptions.None.AssumeNonNegative("x").AssumeNonZero("x").SigningOf("x"));
        Assert.Equal(Signing.Zero, Assumptions.None.AssumeNonNegative("x").AssumeNonPositive("x").SigningOf("x"));
        Assert.True(Assumptions.None.AssumeNatural("n").IsPositive("n"));

        var ex = Assert.Throws<ArgumentException>(() => Assumptions.None.AssumePositive("x").AssumeNegative("x"));
        Assert.Equal("Variable 'x': Contradictory assumptions: Positive and Negative cannot both hold. (Parameter 'variable')", ex.Message);
    }

    [Fact]
    public void Unlocked_rules()
    {
        var neg = Assumptions.None.AssumeNegative("x");
        Assert.Equal("-x", ExprParser.Parse("abs(x)").Simplify(neg).Print());

        var pos = Assumptions.None.AssumePositive("x");
        Assert.Equal("1", ExprParser.Parse("sign(x)").Simplify(pos).Print());
        Assert.Equal("0", ExprParser.Parse("0^x").Simplify(pos).Print());
        Assert.Equal("x", ExprParser.Parse("(x^2)^(1/2)").Simplify(pos).Print());
        Assert.Equal("x", ExprParser.Parse("exp(ln(x))").Simplify(pos, SimplifyMode.Strict).Print());

        var xy = Assumptions.None.AssumePositive("x").AssumeNegative("y");
        Assert.Equal("-x * y", ExprParser.Parse("sqrt(x^2)*sqrt(y^2)").Simplify(xy).Print());
    }

    [Fact]
    public void Provability()
    {
        Assert.True(ExprParser.Parse("x^2 + 1").IsProvablyPositive(Assumptions.None));

        var both = Assumptions.None.AssumePositive("x").AssumePositive("y");
        Assert.True(ExprParser.Parse("x + y").IsProvablyPositive(both));
        Assert.False(ExprParser.Parse("x - y").IsProvablyPositive(both));

        Assert.Equal("x^2 + 1", ExprParser.Parse("abs(x^2 + 1)").Simplify().Print());
        Assert.Equal("1", ExprParser.Parse("sign(exp(x))").Simplify().Print());
    }
}

public class DifferentiationDocTests
{
    [Theory]
    [InlineData("sin(x^2)", "2cos(x^2) * x")]
    [InlineData("x/(x + 1)", "1 / (x + 1)^2")]
    [InlineData("x^x", "(ln(x) + 1) * x^x")]
    [InlineData("sqrt(x)", "1 / (2sqrt(x))")]
    [InlineData("log(x, 2)", "1 / (ln(2) * x)")]
    [InlineData("nthroot(x, 3)", "1 / (3nthroot(x, 3)^2)")]
    [InlineData("tan(x)", "1 / cos(x)^2")]
    [InlineData("atan(x)", "1 / (x^2 + 1)")]
    [InlineData("sinh(x)", "cosh(x)")]
    [InlineData("abs(x)", "x / abs(x)")]
    [InlineData("ln(x)", "1 / x")]
    [InlineData("x^2", "2x")]
    public void Derivatives(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).Differentiate("x").Print());
    }

    [Theory]
    [InlineData("2^x")]
    [InlineData("exp(x)")]
    [InlineData("cot(x)")]
    [InlineData("sec(x)")]
    [InlineData("csc(x)")]
    [InlineData("asin(x)")]
    [InlineData("acos(x)")]
    [InlineData("cosh(x)")]
    [InlineData("tanh(x)")]
    [InlineData("coth(x)")]
    [InlineData("sech(x)")]
    [InlineData("csch(x)")]
    [InlineData("asinh(x)")]
    [InlineData("acosh(x)")]
    [InlineData("atanh(x)")]
    public void Every_listed_function_is_supported(string input)
    {
        Expr f = ExprParser.Parse(input);
        Expr df = f.Differentiate("x");

        // Compare against a central difference at a point inside every domain.
        double h = 1e-6, x = 0.5;
        if (input.StartsWith("acosh")) x = 1.5;
        double numeric = (f.Evaluate(x + h) - f.Evaluate(x - h)) / (2 * h);
        Assert.Equal(numeric, df.Evaluate(x), precision: 5);
    }

    [Fact]
    public void Partial_and_higher()
    {
        Assert.Equal("6y^2 * x", ExprParser.Parse("x^2*y^3").Differentiate("x").Differentiate("y").Print());
        Assert.Equal("12x^2", ExprParser.Parse("x^4").Differentiate("x").Differentiate("x").Print());
        Assert.Equal("0", ExprParser.Parse("x^2").Differentiate("y").Print());
    }

    [Fact]
    public void Without_a_variable_name()
    {
        Assert.Equal("3x^2", ExprParser.Parse("x^3").Differentiate().Print());
        Assert.Equal("0", ExprParser.Parse("2pi").Differentiate().Print());

        var ex = Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("x*y").Differentiate());
        Assert.Equal("Expected exactly 1 variable, found 2: [x, y]. Use the explicit-variable overload for multivariable expressions.", ex.Message);
    }

    [Fact]
    public void Tangent_line()
    {
        Expr f = ExprParser.Parse("x^2", "x");
        Expr df = f.Differentiate("x");

        double a = 3;
        Expr tangent = f.Evaluate(a) + df.Evaluate(a) * (new Variable("x") - a);
        Assert.Equal("6 * (x - 3) + 9", tangent.Simplify().Print());
        Assert.Equal(15, tangent.Evaluate(4));
    }
}

public class FactoringDocTests
{
    [Theory]
    [InlineData("x^2 - 5x + 6", "(x - 2) * (x - 3)")]
    [InlineData("x^3 - x", "(x + 1) * (x - 1) * x")]
    [InlineData("x^3 - 3x^2 + 3x - 1", "(x - 1)^3")]
    [InlineData("2x^2 - x - 1", "2 * (x + 1/2) * (x - 1)")]
    [InlineData("0.5x^2 - 0.5", "(1/2) * (x + 1) * (x - 1)")]
    [InlineData("x^4 + x^2 + 1", "(x^2 - x + 1) * (x^2 + x + 1)")]
    [InlineData("x^4 - 5x^2 + 6", "(x + sqrt(2)) * (x + sqrt(3)) * (x - sqrt(2)) * (x - sqrt(3))")]
    [InlineData("x^2 - x - 1", "(x + (1/2) * sqrt(5) - 1/2) * (x - (1/2) * sqrt(5) - 1/2)")]
    [InlineData("x^4 - 1", "(x^2 + 1) * (x + 1) * (x - 1)")]
    public void Real(string input, string expected)
    {
        var (factored, success) = ExprParser.Parse(input).TryFactorReal("x");
        Assert.True(success);
        Assert.Equal(expected, factored.Print());
    }

    [Fact]
    public void Complex()
    {
        Assert.Equal("(x + 1) * (x + i) * (x - 1) * (x - i)", ExprParser.Parse("x^4 - 1").TryFactorComplex("x").Factored.Print());
    }

    [Theory]
    [InlineData("sin(x)")]
    [InlineData("5")]
    [InlineData("x^2 - a")]
    [InlineData("(x - 2)^4")]
    [InlineData("x^3 - 2")]
    public void Failures(string input)
    {
        Expr e = ExprParser.Parse(input);
        var (factored, success) = e.TryFactorReal("x");
        Assert.False(success);
        Assert.Same(e, factored);
    }

    [Fact]
    public void Failure_example()
    {
        var (factored, success) = ExprParser.Parse("x^3 - 2").TryFactorReal("x");
        Assert.False(success);
        Assert.Equal("x^3 - 2", factored.Print());
    }
}

public class RootFindingDocTests
{
    [Fact]
    public void Real_roots()
    {
        Assert.Equal([-1.4142135623730951, 1.414213562373095], ExprParser.Parse("x^2 - 2").FindRealRoots());
        Assert.Equal([1.414213562373095], ExprParser.Parse("x^2 - 2").FindRealRoots(0, 10));

        Assert.Equal([0.0], ExprParser.Parse("x^2").FindRealRoots());
        Assert.Equal([0.0], ExprParser.Parse("sqrt(x)").FindRealRoots());
        Assert.Equal([1.0], ExprParser.Parse("ln(x)").FindRealRoots());
        Assert.Equal([0.0], ExprParser.Parse("tan(x)").FindRealRoots(-2, 2));
        Assert.Empty(ExprParser.Parse("x^2/x").FindRealRoots(-1, 1));
        Assert.Empty(ExprParser.Parse("1/x").FindRealRoots());
    }

    [Fact]
    public void Infinite_range_periodic()
    {
        Assert.Equal(7, ExprParser.Parse("sin(x)").FindRealRoots().Count);
    }

    [Fact]
    public void Equations()
    {
        Assert.Equal([0.0, 2.0], ExprParser.Parse("x^2").FindRealRoots(ExprParser.Parse("2x"), -10, 10));
        Assert.Equal([0.7390851332151607], ExprParser.Parse("cos(x)").FindRealRoots(ExprParser.Parse("x")));
    }

    [Fact]
    public void Several_variables()
    {
        var a = new Dictionary<string, double> { ["a"] = 9 };
        Assert.Equal([-3.0, 3.0], ExprParser.Parse("x^2 - a").FindRealRoots("x", a));
    }

    [Fact]
    public void Complex_roots()
    {
        var cube = ExprParser.Parse("x^3 - 1").FindComplexRoots(-2, 2, -2, 2).OrderBy(z => z.Imaginary).ToList();
        Assert.Equal(3, cube.Count);
        Assert.Equal(-0.5, cube[0].Real, precision: 9);
        Assert.Equal(-Math.Sqrt(3) / 2, cube[0].Imaginary, precision: 9);
        Assert.Equal(1, cube[1].Real, precision: 9);
        Assert.Equal(Math.Sqrt(3) / 2, cube[2].Imaginary, precision: 9);

        var exp = ExprParser.Parse("exp(x)").FindComplexRoots(ExprParser.Parse("1"), -1, 1, -7, 7).OrderBy(z => z.Imaginary).ToList();
        Assert.Equal(3, exp.Count);
        Assert.Equal(-2 * Math.PI, exp[0].Imaginary, precision: 9);
        Assert.Equal(0, exp[1].Imaginary, precision: 9);
        Assert.Equal(2 * Math.PI, exp[2].Imaginary, precision: 9);
    }

    [Fact]
    public void Complex_roots_multiple_and_close()
    {
        ComplexNumber doubleRoot = Assert.Single(ExprParser.Parse("x^2 - 2x + 1").FindComplexRoots(-2, 2, -2, 2));
        Assert.True((doubleRoot - 1).Magnitude < 1e-12);

        var close = ExprParser.Parse("(x - 1)*(x - 1.001)").FindComplexRoots(-2, 2, -2, 2).OrderBy(z => z.Real).ToList();
        Assert.Equal(2, close.Count);
        Assert.Equal(1, close[0].Real, precision: 12);
        Assert.Equal(1.001, close[1].Real, precision: 12);

        ComplexNumber tripleRoot = Assert.Single(ExprParser.Parse("x^3 - 3x^2 + 3x - 1").FindComplexRoots(-2, 2, -2, 2));
        Assert.True((tripleRoot - 1).Magnitude < 1e-8);
    }

    [Fact]
    public void Newton()
    {
        Assert.Equal((1.4142135623746899, true), ExprParser.Parse("x^2 - 2").TryFindRoot(1));
        Assert.Equal((null, false), ExprParser.Parse("x^2 + 1").TryFindRoot(1));

        var (root, found) = ExprParser.Parse("x^2 + 1").TryFindComplexRoot(new ComplexNumber(1, 1));
        Assert.True(found);
        Assert.Equal(1, root!.Value.Imaginary, precision: 9);
        Assert.Equal(0, root.Value.Real, precision: 9);
    }

    [Fact]
    public void Errors()
    {
        Assert.Throws<ArgumentException>(() => ExprParser.Parse("x").FindRealRoots(1, 0));
        Assert.Throws<ArgumentException>(() => ExprParser.Parse("x").FindRealRoots(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExprParser.Parse("x").FindRealRoots(0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExprParser.Parse("x").FindComplexRoots(0, 1, 0, 1, 0));
        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("x*y").FindRealRoots());
    }
}

public class EvaluationDocTests
{
    [Fact]
    public void Real_values()
    {
        Expr f = ExprParser.Parse("x*y");
        Assert.Equal(10, f.Evaluate(new Dictionary<string, double> { ["x"] = 2, ["y"] = 5 }));
        Assert.Equal(10, f.Evaluate(("x", 2), ("y", 5)));
        Assert.Equal(9, ExprParser.Parse("x^2").Evaluate(3));
        Assert.Equal(6.283185307179586, ExprParser.Parse("2pi").Evaluate(0));

        var ex = Assert.Throws<ArgumentException>(() => f.Evaluate(("x", 2)));
        Assert.Equal("No binding provided for variable 'y'.", ex.Message);
        Assert.Throws<InvalidOperationException>(() => f.Evaluate(2));
    }

    [Fact]
    public void Undefined_points()
    {
        Assert.Equal(double.NaN, ExprParser.Parse("sqrt(x)").Evaluate(-1));
        Assert.Equal(double.NaN, ExprParser.Parse("asin(x)").Evaluate(2));
        Assert.Equal(double.NegativeInfinity, ExprParser.Parse("ln(x)").Evaluate(0));
        Assert.Equal(double.PositiveInfinity, ExprParser.Parse("1/x").Evaluate(0));
        Assert.Equal(double.NaN, ExprParser.Parse("x/x").Evaluate(0));
        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("i").Evaluate(0));
    }

    [Fact]
    public void Conventions()
    {
        Assert.Equal(double.NaN, ExprParser.Parse("x^(1/3)").Evaluate(-8));
        Assert.Equal(-2, ExprParser.Parse("nthroot(x, 3)").Evaluate(-8));
        Assert.Equal(-3, ExprParser.Parse("round(x)").Evaluate(-2.5));
        Assert.Equal(3, ExprParser.Parse("round(x)").Evaluate(2.5));
        Assert.Equal(-3, ExprParser.Parse("floor(x)").Evaluate(-2.5));
        Assert.Equal(-2, ExprParser.Parse("ceiling(x)").Evaluate(-2.5));
    }

    private static void Near(double re, double im, ComplexNumber z)
    {
        Assert.Equal(re, z.Real, precision: 12);
        Assert.Equal(im, z.Imaginary, precision: 12);
    }

    [Fact]
    public void Complex_values()
    {
        Near(0, 2, ExprParser.Parse("sqrt(x)").EvaluateComplex(-4));
        Near(0, Math.PI, ExprParser.Parse("ln(x)").EvaluateComplex(-1));
        Near(1.5707963267948966, -1.3169578969248166, ExprParser.Parse("asin(x)").EvaluateComplex(2));
        Near(-1, 0, ExprParser.Parse("e^(i*pi)").EvaluateComplex(0));
        Near(5, 0, ExprParser.Parse("abs(x)").EvaluateComplex(new ComplexNumber(3, 4)));
        Near(-2, 0, ExprParser.Parse("nthroot(x, 3)").EvaluateComplex(-8));

        var i = ComplexNumber.ImaginaryUnit;
        Near(-1, 0, ExprParser.Parse("x*y").EvaluateComplex(("x", i), ("y", i)));
    }
}

public class OutputDocTests
{
    [Theory]
    [InlineData("x^2/2 + sin(x)", "x^2 / 2 + sin(x)")]
    [InlineData("sqrt(x) + |y|", "abs(y) + sqrt(x)")]
    [InlineData("2pi", "2 * π")]
    [InlineData("2cos(x)*y", "2cos(x) * y")]
    [InlineData("3 * 2^x", "3 * 2^x")]
    [InlineData("(-2)^x", "(-2)^x")]
    [InlineData("x^(1/2)", "x^(1 / 2)")]
    public void Print(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).Print());
    }

    [Fact]
    public void Print_orders_terms_by_degree()
    {
        Assert.Equal("3x^2 - 4x + 1", ExprParser.Parse("1 - 4x + 3x^2").Simplify().Print());
    }

    [Theory]
    [InlineData("x^2/2 + sin(x)", @"\frac{x^{2}}{2} + \sin\left(x\right)")]
    [InlineData("sqrt(x + 1)", @"\sqrt{x + 1}")]
    [InlineData("nthroot(x, 3)", @"\sqrt[3]{x}")]
    [InlineData("|x|", @"\left|x\right|")]
    [InlineData("exp(x)", "e^{x}")]
    [InlineData("asin(x)", @"\arcsin\left(x\right)")]
    [InlineData("(-2)^x", @"\left(-2\right)^{x}")]
    [InlineData("round(x)", @"\operatorname{round}\left(x\right)")]
    [InlineData("sign(x)", @"\operatorname{sgn}\left(x\right)")]
    [InlineData("3x^2 - 4x + 1", "3x^{2} - 4x + 1")]
    public void Latex(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).ToLatex());
    }

    [Fact]
    public void To_string()
    {
        Assert.Equal("(((3 * (x ^ 2)) - (4 * x)) + 1)", ExprParser.Parse("3x^2 - 4x + 1").ToString());
    }
}

public class NumbersDocTests
{
    [Fact]
    public void Rationals()
    {
        Assert.Equal("1/2", new Rational(2, 4).ToString());
        Assert.Equal("-1/2", new Rational(1, -2).ToString());
        Assert.Equal("1/2", (new Rational(1, 3) + new Rational(1, 6)).ToString());
        Assert.Equal("9/4", new Rational(2, 3).Pow(-2).ToString());
        Assert.Equal("1000000000000000000000000000000/3", new Rational(System.Numerics.BigInteger.Pow(10, 30), 3).ToString());
        Assert.Equal("1267650600228229401496703205376", ExprParser.Parse("2^100").Simplify().Print());
        Assert.Throws<DivideByZeroException>(() => new Rational(1, 0));
    }

    [Fact]
    public void From_double_and_text()
    {
        Assert.Equal(new Rational(1, 10), Rational.FromDouble(0.1));
        Assert.Equal(new Rational(3, 2000), Rational.FromDecimalString("1.5E-3"));
        Assert.Equal("333333333333333/1000000000000000", new Constant(1.0 / 3).Value.ToString());
        Assert.Throws<ArgumentException>(() => Rational.FromDouble(double.NaN));
    }

    [Fact]
    public void Complex_numbers()
    {
        var z = new ComplexNumber(3, 4);
        Assert.Equal(5, z.Magnitude);
        Assert.Equal("3 - 4i", z.Conjugate.ToString());
        Assert.Equal("-5 + 10i", (new ComplexNumber(1, 2) * new ComplexNumber(3, 4)).ToString());

        ComplexNumber s = ComplexNumber.Sqrt(-4);
        Assert.Equal(0, s.Real, precision: 12);
        Assert.Equal(2, s.Imaginary, precision: 12);

        ComplexNumber p = ComplexNumber.FromPolar(2, Math.PI / 2);
        Assert.Equal(0, p.Real, precision: 12);
        Assert.Equal(2, p.Imaginary, precision: 12);
    }
}

// docs/api-reference.md must list every public type, member and enum value. A new public
// member fails this test until it is documented there.
public class ApiReferenceDocTests
{
    private static readonly HashSet<string> DocumentedNames = LoadDocumentedNames();

    // Every identifier that occurs inside a `code span` of the page.
    private static HashSet<string> LoadDocumentedNames()
    {
        string dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "docs", "api-reference.md")))
            dir = Path.GetDirectoryName(dir) ?? throw new FileNotFoundException("docs/api-reference.md not found");

        string text = File.ReadAllText(Path.Combine(dir, "docs", "api-reference.md"));
        return System.Text.RegularExpressions.Regex.Matches(text, "`([^`]+)`")
            .SelectMany(m => System.Text.RegularExpressions.Regex.Matches(m.Groups[1].Value, @"[A-Za-z_]\w*"))
            .Select(m => m.Value)
            .ToHashSet();
    }

    private static IEnumerable<string> PublicNames()
    {
        const System.Reflection.BindingFlags Declared =
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.DeclaredOnly;

        foreach (Type type in typeof(Expr).Assembly.GetExportedTypes())
        {
            yield return type.Name;

            if (type.IsEnum)
            {
                foreach (string value in Enum.GetNames(type))
                    yield return $"{type.Name}.{value}";
                continue;
            }

            foreach (var member in type.GetMembers(Declared))
            {
                bool visible = member switch
                {
                    System.Reflection.MethodInfo m => (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly) && !m.IsSpecialName
                        && m.GetBaseDefinition() == m,   // overrides are documented on the base
                    System.Reflection.PropertyInfo p => p.GetMethod is { } g && (g.IsPublic || g.IsFamily) && g.GetBaseDefinition() == g,
                    System.Reflection.FieldInfo f => f.IsPublic || f.IsFamily,
                    _ => false
                };
                if (visible)
                    yield return $"{type.Name}.{member.Name}";
            }
        }
    }

    [Fact]
    public void Every_public_type_and_member_is_listed()
    {
        var missing = PublicNames()
            .Where(name => !DocumentedNames.Contains(name.Split('.').Last()))
            .Distinct()
            .Order()
            .ToList();

        Assert.Empty(missing);
    }
}

// The example tests in CONTRIBUTING.md ("Worked example: adding a function").
public class ContributingDocTests
{
    private static readonly Variable X = new("x");

    [Fact]
    public void Evaluates_over_the_reals_and_complex_numbers()
    {
        Assert.Equal(Math.Sinh(0.7), ExprParser.Parse("sinh(x)").Evaluate(0.7));

        // sinh(i) = i * sin(1)
        ComplexNumber z = ExprParser.Parse("sinh(x)").EvaluateComplex(ComplexNumber.ImaginaryUnit);
        Assert.Equal(0, z.Real, precision: 12);
        Assert.Equal(Math.Sin(1), z.Imaginary, precision: 12);
    }

    [Fact]
    public void Differentiates_with_the_chain_rule()
    {
        Assert.Equal("cosh(x)", ExprParser.Parse("sinh(x)").Differentiate("x").Print());
        Assert.Equal("2cosh(2x)", ExprParser.Parse("sinh(2x)").Differentiate("x").Print());
    }

    [Fact]
    public void Prints_and_parses_back()
    {
        // Not "asinh(x)", which would parse as a different function.
        Expr e = new Multiply(new Variable("a"), new Sinh(X));
        Assert.Equal("a * sinh(x)", e.Print());

        // Parse returns the canonical tree.
        Assert.Equal(e.Canonicalize(), ExprParser.Parse(e.Print()));
        Assert.Equal(@"\sinh\left(x\right)", new Sinh(X).ToLatex());
    }

    [Fact]
    public void Sinh_of_zero_is_not_folded_yet()
    {
        // CONTRIBUTING suggests sinh(0) = 0 as a first rule; it isn't in the core yet.
        Assert.Equal("sinh(0)", ExprParser.Parse("sinh(0)").Simplify().Print());
    }
}
