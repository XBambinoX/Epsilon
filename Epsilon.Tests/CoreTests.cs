using Xunit;
using System.Numerics;
using Epsilon.Core;

namespace Epsilon.Tests.Core;

public class ParserTests
{
    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("0.5", 0.5)]
    public void Parses_decimal_numbers_with_invariant_culture(string input, double expected)
    {
        Expr expr = ExprParser.Parse(input);
        Assert.Equal(expected, expr.Evaluate(new Dictionary<string, double>()));
    }

    [Fact]
    public void Parses_declared_multiletter_variable()
    {
        Expr expr = ExprParser.Parse("radius^2 * pi", variableNames: new[] { "radius" });
        Assert.Equal(new HashSet<string> { "radius" }, expr.GetVariables());
    }

    [Fact]
    public void Parses_implicit_multiplication_for_single_letters_by_default()
    {
        // Without declared variable names, multi-letter runs fall back to single-letter implicit multiplication.
        Expr expr = ExprParser.Parse("2x");
        Assert.Equal(new Multiply(new Constant(2), new Variable("x")), expr);
    }

    [Fact]
    public void Parses_unary_minus_before_power_correctly()
    {
        // -x^2 must mean -(x^2), not (-x)^2
        Expr expr = ExprParser.Parse("-x^2");
        Assert.Equal(-1, expr.Evaluate(1.0));
        Assert.Equal(-4, expr.Evaluate(2.0));
    }

    [Fact]
    public void Parses_negative_exponent_without_parentheses()
    {
        Expr expr = ExprParser.Parse("x^-2");
        Assert.Equal(0.25, expr.Evaluate(2.0));
    }

    [Fact]
    public void Parses_implicit_multiplication_with_function_call()
    {
        Expr expr = ExprParser.Parse("2xsin(x)");
        Assert.Equal(0, expr.Evaluate(0.0));
    }

    [Fact]
    public void Recognizes_multiple_variables_from_string()
    {
        Expr expr = ExprParser.Parse("x^2 - a");
        var vars = expr.GetVariables();
        Assert.Equal(2, vars.Count);
        Assert.Contains("x", vars);
        Assert.Contains("a", vars);
        Assert.Equal(5, expr.Evaluate(new Dictionary<string, double> { ["x"] = 3, ["a"] = 4 }));
    }

    [Fact]
    public void Parses_exp_correctly_despite_containing_letter_x()
    {
        Expr expr = ExprParser.Parse("exp(x)");
        Assert.Equal(Math.E, expr.Evaluate(1.0), precision: 10);
    }

    [Fact]
    public void Parser_ParsesAbs()
    {
        Expr expr = ExprParser.Parse("abs(x)", "x");

        Assert.IsType<Abs>(expr);
    }

    [Fact]
    public void Parser_ParsesAbsWithExpression()
    {
        Expr expr = ExprParser.Parse("abs(x + 1)", "x");

        var abs = Assert.IsType<Abs>(expr);

        Assert.Equal(
            ExprParser.Parse("x + 1", "x"),
            abs.Argument
        );
    }

    [Fact]
    public void Parser_AbsEvaluatesNegativeConstant()
    {
        Expr expr = ExprParser.Parse("abs(-5)");

        Assert.Equal(5, expr.Evaluate(
            new Dictionary<string, double>()));
    }

    [Fact]
    public void Parser_ParsesNestedAbs()
    {
        Expr expr = ExprParser.Parse("abs(abs(x))", "x");

        var outer = Assert.IsType<Abs>(expr);
        Assert.IsType<Abs>(outer.Argument);
    }
}

public class SimplifierTests
{
    [Fact]
    public void Combines_scattered_constants_in_a_sum()
    {
        Expr expr = ExprParser.Parse("3 + x + 5");
        Expr simplified = expr.Simplify();
        Assert.Equal(8, simplified.Evaluate(0.0));
        Assert.Equal(9, simplified.Evaluate(1.0));
    }

    [Fact]
    public void Cancels_terms_that_sum_to_zero()
    {
        Expr expr = ExprParser.Parse("x - x + 3 - 3");
        Expr simplified = expr.Simplify();
        // Fully constant after simplification — no variable binding needed.
        Assert.Equal(0, simplified.Evaluate(new Dictionary<string, double>()));
    }

    [Theory]
    [InlineData("-(2x) + x", "-x")]          // was -2x + x: -(2x) counted as the term 2x
    [InlineData("x - -(3x)", "4x")]
    [InlineData("-(-(2x)) - x", "x")]
    public void Negated_multiples_combine_with_like_terms(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).Simplify().Print());
    }


    [Fact]
    public void Canonicalizes_zero_minus_x_to_negation()
    {
        Expr expr = ExprParser.Parse("0 - x").Simplify();
        Assert.Equal(-5, expr.Evaluate(5.0));
    }

    [Fact]
    public void Simplifies_power_over_bare_variable()
    {
        Expr expr = ExprParser.Parse("x^2 / x").Simplify();
        Assert.Equal(5, expr.Evaluate(5.0));
    }

    [Fact]
    public void Recognizes_pythagorean_identity()
    {
        Expr expr = ExprParser.Parse("sin(x)^2 + cos(x)^2").Simplify();
        Assert.Equal(1, expr.Evaluate(new Dictionary<string, double>()));
    }

    [Fact]
    public void Treats_addition_as_commutative_regardless_of_written_order()
    {
        Expr a = ExprParser.Parse("3 + x");
        Expr b = ExprParser.Parse("x + 3");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Simplify_AbsOfConstant()
    {
        Expr expr = new Abs(new Constant(-5));

        Assert.Equal(
            new Constant(5),
            expr.Simplify());
    }

    [Fact]
    public void Simplify_AbsOfZero()
    {
        Expr expr = new Abs(new Constant(0));

        Assert.Equal(
            new Constant(0),
            expr.Simplify());
    }
}

public class RootFindingTests
{
    [Fact]
    public void Finds_positive_root_of_quadratic()
    {
        Expr expr = ExprParser.Parse("x^2 - 4");
        var (root, found) = expr.TryFindRoot(3.0);
        Assert.True(found);
        Assert.Equal(2.0, root!.Value, precision: 6);
    }

    [Fact]
    public void Finds_root_with_fixed_parameter()
    {
        Expr expr = new Subtract(new Power(new Variable("x"), new Constant(2)), new Variable("a"));
        var (root, found) = expr.TryFindRoot("x", 3.0, new Dictionary<string, double> { ["a"] = 9.0 });
        Assert.True(found);
        Assert.Equal(3.0, root!.Value, precision: 6);
    }

    [Fact]
    public void Finds_all_real_roots_of_quadratic()
    {
        Expr expr = ExprParser.Parse("x^2 - 4");
        var roots = expr.FindRealRoots(-10, 10);
        Assert.Equal(2, roots.Count);
        Assert.Contains(roots, r => Math.Abs(r - 2.0) < 1e-6);
        Assert.Contains(roots, r => Math.Abs(r + 2.0) < 1e-6);
    }
}

