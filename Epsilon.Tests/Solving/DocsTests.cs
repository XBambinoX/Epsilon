using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

// Epsilon.Solving/docs/api-reference.md must list every public type and member. A new public
// member fails this test until it is documented there.
public class SolvingApiReferenceDocTests
{
    [Fact]
    public void Every_public_type_and_member_is_listed() =>
        Assert.Empty(Core.ApiReferencePages.Missing(typeof(Equation).Assembly, "Epsilon.Solving", "docs", "api-reference.md"));
}

public class SolvingDocsLinksTests
{
    [Fact]
    public void Relative_links_resolve() =>
        Assert.Empty(LinearAlgebra.MarkdownLinks.Broken(Path.Combine("Epsilon.Solving", "docs")));
}

internal static class Docs
{
    public static string Solved(string equation) => Equation.Parse(equation).Solve().Print();

    public static string Solved(string equation, string variable, Assumptions? assumptions = null) =>
        Equation.Parse(equation).Solve(variable, assumptions).Print();
}

// Every example in Epsilon.Solving/README.md, with the result its comment shows.
public class SolvingReadmeExamplesTests
{
    [Fact]
    public void Header()
    {
        Assert.Equal("{2, 3}", Docs.Solved("x^2 - 5x + 6 = 0"));
        Assert.Equal("{π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z}", Docs.Solved("sin(x) = 1/2"));
        Assert.Equal("{b / a} if a != 0", Docs.Solved("a*x = b", "x"));
        Assert.Equal("{1.1673039782614187}", Equation.Parse("x^5 - x - 1 = 0").SolveNumerically().Print());
    }

    [Fact]
    public void Quick_start()
    {
        Assert.Equal("{1, 2, 3}", Docs.Solved("x^3 - 6x^2 + 11x - 6 = 0"));
        Assert.Equal("{3}", Docs.Solved("sqrt(x + 1) = x - 1"));
        Assert.Equal("{0, 1}", Docs.Solved("4^x - 3*2^x + 2 = 0"));

        Assert.Equal("{-sqrt(2E / m), sqrt(2E / m)} if m != 0 and E / m >= 0", Docs.Solved("E = m*v^2/2", "v"));

        SolutionSet sines = Equation.Parse("sin(x) = 1/2").Solve();
        Assert.Equal("{π / 6, 5 * π / 6, 13 * π / 6, 17 * π / 6}", sines.Within(0, 10).Print());

        Assert.Equal("{0.7390851332151607}", Equation.Parse("x = cos(x)").SolveNumerically(-10, 10).Print());
    }

    [Fact]
    public void Equations_and_solution_sets()
    {
        SolutionSet set = Equation.Parse("x = cos(x)").Solve();
        Assert.False(set.IsComplete);
        Assert.Equal("x - cos(x)", set.Unsolved[0].Print());
        Assert.Equal("{...}", set.Print());

        Assert.Equal("(-inf, inf) if x != 0", Docs.Solved("x/x = 1"));
        Assert.Equal("{-1}", Docs.Solved("(x^2 - 1)/(x - 1) = 0"));
    }

    [Fact]
    public void Polynomials()
    {
        Assert.Equal("{2cos(10 * π / 9), 2cos(4 * π / 9), 2cos(2 * π / 9)}", Docs.Solved("x^3 - 3x + 1 = 0"));
        Assert.Equal("{nthroot(2, 3)}", Docs.Solved("x^3 = 2"));
        Assert.Equal("{...}", Docs.Solved("x^5 - x - 1 = 0"));
        Assert.Equal("{-i, i}", Equation.Parse("x^2 + 1 = 0").SolveComplex().Print());
    }

    [Fact]
    public void Functions_and_families_of_solutions()
    {
        Assert.Equal("{1 + e}", Docs.Solved("3ln(x - 1) + 2 = 5"));
        Assert.Equal("{-1, 3}", Docs.Solved("abs(x - 1) = 2"));
        Assert.Equal("{}", Docs.Solved("cos(x) = 2"));
        Assert.Equal("{π * k | k in Z, k != 0}", Docs.Solved("sin(x)/x = 0"));

        Solution family = Equation.Parse("sin(x) = 1/2").Solve().Solutions[0];
        Assert.Equal("2 * π", family.Period!.Print());
        Assert.Equal("13 * π / 6", family.At(1)!.Print());
    }

