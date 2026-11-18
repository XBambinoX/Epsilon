using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class PartialFractionsTests
{
    private static readonly double[] Points = [-2.7, -1.3, -0.6, 0.35, 0.8, 1.7, 3.1];

    private static string Integral(string integrand) => ExprParser.Parse(integrand).Integrate("x").Print();

    [Theory]
    [InlineData("1/(x^3 + 1)")]
    [InlineData("x^4/(x^3 + 1)")]
    [InlineData("(x + 1)^2/(x^3 + 1)")]
    [InlineData("1/(x^3 - x)")]
    [InlineData("1/(x^3 - 8)")]
    [InlineData("1/((x - 1)^3*(x + 1)^3)")]
    [InlineData("(3*x^2 + 2*x - 1)/(x*(x - 1)^2*(x^2 + 1)^2)")]
    [InlineData("1/(x^2 + 1)^3")]
    [InlineData("(x^5 + 1)/(x^6 + 3*x^4 + 3*x^2 + 1)")]
    [InlineData("1/(x^2*(x^2 + x + 1)^2)")]
    [InlineData("1/(x^4 + 4)")]
    [InlineData("1/(x^6 - 1)")]
    [InlineData("x^3/(x^4 + x^2 + 1)")]
    [InlineData("1/(x*(x^2 - 2))")]
    [InlineData("1/(4*x^2 - 1)^2")]
    [InlineData("pi/(x^3 - 1)")]
    [InlineData("-x/(2*(x^2 + x - 6))")]
    [InlineData("x*(x + 1)^(-2)")]
    [InlineData("1/(x^4 - 2*x^2 + 1)")]
    [InlineData("1/((x^2 - 1)*(x + 1))")]
    [InlineData("x^2/(x^3 - 2)^2")]
    [InlineData("1/(x^2 + 2*x + 5)^2")]
    [InlineData("1/(2*x^2 + 1)^2")]
    [InlineData("1/(x^2 + pi)^2")]
    [InlineData("1/(x^3 - 2)")]
    [InlineData("1/(x^3 + 2)")]
    [InlineData("x/(x^3 - 3)")]
    [InlineData("1/(x^4 + 1)")]
    [InlineData("x^2/(x^4 + 1)")]
    [InlineData("1/(x^4 + 2)")]
    [InlineData("1/(x^5 - 1)")]
    [InlineData("1/(x^5 + 1)")]
    [InlineData("1/(x^4 + x^3 + x^2 + x + 1)")]
    [InlineData("1/(x^6 + 1)")]
    [InlineData("1/(x^7 - 1)")]
    [InlineData("1/(x^8 - 1)")]
    [InlineData("1/(x^12 - 1)")]
    [InlineData("1/((x + 1)^3 - 2)")]
    [InlineData("1/(x^3 - 2)^2")]
    [InlineData("1/((x^3 - 2)*(x^3 - 3))")]
    [InlineData("1/(x^5 + x)")]
    [InlineData("1/(x^3 - x - 1)")]
    [InlineData("x/(x^3 + x + 1)")]
    [InlineData("(x^2 + 1)/(x^3 + 3*x^2 + 1)")]
    [InlineData("1/(x^3 - 3*x + 1)")]
    [InlineData("1/(x^3 - 7*x + 7)")]
    [InlineData("1/(x^3 - x - 1)^2")]
    [InlineData("1/((x^3 - x - 1)*(x^2 + 1))")]
    [InlineData("1/(x^4 - 2*x^2 + 2*x + 3)")]
    [InlineData("1/(x^4 + x + 1)")]
    [InlineData("1/(x^4 - 4*x - 1)")]
    [InlineData("(x^3 + 1)/(x^4 + x^3 + 2)")]
    [InlineData("1/(x^4 + 2*x^2 + 3)")]
    [InlineData("1/(x^4 - 10*x^2 + 1)")]
    [InlineData("1/(x^6 - 5*x^3 + 6)")]
    [InlineData("1/(x^8 - 5*x^4 + 6)")]
    [InlineData("1/(x^6 - 3*x^3 + 3)")]
    [InlineData("1/((x^3 - x - 1)*(x^3 + 2))")]
    [InlineData("atan(x)/x^2")]
    public void The_derivative_of_the_antiderivative_is_the_integrand(string integrand)
    {
        var f = ExprParser.Parse(integrand);
        var derivative = f.Integrate("x").Differentiate("x");

        int checkedPoints = 0;
        foreach (double x in Points)
        {
            double expected = f.Evaluate(("x", x));
            if (!double.IsFinite(expected))
                continue;

            Assert.Equal(expected, derivative.Evaluate(("x", x)), 1e-9 * Math.Max(1, Math.Abs(expected)));
            checkedPoints++;
        }

        Assert.True(checkedPoints >= 2);
    }

    [Fact]
    public void Linear_factors()
    {
        Assert.Equal("ln(abs(x - 1)) / 3 + 2ln(abs(x + 2)) / 3", Integral("x/((x - 1)*(x + 2))"));
        Assert.Equal("2ln(abs(x - 1)) - ln(abs(x + 1))", Integral("(x + 3)/(x^2 - 1)"));
        Assert.Equal("ln(abs(x^2 - 1)) / 2 - ln(abs(x))", Integral("1/(x^3 - x)"));
    }

    // Hermite's reduction: the rational part is one fraction over the repeated factors to one power less.
    [Fact]
    public void Repeated_factors()
    {
        Assert.Equal("ln(abs(x - 1)) - 1 / (x - 1)", Integral("x/(x - 1)^2"));
        Assert.Equal("ln(abs(x)) - 1 / (2x^2)", Integral("(x^2 + 1)/x^3"));
        Assert.Equal("x / (2 * (x^2 + 1)) + atan(x) / 2", Integral("1/(x^2 + 1)^2"));
        Assert.Equal("(3x^3 + 5x) / (8(x^2 + 1)^2) + 3atan(x) / 8", Integral("1/(x^2 + 1)^3"));
        Assert.Equal("-1 / (2 * (x^2 + 1))", Integral("x/(x^2 + 1)^2"));
        Assert.Equal("(3x^3 - 5x) / (8(x + 1)^2 * (x - 1)^2) + 3ln(abs((x - 1) / (x + 1))) / 16", Integral("1/((x - 1)^3*(x + 1)^3)"));
    }

    // Repeated factors found in an expanded denominator, and shared by two factors of it.
    [Fact]
    public void Squarefree_factorization()
    {
        Assert.Equal("ln(abs((x + 1) / (x - 1))) / 4 - x / (2 * (x + 1) * (x - 1))", Integral("1/(x^4 - 2*x^2 + 1)"));
        Assert.Equal("1 / (2 * (x + 1)) + ln(abs((x - 1) / (x + 1))) / 4", Integral("1/((x^2 - 1)*(x + 1))"));
    }

    // Without a logarithmic part the factors needn't be split.
    [Fact]
    public void Only_a_rational_part() =>
        Assert.Equal("-1 / (3 * (x^3 - 2))", Integral("x^2/(x^3 - 2)^2"));

    [Fact]
    public void Quadratic_factors()
    {
        Assert.Equal("atan((2x - 1) / sqrt(3)) / sqrt(3) + ln(abs(x + 1)) / 3 - ln(x^2 - x + 1) / 6", Integral("1/(x^3 + 1)"));
        Assert.Equal("ln(abs(x)) - ln(x^2 + 1) / 2", Integral("1/(x*(x^2 + 1))"));
        Assert.Equal("ln(abs((x - 1) / (x + 1))) / 4 - atan(x) / 2", Integral("1/(x^4 - 1)"));
        Assert.Equal("ln(abs(x^2 - 2)) / 4 - ln(abs(x)) / 2", Integral("1/(x*(x^2 - 2))"));
    }

    // Logarithms with coefficients of the same size become one, of a product or a quotient.
    [Fact]
    public void Logarithms_are_merged()
    {
        Assert.Equal("ln(abs(x^3 + 1)) / 3", Integral("x^2/(x^3 + 1)"));
        Assert.Equal("ln(abs((x - 1) / (x + 1))) / 2", Integral("1/(x^2 - 1)"));
        Assert.Equal("ln(abs((x - 1) / (2x + 1))) / 3", Integral("1/((2*x + 1)*(x - 1))"));
        Assert.Equal("ln(abs((x^2 - 1) / (x^2 + 1))) / 4", Integral("x/(x^4 - 1)"));
    }

    // Expanded denominators with no rational roots, split into quadratics.
    [Fact]
    public void Products_of_quadratics()
    {
        Assert.Equal("atan(x) / 3 - atan(x / 2) / 6", Integral("1/((x^2 + 1)*(x^2 + 4))"));
        Assert.Equal("atan(x) / 3 - atan(x / 2) / 6", Integral("1/(x^4 + 5*x^2 + 4)"));
        Assert.Equal("atan((2x + 1) / sqrt(3)) / (2sqrt(3)) + atan((2x - 1) / sqrt(3)) / (2sqrt(3)) + ln((x^2 + x + 1) / (x^2 - x + 1)) / 4",
            Integral("1/(x^4 + x^2 + 1)"));
        Assert.Equal("ln((x^2 + 2x + 2) / (x^2 - 2x + 2)) / 16 + atan(x + 1) / 8 + atan(x - 1) / 8", Integral("1/(x^4 + 4)"));
    }

    [Fact]
    public void Constant_factors()
    {
        Assert.Equal("ln(x^2 - x + 1) / 6 - atan((2x - 1) / sqrt(3)) / sqrt(3) - ln(abs(x + 1)) / 3", Integral("-1/(x^3 + 1)"));
        Assert.Equal("π * ln(abs((x - 1) / (x + 1))) / 2", Integral("pi/(x^2 - 1)"));
    }

    [Fact]
    public void After_integration_by_parts() =>
        Assert.Equal("ln(abs(x)) - atan(x) / x - ln(x^2 + 1) / 2", Integral("atan(x)/x^2"));

    // Factors of degree 3 or more over the rationals by the roots of x^n - a they divide: a pair
    // of complex roots gives a logarithm and an arctangent.
    [Fact]
    public void Roots_of_binomials()
    {
        Assert.Equal("sqrt(2) * ln((x^2 + sqrt(2) * x + 1) / (x^2 - sqrt(2) * x + 1)) / 8 + sqrt(2) * (atan(sqrt(2) * x + 1) + atan(sqrt(2) * x - 1)) / 4",
            Integral("1/(x^4 + 1)"));
        Assert.Equal("(atan(2x + sqrt(3)) + atan(2x - sqrt(3))) / 6 + atan(x) / 3 + sqrt(3) * ln((x^2 + sqrt(3) * x + 1) / (x^2 - sqrt(3) * x + 1)) / 12",
            Integral("1/(x^6 + 1)"));
        Assert.Equal("nthroot(2, 3) * ln(abs(x - nthroot(2, 3))) / 6 - nthroot(2, 3) * ln(x^2 + nthroot(2, 3) * x + nthroot(2, 3)^2) / 12"
            + " - nthroot(2, 3) * sqrt(3) * atan(2sqrt(3) * x / (3nthroot(2, 3)) + sqrt(3) / 3) / 6", Integral("1/(x^3 - 2)"));
    }

    // Angles without a known value stay exact.
    [Fact]
    public void Roots_of_unity() =>
        Assert.Equal("ln(abs(x - 1)) / 5 + cos(2 * π / 5) * ln(x^2 - 2cos(2 * π / 5) * x + 1) / 5"
            + " - 2sin(π / 5) * atan(csc(π / 5) * x + cot(π / 5)) / 5 - 2sin(2 * π / 5) * atan(csc(2 * π / 5) * x - cot(2 * π / 5)) / 5"
            + " - cos(π / 5) * ln(x^2 + 2cos(π / 5) * x + 1) / 5", Integral("1/(x^5 - 1)"));

    // c/p' is a constant modulo p: one logarithm of p, whatever its roots.
    [Fact]
    public void A_constant_residue_needs_no_roots() =>
        Assert.Equal("ln(abs(x)) - ln(x^4 + 1) / 4", Integral("1/(x^5 + x)"));

    // Cardano's formula: everything about the pair of complex roots is a polynomial in the real root.
    [Fact]
    public void Cubics()
    {
        const string r = "(nthroot(1/2 - sqrt(69) / 18, 3) + nthroot(sqrt(69) / 18 + 1/2, 3))";
        Assert.Equal($"(9 * {r} / 23 - 6{r}^2 / 23 + 4/23) * ln(abs(x - nthroot(1/2 - sqrt(69) / 18, 3) - nthroot(sqrt(69) / 18 + 1/2, 3)))"
            + $" + (3{r}^2 / 23 - 9 * {r} / 46 - 2/23) * ln(x^2 + {r} * x + {r}^2 - 1)"
            + $" - (6 * {r} / 23 + 9/23) * sqrt(3{r}^2 - 4) * atan((2x + nthroot(1/2 - sqrt(69) / 18, 3) + nthroot(sqrt(69) / 18 + 1/2, 3)) / sqrt(3{r}^2 - 4))",
            Integral("1/(x^3 - x - 1)"));
    }

    // Three real roots in the trigonometric form.
    [Fact]
    public void Cubics_with_three_real_roots() =>
        Assert.Equal("(2(2cos(π / 9))^2 / 9 - 2cos(π / 9) / 9 - 4/9) * ln(abs(x + 2cos(π / 9)))"
            + " + (2cos(2 * π / 9) / 9 + 2(2cos(2 * π / 9))^2 / 9 - 4/9) * ln(abs(x - 2cos(2 * π / 9)))"
            + " + (2cos(4 * π / 9) / 9 + 2(2cos(4 * π / 9))^2 / 9 - 4/9) * ln(abs(x - 2cos(4 * π / 9)))", Integral("1/(x^3 - 3*x + 1)"));

    // A sum of fractions brought to one, which may be a polynomial.
    [Fact]
    public void Sums_of_fractions()
    {
        Assert.Equal("x - ln(abs(x + 1))", Integral("1/(1/x + 1)"));
        Assert.Equal("(x^3 / 3 + x) / 2", Integral("1/((1 - x^2)/(1 + x^2) + 1)"));
    }

    // A polynomial in x^k splits as one in y = x^k does, which the search for linear and
    // quadratic factors doesn't find.
    [Fact]
    public void Polynomials_in_a_power_of_x() =>
        Assert.Equal(Integral("1/((x^3 - 2)*(x^3 - 3))"), Integral("1/(x^6 - 5*x^3 + 6)"));

    // Roots that can't be written in radicals, an expanded product of cubics that isn't a
    // polynomial in a power of x, and symbols in the denominator.
    [Theory]
    [InlineData("1/(x^5 - x - 1)")]
    [InlineData("1/(x^6 - x^4 + x^3 - 2*x - 2)")]
    [InlineData("1/(x^6 + x + 1)")]
    [InlineData("1/(x^3 + a)")]
    public void What_isnt_integrated(string integrand) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));
}