public class ComplexNumberTests
{
    [Fact]
    public void Adds_complex_numbers()
    {
        var a = new ComplexNumber(1, 2);
        var b = new ComplexNumber(3, -1);
        var result = a + b;
        Assert.Equal(4, result.Real, precision: 10);
        Assert.Equal(1, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Multiplies_complex_numbers()
    {
        // (2 + 3i) * (1 - i) = 2 - 2i + 3i - 3i^2 = 2 + i + 3 = 5 + i
        var a = new ComplexNumber(2, 3);
        var b = new ComplexNumber(1, -1);
        var result = a * b;
        Assert.Equal(5, result.Real, precision: 10);
        Assert.Equal(1, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Divides_complex_numbers()
    {
        var a = new ComplexNumber(4, 2);
        var b = new ComplexNumber(2, 0);
        var result = a / b;
        Assert.Equal(2, result.Real, precision: 10);
        Assert.Equal(1, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Computes_magnitude_correctly()
    {
        var z = new ComplexNumber(3, 4);
        Assert.Equal(5, z.Magnitude, precision: 10);
    }

    [Fact]
    public void Imaginary_unit_squared_equals_negative_one()
    {
        var i = ComplexNumber.ImaginaryUnit;
        var result = i * i;
        Assert.Equal(-1, result.Real, precision: 10);
        Assert.Equal(0, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Sqrt_of_negative_one_equals_imaginary_unit()
    {
        var result = ComplexNumber.Sqrt(new ComplexNumber(-1, 0));
        Assert.Equal(0, result.Real, precision: 10);
        Assert.Equal(1, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Exp_of_i_pi_equals_negative_one()
    {
        // Euler's identity: e^(i*pi) = -1
        var z = new ComplexNumber(0, Math.PI);
        var result = ComplexNumber.Exp(z);
        Assert.Equal(-1, result.Real, precision: 10);
        Assert.Equal(0, result.Imaginary, precision: 10);
    }

    // Parsing 'i' from strings

    [Fact]
    public void Parses_imaginary_unit_from_string()
    {
        Expr expr = ExprParser.Parse("2 + 3i");
        ComplexNumber result = expr.EvaluateComplex(new Dictionary<string, ComplexNumber>());
        Assert.Equal(2, result.Real, precision: 10);
        Assert.Equal(3, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Parses_pure_imaginary_expression()
    {
        Expr expr = ExprParser.Parse("i * i");
        ComplexNumber result = expr.EvaluateComplex(new Dictionary<string, ComplexNumber>());
        Assert.Equal(-1, result.Real, precision: 10);
        Assert.Equal(0, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Evaluating_imaginary_expression_as_real_throws()
    {
        Expr expr = ExprParser.Parse("i");
        Assert.Throws<InvalidOperationException>(() => expr.Evaluate(new Dictionary<string, double>()));
    }

    // Complex evaluation of ordinary real-variable expressions

    [Fact]
    public void Evaluates_polynomial_at_complex_point()
    {
        // f(x) = x^2 + 1, at x = i: i^2 + 1 = -1 + 1 = 0
        Expr expr = ExprParser.Parse("x^2 + 1");
        ComplexNumber result = expr.EvaluateComplex(ComplexNumber.ImaginaryUnit);
        Assert.Equal(0, result.Real, precision: 10);
        Assert.Equal(0, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Evaluates_sin_at_complex_point()
    {
        Expr expr = ExprParser.Parse("sin(x)");
        ComplexNumber result = expr.EvaluateComplex(new ComplexNumber(0, 1));
        // sin(i) = i * sinh(1)
        Assert.Equal(0, result.Real, precision: 10);
        Assert.Equal(Math.Sinh(1), result.Imaginary, precision: 10);
    }

    //Complex root finding

    [Fact]
    public void Finds_complex_root_of_x_squared_plus_one()
    {
        Expr expr = ExprParser.Parse("x^2 + 1");
        var (root, found) = expr.TryFindComplexRoot(new ComplexNumber(0, 1));
        Assert.True(found);
        Assert.Equal(0, root!.Value.Real, precision: 6);
        Assert.Equal(1, root.Value.Imaginary, precision: 6);
    }

    [Fact]
    public void Finds_complex_root_with_fixed_parameter()
    {
        // x^2 + a = 0, a = 1  =>  x = i (or -i)
        Expr expr = new Add(new Power(new Variable("x"), new Constant(2)), new Variable("a"));
        var (root, found) = expr.TryFindComplexRoot(
            "x", new ComplexNumber(0, 1),
            new Dictionary<string, ComplexNumber> { ["a"] = new ComplexNumber(1, 0) });

        Assert.True(found);
        Assert.Equal(0, root!.Value.Real, precision: 6);
        Assert.Equal(1, root.Value.Imaginary, precision: 6);
    }

    [Fact]
    public void Finds_all_complex_roots_in_grid()
    {
        // x^2 + 1 = 0 has roots i and -i
        Expr expr = ExprParser.Parse("x^2 + 1");
        var roots = expr.FindComplexRoots(-2, 2, -2, 2, gridSteps: 8);

        Assert.Contains(roots, r => Math.Abs(r.Real - 0) < 1e-4 && Math.Abs(r.Imaginary - 1) < 1e-4);
        Assert.Contains(roots, r => Math.Abs(r.Real - 0) < 1e-4 && Math.Abs(r.Imaginary + 1) < 1e-4);
    }
}

public class RootFindingEdgeCaseTests
{
    [Fact]
    public void Finds_roots_over_infinite_interval()
    {
        Expr expr = ExprParser.Parse("x^2 - 4");
        var roots = expr.FindRealRoots(double.NegativeInfinity, double.PositiveInfinity);
        Assert.Equal(2, roots.Count);
        Assert.Contains(roots, r => Math.Abs(r - 2.0) < 1e-4);
        Assert.Contains(roots, r => Math.Abs(r + 2.0) < 1e-4);
    }

    [Fact]
    public void Finds_root_on_right_infinite_interval()
    {
        Expr expr = ExprParser.Parse("x^2 - 4");
        var roots = expr.FindRealRoots(0, double.PositiveInfinity);
        Assert.Single(roots);
        Assert.True(Math.Abs(roots[0] - 2.0) < 1e-4);
    }

    [Fact]
    public void Returns_empty_when_no_real_roots_exist()
    {
        Expr expr = ExprParser.Parse("x^2 + 1");
        var roots = expr.FindRealRoots(-100, 100);
        Assert.Empty(roots);
    }

    [Fact]
    public void Solves_equation_in_left_equals_right_form()
    {
        // x^2 = 4  =>  x = ±2
        Expr left = ExprParser.Parse("x^2");
        Expr right = ExprParser.Parse("4");
        var roots = left.SolveNumerically(right, -10, 10);
        Assert.Equal(2, roots.Count);
    }

    [Fact]
    public void Throws_when_left_limit_not_less_than_right_limit()
    {
        Expr expr = ExprParser.Parse("x^2 - 4");
        Assert.Throws<ArgumentException>(() => expr.FindRealRoots(5, 5));
        Assert.Throws<ArgumentException>(() => expr.FindRealRoots(10, -10));
    }

    [Fact]
    public void Throws_when_bounds_are_nan()
    {
        Expr expr = ExprParser.Parse("x^2 - 4");
        Assert.Throws<ArgumentException>(() => expr.FindRealRoots(double.NaN, 10));
    }

    [Fact]
    public void Throws_when_scan_steps_too_low()
    {
        Expr expr = ExprParser.Parse("x^2 - 4");
        Assert.Throws<ArgumentOutOfRangeException>(() => expr.FindRealRoots(-10, 10, scanSteps: 1));
    }

    [Fact]
    public void Newton_fails_gracefully_at_stationary_point()
    {
        // x^2 + 5 has no real root, but its derivative (2x) is exactly zero at x=0.
        Expr expr = ExprParser.Parse("x^2 + 5");
        var (root, found) = expr.TryFindRoot(0.0);
        Assert.False(found);
        Assert.Null(root);
    }

    [Fact]
    public void Does_not_mistake_asymptote_for_root()
    {
        // 1/x has no real root anywhere — it approaches infinity near x=0 but never crosses zero.
        Expr expr = ExprParser.Parse("1 / x");
        var roots = expr.FindRealRoots(-10, 10);
        Assert.Empty(roots);
    }

    [Fact]
    public void Finds_complex_roots_with_two_expression_form()
    {
        // x^2 = -4  =>  x = ±2i
        Expr left = ExprParser.Parse("x^2");
        Expr right = ExprParser.Parse("0 - 4");
        var roots = left.SolveComplexNumerically(right, -5, 5, -5, 5, gridSteps: 8);

        Assert.Contains(roots, r => Math.Abs(r.Real) < 1e-3 && Math.Abs(r.Imaginary - 2.0) < 1e-3);
        Assert.Contains(roots, r => Math.Abs(r.Real) < 1e-3 && Math.Abs(r.Imaginary + 2.0) < 1e-3);
    }
}

public class ComplexRootMultiplicityTests
{
    [Theory]
    [InlineData("(x - 1)^2", 1, 1e-12)]                                    // was 80 roots around 0.99999
    [InlineData("x^2 - 2x + 1", 1, 1e-12)]
    [InlineData("(x - 1)^3", 1, 1e-12)]                                    // was 150
    [InlineData("x^3 - 3x^2 + 3x - 1", 1, 1e-6)]                           // expanded: rounding limits the precision
    [InlineData("x^4 - 4x^3 + 6x^2 - 4x + 1", 1, 1e-4)]
    [InlineData("(x - 1)^7", 1, 1e-12)]
    [InlineData("x^5", 0, 1e-12)]                                          // was 161, up to 0.009 away from 0
    [InlineData("(exp(x) - 1)^2", 0, 1e-12)]                               // was 50
    public void Multiple_root_is_reported_once(string input, double expected, double precision)
    {
        var roots = ExprParser.Parse(input).FindComplexRoots(-3, 3, -3, 3);

        ComplexNumber root = Assert.Single(roots);
        Assert.True((root - expected).Magnitude < precision, $"{root} is not within {precision} of {expected}");
    }

    [Fact]
    public void Multiple_and_simple_roots_together()
    {
        var roots = ExprParser.Parse("(x - 1)^2 * (x + 2)").FindComplexRoots(-3, 3, -3, 3)
            .OrderBy(z => z.Real).ToList();

        Assert.Equal(2, roots.Count);                                      // was 37
        Assert.True((roots[0] + 2).Magnitude < 1e-12);
        Assert.True((roots[1] - 1).Magnitude < 1e-12);
    }

    [Theory]
    [InlineData("(x^2 + 1)^2")]
    [InlineData("x^4 + 2x^2 + 1")]
    public void Double_complex_roots_are_reported_once(string input)
    {
        var roots = ExprParser.Parse(input).FindComplexRoots(-3, 3, -3, 3).OrderBy(z => z.Imaginary).ToList();

        Assert.Equal(2, roots.Count);                                      // was 55
        Assert.True((roots[0] + ComplexNumber.ImaginaryUnit).Magnitude < 1e-9);
        Assert.True((roots[1] - ComplexNumber.ImaginaryUnit).Magnitude < 1e-9);
    }

    [Fact]
    public void Close_simple_roots_stay_apart()
    {
        var roots = ExprParser.Parse("(x - 1)*(x - 1.001)").FindComplexRoots(-3, 3, -3, 3).OrderBy(z => z.Real).ToList();

        Assert.Equal(2, roots.Count);
        Assert.True((roots[0] - 1).Magnitude < 1e-12);
        Assert.True((roots[1] - 1.001).Magnitude < 1e-12);
    }

    [Fact]
    public void Root_between_two_roots_does_not_merge_them()
    {
        var roots = ExprParser.Parse("x^3 - x").FindComplexRoots(-3, 3, -3, 3).Select(z => z.Real).Order().ToList();

        Assert.Equal(3, roots.Count);
        Assert.Equal(-1, roots[0], precision: 12);
        Assert.Equal(0, roots[1], precision: 12);
        Assert.Equal(1, roots[2], precision: 12);
    }

    [Fact]
    public void Evenly_spaced_roots_are_not_merged()
    {
        // Merge checks at k/16 of the segment would all hit roots between -0.8pi and 0.8pi.
        var roots = ExprParser.Parse("sin(10x)").FindComplexRoots(-3, 3, -0.5, 0.5, gridSteps: 60)
            .Select(z => z.Real / (Math.PI / 10)).Order().ToList();

        Assert.Equal(Enumerable.Range(-10, 21).Select(k => (double)k), roots.Select(k => Math.Round(k)));
        Assert.All(roots, k => Assert.Equal(Math.Round(k), k, precision: 9));
    }
}

public class SolveNumericallyTests
{
    [Fact]
    public void Number_as_right_side_is_the_equation()
    {
        // FindRealRoots(0.5, -4, 4) took 0.5 and -4 for the range and threw.
        var roots = ExprParser.Parse("sin(x)").SolveNumerically(0.5, -4, 4);

        Assert.Equal(3, roots.Count);
        Assert.Equal(-7 * Math.PI / 6, roots[0], precision: 12);
        Assert.Equal(Math.PI / 6, roots[1], precision: 12);
        Assert.Equal(5 * Math.PI / 6, roots[2], precision: 12);
    }

    [Fact]
    public void Negative_number_as_right_side_is_the_equation()
    {
        // FindRealRoots(-1, 2, 3) silently gave the root 0 of sin(x) = 0 in [-1, 2].
        Expr sin = ExprParser.Parse("sin(x)");

        Assert.Empty(sin.SolveNumerically(-1, 2, 3));
        Assert.Equal(-Math.PI / 2, Assert.Single(sin.SolveNumerically(-1, -2, -1)), precision: 8);
    }

    [Fact]
    public void Named_variable_with_number_as_right_side()
    {
        var a = new Dictionary<string, double> { ["a"] = 2 };

        Assert.Equal([3.0], ExprParser.Parse("a*x").SolveNumerically(6, "x", a));
        Assert.Equal([-3.0, 3.0], ExprParser.Parse("x^2").SolveNumerically(ExprParser.Parse("a + 7"), "x", a));
    }

    [Fact]
    public void Undefined_points_are_not_solutions()
    {
        Assert.Equal([1.0], ExprParser.Parse("x^2/x").SolveNumerically(ExprParser.Parse("x^2"), -10, 10));
    }

    [Fact]
    public void Complex_number_as_right_side_is_the_equation()
    {
        // FindComplexRoots(1, -1, 1, -7, 7) took 1 and -1 for the real range and threw.
        var roots = ExprParser.Parse("exp(x)").SolveComplexNumerically(1, -1, 1, -7, 7)
            .OrderBy(z => z.Imaginary).ToList();

        Assert.Equal(3, roots.Count);
        Assert.All(roots, z => Assert.Equal(0, z.Real, precision: 12));
        Assert.Equal(-2 * Math.PI, roots[0].Imaginary, precision: 12);
        Assert.Equal(0, roots[1].Imaginary, precision: 12);
        Assert.Equal(2 * Math.PI, roots[2].Imaginary, precision: 12);
    }

    [Fact]
    public void Obsolete_equation_overloads_still_work()
    {
#pragma warning disable CS0618 // the old names are kept for compatibility and must keep working
        Assert.Equal([0.0, 2.0], ExprParser.Parse("x^2").FindRealRoots(ExprParser.Parse("2x"), -10, 10));
        Assert.Equal([-3.0, 3.0], ExprParser.Parse("x^2").FindRealRoots(ExprParser.Parse("9"), "x"));
        Assert.Equal(2, ExprParser.Parse("x^2").FindComplexRoots(ExprParser.Parse("-4"), -3, 3, -3, 3).Count);
#pragma warning restore CS0618
    }
}

public class CanonicalizationTests
{
    [Fact]
    public void Add_is_order_independent_regardless_of_written_form()
    {
        Expr a = ExprParser.Parse("3 + x");
        Expr b = ExprParser.Parse("x + 3");
        Assert.Equal(a, b);
        Assert.Equal(a.ToString(), b.ToString());
    }

    [Fact]
    public void Multiply_places_numeric_coefficient_consistently()
    {
        Expr a = ExprParser.Parse("x * 2");
        Expr b = ExprParser.Parse("2 * x");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Trig_identity_terms_canonicalize_to_same_order()
    {
        Expr a = ExprParser.Parse("sin(x)^2 + cos(x)^2");
        Expr b = ExprParser.Parse("cos(x)^2 + sin(x)^2");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Canonicalization_is_idempotent()
    {
        Expr expr = ExprParser.Parse("x + 3 + y");
        Expr once = expr.Canonicalize();
        Expr twice = once.Canonicalize();
        Assert.Equal(once, twice);
    }

    // Simplify canonicalizes at every level; reusing the nodes keeps their kept hashes and
    // their Simplify cache entries valid instead of building the same tree again.
    [Theory]
    [InlineData("x + 3 + y")]
    [InlineData("3x^2 * sin(x) + exp(-x/2) * cos(2x) - sqrt(x^2 + 1) / (1 + x)")]
    [InlineData("2 * y * x * 3 + x * x")]
    public void Canonical_tree_comes_back_as_the_same_instance(string input)
    {
        Expr canonical = ExprParser.Parse(input, "x", "y").Canonicalize();

        Assert.Same(canonical, canonical.Canonicalize());
    }

    [Fact]
    public void Only_the_nodes_above_a_changed_term_are_rebuilt()
    {
        Expr x = new Variable("x"), y = new Variable("y");
        Expr prefix = (2 * x + 3 * x).Canonicalize();

        // y * 5 becomes 5 * y and stays last, so the sum before it is unchanged.
        var result = (Add)new Add(prefix, y * 5).Canonicalize();

        Assert.Same(prefix, result.Left);
        Assert.Equal(5 * y, result.Right);
    }

    [Fact]
    public void Right_nested_sum_is_rebuilt_left_associative()
    {
        Expr x = new Variable("x"), y = new Variable("y"), z = new Variable("z");

        Assert.Equal(x + y + z, new Add(x, new Add(y, z)).Canonicalize());
    }

    [Fact]
    public void Subtract_operands_are_not_reordered()
    {
        // Subtraction is non-commutative — canonicalization must not swap operands.
        Expr expr = ExprParser.Parse("x - 3");
        Assert.Equal(-3.0, expr.Evaluate(0.0));
    }
}

public class SubstituteAndDependsOnTests
{
    [Fact]
    public void Substitute_replaces_variable_with_expression()
    {
        Expr expr = ExprParser.Parse("x^2 + 1");
        Expr substituted = expr.Substitute("x", ExprParser.Parse("y + 1"));
        Assert.Equal(new HashSet<string> { "y" }, substituted.GetVariables());
        Assert.Equal(5, substituted.Evaluate(new Dictionary<string, double> { ["y"] = 1 })); // (1+1)^2+1=5
    }

    [Fact]
    public void Substitute_leaves_other_variables_untouched()
    {
        Expr expr = ExprParser.Parse("x + y");
        Expr substituted = expr.Substitute("x", new Constant(10));
        Assert.Equal(new HashSet<string> { "y" }, substituted.GetVariables());
        Assert.Equal(15, substituted.Evaluate(new Dictionary<string, double> { ["y"] = 5 }));
    }

    [Fact]
    public void Substitute_into_function_argument_works()
    {
        Expr expr = ExprParser.Parse("sin(x)");
        Expr substituted = expr.Substitute("x", ExprParser.Parse("x^2"));
        Assert.Equal(Math.Sin(4), substituted.Evaluate(2.0), precision: 10);
    }

    [Fact]
    public void DependsOn_detects_variable_presence()
    {
        Expr expr = ExprParser.Parse("x^2 + y");
        Assert.True(expr.DependsOn("x"));
        Assert.True(expr.DependsOn("y"));
        Assert.False(expr.DependsOn("z"));
    }

    [Fact]
    public void DependsOn_is_false_for_constant_expression()
    {
        Expr expr = new Constant(42);
        Assert.False(expr.DependsOn("x"));
    }

    [Fact]
    public void GetVariables_on_pi_and_e_returns_empty()
    {
        Expr expr = ExprParser.Parse("pi + e");
        Assert.Empty(expr.GetVariables());
    }
}

public class DifferentiationTests
{
    private const double H = 1e-6; // step for numerical cross-check
    private const double Tolerance = 1e-4;

    [Theory]
    [InlineData("ln(y)")]
    [InlineData("5 / y")]
    [InlineData("sqrt(y)")]
    [InlineData("atan(y)")]
    [InlineData("floor(y)")] // would throw if the node's own rule were reached
    public void Derivative_of_expression_independent_of_variable_is_exactly_zero(string input)
    {
        Expr derivative = ExprParser.Parse(input, "y").Differentiate("x");
        Assert.Equal(new Constant(0), derivative);
    }

    [Fact]
    public void Derivative_of_product_with_constant_factor_has_no_zero_quotient_leftovers()
    {
        Expr derivative = ExprParser.Parse("x * ln(y)", "x", "y").Differentiate("x");
        Assert.Equal(new Ln(new Variable("y")), derivative);
    }

    // Numerically verifies a symbolic derivative against central-difference approximation,
    // catching cases where the symbolic rule is subtly wrong but still "compiles and runs".
    private static void AssertDerivativeMatchesNumeric(string input, double atPoint)
    {
        Expr expr = ExprParser.Parse(input);
        Expr derivative = expr.Differentiate();

        double symbolic = derivative.Evaluate(atPoint);
        double numeric = (expr.Evaluate(atPoint + H) - expr.Evaluate(atPoint - H)) / (2 * H);

        Assert.Equal(numeric, symbolic, precision: 3);
    }

    [Theory]
    [InlineData("x^2", 3.0)]
    [InlineData("x^3", 2.0)]
    [InlineData("sin(x)", 1.0)]
    [InlineData("cos(x)", 1.0)]
    [InlineData("tan(x)", 0.5)]
    [InlineData("cot(x)", 1.0)]
    [InlineData("sec(x)", 0.5)]
    [InlineData("csc(x)", 1.0)]
    [InlineData("exp(x)", 1.0)]
    [InlineData("ln(x)", 2.0)]
    [InlineData("sqrt(x)", 4.0)]
    [InlineData("sinh(x)", 1.0)]
    [InlineData("cosh(x)", 1.0)]
    [InlineData("tanh(x)", 0.5)]
    [InlineData("asin(x)", 0.3)]
    [InlineData("acos(x)", 0.3)]
    [InlineData("atan(x)", 1.0)]
    public void Derivative_matches_numerical_approximation(string input, double atPoint)
    {
        AssertDerivativeMatchesNumeric(input, atPoint);
    }

    [Fact]
    public void Chain_rule_applies_to_composite_argument()
    {
        // d/dx sin(x^2) = cos(x^2) * 2x
        Expr expr = ExprParser.Parse("sin(x^2)");
        Expr derivative = expr.Differentiate();
        double atPoint = 1.5;
        double expected = Math.Cos(atPoint * atPoint) * 2 * atPoint;
        Assert.Equal(expected, derivative.Evaluate(atPoint), precision: 8);
    }

    [Fact]
    public void Product_rule_applies_correctly()
    {
        // d/dx (x * sin(x)) = sin(x) + x*cos(x)
        Expr expr = ExprParser.Parse("x * sin(x)");
        Expr derivative = expr.Differentiate();
        double atPoint = 2.0;
        double expected = Math.Sin(atPoint) + atPoint * Math.Cos(atPoint);
        Assert.Equal(expected, derivative.Evaluate(atPoint), precision: 8);
    }

    [Fact]
    public void Quotient_rule_applies_correctly()
    {
        // d/dx (sin(x) / x) = (cos(x)*x - sin(x)) / x^2
        Expr expr = ExprParser.Parse("sin(x) / x");
        Expr derivative = expr.Differentiate();
        double atPoint = 1.5;
        double expected = (Math.Cos(atPoint) * atPoint - Math.Sin(atPoint)) / (atPoint * atPoint);
        Assert.Equal(expected, derivative.Evaluate(atPoint), precision: 6);
    }

    [Fact]
    public void Derivative_of_constant_is_zero()
    {
        Expr expr = new Constant(42);
        Expr derivative = expr.Differentiate("x");
        Assert.Equal(0, derivative.Evaluate(new Dictionary<string, double>()));
    }

    [Fact]
    public void Derivative_with_respect_to_unrelated_variable_is_zero()
    {
        // d/dy (x^2) = 0, since the expression doesn't depend on y
        Expr expr = ExprParser.Parse("x^2");
        Expr derivative = expr.Differentiate("y");
        Assert.Equal(0, derivative.Evaluate(new Dictionary<string, double> { ["x"] = 5 }));
    }

    [Fact]
    public void Partial_derivative_treats_other_variables_as_constants()
    {
        // d/dx (x^2 * y) = 2xy
        Expr expr = ExprParser.Parse("x^2 * y");
        Expr derivative = expr.Differentiate("x");
        double result = derivative.Evaluate(new Dictionary<string, double> { ["x"] = 3, ["y"] = 4 });
        Assert.Equal(24, result, precision: 8); // 2*3*4
    }

    [Fact]
    public void Power_rule_handles_variable_exponent_via_logarithmic_differentiation()
    {
        // d/dx (x^x) = x^x * (ln(x) + 1)
        Expr expr = ExprParser.Parse("x^x");
        Expr derivative = expr.Differentiate();
        double atPoint = 2.0;
        double expected = Math.Pow(atPoint, atPoint) * (Math.Log(atPoint) + 1);
        Assert.Equal(expected, derivative.Evaluate(atPoint), precision: 6);
    }

    [Fact]
    public void Second_derivative_via_double_differentiation()
    {
        // f(x) = x^3, f'(x) = 3x^2, f''(x) = 6x
        Expr expr = ExprParser.Parse("x^3");
        Expr firstDerivative = expr.Differentiate();
        Expr secondDerivative = firstDerivative.Differentiate();
        Assert.Equal(12, secondDerivative.Evaluate(2.0), precision: 8);
    }

    [Fact]
    public void NthRoot_derivative_matches_numerical_approximation()
    {
        // d/dx nthroot(x, 3) = (1/3) * x^(-2/3)
        Expr expr = ExprParser.Parse("nthroot(x, 3)");
        Expr derivative = expr.Differentiate();
        double atPoint = 8.0;
        double numeric = (expr.Evaluate(atPoint + H) - expr.Evaluate(atPoint - H)) / (2 * H);
        Assert.Equal(numeric, derivative.Evaluate(atPoint), precision: 3);
    }
}

public class PrinterTests
{
    [Fact]
    public void Prints_simple_addition_without_parentheses()
    {
        Expr expr = ExprParser.Parse("x + 1");
        Assert.Equal("x + 1", expr.Print());
    }

    [Fact]
    public void Prints_unary_minus_compactly()
    {
        Expr expr = ExprParser.Parse("0 - x").Simplify();
        Assert.Equal("-x", expr.Print());
    }

    [Fact]
    public void Prints_sin_without_double_parentheses()
    {
        Expr expr = ExprParser.Parse("sin(x + 1)");
        Assert.Equal("sin(x + 1)", expr.Print());
    }

    [Fact]
    public void Prints_cos_without_double_parentheses()
    {
        Expr expr = ExprParser.Parse("cos(x + 1)");
        Assert.Equal("cos(x + 1)", expr.Print());
    }

    [Fact]
    public void Prints_negated_sin_argument_cleanly()
    {
        Expr expr = ExprParser.Parse("sin(0 - x)").Simplify();
        Assert.Equal("sin(-x)", expr.Print());
    }

    [Fact]
    public void Wraps_addition_in_parentheses_when_multiplied()
    {
        // (x + 1) * 2 must keep parens — without them, "x + 1 * 2" means something else
        Expr expr = ExprParser.Parse("(x + 1) * 2");
        string result = expr.Print();
        Assert.Contains("(x + 1)", result);
    }

    [Fact]
    public void Does_not_add_unnecessary_parentheses_for_left_associative_subtraction()
    {
        // (x - x) - 1 should NOT keep parens around the left side; a - b - c is standard
        Expr expr = ExprParser.Parse("(x - x) - 1").Simplify();
        Assert.DoesNotContain("(", expr.Print());
    }

    [Fact]
    public void Requires_parentheses_for_non_associative_subtraction_on_the_right()
    {
        // x - (x - 1) is NOT the same as x - x - 1: the printed sum distributes the minus
        // over the inner terms instead, and must still mean the same.
        Expr expr = ExprParser.Parse("x - (x - 1)");
        string result = expr.Print();
        Assert.Equal("x - x + 1", result);
        Assert.Equal(expr.Evaluate(5), ExprParser.Parse(result).Evaluate(5));
    }

    [Fact]
    public void Requires_parentheses_for_right_associative_power_on_the_left()
    {
        //(x^2)^3 != x^(2^3), so left side must keep parens under right-associative ^
        Expr expr = ExprParser.Parse("(x^2)^3");
        string result = expr.Print();
        Assert.Contains("(", result);
    }

    [Fact]
    public void Prints_pi_and_e_as_symbols()
    {
        Expr piExpr = new Pi();
        Expr eExpr = new EulerNumber();
        Assert.Equal("π", piExpr.Print());
        Assert.Equal("e", eExpr.Print());
    }

    [Fact]
    public void Prints_named_multiletter_variable()
    {
        Expr expr = ExprParser.Parse("radius^2", variableNames: new[] { "radius" });
        Assert.Contains("radius", expr.Print());
    }

    [Fact]
    public void Prints_all_trig_functions_with_correct_names()
    {
        Assert.Equal("cot(x)", ExprParser.Parse("cot(x)").Print());
        Assert.Equal("sec(x)", ExprParser.Parse("sec(x)").Print());
        Assert.Equal("csc(x)", ExprParser.Parse("csc(x)").Print());
        Assert.Equal("asin(x)", ExprParser.Parse("asin(x)").Print());
        Assert.Equal("acos(x)", ExprParser.Parse("acos(x)").Print());
        Assert.Equal("atan(x)", ExprParser.Parse("atan(x)").Print());
    }

    [Fact]
    public void Prints_sqrt_and_nthroot()
    {
        Expr sqrtExpr = ExprParser.Parse("sqrt(x)");
        Expr nthRootExpr = ExprParser.Parse("nthroot(x, 3)");
        Assert.Equal("sqrt(x)", sqrtExpr.Print());
        Assert.Equal("nthroot(x, 3)", nthRootExpr.Print());
    }
}

public class ParserErrorTests
{
    [Theory]
    [InlineData("2 +")]
    [InlineData("* 2")]
    [InlineData("sin(")]
    [InlineData("sin)")]
    [InlineData("(2 + 3")]
    [InlineData("2 + 3)")]
    [InlineData("sin(x))")]
    public void Throws_FormatException_for_malformed_expressions(string input)
    {
        Assert.Throws<FormatException>(() => ExprParser.Parse(input));
    }

    [Fact]
    public void Throws_when_function_name_not_followed_by_parenthesis()
    {
        var ex = Assert.Throws<FormatException>(() => ExprParser.Parse("sin x"));
        Assert.Contains("(", ex.Message);
    }

    [Fact]
    public void Unknown_multiletter_word_decomposes_into_single_letter_variables()
    {
        Expr expr = ExprParser.Parse("abc");
        Assert.Equal(3, expr.GetVariables().Count);
    }

    [Fact]
    public void Same_word_becomes_single_variable_when_declared()
    {
        // The same input parses as ONE variable when explicitly declared.
        Expr expr = ExprParser.Parse("abc", variableNames: new[] { "abc" });
        Assert.Single(expr.GetVariables());
        Assert.Contains("abc", expr.GetVariables());
    }

    [Fact]
    public void Throws_when_nthroot_has_wrong_argument_count()
    {
        Assert.Throws<FormatException>(() => ExprParser.Parse("nthroot(x)"));
        Assert.Throws<FormatException>(() => ExprParser.Parse("nthroot(x, 2, 3)"));
    }

    [Fact]
    public void Throws_for_unexpected_character()
    {
        Assert.Throws<FormatException>(() => ExprParser.Parse("x @ 2"));
    }

    [Fact]
    public void Throws_for_empty_input()
    {
        Assert.Throws<FormatException>(() => ExprParser.Parse(""));
    }

    [Fact]
    public void Throws_for_trailing_tokens_after_valid_expression()
    {
        Assert.Throws<FormatException>(() => ExprParser.Parse("x + 1 )"));
    }
}

public class EvaluationErrorTests
{
    [Fact]
    public void Throws_when_variable_binding_missing()
    {
        Expr expr = ExprParser.Parse("x + y");
        var ex = Assert.Throws<ArgumentException>(() =>
            expr.Evaluate(new Dictionary<string, double> { ["x"] = 1 }));
        Assert.Contains("y", ex.Message);
    }

    [Fact]
    public void Throws_when_calling_single_variable_evaluate_on_multivariable_expr()
    {
        Expr expr = ExprParser.Parse("x + y");
        var ex = Assert.Throws<InvalidOperationException>(() => expr.Evaluate(5.0));
        Assert.Contains("2", ex.Message); // message should mention it found 2 variables
    }

    [Fact]
    public void Throws_when_evaluating_imaginary_unit_as_real()
    {
        Expr expr = new ImaginaryUnit();
        Assert.Throws<InvalidOperationException>(() =>
            expr.Evaluate(new Dictionary<string, double>()));
    }

    [Fact]
    public void Throws_when_complex_binding_missing()
    {
        Expr expr = ExprParser.Parse("x + y");
        var ex = Assert.Throws<ArgumentException>(() =>
            expr.EvaluateComplex(new Dictionary<string, ComplexNumber> { ["x"] = new ComplexNumber(1, 0) }));
        Assert.Contains("y", ex.Message);
    }

    [Fact]
    public void GetSingleVariable_message_lists_all_found_variables()
    {
        Expr expr = ExprParser.Parse("x + y + z");
        var ex = Assert.Throws<InvalidOperationException>(() => expr.Differentiate());
        Assert.Contains("x", ex.Message);
        Assert.Contains("y", ex.Message);
        Assert.Contains("z", ex.Message);
    }
}

public class AdvancedSimplifierTests
{
    [Fact]
    public void Simplifies_ln_of_exp_to_identity()
    {
        Expr expr = ExprParser.Parse("ln(exp(x))").Simplify();
        Assert.Equal(5, expr.Evaluate(5.0));
    }

    [Fact]
    public void Simplifies_exp_of_ln_to_identity()
    {
        Expr expr = ExprParser.Parse("exp(ln(x))").Simplify();
        Assert.Equal(5, expr.Evaluate(5.0), precision: 8);
    }

    [Fact]
    public void Simplifies_sin_over_cos_to_tan()
    {
        Expr expr = ExprParser.Parse("sin(x) / cos(x)").Simplify();
        Assert.Equal("tan(x)", expr.Print());
    }

    [Fact]
    public void Simplifies_cos_over_sin_to_cot()
    {
        Expr expr = ExprParser.Parse("cos(x) / sin(x)").Simplify();
        Assert.Equal("cot(x)", expr.Print());
    }

    [Fact]
    public void Strict_does_not_simplify_tan_times_cot_without_proof_that_both_are_defined()
    {
        // tan(x) * cot(x) is undefined at multiples of pi/2, while 1 is defined everywhere.
        Expr expr = ExprParser.Parse("tan(x) * cot(x)").Simplify(SimplifyMode.Strict);
        Assert.IsType<Multiply>(expr);
        Assert.Equal(1, expr.Evaluate(0.7), precision: 10);
    }

    [Fact]
    public void Simplifies_sin_squared_over_cos_squared_to_tan_squared()
    {
        Expr expr = ExprParser.Parse("sin(x)^2 / cos(x)^2").Simplify();
        double atPoint = 0.7;
        double expected = Math.Pow(Math.Tan(atPoint), 2);
        Assert.Equal(expected, expr.Evaluate(atPoint), precision: 6);
    }

    [Fact]
    public void Simplifies_one_minus_sin_squared_to_cos_squared()
    {
        Expr expr = ExprParser.Parse("1 - sin(x)^2").Simplify();
        double atPoint = 0.8;
        double expected = Math.Pow(Math.Cos(atPoint), 2);
        Assert.Equal(expected, expr.Evaluate(atPoint), precision: 8);
    }

    [Fact]
    public void Simplifies_one_minus_cos_squared_to_sin_squared()
    {
        Expr expr = ExprParser.Parse("1 - cos(x)^2").Simplify();
        double atPoint = 0.8;
        double expected = Math.Pow(Math.Sin(atPoint), 2);
        Assert.Equal(expected, expr.Evaluate(atPoint), precision: 8);
    }

    [Theory]
    [InlineData("sec(x)^2 - tan(x)^2")] // undefined where cos(x) = 0
    [InlineData("csc(x)^2 - cot(x)^2")] // undefined where sin(x) = 0
    public void Strict_does_not_simplify_pythagorean_reciprocal_identities_without_domain_proof(string input)
    {
        Expr expr = ExprParser.Parse(input).Simplify(SimplifyMode.Strict);
        Assert.IsType<Subtract>(expr);
        Assert.Equal(1, expr.Evaluate(0.7), precision: 10);
    }

    [Theory]
    [InlineData("x * x^-1")]
    [InlineData("x^-1 * x^2")]
    [InlineData("x^(1/2) * x^(1/2)")]
    [InlineData("sqrt(x)^2")]
    [InlineData("(x^-1)^-1")]
    public void Strict_does_not_merge_powers_when_result_would_be_defined_at_more_points(string input)
    {
        Expr original = ExprParser.Parse(input, "x");
        Expr simplified = original.Simplify(SimplifyMode.Strict);

        // The left side is undefined at x = 0 or x = -1; the simplified form must be too.
        foreach (double x in new[] { 0.0, -1.0 })
        {
            bool originalDefined = double.IsFinite(original.Evaluate(x));
            bool simplifiedDefined = double.IsFinite(simplified.Evaluate(x));
            Assert.Equal(originalDefined, simplifiedDefined);
        }
    }

    [Theory]
    [InlineData("x * x^-1", "1")]
    [InlineData("x^-1 * x^2", "x")]
    [InlineData("(x^-1)^-1", "x")]
    public void Merges_powers_when_base_is_provably_nonzero(string input, string expected)
    {
        Expr expr = ExprParser.Parse(input, "x").Simplify(Assumptions.None.AssumeNonZero("x"));
        Assert.Equal(expected, expr.Print());
    }

    [Theory]
    [InlineData("x^(1/2) * x^(1/2)", "x")]
    [InlineData("sqrt(x)^2", "x")]
    public void Merges_fractional_powers_when_base_is_provably_nonnegative(string input, string expected)
    {
        Expr expr = ExprParser.Parse(input, "x").Simplify(Assumptions.None.AssumeNonNegative("x"));
        Assert.Equal(expected, expr.Print());
    }

    [Theory]
    [InlineData("x * x", "x^2")]
    [InlineData("x^2 * x^3", "x^5")]
    [InlineData("x^-1 * x^-2", "x^-3")]
    [InlineData("x^2 * x^(1/2)", "x^(5/2)")]
    public void Merges_powers_that_are_always_safe(string input, string expected)
    {
        Expr expr = ExprParser.Parse(input, "x").Simplify(SimplifyMode.Strict);
        Assert.Equal(expected, expr.Print());
    }

    [Theory]
    [InlineData("tan(x) * cot(x)", "1")]
    [InlineData("sec(x)^2 - tan(x)^2", "1")]
    [InlineData("csc(x)^2 - cot(x)^2", "1")]
    [InlineData("x / x", "1")]
    [InlineData("x * x^-1", "1")]
    [InlineData("0 / x", "0")]
    [InlineData("x^2 / x", "x")]
    [InlineData("x^-1 * x^2", "x")]
    [InlineData("sqrt(x)^2", "x")]
    [InlineData("x^(1/2) * x^(1/2)", "x")]
    [InlineData("(x^(1/2))^2", "x")]
    [InlineData("(x^-1)^-1", "x")]
    [InlineData("exp(ln(x))", "x")]
    public void Generic_mode_applies_domain_enlarging_identities_by_default(string input, string expected)
    {
        Expr expr = ExprParser.Parse(input, "x").Simplify();
        Assert.Equal(expected, expr.Print());
    }

    [Theory]
    [InlineData("tan(x) * cot(x)")]
    [InlineData("sec(x)^2 - tan(x)^2")]
    [InlineData("x^2 / x")]
    [InlineData("sqrt(x)^2")]
    [InlineData("(x^-1)^-1")]
    [InlineData("exp(ln(x))")]
    public void Generic_result_agrees_with_original_wherever_original_is_defined(string input)
    {
        Expr original = ExprParser.Parse(input, "x");
        Expr simplified = original.Simplify();

        // No points near pi/2: there sec^2 and tan^2 are ~1e32 in double precision and
        // their difference is pure rounding noise, not a meaningful value.
        foreach (double x in new[] { -2.3, -1.0, -0.4, 0.0, 0.4, 1.0, 2.3 })
        {
            // Dictionary overload: the simplified form may no longer contain x at all (e.g. "1").
            var bindings = new Dictionary<string, double> { ["x"] = x };
            double expected = original.Evaluate(bindings);
            if (double.IsFinite(expected) && Math.Abs(expected) < 1e10)
                Assert.Equal(expected, simplified.Evaluate(bindings), precision: 6);
        }
    }

    [Theory]
    [InlineData("sqrt(x^2)", "abs(x)")]   // x = -1: both sides defined, x would give the wrong value
    [InlineData("(x^2)^(1/2)", "(x^2)^(1/2)")] // same reason, must not collapse to x
    [InlineData("0 / 0", "0 / 0")]        // undefined everywhere, never folded
    public void Generic_mode_never_changes_a_defined_value(string input, string expected)
    {
        Expr expr = ExprParser.Parse(input, "x").Simplify();
        Assert.Equal(expected, expr.Print());
    }

    [Fact]
    public void Simplifies_sqrt_of_perfect_square_constant()
    {
        Expr expr = ExprParser.Parse("sqrt(4)").Simplify();
        Assert.Equal(2, expr.Evaluate(new Dictionary<string, double>()));
    }

    [Fact]
    public void Simplifies_sqrt_of_square_expression_to_base()
    {
        Expr expr = ExprParser.Parse("sqrt(x^2)").Simplify();
        Assert.Equal(5, expr.Evaluate(5.0));
    }

    [Fact]
    public void Simplifies_nthroot_of_constant()
    {
        Expr expr = ExprParser.Parse("nthroot(27, 3)").Simplify();
        Assert.Equal(3, expr.Evaluate(new Dictionary<string, double>()), precision: 8);
    }

    [Fact]
    public void Simplifies_division_of_identical_powers_to_one()
    {
        // Without this assumption, the simplifier returns x^3 / x^3.
        // x must be nonzero.
        var assumption = Assumptions.None.AssumeNonZero("x");

        Expr expr = ExprParser.Parse("x^3 / x^3").Simplify(assumption);
        Assert.Equal(1, expr.Evaluate(new Dictionary<string, double>()));
    }

    [Fact]
    public void Simplifies_zero_divided_by_provably_nonzero_variable_to_zero()
    {
        Expr expr = ExprParser.Parse("0 / x").Simplify(Assumptions.None.AssumePositive("x"));
        Assert.Equal(new Constant(0), expr);
    }

    [Fact]
    public void Strict_does_not_simplify_zero_divided_by_unknown_variable()
    {
        // x could be 0, so 0/x must not become a defined value.
        Expr expr = ExprParser.Parse("0 / x").Simplify(SimplifyMode.Strict);
        Assert.IsType<Divide>(expr);
    }

    [Theory]
    [InlineData("0 / 0")]
    [InlineData("0 / (x - x)")]
    public void Does_not_simplify_zero_divided_by_zero(string input)
    {
        Expr expr = ExprParser.Parse(input, "x").Simplify(SimplifyMode.Strict);
        Assert.Equal(new Divide(new Constant(0), new Constant(0)), expr);
        Assert.True(double.IsNaN(expr.Evaluate(new Dictionary<string, double>())));
    }

    [Fact]
    public void Simplifies_zero_divided_by_positive_polynomial_to_zero()
    {
        Expr expr = ExprParser.Parse("0 / (x^2 + 1)", "x").Simplify();
        Assert.Equal(new Constant(0), expr);
    }

    [Fact]
    public void Simplifies_nested_fraction_multiplication()
    {
        // (a/b) * (c/d) = (a*c)/(b*d)
        Expr expr = ExprParser.Parse("(x / 2) * (y / 3)").Simplify();
        double result = expr.Evaluate(new Dictionary<string, double> { ["x"] = 6, ["y"] = 9 });
        Assert.Equal((6.0 / 2) * (9.0 / 3), result, precision: 8);
    }

    [Fact]
    public void Simplifies_nested_division_by_multiplying_denominators()
    {
        // (a/b)/c = a/(b*c)
        Expr expr = ExprParser.Parse("(x / 2) / 3").Simplify();
        Assert.Equal(6.0 / 6, expr.Evaluate(6.0), precision: 8);
    }
}

public class PolynomialFactoringTests
{
    [Fact]
    public void Factors_simple_quadratic_with_two_real_roots()
    {
        Expr expr = ExprParser.Parse("x^2 - 5*x + 6");
        var (factored, success) = expr.TryFactorReal("x");

        Assert.True(success);
        for (double x = -3; x <= 5; x += 1)
            Assert.Equal(expr.Evaluate(x), factored.Evaluate(x), precision: 6);
    }

    [Fact]
    public void Factors_produce_clean_integer_roots_not_floating_point_noise()
    {
        Expr expr = ExprParser.Parse("x^2 - 5*x + 6");
        var (factored, _) = expr.TryFactorReal("x");
        string printed = factored.Print();

        Assert.DoesNotContain("00000", printed); // no floating-point noise like 2.0000000004
    }

    [Fact]
    public void Fails_to_factor_over_reals_when_no_real_roots_exist()
    {
        Expr expr = ExprParser.Parse("x^2 + 1");
        var (factored, success) = expr.TryFactorReal("x");

        Assert.False(success);
        Assert.Equal(expr, factored); // original expression returned unchanged
    }

    [Fact]
    public void Factors_over_complex_numbers_when_no_real_roots_exist()
    {
        Expr expr = ExprParser.Parse("x^2 + 1");
        var (factored, success) = expr.TryFactorComplex("x");

        Assert.True(success);
        // (x - i)(x + i) evaluated at x=i should be 0
        ComplexNumber result = factored.EvaluateComplex(ComplexNumber.ImaginaryUnit);
        Assert.Equal(0, result.Real, precision: 6);
        Assert.Equal(0, result.Imaginary, precision: 6);
    }

    [Fact]
    public void Handles_repeated_root_multiplicity_correctly()
    {
        // (x-2)^2 = x^2 - 4x + 4
        Expr expr = ExprParser.Parse("x^2 - 4*x + 4");
        var (factored, success) = expr.TryFactorReal("x");

        Assert.True(success);
        for (double x = -2; x <= 6; x += 1)
            Assert.Equal(expr.Evaluate(x), factored.Evaluate(x), precision: 6);
    }

    [Fact]
    public void Factors_cubic_with_three_real_roots()
    {
        // (x-1)(x-2)(x-3) = x^3 - 6x^2 + 11x - 6
        Expr expr = ExprParser.Parse("x^3 - 6*x^2 + 11*x - 6");
        var (factored, success) = expr.TryFactorReal("x");

        Assert.True(success);
        for (double x = -2; x <= 5; x += 0.5)
            Assert.Equal(expr.Evaluate(x), factored.Evaluate(x), precision: 4);
    }

    [Fact]
    public void Fails_for_constant_expression()
    {
        Expr expr = new Constant(5);
        var (_, success) = expr.TryFactorReal("x");
        Assert.False(success);
    }

    [Fact]
    public void Fails_for_non_polynomial_expression()
    {
        Expr expr = ExprParser.Parse("sin(x) + x");
        var (factored, success) = expr.TryFactorReal("x");

        Assert.False(success);
        Assert.Equal(expr, factored);
    }

    [Fact]
    public void Complex_factoring_returns_conjugate_pair_for_negative_discriminant_quadratic()
    {
        // x^2 + 4 has roots ±2i
        Expr expr = ExprParser.Parse("x^2 + 4");
        var (factored, success) = expr.TryFactorComplex("x");

        Assert.True(success);
        ComplexNumber atI = factored.EvaluateComplex(new ComplexNumber(0, 2));
        Assert.Equal(0, atI.Real, precision: 4);
        Assert.Equal(0, atI.Imaginary, precision: 4);
    }

    [Fact]
    public void Factors_polynomial_with_leading_coefficient()
    {
        // 2x^2 - 8 = 2(x-2)(x+2)
        Expr expr = ExprParser.Parse("2*x^2 - 8");
        var (factored, success) = expr.TryFactorReal("x");

        Assert.True(success);
        for (double x = -3; x <= 3; x += 1)
            Assert.Equal(expr.Evaluate(x), factored.Evaluate(x), precision: 6);
    }

    [Fact]
    public void Fails_for_multivariable_expression_with_wrong_variable_name()
    {
        Expr expr = ExprParser.Parse("y^2 - 4"); // polynomial in y, asking to factor by x
        var (_, success) = expr.TryFactorReal("x");
        Assert.False(success);
    }

    [Theory]
    [InlineData("(x - 2)^4", "(x - 2)^4", "(x - 2)^4")]
    [InlineData("(x + 1)*(x - 2)*x", "(x + 1) * (x - 2) * x", "(x + 1) * (x - 2) * x")]
    [InlineData("3(x - 1)^2 (x + 2)", "3 * (x + 2) * (x - 1)^2", "3 * (x + 2) * (x - 1)^2")]
    [InlineData("(x + 1)^2/2", "(1/2) * (x + 1)^2", "(1/2) * (x + 1)^2")]
    [InlineData("(x^2 + 1)*(x - 1)", "(x^2 + 1) * (x - 1)", "(x + i) * (x - 1) * (x - i)")]
    public void Factors_a_polynomial_that_is_not_expanded(string input, string real, string complex)
    {
        Expr expr = ExprParser.Parse(input);

        var (realFactored, realSuccess) = expr.TryFactorReal("x");
        var (complexFactored, complexSuccess) = expr.TryFactorComplex("x");

        Assert.True(realSuccess);
        Assert.True(complexSuccess);
        Assert.Equal(real, realFactored.Print());
        Assert.Equal(complex, complexFactored.Print());
    }
}

public class AbsValueTests
{
    [Fact]
    public void Abs_EvaluatesPositive()
    {
        var expr = new Abs(new Constant(-5));

        Assert.Equal(5, expr.Evaluate(new Dictionary<string, double>()));
    }

    [Fact]
    public void Abs_EvaluatesZero()
    {
        var expr = new Abs(new Constant(0));

        Assert.Equal(0, expr.Evaluate(new Dictionary<string, double>()));
    }

    [Fact]
    public void Abs_EvaluatesSymbolically()
    {
        var x = new Variable("x");
        var expr = new Abs(x);

        Assert.Equal(5, expr.Evaluate(
            new Dictionary<string, double> { ["x"] = -5 }));
    }
}

public class CoreCoverageBatchTests
{
    public static TheoryData<Expr, double, double> ArithmeticEvaluationCases
    {
        get
        {
            var data = new TheoryData<Expr, double, double>();

            for (int n = -25; n <= 25; n++)
            {
                data.Add(new Add(new Constant(n), new Variable("x")), 3.0, n + 3.0);
                data.Add(new Subtract(new Constant(n), new Variable("x")), 3.0, n - 3.0);
                data.Add(new Multiply(new Constant(n), new Variable("x")), 4.0, n * 4.0);
                data.Add(new Divide(new Constant(n), new Variable("x")), 2.0, n / 2.0);
                data.Add(new Power(new Variable("x"), new Constant(n)), 2.0, Math.Pow(2.0, n));
                data.Add(new Add(new Multiply(new Constant(2), new Variable("x")), new Constant(n)), 5.0, 10.0 + n);
                data.Add(new Subtract(new Multiply(new Constant(3), new Variable("x")), new Constant(n)), 4.0, 12.0 - n);
                data.Add(new Divide(new Add(new Variable("x"), new Constant(n)), new Constant(2)), 8.0, (8.0 + n) / 2.0);
            }

            return data;
        }
    }

    public static TheoryData<string, double, double> ParserEvaluationCases
    {
        get
        {
            var data = new TheoryData<string, double, double>();

            for (int n = -20; n <= 20; n++)
            {
                data.Add($"x + {n}", 4.0, 4.0 + n);
                data.Add($"x - {n}", 4.0, 4.0 - n);
                data.Add($"{n} - x", 4.0, n - 4.0);
                data.Add($"{n} * x", 4.0, n * 4.0);
                data.Add($"x * {n}", 4.0, 4.0 * n);
                data.Add($"(x + {n})^2", 2.0, Math.Pow(2.0 + n, 2.0));
                data.Add($"sin(x + {n})", 0.5, Math.Sin(0.5 + n));
                data.Add($"cos(x - {n})", 0.5, Math.Cos(0.5 - n));
                data.Add($"x^2 + {n}", 3.0, 9.0 + n);

                if (n != 0)
                {
                    data.Add($"x / {n}", 3.0, 3.0 / n);
                    data.Add($"{n} / x", 2.0, n / 2.0);
                }
            }

            return data;
        }
    }

    public static TheoryData<string, double, double> SimplificationCases
    {
        get
        {
            var data = new TheoryData<string, double, double>();

            for (int n = -15; n <= 15; n++)
            {
                data.Add($"x + 0 + {n}", 3.0, 3.0 + n);
                data.Add($"x * 1 + {n}", 6.0, 6.0 + n);
                data.Add($"x * 0 + {n}", 4.0, n);
                data.Add($"(x - x) + {n}", 5.0, n);
                data.Add($"(x^0) + {n}", 2.0, 1.0 + n);
                data.Add($"(x^1) + {n}", 2.0, 2.0 + n);
                data.Add($"0 / x + {n}", 7.0, n);
                data.Add($"sin(0)^2 + cos(0)^2 + {n}", 10.0, 1.0 + n);
                data.Add($"sqrt(x^2) + {n}", 7.0, 7.0 + n);
                data.Add($"abs(-x + {n})", -4.0, Math.Abs(4.0 + n));
            }

            return data;
        }
    }

    public static TheoryData<Expr, double, double> DifferentiationCases
    {
        get
        {
            var data = new TheoryData<Expr, double, double>();

            for (int n = 1; n <= 20; n++)
            {
                data.Add(new Power(new Variable("x"), new Constant(n)), 3.0, n * Math.Pow(3.0, n - 1));
                data.Add(new Multiply(new Constant(n), new Power(new Variable("x"), new Constant(2))), 4.0, 2.0 * n * 4.0);
                data.Add(new Add(new Variable("x"), new Multiply(new Constant(n), new Variable("x"))), 2.0, 1.0 + n);
                data.Add(new Divide(new Sin(new Variable("x")), new Variable("x")), 1.5, (Math.Cos(1.5) * 1.5 - Math.Sin(1.5)) / (1.5 * 1.5));
                data.Add(new Multiply(new Variable("x"), new Sin(new Variable("x"))), 2.0, Math.Sin(2.0) + 2.0 * Math.Cos(2.0));
                data.Add(new Exp(new Variable("x")), 1.2, Math.Exp(1.2));
                data.Add(new Ln(new Variable("x")), 2.5, 1.0 / 2.5);
                data.Add(new Sqrt(new Variable("x")), 9.0, 1.0 / (2.0 * Math.Sqrt(9.0)));
            }

            data.Add(new Add(new Sin(new Variable("x")), new Cos(new Variable("x"))), 0.75, Math.Cos(0.75) - Math.Sin(0.75));
            data.Add(new Multiply(new Constant(2), new Power(new Variable("x"), new Constant(3))), 2.5, 2.0 * 3.0 * Math.Pow(2.5, 2));
            data.Add(new Divide(new Constant(1), new Variable("x")), 2.0, -1.0 / (2.0 * 2.0));
            data.Add(new Subtract(new Power(new Variable("x"), new Constant(2)), new Constant(1)), 3.0, 6.0);
            data.Add(new Atan(new Variable("x")), 0.5, 1.0 / (1.0 + 0.25));
            data.Add(new Tanh(new Variable("x")), 0.3, 1.0 / Math.Cosh(0.3) / Math.Cosh(0.3));
            data.Add(new Cosh(new Variable("x")), 0.7, Math.Sinh(0.7));
            data.Add(new Sinh(new Variable("x")), 0.7, Math.Cosh(0.7));
            data.Add(new Asin(new Variable("x")), 0.2, 1.0 / Math.Sqrt(1.0 - 0.04));
            data.Add(new Acos(new Variable("x")), 0.2, -1.0 / Math.Sqrt(1.0 - 0.04));

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ArithmeticEvaluationCases))]
    public void Arithmetic_core_expressions_evaluate_correctly(Expr expr, double x, double expected)
    {
        Assert.Equal(expected, expr.Evaluate(new Dictionary<string, double> { ["x"] = x }), precision: 10);
    }

    [Theory]
    [MemberData(nameof(ParserEvaluationCases))]
    public void Parser_supports_core_expression_forms(string input, double x, double expected)
    {
        Expr expr = ExprParser.Parse(input, "x");
        Assert.Equal(expected, expr.Evaluate(new Dictionary<string, double> { ["x"] = x }), precision: 10);
    }

    [Theory]
    [MemberData(nameof(SimplificationCases))]
    public void Simplification_reduces_core_expressions(string input, double x, double expected)
    {
        Expr expr = ExprParser.Parse(input, "x").Simplify();
        Assert.Equal(expected, expr.Evaluate(new Dictionary<string, double> { ["x"] = x }), precision: 10);
    }

    [Theory]
    [MemberData(nameof(DifferentiationCases))]
    public void Differentiation_matches_expected_values(Expr expr, double x, double expected)
    {
        Expr derivative = expr.Differentiate("x");
        Assert.Equal(expected, derivative.Evaluate(new Dictionary<string, double> { ["x"] = x }), precision: 8);
    }
}
public class RationalTests
{
    [Theory]
    [InlineData(1e-20, "1", "100000000000000000000")]
    [InlineData(1e20, "100000000000000000000", "1")]
    [InlineData(-2.5e-7, "-1", "4000000")]
    [InlineData(1.5e300, "15", "1")] // numerator is 15 followed by 299 zeros
    public void FromDouble_handles_scientific_notation(double value, string expectedNumeratorPrefix, string expectedDenominator)
    {
        Rational r = Rational.FromDouble(value);

        Assert.StartsWith(expectedNumeratorPrefix, r.Numerator.ToString());
        Assert.Equal(expectedDenominator, r.Denominator.ToString());
        Assert.Equal(value, r.ToDouble(), 1e-12 * Math.Abs(value));
    }

    [Fact]
    public void Constant_accepts_tiny_and_huge_doubles()
    {
        Assert.Equal(new Rational(1, System.Numerics.BigInteger.Pow(10, 20)), new Constant(1e-20).Value);
        Assert.Equal(new Rational(System.Numerics.BigInteger.Pow(10, 20)), new Constant(1e20).Value);
    }

    [Theory]
    [InlineData("12.5", 25, 2)]
    [InlineData("-0.125", -1, 8)]
    [InlineData("1.5E-3", 3, 2000)]
    [InlineData("2.5e+2", 250, 1)]
    [InlineData("7", 7, 1)]
    public void FromDecimalString_parses_plain_and_exponent_forms(string text, long numerator, long denominator)
    {
        Assert.Equal(new Rational(numerator, denominator), Rational.FromDecimalString(text));
    }
}

public class EvaluationConventionTests
{
    private static readonly Dictionary<string, double> NoBindings = new();

    [Theory]
    [InlineData("round(0.5)", 1)]
    [InlineData("round(1.5)", 2)]
    [InlineData("round(2.5)", 3)]
    [InlineData("round(-2.5)", -3)]
    [InlineData("round(-0.5)", -1)]
    [InlineData("round(2.4)", 2)]
    [InlineData("round(-2.6)", -3)]
    public void Round_halves_away_from_zero_in_both_simplify_and_evaluate(string input, double expected)
    {
        Expr expr = ExprParser.Parse(input);

        Assert.Equal(expected, expr.Evaluate(NoBindings));
        Assert.Equal(new Constant((int)expected), expr.Simplify());
    }

    [Theory]
    [InlineData("nthroot(-8, 3)", -2)]
    [InlineData("nthroot(-32, 5)", -2)]
    [InlineData("nthroot(27, 3)", 3)]
    [InlineData("nthroot(16, 4)", 2)]
    public void Nthroot_is_the_real_root_in_both_simplify_and_evaluate(string input, double expected)
    {
        Expr expr = ExprParser.Parse(input);

        Assert.Equal(expected, expr.Evaluate(NoBindings)); // exact, not just close
        Assert.Equal(new Constant((int)expected), expr.Simplify());
    }

    [Fact]
    public void Nthroot_with_negative_odd_degree_is_real()
    {
        Expr expr = new NthRoot(new Constant(-8), new Constant(-3));
        Assert.Equal(-0.5, expr.Evaluate(NoBindings), precision: 12);
    }

    [Fact]
    public void Nthroot_of_negative_number_with_even_degree_is_undefined()
    {
        Assert.True(double.IsNaN(ExprParser.Parse("nthroot(-16, 4)").Evaluate(NoBindings)));
    }

    [Fact]
    public void Nthroot_complex_evaluation_matches_real_evaluation_on_the_real_line()
    {
        ComplexNumber result = ExprParser.Parse("nthroot(x, 3)", "x").EvaluateComplex(new ComplexNumber(-8));
        Assert.Equal(new ComplexNumber(-2), result);
    }

    [Fact]
    public void Power_with_fractional_exponent_is_the_principal_value()
    {
        // Unlike nthroot, x^(1/3) is the principal value: undefined over the reals for x < 0.
        Expr expr = new Power(new Constant(-8), new Constant(new Rational(1, 3)));

        Assert.IsType<Power>(expr.Simplify());
        Assert.True(double.IsNaN(expr.Evaluate(NoBindings)));

        ComplexNumber principal = expr.EvaluateComplex(new Dictionary<string, ComplexNumber>());
        Assert.Equal(1.0, principal.Real, precision: 10);
        Assert.Equal(Math.Sqrt(3), principal.Imaginary, precision: 10);
    }

    [Theory]
    [InlineData(-8.0, 1.0 / 12)]
    [InlineData(8.0, 1.0 / 12)]
    [InlineData(-1.0, 1.0 / 3)]
    public void Nthroot_derivative_is_real_where_the_function_is(double x, double expected)
    {
        Expr derivative = ExprParser.Parse("nthroot(x, 3)", "x").Differentiate("x");
        Assert.Equal(expected, derivative.Evaluate(x), precision: 12);
    }

    [Fact]
    public void Nthroot_derivative_with_negative_degree_is_real_for_negative_argument()
    {
        // d/dx x^(-1/3) = -1/3 * x^(-4/3); at x = -8 that is -1/3 * 1/16.
        Expr derivative = new NthRoot(new Variable("x"), new Constant(-3)).Differentiate("x");
        Assert.Equal(-1.0 / 48, derivative.Evaluate(-8.0), precision: 12);
    }

    [Theory]
    [InlineData("(x^3)^(1/3)", SimplifyMode.Strict, "(x^3)^(1/3)")] // undefined for x < 0, x is not
    [InlineData("(x^3)^(1/3)", SimplifyMode.Generic, "x")]
    [InlineData("(x^3)^2", SimplifyMode.Strict, "x^6")]              // integer outer exponent: exact
    public void Odd_inner_power_collapses_only_when_domain_is_preserved(string input, SimplifyMode mode, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input, "x").Simplify(mode).Print());
    }
}

public class NthRootSimplificationTests
{
    private static readonly Dictionary<string, double> NoBindings = new();

    [Theory]
    [InlineData(8, 1, 3, 2, 4, 1)]      // nthroot(8, 3/2) = 8^(2/3) = 4 (was 2: only the numerator 3 was used)
    [InlineData(16, 1, 4, 3, 8, 1)]     // nthroot(16, 4/3) = 16^(3/4) = 8
    [InlineData(4, 1, 1, 2, 16, 1)]     // nthroot(4, 1/2) = 4^2 = 16
    [InlineData(8, 1, -3, 1, 1, 2)]     // nthroot(8, -3) = 1/2
    [InlineData(-8, 1, -3, 1, -1, 2)]   // nthroot(-8, -3) = -1/2 (real root, odd degree)
    [InlineData(27, 8, 3, 1, 3, 2)]     // nthroot(27/8, 3) = 3/2
    public void Simplifies_exact_real_roots_for_any_rational_degree(
        long cNum, long cDen, long dNum, long dDen, long expectedNum, long expectedDen)
    {
        Expr expr = new NthRoot(new Constant(new Rational(cNum, cDen)), new Constant(new Rational(dNum, dDen)));
        Rational expected = new Rational(expectedNum, expectedDen);

        Assert.Equal(new Constant(expected), expr.Simplify());
        Assert.Equal(expected.ToDouble(), expr.Evaluate(NoBindings)); // Evaluate agrees exactly
    }

    [Theory]
    [InlineData(-8, 3, 2)] // negative base, non-integer degree: undefined over the reals
    [InlineData(0, -3, 1)] // 1/0
    [InlineData(8, 0, 1)]  // degree 0: 8^(1/0)
    [InlineData(2, 3, 1)]  // irrational
    public void Leaves_undefined_or_irrational_roots_symbolic(long c, long dNum, long dDen)
    {
        Expr expr = new NthRoot(new Constant(new Rational(c)), new Constant(new Rational(dNum, dDen)));
        Assert.IsType<NthRoot>(expr.Simplify());
    }

    [Fact]
    public void Does_not_overflow_on_huge_integer_degree()
    {
        var hugeOdd = new Rational(System.Numerics.BigInteger.Pow(10, 30) + 1);
        Expr expr = new NthRoot(new Constant(-8), new Constant(hugeOdd));
        Assert.IsType<NthRoot>(expr.Simplify());
    }
}

public class PrintRoundTripTests
{
    private static readonly string[] Vars = ["x", "y", "a", "c", "o", "s"];

    private static Expr X => new Variable("x");
    private static Expr Y => new Variable("y");
    private static Constant C(long n, long d = 1) => new(new Rational(n, d));

    public static TheoryData<Expr> TrickyExpressions => new()
    {
        new Power(X, C(1, 2)),                        // was "x^1/2" -> parsed as (x^1)/2
        new Power(C(-2), X),                          // was "-2^x" -> parsed as -(2^x)
        new Power(C(1, 2), X),
        new Power(C(-1, 2), X),
        new Divide(X, C(1, 2)),                       // was "x / 1/2" -> parsed as (x/1)/2
        new Multiply(C(1, 2), X),
        new Multiply(C(-3, 4), new Sin(X)),
        new Ceiling(X),                               // was "ceil(x)" -> parsed as e*i*c*l*x
        new Multiply(new Pi(), X),                    // "π" must parse back as pi
        new Multiply(new Variable("a"), new Sinh(X)), // implicit "asinh(x)" would be a different function
        new Multiply(new Multiply(new Variable("c"), new Variable("o")), new Variable("s")), // "cos"
        new Multiply(X, Y),
        new Multiply(C(2), X),
        new Multiply(C(-2), X),
        new Negate(new Power(X, C(2))),
        new Power(new Negate(X), C(2)),
        new Power(X, C(-1)),
        new Power(X, new Negate(Y)),
        new Power(C(2), new Power(C(3), C(2))),       // right-associative
        new Power(new Power(C(2), C(3)), C(2)),
        new Subtract(X, C(-2)),
        new Subtract(X, new Multiply(C(-2), X)),
        new Negate(new Add(X, C(1))),
        new Divide(C(1), new Multiply(C(2), X)),
        new Power(new Multiply(C(2), X), C(3)),
        new Multiply(new Exp(X), X),
        new NthRoot(X, C(3)),
        new Min(X, new Negate(Y)),
    };

    [Theory]
    [MemberData(nameof(TrickyExpressions))]
    public void Parse_of_Print_evaluates_to_the_same_value(Expr original)
    {
        string printed = original.Print();
        Expr reparsed = ExprParser.Parse(printed, Vars);

        foreach (double x in new[] { -1.7, 0.6, 2.3 })
        {
            var bindings = new Dictionary<string, double>
            {
                ["x"] = x, ["y"] = 1.3, ["a"] = 0.8, ["c"] = 1.1, ["o"] = -0.9, ["s"] = 2.0
            };

            double expected = original.Evaluate(bindings);
            double actual = reparsed.Evaluate(bindings);

            if (double.IsNaN(expected))
                Assert.True(double.IsNaN(actual), $"'{printed}' at x={x}: expected NaN, got {actual}");
            else
                Assert.True(Math.Abs(expected - actual) <= 1e-9 * Math.Max(1, Math.Abs(expected)),
                    $"'{printed}' at x={x}: expected {expected}, got {actual}");
        }
    }

    [Theory]
    [InlineData("x^(1/2)")]
    [InlineData("(-2)^x")]
    [InlineData("x / (1/2)")]
    [InlineData("ceiling(x)")]
    [InlineData("a * sinh(x)")]
    [InlineData("x^-1")]
    [InlineData("2x")]
    public void Prints_unambiguous_form(string expected)
    {
        Expr expr = expected switch
        {
            "x^(1/2)" => new Power(X, C(1, 2)),
            "(-2)^x" => new Power(C(-2), X),
            "x / (1/2)" => new Divide(X, C(1, 2)),
            "ceiling(x)" => new Ceiling(X),
            "a * sinh(x)" => new Multiply(new Variable("a"), new Sinh(X)),
            "x^-1" => new Power(X, C(-1)),
            "2x" => new Multiply(C(2), X),
            _ => throw new ArgumentOutOfRangeException(nameof(expected))
        };

        Assert.Equal(expected, expr.Print());
    }

    [Fact]
    public void Parser_accepts_pi_symbol()
    {
        Assert.IsType<Pi>(ExprParser.Parse("π"));
    }

    [Fact]
    public void Latex_parenthesizes_negative_and_fractional_power_bases()
    {
        Assert.Equal("\\left(-2\\right)^{x}", new Power(C(-2), X).ToLatex());
        Assert.Equal("\\left(\\frac{1}{2}\\right)^{x}", new Power(C(1, 2), X).ToLatex());
        Assert.Equal("x^{\\frac{1}{2}}", new Power(X, C(1, 2)).ToLatex());
    }

    [Fact]
    public void Latex_separates_floor_and_ceiling_commands_from_the_argument()
    {
        Assert.Equal(@"\left\lfloor x\right\rfloor", ExprParser.Parse("floor(x)").ToLatex());
        Assert.Equal(@"\left\lceil x\right\rceil", ExprParser.Parse("ceiling(x)").ToLatex());
        Assert.Equal(@"\left\lfloor 2x\right\rfloor", ExprParser.Parse("floor(2x)").ToLatex());
    }
}

public class AssumptionsTests
{
    [Theory]
    [InlineData(Signing.Positive, Signing.NonZero, Signing.Positive)]      // was NonZero: info lost
    [InlineData(Signing.NonZero, Signing.Positive, Signing.Positive)]
    [InlineData(Signing.Positive, Signing.NonNegative, Signing.Positive)]
    [InlineData(Signing.NonNegative, Signing.NonPositive, Signing.Zero)]
    [InlineData(Signing.NonNegative, Signing.NonZero, Signing.Positive)]
    [InlineData(Signing.NonPositive, Signing.NonZero, Signing.Negative)]
    [InlineData(Signing.Negative, Signing.Unknown, Signing.Negative)]
    [InlineData(Signing.Unknown, Signing.NonZero, Signing.NonZero)]
    public void Repeated_assumptions_narrow_the_sign(Signing first, Signing second, Signing expected)
    {
        Assumptions assumptions = Assumptions.None
            .Assume("x", signing: first)
            .Assume("x", signing: second);

        Assert.Equal(expected, assumptions.SigningOf("x"));
    }

    [Fact]
    public void Positive_then_NonZero_is_still_provably_positive()
    {
        Assumptions assumptions = Assumptions.None.AssumePositive("x").AssumeNonZero("x");
        Assert.True(assumptions.IsPositive("x"));
    }

    [Theory]
    [InlineData(Signing.Positive, Signing.Negative)]
    [InlineData(Signing.Positive, Signing.Zero)]
    [InlineData(Signing.Zero, Signing.NonZero)]
    [InlineData(Signing.NonNegative, Signing.Negative)]
    public void Contradictory_signs_throw(Signing first, Signing second)
    {
        Assumptions assumptions = Assumptions.None.Assume("x", signing: first);

        var ex = Assert.Throws<ArgumentException>(() => assumptions.Assume("x", signing: second));
        Assert.Contains("'x'", ex.Message);
        Assert.Contains("Contradictory", ex.Message);
    }

    [Fact]
    public void Natural_implies_positive_in_either_order()
    {
        Assert.Throws<ArgumentException>(() => Assumptions.None.AssumeNatural("n").AssumeNegative("n"));
        Assert.Throws<ArgumentException>(() => Assumptions.None.AssumeNonPositive("n").AssumeNatural("n"));

        Assumptions viaDomainOnly = Assumptions.None.Assume("n", domain: NumberDomain.Natural);
        Assert.True(viaDomainOnly.IsPositive("n"));

        Assumptions narrowed = Assumptions.None.AssumeNonZero("n").AssumeNatural("n");
        Assert.True(narrowed.IsPositive("n"));
    }

    [Fact]
    public void Integer_and_natural_are_also_rational_and_real()
    {
        Assumptions assumptions = Assumptions.None.AssumeInteger("k").AssumeNatural("n");

        Assert.True(assumptions.Has("k", NumberDomain.Rational));
        Assert.True(assumptions.IsReal("k"));
        Assert.True(assumptions.Has("n", NumberDomain.Rational | NumberDomain.Integer));
    }

    [Fact]
    public void Domains_accumulate_across_calls()
    {
        Assumptions assumptions = Assumptions.None.AssumeReal("x").AssumePositive("x").AssumeInteger("x");

        Assert.True(assumptions.IsInteger("x"));
        Assert.True(assumptions.IsReal("x"));
        Assert.True(assumptions.IsPositive("x"));
    }

    [Fact]
    public void Assume_never_mutates_the_original()
    {
        Assumptions original = Assumptions.None.AssumeNonZero("x");
        _ = original.AssumePositive("x");

        Assert.Equal(Signing.NonZero, original.SigningOf("x"));
        Assert.Equal(Signing.Unknown, Assumptions.None.SigningOf("x"));
    }
}

public class PythagoreanIdentityTests
{
    [Theory]
    [InlineData("1 - sin(x)^2", "cos(x)^2")]      // was dead code: stayed "-sin(x)^2 + 1"
    [InlineData("1 - cos(x)^2", "sin(x)^2")]      // was dead code
    [InlineData("sin(x)^2 - 1", "-cos(x)^2")]
    [InlineData("3 - 3cos(x)^2", "3sin(x)^2")]
    [InlineData("sin(x)^2 + cos(x)^2", "1")]
    [InlineData("cos(x)^2 + sin(x)^2", "1")]
    [InlineData("sin(x)^2 + cos(x)^2 + 1", "2")]    // extra term used to block the old two-node rule
    [InlineData("2sin(x)^2 + 2cos(x)^2", "2")]      // coefficients used to block it too
    [InlineData("2sin(x)^2 + 3cos(x)^2", "cos(x)^2 + 2")]
    [InlineData("y + sin(x)^2 + z + cos(x)^2", "y + z + 1")]
    [InlineData("sin(2x)^2 + cos(2x)^2", "1")]
    [InlineData("1 - sin(x)^2 - cos(x)^2", "0")]
    [InlineData("sin(x)^2 + sin(y)^2 + cos(x)^2 + cos(y)^2", "2")]
    public void Applies_pythagorean_identity_across_the_whole_sum(string input, string expected)
    {
        Expr expr = ExprParser.Parse(input, "x", "y", "z").Simplify(SimplifyMode.Strict);
        Assert.Equal(expected, expr.Print());
    }

    [Theory]
    [InlineData("sec(x)^2 - tan(x)^2", "1")]
    [InlineData("tan(x)^2 - sec(x)^2", "-1")]                 // the old two-node rule needed sec^2 first
    [InlineData("y - csc(x)^2 + cot(x)^2", "y - 1")]          // and the two terms next to each other
    [InlineData("2sec(x)^2 - 2tan(x)^2", "2")]
    [InlineData("2sec(x)^2 - tan(x)^2", "sec(x)^2 + 1")]      // the smaller coefficient's square goes
    [InlineData("sec(x)^2 - 2tan(x)^2", "-sec(x)^2 + 2")]
    [InlineData("3sin(x)^2 + 2cos(x)^2", "sin(x)^2 + 2")]     // was -cos(x)^2 + 3
    [InlineData("sin(x)^2 - tan(x)^2 + cos(x)^2 + sec(x)^2", "2")]
    public void Applies_sec_tan_and_csc_cot_identities_across_the_whole_sum(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input, "x", "y").Simplify().Print());
    }

    [Theory]
    [InlineData("sec(x)^2 - tan(x)^2", "sec(x)^2 - tan(x)^2")]    // 1 would be defined where cos(x) = 0
    [InlineData("csc(x)^2 - cot(x)^2", "csc(x)^2 - cot(x)^2")]
    [InlineData("2sec(x)^2 - tan(x)^2", "sec(x)^2 + 1")]          // sec(x)^2 keeps the domain
    public void Strict_mode_cancels_sec_and_tan_only_if_the_domain_stays(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input, "x").Simplify(SimplifyMode.Strict).Print());
    }

    [Theory]
    [InlineData("sin(x)^2 + cos(y)^2", "cos(y)^2 + sin(x)^2")] // different arguments
    [InlineData("5 - sin(x)^2", "-sin(x)^2 + 5")]                // constant doesn't match the coefficient
    public void Leaves_non_matching_sums_alone(string input, string expected)
    {
        Expr expr = ExprParser.Parse(input, "x", "y").Simplify();
        Assert.Equal(expected, expr.Print());
    }
}

public class RealRootScanTests
{
    private static Expr P(string s) => ExprParser.Parse(s, "x");

    [Theory]
    [InlineData("x^2", -5, 5, 200, new[] { 0.0 })]              // was 0 and a phantom 6.1e-6
    [InlineData("x^2", -5, 5, 201, new[] { 0.0 })]              // grid misses 0: was no root at all
    [InlineData("x^2 - 2x + 1", -3, 3, 200, new[] { 1.0 })]     // same: was no root at all
    [InlineData("x^2 - 2x + 1", -5, 5, 200, new[] { 1.0 })]     // was 1 and 1.0000061
    [InlineData("(x - 1)^3", -5, 5, 200, new[] { 1.0 })]        // was 1 and 1.00043
    [InlineData("x^4", -5, 5, 200, new[] { 0.0 })]              // was 0 and 0.0025
    [InlineData("(x - 1)^2 * (x + 2)", -5, 5, 200, new[] { -2.0, 1.0 })]
    [InlineData("(x - 0.3)^2", -5, 5, 201, new[] { 0.3 })]
    [InlineData("x^3 - x", -5, 5, 200, new[] { -1.0, 0.0, 1.0 })]
    [InlineData("x - 5", 5, 10, 200, new[] { 5.0 })]            // root on the left limit
    [InlineData("x - 10", 5, 10, 200, new[] { 10.0 })]          // root on the right limit
    public void Finds_each_root_exactly_once(string input, double left, double right, int steps, double[] expected)
    {
        IReadOnlyList<double> roots = P(input).FindRealRoots(left, right, steps);

        Assert.Equal(expected.Length, roots.Count);
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], roots[i], precision: 9);
    }

    [Theory]
    [InlineData("x^2 + 0.0000001")] // small positive minimum is not a root
    [InlineData("x^2 + 1")]
    [InlineData("1/x")]             // sign change at a pole
    [InlineData("exp(-x)")]         // approaches 0 but never reaches it
    public void Reports_no_false_roots(string input)
    {
        Assert.Empty(P(input).FindRealRoots(-5, 50, 401));
    }

    [Fact]
    public void Ignores_poles_of_tan()
    {
        IReadOnlyList<double> roots = P("tan(x)").FindRealRoots(-2, 2);
        Assert.Equal(0.0, Assert.Single(roots), precision: 12);
    }

    [Fact]
    public void Finds_touching_roots_of_sin_squared()
    {
        IReadOnlyList<double> roots = P("sin(x)^2").FindRealRoots(-7, 7);

        double[] expected = [-2 * Math.PI, -Math.PI, 0, Math.PI, 2 * Math.PI];
        Assert.Equal(expected.Length, roots.Count);
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], roots[i], precision: 9);
    }

    [Fact]
    public void Finds_double_root_on_infinite_interval()
    {
        Assert.Equal(0.0, Assert.Single(P("x^2").FindRealRoots()), precision: 9);
    }

    [Theory]
    [InlineData("x^2 - 2x + 1", "(x - 1)^2")]                  // used to fail: phantom root broke deflation
    [InlineData("x^4 - 8x^3 + 24x^2 - 32x + 16", "(x - 2)^4")] // (x - 2)^4 expanded
    public void Factors_polynomials_with_a_single_repeated_root(string input, string expected)
    {
        var (factored, success) = P(input).TryFactorReal("x");

        Assert.True(success);
        Assert.Equal(expected, factored.Print());
    }

    [Fact]
    public void Factors_polynomial_with_repeated_and_simple_roots()
    {
        // (x - 1)^2 (x + 2) expanded; repeated factors are grouped into a power by the factorizer.
        var (factored, success) = P("x^3 - 3x + 2").TryFactorReal("x");

        Assert.True(success);
        Assert.Equal("(x + 2) * (x - 1)^2", factored.Print());
    }
}

public class ExactFactoringTests
{
    private static Expr P(string s) => ExprParser.Parse(s, "x");

    private static void AssertSameFunction(Expr original, Expr factored)
    {
        foreach (var z in new[] { new ComplexNumber(-1.3, 0.4), new ComplexNumber(0.7, -2.1), new ComplexNumber(2.2, 0) })
        {
            ComplexNumber a = original.EvaluateComplex(new Dictionary<string, ComplexNumber> { ["x"] = z });
            ComplexNumber b = factored.EvaluateComplex(new Dictionary<string, ComplexNumber> { ["x"] = z });
            Assert.True((a - b).Magnitude <= 1e-9 * (1 + a.Magnitude), $"{factored.Print()} differs at {z}");
        }
    }

    // No constant in the result may be a long decimal-looking fraction: every Rational
    // must be the exact value, not a rationalized double like 14142135623731/10^13.
    private static void AssertNoApproximatedConstants(Expr expr)
    {
        switch (expr)
        {
            case Constant c:
                Assert.True(System.Numerics.BigInteger.Abs(c.Value.Denominator) < 1000,
                    $"constant {c.Value} looks like a rationalized double");
                Assert.True(System.Numerics.BigInteger.Abs(c.Value.Numerator) < 1000,
                    $"constant {c.Value} looks like a rationalized double");
                break;
            default:
                foreach (Expr child in Children(expr))
                    AssertNoApproximatedConstants(child);
                break;
        }
    }

    private static IEnumerable<Expr> Children(Expr expr) => expr switch
    {
        Add(var l, var r) => [l, r],
        Subtract(var l, var r) => [l, r],
        Multiply(var l, var r) => [l, r],
        Divide(var l, var r) => [l, r],
        Power(var l, var r) => [l, r],
        Negate(var a) => [a],
        Sqrt(var a) => [a],
        _ => []
    };

    [Theory]
    [InlineData("x^2 - 2", "(x + sqrt(2)) * (x - sqrt(2))")]           // was x - 14142135623731/10000000000000
    [InlineData("x^2 - x - 1", "(x + (1/2) * sqrt(5) - 1/2) * (x - (1/2) * sqrt(5) - 1/2)")]
    [InlineData("x^4 - 5x^2 + 6", "(x + sqrt(2)) * (x + sqrt(3)) * (x - sqrt(2)) * (x - sqrt(3))")]
    [InlineData("6x^2 - 5x + 1", "6 * (x - 1/3) * (x - 1/2)")]
    [InlineData("x^4 + 5x^2 + 4", "(x^2 + 1) * (x^2 + 4)")]         // irreducible quadratics kept
    [InlineData("x^5 - x", "(x^2 + 1) * (x + 1) * (x - 1) * x")]
    public void Factors_over_the_reals_exactly(string input, string expected)
    {
        Expr original = P(input);
        var (factored, success) = original.TryFactorReal("x");

        Assert.True(success);
        Assert.Equal(expected, factored.Print());
        AssertNoApproximatedConstants(factored);
        AssertSameFunction(original, factored);
    }

    [Theory]
    [InlineData("x^2 + 1", "(x + i) * (x - i)")]
    [InlineData("x^2 + 2", "(x + i * sqrt(2)) * (x - i * sqrt(2))")]  // was x - 1.41421...i
    [InlineData("x^4 + 5x^2 + 4", "(x + i) * (x + 2 * i) * (x - i) * (x - 2 * i)")]
    public void Factors_over_the_complex_numbers_exactly(string input, string expected)
    {
        Expr original = P(input);
        var (factored, success) = original.TryFactorComplex("x");

        Assert.True(success);
        Assert.Equal(expected, factored.Print());
        AssertNoApproximatedConstants(factored);
        AssertSameFunction(original, factored);
    }

    [Theory]
    [InlineData("x^3 - 2", false)] // real root 2^(1/3): no exact linear factor over Q or with square roots
    [InlineData("x^3 - 2", true)]
    [InlineData("x^8 - 1", true)]  // contains x^4 + 1, whose roots aren't square-root expressions of rationals
    [InlineData("x^2 + 1", false)] // no real roots
    public void Refuses_rather_than_approximating(string input, bool complex)
    {
        Expr original = P(input);
        var (factored, success) = complex ? original.TryFactorComplex("x") : original.TryFactorReal("x");

        Assert.False(success);
        Assert.Equal(original, factored);
    }
}

public class ComplexEdgeCaseTests
{
    [Theory]
    [InlineData(-1.0)]
    [InlineData(-2.5)]
    public void Zero_to_negative_real_power_is_positive_infinity(double exponent)
    {
        ComplexNumber result = ComplexNumber.Pow(ComplexNumber.Zero, new ComplexNumber(exponent)); // was 0

        Assert.Equal(double.PositiveInfinity, result.Real);
        Assert.Equal(0, result.Imaginary);
        Assert.Equal(Math.Pow(0, exponent), result.Real); // same as the real Math.Pow
    }

    [Fact]
    public void Zero_to_power_with_positive_real_part_is_zero()
    {
        Assert.Equal(ComplexNumber.Zero, ComplexNumber.Pow(ComplexNumber.Zero, new ComplexNumber(2)));
        Assert.Equal(ComplexNumber.Zero, ComplexNumber.Pow(ComplexNumber.Zero, new ComplexNumber(1, 1)));
    }

    [Fact]
    public void Zero_to_zero_is_one()
    {
        Assert.Equal(ComplexNumber.One, ComplexNumber.Pow(ComplexNumber.Zero, ComplexNumber.Zero));
    }

    [Fact]
    public void Zero_to_purely_imaginary_power_is_undefined()
    {
        ComplexNumber result = ComplexNumber.Pow(ComplexNumber.Zero, ComplexNumber.ImaginaryUnit); // was 0
        Assert.True(double.IsNaN(result.Real) && double.IsNaN(result.Imaginary));
    }

    [Fact]
    public void Complex_evaluation_of_negative_power_at_zero_matches_real_evaluation()
    {
        Expr expr = ExprParser.Parse("x^-1", "x");
        Assert.Equal(expr.Evaluate(0.0), expr.EvaluateComplex(ComplexNumber.Zero).Real);
    }

    [Theory]
    [InlineData(1e200, 1e200, 1.4142135623730951e200)]    // was Infinity
    [InlineData(1e-200, 1e-200, 1.4142135623730951e-200)] // was 0
    [InlineData(3, 4, 5)]
    public void Magnitude_neither_overflows_nor_underflows(double re, double im, double expected)
    {
        double magnitude = new ComplexNumber(re, im).Magnitude;
        Assert.Equal(expected, magnitude, expected * 1e-15);
    }

    [Fact]
    public void Division_of_huge_numbers_does_not_overflow()
    {
        ComplexNumber result = new ComplexNumber(1e200, 1e200) / new ComplexNumber(1e200, 1e200); // was 0
        Assert.Equal(1.0, result.Real, precision: 15);
        Assert.Equal(0.0, result.Imaginary, precision: 15);
    }

    [Fact]
    public void Division_by_tiny_number_does_not_underflow()
    {
        ComplexNumber result = ComplexNumber.One / new ComplexNumber(0, 1e-200); // 1 / (1e-200 i) = -1e200 i
        Assert.Equal(0.0, result.Real);
        Assert.Equal(-1e200, result.Imaginary, 1e185);
    }

    [Theory]
    [InlineData(1, 2, 3, 4, 0.44, 0.08)]
    [InlineData(1, 0, 0, 1, 0, -1)]
    [InlineData(5, -3, -2, 7, -31.0 / 53, -29.0 / 53)]
    public void Division_matches_textbook_formula_for_ordinary_values(
        double a, double b, double c, double d, double expectedRe, double expectedIm)
    {
        ComplexNumber result = new ComplexNumber(a, b) / new ComplexNumber(c, d);
        Assert.Equal(expectedRe, result.Real, precision: 12);
        Assert.Equal(expectedIm, result.Imaginary, precision: 12);
    }

    [Theory]
    [InlineData(1, 0, double.PositiveInfinity, 0)]                       // was NaN
    [InlineData(-1, 0, double.NegativeInfinity, 0)]
    [InlineData(0, 1, 0, double.PositiveInfinity)]                       // i/0 = inf*i, not NaN + inf*i
    [InlineData(0, -2, 0, double.NegativeInfinity)]
    [InlineData(1, 1, double.PositiveInfinity, double.PositiveInfinity)]
    [InlineData(-3, 2, double.NegativeInfinity, double.PositiveInfinity)]
    public void Division_by_zero_is_infinite_in_the_direction_of_the_numerator(
        double a, double b, double expectedRe, double expectedIm)
    {
        ComplexNumber result = new ComplexNumber(a, b) / ComplexNumber.Zero;
        Assert.Equal(expectedRe, result.Real);
        Assert.Equal(expectedIm, result.Imaginary);
    }

    [Fact]
    public void Zero_divided_by_zero_is_undefined()
    {
        ComplexNumber result = ComplexNumber.Zero / ComplexNumber.Zero;
        Assert.True(double.IsNaN(result.Real) && double.IsNaN(result.Imaginary));
    }

    [Fact]
    public void Division_by_real_or_imaginary_number_keeps_infinite_parts()
    {
        var infinity = new ComplexNumber(double.PositiveInfinity);

        Assert.Equal(infinity, infinity / new ComplexNumber(2));                        // was inf + NaN*i
        Assert.Equal(new ComplexNumber(0, double.NegativeInfinity), infinity / ComplexNumber.ImaginaryUnit);
    }

    [Theory]
    [InlineData("1/x", 0)]
    [InlineData("-1/x", 0)]
    [InlineData("x/0", -2)]
    [InlineData("atanh(x)", 1)]      // ln(2/0) / 2: was NaN
    [InlineData("atanh(x)", -1)]     // ln(0/2) / 2: was NaN
    [InlineData("csch(x)", 0)]
    [InlineData("coth(x)", 0)]
    public void Complex_evaluation_at_a_pole_matches_real_evaluation(string input, double x)
    {
        Expr expr = ExprParser.Parse(input, "x");
        ComplexNumber result = expr.EvaluateComplex(x);

        Assert.True(double.IsInfinity(expr.Evaluate(x)));
        Assert.Equal(expr.Evaluate(x), result.Real);
        Assert.Equal(0, result.Imaginary);
    }

    [Fact]
    public void Sqrt_of_huge_number_is_finite()
    {
        ComplexNumber root = ComplexNumber.Sqrt(new ComplexNumber(1e300, 1e300)); // magnitude used to overflow
        Assert.True(double.IsFinite(root.Real) && double.IsFinite(root.Imaginary));

        ComplexNumber squared = root * root;
        Assert.Equal(1.0, squared.Real / 1e300, precision: 12);
        Assert.Equal(1.0, squared.Imaginary / 1e300, precision: 12);
    }
}

public class ProductCombiningTests
{
    private static readonly string[] Vars = ["x", "y", "z", "n", "m"];

    private static string S(string input, SimplifyMode mode = SimplifyMode.Generic, Assumptions? assumptions = null) =>
        ExprParser.Parse(input, Vars).Simplify(assumptions ?? Assumptions.None, mode).Print();

    [Theory]
    [InlineData("x*y*x*y", "x^2 * y^2")]           // was x ^ 2 * y * y
    [InlineData("2*x*3*x", "6x^2")]               // was 6 * x * x
    [InlineData("y*x*z*x*y*x", "x^3 * y^2 * z")]
    [InlineData("x^2*y*x^3", "x^5 * y")]
    [InlineData("x*(x*y)*x", "x^3 * y")]
    [InlineData("(x+1)*y*(x+1)", "(x + 1)^2 * y")]
    [InlineData("sin(x)*y*sin(x)*y", "sin(x)^2 * y^2")]
    [InlineData("2^x*3*2^y", "3 * 2^(x + y)")]
    [InlineData("2*x*y*0.5", "x * y")]
    public void Merges_repeated_factors_anywhere_in_a_product(string input, string expected)
    {
        Assert.Equal(expected, S(input));
    }

    [Theory]
    [InlineData("-x*y*x", "-x^2 * y")]
    [InlineData("-(x*y)*x", "-x^2 * y")]
    [InlineData("x*(-y)*x*(-y)", "x^2 * y^2")]
    [InlineData("(-x)*(-x)", "x^2")]
    [InlineData("x*y*(-1)", "-x * y")]
    public void Pulls_negations_out_of_the_product(string input, string expected)
    {
        Assert.Equal(expected, S(input));
    }

    [Fact]
    public void Symbolic_exponents_add_up()
    {
        Assert.Equal("x^(m + n) * y", S("x^n*y*x^m"));
    }

    [Fact]
    public void Respects_domain_rules_in_strict_mode()
    {
        // x * x^-1 would become defined at x = 0, so Strict keeps them apart...
        Assert.Equal("x^-1 * x * y", S("x*y*x^-1", SimplifyMode.Strict));
        // ...unless x is known to be nonzero; Generic merges them either way.
        Assert.Equal("y", S("x*y*x^-1", SimplifyMode.Strict, Assumptions.None.AssumeNonZero("x")));
        Assert.Equal("y", S("x*y*x^-1"));
    }

    [Fact]
    public void Long_chains_collapse_completely()
    {
        string input = string.Join("*", Enumerable.Range(0, 40).Select(i => "xyz"[i % 3].ToString()));
        Assert.Equal("x^14 * y^13 * z^13", S(input));
    }

    [Theory]
    [InlineData("x*y*x*y")]
    [InlineData("-x*y*x")]
    [InlineData("x*(-y)*x*(-y)")]
    [InlineData("x^n*y*x^m")]
    [InlineData("(x/y)*x*y")]
    public void Result_has_the_same_value(string input)
    {
        Expr original = ExprParser.Parse(input, Vars);
        Expr simplified = original.Simplify();
        var bindings = new Dictionary<string, double> { ["x"] = 1.7, ["y"] = -0.6, ["z"] = 2.3, ["n"] = 1.5, ["m"] = -0.5 };

        Assert.Equal(original.Evaluate(bindings), simplified.Evaluate(bindings), precision: 10);
    }
}

// A node type defined outside Epsilon.Core, the way a contributor or another module would.
// It only implements its own math - no registration anywhere else.
public sealed class Sigmoid(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1 / (1 + Math.Exp(-Argument.Evaluate(bindings)));

    protected override Expr DifferentiateCore(string variable) =>
        new Multiply(new Multiply(this, new Subtract(new Constant(1), this)), Argument.Differentiate(variable));

    protected override Expr WithArgument(Expr argument) => new Sigmoid(argument);

    public override string ToString() => $"sigmoid({Argument})";
}

public class ExprTreeApiTests
{
    private static readonly Variable X = new("x");
    private static readonly Variable Y = new("y");

    [Fact]
    public void Children_lists_direct_subexpressions_in_order()
    {
        // Assert.Equal<Expr> compares element by element; ImmutableArray's own Equals
        // only compares the underlying array reference.
        Assert.Empty(new Constant(3).Children);
        Assert.Empty(X.Children);
        Assert.Equal<Expr>([X], new Sin(X).Children);
        Assert.Equal<Expr>([X, Y], new Divide(X, Y).Children);
        Assert.Equal<Expr>([X, new Constant(3)], new NthRoot(X, new Constant(3)).Children);
    }

    [Fact]
    public void Children_cannot_be_used_to_mutate_a_node()
    {
        var node = new Add(X, Y);

        // Viewed through the mutable collection interfaces, writes are rejected.
        IList<Expr> asList = node.Children;
        Assert.Throws<NotSupportedException>(() => asList[0] = new Constant(1));
        Assert.IsNotType<Expr[]>((object)node.Children);

        Assert.Same(X, node.Left);
        Assert.Equal(new Add(X, Y), node);
    }

    [Fact]
    public void Canonical_order_does_not_tie_nodes_from_different_namespaces_with_the_same_name()
    {
        // Epsilon.Tests.Other.Sin has the same short name as Epsilon.Core.Sin. Comparing
        // only short names made them tie, so the canonical order followed the input order.
        Expr coreSin = new Sin(X);
        Expr otherSin = new Epsilon.Tests.Other.Sin(X);

        Expr ab = new Add(coreSin, otherSin).Canonicalize();
        Expr ba = new Add(otherSin, coreSin).Canonicalize();

        Assert.Equal(ab, ba);
    }

    [Fact]
    public void WithChildren_rebuilds_the_same_node_type()
    {
        Expr rebuilt = new Power(X, new Constant(2)).WithChildren([Y, new Constant(3)]);
        Assert.Equal(new Power(Y, new Constant(3)), rebuilt);

        Assert.Equal(new Cos(Y), new Cos(X).WithChildren([Y]));
    }

    [Fact]
    public void WithChildren_returns_the_same_instance_when_nothing_changed()
    {
        var node = new Add(X, Y);
        Assert.Same(node, node.WithChildren([X, Y]));

        var leaf = new Constant(1);
        Assert.Same(leaf, leaf.WithChildren([]));
    }

    [Fact]
    public void WithChildren_rejects_the_wrong_number_of_children()
    {
        Assert.Throws<ArgumentException>(() => new Sin(X).WithChildren([X, Y]));
        Assert.Throws<ArgumentException>(() => new Add(X, Y).WithChildren([X]));
        Assert.Throws<ArgumentException>(() => X.WithChildren([Y]));
    }

    [Fact]
    public void MapChildren_allocates_nothing_when_no_child_changes()
    {
        Expr expr = new Multiply(new Sin(X), Y);
        Assert.Same(expr, expr.MapChildren(child => child));

        Expr mapped = expr.MapChildren(child => child is Variable ? new Constant(2) : child);
        Assert.Equal(new Multiply(new Sin(X), new Constant(2)), mapped);
    }

    [Fact]
    public void Substitute_keeps_untouched_subtrees_by_reference()
    {
        Expr untouched = new Sin(Y);
        Expr expr = new Add(untouched, X);

        var substituted = (Add)expr.Substitute("x", new Constant(1));
        Assert.Same(untouched, substituted.Left);
    }

    [Fact]
    public void Custom_node_gets_equality_hashing_and_ordering_for_free()
    {
        Assert.Equal(new Sigmoid(X), new Sigmoid(X));
        Assert.NotEqual<Expr>(new Sigmoid(X), new Sigmoid(Y));
        Assert.NotEqual<Expr>(new Sigmoid(X), new Sin(X));
        Assert.Equal(new Sigmoid(X).GetHashCode(), new Sigmoid(X).GetHashCode());

        // Canonical ordering and like-term collection used to throw NotSupportedException
        // for any node type they didn't list explicitly.
        Expr sum = new Add(new Add(new Sigmoid(X), Y), new Sigmoid(X)).Simplify();
        Assert.Equal(new Add(new Multiply(new Constant(2), new Sigmoid(X)), Y), sum);
    }

    [Fact]
    public void Custom_node_gets_variables_substitution_and_differentiation_for_free()
    {
        Expr expr = new Sigmoid(new Multiply(new Constant(2), X));

        Assert.Equal(new HashSet<string> { "x" }, expr.GetVariables());
        Assert.Equal(new Sigmoid(new Multiply(new Constant(2), Y)), expr.Substitute("x", Y));
        Assert.Equal(new Constant(0), expr.Differentiate("y"));

        // d/dx sigmoid(2x) = 2 * s * (1 - s); check numerically at x = 0.3.
        double s = 1 / (1 + Math.Exp(-0.6));
        Assert.Equal(2 * s * (1 - s), expr.Differentiate("x").Evaluate(0.3), precision: 12);
    }
}

public class ExprOperatorTests
{
    private static readonly Expr X = new Variable("x");
    private static readonly Expr Y = new Variable("y");

    [Fact]
    public void Operators_build_the_matching_nodes()
    {
        Assert.Equal(new Add(X, Y), X + Y);
        Assert.Equal(new Subtract(X, Y), X - Y);
        Assert.Equal(new Multiply(X, Y), X * Y);
        Assert.Equal(new Divide(X, Y), X / Y);
        Assert.Equal(new Negate(X), -X);
        Assert.Equal(new Power(X, new Constant(2)), X.Pow(2));
    }

    [Fact]
    public void Operators_only_construct_and_do_not_simplify()
    {
        Assert.Equal(new Add(X, new Constant(0)), X + 0);
    }

    [Fact]
    public void Follow_normal_csharp_precedence()
    {
        // 2 * x + 1 is (2 * x) + 1, and -x * y is (-x) * y - same as in C#.
        Assert.Equal(new Add(new Multiply(new Constant(2), X), new Constant(1)), 2 * X + 1);
        Assert.Equal(new Multiply(new Negate(X), Y), -X * Y);
    }

    [Fact]
    public void Numbers_convert_on_either_side()
    {
        Assert.Equal(new Multiply(new Constant(3), X), 3 * X);
        Assert.Equal(new Divide(new Constant(1), X), 1 / X);
        Assert.Equal(new Subtract(X, new Constant(new Rational(10_000_000_000L))), X - 10_000_000_000L);
        Assert.Equal(new Add(X, new Constant(new Rational(1, 3))), X + new Rational(1, 3));
    }

    [Theory]
    [InlineData(0.1, 1, 10)] // the decimal the author wrote, not the nearest binary double
    [InlineData(0.5, 1, 2)]
    [InlineData(-2.25, -9, 4)]
    public void Doubles_convert_to_their_exact_decimal_value(double value, long numerator, long denominator)
    {
        Expr converted = value;
        Assert.Equal(new Constant(new Rational(numerator, denominator)), converted);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Non_finite_doubles_are_rejected(double value)
    {
        Assert.Throws<ArgumentException>(() => { Expr _ = value; });
    }

    [Fact]
    public void Builds_a_realistic_expression()
    {
        Expr f = 0.5 * X * X + 2 * X - 1;

        Assert.Equal(0.5 * 3 * 3 + 2 * 3 - 1, f.Evaluate(3.0));
        Assert.Equal("(1/2) * x^2 + 2x - 1", f.Simplify().Print());
        Assert.Equal("x + 2", f.Differentiate("x").Simplify().Print());
    }

    [Fact]
    public void Works_with_function_nodes()
    {
        Expr wave = 2 * new Sin(3 * X) + new Cos(X).Pow(2);

        double x = 0.4;
        Assert.Equal(2 * Math.Sin(3 * x) + Math.Pow(Math.Cos(x), 2), wave.Evaluate(x), precision: 12);
    }
}

public class ApiNamingTests
{
    [Fact]
    public void ComplexNumber_does_not_clash_with_System_Numerics()
    {
        // Both namespaces are imported by this file; an unqualified Complex from Epsilon
        // would make this line ambiguous (CS0104).
        System.Numerics.Complex bcl = new(1, 2);
        ComplexNumber ours = new(bcl.Real, bcl.Imaginary);

        Assert.Equal(new ComplexNumber(1, 2), ours);
    }

    [Fact]
    public void EulerNumber_parses_prints_and_evaluates_as_e()
    {
        Expr e = ExprParser.Parse("e");

        Assert.IsType<EulerNumber>(e);
        Assert.Equal("e", e.ToString());
        Assert.Equal(Math.E, e.Evaluate(0));
    }

    [Fact]
    public void Constant_accepts_long_without_ambiguity()
    {
        long big = 1L << 40;

        Assert.Equal(new Constant(new Rational(big)), new Constant(big));
    }

    [Fact]
    public void Single_argument_Evaluate_ignores_the_argument_for_constant_expressions()
    {
        Expr expr = ExprParser.Parse("2*pi");

        Assert.Equal(2 * Math.PI, expr.Evaluate(123), precision: 12);
        Assert.Equal(new ComplexNumber(2 * Math.PI), expr.EvaluateComplex(new ComplexNumber(0, 1)));
    }

    [Fact]
    public void Assume_takes_a_camel_case_signing_argument()
    {
        Assumptions a = Assumptions.None.Assume("x", signing: Signing.Positive);

        Assert.True(a.IsPositive("x"));
    }
}

public class StrictParserTests
{
    [Theory]
    [InlineData("1e-5", 1e-5)]
    [InlineData("2e5", 2e5)]
    [InlineData("2.5E3", 2500)]
    [InlineData(".5e+1", 5)]
    public void Parses_scientific_notation_as_one_number(string input, double expected)
    {
        Expr expr = ExprParser.Parse(input);

        Assert.IsType<Constant>(expr);
        Assert.Equal(expected, expr.Evaluate(0), precision: 15);
    }

    [Fact]
    public void Scientific_notation_is_exact()
    {
        Assert.Equal(new Constant(new Rational(1, 100000)), ExprParser.Parse("1e-5"));
    }

    [Theory]
    [InlineData("2e", 2 * Math.E)]
    [InlineData("2e-1", 0.2)]
    [InlineData("2e - 1", 2 * Math.E - 1)]
    [InlineData("2e-x", 2 * Math.E - 3)]
    public void E_without_exponent_digits_is_still_eulers_number(string input, double expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).Evaluate(3), precision: 12);
    }

    [Fact]
    public void Rejects_absurd_exponents_instead_of_building_huge_numbers()
    {
        Assert.Throws<FormatException>(() => ExprParser.Parse("1e999999999"));
        Assert.Throws<FormatException>(() => ExprParser.Parse("1e-99999999999999"));
    }

    [Theory]
    [InlineData("1.5.2")]
    [InlineData("1..2")]
    public void Rejects_malformed_numbers(string input)
    {
        var ex = Assert.Throws<FormatException>(() => ExprParser.Parse(input));
        Assert.Contains("Invalid number", ex.Message);
    }

    [Theory]
    [InlineData("sin(x, 2)")]
    [InlineData("abs(x, y)")]
    [InlineData("sqrt(4, 2)")]
    public void Rejects_extra_arguments_to_one_argument_functions(string input)
    {
        var ex = Assert.Throws<FormatException>(() => ExprParser.Parse(input));
        Assert.Contains("exactly 1 argument", ex.Message);
    }

    [Theory]
    [InlineData("2 3")]
    [InlineData("1.5 2")]
    [InlineData("x + 2 3")]
    public void Rejects_two_numbers_without_an_operator(string input)
    {
        var ex = Assert.Throws<FormatException>(() => ExprParser.Parse(input));
        Assert.Contains("Missing operator", ex.Message);
    }

    [Theory]
    [InlineData("2x")]
    [InlineData("2(x + 1)")]
    [InlineData("x 2")]
    [InlineData("(2)(3)")]
    public void Keeps_other_implicit_multiplication(string input)
    {
        Assert.IsType<Multiply>(ExprParser.Parse(input));
    }

    [Theory]
    [InlineData("x + y", "y")]
    [InlineData("sen(x)", "sen")]
    [InlineData("xy", "y")]
    public void Rejects_undeclared_identifiers_when_variables_are_declared(string input, string unknown)
    {
        var ex = Assert.Throws<FormatException>(() => ExprParser.Parse(input, "x"));
        Assert.Contains($"'{unknown}'", ex.Message);
    }

    [Fact]
    public void Rejects_x_when_only_other_variables_are_declared()
    {
        Assert.Throws<FormatException>(() => ExprParser.Parse("x + t", "t"));
    }

    [Fact]
    public void Declared_variables_still_combine_with_constants_and_functions()
    {
        Expr expr = ExprParser.Parse("2pi t + sin(t) + e", "t");

        Assert.Equal(new HashSet<string> { "t" }, expr.GetVariables());
    }

    [Fact]
    public void Without_declared_variables_every_letter_is_a_variable()
    {
        Assert.Equal(new HashSet<string> { "x", "y" }, ExprParser.Parse("xy + y").GetVariables());
    }
}

public class NestingLimitTests
{
    private static string Repeat(string text, int count) => string.Concat(Enumerable.Repeat(text, count));

    private static string Chain(string op, int terms) => string.Join($" {op} ", Enumerable.Repeat("x", terms));

    // 1 MB is the default thread stack on Windows, so the limits must hold on it. Running the
    // check on its own thread also means an overflow is reported for this test, not as a
    // crash of the whole test run.
    private static void RunOnSmallStack(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        }, maxStackSize: 1024 * 1024);

        thread.Start();
        thread.Join();

        if (error is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(error);
    }

    public static TheoryData<string> InputsAtTheLimit => new()
    {
        Repeat("(", 256) + "x" + Repeat(")", 256),
        Repeat("sin(", 256) + "x" + Repeat(")", 256),
        Repeat("x / (", 256) + "x" + Repeat(")", 256),
        Repeat("-", 256) + "x",
        Chain("^", 257),
        Chain("-", 500),
        Chain("/", 500),
    };

    public static TheoryData<string> InputsOverTheLimit => new()
    {
        Repeat("(", 257) + "x" + Repeat(")", 257),
        Repeat("sin(", 257) + "x" + Repeat(")", 257),
        Repeat("-", 257) + "x",
        Chain("^", 258),
        Chain("-", 501),
        Chain("*", 501),
        "sin(" + Chain("+", 500) + ")",
        Repeat("(", 100_000) + "x" + Repeat(")", 100_000),
    };

    [Theory]
    [MemberData(nameof(InputsAtTheLimit))]
    public void Input_at_the_limit_is_parsed_evaluated_and_printed_on_a_small_stack(string input)
    {
        RunOnSmallStack(() =>
        {
            Expr expr = ExprParser.Parse(input);

            expr.Evaluate(0.5);
            expr.EvaluateComplex(new Dictionary<string, ComplexNumber> { ["x"] = new ComplexNumber(0.5) });
            Assert.Equal(expr, ExprParser.Parse(expr.Print()));
            expr.ToLatex();
        });
    }

    [Theory]
    [MemberData(nameof(InputsOverTheLimit))]
    public void Input_over_the_limit_is_rejected_instead_of_overflowing_the_stack(string input)
    {
        RunOnSmallStack(() =>
        {
            var ex = Assert.Throws<FormatException>(() => ExprParser.Parse(input));
            Assert.Contains("too deep", ex.Message);
        });
    }
}

public class ComplexNumberFormattingTests
{
    [Theory]
    [InlineData(1.5, 0, "1.5")]
    [InlineData(0, -2.5, "-2.5i")]
    [InlineData(1.5, 2.5, "1.5 + 2.5i")]
    [InlineData(1.5, -2.5, "1.5 - 2.5i")]
    public void Uses_invariant_culture_regardless_of_current_culture(double re, double im, string expected)
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            // uk-UA uses a comma as the decimal separator.
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("uk-UA");
            Assert.Equal(expected, new ComplexNumber(re, im).ToString());
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = saved;
        }
    }

    [Theory]
    [InlineData(double.NaN, double.NaN)]
    [InlineData(double.NaN, 1)]
    [InlineData(1, double.NaN)]
    public void Prints_any_NaN_part_as_a_single_NaN(double re, double im)
    {
        Assert.Equal("NaN", new ComplexNumber(re, im).ToString());
    }
}

public class ProductSimplificationAfterCleanupTests
{
    [Theory]
    [InlineData("2 * 3", "6")]
    [InlineData("1 * x", "x")]
    [InlineData("x * 1", "x")]
    [InlineData("0 * x", "0")]
    [InlineData("x^2 * x^3", "x^5")]
    [InlineData("x * x^2", "x^3")]
    [InlineData("x^2 * x", "x^3")]
    [InlineData("x * x", "x^2")]
    public void Binary_products_still_simplify_through_CombineProductFactors(string input, string expected)
    {
        Assert.Equal(ExprParser.Parse(expected), ExprParser.Parse(input).Simplify());
    }

    [Fact]
    public void Strict_squares_equal_factors_it_may_not_merge()
    {
        Expr simplified = ExprParser.Parse("x^(1/2) * x^(1/2)").Simplify(SimplifyMode.Strict);

        Assert.Equal(new Power(new Power(new Variable("x"), new Constant(new Rational(1, 2))), new Constant(2)), simplified);
    }
}

public class CultureInvariantPrintingTests
{
    // sv-SE formats negative numbers with U+2212 (−) instead of '-'.
    private static T InSwedishCulture<T>(Func<T> action)
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("sv-SE");
            return action();
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = saved;
        }
    }

    [Theory]
    [InlineData("(-2)^x")]
    [InlineData("x^(-3/4)")]
    [InlineData("-3/4 * x - 2")]
    public void Print_output_round_trips_under_a_culture_with_a_unicode_minus(string input)
    {
        Expr expr = ExprParser.Parse(input).Simplify();

        string printed = InSwedishCulture(expr.Print);

        Assert.DoesNotContain('−', printed);
        Assert.Equal(expr, ExprParser.Parse(printed).Simplify());
    }

    [Fact]
    public void Rational_ToString_uses_an_ascii_minus()
    {
        Assert.Equal("-3/4", InSwedishCulture(() => new Rational(-3, 4).ToString()));
        Assert.Equal("-12", InSwedishCulture(() => new Rational(-12).ToString()));
    }

    [Fact]
    public void Latex_uses_an_ascii_minus()
    {
        string latex = InSwedishCulture(() => ExprParser.Parse("(-2)^x").Simplify().ToLatex());

        Assert.DoesNotContain('−', latex);
    }
}

public class DomainEdgeRootTests
{
    private static IReadOnlyList<double> Roots(string input, double left = double.NegativeInfinity, double right = double.PositiveInfinity) =>
        ExprParser.Parse(input, "x").FindRealRoots(left, right);