    [Fact]
    public void Roots_of_the_unknown_and_substitutions()
    {
        Assert.Equal("{16/9}", Docs.Solved("sqrt(x) + sqrt(x + 1) = 3"));
        Assert.Equal("{π / 2 + 2 * π * k | k in Z}", Docs.Solved("sin(x)^2 - 3*sin(x) + 2 = 0"));
        Assert.Equal("{-ln(sqrt(3) + 2), -ln(2 - sqrt(3))}", Docs.Solved("exp(x) + exp(-x) = 4"));
        Assert.Equal("{1}", Docs.Solved("x^(2/3) + x^(1/3) = 2"));
    }

    [Fact]
    public void Parameters()
    {
        Assert.Equal(
            "{(-b - sqrt(b^2 - 4a * c)) / (2a), (-b + sqrt(b^2 - 4a * c)) / (2a)} if a != 0 and b^2 - 4a * c >= 0",
            Docs.Solved("a*x^2 + b*x + c = 0", "x"));
        Assert.Equal("{4π^2 * L / T^2} if T > 0 and L != 0", Docs.Solved("T = 2*pi*sqrt(L/g)", "g"));
        Assert.Equal("{-sqrt(a), sqrt(a)}", Docs.Solved("x^2 = a", "x", Assumptions.None.AssumePositive("a")));
    }

    [Fact]
    public void Numeric_solutions()
    {
        Assert.Equal("{-1.8414056604369606, 1.1461932206205825}",
            Equation.Parse("exp(x) = x + 2").SolveNumerically(double.NegativeInfinity, double.PositiveInfinity).Print());
        Assert.Equal(0.7390851332151607, Equation.Parse("x = cos(x)").FindRoot(1));
        Assert.Equal("{-0.662358978622373 - 0.5622795120623012i, -0.662358978622373 + 0.5622795120623012i, 1.324717957244746}",
            Equation.Parse("x^3 - x - 1 = 0").SolveComplexNumerically().Print());
    }

    [Fact]
    public void Good_to_know() =>
        Assert.Equal("{phi / omega} if omega != 0", Equation.Parse("omega*t = phi", "omega", "t", "phi").Solve("t").Print());
}

public class SolvingGettingStartedDocTests
{
    [Fact]
    public void A_first_equation()
    {
        Equation equation = Equation.Parse("x^2 - 2x - 1 = 0");
        SolutionSet solutions = equation.Solve();

        Assert.Equal("{1 - sqrt(2), sqrt(2) + 1}", solutions.Print());
        Assert.Equal(-0.41421356237309515, solutions.Solutions[0].Value.Evaluate());

        Assert.Equal("{-5 * π / 6 + 2 * π * k, -π / 6 + 2 * π * k | k in Z}", Docs.Solved("2sin(x) + 1 = 0"));
    }

    [Fact]
    public void Other_variables() => Assert.Equal("{-sqrt(a), sqrt(a)} if a >= 0", Docs.Solved("x^2 = a", "x"));

    [Fact]
    public void Numbers()
    {
        Assert.Equal("{...}", Docs.Solved("x^5 - x - 1 = 0"));
        Assert.Equal("{1.1673039782614187}", Equation.Parse("x^5 - x - 1 = 0").SolveNumerically().Print());
        Assert.Equal("{0.7390851332151607}", Equation.Parse("x = cos(x)").SolveNumerically(-10, 10).Print());
    }
}

public class SolvingSolutionSetsDocTests
{
    [Fact]
    public void Writing_an_equation()
    {
        Equation sine = Equation.Parse("sin(x) = 1/2");
        Equation phase = Equation.Parse("omega*t = phi", "omega", "t", "phi");
        Equation square = new Equation(ExprParser.Parse("x^2"), 4);

        Assert.Equal("sin(x) = 1 / 2", sine.Print());
        Assert.Equal("\\sin\\left(x\\right) = \\frac{1}{2}", sine.ToLatex());

        Assert.Equal("{-2, 2}", square.Solve().Print());
        Assert.Equal("{phi / omega} if omega != 0", phase.Solve("t").Print());
    }

