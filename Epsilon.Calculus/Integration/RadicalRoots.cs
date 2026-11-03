using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    // c/p for a cubic or quartic p over its roots written in radicals, S = c/p' mod p.
    private Expr? OverRadicals(RationalPolynomial p, RationalPolynomial s) =>
        p.Degree == 3 ? OverCubicRoots(p, s) : OverQuarticRoots(p, s);

    private Expr? OverCubicRoots(RationalPolynomial p, RationalPolynomial s)
    {
        if (CubicRealRoots(p) is not { } roots)
            return null;
        if (roots.Count == 3)
            return roots.Aggregate((Expr)0, (sum, root) => sum + s.ToExpr(root) * new Ln(new Abs(_x - root)));

        Expr r = roots[0];
        RationalPolynomial b = new(p[2], 1), c = new(p[1], p[2], 1);
        RationalPolynomial s1 = new RationalPolynomial(s[1]) - b * s[2], s0 = new RationalPolynomial(s[0]) - c * s[2];
        return s.ToExpr(r) * new Ln(new Abs(_x - r))
            + OverQuadratic(b.ToExpr(r), c.ToExpr(r), (s0 - s1 * b * new Rational(1, 2)).ToExpr(r), s1.ToExpr(r), (c * 4 - b * b).ToExpr(r));
    }

    private Expr? OverQuarticRoots(RationalPolynomial p, RationalPolynomial s)
    {
        if (QuarticFactors(p) is not { } quadratics)
            return null;

        Expr sum = 0;
        foreach (var (b, c) in quadratics)
        {
            // x^2 = -B x - C and x^3 = (B^2 - C) x + B C modulo the quadratic.
            Expr s1 = (s[1] - s[2] * b + s[3] * (b * b - c)).Simplify(), s0 = (s[0] - s[2] * c + s[3] * b * c).Simplify();
            sum += OverQuadratic(b, c, (s0 - s1 * b / 2).Simplify(), s1, (4 * c - b * b).Simplify());
        }

        return sum;
    }

    private Expr OverQuadratic(Expr b, Expr c, Expr re, Expr s1, Expr k)
    {
        Expr linear = 2 * _x + b, q = new Power(_x, 2) + b * _x + c;
        return k.Evaluate() > 0
            ? re * new Ln(q) - (s1 * new Sqrt(k)).Simplify() * new Atan(linear / new Sqrt(k))
            : re * new Ln(new Abs(q)) + (s1 * new Sqrt(-k) / 2).Simplify() * new Ln(new Abs((linear - new Sqrt(-k)) / (linear + new Sqrt(-k))));
    }

    // x = t - a/3 makes x^3 + a x^2 + b x + c the depressed t^3 + P t + Q. One real root is
    // w1 + w2 - a/3 by Cardano's formula; three are 2 sqrt(-P/3) cos(acos(3Q/(2P) sqrt(-3/P))/3 - 2 pi k/3) - a/3.
    private static List<Expr>? CubicRealRoots(RationalPolynomial p)
    {
        Rational a = p[2], b = p[1], c = p[0], h = a / 3;
        Rational linear = b - a * a / 3, constant = 2 * a * a * a / 27 - a * b / 3 + c;
        Rational half = constant * constant / 4 + linear * linear * linear / 27;
        if (half.IsZero)
            return null;
        if (half.Sign > 0)
            return [(new NthRoot(-constant / 2 + new Sqrt(half), 3) + new NthRoot(-constant / 2 - new Sqrt(half), 3) - h).Simplify()];

        Expr amplitude = 2 * new Sqrt(-linear / 3), third = (new Acos(3 * constant / (2 * linear) * new Sqrt(-3 / linear)) / 3).Simplify();
        return [.. Enumerable.Range(0, 3).Select(k => (amplitude * (PiMultiple(third) is { } q
            ? CosSin(q - new Rational(2 * k, 3)).Cos
            : new Cos(third - new Rational(2 * k, 3) * new Pi())) - h).Simplify())];
    }

    internal static Rational? PiMultiple(Expr e) => e switch
    {
        Pi => Rational.One,
        Multiply { Left: Constant c } product when PiMultiple(product.Right) is { } q => c.Value * q,
        Divide { Denominator: Constant d } quotient when PiMultiple(quotient.Numerator) is { } q => q / d.Value,
        _ => null,
    };

    private static List<(Expr B, Expr C)>? QuarticFactors(RationalPolynomial p)
    {
        Rational a = p[3], b = p[2], c = p[1], d = p[0], h = a / 4;
        Rational square = b - 3 * a * a / 8, linear = a * a * a / 8 - a * b / 2 + c;
        Rational constant = -3 * a * a * a * a / 256 + a * a * b / 16 - a * c / 4 + d;

        List<(Expr B, Expr C)> quadratics;
        if (linear.IsZero)
        {
            Rational discriminant = square * square - 4 * constant;
            Expr m = new Sqrt(2 * new Sqrt(constant) - square);
            quadratics = discriminant.Sign > 0
                ? [(0, (square - new Sqrt(discriminant)) / 2), (0, (square + new Sqrt(discriminant)) / 2)]
                : [(-m, new Sqrt(constant)), (m, new Sqrt(constant))];
        }
        else
        {
            var resolvent = new RationalPolynomial(4 * square * constant - linear * linear, -8 * constant, -4 * square, 8).Monic();
            Expr? y = resolvent.RationalRoots().Where(root => 2 * root > square).Select(root => (Expr)root).FirstOrDefault()
                ?? CubicRealRoots(resolvent)?.MaxBy(root => root.Evaluate());
            if (y is null)
                return null;

            Expr m = new Sqrt(2 * y - square).Simplify(), offset = (linear / (2 * m)).Simplify();
            quadratics = [(-m, y + offset), (m, y - offset)];
        }

        // t^2 + B t + C with t = x + h.
        return [.. quadratics.Select(q => ((q.B + 2 * h).Simplify(), (h * h + q.B * h + q.C).Simplify()))];
    }
}
