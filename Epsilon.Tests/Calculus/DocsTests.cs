using Epsilon.Calculus;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.Calculus;

// Epsilon.Calculus/docs/api-reference.md must list every public type and member. A new public
// member fails this test until it is documented there.
public class CalculusApiReferenceDocTests
{
    [Fact]
    public void Every_public_type_and_member_is_listed() =>
        Assert.Empty(Core.ApiReferencePages.Missing(typeof(GradientExtensions).Assembly, "Epsilon.Calculus", "docs", "api-reference.md"));
}

public class CalculusDocsLinksTests
{
    [Fact]
    public void Relative_links_resolve() =>
        Assert.Empty(LinearAlgebra.MarkdownLinks.Broken(Path.Combine("Epsilon.Calculus", "docs")));
}

// Every example in Epsilon.Calculus/docs/getting-started.md.
public class CalculusGettingStartedDocTests
{
    [Fact]
    public void A_first_example()
    {
        var f = ExprParser.Parse("x^3 - 3*x + y^2");
        Assert.Equal("[3x^2 - 3, 2y]", f.Gradient("x", "y").Print());
        Assert.Equal("[[6x, 0], [0, 2]]", f.Hessian("x", "y").Print());

        Assert.Equal(CriticalPointKind.LocalMinimum, f.ClassifyCriticalPoint(("x", 1), ("y", 0)));
        Assert.Equal(CriticalPointKind.Saddle, f.ClassifyCriticalPoint(("x", -1), ("y", 0)));

        Assert.Equal("x^3 - 3x", ExprParser.Parse("3*x^2 - 3").Integrate("x").Print());
        double gauss = ExprParser.Parse("exp(-x^2)").Integrate(double.NegativeInfinity, double.PositiveInfinity);
        Assert.Equal(1.7724538509055159, gauss);
        Assert.Equal(Math.Sqrt(Math.PI), gauss);
        Assert.Equal(-1, ExprParser.Parse("ln(x)").Integrate(0, 1));
    }
}

// Every example in Epsilon.Calculus/docs/gradients.md.
public class CalculusGradientsDocTests
{
    private static readonly Expr F = ExprParser.Parse("x^2*y + sin(y)");

    [Fact]
    public void The_gradient()
    {
        Assert.Equal("[2x * y, x^2 + cos(y)]", F.Gradient("x", "y").Print());
        Assert.Equal("[x^2 + cos(y), 2x * y]", F.Gradient("y", "x").Print());
        Assert.Equal("[2x * y, 0]", F.Gradient("x", "z").Print());
        Assert.Equal(F.Gradient("x", "y"), F.Gradient(["x", "y"]));
        Assert.Equal("[2x * y, x^2 + cos(y)]", F.Gradient().Print());

        var g = ExprParser.Parse("omega*sigma^2", "omega", "sigma");
        Assert.Equal("[sigma^2, 2omega * sigma]", g.Gradient("omega", "sigma").Print());
        Assert.Equal("e * i * a^2 * a * g * g * m * m * o * s", ExprParser.Parse("omega*sigma^2").Print());
    }

    [Fact]
    public void Numbers()
    {
        var gradient = F.Gradient("x", "y");
        Assert.Equal("[4, 0.5838531634528576]", gradient.Evaluate(("x", 1), ("y", 2)).ToString());
        Assert.Equal("[4, 0.5838531634528576]", gradient.Compile("x", "y")(1, 2).ToString());
    }

    [Fact]
    public void Gradient_descent()
    {
        var slope = ExprParser.Parse("(x - 1)^2 + 2*(y + 2)^2").Gradient("x", "y").Compile("x", "y");

        Vector<double> p = [0.0, 0.0];
        for (int i = 0; i < 100; i++)
            p -= 0.1 * slope(p[0], p[1]);

        Assert.Equal("[0.9999999997962964, -1.9999999999999998]", p.ToString());
    }

