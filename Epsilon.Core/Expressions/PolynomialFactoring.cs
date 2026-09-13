using System.Numerics;

namespace Epsilon.Core;

public static class PolynomialFactoring
{
    /// <summary>
    /// Attempts to factor a univariate polynomial into real linear factors
    /// by finding real roots and performing synthetic division to deflate the degree.
    /// Any remaining quadratic factor with no real roots is left as-is (irreducible over the reals).
    /// Returns (original expression, false) if the input isn't recognized as a polynomial
    /// in `variable`, or if no real roots are found at all.
    /// </summary>
    public static (Expr Factored, bool Success) TryFactorReal(this Expr expr, string variable)
    {
        Rational[]? exactCoefficients = TryGetPolynomialCoefficients(expr, variable);
        if (exactCoefficients is null)
            return (expr, false);

        int degree = exactCoefficients.Length - 1;
        if (degree < 1)
            return (expr, false); // constant - nothing to factor

        // Root-finding and synthetic division are inherently numeric (Newton's method
        // works with irrational roots in general), so we convert to double here -
        // but the extraction step above stayed exact, avoiding any precision loss
        // while reading the polynomial's coefficients out of the expression tree.
        double[] coefficients = ToDoubleArray(exactCoefficients);

        double bound = CauchyRootBound(coefficients);
        var roots = expr.FindRealRoots(variable, null, -bound, bound, scanSteps: Math.Max(200, degree * 50));

        if (roots.Count == 0)
            return (expr, false); // no real roots found - cannot factor over the reals

        // No need to clone: TrySyntheticDivide always allocates a new array for its
        // quotient rather than mutating its input, so 'coefficients' itself is never
        // touched after this point - 'remaining' can just start out pointing at it.
        double[] remaining = coefficients;
        var linearFactors = new List<double>(); // each entry r contributes a factor (x - r)

        foreach (double root in roots)
        {
            double scale = MaxAbsCoefficient(remaining);

            // Deflate repeatedly while the root still divides evenly (handles multiplicity).
            while (remaining.Length > 1 && IsNegligible(EvaluatePolynomial(remaining, root), scale))
            {
                double[]? deflated = TrySyntheticDivide(remaining, root, scale);
                if (deflated is null)
                    break;

                remaining = deflated;
                linearFactors.Add(root);
                scale = MaxAbsCoefficient(remaining);
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
        Rational[]? exactCoefficients = TryGetPolynomialCoefficients(expr, variable);
        if (exactCoefficients is null)
            return (expr, false);

        int degree = exactCoefficients.Length - 1;
        if (degree < 1)
            return (expr, false);

        double[] coefficients = ToDoubleArray(exactCoefficients);

        double bound = CauchyRootBound(coefficients) + 1;
        var complexRoots = expr.FindComplexRoots(variable, null, -bound, bound, -bound, bound, gridSteps: Math.Max(12, degree * 4));

        // A degree-n polynomial has exactly n roots counted with multiplicity;
        // if the grid search didn't find that many, we can't guarantee a full split.
        if (complexRoots.Count < degree)
            return (expr, false);

        double leadingCoefficient = coefficients[degree];
        Expr factored = BuildComplexFactoredExpression(leadingCoefficient, complexRoots, variable);
        return (factored, true);
    }

    // Polynomial extraction (exact, Rational-based)

    private static Rational[]? TryGetPolynomialCoefficients(Expr expr, string variable)
    {
        try
        {
            Expr simplified = expr.Simplify();
            var coeffs = new Dictionary<int, Rational>();
            CollectPolynomialTerms(simplified, variable, Rational.One, coeffs);

            if (coeffs.Count == 0)
                return new[] { Rational.Zero };

            int maxDegree = coeffs.Keys.Max();

            var result = new Rational[maxDegree + 1];
            for (int i = 0; i < result.Length; i++)
                result[i] = Rational.Zero;

            foreach (var (degree, coef) in coeffs)
                result[degree] += coef;

            return result;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static void CollectPolynomialTerms(Expr expr, string variable, Rational sign, Dictionary<int, Rational> coeffs)
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
                coeffs[degree] = coeffs.GetValueOrDefault(degree, Rational.Zero) + sign * coef;
                break;
        }
    }

    private static (int Degree, Rational Coefficient) ExtractTerm(Expr expr, string variable)
    {
        // term / c  ->  same degree, coefficient divided by c. This is common after
        // Simplify() now that the engine keeps exact fractions (e.g. "x/2", "x^2/3")
        // instead of folding them into decimal constants - the old two-level
        // pattern-matching approach didn't handle this shape at all.
        if (expr is Divide(var numerator, Constant divisor))
        {
            if (divisor.Value.IsZero)
                throw new NotSupportedException($"'{expr.Print()}' divides by zero and is not a valid polynomial term.");

            var (deg, coef) = ExtractTerm(numerator, variable);
            return (deg, coef / divisor.Value);
        }

        if (expr is Negate(var inner))
        {
            var (deg, coef) = ExtractTerm(inner, variable);
            return (deg, -coef);
        }

        if (expr is Multiply)
        {
            // Flatten an arbitrarily nested/ordered Multiply chain into a flat list of
            // factors, then fold all constant factors together and identify the single
            // remaining variable-bearing factor. This replaces the old approach of
            // matching only two fixed shapes (Multiply(Constant, X) / Multiply(X, Constant)),
            // which silently failed on anything deeper - e.g. Multiply(Multiply(c1, c2), x).
            var factors = new List<Expr>();
            FlattenMultiply(expr, factors);

            Rational coefficient = Rational.One;
            int? degree = null;

            foreach (Expr factor in factors)
            {
                if (factor is Constant c)
                {
                    coefficient *= c.Value;
                    continue;
                }

                if (degree is not null)
                    throw new NotSupportedException(
                        $"'{expr.Print()}' has more than one variable-bearing factor and is not a polynomial term.");

                var (factorDegree, factorCoefficient) = ExtractTerm(factor, variable);
                degree = factorDegree;
                coefficient *= factorCoefficient;
            }

            return (degree ?? 0, coefficient);
        }

        return expr switch
        {
            Constant c => (0, c.Value),

            Variable v when v.Name == variable => (1, Rational.One),

            Power(Variable v, Constant n) when v.Name == variable && IsNonNegativeInteger(n.Value) =>
                (ToSafeInt32(n.Value.Numerator, expr), Rational.One),

            _ => throw new NotSupportedException($"'{expr.Print()}' is not a recognized polynomial term.")
        };
    }

    private static void FlattenMultiply(Expr expr, List<Expr> factors)
    {
        if (expr is Multiply(var l, var r))
        {
            FlattenMultiply(l, factors);
            FlattenMultiply(r, factors);
        }
        else
        {
            factors.Add(expr);
        }
    }

    private static bool IsNonNegativeInteger(Rational value) =>
        value.Sign >= 0 && value.IsInteger;

    // Guards against a degree exponent too large to fit in an int (which would
    // otherwise silently overflow/wrap on a raw cast and corrupt the coefficient array).
    private static int ToSafeInt32(BigInteger value, Expr context)
    {
        if (value > int.MaxValue)
            throw new NotSupportedException($"'{context.Print()}' has a degree too large to represent.");

        return (int)value;
    }

    //Numeric helpers (double-based - root-finding is inherently approximate)

    private static double[] ToDoubleArray(Rational[] coefficients)
    {
        var result = new double[coefficients.Length];
        for (int i = 0; i < coefficients.Length; i++)
            result[i] = coefficients[i].ToDouble();
        return result;
    }

    private static double MaxAbsCoefficient(double[] coefficients)
    {
        double max = 0;
        foreach (double c in coefficients)
            max = Math.Max(max, Math.Abs(c));
        return max;
    }

    // Relative-to-scale tolerance check, replacing a fixed absolute threshold that
    // was either too loose (large-coefficient polynomials) or too tight (small ones).
    private static bool IsNegligible(double value, double scale) =>
        Math.Abs(value) < 1e-6 * Math.Max(1.0, scale);

    private static double EvaluatePolynomial(double[] coefficients, double x)
    {
        double result = 0;
        for (int i = coefficients.Length - 1; i >= 0; i--)
            result = result * x + coefficients[i];
        return result;
    }

    private static double[]? TrySyntheticDivide(double[] coefficients, double root, double scale)
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
        return IsNegligible(remainder, scale) ? quotient : null;
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
            Rational rationalizedRoot = RationalizeRoot(root);

            Expr factor = rationalizedRoot.IsZero
                ? new Variable(variable)
                : new Subtract(new Variable(variable), new Constant(rationalizedRoot));

            result = new Multiply(factor, result);
        }

        return result.Simplify();
    }

