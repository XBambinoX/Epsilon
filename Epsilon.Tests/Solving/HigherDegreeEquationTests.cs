using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class HigherDegreeEquationTests
{
    private static string Printed(string equation) => Equation.Parse(equation).Solve("x").Print();

    private static string PrintedComplex(string equation) => Equation.Parse(equation).SolveComplex("x").Print();

    [Theory]
    [InlineData("x^3 = 2", "{nthroot(2, 3)}")]
    [InlineData("x^3 - x - 1 = 0", "{nthroot(1/2 - sqrt(69) / 18, 3) + nthroot(sqrt(69) / 18 + 1/2, 3)}")]
    [InlineData("x^3 - 3x + 1 = 0", "{2cos(10 * π / 9), 2cos(4 * π / 9), 2cos(2 * π / 9)}")]
    [InlineData("x^4 - 4x - 1 = 0", "{sqrt(2) / 2 - sqrt(4sqrt(2) - 2) / 2, sqrt(2) / 2 + sqrt(4sqrt(2) - 2) / 2}")]
    [InlineData("x^4 + 2x^3 - 3 = 0", "{-nthroot(2, 3) - 1, 1}")]
    [InlineData("x^4 + 1 = 0", "{}")]
    public void Cubics_and_quartics_in_radicals(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("x^3 = 2", "{-nthroot(2, 3) / 2 - i * nthroot(2, 3) * sqrt(3) / 2, i * nthroot(2, 3) * sqrt(3) / 2 - nthroot(2, 3) / 2, nthroot(2, 3)}")]
    [InlineData("x^4 + 1 = 0", "{-sqrt(2) / 2 - i * sqrt(2) / 2, i * sqrt(2) / 2 - sqrt(2) / 2, sqrt(2) / 2 - i * sqrt(2) / 2, sqrt(2) / 2 + i * sqrt(2) / 2}")]
    public void Complex_roots_in_radicals(string equation, string expected) => Assert.Equal(expected, PrintedComplex(equation));

    // An expanded square has its root once, by its square-free part.
    [Theory]
    [InlineData("x^6 - 4x^3 + 4 = 0", "{nthroot(2, 3)}")]
    [InlineData("x^4 - 4x^2 + 4 = 0", "{-sqrt(2), sqrt(2)}")]
    [InlineData("x^10 - 2x^6 - 2x^5 + x^2 + 2x + 1 = 0", "{~1.16730397826142}")]
    public void Repeated_roots_are_listed_once(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    [Fact]
    public void Roots_without_a_closed_form_are_approximate()
    {
        SolutionSet solutions = Equation.Parse("x^5 - x - 1 = 0").Solve();

        Assert.Equal("{~1.16730397826142}", solutions.Print());
        Assert.Equal("\\left\\{\\approx 1.16730397826142\\right\\}", solutions.ToLatex());
        Assert.False(solutions.IsExact);
        Assert.True(solutions.IsComplete);
        Assert.True(solutions.Solutions[0].IsApproximate);
        Assert.Equal(new Constant(Rational.FromDecimalString("1.16730397826142")), solutions.Solutions[0].Value);
    }

    [Fact]
    public void Approximate_roots_mix_with_exact_ones()
    {
        SolutionSet solutions = Equation.Parse("x*(x^5 - x - 1) = 0").Solve();

        Assert.Equal("{0, ~1.16730397826142}", solutions.Print());
        Assert.False(solutions.Solutions[0].IsApproximate);
        Assert.True(Equation.Parse("x^2 = 2").Solve().IsExact);
    }

    [Fact]
    public void Approximate_complex_roots()
    {
        Assert.Equal(
            "{~-0.764884433600585 - 0.352471546031726 * i, ~-0.764884433600585 + 0.352471546031726 * i, "
                + "~0.181232444469875 - 1.08395410131771 * i, ~0.181232444469875 + 1.08395410131771 * i, ~1.16730397826142}",
            PrintedComplex("x^5 - x - 1 = 0"));
        Assert.Equal(
            "\\left\\{\\approx -0.727136084491197 - 0.430014288329716 i, \\approx -0.727136084491197 + 0.430014288329716 i, "
                + "\\approx 0.727136084491197 - 0.934099289460529 i, \\approx 0.727136084491197 + 0.934099289460529 i\\right\\}",
            Equation.Parse("x^4 + x + 1 = 0").SolveComplex().ToLatex());
    }

    // Radicals only for rational coefficients, and for a quartic only without cube roots; numbers
    // otherwise. A polynomial with other variables in its coefficients isn't solved.
    [Theory]
    [InlineData("x^4 - 2x^3 - x + 3 = 0", "{~1.37512552707099, ~1.78793319384471}")]
    [InlineData("pi*x^3 + x - 1 = 0", "{~0.530627058457308}")]
    [InlineData("x^5 - 5x + 1 = 0", "{~-1.54165168410452, ~0.200064102629975, ~1.44050039734156}")]
    [InlineData("x^3 + a = 0", "{...}")]
    public void Numbers_where_radicals_dont_apply(string equation, string expected) => Assert.Equal(expected, Printed(equation));

    // Each solution makes both sides equal, and over the complex numbers a polynomial has as many
    // distinct roots as the degree of its square-free part.
    [Theory]
    [InlineData("2x + 1 = 7", false, 1)]
    [InlineData("x^2 + 2x - 1 = 0", false, 2)]
    [InlineData("x^2 + 2x + 5 = 0", true, 2)]
    [InlineData("x^3 - x - 1 = 0", false, 1)]
    [InlineData("x^3 - x - 1 = 0", true, 3)]
    [InlineData("x^3 - 3x + 1 = 0", true, 3)]
    [InlineData("x^3 + x^2 + x + 2 = 0", true, 3)]
    [InlineData("x^4 - 4x - 1 = 0", true, 4)]
    [InlineData("x^4 - 10x^2 + 1 = 0", false, 4)]
    [InlineData("x^4 + x + 1 = 0", true, 4)]
    [InlineData("x^5 - x - 1 = 0", true, 5)]
    [InlineData("x^5 - 5x + 1 = 0", false, 3)]
    [InlineData("x^7 = 3", true, 7)]
    [InlineData("x^20 = 1", true, 20)]
    [InlineData("x^6 - 4x^3 + 4 = 0", true, 3)]
    [InlineData("x^2 + i = 0", true, 2)]
    [InlineData("1/x + 1/(x - 1) = 0", false, 1)]
    [InlineData("(x^5 - x - 1)/(x - 1) = 0", false, 1)]
    public void Solutions_satisfy_the_equation(string text, bool complex, int count)
    {
        Equation equation = Equation.Parse(text);
        SolutionSet solutions = complex ? equation.SolveComplex("x") : equation.Solve("x");

        Assert.True(solutions.IsComplete);
        Assert.Equal(count, solutions.Solutions.Count);
        foreach (Solution solution in solutions.Solutions)
        {
            var at = new Dictionary<string, ComplexNumber> { ["x"] = solution.Value.EvaluateComplex() };
            double residual = (equation.Left - equation.Right).EvaluateComplex(at).Magnitude;
            Assert.True(residual < 1e-9, $"{solution.Print()}: |left - right| = {residual}");
        }
    }

    [Theory]
    [InlineData(new[] { 4, -4, 1 }, new[] { -2, 1 })]
    [InlineData(new[] { 0, 1, 2, 1 }, new[] { 0, 1, 1 })]
    [InlineData(new[] { -2, 0, 0, 1 }, new[] { -2, 0, 0, 1 })]
    public void Square_free_parts(int[] coefficients, int[] expected)
    {
        Rational[] squareFree = Polynomials.SquareFree([.. coefficients.Select(coefficient => (Rational)coefficient)]);
        Rational[] monic = [.. squareFree.Select(coefficient => coefficient / squareFree[^1])];
        Rational[] expectedMonic = [.. expected.Select(coefficient => new Rational(coefficient) / expected[^1])];

        Assert.Equal(expectedMonic, monic);
    }
}
