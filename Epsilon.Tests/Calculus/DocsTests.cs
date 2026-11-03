using Epsilon.Calculus;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.Calculus;

// The digits shown for numeric integrals, up to the last ones: the nodes come from sinh, cosh and
// tanh of the C library, which round differently from version to version (glibc 2.39 and 2.44).
internal static class Shown
{
    public static void Equal(double shown, double actual) => Assert.Equal(shown, actual, Math.Abs(shown) * 1e-14);
}

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
        Assert.Empty(LinearAlgebra.MarkdownLinks.Broken(Path.Combine("Epsilon.Calculus", "docs"), Path.Combine("Epsilon.Calculus", "CHANGELOG.md")));
}

// Every example in Epsilon.Calculus/README.md, with the result its comment shows.
public class CalculusReadmeExamplesTests
{
    private static readonly Expr F = ExprParser.Parse("x^2*y + sin(y)");

    private static string Integral(string integrand) => ExprParser.Parse(integrand).Integrate("x").Print();

    [Fact]
    public void Header()
    {
        var f = ExprParser.Parse("x^3 - 3*x + y^2");
        Assert.Equal("[3x^2 - 3, 2y]", f.Gradient("x", "y").Print());
        Assert.Equal(CriticalPointKind.LocalMinimum, f.ClassifyCriticalPoint(("x", 1), ("y", 0)));
        Assert.Equal("exp(x) * x - exp(x)", Integral("x*exp(x)"));
    }

    [Fact]
    public void Quick_start()
    {
        Assert.Equal("[2x * y, x^2 + cos(y)]", F.Gradient("x", "y").Print());
        Assert.Equal("[[2y, 2x], [2x, -sin(y)]]", F.Hessian("x", "y").Print());
        Assert.Equal("2y - sin(y)", F.Laplacian("x", "y").Print());

        Assert.Equal("(1/2) * ln(x) * x^2 - (1/4) * x^2", Integral("x*ln(x)"));
        Assert.Equal("(1/2) * ln(abs((x - 1) / (x + 1)))", Integral("1/(x^2 - 1)"));

        Shown.Equal(1.7724538509055159, ExprParser.Parse("exp(-x^2)").Integrate(double.NegativeInfinity, double.PositiveInfinity));
        Shown.Equal(-1, ExprParser.Parse("ln(x)").Integrate(0, 1));
    }

    [Fact]
    public void Gradients_and_vector_fields()
    {
        var gradient = F.Gradient("x", "y");
        Assert.Equal("[4, 0.5838531634528576]", gradient.Evaluate(("x", 1), ("y", 2)).ToString());
        Assert.Equal("[4, 0.5838531634528576]", gradient.Compile("x", "y")(1, 2).ToString());

        Assert.Equal("2x * y + x^2", ExprParser.Parse("x^2*y").DirectionalDerivative([1, 1], "x", "y").Print());
        Assert.Equal("3y", Vector<Expr>.Parse("[x*y, y^2]").Divergence("x", "y").Print());
        Assert.Equal("[0, 0, 2]", Vector<Expr>.Parse("[-y, x, 0]").Curl("x", "y", "z").Print());
    }

    [Fact]
    public void Hessians_and_critical_points()
    {
        var g = ExprParser.Parse("x^3 - 3*x + y^2");
        Assert.Equal(CriticalPointKind.LocalMinimum, g.ClassifyCriticalPoint(("x", 1), ("y", 0)));
        Assert.Equal(CriticalPointKind.Saddle, g.ClassifyCriticalPoint(("x", -1), ("y", 0)));
        Assert.Equal(CriticalPointKind.Inconclusive, ExprParser.Parse("x^4 + y^4").ClassifyCriticalPoint(("x", 0), ("y", 0)));

        var rosenbrock = ExprParser.Parse("(1 - x)^2 + 100*(y - x^2)^2");
        var slope = rosenbrock.Gradient("x", "y").Compile("x", "y");
        var hessian = rosenbrock.Hessian("x", "y").Compile("x", "y");

        Vector<double> p = [-1.2, 1.0];
        for (int i = 0; i < 8; i++)
            p -= hessian(p[0], p[1]).Solve(slope(p[0], p[1]));

        Assert.Equal("[1, 1]", p.ToString());
    }

    [Fact]
    public void Antiderivatives()
    {
        Assert.Equal("sin(x^2)", Integral("2*x*cos(x^2)"));
        Assert.Equal("(exp(x) * sin(x) - cos(x) * exp(x)) / 2", Integral("exp(x)*sin(x)"));
        Assert.Equal("3nthroot(x, 3) * x / 4", Integral("nthroot(x, 3)"));
        Assert.Equal("-sqrt(x^2 + 4) / (4x)", Integral("1/(x^2*sqrt(4 + x^2))"));

        Assert.Equal("(1/3) * ln(abs(x - 1)) + (2/3) * ln(abs(x + 2))", Integral("x/((x - 1)*(x + 2))"));
        Assert.Equal("(3x^3 + 5x) / (8(x^2 + 1)^2) + (3/8) * atan(x)", Integral("1/(x^2 + 1)^3"));
        Assert.Equal("cos(x) - 2atan(cos(x))", Integral("sin(x)^3/(1 + cos(x)^2)"));
        Assert.Equal("(x + 2atan(-sin(x) / (cos(x) + sqrt(3) + 2))) / sqrt(3)", Integral("1/(2 + cos(x))"));

        Assert.Throws<NotSupportedException>(() => Integral("exp(x^2)"));
    }