    private static Expr BuildComplexFactoredExpression(double leadingCoefficient, IReadOnlyList<Complex> roots, string variable)
    {
        Expr result = new Constant(RationalizeRoot(leadingCoefficient));

        foreach (Complex root in roots)
        {
            Rational realPart = RationalizeRoot(root.Real);
            Rational imagPart = RationalizeRoot(root.Imaginary);

            Expr realExpr = realPart.IsZero
                ? new Variable(variable)
                : new Subtract(new Variable(variable), new Constant(realPart));

            Expr factor = imagPart.IsZero
                ? realExpr
                : new Subtract(realExpr, new Multiply(new Constant(imagPart), new ImaginaryUnit()));

            result = new Multiply(result, factor);
        }

        return result.Simplify();
    }

    private static Expr BuildPolynomialFromCoefficients(double[] coefficients, string variable)
    {
        Expr result = new Constant(RationalizeRoot(coefficients[0]));

        for (int degree = 1; degree < coefficients.Length; degree++)
        {
            Rational coefficient = RationalizeRoot(coefficients[degree]);
            if (coefficient.IsZero)
                continue;

            Expr term = degree == 1
                ? new Variable(variable)
                : new Power(new Variable(variable), new Constant(degree));

            term = new Multiply(new Constant(coefficient), term);
            result = new Add(result, term);
        }

        return result;
    }

    private static Rational RationalizeRoot(double value, int maxDenominator = 1000)
    {
        double rounded = Math.Round(value);
        if (Math.Abs(value - rounded) < 1e-8)
            return new Rational((BigInteger)rounded);

        for (int denominator = 2; denominator <= maxDenominator; denominator++)
        {
            double numerator = value * denominator;
            double roundedNumerator = Math.Round(numerator);

            if (Math.Abs(numerator - roundedNumerator) < 1e-5)
                return new Rational((BigInteger)roundedNumerator, denominator);
        }

        return Rational.FromDouble(value);
    }
}