    [Theory]
    [InlineData(double.NegativeInfinity, double.PositiveInfinity)]
    [InlineData(-1, 1)]
    [InlineData(-0.37, 5.1)]
    public void Finds_the_root_of_sqrt_at_the_edge_of_its_domain(double left, double right)
    {
        Assert.Equal([0.0], Roots("sqrt(x)", left, right));
    }

    [Theory]
    [InlineData(double.NegativeInfinity, double.PositiveInfinity)]
    [InlineData(-2.3, 1.7)]
    public void Finds_both_edges_of_a_semicircle(double left, double right)
    {
        Assert.Equal([-1.0, 1.0], Roots("sqrt(1 - x^2)", left, right));
    }

    [Fact]
    public void Finds_edges_that_are_not_exact_doubles()
    {
        var roots = Roots("sqrt(2 - x^2)");

        Assert.Equal(2, roots.Count);
        Assert.Equal(-Math.Sqrt(2), roots[0], precision: 12);
        Assert.Equal(Math.Sqrt(2), roots[1], precision: 12);
    }

    [Fact]
    public void Finds_an_edge_root_of_acos()
    {
        Assert.Equal([1.0], Roots("acos(x)"));
    }

    [Theory]
    [InlineData("x * ln(x)", 1.0)]      // tends to 0 at x = 0, but is undefined there
    [InlineData("sqrt(x) * ln(x)", 1.0)]
    [InlineData("ln(x)", 1.0)]
    public void Does_not_report_a_limit_at_an_undefined_edge_as_a_root(string input, double onlyRoot)
    {
        Assert.Equal([onlyRoot], Roots(input));
    }