    [Fact]
    public void The_solutions()
    {
        SolutionSet set = Equation.Parse("x^3 - x^2 - x + 1 = 0").Solve();

        Assert.Equal("{-1, 1}", set.Print());
        Assert.Equal(2, set.Solutions.Count);
        Assert.Equal("1", set.Solutions[1].Value.Print());
    }

    [Fact]
    public void What_isnt_solved()
    {
        SolutionSet partly = Equation.Parse("x*(x - cos(x)) = 0").Solve();

        Assert.Equal("{0, ...}", partly.Print());
        Assert.False(partly.IsComplete);
        Assert.Equal("x - cos(x)", partly.Unsolved[0].Print());
    }

    [Fact]
    public void No_solution_or_every_value()
    {
        Assert.True(Equation.Parse("cos(x) = 2").Solve().IsEmpty);
        Assert.Equal("{}", Docs.Solved("cos(x) = 2"));

        SolutionSet always = Equation.Parse("x + 1 = 1 + x").Solve();
        Assert.True(always.IsEveryValue);
        Assert.Equal("(-inf, inf)", always.Print());

        Assert.Equal("(-inf, inf) if a = b", Docs.Solved("a = b", "x"));
    }

    [Fact]
    public void The_domain()
    {
        Assert.Equal("(-inf, inf) if x != 0", Docs.Solved("x/x = 1"));
        Assert.Equal("{-1}", Docs.Solved("(x^2 - 1)/(x - 1) = 0"));
        Assert.Equal("{}", Docs.Solved("1/x = 0"));
        Assert.Equal("{exp(2)}", Docs.Solved("ln(x) = 2"));
    }

    [Fact]
    public void Assumptions_about_the_unknown()
    {
        var positive = Assumptions.None.AssumePositive("x");

        Assert.Equal("{2}", Docs.Solved("x^2 = 4", "x", positive));
        Assert.Equal("{sqrt(2) + 1}", Docs.Solved("x^2 - 2x - 1 = 0", "x", positive));
    }

    [Fact]
    public void Text_and_LaTeX()
    {
        Assert.Equal("\\left\\{1 - \\sqrt{2}, \\sqrt{2} + 1\\right\\}", Equation.Parse("x^2 - 2x - 1 = 0").Solve().ToLatex());
        Assert.Equal("\\emptyset", Equation.Parse("cos(x) = 2").Solve().ToLatex());
    }
}

public class SolvingPolynomialsDocTests
{
    [Fact]
    public void Linear_and_quadratic_equations()
    {
        Assert.Equal("{-5}", Docs.Solved("2(x - 1) = 3(x + 1)"));
        Assert.Equal("{2, 3}", Docs.Solved("x^2 - 5x + 6 = 0"));
        Assert.Equal("{1 - sqrt(2), sqrt(2) + 1}", Docs.Solved("x^2 - 2x - 1 = 0"));
        Assert.Equal("{}", Docs.Solved("x^2 + 1 = 0"));
    }

    [Fact]
    public void Higher_degrees()
    {
        Assert.Equal("{1, 2, 3}", Docs.Solved("x^3 - 6x^2 + 11x - 6 = 0"));
        Assert.Equal("{-2, -1, 1, 2}", Docs.Solved("x^4 - 5x^2 + 4 = 0"));
        Assert.Equal("{nthroot(2, 3)}", Docs.Solved("x^3 = 2"));
        Assert.Equal("{nthroot(sqrt(93) / 18 - 1/2, 3) - nthroot(sqrt(93) / 18 + 1/2, 3)}", Docs.Solved("x^3 + x + 1 = 0"));
        Assert.Equal("{2cos(10 * π / 9), 2cos(4 * π / 9), 2cos(2 * π / 9)}", Docs.Solved("x^3 - 3x + 1 = 0"));

        Assert.Equal("{...}", Docs.Solved("x^5 - x - 1 = 0"));
        Assert.Equal("{nthroot(2, 5)}", Docs.Solved("x^5 = 2"));
    }