    [Fact]
    public void Definite_integrals()
    {
        Shown.Equal(3.141592653589763, ExprParser.Parse("1/sqrt(1 - x^2)").Integrate(-1, 1));
        Shown.Equal(8.999999999999998, ExprParser.Parse("abs(x)^(-2/3)").Integrate(-1, 8));
        Shown.Equal(1.772453850905513, ExprParser.Parse("exp(-(x - 200)^2)").Integrate(double.NegativeInfinity, double.PositiveInfinity));
        Shown.Equal(1.4626517459071819, ExprParser.Parse("exp(x^2)").Integrate(0, 1));

        Assert.Equal("The integral diverges at x = 0, or converges there too slowly for double precision.",
            Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("1/x").Integrate(0, 1)).Message);
    }

    [Fact]
    public void Good_to_know()
    {
        Assert.Equal("[sigma^2, 2omega * sigma]",
            ExprParser.Parse("omega*sigma^2", "omega", "sigma").Gradient("omega", "sigma").Print());

        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("floor(x)*y").Gradient());
        Shown.Equal(4.500000000000001, ExprParser.Parse("floor(x)").Integrate(0, 3.5));
        Assert.Equal("abs(x)", Integral("sign(x)"));
        Assert.NotNull(ExprParser.Parse("max(x, 0)").Integrate("x"));
    }
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
        Shown.Equal(1.7724538509055159, gauss);
        Shown.Equal(Math.Sqrt(Math.PI), gauss);
        Shown.Equal(-1, ExprParser.Parse("ln(x)").Integrate(0, 1));
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
        Shown.Equal(0.33333333333333326, Integrate("x^2", 0, 1));
        Shown.Equal(0.6826894921370859, Integrate("exp(-x^2/2)/sqrt(2*pi)", -1, 1));
        Shown.Equal(-0.33333333333333326, Integrate("x^2", 1, 0));
        Shown.Equal(1.7724538509055159, ExprParser.Parse("exp(-t^2)").Integrate("t", -Inf, Inf));

        Assert.Equal("The expression has the variable 'a' besides 'x': substitute a value for it first. (Parameter 'variable')",
            Assert.Throws<ArgumentException>(() => ExprParser.Parse("a*x").Integrate("x", 0, 1)).Message);
        Shown.Equal(1.4999999999999998, ExprParser.Parse("a*x").Substitute("a", 3).Integrate("x", 0, 1));
    }

    [Fact]
    public void Infinite_limits()
    {
        Shown.Equal(0.9999999999999994, Integrate("exp(-x)", 0, Inf));
        Shown.Equal(3.1415926535897927, Integrate("1/(1 + x^2)", -Inf, Inf));
        Shown.Equal(0.8862269254527574, Integrate("exp(-x^2)", 0, 1e300));
    }

    [Fact]
    public void Singularities()
    {
        Shown.Equal(-4, Integrate("ln(x)/sqrt(x)", 0, 1));
        Shown.Equal(10, Integrate("x^(-0.9)", 0, 1));
        Shown.Equal(2, Integrate("(1 - x)^(-1/2)", 0, 1));
        Shown.Equal(3.141592653589763, Integrate("1/sqrt(1 - x^2)", -1, 1));

        Shown.Equal(8.999999999999998, Integrate("abs(x)^(-2/3)", -1, 8));
        Shown.Equal(6, Integrate("1/sqrt(abs(x))", -1, 4));
        Shown.Equal(2.768765168078484, Integrate("abs(x - 0.3)^(-1/2)", 0, 1));
        Shown.Equal(1.8921661407343664, Integrate("sin(x)/x", -1, 1));

        Shown.Equal(7.120606397869798, Integrate("1/sqrt(abs(sin(x)))", 0, 4));
        Shown.Equal(2.45216991381447, Integrate("1/sqrt(abs(x^2 - 2))", 0, 2));
    }

    [Fact]
    public void Kinks_jumps_and_peaks()
    {
        Shown.Equal(2.499999999999999, Integrate("abs(x)", -1, 2));
        Shown.Equal(0.24999999999999992, Integrate("min(x, 1 - x)", 0, 1));
        Shown.Equal(4.500000000000001, Integrate("floor(x)", 0, 3.5));
        Shown.Equal(1.772453850905513, Integrate("exp(-(x - 200)^2)", -Inf, Inf));
        Shown.Equal(0.19999999999998613, Integrate("sech(10*(x - 1000))^2", -Inf, Inf));
        Shown.Equal(3.1415926535897935E-05, Integrate("1/(1 + 1e10*(x - 1000)^2)", -1e300, 1e300));

        Shown.Equal(3140.3068990234406, Integrate("1/(sin(x)^2 + 1e-6)", -1, 1));
        Assert.Equal(3140.306899023443, 2 / Math.Sqrt(1e-6 * (1 + 1e-6)) * Math.Atan(Math.Sqrt((1 + 1e-6) / 1e-6) * Math.Tan(1)), 1e-10);
    }

    [Fact]
    public void Oscillating_integrands()
    {
        Shown.Equal(1.5707963267948968, Integrate("sin(x)/x", 0, Inf));
        Shown.Equal(0.5778636748954608, Integrate("cos(x)/(1 + x^2)", 0, Inf));
        Shown.Equal(1.5707963267948963, Integrate("sin(x)^2/x^2", 0, Inf));

        Fails("sin(x)", 0, Inf);
        Fails("sin(x)^2/x", 1, Inf);
    }

    [Fact]
    public void Divergent_integrals()
    {
        Assert.Equal("The integral diverges at x = 0, or converges there too slowly for double precision.", Fails("1/x", 0, 1));
        Assert.Equal("The integral diverges at infinity, or converges there too slowly for double precision.", Fails("1/x", 1, Inf));
        Assert.Equal("The integral diverges at x = 1.5707963267948966, or converges there too slowly for double precision.", Fails("tan(x)", 0, 2));
        Fails("x^(-0.999)", 0, 1);
    }

    [Fact]
    public void The_tolerance()
    {
        Assert.Equal("The integrand is singular next to x = 3.141592653589793, at a point that isn't a double, which limits the accuracy to about 1.1E-07.",
            Fails("1/sqrt(sin(x))", 0, Math.PI));
        Shown.Equal(5.2441150944578965, ExprParser.Parse("1/sqrt(sin(x))").Integrate(0, Math.PI, 1e-6));
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
    [InlineData("2^x*3^x", "2^x * 3^x / ln(6)")]
    [InlineData("ln(x)", "ln(x) * x - x")]
    [InlineData("cos(3*x + 1)", "sin(3x + 1) / 3")]
    [InlineData("tan(x)", "-ln(abs(cos(x)))")]
    [InlineData("sec(x)", "ln(abs(sec(x) + tan(x)))")]
    [InlineData("atan(x)", "atan(x) * x - (1/2) * ln(x^2 + 1)")]
    [InlineData("abs(x)", "abs(x) * x / 2")]
    [InlineData("floor(x)", "floor(x) * x - (1/2) * (floor(x) + 1) * floor(x)")]
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
        Shown.Equal(1.570796326794897, ExprParser.Parse("sqrt(1 - x^2)").Integrate(-1, 1));
        Assert.Equal("(1/2) * π", ExprParser.Parse("sqrt(1 - x^2)").IntegrateExact("x", -1, 1).Print());
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
    public void Trigonometric_and_hyperbolic_functions()
    {
        Assert.Equal("(x + 2atan(-sin(x) / (cos(x) + sqrt(3) + 2))) / sqrt(3)", Integral("1/(2 + cos(x))"));
        Assert.Equal("x + 2atan(-sin(x) / (cos(x) + sqrt(2) + 1))", Integral("1/(sqrt(2) + cos(x))"));
        Assert.Equal("(1/4) * ln(abs((-4cos(x) + 3sin(x) + 5) / (5sin(x) + 3)))", Integral("1/(3 + 5*sin(x))"));
        Assert.Equal("x + cos(x) / (sin(x) + 1)", Integral("sin(x)/(1 + sin(x))"));

        var f = ExprParser.Parse("1/(2 + cos(x))");
        var antiderivative = f.Integrate("x");
        Shown.Equal(3.6275987284684357, antiderivative.Evaluate(("x", 2 * Math.PI)) - antiderivative.Evaluate(("x", 0)));
        Shown.Equal(3.627598728468432, f.Integrate(0, 2 * Math.PI));

        Assert.Equal("cos(x) - 2atan(cos(x))", Integral("sin(x)^3/(1 + cos(x)^2)"));
        Assert.Equal("(1/3) * sin(x)^3 - 1 / sin(x) - 2sin(x)", Integral("cos(x)^5/sin(x)^2"));
        Assert.Equal("(1/2) * (x + atan(-sin(2x) / (cos(2x) + 3)))", Integral("1/(sin(x)^2 + 4*cos(x)^2)"));
        Assert.Equal("-ln(cos(x)^2 + 1)", Integral("sin(2*x)/(1 + cos(x)^2)"));

        Assert.Equal("-2 * (3tan(x / 2)^2 + 3tan(x / 2) + 2) / (3(tan(x / 2) + 1)^3)", Integral("1/(1 + sin(x))^2"));

        Assert.Equal("ln(abs((exp(x) - sqrt(3) + 2) / (exp(x) + sqrt(3) + 2))) / sqrt(3)", Integral("1/(2 + cosh(x))"));
        Assert.Equal("x - coth(x)", Integral("cosh(x)^2/sinh(x)^2"));
    }

    [Fact]
    public void Square_roots_of_quadratics()
    {
        Assert.Equal("-(1/2) * sqrt(-x^2 + 1) * x + (1/2) * asin(x)", Integral("x^2/sqrt(1 - x^2)"));
        Assert.Equal("-sqrt(x^2 + 4) / (4x)", Integral("1/(x^2*sqrt(4 + x^2))"));
        Assert.Equal("(1/4) * x^3 * sqrt(x^2 + 1) + (1/8) * sqrt(x^2 + 1) * x - (1/8) * asinh(x)", Integral("x^2*sqrt(x^2 + 1)"));

        Assert.Equal("sqrt(x^2 - 1) / x", Integral("1/(x^2*sqrt(x^2 - 1))"));
        Assert.Equal("ln(abs(x + sqrt(x^2 - 1))) - sqrt(x^2 - 1) / x", Integral("sqrt(x^2 - 1)/x^2"));

        Assert.Equal("-2 / (x + sqrt(x^2 - 2x))", Integral("1/(x*sqrt(x^2 - 2*x))"));
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
        Assert.Equal("(1/2) * asin(x) * x^2 + (1/4) * sqrt(-x^2 + 1) * x - (1/4) * asin(x)", Integral("x*asin(x)"));

        Assert.Equal("(exp(x) * sin(x) - cos(x) * exp(x)) / 2", Integral("exp(x)*sin(x)"));
        Assert.Equal("(2/13) * cos(3x) * exp(2x) + (3/13) * exp(2x) * sin(3x)", Integral("exp(2*x)*cos(3*x)"));
        Assert.Equal("(1/2) * cos(x) * sinh(x) + (1/2) * cosh(x) * sin(x)", Integral("cosh(x)*cos(x)"));
        Assert.Equal("(sin(ln(x)) * x - cos(ln(x)) * x) / 2", Integral("sin(ln(x))"));

        Assert.Equal("-(1/2) * x + (1/4) * exp(2x)", Integral("exp(x)*sinh(x)"));
        Assert.Equal("(1/2) * ln(abs(sec(x) + tan(x))) + (1/2) * sec(x) * tan(x)", Integral("sec(x)^3"));
        Assert.Equal("2exp(sqrt(x)) * sqrt(x) - 2exp(sqrt(x))", Integral("exp(sqrt(x))"));

        Assert.Equal("(1/2) * exp(x) + (1/10) * cos(2x) * exp(x) + (1/5) * exp(x) * sin(2x)", Integral("exp(x)*cos(x)^2"));
        Assert.Equal("(1/2) * sin(x) + (-1/10) * cos(x) * exp(-2x) + (1/20) * exp(-2x) * sin(x) + (1/20) * exp(2x) * sin(x) + (1/10) * cos(x) * exp(2x)",
            Integral("cosh(x)^2*cos(x)"));
    }

    [Fact]
    public void The_imaginary_unit()
    {
        Assert.Equal("i * x^2 / 2", Integral("i*x"));
        Assert.Equal("(1/2) * ln(x^2 + 1) - atan(x) * i", Integral("1/(x + i)"));
        Assert.Equal("-cos(x) * i + sin(x)", Integral("exp(i*x)"));
        Assert.Equal("(-1/2) * ln(abs(cos(2x) + cosh(2))) + (1/2) * (2x + 2atan(-sin(2x) / (cos(2x) + exp(2)))) * i", Integral("tan(x + i)"));
        Assert.Equal("2(x + i)^(3/2) / 3", Integral("sqrt(x + i)"));
        Assert.Throws<NotSupportedException>(() => Integral("atan(x + i)"));
    }

    [Theory]
    [InlineData("1/(x^5 - x - 1)")]
    [InlineData("1/(x^2 + a^2)")]
    [InlineData("1/(sqrt(2) + cos(x))^2")]
    [InlineData("exp(x^2)")]
    [InlineData("x*tan(x)")]
    [InlineData("sqrt(1 + x^3)")]
    [InlineData("1/sqrt(x^4 + 1)")]
    public void What_isnt_integrated_yet(string integrand) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));

    [Fact]
    public void The_message_and_the_numeric_integral()
    {
        Assert.Equal("No antiderivative of 1 / (a^2 + x^2) by x was found.",
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse("1/(x^2 + a^2)").Integrate("x")).Message);
        Shown.Equal(1.4626517459071819, ExprParser.Parse("exp(x^2)").Integrate(0, 1));
    }
}