    [Theory]
    [InlineData(-1, 1)]     // the grid hits x = 0 exactly
    [InlineData(-0.37, 5.1)]
    public void Still_reports_no_root_where_the_expression_is_undefined(double left, double right)
    {
        Assert.Empty(ExprParser.Parse("x^2 / x", "x").FindRealRoots(left, right));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("x^3")]      // x^3 underflows to exactly 0 on a plateau of tiny x
    [InlineData("tan(x)")]
    public void Finds_a_root_at_zero_exactly_when_the_grid_misses_it(string input)
    {
        Assert.Equal(0.0, Roots(input, -0.37, 3).First());
    }

    [Fact]
    public void Does_not_report_a_hole_away_from_zero_as_a_root()
    {
        Assert.Empty(Roots("(x - 0.5)^2 / (x - 0.5)", -0.37, 5.1));
    }

    [Fact]
    public void Ignores_poles_next_to_a_domain_edge()
    {
        Assert.Empty(Roots("1 / sqrt(x)"));
    }
}

public class DifferentiateSimplifiesTests
{
    // One case per kind of node, so every DifferentiateCore goes through the same path.
    [Theory]
    [InlineData("x^3 - 2x^2 + x")]
    [InlineData("-(x^2 + 0)")]
    [InlineData("x * sin(x)")]
    [InlineData("x / (x + 1)")]
    [InlineData("x^x")]
    [InlineData("sin(x^2 + 1)")]
    [InlineData("cos(2x) + tan(x) + cot(x) + sec(x) + csc(x)")]
    [InlineData("asin(x) + acos(x) + atan(x)")]
    [InlineData("sinh(x) + cosh(x) + tanh(x) + coth(x) + sech(x) + csch(x)")]
    [InlineData("asinh(x) + acosh(x) + atanh(x)")]
    [InlineData("exp(x^2) + ln(x^2 + 1)")]
    [InlineData("sqrt(x^2 + 1) + nthroot(x, 3) + abs(x)")]
    public void Result_is_already_simplified(string input)
    {
        Expr derivative = ExprParser.Parse(input, "x").Differentiate("x");

        Assert.Equal(derivative, derivative.Simplify());
    }

