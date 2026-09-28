using System.Numerics;
using NumericComplex = System.Numerics.Complex;

namespace Epsilon.Core;

/// <summary>Exact factorization of univariate polynomials with rational coefficients.</summary>
public static class PolynomialFactoring
{
    /// <summary>
    /// Factors a univariate polynomial with rational coefficients exactly - no approximated
    /// roots ever appear in the result. Rational roots become linear factors, and a quadratic
    /// factor with a positive discriminant is split with square roots:
    /// x^2 - 2 -> (x - sqrt(2)) * (x + sqrt(2)). A factor whose roots can't be written that
    /// way (x^3 - 2, or x^2 + 1 over the reals) is kept as a polynomial - use FindRealRoots
    /// for numeric roots. Returns (original expression, false) if the input isn't a
    /// polynomial in `variable`, or if nothing could be factored.
    /// </summary>
    public static (Expr Factored, bool Success) TryFactorReal(this Expr expr, string variable) =>
        TryFactor(expr, variable, complex: false);

    /// <summary>
    /// Like <see cref="TryFactorReal"/>, but quadratic factors with a negative discriminant are
    /// split too, using the imaginary unit: x^2 + 1 -> (x - i) * (x + i). Succeeds only if the
    /// polynomial splits completely into exact linear factors.
    /// </summary>
    public static (Expr Factored, bool Success) TryFactorComplex(this Expr expr, string variable) =>
        TryFactor(expr, variable, complex: true);

    private static (Expr Factored, bool Success) TryFactor(Expr expr, string variable, bool complex)
    {
        Rational[]? coefficients = TryGetPolynomialCoefficients(expr, variable);
        if (coefficients is null)
            return (expr, false);

        coefficients = TrimLeadingZeros(coefficients);
        if (coefficients.Length < 2)
            return (expr, false); // constant - nothing to factor

        Rational leading = coefficients[^1];
        Rational[] monic = Scale(coefficients, Rational.One / leading);

        List<Rational[]> factors = ExtractExactFactors(monic);

        var x = new Variable(variable);
        var linear = new List<Expr>();
        var other = new List<Expr>();

        foreach (Rational[] factor in factors)
        {
            if (factor.Length == 2)
            {
                linear.Add(LinearFactor(x, -factor[0]));
            }
            else if (factor.Length == 3 && TrySplitQuadratic(x, factor, complex, out Expr first, out Expr second))
            {
                linear.Add(first);
                linear.Add(second);
            }
            else
            {
                other.Add(BuildPolynomial(x, factor));
            }
        }

        // Complex: must split completely. Real: any genuine factorization counts, including
        // one into irreducible quadratics only: x^4 + 5x^2 + 4 -> (x^2 + 1) * (x^2 + 4).
        bool success = complex
            ? other.Count == 0
            : linear.Count > 0 || other.Count >= 2;

        if (!success)
            return (expr, false);

        Expr result = BuildProduct(leading, linear.Concat(other));
        return (result.Simplify(), true);
    }

    // Polynomial extraction (exact, Rational-based)