    [Fact]
    public void Directional_derivatives()
    {
        Assert.Equal("2x * y + x^2", ExprParser.Parse("x^2*y").DirectionalDerivative([1, 1], "x", "y").Print());

        double unit = F.DirectionalDerivative([0.6, 0.8], "x", "y").Evaluate(("x", 1), ("y", 2));
        double longer = F.DirectionalDerivative([3, 4], "x", "y").Evaluate(("x", 1), ("y", 2));
        Assert.Equal(2.867082530762286, unit);
        Assert.Equal(14.335412653811431, longer);
        Assert.Equal(5 * unit, longer, 1e-14);

        Assert.Equal("2cos(t) * x * y + (x^2 + cos(y)) * sin(t)",
            F.DirectionalDerivative(Vector<Expr>.Parse("[cos(t), sin(t)]"), "x", "y").Print());
    }

    [Fact]
    public void Errors()
    {
        Assert.StartsWith("Variable 'x' is listed twice.", Assert.Throws<ArgumentException>(() => F.Gradient("x", "y", "x")).Message);
        Assert.StartsWith("The direction has 3 entries but there are 2 variables.",
            Assert.Throws<ArgumentException>(() => F.DirectionalDerivative([1, 0, 0], "x", "y")).Message);
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("floor(x)*y").Gradient());
    }
}

// Every example in Epsilon.Calculus/docs/vector-fields.md.
public class CalculusVectorFieldsDocTests
{
    [Fact]
    public void Divergence()
    {
        Assert.Equal("3y", Vector<Expr>.Parse("[x*y, y^2]").Divergence("x", "y").Print());
        Assert.Equal("x", Vector<Expr>.Parse("[x*y, y^2]").Divergence("y", "x").Print());
    }

    [Fact]
    public void Curl()
    {
        Assert.Equal("[0, 0, 2]", Vector<Expr>.Parse("[-y, x, 0]").Curl("x", "y", "z").Print());
        Assert.Equal("[0, 0, y^2 - x^2]", Vector<Expr>.Parse("[x^2*y, x*y^2, 0]").Curl("x", "y", "z").Print());
    }

    [Fact]
    public void Identities()
    {
        var f = ExprParser.Parse("x*y*z + exp(x)*sin(y) + z^2");
        Assert.Equal("[0, 0, 0]", f.Gradient("x", "y", "z").Curl("x", "y", "z").Print());

        var field = Vector<Expr>.Parse("[x*y*z, x^2 + z, sin(x*y)]");
        Assert.Equal("0", field.Curl("x", "y", "z").Divergence("x", "y", "z").Print());

        Assert.Equal("8y", ExprParser.Parse("x^2*y + y^3").Gradient("x", "y").Divergence("x", "y").Print());

        var curl = ExprParser.Parse("atan(y/z)").Gradient("x", "y", "z").Curl("x", "y", "z");
        Assert.Equal("[6.938893903907228E-18, 0, 0]", curl.Evaluate(("x", 1), ("y", 2), ("z", 3)).ToString());
    }

    [Fact]
    public void Errors()
    {
        var field = Vector<Expr>.Parse("[x*y*z, x^2 + z, sin(x*y)]");
        Assert.StartsWith("The field has 2 entries but there are 1 variables.",
            Assert.Throws<ArgumentException>(() => Vector<Expr>.Parse("[x, y]").Divergence("x")).Message);
        Assert.StartsWith("The curl needs a field of 3 entries, not 2.",
            Assert.Throws<ArgumentException>(() => Vector<Expr>.Parse("[x, y]").Curl("x", "y")).Message);
        Assert.StartsWith("Variable 'x' is listed twice.", Assert.Throws<ArgumentException>(() => field.Curl("x", "y", "x")).Message);
    }
}