    [Fact]
    public void Chain_rule_leaves_no_zero_terms()
    {
        // sin's derivative used to be built without simplifying: cos(x^2 + 1) * (2x + 0).
        Assert.Equal("2cos(x^2 + 1) * x", ExprParser.Parse("sin(x^2 + 1)", "x").Differentiate("x").Print());
    }

    [Fact]
    public void Uses_generic_mode_like_Simplify()
    {
        // Strict would keep x/x here: d/dx x^x = (x/x + ln(x)) * x^x.
        Assert.Equal("(ln(x) + 1) * x^x", ExprParser.Parse("x^x", "x").Differentiate("x").Print());
    }
}

public class UnsupportedOperationTests
{
    [Theory]
    [InlineData("floor(x)")]
    [InlineData("ceiling(x)")]
    [InlineData("round(x)")]
    [InlineData("sign(x)")]
    [InlineData("min(x, 1)")]
    [InlineData("max(x, 1)")]
    [InlineData("nthroot(2, x)")]
    public void Unsupported_derivatives_throw_NotSupportedException_saying_it_is_planned(string input)
    {
        var ex = Assert.Throws<NotSupportedException>(() => ExprParser.Parse(input, "x").Differentiate("x"));
        Assert.Contains("planned for a future version", ex.Message);
    }

