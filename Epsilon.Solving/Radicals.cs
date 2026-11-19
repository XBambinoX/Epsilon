using Epsilon.Core;

namespace Epsilon.Solving;

// Roots of cubics and quartics with rational coefficients, written in radicals.
internal static class Radicals
{
    // x = t - a/3 makes x^3 + a x^2 + b x + c the depressed t^3 + P t + Q. With one real root it is
    // u + v - a/3 by Cardano's formula, the others -(u + v)/2 - a/3 +- i sqrt(3)/2 (u - v); three
    // are 2 sqrt(-P/3) cos(acos(3Q/(2P) sqrt(-3/P))/3 - 2 pi k/3) - a/3.
    internal static List<Expr>? Cubic(Rational[] monic, bool complex)
    {
        Rational a = monic[2], b = monic[1], c = monic[0], h = a / 3;
        Rational linear = b - a * a / 3, constant = 2 * a * a * a / 27 - a * b / 3 + c;
        Rational half = constant * constant / 4 + linear * linear * linear / 27;
        if (half.IsZero)
            return null;

        if (half.Sign > 0)
        {
            Expr u = new NthRoot(-constant / 2 + new Sqrt(half), 3), v = new NthRoot(-constant / 2 - new Sqrt(half), 3);
            Expr real = (u + v - h).Simplify();
            if (!complex)
                return [real];

            Expr centre = (-(u + v) / 2 - h).Simplify(), apart = (Expr.I * new Sqrt(3) * (u - v) / 2).Simplify();
            return [real, (centre - apart).Simplify(), (centre + apart).Simplify()];
        }

        Expr amplitude = 2 * new Sqrt(-linear / 3), third = (new Acos(3 * constant / (2 * linear) * new Sqrt(-3 / linear)) / 3).Simplify();
        return [.. Enumerable.Range(0, 3).Select(k => (amplitude * new Cos(third - new Rational(2 * k, 3) * Expr.PI) - h).Simplify())];
    }

    // x^4 + a x^3 + b x^2 + c x + d as two quadratics x^2 + B x + C, by Ferrari's method on the
    // depressed t^4 + P t^2 + Q t + R, t = x + a/4.
    internal static List<(Expr B, Expr C)>? Quartic(Rational[] monic, string variable)
    {
        Rational a = monic[3], b = monic[2], c = monic[1], d = monic[0], h = a / 4;
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
            // A root y of the resolvent with 2y > P splits the quartic. Without a rational one the
            // roots need cube roots inside square roots, too long to read: null, for numeric roots.
            Rational[] resolvent = [(4 * square * constant - linear * linear) / 8, -constant, -square / 2, 1];
            if (RationalRoots(resolvent, variable).Where(root => 2 * root > square).Select(root => (Expr)root).FirstOrDefault() is not { } y)
                return null;

            Expr m = new Sqrt(2 * y - square).Simplify(), offset = (linear / (2 * m)).Simplify();
            quadratics = [(-m, y + offset), (m, y - offset)];
        }

        return [.. quadratics.Select(q => ((q.B + 2 * h).Simplify(), (h * h + q.B * h + q.C).Simplify()))];
    }

    private static IEnumerable<Rational> RationalRoots(Rational[] p, string variable)
    {
        var (factored, _) = Polynomials.Of(p.Select(coefficient => (Expr)coefficient), variable).TryFactorReal(variable);
        foreach (Expr factor in Solver.Factors(factored))
            if (Polynomials.Coefficients(factor, variable, 1) is [Constant c0, Constant c1])
                yield return -c0.Value / c1.Value;
    }
}
