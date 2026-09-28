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
    [InlineData("x * x", "x ^ 2")]
    [InlineData("x^2 * x^3", "x ^ 5")]
    [InlineData("x^-1 * x^-2", "x ^ -3")]
    [InlineData("x^2 * x^(1/2)", "x ^ (5/2)")]
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
    [InlineData("(x^2)^(1/2)", "(x ^ 2) ^ (1/2)")] // same reason, must not collapse to x
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
        Complex result = factored.EvaluateComplex(Complex.ImaginaryUnit);
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
        Complex atI = factored.EvaluateComplex(new Complex(0, 2));
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
        Complex result = ExprParser.Parse("nthroot(x, 3)", "x").EvaluateComplex(new Complex(-8));
        Assert.Equal(new Complex(-2), result);
    }

    [Fact]
    public void Power_with_fractional_exponent_is_the_principal_value()
    {
        // Unlike nthroot, x^(1/3) is the principal value: undefined over the reals for x < 0.
        Expr expr = new Power(new Constant(-8), new Constant(new Rational(1, 3)));

        Assert.IsType<Power>(expr.Simplify());
        Assert.True(double.IsNaN(expr.Evaluate(NoBindings)));

        Complex principal = expr.EvaluateComplex(new Dictionary<string, Complex>());
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
    [InlineData("(x^3)^(1/3)", SimplifyMode.Strict, "(x ^ 3) ^ (1/3)")] // undefined for x < 0, x is not
    [InlineData("(x^3)^(1/3)", SimplifyMode.Generic, "x")]
    [InlineData("(x^3)^2", SimplifyMode.Strict, "x ^ 6")]              // integer outer exponent: exact
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
        new Power(X, C(1, 2)),                        // was "x ^ 1/2" -> parsed as (x^1)/2
        new Power(C(-2), X),                          // was "-2 ^ x" -> parsed as -(2^x)
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
    [InlineData("x ^ (1/2)")]
    [InlineData("(-2) ^ x")]
    [InlineData("x / (1/2)")]
    [InlineData("ceiling(x)")]
    [InlineData("a * sinh(x)")]
    [InlineData("x ^ -1")]
    [InlineData("2x")]
    public void Prints_unambiguous_form(string expected)
    {
        Expr expr = expected switch
        {
            "x ^ (1/2)" => new Power(X, C(1, 2)),
            "(-2) ^ x" => new Power(C(-2), X),
            "x / (1/2)" => new Divide(X, C(1, 2)),
            "ceiling(x)" => new Ceiling(X),
            "a * sinh(x)" => new Multiply(new Variable("a"), new Sinh(X)),
            "x ^ -1" => new Power(X, C(-1)),
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
            .Assume("x", Signing: first)
            .Assume("x", Signing: second);

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
        Assumptions assumptions = Assumptions.None.Assume("x", Signing: first);

        var ex = Assert.Throws<ArgumentException>(() => assumptions.Assume("x", Signing: second));
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
    [InlineData("1 - sin(x)^2", "cos(x) ^ 2")]      // was dead code: stayed "-sin(x) ^ 2 + 1"
    [InlineData("1 - cos(x)^2", "sin(x) ^ 2")]      // was dead code
    [InlineData("sin(x)^2 - 1", "-cos(x) ^ 2")]
    [InlineData("3 - 3cos(x)^2", "3 * sin(x) ^ 2")]
    [InlineData("sin(x)^2 + cos(x)^2", "1")]
    [InlineData("cos(x)^2 + sin(x)^2", "1")]
    [InlineData("sin(x)^2 + cos(x)^2 + 1", "2")]    // extra term used to block the old two-node rule
    [InlineData("2sin(x)^2 + 2cos(x)^2", "2")]      // coefficients used to block it too
    [InlineData("2sin(x)^2 + 3cos(x)^2", "cos(x) ^ 2 + 2")]
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
    [InlineData("sin(x)^2 + cos(y)^2", "cos(y) ^ 2 + sin(x) ^ 2")] // different arguments
    [InlineData("5 - sin(x)^2", "-sin(x) ^ 2 + 5")]                // constant doesn't match the coefficient
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
    [InlineData("x^2 - 2x + 1", "(x - 1) ^ 2")]                  // used to fail: phantom root broke deflation
    [InlineData("x^4 - 8x^3 + 24x^2 - 32x + 16", "(x - 2) ^ 4")] // (x - 2)^4 expanded
    public void Factors_polynomials_with_a_single_repeated_root(string input, string expected)
    {
        var (factored, success) = P(input).TryFactorReal("x");

        Assert.True(success);
        Assert.Equal(expected, factored.Print());
    }

    [Fact]
    public void Factors_polynomial_with_repeated_and_simple_roots()
    {
        // (x - 1)^2 (x + 2) expanded. Checked by value rather than by printed form: Simplify
        // currently leaves (x + 2) * (x - 1) * (x - 1) instead of merging the repeated factor.
        Expr original = P("x^3 - 3x + 2");
        var (factored, success) = original.TryFactorReal("x");

        Assert.True(success);
        Assert.IsType<Multiply>(factored);
        foreach (double x in new[] { -3.0, -0.5, 0.7, 2.5 })
            Assert.Equal(original.Evaluate(x), factored.Evaluate(x), precision: 9);
    }
}
