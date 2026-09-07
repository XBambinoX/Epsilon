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
        // x - (x - 1) is NOT the same as x - x - 1, so parens must be preserved
        Expr expr = ExprParser.Parse("x - (x - 1)");
        string result = expr.Print();
        Assert.Contains("(", result);
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
        Expr eExpr = new E();
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
    public void Throws_when_calling_single_variable_evaluate_on_zero_variable_expr()
    {
        Expr expr = new Constant(42);
        Assert.Throws<InvalidOperationException>(() => expr.Evaluate(5.0));
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
            expr.EvaluateComplex(new Dictionary<string, Complex> { ["x"] = new Complex(1, 0) }));
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