// Every example in Epsilon.Calculus/docs/hessians.md.
public class CalculusHessiansDocTests
{
    private static readonly Expr F = ExprParser.Parse("x^2*y + sin(y)");
    private static readonly Expr G = ExprParser.Parse("x^3 - 3*x + y^2");

    [Fact]
    public void The_Hessian()
    {
        Assert.Equal("[[2y, 2x], [2x, -sin(y)]]", F.Hessian("x", "y").Print());
        Assert.Equal("[[2y, 2x], [2x, -sin(y)]]", F.Hessian().Print());

        var h = ExprParser.Parse("x^y").Hessian("x", "y");
        Assert.True(h == h.Transpose());
    }

    [Fact]
    public void The_Laplacian()
    {
        Assert.Equal("2y - sin(y)", F.Laplacian("x", "y").Print());
        Assert.Equal("2y - sin(y)", F.Laplacian().Print());
        Assert.Equal("0", ExprParser.Parse("exp(x)*sin(y)").Laplacian("x", "y").Print());
        Assert.Equal("0", ExprParser.Parse("x^3 - 3*x*y^2").Laplacian("x", "y").Print());
        Assert.Equal(0, ExprParser.Parse("1/sqrt(x^2 + y^2 + z^2)").Laplacian("x", "y", "z").Evaluate(("x", 1), ("y", 2), ("z", 3)));
    }

    [Fact]
    public void Critical_points()
    {
        Assert.Equal(CriticalPointKind.LocalMinimum, G.ClassifyCriticalPoint(("x", 1), ("y", 0)));
        Assert.Equal(CriticalPointKind.Saddle, G.ClassifyCriticalPoint(("x", -1), ("y", 0)));
        Assert.Equal(CriticalPointKind.LocalMaximum, ExprParser.Parse("-x^2 - 3*y^2 + x*y").ClassifyCriticalPoint(("x", 0), ("y", 0)));
        Assert.Equal(CriticalPointKind.Inconclusive, ExprParser.Parse("x^4 + y^4").ClassifyCriticalPoint(("x", 0), ("y", 0)));
        Assert.Equal(CriticalPointKind.Inconclusive, ExprParser.Parse("x^3").ClassifyCriticalPoint(("x", 0)));
        Assert.Equal(CriticalPointKind.Inconclusive, ExprParser.Parse("-x^4").ClassifyCriticalPoint(("x", 0)));

        Assert.Equal(CriticalPointKind.Saddle, ExprParser.Parse("x^2 - y^2 + z^4").ClassifyCriticalPoint(("x", 0), ("y", 0), ("z", 0)));

        Assert.Equal(CriticalPointKind.LocalMinimum, ExprParser.Parse("x^2").ClassifyCriticalPoint(("x", 0)));
        Assert.Equal(CriticalPointKind.Inconclusive, ExprParser.Parse("x^2").ClassifyCriticalPoint(("x", 0), ("y", 0)));
        Assert.Equal(CriticalPointKind.LocalMinimum, G.ClassifyCriticalPoint(new Dictionary<string, double> { ["x"] = 1, ["y"] = 0 }));

        Assert.StartsWith("The point has no value for variable 'y'.", Assert.Throws<ArgumentException>(() => G.ClassifyCriticalPoint(("x", 1))).Message);
        Assert.StartsWith("Variable 'x' is given twice.",
            Assert.Throws<ArgumentException>(() => G.ClassifyCriticalPoint(("x", 1), ("y", 0), ("x", 2))).Message);
        Assert.Equal("The second derivative by x and x is Infinity at the point.",
            Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("x*ln(x)").ClassifyCriticalPoint(("x", 0))).Message);
    }

    [Fact]
    public void Newtons_method()
    {
        var rosenbrock = ExprParser.Parse("(1 - x)^2 + 100*(y - x^2)^2");
        var gradient = rosenbrock.Gradient("x", "y").Compile("x", "y");
        var hessian = rosenbrock.Hessian("x", "y").Compile("x", "y");

        Vector<double> p = [-1.2, 1.0];
        for (int i = 0; i < 8; i++)
            p -= hessian(p[0], p[1]).Solve(gradient(p[0], p[1]));

        Assert.Equal("[1, 1]", p.ToString());
        Assert.Equal(CriticalPointKind.LocalMinimum, rosenbrock.ClassifyCriticalPoint(("x", p[0]), ("y", p[1])));
    }
}

