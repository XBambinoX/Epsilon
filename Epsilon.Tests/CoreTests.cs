using Xunit;
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
        Assert.Equal("2 * x", expr.ToString().Replace("(", "").Replace(")", ""));
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
        var a = new Complex(1, 2);
        var b = new Complex(3, -1);
        var result = a + b;
        Assert.Equal(4, result.Real, precision: 10);
        Assert.Equal(1, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Multiplies_complex_numbers()
    {
        // (2 + 3i) * (1 - i) = 2 - 2i + 3i - 3i^2 = 2 + i + 3 = 5 + i
        var a = new Complex(2, 3);
        var b = new Complex(1, -1);
        var result = a * b;
        Assert.Equal(5, result.Real, precision: 10);
        Assert.Equal(1, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Divides_complex_numbers()
    {
        var a = new Complex(4, 2);
        var b = new Complex(2, 0);
        var result = a / b;
        Assert.Equal(2, result.Real, precision: 10);
        Assert.Equal(1, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Computes_magnitude_correctly()
    {
        var z = new Complex(3, 4);
        Assert.Equal(5, z.Magnitude, precision: 10);
    }

    [Fact]
    public void Imaginary_unit_squared_equals_negative_one()
    {
        var i = Complex.ImaginaryUnit;
        var result = i * i;
        Assert.Equal(-1, result.Real, precision: 10);
        Assert.Equal(0, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Sqrt_of_negative_one_equals_imaginary_unit()
    {
        var result = Complex.Sqrt(new Complex(-1, 0));
        Assert.Equal(0, result.Real, precision: 10);
        Assert.Equal(1, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Exp_of_i_pi_equals_negative_one()
    {
        // Euler's identity: e^(i*pi) = -1
        var z = new Complex(0, Math.PI);
        var result = Complex.Exp(z);
        Assert.Equal(-1, result.Real, precision: 10);
        Assert.Equal(0, result.Imaginary, precision: 10);
    }

    // Parsing 'i' from strings

    [Fact]
    public void Parses_imaginary_unit_from_string()
    {
        Expr expr = ExprParser.Parse("2 + 3i");
        Complex result = expr.EvaluateComplex(new Dictionary<string, Complex>());
        Assert.Equal(2, result.Real, precision: 10);
        Assert.Equal(3, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Parses_pure_imaginary_expression()
    {
        Expr expr = ExprParser.Parse("i * i");
        Complex result = expr.EvaluateComplex(new Dictionary<string, Complex>());
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
        Complex result = expr.EvaluateComplex(Complex.ImaginaryUnit);
        Assert.Equal(0, result.Real, precision: 10);
        Assert.Equal(0, result.Imaginary, precision: 10);
    }

    [Fact]
    public void Evaluates_sin_at_complex_point()
    {
        Expr expr = ExprParser.Parse("sin(x)");
        Complex result = expr.EvaluateComplex(new Complex(0, 1));
        // sin(i) = i * sinh(1)
        Assert.Equal(0, result.Real, precision: 10);
        Assert.Equal(Math.Sinh(1), result.Imaginary, precision: 10);
    }

    //Complex root finding

    [Fact]
    public void Finds_complex_root_of_x_squared_plus_one()
    {
        Expr expr = ExprParser.Parse("x^2 + 1");
        var (root, found) = expr.TryFindComplexRoot(new Complex(0, 1));
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
            "x", new Complex(0, 1),
            new Dictionary<string, Complex> { ["a"] = new Complex(1, 0) });

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
        var roots = left.FindRealRoots(right, -10, 10);
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
        var roots = left.FindComplexRoots(right, -5, 5, -5, 5, gridSteps: 8);

        Assert.Contains(roots, r => Math.Abs(r.Real) < 1e-3 && Math.Abs(r.Imaginary - 2.0) < 1e-3);
        Assert.Contains(roots, r => Math.Abs(r.Real) < 1e-3 && Math.Abs(r.Imaginary + 2.0) < 1e-3);
    }
}