    private static Rational[]? TryGetPolynomialCoefficients(Expr expr, string variable)
    {
        try
        {
            Expr simplified = expr.Simplify(SimplifyMode.Strict); // x^2/x is not the polynomial x
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

    // Exact factor extraction. Numeric roots are only used to generate candidates;
    // every factor is confirmed by exact Rational arithmetic before it is accepted.

    private const long MaxCandidateDenominator = 1_000_000_000_000;

    // Splits a monic polynomial into monic factors with rational coefficients: linear
    // factors (one per rational root, repeated for multiplicity), quadratic factors, and
    // possibly one remaining factor of degree >= 3 that couldn't be reduced further.
    private static List<Rational[]> ExtractExactFactors(Rational[] monic)
    {
        var factors = new List<Rational[]>();
        Rational[] rest = monic;

        while (Degree(rest) >= 1)
        {
            if (Degree(rest) == 1)
            {
                factors.Add(rest);
                return factors;
            }

            NumericComplex[] roots = FindAllRootsNumerically(rest);

            if (TryFindRationalRoot(rest, roots, out Rational root))
            {
                factors.Add([-root, Rational.One]);
                rest = DivideExactly(rest, [-root, Rational.One])!;
                continue;
            }

            if (Degree(rest) == 2)
            {
                factors.Add(rest);
                return factors;
            }

            if (TryFindRationalQuadratic(rest, roots, out Rational[] quadratic, out Rational[] quotient))
            {
                factors.Add(quadratic);
                rest = quotient;
                continue;
            }

            break; // e.g. x^3 - 2: irreducible over the rationals, no exact split possible
        }

        if (Degree(rest) >= 1)
            factors.Add(rest);

        return factors;
    }

    private static bool TryFindRationalRoot(Rational[] polynomial, NumericComplex[] roots, out Rational root)
    {
        // No "is this root real?" filter: a k-fold root comes back from the numeric solver as
        // a small ring of k points around it, most of them slightly complex. Every candidate
        // is verified exactly anyway, so trying the real part of each one is safe.
        foreach (NumericComplex z in roots)
        {
            foreach (Rational candidate in Convergents(z.Real, MaxCandidateDenominator))
            {
                if (Evaluate(polynomial, candidate).IsZero)
                {
                    root = candidate;
                    return true;
                }
            }
        }

        root = default;
        return false;
    }

    // Any two roots r1, r2 (a real pair or a complex-conjugate pair) with a rational sum s and
    // product p give the factor x^2 - s*x + p - e.g. sqrt(2), -sqrt(2) -> x^2 - 2.
    private static bool TryFindRationalQuadratic(
        Rational[] polynomial, NumericComplex[] roots, out Rational[] quadratic, out Rational[] quotient)
    {
        for (int i = 0; i < roots.Length; i++)
        {
            for (int j = i + 1; j < roots.Length; j++)
            {
                // Loose filter, only to skip pairs that clearly aren't conjugates (the exact
                // division below is the real check); loose so repeated roots still qualify.
                NumericComplex sum = roots[i] + roots[j];
                NumericComplex product = roots[i] * roots[j];
                if (Math.Abs(sum.Imaginary) > 1e-3 * (1 + sum.Magnitude) ||
                    Math.Abs(product.Imaginary) > 1e-3 * (1 + product.Magnitude))
                    continue;

                foreach (Rational s in Convergents(sum.Real, 1_000_000))
                {
                    foreach (Rational p in Convergents(product.Real, 1_000_000))
                    {
                        Rational[] candidate = [p, -s, Rational.One];
                        if (DivideExactly(polynomial, candidate) is Rational[] q)
                        {
                            quadratic = candidate;
                            quotient = q;
                            return true;
                        }
                    }
                }
            }
        }

        quadratic = [];
        quotient = [];
        return false;
    }

    // Continued-fraction convergents of `value`: the best rational approximations with
    // growing denominators. Only candidates - callers verify each one exactly.
    private static IEnumerable<Rational> Convergents(double value, long maxDenominator)
    {
        if (!double.IsFinite(value))
            yield break;

        // Standard recurrence: h(n) = a(n) * h(n-1) + h(n-2), same for k; seeded with
        // h(-1) = 1, h(-2) = 0, k(-1) = 0, k(-2) = 1.
        BigInteger h = BigInteger.One, hPrevious = BigInteger.Zero;
        BigInteger k = BigInteger.Zero, kPrevious = BigInteger.One;
        double x = value;

        for (int i = 0; i < 64; i++)
        {
            double a = Math.Floor(x);
            BigInteger term = new BigInteger(a);

            (h, hPrevious) = (term * h + hPrevious, h);
            (k, kPrevious) = (term * k + kPrevious, k);

            if (k > maxDenominator)
                yield break;

            yield return new Rational(h, k);

            double fraction = x - a;
            if (fraction < 1e-15)
                yield break;

            x = 1 / fraction;
        }
    }

    // x^2 + b*x + c (monic) with discriminant D = b^2 - 4c has roots -b/2 +- sqrt(D/4).
    // Only called for quadratics without rational roots, so D/4 is never a perfect square.
    private static bool TrySplitQuadratic(Variable x, Rational[] quadratic, bool complex, out Expr first, out Expr second)
    {
        Rational b = quadratic[1], c = quadratic[0];
        Rational quarterDiscriminant = b * b / 4 - c;

        Expr offset;
        if (quarterDiscriminant.Sign > 0)
            offset = SimplifiedSqrt(quarterDiscriminant);
        else if (quarterDiscriminant.Sign < 0 && complex)
            offset = new Multiply(SimplifiedSqrt(-quarterDiscriminant), new ImaginaryUnit());
        else
        {
            first = second = null!;
            return false;
        }

        Rational center = -b / 2;
        Expr shifted = center.IsZero ? x : new Subtract(x, new Constant(center));

        first = new Subtract(shifted, offset);
        second = new Add(shifted, offset);
        return true;
    }

    // sqrt(n/d) written as (k/d) * sqrt(m) with m square-free: sqrt(5/4) -> sqrt(5)/2,
    // sqrt(1/2) -> sqrt(2)/2, sqrt(8) -> 2*sqrt(2). Uses sqrt(n/d) = sqrt(n*d)/d.
    private static Expr SimplifiedSqrt(Rational value)
    {
        BigInteger radicand = value.Numerator * value.Denominator;
        BigInteger outside = BigInteger.One;

        // Trial division is enough here: radicands come from small polynomial coefficients.
        for (BigInteger p = 2; p * p <= radicand && p <= 1_000_000; p++)
        {
            while (radicand % (p * p) == 0)
            {
                radicand /= p * p;
                outside *= p;
            }
        }

        Rational coefficient = new Rational(outside, value.Denominator);
        Expr root = new Sqrt(new Constant(new Rational(radicand)));

        return coefficient.IsOne ? root : new Multiply(new Constant(coefficient), root);
    }

    private static Expr LinearFactor(Variable x, Rational root) =>
        root.IsZero ? x : new Subtract(x, new Constant(root));

    // leading * f1^m1 * f2^m2 * ..., with identical factors grouped into powers.
    private static Expr BuildProduct(Rational leading, IEnumerable<Expr> factors)
    {
        var grouped = new List<(Expr Factor, int Count)>();
        foreach (Expr factor in factors)
        {
            int index = grouped.FindIndex(g => g.Factor.Equals(factor));
            if (index >= 0)
                grouped[index] = (factor, grouped[index].Count + 1);
            else
                grouped.Add((factor, 1));
        }

        Expr? result = leading.IsOne ? null : new Constant(leading);
        foreach (var (factor, count) in grouped)
        {
            Expr term = count == 1 ? factor : new Power(factor, new Constant(count));
            result = result is null ? term : new Multiply(result, term);
        }

        return result ?? new Constant(leading);
    }

    private static Expr BuildPolynomial(Variable x, Rational[] coefficients)
    {
        Expr? result = null;
        for (int degree = coefficients.Length - 1; degree >= 0; degree--)
        {
            Rational c = coefficients[degree];
            if (c.IsZero)
                continue;

            Expr power = degree switch
            {
                0 => new Constant(1),
                1 => x,
                _ => new Power(x, new Constant(degree))
            };
            Expr term = degree == 0 ? new Constant(c) : c.IsOne ? power : new Multiply(new Constant(c), power);
            result = result is null ? term : new Add(result, term);
        }

        return result ?? new Constant(0);
    }

    // Exact Rational polynomial arithmetic; coefficients[i] belongs to x^i.

    private static int Degree(Rational[] polynomial) => polynomial.Length - 1;

    private static Rational[] TrimLeadingZeros(Rational[] polynomial)
    {
        int length = polynomial.Length;
        while (length > 1 && polynomial[length - 1].IsZero)
            length--;
        return polynomial[..length];
    }

    private static Rational[] Scale(Rational[] polynomial, Rational factor) =>
        polynomial.Select(c => c * factor).ToArray();

    private static Rational Evaluate(Rational[] polynomial, Rational x)
    {
        Rational result = Rational.Zero;
        for (int i = polynomial.Length - 1; i >= 0; i--)
            result = result * x + polynomial[i];
        return result;
    }

    // Long division by a monic divisor; null unless the remainder is exactly zero.
    private static Rational[]? DivideExactly(Rational[] dividend, Rational[] monicDivisor)
    {
        int divisorDegree = Degree(monicDivisor);
        if (Degree(dividend) < divisorDegree)
            return null;

        Rational[] remainder = (Rational[])dividend.Clone();
        var quotient = new Rational[Degree(dividend) - divisorDegree + 1];

        for (int i = quotient.Length - 1; i >= 0; i--)
        {
            Rational q = remainder[i + divisorDegree];
            quotient[i] = q;
            for (int j = 0; j <= divisorDegree; j++)
                remainder[i + j] -= q * monicDivisor[j];
        }

        for (int i = 0; i < divisorDegree; i++)
            if (!remainder[i].IsZero)
                return null;

        return quotient;
    }

    // All complex roots at once (Durand-Kerner). Accuracy only matters up to the point
    // where the continued fractions above can recognise the exact value.
    private static NumericComplex[] FindAllRootsNumerically(Rational[] monic)
    {
        int n = Degree(monic);
        double[] a = monic.Select(c => c.ToDouble()).ToArray();

        double radius = 1 + a.Take(n).Select(Math.Abs).DefaultIfEmpty(0).Max();
        var z = new NumericComplex[n];
        for (int k = 0; k < n; k++)
            z[k] = NumericComplex.FromPolarCoordinates(radius, 2 * Math.PI * k / n + 0.4);

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            double largestStep = 0;
            for (int k = 0; k < n; k++)
            {
                NumericComplex value = NumericComplex.Zero;
                for (int i = n; i >= 0; i--)
                    value = value * z[k] + a[i];

                NumericComplex denominator = NumericComplex.One;
                for (int j = 0; j < n; j++)
                    if (j != k)
                        denominator *= z[k] - z[j];

                if (denominator == NumericComplex.Zero)
                    denominator = new NumericComplex(1e-300, 0);

                NumericComplex step = value / denominator;
                z[k] -= step;
                largestStep = Math.Max(largestStep, step.Magnitude / (1 + z[k].Magnitude));
            }

            if (largestStep < 1e-15)
                break;
        }

        return z;
    }
}