// Every example in Epsilon.Calculus/docs/numeric-integration.md.
public class CalculusNumericIntegrationDocTests
{
    private const double Inf = double.PositiveInfinity;

    private static double Integrate(string integrand, double a, double b) => ExprParser.Parse(integrand).Integrate(a, b);

    private static string Fails(string integrand, double a, double b) =>
        Assert.Throws<InvalidOperationException>(() => Integrate(integrand, a, b)).Message;

    [Fact]
    public void Definite_integrals()
    {
        Assert.Equal(0.33333333333333326, Integrate("x^2", 0, 1));
        Assert.Equal(0.6826894921370859, Integrate("exp(-x^2/2)/sqrt(2*pi)", -1, 1));
        Assert.Equal(-0.33333333333333326, Integrate("x^2", 1, 0));
        Assert.Equal(1.7724538509055159, ExprParser.Parse("exp(-t^2)").Integrate("t", -Inf, Inf));

        Assert.Equal("The expression has the variable 'a' besides 'x': substitute a value for it first. (Parameter 'variable')",
            Assert.Throws<ArgumentException>(() => ExprParser.Parse("a*x").Integrate("x", 0, 1)).Message);
        Assert.Equal(1.4999999999999998, ExprParser.Parse("a*x").Substitute("a", 3).Integrate("x", 0, 1));
    }

    [Fact]
    public void Infinite_limits()
    {
        Assert.Equal(0.9999999999999994, Integrate("exp(-x)", 0, Inf));
        Assert.Equal(3.1415926535897927, Integrate("1/(1 + x^2)", -Inf, Inf));
    }

    [Fact]
    public void Singularities()
    {
        Assert.Equal(-4, Integrate("ln(x)/sqrt(x)", 0, 1));
        Assert.Equal(10, Integrate("x^(-0.9)", 0, 1));
        Assert.Equal(2, Integrate("(1 - x)^(-1/2)", 0, 1));
        Assert.Equal(3.141592653589763, Integrate("1/sqrt(1 - x^2)", -1, 1));

        Assert.Equal(8.999999999999998, Integrate("abs(x)^(-2/3)", -1, 8));
        Assert.Equal(6, Integrate("1/sqrt(abs(x))", -1, 4));
        Assert.Equal(2.768765168078484, Integrate("abs(x - 0.3)^(-1/2)", 0, 1));
        Assert.Equal(1.8921661407343664, Integrate("sin(x)/x", -1, 1));
    }

    [Fact]
    public void Kinks_jumps_and_peaks()
    {
        Assert.Equal(2.499999999999999, Integrate("abs(x)", -1, 2));
        Assert.Equal(0.24999999999999992, Integrate("min(x, 1 - x)", 0, 1));
        Assert.Equal(4.500000000000001, Integrate("floor(x)", 0, 3.5));
        Assert.Equal(1.772453850905513, Integrate("exp(-(x - 200)^2)", -Inf, Inf));

        Assert.Equal(3139.5926542564584, Integrate("1/(x^2 + 1e-6)", -1, 1));
        Assert.Equal(3139.59265425646, 2000 * Math.Atan(1000), 1e-10);
    }

    [Fact]
    public void Divergent_integrals()
    {
        Assert.Equal("The integral diverges at x = 0, or converges there too slowly for double precision.", Fails("1/x", 0, 1));
        Assert.Equal("The integral diverges at infinity, or converges there too slowly for double precision.", Fails("1/x", 1, Inf));
        Assert.Equal("The integral diverges at x = 1.5707963267948966, or converges there too slowly for double precision.", Fails("tan(x)", 0, 2));
        Fails("x^(-0.999)", 0, 1);
        Fails("sin(x)/x", 0, Inf);
    }