// Every example in Epsilon.Calculus/docs/exact-integration.md.
public class CalculusExactIntegrationDocTests
{
    private static string Integral(string expr, Expr a, Expr b) => ExprParser.Parse(expr).IntegrateExact("x", a, b).Print();

    private static string Fails<T>(string expr, Expr a, Expr b) where T : Exception =>
        Assert.Throws<T>(() => ExprParser.Parse(expr).IntegrateExact("x", a, b)).Message;

    [Fact]
    public void Definite_integrals()
    {
        Assert.Equal("1/3", Integral("x^2", 0, 1));
        Assert.Equal("(1/4) * π", Integral("1/(1 + x^2)", 0, 1));
        Assert.Equal("(1/2) * ln(3/2)", Integral("1/(x^2 - 1)", 2, 3));
        Assert.Equal("π / (3sqrt(3)) + (1/3) * ln(2)", Integral("1/(x^3 + 1)", 0, 1));
        Assert.Equal("-1/3", Integral("x^2", 1, 0));

        Assert.Equal("2", Integral("sin(x)", 0, new Pi()));
        Assert.Equal("1/2", Integral("exp(-x)", 0, ExprParser.Parse("ln(2)")));
        Assert.Equal("2", ExprParser.Parse("sin(t)").IntegrateExact(0, new Pi()).Print());
        Assert.Equal("-cos(314159265358979/100000000000000) + 1", Integral("sin(x)", 0, Math.PI));
    }

