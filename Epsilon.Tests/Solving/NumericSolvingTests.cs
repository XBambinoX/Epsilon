using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class NumericSolvingTests
{
    internal static void AssertSatisfies(Equation equation, ComplexNumber value)
    {
        var at = new Dictionary<string, ComplexNumber> { ["x"] = value };
        double residual = (equation.Left - equation.Right).EvaluateComplex(at).Magnitude;
        Assert.True(residual < 1e-9, $"{value}: |left - right| = {residual}");
    }

    [Theory]
    [InlineData("x^5 - x - 1 = 0", "{1.1673039782614187}")]
    [InlineData("x^10 - 2x^6 - 2x^5 + x^2 + 2x + 1 = 0", "{1.1673039782614187}")]
    [InlineData("x^4 - 2x^3 - x + 3 = 0", "{1.3751255270709886, 1.7879331938447123}")]
    [InlineData("pi*x^3 + x - 1 = 0", "{0.530627058457308}")]
    [InlineData("x^2 = 2", "{-1.4142135623730951, 1.4142135623730951}")]
    [InlineData("x^3 - 3x + 1 = 0", "{-1.8793852415718169, 0.3472963553338607, 1.532088886237956}")]
    [InlineData("x^2 + 1 = 0", "{}")]
    public void Real_roots_as_numbers(string equation, string expected) =>
        Assert.Equal(expected, Equation.Parse(equation).SolveNumerically().Print());

    [Fact]
    public void Complex_roots_as_numbers()
    {
        NumericSolutionSet<ComplexNumber> solutions = Equation.Parse("x^5 - x - 1 = 0").SolveComplexNumerically();

        Assert.Equal(5, solutions.Values.Count);
        Assert.Equal(new ComplexNumber(1.1673039782614187), solutions.Values[4]);
        Assert.Equal(-0.7648844336005847, solutions.Values[0].Real, 1e-15);
        Assert.Equal(-0.35247154603172626, solutions.Values[0].Imaginary, 1e-15);
    }

    // 5e7 - sqrt(2.5e15 - 1) as a double keeps few of the digits of 1e-8.
    [Fact]
    public void Digits_lost_where_terms_cancel_are_restored()
    {
        IReadOnlyList<double> roots = Equation.Parse("x^2 - 100000000x + 1 = 0").SolveNumerically().Values;

        Assert.Equal(1e-8, roots[0], 1e-23);
        Assert.Equal(1e8, roots[1]);
    }

    [Fact]
    public void The_domain_is_checked_exactly()
    {
        Assert.Equal("{-1}", Equation.Parse("(x^2 - 1)/(x - 1) = 0").SolveNumerically().Print());
        Assert.Equal("{-1.4142135623730951}", Equation.Parse("(x^2 - 2)/(x - sqrt(2)) = 0").SolveNumerically().Print());

        NumericSolutionSet<double> everyValue = Equation.Parse("x/x = 1").SolveNumerically();
        Assert.True(everyValue.IsEveryValue);
        Assert.Equal("(-inf, inf) if x != 0", everyValue.Print());
        Assert.Equal("C if x != 0", Equation.Parse("x/x = 1").SolveComplexNumerically().Print());
    }

    [Fact]
    public void What_isnt_solved_is_listed()
    {
        NumericSolutionSet<double> solutions = Equation.Parse("x*sin(x) = 0").SolveNumerically();

        Assert.Equal("{0, ...}", solutions.Print());
        Assert.False(solutions.IsComplete);
        Assert.False(solutions.IsEmpty);
        Assert.Equal("sin(x)", Assert.Single(solutions.Unsolved).Print());
        Assert.True(Equation.Parse("1 = 2").SolveNumerically().IsEmpty);
    }

    [Fact]
    public void The_sign_assumed_for_the_unknown_selects_the_solutions() =>
        Assert.Equal("{2}", Equation.Parse("x^2 = 4").SolveNumerically("x", Assumptions.None.AssumePositive("x")).Print());

    [Fact]
    public void Every_variable_but_the_unknown_needs_a_number()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Equation.Parse("x + a = 0").SolveNumerically("x"));
        Assert.Equal("Expected no variable but x, found [a]. Substitute numbers for the others first.", ex.Message);

        Assert.Throws<InvalidOperationException>(() => Equation.Parse("x + a = 0").SolveComplexNumerically("x"));
        Assert.Throws<InvalidOperationException>(() => Equation.Parse("x + a = 0").SolveNumerically());
        Assert.Throws<ArgumentNullException>(() => Equation.Parse("x = 1").SolveNumerically(null!));
    }

    // Each value makes both sides equal, and over the complex numbers a polynomial has as many
    // distinct roots as the degree of its square-free part.
    [Theory]
    [InlineData("x^5 - x - 1 = 0", false, 1)]
    [InlineData("x^5 - x - 1 = 0", true, 5)]
    [InlineData("x^5 - 5x + 1 = 0", false, 3)]
    [InlineData("x^7 = 3", true, 7)]
    [InlineData("x^20 = 1", true, 20)]
    [InlineData("x^4 + x + 1 = 0", true, 4)]
    [InlineData("x^10 - 2x^6 - 2x^5 + x^2 + 2x + 1 = 0", true, 5)]
    [InlineData("x^3 - x - 1 = 0", true, 3)]
    [InlineData("x^2 + i = 0", true, 2)]
    [InlineData("pi*x^3 + x - 1 = 0", true, 3)]
    [InlineData("(x^5 - x - 1)/(x - 1) = 0", false, 1)]
    public void Values_satisfy_the_equation(string text, bool complex, int count)
    {
        Equation equation = Equation.Parse(text);
        IReadOnlyList<ComplexNumber> values = complex
            ? equation.SolveComplexNumerically().Values
            : [.. equation.SolveNumerically().Values.Select(value => new ComplexNumber(value))];

        Assert.Equal(count, values.Count);
        foreach (ComplexNumber value in values)
            AssertSatisfies(equation, value);
    }
}