    [Fact]
    public void The_tolerance()
    {
        Assert.Equal("The integrand is singular next to x = 3.141592653589793, at a point that isn't a double, which limits the accuracy to about 1.1E-07.",
            Fails("1/sqrt(sin(x))", 0, Math.PI));
        Assert.Equal(5.2441150944578965, ExprParser.Parse("1/sqrt(sin(x))").Integrate(0, Math.PI, 1e-6));
    }

    [Fact]
    public void Other_errors()
    {
        Assert.Equal("The integrand is not a real number at x = -0.5.", Fails("sqrt(x)", -1, 1));
        Assert.StartsWith("The integral over [0, 9.313225746154785E-10] didn't converge: ", Fails("sin(1/x)", 0, 1));
        Assert.Throws<InvalidOperationException>(() => Integrate("x*y", 0, 1));
        Assert.StartsWith("The tolerance must be between 0 and 1.",
            Assert.Throws<ArgumentOutOfRangeException>(() => ExprParser.Parse("x^2").Integrate(0, 1, 0)).Message);
    }
}

// Every example in Epsilon.Calculus/docs/symbolic-integration.md.
public class CalculusSymbolicIntegrationDocTests
{
    private static string Integral(string integrand) => ExprParser.Parse(integrand).Integrate("x").Print();

    [Fact]
    public void Antiderivatives()
    {
        Assert.Equal("x^3 / 3", Integral("x^2"));
        Assert.Equal("t^3 / 3", ExprParser.Parse("t^2").Integrate().Print());
        Assert.Equal("-cos(a * x) / a", Integral("sin(a*x)"));
        Assert.Equal("y^2 * x / 2", ExprParser.Parse("x*y").Integrate("y").Print());
    }

    [Fact]
    public void Exact_results()
    {
        Assert.Equal("2x^(3/2) / 3", Integral("sqrt(x)"));
        Assert.Equal("ln(abs(x))", Integral("1/x"));
        Assert.Equal("3nthroot(x, 3) * x / 4", Integral("nthroot(x, 3)"));
        Assert.Equal(12, ExprParser.Parse("nthroot(x, 3)").Integrate("x").Evaluate(("x", -8)));
    }

    [Theory]
    [InlineData("(2*x + 1)^5", "(2x + 1)^6 / 12")]
    [InlineData("exp(-3*x)", "-exp(-3x) / 3")]
    [InlineData("2^x", "2^x / ln(2)")]
    [InlineData("ln(x)", "ln(x) * x - x")]
    [InlineData("cos(3*x + 1)", "sin(3x + 1) / 3")]
    [InlineData("tan(x)", "-ln(abs(cos(x)))")]
    [InlineData("sec(x)", "ln(abs(sec(x) + tan(x)))")]
    [InlineData("atan(x)", "atan(x) * x - (1/2) * ln(x^2 + 1)")]
    [InlineData("abs(x)", "abs(x) * x / 2")]
    [InlineData("sin(x)^2", "(1/2) * x - (1/4) * sin(2x)")]
    [InlineData("sec(x)*tan(x)", "sec(x)")]
    [InlineData("sin(x)*cos(x)", "-cos(2x) / 4")]
    [InlineData("x*(x + 1)", "(1/3) * x^3 + (1/2) * x^2")]
    [InlineData("x^3/(x - 1)", "(1/3) * x^3 + (1/2) * x^2 + x + ln(abs(x - 1))")]
    [InlineData("x^2/(x^2 + 1)", "x - atan(x)")]
    [InlineData("1/(1 + x^2)", "atan(x)")]
    [InlineData("1/(x^2 + 2*x + 5)", "atan((x + 1) / 2) / 2")]
    [InlineData("x/(x^2 + 1)", "(1/2) * ln(x^2 + 1)")]
    [InlineData("1/sqrt(1 - x^2)", "asin(x)")]
    [InlineData("1/sqrt(x^2 + 1)", "asinh(x)")]
    [InlineData("sqrt(1 - x^2)", "(1/2) * sqrt(-x^2 + 1) * x + (1/2) * asin(x)")]
    public void The_table(string integrand, string antiderivative) =>
        Assert.Equal(antiderivative, Integral(integrand));