    [Fact]
    public void Singular_limits_and_points_inside()
    {
        Assert.Equal("-1", Integral("ln(x)", 0, 1));
        Assert.Equal("4", Integral("1/sqrt(x)", 0, 4));
        Assert.Equal("-1/4", Integral("x*ln(x)", 0, 1));
        Assert.Equal("9/2", Integral("1/nthroot(x, 3)", -1, 8));

        var f = ExprParser.Parse("1/(1 + tan(x))");
        Assert.Equal("(1/2) * x + (-1/4) * ln(tan(x)^2 + 1) + (1/2) * ln(abs(tan(x) + 1))", f.Integrate("x").Print());
        Assert.Equal("(1/4) * π", f.IntegrateExact("x", 0, ExprParser.Parse("pi/2")).Print());
    }

    [Fact]
    public void Infinite_limits()
    {
        var inf = double.PositiveInfinity;

        Assert.Equal("1", ExprParser.Parse("exp(-x)").IntegrateExact("x", 0, inf).Print());
        Assert.Equal("π", ExprParser.Parse("1/(1 + x^2)").IntegrateExact("x", -inf, inf).Print());
        Assert.Equal("1/2", ExprParser.Parse("exp(-x)*sin(x)").IntegrateExact("x", 0, inf).Print());
        Assert.Equal("(1/4) * π * sqrt(2)", ExprParser.Parse("1/(x^4 + 1)").IntegrateExact("x", 0, inf).Print());
        Assert.Equal("π", ExprParser.Parse("1/(sqrt(x)*(1 + x))").IntegrateExact("x", 0, inf).Print());
        Assert.Equal("exp(-π)", ExprParser.Parse("exp(-x)").IntegrateExact("x", new Pi(), inf).Print());
        Assert.Equal("The integral of 1 / x from 1 to inf diverges at inf.",
            Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("1/x").IntegrateExact("x", 1, inf)).Message);
    }

    [Fact]
    public void Classic_integrals()
    {
        var inf = double.PositiveInfinity;

        Assert.Equal("sqrt(π)", ExprParser.Parse("exp(-x^2)").IntegrateExact("x", -inf, inf).Print());
        Assert.Equal("1", ExprParser.Parse("exp(-x^2/2)/sqrt(2*pi)").IntegrateExact("x", -inf, inf).Print());
        Assert.Equal("π / 2", ExprParser.Parse("sin(x)/x").IntegrateExact("x", 0, inf).Print());
        Assert.Equal("sqrt(π) / 2", ExprParser.Parse("sqrt(x)*exp(-x)").IntegrateExact("x", 0, inf).Print());
        Assert.Equal("(1/15) * π^4", ExprParser.Parse("x^3/(exp(x) - 1)").IntegrateExact("x", 0, inf).Print());
        Assert.Equal("(-1/6) * π^2", ExprParser.Parse("ln(x)/(1 - x)").IntegrateExact("x", 0, 1).Print());
        Assert.Equal("(1/2) * sqrt(π) + 1", ExprParser.Parse("exp(-x^2) + exp(-x)").IntegrateExact("x", 0, inf).Print());

        Assert.Equal("The integral of exp(-x^2 * a) from -inf to inf isn't found: it depends on the sign of a.",
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse("exp(-a*x^2)").IntegrateExact("x", -inf, inf)).Message);
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("exp(-x^3)").IntegrateExact("x", 0, inf));
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("x^2/(exp(x) - 1)").IntegrateExact("x", 0, inf));
    }

    [Fact]
    public void Across_periods_and_jumps()
    {
        Assert.Equal("2 * π / sqrt(3)", Integral("1/(2 + cos(x))", 0, ExprParser.Parse("2*pi")));
        Assert.Equal("-2 * (3tan(3/2)^2 + 3tan(3/2) + 2) / (3(tan(3/2) + 1)^3) + 4/3", Integral("1/(1 + sin(x))^2", 0, 3));

        var one = ExprParser.Parse("sec(x)^2/(1 + tan(x)^2)");
        Assert.Equal("atan(tan(x))", one.Integrate("x").Print());
        Assert.Equal("10", one.IntegrateExact("x", 0, 10).Print());
    }

    [Fact]
    public void Odd_integrands()
    {
        Assert.Equal("0", Integral("x^3*cos(x)", -2, 2));
        Assert.Equal("0", Integral("sin(x)/(1 + x^2)", -1, 1));
        Assert.Equal("0", Integral("atan(1/x)", -1, 1));
        Assert.Equal("0", ExprParser.Parse("sin(x)/(1 + x^2)").IntegrateExact("x", double.NegativeInfinity, double.PositiveInfinity).Print());
        Assert.Equal("The integral of 1 / x^3 from -1 to 1 diverges at x = 0.", Fails<InvalidOperationException>("1/x^3", -1, 1));
        Assert.Equal("The integral of x / (x^2 + 1) from -inf to inf diverges at inf.",
            Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("x/(1 + x^2)").IntegrateExact("x", double.NegativeInfinity, double.PositiveInfinity)).Message);
    }

    [Fact]
    public void Divergent_integrals()
    {
        Assert.Equal("The integral of 1 / x from -1 to 1 diverges at x = 0.", Fails<InvalidOperationException>("1/x", -1, 1));
        Assert.Equal("The integral of tan(x) from 0 to π diverges at x = (1/2) * π.", Fails<InvalidOperationException>("tan(x)", 0, new Pi()));
        Assert.Equal("The integral of sqrt(x) from -1 to 1 doesn't exist: the integrand isn't real at x = -0.5.",
            Fails<InvalidOperationException>("sqrt(x)", -1, 1));
    }

    [Fact]
    public void Symbols()
    {
        Assert.Equal("exp(c) / c - 1 / c", Integral("exp(c*x)", 0, 1));
        Assert.Equal("(1/3) * c^3", Integral("x^2", 0, new Variable("c")));
        Assert.Equal("2 * i", Integral("exp(i*x)", 0, new Pi()));
        Assert.Equal("The integral of 1 / x from 1 to c isn't found: whether its antiderivative ln(abs(x)) is undefined or jumps between the limits depends on c.",
            Fails<NotSupportedException>("1/x", 1, new Variable("c")));
    }

    [Theory]
    [InlineData("sin(x)/x", 1, 2)]
    [InlineData("1/(x + c)", 0, 1)]
    [InlineData("(1 - sin(x))/(x + cos(x))", -1, 0)]
    [InlineData("1/(1 + cos(x)^4)", 0, 10)]
    [InlineData("cos(1/x)/x^2", -1, 1)]
    [InlineData("exp(-x^2)", 0, 1)]
    public void What_isnt_found(string expr, int a, int b) => Fails<NotSupportedException>(expr, a, b);

    [Theory]
    [InlineData("exp(-x^3)", 0)]
    [InlineData("x^2/(exp(x) - 1)", 0)]
    [InlineData("cos(x)", 0)]
    [InlineData("exp(-c*x)", 0)]
    public void What_isnt_found_to_inf(string expr, double a) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(expr).IntegrateExact("x", a, double.PositiveInfinity));
}