    [Fact]
    public void Repeated_roots()
    {
        Assert.Equal("{1}", Docs.Solved("(x - 1)^3 = 0"));
        Assert.Equal("{-sqrt(2), sqrt(2)}", Docs.Solved("x^4 - 4x^2 + 4 = 0"));
    }

    [Fact]
    public void Rational_equations()
    {
        Assert.Equal("{1 - sqrt(2), sqrt(2) + 1}", Docs.Solved("1/(x - 1) + 1/(x + 1) = 1"));
        Assert.Equal("{1}", Docs.Solved("x + 1/x = 2"));
        Assert.Equal("{-1}", Docs.Solved("(x^2 - 1)/(x - 1) = 0"));
    }

    [Fact]
    public void Over_the_complex_numbers()
    {
        string Complex(string equation) => Equation.Parse(equation).SolveComplex().Print();

        Assert.Equal("{-i, i}", Complex("x^2 + 1 = 0"));
        Assert.Equal("{1 - 2 * i, 1 + 2 * i}", Complex("x^2 = 2x - 5"));
        Assert.Equal("{-i - 1, i - 1, 1 - i, 1 + i}", Complex("x^4 + 4 = 0"));
        Assert.Equal("{-1, 1/2 - i * sqrt(3) / 2, i * sqrt(3) / 2 + 1/2}", Complex("x^3 + 1 = 0"));
    }
}

public class SolvingFunctionsDocTests
{
    [Fact]
    public void Undoing_a_function()
    {
        Assert.Equal("{ln(5)}", Docs.Solved("exp(x) = 5"));
        Assert.Equal("{}", Docs.Solved("exp(x) = -1"));
        Assert.Equal("{1 + e}", Docs.Solved("3ln(x - 1) + 2 = 5"));
        Assert.Equal("{3}", Docs.Solved("10^x = 1000"));
        Assert.Equal("{ln(5) / ln(2)}", Docs.Solved("2^x = 5"));

        Assert.Equal("{9}", Docs.Solved("sqrt(x) = 3"));
        Assert.Equal("{-8}", Docs.Solved("nthroot(x, 3) = -2"));
        Assert.Equal("{-1, 3}", Docs.Solved("abs(x - 1) = 2"));
        Assert.Equal("{-1, 3}", Docs.Solved("(x - 1)^4 = 16"));

        Assert.Equal("{tan(1)}", Docs.Solved("atan(x) = 1"));
        Assert.Equal("{}", Docs.Solved("atan(x) = 2"));
        Assert.Equal("{-acosh(2), acosh(2)}", Docs.Solved("cosh(x) = 2"));
        Assert.Equal("{}", Docs.Solved("tanh(x) = 2"));
    }

    [Fact]
    public void Families_of_solutions()
    {
        Assert.Equal("{π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z}", Docs.Solved("sin(x) = 1/2"));
        Assert.Equal("{-π / 4 + π * k | k in Z}", Docs.Solved("tan(x) + 1 = 0"));
        Assert.Equal("{π / 4 + π * k | k in Z}", Docs.Solved("sin(2x) = 1"));
        Assert.Equal("{}", Docs.Solved("cos(x) = 2"));
        Assert.Equal("{π * k | k in Z}", Docs.Solved("sin(x) = 0"));

        Solution family = Equation.Parse("sin(x) = 1/2").Solve().Solutions[1];
        Assert.Equal("2 * π * k + 5 * π / 6", family.Value.Print());
        Assert.Equal("5 * π / 6 + 2 * π * k", family.Print());
        Assert.Equal("2 * π", family.Period!.Print());
        Assert.Equal("k", family.Index);
    }

    [Fact]
    public void Conditions_on_the_index() => Assert.Equal("{π * k | k in Z, k != 0}", Docs.Solved("sin(x)/x = 0"));

    [Fact]
    public void One_value_of_a_family()
    {
        Solution multiples = Equation.Parse("sin(x)/x = 0").Solve().Solutions[0];
        Solution family = Equation.Parse("sin(x) = 1/2").Solve().Solutions[1];

        Assert.Equal("2 * π", multiples.At(2)!.Print());
        Assert.Null(multiples.At(0));
        Assert.Equal("-7 * π / 6", family.At(-1)!.Print());
    }

