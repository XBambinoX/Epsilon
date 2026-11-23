using Epsilon.Core;
using Epsilon.Solving;

namespace Epsilon.Tests.Solving;

// The general solution with the conditions on the other variables it needs; what they leave out,
// as a = 0 in a x = b, isn't solved on its own.
public class ParameterEquationTests
{
    private static string Printed(string equation, string variable = "x", Assumptions? assumptions = null) =>
        Equation.Parse(equation).Solve(variable, assumptions).Print();

    [Theory]
    [InlineData("a*x = b", "x", "{b / a} if a != 0")]
    [InlineData("a*x + b*x = c", "x", "{c / (a + b)} if a + b != 0")]
    [InlineData("(a - 1)*x = 2", "x", "{2 / (a - 1)} if a != 1")]
    [InlineData("(a^2 + 1)*x = 1", "x", "{1 / (a^2 + 1)}")]
    [InlineData("x/a = 1", "x", "{a} if a != 0")]
    [InlineData("F = m*a", "a", "{F / m} if m != 0")]
    [InlineData("v = u + a*t", "t", "{(v - u) / a} if a != 0")]
    public void Linear_equations_divide_by_a_slope_that_isnt_0(string equation, string variable, string expected) =>
        Assert.Equal(expected, Printed(equation, variable));

    [Theory]
    [InlineData("x^2 = a", "x", "{-sqrt(a), sqrt(a)} if a >= 0")]
    [InlineData("x^2 + 1 = a", "x", "{-sqrt(a - 1), sqrt(a - 1)} if a >= 1")]
    [InlineData("a*x^2 = 1", "x", "{-1 / sqrt(a), 1 / sqrt(a)} if a > 0")]
    [InlineData("E = m*v^2/2", "v", "{-sqrt(2E / m), sqrt(2E / m)} if m != 0 and E / m >= 0")]
    [InlineData("x^2 - 2*a*x + 1 = 0", "x", "{a - sqrt(a^2 - 1), a + sqrt(a^2 - 1)} if abs(a) >= 1")]
    [InlineData("x^2 + p*x + q = 0", "x", "{(-p - sqrt(p^2 - 4q)) / 2, (-p + sqrt(p^2 - 4q)) / 2} if p^2 - 4q >= 0")]
    [InlineData("a*x^2 + b*x + c = 0", "x",
        "{(-b - sqrt(b^2 - 4a * c)) / (2a), (-b + sqrt(b^2 - 4a * c)) / (2a)} if a != 0 and b^2 - 4a * c >= 0")]
    [InlineData("s = u*t + a*t^2/2", "t",
        "{(-u - sqrt(2a * s + u^2)) / a, (-u + sqrt(2a * s + u^2)) / a} if a != 0 and 2a * s + u^2 >= 0")]
    public void Quadratic_equations_need_a_discriminant_that_isnt_negative(string equation, string variable, string expected) =>
        Assert.Equal(expected, Printed(equation, variable));