// Every example in Epsilon.Calculus/docs/series.md.
public class CalculusSeriesDocTests
{
    private static string Printed(string expr, string point, int degree) =>
        ExprParser.Parse(expr).Series("x", ExprParser.Parse(point), degree).Print();

    [Fact]
    public void Taylor_series()
    {
        Assert.Equal("1 - (1/6) * x^2 + (1/120) * x^4 + O(x^5)", ExprParser.Parse("sin(x)/x").Series("x", 0, 4).Print());
        Assert.Equal("1 + x + (1/2) * x^2 + (1/6) * x^3 + (1/24) * x^4 + O(x^5)", ExprParser.Parse("exp(x)").Series("x", 0, 4).Print());
        Assert.Equal("x + (1/3) * x^3 + (2/15) * x^5 + O(x^6)", ExprParser.Parse("tan(x)").Series("x", 0, 5).Print());

        Assert.Equal("(x - 1) - (1/2) * (x - 1)^2 + (1/3) * (x - 1)^3 + O((x - 1)^4)", ExprParser.Parse("ln(x)").Series("x", 1, 3).Print());
        Assert.Equal("1 - (1/2) * (x - (1/2) * π)^2 + O((x - (1/2) * π)^3)", Printed("sin(x)", "pi/2", 2));

        Assert.Equal("15707963267949/10000000000000", ExprParser.Parse("tan(x)").Series("x", Math.PI / 2, 1).Point.Print());
        Assert.Equal("t - (1/6) * t^3 + O(t^4)", ExprParser.Parse("sin(t)").Series(0, 3).Print());
    }

