using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    // Over the roots of factors of degree 3 or more only after substitution, which writes
    // x/(1 + x^4) as atan(x^2)/2.
    private Expr? PartialFractions(Expr f, bool overRoots)
    {
        Expr constant = 1;
        var numerator = new List<RationalPolynomial>();
        var denominator = new List<(RationalPolynomial Base, int Exponent)>();
        if (!Collect(f, 1))
            return null;

        var factors = Factors(denominator);
        RationalPolynomial d = factors.Aggregate(RationalPolynomial.One, (product, factor) => product * factor.Factor.Pow(factor.Multiplicity));
        if (d.Degree == 0)
            return null;

        Rational leading = denominator.Aggregate(Rational.One, (product, b) => product * b.Base.Leading.Pow(b.Exponent));
        if (constant.Simplify() is Constant { Value: var scale })
            (constant, leading) = (1, leading / scale);

        RationalPolynomial n = numerator.Aggregate(RationalPolynomial.One, (product, factor) => product * factor) * (1 / leading);
        var (whole, rest) = n.DivRem(d);
        var (rational, a) = Hermite(rest, d, factors);

        Expr integral = whole.Antiderivative().ToExpr(_x) + RationalPart(rational, factors);
        var logarithms = new List<(Rational Coefficient, RationalPolynomial Argument)>();
        RationalPolynomial s = factors.Aggregate(RationalPolynomial.One, (product, factor) => product * factor.Factor);
        foreach (var (factor, _) in factors)
        {
            RationalPolynomial c = (a * (s / factor).InverseModulo(factor)).DivRem(factor).Remainder;
            if (c.IsZero)
                continue;
            if (Term(c, factor, logarithms, overRoots) is not { } term)
                return null;

            integral += term;
        }

        return constant * (integral + Logarithms(logarithms));

        // f as a constant times a product of polynomials over a product of powers of polynomials.
        bool Collect(Expr e, int exponent)
        {
            if (!e.DependsOn(variable))
            {
                constant = exponent > 0 ? constant * new Power(e, exponent) : constant / new Power(e, -exponent);
                return true;
            }

            switch (e)
            {
                case Multiply product:
                    return Collect(product.Left, exponent) && Collect(product.Right, exponent);
                case Divide quotient:
                    return Collect(quotient.Numerator, exponent) && Collect(quotient.Denominator, -exponent);
                case Negate negate:
                    if (exponent % 2 != 0)
                        constant = -constant;
                    return Collect(negate.Argument, exponent);
                case Power { Exponent: Constant { Value.IsInteger: true } n } power when Math.Abs(exponent * n.Value.ToDouble()) <= RationalPolynomial.MaxDegree:
                    return Collect(power.Base, exponent * (int)n.Value.Numerator);
            }

            if (RationalPolynomial.Of(e, variable) is not { } p || p.Degree * Math.Abs(exponent) > RationalPolynomial.MaxDegree)
                return false;
            if (exponent >= 0)
                numerator.Add(p.Pow(exponent));
            else
                denominator.Add((p, -exponent));

            return true;
        }
    }

    // The factors of the bases, made coprime, with their multiplicities in the denominator.
    private static List<(RationalPolynomial Factor, int Multiplicity)> Factors(List<(RationalPolynomial Base, int Exponent)> denominator)
    {
        var factors = new List<(RationalPolynomial Factor, int Multiplicity)>();
        foreach (var (b, exponent) in denominator)
            foreach (var (piece, multiplicity) in b.Factor())
            {
                RationalPolynomial p = piece;
                var coprime = new List<(RationalPolynomial Factor, int Multiplicity)>();
                foreach (var (q, m) in factors)
                {
                    RationalPolynomial common = RationalPolynomial.Gcd(p, q);
                    if (common.Degree == 0)
                    {
                        coprime.Add((q, m));
                        continue;
                    }

                    coprime.Add((common, m + multiplicity * exponent));
                    if (common.Degree < q.Degree)
                        coprime.Add((q / common, m));
                    p /= common;
                }

                if (p.Degree > 0)
                    coprime.Add((p, multiplicity * exponent));
                factors = coprime;
            }

        return factors;
    }

    // Hermite's reduction: a/d = (g/q)' + h/s for q the product of the factors to one power less
    // and s their product; returns g and h.
    private static (RationalPolynomial G, RationalPolynomial H) Hermite(RationalPolynomial a, RationalPolynomial d, List<(RationalPolynomial Factor, int Multiplicity)> factors)
    {
        RationalPolynomial q = factors.Aggregate(RationalPolynomial.One, (product, factor) => product * factor.Factor.Pow(factor.Multiplicity - 1));
        RationalPolynomial g = RationalPolynomial.Zero;
        foreach (var (v, i) in factors.Where(factor => factor.Multiplicity > 1))
        {
            RationalPolynomial u = d / v.Pow(i), w = u * v.Derivative();
            for (int j = i - 1; j >= 1; j--)
            {
                // b w + c v = -a/j makes a/(u v^(j+1)) = (b/v^j)' + (-j c - u b')/(u v^j).
                RationalPolynomial right = a * new Rational(-1, j);
                RationalPolynomial b = (right * w.InverseModulo(v)).DivRem(v).Remainder, c = (right - b * w) / v;
                g += b * (q / v.Pow(j));
                a = c * -j - u * b.Derivative();
            }

            d = u * v;
        }

        return (g, a);
    }

    // g over the factors to one power less, written with their multiples with integer coefficients.
    private Expr RationalPart(RationalPolynomial g, List<(RationalPolynomial Factor, int Multiplicity)> factors)
    {
        if (g.IsZero)
            return 0;

        Expr denominator = 1;
        foreach (var (factor, multiplicity) in factors.Where(factor => factor.Multiplicity > 1))
        {
            RationalPolynomial primitive = factor.Primitive();
            g *= primitive.Leading.Pow(multiplicity - 1);
            denominator *= new Power(primitive.ToExpr(_x), multiplicity - 1);
        }

        RationalPolynomial integral = g.Monic().Primitive();
        Rational content = g.Leading / integral.Leading;
        return new Rational(content.Numerator) * integral.ToExpr(_x) / (new Rational(content.Denominator) * denominator);
    }

    // a/f for a monic f and a of lower degree, written with g = d f, the multiple of f with
    // integer coefficients. The logarithms are left to be merged.
    private Expr? Term(RationalPolynomial a, RationalPolynomial f, List<(Rational Coefficient, RationalPolynomial Argument)> logarithms, bool overRoots)
    {
        RationalPolynomial g = f.Primitive();
        if (f.Degree == 1)
        {
            logarithms.Add((a[0], g));
            return 0;
        }
        if (f.Degree > 2)
            return Residues(a, f, logarithms, overRoots);

        // a = alpha x + beta = alpha/2 f' + gamma
        Rational alpha = a[1], gamma = a[0] - alpha * f[1] / 2;
        if (!alpha.IsZero)
            logarithms.Add((alpha / 2, g));

        return gamma.IsZero ? 0 : gamma * g.Leading * QuadraticIntegrals.Of(g.ToExpr(_x), variable)!.Reciprocal();
    }

    // c ln|g| for each c and g, with the logarithms of coefficients equal in size merged into one
    // of a product or quotient: ln|x - 1|/2 - ln|x + 1|/2 is ln|(x - 1)/(x + 1)|/2.
    private Expr Logarithms(List<(Rational Coefficient, RationalPolynomial Argument)> logarithms)
    {
        Expr sum = 0;
        foreach (var group in logarithms.GroupBy(logarithm => logarithm.Coefficient.Abs()))
        {
            RationalPolynomial up = Product(group, 1), down = Product(group, -1);
            Expr argument = down.Degree == 0 ? up.ToExpr(_x)
                : up.Degree == 0 ? down.ToExpr(_x)
                : up.ToExpr(_x) / down.ToExpr(_x);
            bool positive = group.All(logarithm => logarithm.Argument is var g && g.Degree == 2 && g[1] * g[1] < 4 * g[2] * g[0]);

            sum += (up.Degree == 0 ? -group.Key : group.Key) * new Ln(positive ? argument : new Abs(argument));
        }

        return sum;

        static RationalPolynomial Product(IEnumerable<(Rational Coefficient, RationalPolynomial Argument)> logarithms, int sign) =>
            logarithms.Where(logarithm => logarithm.Coefficient.Sign == sign).Aggregate(RationalPolynomial.One, (product, logarithm) => product * logarithm.Argument);
    }
}