    [Fact]
    public void Rational_functions()
    {
        Assert.Equal("(1/3) * ln(abs(x - 1)) + (2/3) * ln(abs(x + 2))", Integral("x/((x - 1)*(x + 2))"));
        Assert.Equal("ln(abs(x)) - (1/2) * ln(x^2 + 1)", Integral("1/(x*(x^2 + 1))"));
        Assert.Equal("atan((2x - 1) / sqrt(3)) / sqrt(3) + (1/3) * ln(abs(x + 1)) - (1/6) * ln(x^2 - x + 1)", Integral("1/(x^3 + 1)"));

        Assert.Equal("(1/2) * ln(abs((x - 1) / (x + 1)))", Integral("1/(x^2 - 1)"));
        Assert.Equal("(1/3) * ln(abs(x^3 + 1))", Integral("x^2/(x^3 + 1)"));
        Assert.Equal("atan((2x + 1) / sqrt(3)) / (2sqrt(3)) + atan((2x - 1) / sqrt(3)) / (2sqrt(3)) + (1/4) * ln((x^2 + x + 1) / (x^2 - x + 1))",
            Integral("1/(x^4 + x^2 + 1)"));

        Assert.Equal("(3x^3 + 5x) / (8(x^2 + 1)^2) + (3/8) * atan(x)", Integral("1/(x^2 + 1)^3"));
        Assert.Equal("-x / (2 * (x + 1) * (x - 1)) + (1/4) * ln(abs((x + 1) / (x - 1)))", Integral("1/(x^4 - 2*x^2 + 1)"));
        Assert.Equal("-1 / (3 * (x^3 - 2))", Integral("x^2/(x^3 - 2)^2"));
    }

    [Fact]
    public void Rational_functions_over_roots()
    {
        Assert.Equal("(1/8) * ln((x^2 + sqrt(2) * x + 1) / (x^2 - sqrt(2) * x + 1)) * sqrt(2) + (1/4) * (atan(sqrt(2) * x + 1) + atan(sqrt(2) * x - 1)) * sqrt(2)",
            Integral("1/(x^4 + 1)"));
        Assert.Equal("(1/6) * (atan(2x + sqrt(3)) + atan(2x - sqrt(3))) + (1/3) * atan(x) + (1/12) * ln((x^2 + sqrt(3) * x + 1) / (x^2 - sqrt(3) * x + 1)) * sqrt(3)",
            Integral("1/(x^6 + 1)"));
        Assert.Contains("cos((1/5) * π)", Integral("1/(x^5 - 1)"));

        Assert.Equal("ln(abs(x + 2cos((1/9) * π))) * ((-2/9) * cos((1/9) * π) + (2/9) * (-2cos((1/9) * π))^2 - 4/9)"
            + " + ln(abs(x - 2cos((2/9) * π))) * ((2/9) * cos((2/9) * π) + (2/9) * (2cos((2/9) * π))^2 - 4/9)"
            + " + ln(abs(x - 2cos((4/9) * π))) * ((2/9) * cos((4/9) * π) + (2/9) * (2cos((4/9) * π))^2 - 4/9)", Integral("1/(x^3 - 3*x + 1)"));

        var integral = ExprParser.Parse("1/(x^3 - x - 1)").Integrate("x");
        Assert.Equal(1012, integral.Print().Length);
        Assert.Equal(0.20000000000000007, integral.Differentiate("x").Evaluate(("x", 2)));

        Assert.Equal("(1/2) * atan(x^2)", Integral("x/(1 + x^4)"));
    }