    [Fact]
    public void The_terms()
    {
        var series = ExprParser.Parse("cos(x)").Series("x", 0, 4);

        Assert.Equal("-1/2", series.Coefficient(2).Print());
        Assert.Equal("0", series.Coefficient(3).Print());
        Assert.Equal(new Rational(5), series.Order);
        Assert.Equal(3, series.Terms.Count);
        Assert.Equal("(1/24) * x^4 + (-1/2) * x^2 + 1", series.ToExpr().Print());
        Assert.Equal(0.9950041666666667, series.ToExpr().Evaluate(("x", 0.1)));
        Assert.Equal(0.9950041652780258, Math.Cos(0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Coefficient(5));
    }

    [Fact]
    public void Poles_Laurent_series()
    {
        Assert.Equal("x^-1 + (1/6) * x + (7/360) * x^3 + O(x^4)", Printed("1/sin(x)", "0", 3));
        Assert.Equal("x^-1 - 1/2 + (1/12) * x - (1/720) * x^3 + O(x^4)", Printed("1/(exp(x) - 1)", "0", 3));
        Assert.Equal("-(x - (1/2) * π)^-1 + (1/3) * (x - (1/2) * π) + O((x - (1/2) * π)^2)", Printed("tan(x)", "pi/2", 1));
    }

    [Fact]
    public void Roots_Puiseux_series()
    {
        var root = ExprParser.Parse("sqrt(x + x^2)").Series("x", 0, 2);
        Assert.Equal("x^(1/2) + (1/2) * x^(3/2) + O(x^(5/2))", root.Print());
        Assert.Equal(new Rational(5, 2), root.Order);
        Assert.True(root.RightSided);

        Assert.Equal("x + O(x^3)", Printed("sqrt(x^2)", "0", 2));
        Assert.True(ExprParser.Parse("sqrt(x^2)").Series("x", 0, 2).RightSided);
        Assert.Equal("x + (1/3) * x^2 + O(x^3)", Printed("nthroot(x^3 + x^4, 3)", "0", 2));
        Assert.False(ExprParser.Parse("nthroot(x^3 + x^4, 3)").Series("x", 0, 2).RightSided);

        Assert.Equal("sqrt(2) * (x - 1)^(1/2) - (sqrt(2) / 12) * (x - 1)^(3/2) + O((x - 1)^(5/2))", Printed("acosh(x)", "1", 2));
    }

    [Fact]
    public void Symbols()
    {
        Assert.Equal("1 + a * x + (1/2) * a^2 * x^2 + O(x^3)", Printed("exp(a*x)", "0", 2));
        Assert.Equal("-1 / a - (1 / a^2) * x - (1 / a^3) * x^2 + O(x^3)", Printed("1/(x - a)", "0", 2));
        Assert.Equal("sin(a) + cos(a) * (x - a) - (sin(a) / 2) * (x - a)^2 + O((x - a)^3)", Printed("sin(x)", "a", 2));
        Assert.Throws<NotSupportedException>(() => Printed("abs(a*x)", "0", 2));
    }

    [Theory]
    [InlineData("ln(x)", "0")]
    [InlineData("x*ln(x)", "0")]
    [InlineData("exp(1/x)", "0")]
    [InlineData("sin(1/x)", "0")]
    [InlineData("asin(x)", "1")]
    [InlineData("sqrt(x - 2)", "0")]
    [InlineData("floor(x)", "1")]
    public void What_has_no_series(string expr, string point) =>
        Assert.Throws<NotSupportedException>(() => Printed(expr, point, 3));

    [Fact]
    public void The_message() =>
        Assert.Equal("ln(x) has a logarithm of x in its series at x = 0.",
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse("ln(x)").Series("x", 0, 3)).Message);

    [Fact]
    public void Their_limits()
    {
        Assert.Equal("0", ExprParser.Parse("x*ln(x)").Limit("x", 0).Print());
        Assert.Equal("0", ExprParser.Parse("exp(1/x)").Limit("x", 0, LimitDirection.Left).Print());
        Assert.Equal("inf", ExprParser.Parse("exp(1/x)").Limit("x", 0, LimitDirection.Right).Print());
    }
}

// Every example in Epsilon.Calculus/docs/limits.md.
public class CalculusLimitsDocTests
{
    private const double Inf = double.PositiveInfinity;

    [Fact]
    public void Limits_at_a_point()
    {
        Assert.Equal("1", ExprParser.Parse("sin(x)/x").Limit("x", 0).Print());
        Assert.Equal("1/2", ExprParser.Parse("(1 - cos(x))/x^2").Limit("x", 0).Print());
        Assert.Equal("12", ExprParser.Parse("(x^3 - 8)/(x - 2)").Limit("x", 2).Print());
        Assert.Equal("0", ExprParser.Parse("x*ln(x)").Limit("x", 0).Print());
        Assert.Equal("exp(-1/2)", ExprParser.Parse("cos(x)^(1/x^2)").Limit("x", 0).Print());
        Assert.Equal("inf", ExprParser.Parse("1/(x - 2)^2").Limit("x", 2).Print());

        Assert.Equal("314159265358979/100000000000000", new Constant(Math.PI).Print());
        Assert.Equal("-1", ExprParser.Parse("sin(x)/(x - pi)").Limit("x", ExprParser.Parse("pi")).Print());
        Assert.Equal("-1", ExprParser.Parse("sin(x)/(x - pi)").Limit("x", new Pi()).Print());
        Assert.Equal("1", ExprParser.Parse("sin(t)/t").Limit(0).Print());
    }