    [Fact]
    public void The_values_in_a_range()
    {
        SolutionSet sines = Equation.Parse("sin(x) = 1/2").Solve();

        Assert.Equal("{π / 6, 5 * π / 6, 13 * π / 6, 17 * π / 6}", sines.Within(0, 10).Print());
        Assert.Equal("{-π, 0, π}", Equation.Parse("sin(x) = 0").Solve().Within(-4, 4).Print());
        Assert.Equal("{π * k | k in Z, k >= 0, k <= 318309}", Equation.Parse("sin(x) = 0").Solve().Within(0, 1e6).Print());
        Assert.Equal("{0, π, 2 * π}", Equation.Parse("sin(x) = 0").Solve().Within(0, 2 * Math.PI).Print());
    }
}

public class SolvingRootsAndSubstitutionsDocTests
{
    [Fact]
    public void Raising_to_powers()
    {
        Assert.Equal("{3}", Docs.Solved("sqrt(x + 1) = x - 1"));
        Assert.Equal("{16/9}", Docs.Solved("sqrt(x) + sqrt(x + 1) = 3"));
        Assert.Equal("{}", Docs.Solved("sqrt(x) + sqrt(x + 1) = 0"));
        Assert.Equal("{-1, 0, 1}", Docs.Solved("nthroot(x, 3) = x"));
        Assert.Equal("{9/2 - sqrt(17) / 2}", Docs.Solved("sqrt(x + sqrt(x)) = 2"));
    }

    [Fact]
    public void Substitutions()
    {
        Assert.Equal("{π / 2 + 2 * π * k | k in Z}", Docs.Solved("sin(x)^2 - 3*sin(x) + 2 = 0"));
        Assert.Equal("{-2 * π / 3 + 2 * π * k, 2 * π * k, 2 * π / 3 + 2 * π * k | k in Z}", Docs.Solved("2*cos(x)^2 - cos(x) - 1 = 0"));
        Assert.Equal("{π * k / 2 | k in Z}", Docs.Solved("sin(x)^3 = sin(x)"));
        Assert.Equal("{exp(-1), exp(2)}", Docs.Solved("ln(x)^2 = ln(x) + 2"));
    }

    [Fact]
    public void Several_powers_of_one_function()
    {
        Assert.Equal("{0, ln(2)}", Docs.Solved("exp(2*x) - 3*exp(x) + 2 = 0"));
        Assert.Equal("{0, 1}", Docs.Solved("4^x - 3*2^x + 2 = 0"));
        Assert.Equal("{2}", Docs.Solved("2^(x + 1) + 2^x = 12"));
        Assert.Equal("{-1, 1}", Docs.Solved("2^x + 2^(-x) = 5/2"));
        Assert.Equal("{-ln(sqrt(3) + 2), -ln(2 - sqrt(3))}", Docs.Solved("exp(x) + exp(-x) = 4"));

        Assert.Equal("{1, 4}", Docs.Solved("x - 3*sqrt(x) + 2 = 0"));
        Assert.Equal("{3}", Docs.Solved("sqrt(x + 1) + x = 5"));
        Assert.Equal("{1, 8}", Docs.Solved("x^(2/3) - 3*x^(1/3) + 2 = 0"));
        Assert.Equal("{1}", Docs.Solved("x^(2/3) + x^(1/3) = 2"));
    }

    [Fact]
    public void What_isnt_substituted()
    {
        Assert.Equal("{...}", Docs.Solved("sin(x)^2 + sin(x) = x"));
        Assert.Equal("{...}", Docs.Solved("sin(x)^2 + cos(x) = 0"));
        Assert.Equal("{...}", Docs.Solved("exp(x) = 2^x"));
        Assert.Equal("{0.15469679775892686}", Equation.Parse("exp(x)^5 - exp(x) - 1 = 0").SolveNumerically().Print());
    }
}

public class SolvingParametersDocTests
{
    [Fact]
    public void Other_variables()
    {
        Assert.Equal("{-a}", Docs.Solved("x + a = 0", "x"));
        Assert.Equal("{b / a} if a != 0", Docs.Solved("a*x = b", "x"));
        Assert.Equal("{-sqrt(a), sqrt(a)} if a >= 0", Docs.Solved("x^2 = a", "x"));

        Condition condition = Equation.Parse("x^2 = a").Solve("x").Solutions[0].Conditions[0];
        Assert.Equal("a", condition.Left.Print());
        Assert.Equal(Comparison.GreaterOrEqual, condition.Comparison);
        Assert.Equal("0", condition.Right.Print());
    }

