using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class EquationTests
{
    private static SolutionSet Solved(string equation, string variable = "x", Assumptions? assumptions = null) =>
        Equation.Parse(equation).Solve(variable, assumptions);

    private static string Printed(string equation, string variable = "x", Assumptions? assumptions = null) =>
        Solved(equation, variable, assumptions).Print();

    [Theory]
    [InlineData("2x + 1 = 7", "{3}")]
    [InlineData("x + 1 = 3 - x", "{1}")]
    [InlineData("2(x - 1) = 3(x + 1)", "{-5}")]
    [InlineData("x/2 + 1/3 = 0", "{-2/3}")]
    [InlineData("(pi - 3)*x = 1", "{1 / (π - 3)}")]
    [InlineData("x + a = 0", "{-a}")]
    public void Linear_equations_have_one_solution(string equation, string expected)
    {
        SolutionSet solutions = Solved(equation);

        Assert.Equal(expected, solutions.Print());
        Assert.True(solutions.IsComplete);
        Assert.False(solutions.IsEmpty);
        Assert.Empty(solutions.Solutions[0].Conditions);
    }

    [Fact]
    public void Every_value_or_none()
    {
        Assert.True(Solved("x + 1 = 1 + x").IsEveryValue);
        Assert.Equal("(-inf, inf)", Printed("x + 1 = 1 + x"));
        Assert.Equal("(-inf, inf)", Printed("1 = 1"));

        Assert.True(Solved("x = x + 1").IsEmpty);
        Assert.Equal("{}", Printed("x = x + 1"));
        Assert.Equal("{}", Printed("1 = 2"));
    }

    // Without the unknown, a = b holds for every x or for none, as a and b decide.
    [Fact]
    public void An_equation_without_the_unknown_holds_under_a_condition()
    {
        SolutionSet solutions = Solved("a = b");

        Assert.True(solutions.IsEveryValue);
        Assert.Equal("(-inf, inf) if a = b", solutions.Print());
        Assert.Equal(Comparison.Equal, solutions.Conditions[0].Comparison);
    }

    // x/x = 1 doesn't hold at x = 0, so it isn't taken for 1 = 1.
    [Fact]
    public void Nothing_enlarges_the_domain()
    {
        Assert.Equal("(-inf, inf) if x != 0", Printed("x/x = 1"));
    }

    [Fact]
    public void What_isnt_solved_is_listed()
    {
        SolutionSet solutions = Solved("x = cos(x)");

        Assert.False(solutions.IsComplete);
        Assert.False(solutions.IsEmpty);
        Assert.Equal("x - cos(x)", Assert.Single(solutions.Unsolved).Print());
        Assert.Equal("{...}", solutions.Print());
        Assert.Equal("\\left\\{\\ldots\\right\\}", solutions.ToLatex());
    }

    [Fact]
    public void The_sign_assumed_for_the_unknown_selects_the_solutions()
    {
        Assumptions positive = Assumptions.None.AssumePositive("x");

        Assert.Equal("{2}", Printed("x = 2", assumptions: positive));
        Assert.Equal("{}", Printed("x = -2", assumptions: positive));
        Assert.Equal("{}", Printed("x = 0", assumptions: Assumptions.None.AssumeNonZero("x")));
        Assert.Equal("{-a} if a < 0", Printed("x + a = 0", assumptions: positive));
        Assert.Equal("{}", Printed("x + a = 0", assumptions: positive.AssumePositive("a")));
    }

    [Fact]
    public void Prints_conditions_once_when_the_solutions_share_them()
    {
        var a = new Variable("a");
        Condition nonzero = new(a, Comparison.NotEqual, 0);
        Condition nonnegative = new(a, Comparison.GreaterOrEqual, 0);

        SolutionSet shared = SolutionSet.Of("x", [new(-a, [nonzero]), new(a, [nonzero])], []);
        Assert.Equal("{-a, a} if a != 0", shared.Print());
        Assert.Equal("\\left\\{-a, a\\right\\} \\text{ if } a \\neq 0", shared.ToLatex());

        SolutionSet separate = SolutionSet.Of("x", [new(1, []), new(a, [nonzero, nonnegative])], []);
        Assert.Equal("{1, a if a != 0 and a >= 0}", separate.Print());
    }

    [Fact]
    public void Numbers_are_ordered_and_listed_once()
    {
        SolutionSet solutions = SolutionSet.Of("x", [new(3, []), new(new Sqrt(2), []), new(-1, []), new(3, [])], []);

        Assert.Equal("{-1, sqrt(2), 3}", solutions.Print());
    }

    [Fact]
    public void Expressions_solve_as_equations()
    {
        Expr expr = ExprParser.Parse("2x - 6");

        Assert.Equal("{3}", expr.Solve().Print());
        Assert.Equal("{3}", expr.Solve("x").Print());
        Assert.Equal("{3}", ExprParser.Parse("x").Solve(3).Print());
        Assert.Equal("{b - a}", ExprParser.Parse("x + a").Solve(ExprParser.Parse("b"), "x").Print());
    }

    [Fact]
    public void The_unknown_must_be_named_with_more_than_one_variable()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Equation.Parse("x + a = 0").Solve());
        Assert.Equal("Expected exactly 1 variable, found 2: [a, x]. Name the unknown with the overload that takes it.", ex.Message);

        Assert.Equal("{}", Equation.Parse("1 = 2").Solve().Print());
    }

    [Fact]
    public void Parses_and_prints_equations()
    {
        Equation equation = Equation.Parse("sin(x) = 1/2");
        Assert.Equal("sin(x) = 1 / 2", equation.Print());
        Assert.Equal("\\sin\\left(x\\right) = \\frac{1}{2}", equation.ToLatex());

        Assert.Equal("{1}", Equation.Parse("theta^2 + 2theta = theta^2 + 2", "theta").Solve().Print());
    }

    [Theory]
    [InlineData("x + 1", "An equation needs '=' between its two sides.")]
    [InlineData("x = 1 = 2", "An equation has one '=', but there is another at position 6.")]
    [InlineData("x = 2 + $", "Unexpected character '$' at position 8.")]
    public void Malformed_equations_throw_with_positions_in_the_whole_input(string input, string message)
    {
        var ex = Assert.Throws<FormatException>(() => Equation.Parse(input));
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() => Equation.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => new Equation(null!, 0));
        Assert.Throws<ArgumentNullException>(() => Equation.Parse("x = 1").Solve(null!));
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Solve("x"));
    }
}

public class PolynomialCoefficientTests
{
    private static string? Coefficients(string expr, int maxDegree = 4) =>
        Polynomials.Coefficients(ExprParser.Parse(expr), "x", maxDegree) is { } coefficients
            ? string.Join("; ", coefficients.Select(coefficient => coefficient.Print()))
            : null;

    [Theory]
    [InlineData("(x + 1)^2", "1; 2; 1")]
    [InlineData("a*x^2 + b", "b; 0; a")]
    [InlineData("x/2 - 3", "-3; 1/2")]
    [InlineData("(a + x)*(b - x)", "a * b; b - a; -1")]
    [InlineData("x - x", "")]
    public void Of_polynomials(string expr, string expected) => Assert.Equal(expected, Coefficients(expr));

    [Theory]
    [InlineData("x*sin(x)")]
    [InlineData("1/x")]
    [InlineData("x/(x + 1)")]
    [InlineData("sqrt(x)")]
    [InlineData("x^5")]
    public void None_for_other_expressions_or_higher_degrees(string expr) => Assert.Null(Coefficients(expr));
}