    [Fact]
    public void At_infinity()
    {
        Assert.Equal("e", ExprParser.Parse("(1 + 1/x)^x").Limit("x", Inf).Print());
        Assert.Equal("1/2", ExprParser.Parse("sqrt(x^2 + x) - x").Limit("x", Inf).Print());
        Assert.Equal("0", ExprParser.Parse("x^7/exp(x)").Limit("x", Inf).Print());
        Assert.Equal("0", ExprParser.Parse("ln(x)/x").Limit("x", Inf).Print());
        Assert.Equal("3", ExprParser.Parse("(2^x + 3^x)^(1/x)").Limit("x", Inf).Print());
        Assert.Equal("(-1/2) * π", ExprParser.Parse("atan(x)").Limit("x", double.NegativeInfinity).Print());
        Assert.Equal("0", ExprParser.Parse("x*exp(x)").Limit("x", double.NegativeInfinity).Print());

        Assert.Equal("-1", ExprParser.Parse("exp(x)*(exp(1/x - exp(-x)) - exp(1/x))").Limit("x", Inf).Print());
    }

    [Fact]
    public void One_side()
    {
        var f = ExprParser.Parse("1/x");

        Assert.Equal("inf", f.Limit("x", 0, LimitDirection.Right).Print());
        Assert.Equal("-inf", f.Limit("x", 0, LimitDirection.Left).Print());
        Assert.Equal("0", ExprParser.Parse("exp(-1/x)").Limit("x", 0, LimitDirection.Right).Print());
        Assert.Equal("0", ExprParser.Parse("floor(x)").Limit("x", 1, LimitDirection.Left).Print());
        Assert.Equal("inf", ExprParser.Parse("tan(x)").Limit("x", ExprParser.Parse("pi/2"), LimitDirection.Left).Print());

        Assert.Throws<ArgumentException>(() => f.Limit("x", Inf, LimitDirection.Right));
        Assert.Throws<ArgumentException>(() => f.Limit("x", double.NegativeInfinity, LimitDirection.Left));
    }

    [Fact]
    public void Where_there_is_no_limit()
    {
        Assert.Equal("The limit of 1 / x at x = 0 doesn't exist: -inf from the left, inf from the right.",
            Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("1/x").Limit("x", 0)).Message);

        Assert.Equal("0", ExprParser.Parse("sqrt(x)").Limit("x", 0).Print());
        Assert.Equal("-inf", ExprParser.Parse("ln(x)").Limit("x", 0).Print());
        Assert.Equal("The limit of sqrt(x) at x = 0 from the left doesn't exist: sqrt(x) isn't real there.",
            Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("sqrt(x)").Limit("x", 0, LimitDirection.Left)).Message);
    }

    [Fact]
    public void The_result()
    {
        Limit limit = ExprParser.Parse("(1 + 1/x)^x").Limit("x", double.PositiveInfinity);

        Assert.Equal(LimitKind.Finite, limit.Kind);
        Assert.Equal(2.718281828459045, limit.Value.Evaluate());

        Limit pole = ExprParser.Parse("1/x^2").Limit("x", 0);

        Assert.Equal(LimitKind.PositiveInfinity, pole.Kind);
        Assert.Equal("inf", pole.Print());
        Assert.Equal("The limit is inf, not a value.", Assert.Throws<InvalidOperationException>(() => pole.Value).Message);
    }

    [Fact]
    public void Symbols()
    {
        Assert.Equal("exp(a)", ExprParser.Parse("(1 + a/x)^x").Limit("x", Inf).Print());
        Assert.Equal("2a", ExprParser.Parse("(x^2 - a^2)/(x - a)").Limit("x", ExprParser.Parse("a")).Print());
        Assert.Equal("The limit of a * x as x -> inf depends on the sign of a.",
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse("a*x").Limit("x", Inf)).Message);
    }

    [Fact]
    public void Oscillations()
    {
        Assert.Equal("0", ExprParser.Parse("sin(x)/x").Limit("x", Inf).Print());
        Assert.Equal("1", ExprParser.Parse("x/(x + sin(x))").Limit("x", Inf).Print());
        Assert.Equal("0", ExprParser.Parse("exp(-x)*floor(x)").Limit("x", Inf).Print());
        Assert.Equal("0", ExprParser.Parse("x*sin(1/x)").Limit("x", 0).Print());
        Assert.Equal("The limit of sin(1 / x) at x = 0 isn't found: a part of it oscillates there.",
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse("sin(1/x)").Limit("x", 0)).Message);
    }

    [Theory]
    [InlineData("sin(x)")]
    [InlineData("x*sin(x)")]
    [InlineData("x/(2 + sin(x))")]
    [InlineData("2^x - 3^x")]
    [InlineData("x^pi - x^2")]
    [InlineData("exp((i - 1)*x)")]
    [InlineData("x^a")]
    public void What_isnt_found(string expr) =>
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse(expr).Limit("x", Inf));
}