    [Theory]
    [InlineData("floor(x)")]
    [InlineData("ceiling(x)")]
    [InlineData("round(x)")]
    [InlineData("sign(x)")]
    public void Complex_evaluation_of_rounding_functions_is_planned(string input)
    {
        var ex = Assert.Throws<NotSupportedException>(() => ExprParser.Parse(input, "x").EvaluateComplex(new ComplexNumber(1.5)));
        Assert.Contains("planned for a future version", ex.Message);
    }

    [Theory]
    [InlineData("min(x, 1)")]
    [InlineData("max(x, 1)")]
    public void Complex_min_and_max_are_not_supported_because_complex_numbers_are_unordered(string input)
    {
        var ex = Assert.Throws<NotSupportedException>(() => ExprParser.Parse(input, "x").EvaluateComplex(new ComplexNumber(1.5)));
        Assert.Contains("not ordered", ex.Message);
    }

    [Fact]
    public void Root_finding_still_works_for_functions_without_a_derivative()
    {
        // round(x) - 2 is 0 on [1.5, 2.5): the scan reports grid points there without needing f'.
        var roots = ExprParser.Parse("round(x) - 2", "x").FindRealRoots(-5, 5);

        Assert.NotEmpty(roots);
        Assert.All(roots, r => Assert.Equal(2, Math.Round(r, MidpointRounding.AwayFromZero)));
    }
}

public class SimplifyFixpointTests
{
    private static readonly Expr X = new Variable("x");