    // sqrt((a - b)^2) would be abs(a - b), which the two signs before it make unnecessary.
    [Theory]
    [InlineData("x^2 - (a + b)*x + a*b = 0", "{b, a}")]
    [InlineData("x^2 - 2*a*x + a^2 - b^2 = 0", "{a - b, a + b}")]
    [InlineData("x^2 = a^2", "{-a, a}")]
    [InlineData("x^2 - 2*a*x + a^2 = 0", "{a}")]
    [InlineData("a*x^2 - (a + 1)*x + 1 = 0", "{1 / a, 1} if a != 0")]
    public void A_discriminant_that_is_a_square_needs_no_condition(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("a*x^2 + b*x = 0", "{0, -b / a if a != 0}")]
    [InlineData("x^3 - a*x = 0", "{0, -sqrt(a) if a >= 0, sqrt(a) if a >= 0}")]
    public void The_root_0_takes_no_condition_of_the_rest(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("x^3 = a", "{nthroot(a, 3)}")]
    [InlineData("x^4 = a", "{-nthroot(a, 4), nthroot(a, 4)} if a >= 0")]
    [InlineData("a*x^3 = b", "{nthroot(b / a, 3)} if a != 0")]
    [InlineData("x^5 = 2", "{nthroot(2, 5)}")]
    public void Powers_of_the_unknown_are_undone_by_roots(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    [Fact]
    public void The_assumptions_decide_conditions()
    {
        Assumptions none = Assumptions.None;

        Assert.Equal("{b / a}", Printed("a*x = b", assumptions: none.AssumePositive("a")));
        Assert.Equal("{-sqrt(a), sqrt(a)}", Printed("x^2 = a", assumptions: none.AssumePositive("a")));
        Assert.Equal("{}", Printed("x^2 = a", assumptions: none.AssumeNegative("a")));
        Assert.Equal("{sqrt(a)} if a > 0", Printed("x^2 = a", assumptions: none.AssumePositive("x")));
        Assert.Equal("{nthroot(a, 4)} if a > 0", Printed("x^4 = a", assumptions: none.AssumePositive("x")));
    }

    [Fact]
    public void Over_the_complex_numbers_only_the_leading_coefficient_is_a_condition()
    {
        Assert.Equal(
            "{(-b - sqrt(b^2 - 4a * c)) / (2a), (-b + sqrt(b^2 - 4a * c)) / (2a)} if a != 0",
            Equation.Parse("a*x^2 + b*x + c = 0").SolveComplex("x").Print());
        Assert.Equal("{-sqrt(a), sqrt(a)}", Equation.Parse("x^2 = a").SolveComplex("x").Print());
        Assert.Equal("{b / a} if a != 0", Equation.Parse("a*x = b").SolveComplex("x").Print());
    }

    // L / g = c has g = L / c, which is 0 where L is.
    [Theory]
    [InlineData("T = 2*pi*sqrt(L/g)", "g", "{4π^2 * L / T^2} if T > 0 and L != 0")]
    [InlineData("T = 2*pi*sqrt(L/g)", "L", "{T^2 * g / (4π^2)} if g != 0 and T >= 0")]
    [InlineData("exp(a*x) = b", "x", "{ln(b) / a} if a != 0 and b > 0")]
    [InlineData("a^x = b", "x", "{ln(b) / ln(a)} if a != 1 and b > 0 and a > 0")]
    [InlineData("abs(x) = a", "x", "{-a, a} if a >= 0")]
    [InlineData("1/x + 1/y = 1/f", "x", "{-f * y / (f - y)} if f != y and f != 0 and y != 0")]
    [InlineData("N = M*exp(-l*t)", "t", "{-ln(N / M) / l} if l != 0 and M != 0 and N / M > 0")]
    public void Functions_of_the_unknown_with_parameters(string equation, string variable, string expected) =>
        Assert.Equal(expected, Printed(equation, variable));

    // Where the parts of the equation without the unknown are defined.
    [Theory]
    [InlineData("x = ln(a)", "{ln(a)} if a > 0")]
    [InlineData("x = sqrt(a)", "{sqrt(a)} if a >= 0")]
    [InlineData("x + 1/a = 0", "{-1 / a} if a != 0")]
    [InlineData("x = ln(a) - ln(b)", "{ln(a) - ln(b)} if b > 0 and a > 0")]
    public void Parameters_are_where_the_equation_is_defined(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    [Theory]
    [InlineData("sin(x) = ln(a)", "{asin(ln(a)) + 2 * π * k, -asin(ln(a)) + π + 2 * π * k | k in Z} if a >= exp(-1) and a <= e")]
    [InlineData("exp(x) = ln(a)", "{ln(ln(a))} if a > 1")]
    public void Conditions_on_logarithms_are_on_their_arguments(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    // u^p, with p not a whole number, is defined for u > 0 only.
    [Theory]
    [InlineData("x^a = 8", "x", "{8^(1 / a)} if a != 0")]
    [InlineData("A = P*(1 + r)^n", "r", "{(A / P)^(1 / n) - 1} if P != 0 and A / P > 0 and n != 0")]
    [InlineData("A = P*(1 + r)^n", "n", "{ln(A / P) / ln(r + 1)} if r != 0 and P != 0 and A / P > 0 and r > -1")]
    public void Powers_of_any_degree(string equation, string variable, string expected) =>
        Assert.Equal(expected, Printed(equation, variable));

    [Theory]
    [InlineData("sin(a*x) = 1/2", "{π / (6a) + 2 * π * k / a, 5 * π / (6a) + 2 * π * k / a | k in Z} if a != 0")]
    [InlineData("sin(a*x) = 0", "{π * k / a | k in Z} if a != 0")]
    [InlineData("cos(x/a) = 1", "{2 * π * a * k | k in Z} if a != 0")]
    [InlineData("sin(a*x + b) = 1", "{(-b + π / 2) / a + 2 * π * k / a | k in Z} if a != 0")]
    [InlineData("sin(a*x)/x = 0", "{π * k / a | k in Z, k != 0} if a != 0")]
    public void Families_with_a_period_of_parameters(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    [Fact]
    public void A_period_of_parameters_has_either_sign()
    {
        Solution family = Equation.Parse("sin(a*x) = 1/2").Solve("x").Solutions[0];

        Assert.Equal("2 * π / a", family.Period!.Print());
        Assert.Equal("13 * π / (6a) if a != 0", family.At(1)!.Print());
    }

    // Which values fall in the range depends on a, so the family stays one, with conditions.
    [Fact]
    public void A_range_keeps_a_family_with_a_period_of_parameters()
    {
        SolutionSet within = Equation.Parse("sin(a*x) = 0").Solve("x").Within(0, 10);

        Assert.Equal("k", Assert.Single(within.Solutions).Index);
        Assert.Equal("{π * k / a | k in Z, k / a >= 0, π * k / a <= 10} if a != 0", within.Print());
    }

    // Bounds rather than roots and absolute values where they say the same, without squares,
    // which are never negative, and without what the other conditions imply.
    [Theory]
    [InlineData("sqrt(x) + sqrt(x + a) = 1", "{(a - 1)^2 / 4} if a >= -1 and a <= 1")]
    [InlineData("cos(x) = 1/a", "{acos(1 / a) + 2 * π * k, -acos(1 / a) + 2 * π * k | k in Z} if abs(a) >= 1")]
    [InlineData("x = asin(1/a)", "{asin(1 / a)} if abs(a) >= 1")]
    [InlineData("sin(x) = a - 1", "{asin(a - 1) + 2 * π * k, -asin(a - 1) + π + 2 * π * k | k in Z} if a >= 0 and a <= 2")]
    [InlineData("sqrt(x^2 + a^2) = 2", "{-sqrt(-a^2 + 4), sqrt(-a^2 + 4)} if a >= -2 and a <= 2")]
    [InlineData("1/(x - a) = b", "{(a * b + 1) / b} if b != 0")]
    [InlineData("x^2 + a^2 = 0", "{0} if a = 0")]
    public void Conditions_are_as_simple_as_found(string equation, string expected) =>
        Assert.Equal(expected, Printed(equation));

    // Where the parameters meet the conditions, each solution makes both sides equal.
    [Theory]
    [InlineData("a*x^2 + b*x + c = 0", "x", "a=2 b=-7 c=3")]
    [InlineData("a*x^2 + b*x + c = 0", "x", "a=-1 b=1 c=5")]
    [InlineData("E = m*v^2/2", "v", "E=8 m=4")]
    [InlineData("x^2 - (a + b)*x + a*b = 0", "x", "a=-3 b=5")]
    [InlineData("x^2 - 2*x + a = 0", "x", "a=-2")]
    [InlineData("x^4 = a", "x", "a=16")]
    [InlineData("T = 2*pi*sqrt(L/g)", "g", "T=2 L=1")]
    [InlineData("a^x = b", "x", "a=0.5 b=3")]
    [InlineData("x^a = 8", "x", "a=1.5")]
    [InlineData("A = P*(1 + r)^n", "r", "A=200 P=100 n=5")]
    [InlineData("A = P*(1 + r)^n", "n", "A=200 P=100 r=0.05")]
    [InlineData("sin(a*x) = 1/2", "x", "a=3")]
    [InlineData("sin(a*x)/x = 0", "x", "a=-2")]
    [InlineData("sqrt(x) + sqrt(x + a) = 1", "x", "a=0.5")]
    [InlineData("sqrt(x) = x - a", "x", "a=2")]
    [InlineData("sqrt(x) = x - a", "x", "a=-0.2")]
    [InlineData("cos(x) = 1/a", "x", "a=-2")]
    public void Solutions_make_both_sides_equal(string text, string variable, string values)
    {
        (string, double)[] at = [.. values.Split(' ').Select(pair => pair.Split('=')).Select(pair => (pair[0], double.Parse(pair[1], System.Globalization.CultureInfo.InvariantCulture)))];
        Equation equation = Equation.Parse(text);
        SolutionSet solutions = equation.Solve(variable);

        // A family by a few of its values.
        List<Solution> met = [.. solutions.Solutions
            .SelectMany(solution => solution.Index is null ? [solution] : new[] { -1, 0, 2 }.Select(solution.At).OfType<Solution>())
            .Where(solution => solution.Conditions.All(condition => Holds(condition, at)))];
        Assert.NotEmpty(met);
        foreach (Solution solution in met)
        {
            double x = solution.Value.Evaluate(at);
            (string, double)[] all = [.. at, (variable, x)];
            Assert.Equal(equation.Left.Evaluate(all), equation.Right.Evaluate(all), 9);
        }
    }

    private static bool Holds(Condition condition, (string, double)[] at)
    {
        double difference = condition.Left.Evaluate(at) - condition.Right.Evaluate(at);
        return condition.Comparison switch
        {
            Comparison.Equal => Math.Abs(difference) <= 1e-12,
            Comparison.NotEqual => Math.Abs(difference) > 1e-12,
            Comparison.Less => difference < 0,
            Comparison.LessOrEqual => difference <= 0,
            Comparison.Greater => difference > 0,
            _ => difference >= 0,
        };
    }
}