    [Fact]
    public void Formulas()
    {
        Assert.Equal("{-sqrt(2E / m), sqrt(2E / m)} if m != 0 and E / m >= 0", Docs.Solved("E = m*v^2/2", "v"));
        Assert.Equal("{4π^2 * L / T^2} if T > 0 and L != 0", Docs.Solved("T = 2*pi*sqrt(L/g)", "g"));
        Assert.Equal("{-f * y / (f - y)} if f != y and f != 0 and y != 0", Docs.Solved("1/x + 1/y = 1/f", "x"));
        Assert.Equal("{ln(A / P) / ln(r + 1)} if r != 0 and P != 0 and A / P > 0 and r > -1", Docs.Solved("A = P*(1 + r)^n", "n"));
    }

    [Fact]
    public void Quadratics()
    {
        Assert.Equal(
            "{(-b - sqrt(b^2 - 4a * c)) / (2a), (-b + sqrt(b^2 - 4a * c)) / (2a)} if a != 0 and b^2 - 4a * c >= 0",
            Docs.Solved("a*x^2 + b*x + c = 0", "x"));
        Assert.Equal("{a - sqrt(a^2 - 1), a + sqrt(a^2 - 1)} if abs(a) >= 1", Docs.Solved("x^2 - 2*a*x + 1 = 0", "x"));
        Assert.Equal("{b, a}", Docs.Solved("x^2 - (a + b)*x + a*b = 0", "x"));
    }

    [Fact]
    public void Functions_with_parameters()
    {
        Assert.Equal("{ln(b) / a} if a != 0 and b > 0", Docs.Solved("exp(a*x) = b", "x"));
        Assert.Equal("{acos(1 / a) + 2 * π * k, -acos(1 / a) + 2 * π * k | k in Z} if abs(a) >= 1", Docs.Solved("cos(x) = 1/a", "x"));
        Assert.Equal("{π / (6a) + 2 * π * k / a, 5 * π / (6a) + 2 * π * k / a | k in Z} if a != 0", Docs.Solved("sin(a*x) = 1/2", "x"));
    }

    [Fact]
    public void The_form_of_the_conditions()
    {
        Assert.Equal("{(a - 1)^2 / 4} if a >= -1 and a <= 1", Docs.Solved("sqrt(x) + sqrt(x + a) = 1", "x"));
        Assert.Equal("{0} if a = 0", Docs.Solved("x^2 + a^2 = 0", "x"));
    }

    [Fact]
    public void Assumptions_about_parameters()
    {
        var positive = Assumptions.None.AssumePositive("a");

        Assert.Equal("{b / a}", Docs.Solved("a*x = b", "x", positive));
        Assert.Equal("{-sqrt(a), sqrt(a)}", Docs.Solved("x^2 = a", "x", positive));
        Assert.Equal("{}", Docs.Solved("x^2 = a", "x", Assumptions.None.AssumeNegative("a")));
    }
}

public class SolvingNumericSolutionsDocTests
{
    [Fact]
    public void Solutions_as_numbers()
    {
        Assert.Equal("{1.1673039782614187}", Equation.Parse("x^5 - x - 1 = 0").SolveNumerically().Print());

        NumericSolutionSet<double> roots = Equation.Parse("x^2 = 2").SolveNumerically();
        Assert.Equal(1.4142135623730951, roots.Values[1]);
        Assert.True(roots.IsComplete);

        Assert.Equal("{...}", Equation.Parse("sin(x) = 1/2").SolveNumerically().Print());
        Assert.Equal("{...}", Equation.Parse("x = cos(x)").SolveNumerically().Print());
    }