    // x + 0 <-> 0 + x: two "rules" that undo each other.
    private static Expr Swap(Expr e) => e is Add(var l, var r) ? new Add(r, l) : e;

    [Fact]
    public void Returns_the_fixpoint_when_the_steps_converge()
    {
        Expr result = Simplifier.IterateToFixpoint(X + 0 + 0, e => e is Add(var l, Constant) ? l : e);

        Assert.Equal(X, result);
    }

    [Fact]
    public void Does_not_throw_on_a_rule_cycle()
    {
        Expr result = Simplifier.IterateToFixpoint(X + 0, Swap);

        Assert.True(result.Equals(X + 0) || result.Equals(0 + X));
    }

    [Fact]
    public void Breaks_ties_in_a_cycle_the_same_way_wherever_it_was_entered()
    {
        Assert.Equal(Simplifier.IterateToFixpoint(X + 0, Swap), Simplifier.IterateToFixpoint(0 + X, Swap));
    }

    [Fact]
    public void Picks_the_smallest_expression_of_a_cycle_wherever_it_was_entered()
    {
        // x*1 -> x*1*1 -> x -> x*1 -> ...: a cycle through three sizes.
        static Expr Step(Expr e) => e switch
        {
            Multiply(Multiply(var a, Constant), Constant) => a,
            Multiply(_, Constant) => new Multiply(e, new Constant(1)),
            _ => new Multiply(e, new Constant(1)),
        };

        Assert.Equal(X, Simplifier.IterateToFixpoint(X, Step));
        Assert.Equal(X, Simplifier.IterateToFixpoint(X * 1, Step));
        Assert.Equal(X, Simplifier.IterateToFixpoint(X * 1 * 1, Step));
    }

