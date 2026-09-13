namespace Epsilon.Core;

public static class PolynomialFactoring
{
    private const double CoefficientTolerance = 1e-9;

    /// <summary>
    /// Attempts to factor a univariate polynomial into real linear factors
    /// by finding real roots and performing synthetic division to deflate the degree.
    /// Any remaining quadratic factor with no real roots is left as-is (irreducible over the reals).
    /// Returns (original expression, false) if the input isn't recognized as a polynomial
    /// in `variable`, or if no real roots are found at all.
    /// </summary>
    public static (Expr Factored, bool Success) TryFactorReal(this Expr expr, string variable)
    {
        double[]? coefficients = TryGetPolynomialCoefficients(expr, variable);
        if (coefficients is null)
            return (expr, false);

        int degree = coefficients.Length - 1;
        if (degree < 1)
            return (expr, false);

        double bound = CauchyRootBound(coefficients);
        var roots = expr.FindRealRoots(variable, null, -bound, bound, scanSteps: Math.Max(200, degree * 50));

        if (roots.Count == 0)
            return (expr, false); // no real roots found — cannot factor over the reals

        double[] remaining = (double[])coefficients.Clone();
        var linearFactors = new List<double>(); // each entry r contributes a factor (x - r)

        foreach (double root in roots)
        {
            while (remaining.Length > 1 && Math.Abs(EvaluatePolynomial(remaining, root)) < 1e-4)
            {
                double[]? deflated = TrySyntheticDivide(remaining, root);
                if (deflated is null)
                    break;

                remaining = deflated;
                linearFactors.Add(root);
            }
        }

        if (linearFactors.Count == 0)
            return (expr, false);

        Expr factored = BuildFactoredExpression(linearFactors, remaining, variable);
        return (factored, true);
    }

    /// <summary>
    /// Attempts to factor a univariate polynomial fully into linear factors over the complex
    /// numbers, expressed using the ImaginaryUnit where roots are non-real. Returns
    /// (original expression, false) if the input isn't recognized as a polynomial, or if
    /// complex root finding fails to account for the full degree.
    /// </summary>
    public static (Expr Factored, bool Success) TryFactorComplex(this Expr expr, string variable)
    {
        double[]? coefficients = TryGetPolynomialCoefficients(expr, variable);
        if (coefficients is null)
            return (expr, false);

        int degree = coefficients.Length - 1;
        if (degree < 1)
            return (expr, false);

        double bound = CauchyRootBound(coefficients) + 1;
        var complexRoots = expr.FindComplexRoots(variable, null, -bound, bound, -bound, bound, gridSteps: Math.Max(12, degree * 4));

        // A degree-n polynomial has exactly n roots counted with multiplicity
        if (complexRoots.Count < degree)
            return (expr, false);

        double leadingCoefficient = coefficients[degree];
        Expr factored = BuildComplexFactoredExpression(leadingCoefficient, complexRoots, variable);
        return (factored, true);
    }

    //Polynomial extraction

