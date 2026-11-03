using Epsilon.Calculus;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.Calculus;

public class LineIntegralTests
{
    private static readonly Curve Circle = C("[cos(t), sin(t)]", "0", "2*pi");
    private static readonly Curve Square = Curve.Polygon([[0, 0], [1, 0], [1, 1], [0, 1]], closed: true);

    private static Curve C(string point, string from, string to) =>
        new("t", Vector<Expr>.Parse(point), ExprParser.Parse(from), ExprParser.Parse(to));

    private static string[] Names(string variables) => variables.Split(',');

    [Theory]
    [InlineData("1", "[cos(t), sin(t)]", "0", "2*pi", "x,y", "2 * π")]
    [InlineData("x^2 + y^2", "[cos(t), sin(t)]", "0", "2*pi", "x,y", "2 * π")]
    [InlineData("1", "[2*cos(t), 2*sin(t)]", "0", "2*pi", "x,y", "4 * π")]
    [InlineData("1", "[R*cos(t), R*sin(t)]", "0", "2*pi", "x,y", "2abs(R) * π")]
    [InlineData("1", "[cos(t), sin(t), t]", "0", "2*pi", "x,y,z", "2 * π * sqrt(2)")]
    [InlineData("z", "[cos(t), sin(t), t]", "0", "2*pi", "x,y,z", "2π^2 * sqrt(2)")]
    [InlineData("1", "[a*cos(t), a*sin(t), b*t]", "0", "2*pi", "x,y,z", "2 * π * sqrt(a^2 + b^2)")]
    [InlineData("1", "[t, t^2]", "0", "1", "x,y", "(1/4) * asinh(2) + (1/2) * sqrt(5)")]
    [InlineData("x", "[t, t^2]", "0", "1", "x,y", "(1/12) * (5sqrt(5) - 1)")]
    [InlineData("1", "[t, cosh(t)]", "0", "1", "x,y", "sinh(1)")]
    [InlineData("1", "[cosh(t), t]", "0", "1", "x,y", "sinh(1)")]
    [InlineData("1", "[2*t, 2*cosh(t)]", "0", "1", "x,y", "2sinh(1)")]
    [InlineData("1", "[R*sin(t), R*cos(t)]", "0", "2*pi", "x,y", "2abs(R) * π")]
    [InlineData("1", "[b*t, a*sin(t), a*cos(t)]", "0", "1", "x,y,z", "sqrt(a^2 + b^2)")]
    [InlineData("x^2", "[t]", "0", "3", "x", "9")]
    public void By_arc_length(string f, string point, string from, string to, string variables, string integral) =>
        Assert.Equal(integral, ExprParser.Parse(f).LineIntegralExact(C(point, from, to), Names(variables)).Print());

    [Theory]
    [InlineData("[-y, x]", "2 * π")]
    [InlineData("[-y/(x^2 + y^2), x/(x^2 + y^2)]", "2 * π")]
    [InlineData("[y, x]", "0")]
    [InlineData("[x^2, y^2]", "0")]
    public void Around_the_unit_circle(string field, string integral) =>
        Assert.Equal(integral, Vector<Expr>.Parse(field).LineIntegralExact(Circle, "x", "y").Print());

    [Fact]
    public void Along_curves_of_pieces()
    {
        var swirl = Vector<Expr>.Parse("[-y, x]");
        Assert.Equal("2", swirl.LineIntegralExact(Square, "x", "y").Print());
        Assert.Equal("-2", swirl.LineIntegralExact(Square.Reversed(), "x", "y").Print());
        Assert.Equal("-2 * π", swirl.LineIntegralExact(Circle.Reversed(), "x", "y").Print());
        Assert.Equal("2 * π * R^2", swirl.LineIntegralExact(C("[R*cos(t), R*sin(t)]", "0", "2*pi"), "x", "y").Print());

        var there = Curve.Segment([0, 0], [1, 2]);
        Assert.Equal("0", swirl.LineIntegralExact(Curve.Join(there, Curve.Segment([1, 2], [0, 0])), "x", "y").Print());
        Assert.Equal("2", Vector<Expr>.Parse("[2*x*y, x^2]").LineIntegralExact(there, "x", "y").Print());
        Assert.Equal("2 * π + 2π^2", Vector<Expr>.Parse("[-y, x, z]").LineIntegralExact(C("[cos(t), sin(t), t]", "0", "2*pi"), "x", "y", "z").Print());
        Assert.Equal("sqrt(2) + 2", ExprParser.Parse("1").LineIntegralExact(Curve.Polygon([[0, 0], [1, 0], [0, 1]], closed: true), "x", "y").Print());
        Assert.Equal("3/2", ExprParser.Parse("x").LineIntegralExact(Curve.Polygon([[0, 0], [1, 0], [1, 1]]), "x", "y").Print());
    }

    // By arc length the direction doesn't count: the parabola backwards has the same integral.
    [Fact]
    public void Reversed_traverses_the_other_way()
    {
        Curve parabola = new("x", Vector<Expr>.Parse("[x, x^2]"), 0, 1);
        Assert.Equal(
            ExprParser.Parse("x^2").LineIntegral(parabola, "x", "y"),
            ExprParser.Parse("x^2").LineIntegralExact(parabola.Reversed(), "x", "y").Evaluate(), 1e-12);

        Assert.Equal("[1, 0]", Circle.Reversed().Start.Print());
        Assert.Equal("[0, 0]", Square.Reversed().Start.Print());
        Assert.Equal("[0, 0]", Curve.Segment([0, 0], [1, 2]).Reversed().End.Print());
    }