    [Fact]
    public void Stops_after_the_iteration_limit_when_steps_keep_growing()
    {
        Expr result = Simplifier.IterateToFixpoint(X, e => e * 1, maxIterations: 10);

        Assert.Equal(X, result); // every step is equivalent, and the start is the smallest
    }
}

public class SimplifyCacheTests
{
    private static string Sum(int terms, Func<int, string> term) =>
        string.Join(" + ", Enumerable.Range(1, terms).Select(term));

    // The cache answers a simplified expression with itself, which is only right if
    // simplifying it again really changes nothing.
    [Theory]
    [InlineData("sin(x)^2 + cos(x)^2 + 2x * 3x - x/x + (x^2 * y) / (x * y) + 2^3 * x", false)]
    [InlineData("x^3 * sin(x) / (1 + x^2) + x^3 * sin(x) / (1 + x^2)", false)]
    [InlineData("(x^2 * y) / (x * y) * (x^2 * y) / (x * y)", true)]
    [InlineData("sqrt(x^2) + x^(1/2) * x^(1/2) + (x^3)^(1/3)", true)]
    [InlineData("exp(ln(x)) - x * x^-1 + tan(x) * cot(x)", false)]
    public void Simplified_result_simplifies_to_itself(string input, bool strict)
    {
        SimplifyMode mode = strict ? SimplifyMode.Strict : SimplifyMode.Generic;
        Expr expr = ExprParser.Parse(input, "x", "y");

        foreach (Assumptions assumptions in new[] { Assumptions.None, Assumptions.None.AssumePositive("x") })
        {
            Expr once = expr.Simplify(assumptions, mode);
            Assert.Equal(once, once.Simplify(assumptions, mode));
        }
    }

    [Fact]
    public void Large_sum_of_distinct_terms_is_kept_in_full()
    {
        Expr sum = ExprParser.Parse(Sum(300, k => $"{k}x^{k}"));

        string expected = string.Join(" + ",
            Enumerable.Range(1, 300).Reverse().Select(k => k == 1 ? "x" : $"{k}x^{k}"));
        Assert.Equal(expected, sum.Simplify().Print());
    }

    [Fact]
    public void Large_sum_of_like_terms_combines()
    {
        Expr sum = ExprParser.Parse(Sum(300, k => $"{k}x"));

        Assert.Equal("45150x", sum.Simplify().Print());
    }

    // Allocations instead of time, so the check doesn't depend on the machine. Without the
    // cache this sum allocated about 4.8 GB, re-simplifying every shorter sum inside it.
    [Fact]
    public void Large_sum_does_not_resimplify_its_parts()
    {
        Expr sum = ExprParser.Parse(Sum(300, k => $"{k}x^{k}"));

        long before = GC.GetAllocatedBytesForCurrentThread();
        sum.Simplify();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 500_000_000, $"Simplify allocated {allocated / 1_000_000} MB");
    }

    private static long AllocatedBySimplify(Expr expr)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        expr.Simplify();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    // 1x + 2x^2 + ... - built in code, as Parse limits how deep a tree may be.
    private static Expr DistinctSum(int terms)
    {
        Expr x = new Variable("x");
        Expr sum = x;
        for (int k = 2; k <= terms; k++)
            sum = new Add(sum, new Multiply(new Constant(k), new Power(x, new Constant(k))));
        return sum;
    }

    // A sum is simplified term by term and combined once, so the work grows with the number of
    // terms. Simplifying every shorter sum inside the chain as well, as Simplify used to, made it
    // grow with the square: 10 times the terms took about 100 times the allocations.
    [Fact]
    public void Simplify_work_grows_linearly_with_the_size_of_a_sum()
    {
        AllocatedBySimplify(DistinctSum(10)); // warm-up

        long small = AllocatedBySimplify(DistinctSum(100));
        long large = AllocatedBySimplify(DistinctSum(1000));

        Assert.True(large < 20 * small, $"100 terms: {small / 1000} KB, 1000 terms: {large / 1000} KB");
    }
}

public class CachedHashTests
{
    // A leaf that counts how often its hash is actually computed.
    private sealed class CountingLeaf : Expr
    {
        public int HashComputations { get; private set; }

        public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => 0;
        protected override Expr DifferentiateCore(string variable) => new Constant(0);
        public override System.Collections.Immutable.ImmutableArray<Expr> Children => NoChildren;
        public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);

        protected override int PayloadHashCode()
        {
            HashComputations++;
            return 42;
        }
    }

    [Fact]
    public void Hash_of_a_subtree_is_computed_only_once()
    {
        var leaf = new CountingLeaf();
        Expr sum = new Add(new Sin(leaf), new Variable("x"));

        sum.GetHashCode();
        sum.GetHashCode();
        new Multiply(sum, new Constant(2)).GetHashCode(); // reuses the hash kept in `sum`

        Assert.Equal(1, leaf.HashComputations);
    }

    [Fact]
    public void Equal_trees_have_equal_hashes_whichever_was_hashed_first()
    {
        Expr a = ExprParser.Parse("x^2 + sin(2x) - 3/4");
        Expr b = ExprParser.Parse("x^2 + sin(2x) - 3/4");

        // Hash a subtree of one tree first, so the two are hashed in a different order.
        ((Subtract)a).Left.GetHashCode();

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(a, b);
    }
}

public class ExpandTests
{
    [Theory]
    [InlineData("(x + 1)^2 - x^2", "2x + 1")]
    [InlineData("(x + 1)*(x - 1)", "x^2 - 1")]
    [InlineData("(x - 2)^4", "x^4 - 8x^3 + 24x^2 - 32x + 16")]
    [InlineData("(x + 1)^2 (x - 1)", "x^3 + x^2 - x - 1")]
    [InlineData("(1 + x)^3 (1 - x)^3", "-x^6 + 3x^4 - 3x^2 + 1")]
    [InlineData("-(x + 1)*(x - 2)", "-x^2 + x + 2")]
    [InlineData("6(x - 3) + 9", "6x - 9")]
    [InlineData("x*(x + 1) - x^2", "x")]
    [InlineData("(x + y)^2", "2x * y + x^2 + y^2")]
    [InlineData("(2x)^3", "8x^3")]
    [InlineData("(x*y)^2", "x^2 * y^2")]
    public void Multiplies_out_products_and_powers_of_sums(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).Expand().Print());
    }

    [Theory]
    [InlineData("(x + 1)^2/2", "(1/2) * x^2 + x + 1/2")]   // dividing by a number divides every term
    [InlineData("(x + 1)^2/(x - 1)^2", "(x^2 + 2x + 1) / (x^2 - 2x + 1)")]
    [InlineData("((x + 1)/y)^2", "(x^2 + 2x + 1) / y^2")]
    [InlineData("x*(y + (x + 1)/y)", "x * y + (x^2 + x) / y")]
    [InlineData("x/(x - 1) + 2x/(x - 1)", "3x / (x - 1)")]
    [InlineData("(x + 1)^-2", "(x + 1)^-2")]               // negative powers are kept
    public void Keeps_a_quotient_as_one_fraction(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).Expand().Print());
    }

    [Fact]
    public void Expands_function_arguments_and_simplifies_the_result()
    {
        Assert.Equal("sin(x^2 + 2x + 1)", ExprParser.Parse("sin((x + 1)^2)").Expand().Print());
        Assert.Equal("2cos(x) * sin(x) + 1", ExprParser.Parse("(sin(x) + cos(x))^2").Expand().Print());
    }

    [Theory]
    [InlineData("(sqrt(x) + 1)^2", "x + 2sqrt(x) + 1", "2sqrt(x) + sqrt(x)^2 + 1")]
    [InlineData("(x + 1/x)^2", "x^2 + 1 / x^2 + 2", "x^2 + 1 / x^2 + 2x / x")]
    // Merging the powers gives (x + 1)^2, which is multiplied out in turn.
    [InlineData("(x + 1)^(1/2) * (x + 1)^(3/2)", "x^2 + 2x + 1", "(x + 1)^(1/2) * (x + 1)^(3/2)")]
    public void Simplifies_in_the_given_mode(string input, string generic, string strict)
    {
        Expr expr = ExprParser.Parse(input);

        Assert.Equal(generic, expr.Expand().Print());
        Assert.Equal(strict, expr.Expand(SimplifyMode.Strict).Print());
    }

    [Theory]
    [InlineData("(x + y)^5 - (x - y)^3 * y")]
    [InlineData("(x^2 + x*y + 1)^3 / (y + 2)")]
    [InlineData("(sin(x) + y)^2 * (x - 1)")]
    [InlineData("((x + 1)/(y + 3))^3 - x")]
    [InlineData("(x - y/2)^4 * (x + 1/3)")]
    public void Keeps_the_value(string input)
    {
        Expr expr = ExprParser.Parse(input);
        Expr expanded = expr.Expand();

        foreach (var (x, y) in new[] { (0.3, -1.7), (1.9, 0.6), (-2.4, 2.2) })
        {
            double expected = expr.Evaluate(("x", x), ("y", y));
            Assert.Equal(expected, expanded.Evaluate(("x", x), ("y", y)), 1e-9 * Math.Max(1, Math.Abs(expected)));
        }
    }

    [Fact]
    public void Combines_like_terms_of_a_high_power()
    {
        Expr expanded = ExprParser.Parse("(x + 1)^20").Expand();

        Assert.StartsWith("x^20 + 20x^19 + 190x^18 + ", expanded.Print());
        Assert.EndsWith(" + 190x^2 + 20x + 1", expanded.Print());
        Assert.Equal(1048576, expanded.Evaluate(1));
    }
}

public class PrintStyleTests
{
    [Theory]
    [InlineData("1 + 2x + x^2", "x^2 + 2x + 1")]
    [InlineData("x + x^3 - 2x^2", "x^3 - 2x^2 + x")]
    [InlineData("sin(x) + 1 + x", "x + sin(x) + 1")]
    [InlineData("5 - x^2", "-x^2 + 5")]
    [InlineData("x*y + x^2*y^2 + 1", "x^2 * y^2 + x * y + 1")]
    [InlineData("sqrt(2) + x", "x + sqrt(2)")]
    public void Prints_terms_by_descending_degree_with_constants_last(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).Simplify().Print());
    }

    [Theory]
    [InlineData("3x^2", "3x^2")]
    [InlineData("-3x^2", "-3x^2")]
    [InlineData("2 * cos(x) * y", "2cos(x) * y")]
    [InlineData("3 * (x + 1)^2", "3(x + 1)^2")]
    [InlineData("3 * 2^x", "3 * 2^x")]       // "32^x" would be a different number
    [InlineData("x * y", "x * y")]           // never juxtapose two letters: c*o*s is not cos
    [InlineData("3/4 * x", "(3/4) * x")]     // "3/4x" would read as 3/(4x)
    public void Writes_an_integer_coefficient_next_to_the_first_factor(string input, string expected)
    {
        Assert.Equal(expected, ExprParser.Parse(input).Simplify().Print());
    }

    [Theory]
    [InlineData("x^3 - 2x^2 + x")]
    [InlineData("3 * 2^x + 3e^x + 3pi^x")]
    [InlineData("-3x^2 * y - (x - 1)^2 / 2")]
    [InlineData("x - (x - 1) - (2 - x^2)")]
    [InlineData("2^-x^2 + x^-1")]
    [InlineData("(-2)^x - 3(1/2)^x")]
    [InlineData("-(x + y) * 3 + 2sin(x)^2 * cos(x)")]
    public void Printed_form_parses_back_to_the_same_value(string input)
    {
        Expr expr = ExprParser.Parse(input).Simplify();
        string printed = expr.Print();
        Expr reparsed = ExprParser.Parse(printed);

        foreach (double x in new[] { 0.7, 1.3, 2.9 })
            Assert.Equal(expr.Evaluate(("x", x), ("y", 0.4)), reparsed.Evaluate(("x", x), ("y", 0.4)), precision: 10);
    }

    [Fact]
    public void Latex_uses_the_same_term_order_and_implicit_powers()
    {
        Assert.Equal(@"3x^{2} - 4x + 1", ExprParser.Parse("1 - 4x + 3x^2").Simplify().ToLatex());
    }
}

// Every example in README.md, with the result its comment shows. If one of these fails,
// update the README together with the code.
public class ReadmeExamplesTests
{
    private static readonly Expr F = ExprParser.Parse("x^3 - 2x^2 + x", "x");

    [Fact]
    public void Quick_start()
    {
        Assert.Equal(12, F.Evaluate(3));
        Assert.Equal("3x^2 - 4x + 1", F.Differentiate("x").Print());
        Assert.Equal("3x^{2} - 4x + 1", F.Differentiate("x").ToLatex());
        Assert.Equal([0.0, 1.0], F.FindRealRoots(-10, 10));
        Assert.Equal("(x - 1)^2 * x", F.TryFactorReal("x").Factored.Print());
    }

    [Fact]
    public void Operators()
    {
        var x = new Variable("x");

        Assert.Equal("x^2 + 2x", (x * x + 3 * x - x).Simplify().Print());
        Assert.Equal("x^2 + 2x + 1", (x.Pow(2) + 2 * x + 1).Print());
    }

    [Fact]
    public void Exact_arithmetic()
    {
        Assert.Equal("3/10", ExprParser.Parse("0.1 + 0.2").Simplify().Print());
        Assert.Equal("(x + sqrt(2)) * (x - sqrt(2))", ExprParser.Parse("x^2 - 2").TryFactorReal("x").Factored.Print());
    }

    [Fact]
    public void Simplify_modes()
    {
        Expr e = ExprParser.Parse("x/x");

        Assert.Equal("1", e.Simplify().Print());
        Assert.Equal("x / x", e.Simplify(SimplifyMode.Strict).Print());
        Assert.Equal("2x + 1", ExprParser.Parse("(x + 1)^2 - x^2").Expand().Print());
    }

    [Fact]
    public void Assumptions_example()
    {
        Expr s = ExprParser.Parse("sqrt(x^2)");

        Assert.Equal("abs(x)", s.Simplify().Print());
        Assert.Equal("x", s.Simplify(Assumptions.None.AssumePositive("x")).Print());
    }

    [Fact]
    public void Differentiation()
    {
        Assert.Equal("3y^2 + x^2", ExprParser.Parse("x^2 * y + y^3").Differentiate("y").Print());
    }

    [Fact]
    public void Root_finding()
    {
        Assert.Equal([-1.0, 1.0], ExprParser.Parse("sqrt(1 - x^2)").FindRealRoots());

        var sinRoots = ExprParser.Parse("sin(x)").SolveNumerically(0.5, -4, 4);
        Assert.Equal(3, sinRoots.Count);
        Assert.Equal(-3.665, sinRoots[0], precision: 3);
        Assert.Equal(0.524, sinRoots[1], precision: 3);
        Assert.Equal(2.618, sinRoots[2], precision: 3);

        var complexRoots = ExprParser.Parse("x^2 + 1").FindComplexRoots(-2, 2, -2, 2)
            .OrderBy(z => z.Imaginary).ToList();
        Assert.Equal(2, complexRoots.Count);
        Assert.Equal(-1, complexRoots[0].Imaginary, precision: 9);
        Assert.Equal(1, complexRoots[1].Imaginary, precision: 9);
    }

    [Fact]
    public void Evaluation()
    {
        Assert.Equal(7, ExprParser.Parse("x*y + 1").Evaluate(("x", 2), ("y", 3)));

        ComplexNumber z = ExprParser.Parse("sqrt(x)").EvaluateComplex(-4);
        Assert.Equal(0, z.Real, precision: 12);
        Assert.Equal(2, z.Imaginary, precision: 12);
    }

    [Fact]
    public void Parser_variables()
    {
        Assert.Equal("theta^2 + 2t", ExprParser.Parse("theta^2 + 2t", "theta", "t").Print());

        var ex = Assert.Throws<FormatException>(() => ExprParser.Parse("sen(x)", "x"));
        Assert.Equal("Unknown identifier 'sen' at position 0. Declared variables: x.", ex.Message);
    }
}
