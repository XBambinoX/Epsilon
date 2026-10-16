using Epsilon.Calculus;
using Epsilon.Core;

namespace Epsilon.Tests.Calculus;

public class NumericIntegrationTests
{
    private const double Inf = double.PositiveInfinity;
    private const double SineIntegralOf1 = 0.946083070367183;

    private static double Integrate(string integrand, double a, double b, double tolerance = 1e-10) =>
        ExprParser.Parse(integrand).Integrate(a, b, tolerance);

    private static InvalidOperationException Fails(string integrand, double a, double b, double tolerance = 1e-10) =>
        Assert.Throws<InvalidOperationException>(() => Integrate(integrand, a, b, tolerance));

    [Fact]
    public void Smooth_integrands_to_full_precision()
    {
        Assert.Equal(1.0 / 3, Integrate("x^2", 0, 1), 1e-15);
        Assert.Equal(2, Integrate("sin(x)", 0, Math.PI), 1e-15);
        Assert.Equal(Math.E - 1, Integrate("exp(x)", 0, 1), 1e-15);
        Assert.Equal(2 * Math.PI, Integrate("2*pi", 0, 1), 1e-14);
        Assert.Equal(0.78343051071213440705, Integrate("x^x", 0, 1), 1e-15);
    }

    [Fact]
    public void Infinite_limits()
    {
        Assert.Equal(Math.PI, Integrate("1/(1 + x^2)", -Inf, Inf), 1e-14);
        Assert.Equal(Math.PI, Integrate("1/(1 + (x - 1000)^2)", -Inf, Inf), 1e-13);
        Assert.Equal(1, Integrate("exp(-x)", 0, Inf), 1e-15);
        Assert.Equal(1, Integrate("exp(x)", -Inf, 0), 1e-15);
        Assert.Equal(1, Integrate("1/x^2", 1, Inf), 1e-15);
        Assert.Equal(0.5, Integrate("cos(x)*exp(-x)", 0, Inf), 1e-15);
        Assert.Equal(0, Integrate("0", -Inf, Inf));
    }

    [Fact]
    public void Gaussian_integrals()
    {
        const string Normal = "exp(-x^2/2)/sqrt(2*pi)";
        Assert.Equal(Math.Sqrt(Math.PI), Integrate("exp(-x^2)", -Inf, Inf), 1e-15);
        Assert.Equal(Math.Sqrt(Math.PI) / 2, Integrate("exp(-x^2)", 0, Inf), 1e-15);
        Assert.Equal(Math.Sqrt(Math.PI) / 2, Integrate("x^2*exp(-x^2)", -Inf, Inf), 1e-15);
        Assert.Equal(0, Integrate("x*exp(-x^2)", -Inf, Inf), 1e-15);
        Assert.Equal(0.74682413281242702540, Integrate("exp(-x^2)", 0, 1), 1e-15);

        Assert.Equal(1, Integrate(Normal, -Inf, Inf), 1e-15);
        Assert.Equal(0.68268949213708589717, Integrate(Normal, -1, 1), 1e-15);
        Assert.Equal(0.95449973610364158560, Integrate(Normal, -2, 2), 1e-15);
        Assert.Equal(0.99730020393673981094, Integrate(Normal, -3, 3), 1e-15);
        Assert.Equal(0.024997895148220435, Integrate(Normal, 1.96, Inf), 1e-16);

        Assert.Equal(10000 * Math.Sqrt(2 * Math.PI), Integrate("exp(-x^2/(2*10000^2))", -Inf, Inf), 1e-10);
        Assert.Equal(Math.Sqrt(Math.PI) / 1000, Integrate("exp(-1000000*x^2)", -Inf, Inf), 1e-17);
    }

    // The nodes of an infinite piece far out are too sparse for a peak of width 1, and every
    // value there is 0: these came out 0 before the peaks of exponentials became breakpoints.
    // Next to 1e6 x itself is rounded to 1.2e-10, which sets the accuracy there.
    [Fact]
    public void A_Gaussian_far_from_0_is_split_at_its_peak()
    {
        Assert.Equal(Math.Sqrt(Math.PI), Integrate("exp(-(x - 200)^2)", -Inf, Inf), 1e-14);
        Assert.Equal(Math.Sqrt(Math.PI), Integrate("exp(-(x - 1000)^2)", -Inf, Inf), 1e-13);
        Assert.Equal(Math.Sqrt(Math.PI), Integrate("e^(-(x + 500)^2)", -Inf, Inf), 1e-13);
        Assert.Equal(1, Integrate("exp(-(x - 1e6)^2/(2*3^2))/(3*sqrt(2*pi))", -Inf, Inf), 1e-11);
    }

