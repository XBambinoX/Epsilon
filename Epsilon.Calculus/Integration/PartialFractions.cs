using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    private Expr? PartialFractions(Expr f)
    {
        Expr constant = 1;
        var numerator = new List<RationalPolynomial>();
        var denominator = new List<(RationalPolynomial Base, int Exponent)>();
        if (!Collect(f, 1) || Factors(denominator) is not ({ Count: > 0 } factors, var leading))
            return null;
        if (constant.Simplify() is Constant { Value: var scale })
            (constant, leading) = (1, leading / scale);

        RationalPolynomial whole = numerator.Aggregate(RationalPolynomial.One, (product, factor) => product * factor) * (1 / leading);
        RationalPolynomial d = factors.Aggregate(RationalPolynomial.One, (product, factor) => product * factor.Factor.Pow(factor.Multiplicity));
        (whole, RationalPolynomial rest) = whole.DivRem(d);

        var logarithms = new List<(Rational Coefficient, RationalPolynomial Argument)>();
        Expr integral = whole.Antiderivative().ToExpr(_x);
        foreach (var (factor, multiplicity) in factors)
        {
            RationalPolynomial power = factor.Pow(multiplicity);
            RationalPolynomial c = (rest * d.DivRem(power).Quotient.InverseModulo(power)).DivRem(power).Remainder;
            for (int j = multiplicity; j >= 1; j--)
            {
                (c, RationalPolynomial a) = c.DivRem(factor);
                if (!a.IsZero)
                    integral += Term(a, factor, j, logarithms);
            }
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

    // The denominator's monic irreducible factors, merged, and its leading coefficient.
    private static (List<(RationalPolynomial Factor, int Multiplicity)> Factors, Rational Leading)? Factors(List<(RationalPolynomial Base, int Exponent)> denominator)
    {
        var factors = new List<(RationalPolynomial Factor, int Multiplicity)>();
        Rational leading = Rational.One;
        foreach (var (b, exponent) in denominator)
        {
            if (b.Factor() is not { } own)
                return null;

            leading *= b.Leading.Pow(exponent);
            foreach (var (factor, multiplicity) in own)
            {
                int index = factors.FindIndex(known => known.Factor.Equals(factor));
                if (index < 0)
                    factors.Add((factor, multiplicity * exponent));
                else
                    factors[index] = (factor, factors[index].Multiplicity + multiplicity * exponent);
            }
        }

        return (factors, leading);
    }

    // a / f^j for a monic f and a of lower degree, written with g = d f, the multiple of f with
    // integer coefficients. The logarithms are left to be merged.
    private Expr Term(RationalPolynomial a, RationalPolynomial f, int j, List<(Rational Coefficient, RationalPolynomial Argument)> logarithms)
    {
        RationalPolynomial g = f.Primitive();
        Rational d = g.Leading;
        Expr gx = g.ToExpr(_x);
        if (f.Degree == 1)
        {
            if (j > 1)
                return a[0] * d.Pow(j - 1) / ((1 - j) * new Power(gx, j - 1));

            logarithms.Add((a[0], g));
            return 0;
        }

        // a = alpha x + beta = alpha/2 f' + gamma
        Rational alpha = a[1], gamma = a[0] - alpha * f[1] / 2;
        QuadraticIntegrals q = QuadraticIntegrals.Of(gx, variable)!;
        Expr rest = gamma.IsZero ? 0 : gamma * d.Pow(j) * q.ReciprocalPower(j);
        if (j > 1)
            return -alpha / 2 * d.Pow(j - 1) / ((j - 1) * new Power(gx, j - 1)) + rest;

        if (!alpha.IsZero)
            logarithms.Add((alpha / 2, g));
        return rest;
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