    [Fact]
    public void Checking_a_result()
    {
        var integral = ExprParser.Parse("atan(x)").Integrate("x");
        Assert.Equal("atan(x) * x - (1/2) * ln(x^2 + 1)", integral.Print());
        Assert.Equal("atan(x)", integral.Differentiate("x").Print());

        var area = ExprParser.Parse("sqrt(1 - x^2)").Integrate("x");
        Assert.Equal(1.5707963267948966, area.Evaluate(("x", 1)) - area.Evaluate(("x", -1)));
        Assert.Equal(1.570796326794897, ExprParser.Parse("sqrt(1 - x^2)").Integrate(-1, 1));
    }

    [Fact]
    public void Substitution()
    {
        Assert.Equal("sin(x^2)", Integral("2*x*cos(x^2)"));
        Assert.Equal("ln(x)^2 / 2", Integral("ln(x)/x"));
        Assert.Equal("exp(sin(x))", Integral("cos(x)*exp(sin(x))"));
        Assert.Equal("(1/2) * exp(x^2)", Integral("x*exp(x^2)"));

        Assert.Equal("(1/2) * atan(x^2)", Integral("x/(1 + x^4)"));
        Assert.Equal("atan(exp(x))", Integral("exp(x)/(1 + exp(2*x))"));
        Assert.Equal("(1/5) * (x^2 + 1)^(5/2) - (1/3) * (x^2 + 1)^(3/2)", Integral("x^3*sqrt(x^2 + 1)"));

        Assert.Equal("(2/5) * sqrt(x + 1)^5 - (2/3) * sqrt(x + 1)^3", Integral("x*sqrt(x + 1)"));
        Assert.Equal("-2atan(sqrt(x)) + 2sqrt(x)", Integral("sqrt(x)/(1 + x)"));

        Assert.Equal("(1/3) * cos(x)^3 - cos(x)", Integral("sin(x)^3"));
        Assert.Equal("(1/8) * x - (1/32) * sin(4x)", Integral("sin(x)^2*cos(x)^2"));
        Assert.Equal("ln(abs(cos(x))) + (1/2) * sec(x)^2", Integral("tan(x)^3"));
    }

    [Fact]
    public void Integration_by_parts()
    {
        Assert.Equal("exp(x) * x - exp(x)", Integral("x*exp(x)"));
        Assert.Equal("x^2 * sin(x) + 2cos(x) * x - 2sin(x)", Integral("x^2*cos(x)"));
        Assert.Equal("(1/2) * ln(x) * x^2 - (1/4) * x^2", Integral("x*ln(x)"));

        Assert.Equal("2x + ln(x)^2 * x - 2ln(x) * x", Integral("ln(x)^2"));
        Assert.Equal("ln(x^2 + 1) * x - 2x + 2atan(x)", Integral("ln(x^2 + 1)"));
        Assert.Equal("asin(x)^2 * x - 2x + 2asin(x) * (-x^2 + 1)^(1/2)", Integral("asin(x)^2"));

        Assert.Equal("(exp(x) * sin(x) - cos(x) * exp(x)) / 2", Integral("exp(x)*sin(x)"));
        Assert.Equal("(2/13) * cos(3x) * exp(2x) + (3/13) * exp(2x) * sin(3x)", Integral("exp(2*x)*cos(3*x)"));
        Assert.Equal("(sin(ln(x)) * x - cos(ln(x)) * x) / 2", Integral("sin(ln(x))"));

        Assert.Equal("-(1/2) * x + (1/4) * exp(2x)", Integral("exp(x)*sinh(x)"));
        Assert.Equal("(1/2) * ln(abs(sec(x) + tan(x))) + (1/2) * sec(x) * tan(x)", Integral("sec(x)^3"));
        Assert.Equal("2exp(sqrt(x)) * sqrt(x) - 2exp(sqrt(x))", Integral("exp(sqrt(x))"));
    }