    [Fact]
    public void In_a_range()
    {
        Assert.Equal("{0.5235987755982989, 2.6179938779914944, 6.806784082777885, 8.901179185171081}",
            Equation.Parse("sin(x) = 1/2").SolveNumerically(0, 10).Print());
        Assert.Equal("{0.7390851332151607}", Equation.Parse("x = cos(x)").SolveNumerically(-10, 10).Print());
        Assert.Equal("{0, 4.493409457909064, 7.725251836937707}", Equation.Parse("tan(x) = x").SolveNumerically(0, 10).Print());
        Assert.Equal("{-1.8414056604369606, 1.1461932206205825}",
            Equation.Parse("exp(x) = x + 2").SolveNumerically(double.NegativeInfinity, double.PositiveInfinity).Print());
    }

    [Fact]
    public void Near_a_guess()
    {
        Assert.Equal(0.7390851332151607, Equation.Parse("x = cos(x)").FindRoot(1));
        Assert.Null(Equation.Parse("x^2 + 1 = 0").FindRoot(1));
    }

    [Fact]
    public void The_complex_plane()
    {
        Assert.Equal("{-0.662358978622373 - 0.5622795120623012i, -0.662358978622373 + 0.5622795120623012i, 1.324717957244746}",
            Equation.Parse("x^3 - x - 1 = 0").SolveComplexNumerically().Print());
        Assert.Equal("{-6.283185307179586i, 0, 6.283185307179586i}",
            Equation.Parse("exp(x) = 1").SolveComplexNumerically(new ComplexNumber(-1, -10), new ComplexNumber(1, 10)).Print());
        Assert.Equal("1i", Equation.Parse("x^2 + 1 = 0").FindComplexRoot(new ComplexNumber(1, 1)).ToString());
    }

    [Fact]
    public void Other_variables_throw() =>
        Assert.Throws<InvalidOperationException>(() => Equation.Parse("x^2 = a").SolveNumerically("x"));
}

public class SolvingLimitationsDocTests
{
    [Fact]
    public void What_is_solved()
    {
        Assert.Equal("{...}", Docs.Solved("x^4 + x + 1 = 0"));
        Assert.Equal("{...}", Docs.Solved("x^5 - x - 1 = 0"));
        Assert.Equal("{-sqrt(8sqrt(6) + 20) / 2, -sqrt(20 - 8sqrt(6)) / 2, sqrt(20 - 8sqrt(6)) / 2, sqrt(8sqrt(6) + 20) / 2}",
            Docs.Solved("x^4 - 10x^2 + 1 = 0"));
        Assert.Equal("{...}", Docs.Solved("x = cos(x)"));
        Assert.Equal("{...}", Docs.Solved("x*exp(x) = 1"));
        Assert.Equal("{...}", Docs.Solved("x^x = 2"));
        Assert.Equal("{...}", Docs.Solved("ln(x) = ln(2x - 1)"));
        Assert.Equal("{...}", Docs.Solved("exp(x) = 2^x"));
        Assert.Equal("{...}", Docs.Solved("sqrt(x) + nthroot(x, 3) = x"));
        Assert.Equal("{...}", Equation.Parse("exp(x) = 1").SolveComplex().Print());
    }

    [Fact]
    public void Parameters()
    {
        Assert.Equal("{b / a} if a != 0", Docs.Solved("a*x = b", "x"));
        Assert.Equal("{(-2sqrt(a + 3) + 4) / 2 if a <= -2 and a >= -3, (2sqrt(a + 3) + 4) / 2 if a >= -3}", Docs.Solved("sqrt(2*x + a) = x - 1", "x"));
        Assert.Equal("{π * k / a | k in Z, k / a >= 0, π * k / a <= 10} if a != 0", Equation.Parse("sin(a*x) = 0").Solve("x").Within(0, 10).Print());
    }

    [Fact]
    public void Numeric_solutions()
    {
        Assert.Equal("{}", Equation.Parse("(x - cos(x))*(x - cos(x) - 1e-4) = 0").SolveNumerically(0, 1).Print());
        Assert.Single(Equation.Parse("(x - cos(x))*(x - cos(x) - 1e-7) = 0").SolveNumerically(0, 1).Values);
        Assert.Equal("{-500000013.6409661, 0.7390851332151607}",
            Equation.Parse("exp(x)*(x - cos(x)) = 0").SolveNumerically(double.NegativeInfinity, double.PositiveInfinity).Print());
    }
}
