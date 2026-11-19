using Epsilon.Core;

namespace Epsilon.Solving;

internal static class Polynomials
{
    // The coefficients of expr as a polynomial in the variable, from the constant term up to the
    // highest nonzero one; null if it isn't a polynomial of at most maxDegree. The coefficients
    // may contain other variables.
    internal static Expr[]? Coefficients(Expr expr, string variable, int maxDegree)
    {
        var parts = new List<Expr>?[maxDegree + 1];
        if (!AddTerms(expr.Expand(SimplifyMode.Strict), variable, maxDegree, parts, positive: true))
            return null;

        Expr[] coefficients = [.. parts.Select(terms => terms is null ? 0 : terms.Aggregate((sum, term) => sum + term).Simplify())];
        int degree = Array.FindLastIndex(coefficients, coefficient => coefficient is not Constant { Value.IsZero: true });
        return coefficients[..(degree + 1)];
    }

    // The sum of coefficients[k] x^k.
    internal static Expr Of(IEnumerable<Expr> coefficients, string variable) =>
        coefficients.Select((coefficient, k) => coefficient * new Power(new Variable(variable), k))
            .Aggregate((sum, term) => sum + term).Simplify();

    // The coefficients as rational numbers, or null if one isn't.
    internal static Rational[]? Rationals(Expr[] coefficients) =>
        coefficients.All(coefficient => coefficient is Constant) ? [.. coefficients.Select(coefficient => ((Constant)coefficient).Value)] : null;

    // p / gcd(p, p'): the same roots, each once; with coprime integer coefficients, the highest
    // positive, where it differs from p.
    internal static Rational[] SquareFree(Rational[] p)
    {
        Rational[] derivative = [.. p.Skip(1).Select((coefficient, k) => coefficient * (k + 1))];
        Rational[] a = p, b = derivative;
        while (b.Length > 0)
            (a, b) = (b, DivideOut(a, b).Remainder);
        if (a.Length == 1)
            return p;

        Rational[] quotient = DivideOut(p, a).Quotient;
        var lcm = quotient.Aggregate(System.Numerics.BigInteger.One, (l, c) => l / System.Numerics.BigInteger.GreatestCommonDivisor(l, c.Denominator) * c.Denominator);
        var gcd = quotient.Aggregate(System.Numerics.BigInteger.Zero, (g, c) => System.Numerics.BigInteger.GreatestCommonDivisor(g, (c * new Rational(lcm)).Numerator));
        Rational scale = new Rational(lcm) / new Rational(gcd) * quotient[^1].Sign;
        return [.. quotient.Select(coefficient => coefficient * scale)];
    }

    private static (Rational[] Quotient, Rational[] Remainder) DivideOut(Rational[] a, Rational[] b)
    {
        var remainder = (Rational[])a.Clone();
        var quotient = new Rational[Math.Max(a.Length - b.Length + 1, 0)];
        for (int k = quotient.Length - 1; k >= 0; k--)
        {
            quotient[k] = remainder[k + b.Length - 1] / b[^1];
            for (int j = 0; j < b.Length; j++)
                remainder[k + j] -= quotient[k] * b[j];
        }

        int length = Math.Min(b.Length - 1, remainder.Length);
        while (length > 0 && remainder[length - 1].IsZero)
            length--;
        return (quotient, remainder[..length]);
    }

    private static bool AddTerms(Expr expr, string variable, int maxDegree, List<Expr>?[] parts, bool positive)
    {
        switch (expr)
        {
            case Add sum:
                return AddTerms(sum.Left, variable, maxDegree, parts, positive) && AddTerms(sum.Right, variable, maxDegree, parts, positive);
            case Subtract difference:
                return AddTerms(difference.Left, variable, maxDegree, parts, positive) && AddTerms(difference.Right, variable, maxDegree, parts, !positive);
            case Negate negation:
                return AddTerms(negation.Argument, variable, maxDegree, parts, !positive);
        }

        if (!TryTerm(expr, variable, maxDegree, out int degree, out Expr coefficient))
            return false;
        (parts[degree] ??= []).Add(positive ? coefficient : -coefficient);
        return true;
    }

    private static bool TryTerm(Expr term, string variable, int maxDegree, out int degree, out Expr coefficient)
    {
        degree = 0;
        coefficient = term;
        if (!term.DependsOn(variable))
            return true;

        switch (term)
        {
            case Variable:
                degree = 1;
                coefficient = 1;
                return degree <= maxDegree;
            case Power { Base: Variable, Exponent: Constant { Value: { IsInteger: true } n } } when n.Numerator.Sign > 0 && n.Numerator <= maxDegree:
                degree = (int)n.Numerator;
                coefficient = 1;
                return true;
            case Multiply product
                when TryTerm(product.Left, variable, maxDegree, out int leftDegree, out Expr left)
                    && TryTerm(product.Right, variable, maxDegree, out int rightDegree, out Expr right)
                    && leftDegree + rightDegree <= maxDegree:
                degree = leftDegree + rightDegree;
                coefficient = left * right;
                return true;
            case Divide quotient when !quotient.Denominator.DependsOn(variable)
                && TryTerm(quotient.Numerator, variable, maxDegree, out degree, out Expr numerator):
                coefficient = numerator / quotient.Denominator;
                return true;
            case Negate negation when TryTerm(negation.Argument, variable, maxDegree, out degree, out Expr argument):
                coefficient = -argument;
                return true;
            default:
                return false;
        }
    }
}