    [Fact]
    public void Reversed_limits_negate_and_equal_limits_give_zero()
    {
        Assert.Equal(-1.0 / 3, Integrate("x^2", 1, 0), 1e-15);
        Assert.Equal(-1, Integrate("exp(-x)", Inf, 0), 1e-15);
        Assert.Equal(0, Integrate("1/x", 2, 2));
    }

    // The distance to a singular end is exact down to the smallest double: these lost up to 4e-7 before.
    [Fact]
    public void Singularities_at_the_limits()
    {
        Assert.Equal(-1, Integrate("ln(x)", 0, 1), 1e-15);
        Assert.Equal(-4, Integrate("ln(x)/sqrt(x)", 0, 1), 1e-14);
        Assert.Equal(2, Integrate("ln(x)^2", 0, 1), 1e-14);
        Assert.Equal(10, Integrate("x^(-0.9)", 0, 1), 1e-13);
        Assert.Equal(Math.Sqrt(Math.PI), Integrate("exp(-x)/sqrt(x)", 0, Inf), 1e-14);
    }

    // Shifted to the end symbolically, (1 - x) next to 1 is the distance itself, and
    // 1 - x^2 expands to 2d - d^2.
    [Fact]
    public void Singularities_at_nonzero_limits()
    {
        Assert.Equal(2, Integrate("(1 - x)^(-1/2)", 0, 1), 1e-14);
        Assert.Equal(Math.PI, Integrate("1/sqrt(1 - x^2)", -1, 1), 1e-13);
        Assert.Equal(Math.PI, Integrate("1/sqrt(x*(1 - x))", 0, 1), 1e-14);
        Assert.Equal(-Math.PI / 2 * Math.Log(2), Integrate("ln(cos(x))", 0, Math.PI / 2), 1e-14);
    }

    // These came out 8.747 for 9, off by 0.4%, and 5.99998 for 6.
    [Fact]
    public void Singularities_inside_the_interval()
    {
        Assert.Equal(9, Integrate("abs(x)^(-2/3)", -1, 8), 1e-14);
        Assert.Equal(2 * Math.Sqrt(0.3) + 2 * Math.Sqrt(0.7), Integrate("abs(x - 0.3)^(-1/2)", 0, 1), 1e-14);
        Assert.Equal(6, Integrate("abs(x)^(-2/3)", -1, 1), 1e-14);
        Assert.Equal(6, Integrate("1/sqrt(abs(x))", -1, 4), 1e-14);
        Assert.Equal(2 * SineIntegralOf1, Integrate("sin(x)/x", -1, 1), 1e-15);
    }

    [Fact]
    public void Kinks_and_jumps()
    {
        Assert.Equal(2.5, Integrate("abs(x)", -1, 2), 1e-15);
        Assert.Equal(0.25, Integrate("min(x, 1 - x)", 0, 1), 1e-15);
        Assert.Equal(17.0 / 6, Integrate("max(x, x^2)", 0, 2), 1e-14);
        Assert.Equal(1.0 / 3, Integrate("sign(x - 1/3)", 0, 1), 1e-15);
        Assert.Equal(4.5, Integrate("floor(x)", 0, 3.5), 1e-14);
        Assert.Equal(2, Integrate("round(x)", 0, 2), 1e-14);
        Assert.Equal(49.5, Integrate("floor(100*x)", 0, 1), 1e-12);
    }

    [Fact]
    public void A_sharp_peak_is_found_by_halving_the_interval()
    {
        Assert.Equal(2000 * Math.Atan(1000), Integrate("1/(x^2 + 1e-6)", -1, 1), 1e-10);
        Assert.Equal(1000 * (Math.Atan(877) + Math.Atan(1123)), Integrate("1/((x - 0.123)^2 + 1e-6)", -1, 1), 1e-10);
    }

    [Fact]
    public void Under_and_overflow_far_out_cut_tails_short()
    {
        Assert.Equal(Math.Exp(-1), Integrate("exp(-1/x)/x^2", 0, 1), 1e-15);
        Assert.Equal(2, Integrate("x^2*exp(-x)", 0, Inf), 1e-14);
    }

