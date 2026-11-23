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
    [InlineData("x^2 - 2*a*x + 1 = 0", "x", "{a - sqrt(a^2 - 1), a + sqrt(a^2 - 1)} if a^2 >= 1")]
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
    [InlineData("1/x + 1/y = 1/f", "x", "{-f * y / (f - y)} if f != y and f * y != 0")]
    public void Functions_of_the_unknown_with_parameters(string equation, string variable, string expected) =>
        Assert.Equal(expected, Printed(equation, variable));

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
    public void Solutions_make_both_sides_equal(string text, string variable, string values)
    {
        (string, double)[] at = [.. values.Split(' ').Select(pair => pair.Split('=')).Select(pair => (pair[0], double.Parse(pair[1], System.Globalization.CultureInfo.InvariantCulture)))];
        Equation equation = Equation.Parse(text);
        SolutionSet solutions = equation.Solve(variable);

        List<Solution> met = [.. solutions.Solutions.Where(solution => solution.Conditions.All(condition => Holds(condition, at)))];
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