    [Theory]
    [InlineData("1/(2 + cos(x))")]
    [InlineData("1/(x^5 - x - 1)")]
    [InlineData("1/(x^2 + a^2)")]
    [InlineData("sin(2*x)/(1 + cos(x)^2)")]
    [InlineData("exp(x)*cos(x)^2")]
    [InlineData("1/(x^2*sqrt(x^2 - 1))")]
    [InlineData("x*asin(x)")]
    [InlineData("exp(x^2)")]
    [InlineData("x*tan(x)")]
    public void What_isnt_integrated_yet(string integrand) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));

    [Fact]
    public void The_message_and_the_numeric_integral()
    {
        Assert.Equal("No antiderivative of 1 / (x^2 * sqrt(x^2 - 1)) by x was found.",
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse("1/(x^2*sqrt(x^2 - 1))").Integrate("x")).Message);
        Assert.Equal(1.4626517459071819, ExprParser.Parse("exp(x^2)").Integrate(0, 1));
    }
}

// Every example in Epsilon.Calculus/docs/limitations.md.
public class CalculusLimitationsDocTests
{
    [Fact]
    public void Symbolic_integration()
    {
        foreach (string integrand in new[] { "sin(2*x)/(1 + cos(x)^2)", "exp(x)*cos(x)^2", "1/(x^2*sqrt(x^2 - 1))", "x*asin(x)", "1/(2 + cos(x))",
            "1/(x^5 - x - 1)", "1/(x^2 + a^2)", "1/(x^3 + pi)" })
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));

        Assert.InRange(ExprParser.Parse("1/(x^3 - x - 1)").Integrate("x").Print().Length, 900, 1100);
        Assert.InRange(ExprParser.Parse("1/(x^4 + x + 1)").Integrate("x").Print().Length, 2500, 3500);
        Assert.NotNull(ExprParser.Parse("1/(x^2 + pi)").Integrate("x"));

        Assert.Equal("((1/2) * x^2 + x - (1/2) * abs(x - 1) * (x - 1)) / 2", ExprParser.Parse("min(x, 1)").Integrate("x").Print());
        Assert.Equal("2atan(2x / sqrt(2)) / sqrt(2)", ExprParser.Parse("1/(x^2 + 1/2)").Integrate("x").Print());
        Assert.Equal("x^2 + x + (x - 1) / abs(x - 1)^2 + 1", ExprParser.Parse("x^3/(x - 1)").Integrate("x").Differentiate("x").Print());
    }

    [Fact]
    public void Numeric_integration()
    {
        var peak = ExprParser.Parse("sech(10*(x - 1000))^2");
        Assert.Equal(0, peak.Integrate(double.NegativeInfinity, double.PositiveInfinity));
        Assert.Equal(0.19999999999998613, peak.Integrate(double.NegativeInfinity, 1000) + peak.Integrate(1000, double.PositiveInfinity));

        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("sin(x)/x").Integrate(0, double.PositiveInfinity));
        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("exp(-x^2)").Integrate(0, 1e300));
        Assert.Equal(0.8862269254527579, ExprParser.Parse("exp(-x^2)").Integrate(0, double.PositiveInfinity));
    }

    [Fact]
    public void Derivatives()
    {
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("floor(x)*y").Hessian());

        var laplacian = ExprParser.Parse("ln(x^2 + y^2)").Laplacian("x", "y");
        Assert.Equal("(-4x^2 + 2 * (x^2 + y^2)) / (x^2 + y^2)^2 + (-4y^2 + 2 * (x^2 + y^2)) / (x^2 + y^2)^2", laplacian.Print());
        Assert.Equal(0, laplacian.Evaluate(("x", 1), ("y", 2)));
    }
}