    [Fact]
    public void Numeric_line_integrals_agree_with_the_exact_ones()
    {
        Assert.Equal(2 * Math.PI, ExprParser.Parse("1").LineIntegral(Circle, "x", "y"), 1e-14);
        Assert.Equal(2 * Math.PI, Vector<Expr>.Parse("[-y, x]").LineIntegral(Circle, "x", "y"), 1e-14);
        Assert.Equal(2, Vector<Expr>.Parse("[-y, x]").LineIntegral(Square, "x", "y"), 1e-14);

        // The length of an ellipse is an elliptic integral.
        Curve ellipse = C("[2*cos(t), sin(t)]", "0", "2*pi");
        Assert.Equal(9.688448220547672, ExprParser.Parse("1").LineIntegral(ellipse, "x", "y"), 1e-12);
        Assert.Throws<NotSupportedException>(() => ExprParser.Parse("1").LineIntegralExact(ellipse, "x", "y"));
    }

    [Fact]
    public void The_ends_and_the_dimension()
    {
        Assert.Equal(2, Circle.Dimension);
        Assert.Equal("[1, 0]", Circle.Start.Print());
        Assert.Equal("[1, 0]", Circle.End.Print());
        Assert.Equal("[0, 0]", Square.End.Print());
        Assert.Equal("[1, 2]", Curve.Segment([0, 0], [1, 2]).End.Print());
    }

    [Fact]
    public void Wrong_curves_and_variables_throw()
    {
        Assert.Throws<ArgumentException>(() => C("[t, t]", "1", "0"));
        Assert.Throws<ArgumentException>(() => C("[t, t]", "0", "t"));
        Assert.Throws<ArgumentException>(() => new Curve("t", Vector.Create<Expr>(), 0, 1));
        Assert.Throws<ArgumentException>(() => Curve.Polygon([[0, 0]]));
        Assert.Throws<ArgumentException>(() => Curve.Segment([0, 0], [1, 2, 3]));
        Assert.Throws<ArgumentException>(() => Curve.Join(Circle, C("[cos(t), sin(t), t]", "0", "1")));

        Assert.Throws<ArgumentException>(() => ExprParser.Parse("1").LineIntegralExact(Circle, "x", "y", "z"));
        Assert.Throws<ArgumentException>(() => Vector<Expr>.Parse("[1, 2, 3]").LineIntegralExact(Circle, "x", "y"));
        Assert.Equal("The curve's points have the variable y in them: name the parameter and the variables apart. (Parameter 'curve')",
            Assert.Throws<ArgumentException>(() => ExprParser.Parse("1").LineIntegralExact(C("[y, t]", "0", "1"), "x", "y")).Message);
        Assert.Equal("The integrand has the variable R besides those of the curve: substitute a value for it first.",
            Assert.Throws<ArgumentException>(() => ExprParser.Parse("1").LineIntegral(C("[R*cos(t), R*sin(t)]", "0", "2*pi"), "x", "y")).Message);
    }

    // The curve's parameter may share a name with a variable: the graph y = x^2 by x.
    [Fact]
    public void The_parameter_may_be_named_as_a_variable() =>
        Assert.Equal("(1/12) * (5sqrt(5) - 1)", ExprParser.Parse("x").LineIntegralExact(new Curve("x", Vector<Expr>.Parse("[x, x^2]"), 0, 1), "x", "y").Print());

    [Theory]
    [InlineData("[2*x*y, x^2]", "x,y", "x^2 * y")]
    [InlineData("[y*z, x*z, x*y]", "x,y,z", "x * y * z")]
    [InlineData("[cos(x)*y, sin(x) + 2*y]", "x,y", "y^2 + sin(x) * y")]
    [InlineData("[exp(x)*sin(y), exp(x)*cos(y)]", "x,y", "exp(x) * sin(y)")]
    [InlineData("[a*y, a*x]", "x,y", "a * x * y")]
    [InlineData("[x/sqrt(x^2 + y^2 + z^2), y/sqrt(x^2 + y^2 + z^2), z/sqrt(x^2 + y^2 + z^2)]", "x,y,z", "sqrt(x^2 + y^2 + z^2)")]
    public void Potentials(string field, string variables, string potential)
    {
        var f = Vector<Expr>.Parse(field);
        Expr phi = f.Potential(Names(variables));
        Assert.Equal(potential, phi.Print());

        string[] names = Names(variables);
        for (int i = 0; i < names.Length; i++)
            Assert.Equal(0, (phi.Differentiate(names[i]) - f[i]).Evaluate([.. names.Select((name, k) => (name, 0.3 + 0.2 * k)), ("a", 1.5)]), 1e-12);
    }

    [Fact]
    public void The_work_of_a_gradient_is_the_difference_of_its_potential()
    {
        var field = Vector<Expr>.Parse("[y*z, x*z, x*y]");
        Curve path = C("[cos(t), sin(t), t]", "0", "pi");
        Expr phi = field.Potential("x", "y", "z");
        Expr difference = (phi.Substitute("x", path.End[0]).Substitute("y", path.End[1]).Substitute("z", path.End[2])
            - phi.Substitute("x", path.Start[0]).Substitute("y", path.Start[1]).Substitute("z", path.Start[2])).Simplify();
        Assert.Equal(difference.Print(), field.LineIntegralExact(path, "x", "y", "z").Print());
    }

    [Fact]
    public void A_field_that_isnt_a_gradient_has_no_potential()
    {
        Assert.Equal("The field [-y, x] isn't a gradient: the derivative of -y by y isn't that of x by x.",
            Assert.Throws<InvalidOperationException>(() => Vector<Expr>.Parse("[-y, x]").Potential("x", "y")).Message);
        Assert.Throws<NotSupportedException>(() => Vector<Expr>.Parse("[a*y, x]").Potential("x", "y"));
        Assert.Throws<ArgumentException>(() => Vector<Expr>.Parse("[x, y]").Potential("x", "y", "z"));
    }
}