// Every example in Epsilon.Calculus/docs/limitations.md.
public class CalculusLimitationsDocTests
{
    [Fact]
    public void Symbolic_integration()
    {
        foreach (string integrand in new[] { "sqrt(1 + x^3)", "1/sqrt(x^4 + 1)",
            "1/(x^5 - x - 1)", "1/(x^2 + a^2)", "1/(x^3 + pi)" })
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));

        Assert.Contains("sign(x - 1)", ExprParser.Parse("1/((x - pi)*sqrt(x^2 - 2*x))").Integrate("x").Print());

        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("1/(x^6 - x^4 + x^3 - 2*x - 2)").Integrate("x"));
        Assert.NotNull(ExprParser.Parse("1/((x^3 - x - 1)*(x^3 + 2))").Integrate("x"));

        Assert.InRange(ExprParser.Parse("1/(x^3 - x - 1)").Integrate("x").Print().Length, 900, 1100);
        Assert.InRange(ExprParser.Parse("1/(x^4 + x + 1)").Integrate("x").Print().Length, 2500, 3500);
        Assert.NotNull(ExprParser.Parse("1/(x^2 + pi)").Integrate("x"));
        Assert.NotNull(ExprParser.Parse("1/(sqrt(2) + cos(x))").Integrate("x"));

        foreach (string integrand in new[] { "x*floor(x)", "floor(x)^2", "floor(x^2)" })
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));
        Shown.Equal(4.500000000000001, ExprParser.Parse("floor(x)").Integrate(0, 3.5));
        Assert.Equal("abs(x)", ExprParser.Parse("sign(x)").Integrate("x").Print());

        Assert.Equal("i * x^2 / 2", ExprParser.Parse("i*x").Integrate("x").Print());
        Assert.Equal("(1/2) * x^2 + i * x", ExprParser.Parse("x + i").Integrate("x").Print());
        Assert.Equal("-cos(x) * i + sin(x)", ExprParser.Parse("exp(i*x)").Integrate("x").Print());
        foreach (string integrand in new[] { "atan(x + i)", "ln(x + i*x)", "sech(x + i)", "1/(sqrt(2) + cos(x))^2", "1/(pi + cos(x)^2)" })
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse(integrand).Integrate("x"));

        Assert.Equal("((1/2) * x^2 + x - (1/2) * abs(x - 1) * (x - 1)) / 2", ExprParser.Parse("min(x, 1)").Integrate("x").Print());
        Assert.Equal("2atan(sqrt(2) * x) / sqrt(2) + 2x / (2x^2 + 1)", ExprParser.Parse("1/(x^2 + 1/2)^2").Integrate("x").Print());
        Assert.Equal("x^2 + x + (x - 1) / abs(x - 1)^2 + 1", ExprParser.Parse("x^3/(x - 1)").Integrate("x").Differentiate("x").Print());
    }

    [Fact]
    public void Numeric_integration()
    {
        Assert.Equal("The integrand is singular next to x = 1.189207115002721, at a point that isn't a double, which limits the accuracy to about 2.5E-08.",
            Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("1/sqrt(abs(x^4 - 2))").Integrate(0, 2)).Message);

        Shown.Equal(0.2705135801622119, ExprParser.Parse("sin(x^2)*exp(-x)").Integrate(0, double.PositiveInfinity));
        Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("sin(x^2)").Integrate(0, double.PositiveInfinity));
        Assert.StartsWith("The integrand oscillates towards an infinity in a way that isn't taken apart yet: ",
            Assert.Throws<InvalidOperationException>(() => ExprParser.Parse("sin(x^2)/x^2").Integrate(1, double.PositiveInfinity)).Message);
    }

    [Fact]
    public void Exact_definite_integrals()
    {
        Assert.Contains("may be undefined at x = -0.739",
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse("(1 - sin(x))/(x + cos(x))").IntegrateExact("x", -1, 0)).Message);
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("1/(1 + cos(x)^4)").IntegrateExact("x", 0, 10));
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("1/x").IntegrateExact("x", 1, new Variable("c")));

        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("exp(-x^2)").IntegrateExact("x", 0, 1));
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("sin(x)/x").IntegrateExact("x", 1, double.PositiveInfinity));
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("exp(-x^3)").IntegrateExact("x", 0, double.PositiveInfinity));
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("x^2/(exp(x) - 1)").IntegrateExact("x", 0, double.PositiveInfinity));
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("cos(x)").IntegrateExact("x", 0, double.PositiveInfinity));

        Expr logistic = ExprParser.Parse("1/(1 + exp(x))").IntegrateExact("x", -1, 1);
        Assert.Equal("ln(exp(1) / (exp(1) + 1)) - ln(exp(-1) / (exp(-1) + 1))", logistic.Print());
        Assert.Equal(1, logistic.Evaluate(), 1e-15);
    }

    [Fact]
    public void Series()
    {
        foreach (string expr in new[] { "ln(x)", "x*ln(x)", "exp(1/x)" })
            Assert.Throws<NotSupportedException>(() => ExprParser.Parse(expr).Series("x", 0, 3));
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("asin(x)").Series("x", 1, 3));
        Assert.StartsWith("sqrt(2) * (x - 1)^(1/2)", ExprParser.Parse("acosh(x)").Series("x", 1, 2).Print());
        Assert.Equal("-1 / a", ExprParser.Parse("1/(x - a)").Series("x", 0, 2).Coefficient(0).Print());
        Assert.Equal("tan(15707963267949/10000000000000)", ExprParser.Parse("tan(x)").Series("x", Math.PI / 2, 1).Coefficient(0).Print());
    }

    [Fact]
    public void Limits()
    {
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("x/(2 + sin(x))").Limit("x", double.PositiveInfinity));
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("sin(1/x)").Limit("x", 0));

        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("2^x - 3^x").Limit("x", double.PositiveInfinity));
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("x^pi - x^2").Limit("x", double.PositiveInfinity));
        Assert.Equal("0", ExprParser.Parse("2^x/3^x").Limit("x", double.PositiveInfinity).Print());
        Assert.Equal("3", ExprParser.Parse("(2^x + 3^x)^(1/x)").Limit("x", double.PositiveInfinity).Print());

        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("exp((i - 1)*x)").Limit("x", double.PositiveInfinity));

        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("sqrt(x - a)").Limit("x", ExprParser.Parse("a")));
        Assert.Equal("0", ExprParser.Parse("sqrt(x - a)").Limit("x", ExprParser.Parse("a"), LimitDirection.Right).Print());

        Assert.Equal("tan(15707963267949/10000000000000)", ExprParser.Parse("tan(x)").Limit("x", Math.PI / 2).Print());
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