    private static double[]? TryGetPolynomialCoefficients(Expr expr, string variable)
    {
        try
        {
            Expr simplified = expr.Simplify();
            var coeffs = new Dictionary<int, double>();
            CollectPolynomialTerms(simplified, variable, 1.0, coeffs);

            if (coeffs.Count == 0)
                return new double[] { 0 };

            int maxDegree = coeffs.Keys.Max();
            var result = new double[maxDegree + 1];
            foreach (var (degree, coef) in coeffs)
                result[degree] += coef;

            return result;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static void CollectPolynomialTerms(Expr expr, string variable, double sign, Dictionary<int, double> coeffs)
    {
        switch (expr)
        {
            case Add(var l, var r):
                CollectPolynomialTerms(l, variable, sign, coeffs);
                CollectPolynomialTerms(r, variable, sign, coeffs);
                break;
            case Subtract(var l, var r):
                CollectPolynomialTerms(l, variable, sign, coeffs);
                CollectPolynomialTerms(r, variable, -sign, coeffs);
                break;
            default:
                var (degree, coef) = ExtractTerm(expr, variable);
                coeffs[degree] = coeffs.GetValueOrDefault(degree) + sign * coef;
                break;
        }
    }

    private static (int Degree, double Coefficient) ExtractTerm(Expr expr, string variable)
    {
        switch (expr)
        {
            case Constant c:
                return (0, c.Value.ToDouble());

            case Variable v when v.Name == variable:
                return (1, 1);

            case Power(Variable v, Constant n) when v.Name == variable && IsNonNegativeInteger(n.Value):
                return ((int)n.Value.Numerator, 1);

            case Multiply(Constant c, var rest):
                {
                    var (d, co) = ExtractTerm(rest, variable);
                    return (d, c.Value.ToDouble() * co);
                }

            case Multiply(var rest, Constant c):
                {
                    var (d, co) = ExtractTerm(rest, variable);
                    return (d, c.Value.ToDouble() * co);
                }

            case Negate(var inner):
                {
                    var (d, co) = ExtractTerm(inner, variable);
                    return (d, -co);
                }

            default:
                throw new NotSupportedException($"'{expr.Print()}' is not a recognized polynomial term.");
        }
    }

    private static bool IsNonNegativeInteger(Rational value) =>
        value.Sign >= 0 && value.IsInteger;

    // Numeric helpers

    private static double EvaluatePolynomial(double[] coefficients, double x)
    {
        double result = 0;
        for (int i = coefficients.Length - 1; i >= 0; i--)
            result = result * x + coefficients[i];
        return result;
    }

    // Synthetic division: divides coefficients by (x - root), returns the quotient
    // coefficients if the remainder is negligible, or null otherwise.
    private static double[]? TrySyntheticDivide(double[] coefficients, double root)
    {
        int n = coefficients.Length;
        var quotient = new double[n - 1];

        double carry = coefficients[n - 1];
        quotient[n - 2] = carry;

        for (int i = n - 2; i >= 1; i--)
        {
            carry = coefficients[i] + carry * root;
            quotient[i - 1] = carry;
        }

        double remainder = coefficients[0] + carry * root;
        return Math.Abs(remainder) < 1e-4 ? quotient : null;
    }

    // Cauchy's bound: all real (and complex) roots of a polynomial lie within this radius of zero.
    private static double CauchyRootBound(double[] coefficients)
    {
        int n = coefficients.Length - 1;
        double leading = Math.Abs(coefficients[n]);
        double maxRatio = 0;

        for (int i = 0; i < n; i++)
            maxRatio = Math.Max(maxRatio, Math.Abs(coefficients[i]) / leading);

        return 1 + maxRatio;
    }

    private static Expr BuildFactoredExpression(List<double> linearRoots, double[] remainingCoefficients, string variable)
    {
        Expr result = BuildPolynomialFromCoefficients(remainingCoefficients, variable);

        foreach (double root in linearRoots)
        {
            double roundedRoot = RoundIfNearInteger(root);

            Expr factor = Math.Abs(roundedRoot) < CoefficientTolerance
                ? new Variable(variable)
                : new Subtract(new Variable(variable), new Constant(roundedRoot));

            result = new Multiply(factor, result);
        }

        return result.Simplify();
    }

    private static Expr BuildComplexFactoredExpression(double leadingCoefficient, IReadOnlyList<Complex> roots, string variable)
    {
        Expr result = new Constant(leadingCoefficient);

        foreach (Complex root in roots)
        {
            double realPart = RoundIfNearInteger(root.Real);
            double imagPart = RoundIfNearInteger(root.Imaginary);

            Expr realExpr = Math.Abs(realPart) < CoefficientTolerance
                ? new Variable(variable)
                : new Subtract(new Variable(variable), new Constant(realPart));

            Expr factor = Math.Abs(imagPart) < CoefficientTolerance
                ? realExpr
                : new Subtract(realExpr, new Multiply(new Constant(imagPart), new ImaginaryUnit()));

            result = new Multiply(result, factor);
        }

        return result.Simplify();
    }

    private static Expr BuildPolynomialFromCoefficients(double[] coefficients, string variable)
    {
        Expr result = new Constant(coefficients[0]);

        for (int degree = 1; degree < coefficients.Length; degree++)
        {
            if (Math.Abs(coefficients[degree]) < CoefficientTolerance)
                continue;

            Expr term = degree == 1
                ? new Variable(variable)
                : new Power(new Variable(variable), new Constant(degree));

            term = new Multiply(new Constant(coefficients[degree]), term);
            result = new Add(result, term);
        }

        return result;
    }

    private static double RoundIfNearInteger(double value, double tolerance = 1e-6)
    {
        double rounded = Math.Round(value);
        return Math.Abs(value - rounded) < tolerance ? rounded : value;
    }
}