    // These came out finite: 38.01, 32.2, 7e-15, 2.3e12 and 2e14; tan(x) on [0, 2] was -4.31.
    [Theory]
    [InlineData("1/x", 0, 1, "x = 0")]
    [InlineData("1/x", 1, Inf, "infinity")]
    [InlineData("1/x", -1, 1, "x = 0")]
    [InlineData("sin(x)", 0, Inf, "infinity")]
    [InlineData("1", -Inf, Inf, "-infinity")]
    [InlineData("1/x^2", -1, 1, "x = 0")]
    [InlineData("tan(x)", 0, 2, "x = 1.5707963267948966")]
    [InlineData("tan(x)", 0, Math.PI / 2, "x = 1.5707963267948966")]
    [InlineData("x^(-0.999)", 0, 1, "x = 0")]
    public void Divergent_integrals_throw(string integrand, double a, double b, string where) =>
        Assert.Equal($"The integral diverges at {where}, or converges there too slowly for double precision.",
            Fails(integrand, a, b).Message);

    [Fact]
    public void Divergence_is_found_at_any_tolerance()
    {
        Fails("1/x", 0, 1, tolerance: 0.99);
        Fails("tan(x)", 0, 2, tolerance: 0.9);
    }

    [Fact]
    public void Oscillation_that_converges_only_conditionally_counts_as_divergent() =>
        Fails("sin(x)/x", 0, Inf);

    [Fact]
    public void An_integrand_that_is_not_real_throws()
    {
        Assert.Equal("The integrand is not a real number at x = -0.5.", Fails("sqrt(x)", -1, 1).Message);
        Assert.Equal("The integrand is not a real number at x = -0.5.", Fails("ln(x)", -1, 1).Message);
        Assert.Equal("The integrand is not a real number at x = 1.5.", Fails("sqrt(1 - x^2)", 0, 2).Message);
    }

    // The double nearest pi isn't pi: x next to it can't come closer to the singularity than
    // its rounding, which leaves about 1e-8 out of reach.
    [Fact]
    public void A_singularity_at_a_point_that_is_not_a_double_limits_the_accuracy()
    {
        const double Exact = 5.244115108584239;
        Assert.StartsWith("The integrand is singular next to x = 3.141592653589793, at a point that isn't a double",
            Fails("1/sqrt(sin(x))", 0, Math.PI).Message);

        Assert.Equal(Exact, Integrate("1/sqrt(sin(x))", 0, Math.PI, tolerance: 1e-6), 1e-7);
        Assert.Equal(Math.PI / 2 + Math.Acosh(Math.Sqrt(2)), Integrate("1/sqrt(abs(x^2 - 2))", 0, 2, tolerance: 1e-6), 1e-7);
    }

    [Fact]
    public void A_region_too_irregular_to_converge_throws() =>
        Assert.StartsWith("The integral over [0, ", Fails("sin(1/x)", 0, 1).Message);

    [Fact]
    public void The_variable_by_name_or_the_only_one()
    {
        var f = ExprParser.Parse("exp(-t^2)");

        Assert.Equal(Math.Sqrt(Math.PI), f.Integrate("t", -Inf, Inf), 1e-15);
        Assert.Equal(f.Integrate("t", -Inf, Inf), f.Integrate(-Inf, Inf));
        Assert.Equal(2, ExprParser.Parse("3").Integrate("t", 0, 2.0 / 3), 1e-15);
    }

    [Fact]
    public void Arguments_are_checked()
    {
        var f = ExprParser.Parse("x^2");

        Assert.Throws<ArgumentException>(() => f.Integrate(double.NaN, 1));
        Assert.Throws<ArgumentException>(() => f.Integrate(0, double.NaN));
        foreach (double tolerance in new[] { 0, 1, -1e-10, double.NaN })
            Assert.Throws<ArgumentOutOfRangeException>(() => f.Integrate(0, 1, tolerance));

        Assert.Throws<ArgumentException>(() => ExprParser.Parse("a*x").Integrate("x", 0, 1));
        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("a*x").Integrate(0, 1));
        Assert.Throws<ArgumentNullException>(() => ((Expr)null!).Integrate(0, 1));
        Assert.Throws<ArgumentNullException>(() => f.Integrate(null!, 0, 1));
    }
}
