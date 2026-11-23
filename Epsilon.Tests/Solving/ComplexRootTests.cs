using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

public class ComplexRootTests
{
    private static readonly ComplexNumber Low = new(-3, -3), High = new(3, 3);

    private static IReadOnlyList<ComplexNumber> Roots(string equation) =>
        Equation.Parse(equation).SolveComplexNumerically(Low, High).Values;

    private static List<ComplexNumber> Searched(string f) =>
        ComplexRoots.InRectangle(ExprParser.Parse(f), "x", Low, High);

    private static void Near(ComplexNumber expected, ComplexNumber actual, double precision = 1e-12) =>
        Assert.True((actual - expected).Magnitude < precision, $"{actual} is not within {precision} of {expected}");

    [Fact]
    public void Solutions_in_a_rectangle()
    {
        Assert.Equal([new ComplexNumber(0, -1), new ComplexNumber(0, 1)], Roots("x^2 + 1 = 0"));
        Assert.Equal([new ComplexNumber(0, 1)], Equation.Parse("x^2 + 1 = 0").SolveComplexNumerically(new ComplexNumber(-2, 0), new ComplexNumber(2, 2)).Values);
        Assert.Equal(
            [new ComplexNumber(-0.7648844336005847, 0.3524715460317262), new ComplexNumber(0.18123244446987535, 1.0839541013177107)],
            Equation.Parse("x^5 - x - 1 = 0").SolveComplexNumerically(new ComplexNumber(-1, 0), new ComplexNumber(1, 2)).Values);
    }

    [Fact]
    public void Solutions_of_what_isnt_a_polynomial()
    {
        Assert.Equal(
            [new ComplexNumber(0, -2 * Math.PI), ComplexNumber.Zero, new ComplexNumber(0, 2 * Math.PI)],
            Equation.Parse("exp(x) = 1").SolveComplexNumerically(new ComplexNumber(-1, -7), new ComplexNumber(1, 7)).Values);

        IReadOnlyList<ComplexNumber> roots = Roots("x = cos(x)");
        Assert.Equal(3, roots.Count);
        Assert.Equal(new ComplexNumber(0.7390851332151607), roots[2]);
    }

    // Merge checks at k/16 of a segment would all hit roots of sin(10x) between -0.8pi and 0.8pi.
    [Fact]
    public void Evenly_spaced_roots_are_not_merged()
    {
        var roots = Equation.Parse("sin(10x) = 0").SolveComplexNumerically(new ComplexNumber(-3, -0.5), new ComplexNumber(3, 0.5)).Values;

        Assert.Equal(19, roots.Count);
        for (int k = -9; k <= 9; k++)
            Near(new ComplexNumber(k * Math.PI / 10), roots[k + 9]);
    }

    [Theory]
    [InlineData("x * ln(x) = 0", 1.0)]                    // also 5.8e-246 + 1e-246i
    [InlineData("x^2 * ln(x) = 0", 1.0)]                  // also -2.4e-112
    [InlineData("(x - 1) * ln(x - 1) = 0", 2.0)]          // also 1 - 7e-248i
    [InlineData("x^2 / x = 0")]                           // was -3e-170
    [InlineData("(x - 1)^2 / (x - 1) = 0")]               // was 1 + 1.5e-168i
    public void No_solution_where_the_equation_is_undefined(string equation, params double[] expected) =>
        Assert.Equal([.. expected.Select(value => new ComplexNumber(value))], Roots(equation));

    [Fact]
    public void A_solution_near_a_guess()
    {
        Assert.Equal(ComplexNumber.ImaginaryUnit, Equation.Parse("x^2 + 1 = 0").FindComplexRoot(ComplexNumber.ImaginaryUnit * 0.9));
        Near(new ComplexNumber(Math.Log(2)), Equation.Parse("exp(z) = 2").FindComplexRoot("z", ComplexNumber.ImaginaryUnit)!.Value);
        Assert.Null(Equation.Parse("x^2 + 5 = 0").FindComplexRoot(ComplexNumber.Zero)); // a flat slope at 0
    }

    [Fact]
    public void Rectangles_have_finite_corners_in_order()
    {
        Equation equation = Equation.Parse("x^2 = 1");

        Assert.Equal("Expected the lower left corner first, got 1 + 1i and -1 - 1i.",
            Assert.Throws<ArgumentException>(() => equation.SolveComplexNumerically(new ComplexNumber(1, 1), new ComplexNumber(-1, -1))).Message);
        Assert.Throws<ArgumentException>(() => equation.SolveComplexNumerically(-5, 5));
        Assert.Equal("A corner of the rectangle isn't a finite number.",
            Assert.Throws<ArgumentException>(() => equation.SolveComplexNumerically(new ComplexNumber(double.NegativeInfinity, -1), High)).Message);
        Assert.Throws<InvalidOperationException>(() => Equation.Parse("x = a").SolveComplexNumerically("x", Low, High));
        Assert.Throws<InvalidOperationException>(() => Equation.Parse("x = a").FindComplexRoot("x", ComplexNumber.Zero));
    }

    // The search itself, which polynomials don't reach through an equation.
    [Theory]
    [InlineData("(x - 1)^2", 1, 1e-12)]                                    // was 80 roots around 0.99999
    [InlineData("x^2 - 2x + 1", 1, 1e-12)]
    [InlineData("(x - 1)^3", 1, 1e-12)]                                    // was 150
    [InlineData("x^3 - 3x^2 + 3x - 1", 1, 1e-6)]                           // expanded: rounding limits the precision
    [InlineData("x^4 - 4x^3 + 6x^2 - 4x + 1", 1, 1e-4)]
    [InlineData("(x - 1)^7", 1, 1e-12)]
    [InlineData("x^5", 0, 1e-12)]                                          // was 161, up to 0.009 away from 0
    [InlineData("(exp(x) - 1)^2", 0, 1e-12)]                               // was 50
    [InlineData("x - 1e-120", 1e-120, 1e-130)]                             // a tiny root stays where it is
    [InlineData("x^3", 0, 0)]
    public void The_search_reports_a_multiple_root_once(string f, double expected, double precision) =>
        Near(new ComplexNumber(expected), Assert.Single(Searched(f)), Math.Max(precision, double.Epsilon));

    [Theory]
    [InlineData("(x^2 + 1)^2")]
    [InlineData("x^4 + 2x^2 + 1")]
    public void The_search_reports_double_complex_roots_once(string f)
    {
        var roots = Searched(f).OrderBy(z => z.Imaginary).ToList();

        Assert.Equal(2, roots.Count);                                      // was 55
        Near(-ComplexNumber.ImaginaryUnit, roots[0], 1e-9);
        Near(ComplexNumber.ImaginaryUnit, roots[1], 1e-9);
    }

    [Theory]
    [InlineData("(x - 1)^2 * (x + 2)", new[] { -2.0, 1.0 })]               // was 37
    [InlineData("(x - 1)*(x - 1.001)", new[] { 1.0, 1.001 })]              // close simple roots stay apart
    [InlineData("x^3 - x", new[] { -1.0, 0.0, 1.0 })]                      // a root between two doesn't merge them
    public void The_search_keeps_distinct_roots_apart(string f, double[] expected)
    {
        var roots = Searched(f).OrderBy(z => z.Real).ToList();

        Assert.Equal(expected.Length, roots.Count);
        for (int i = 0; i < expected.Length; i++)
            Near(new ComplexNumber(expected[i]), roots[i]);
    }
}
