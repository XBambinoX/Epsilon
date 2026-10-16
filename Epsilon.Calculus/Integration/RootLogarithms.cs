using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    // c/p for a squarefree monic p: the sum of S(r) ln(x - r) over the roots r of p for
    // S = c/p' mod p, a pair of complex roots giving a logarithm and an arctangent. A constant S
    // needs no roots.
    private Expr? Residues(RationalPolynomial c, RationalPolynomial p, List<(Rational Coefficient, RationalPolynomial Argument)> logarithms, bool overRoots)
    {
        RationalPolynomial s = (c * p.Derivative().InverseModulo(p)).DivRem(p).Remainder;
        if (s.Degree == 0)
        {
            logarithms.Add((s[0], p.Primitive()));
            return 0;
        }

        if (!overRoots)
            return null;
        if (BinomialRoots(p) is var (h, radius, turns))
            return OverBinomialRoots(s, h, radius, turns);

        return p.Degree is 3 or 4 ? OverRadicals(p, s) : null;
    }

    private Expr OverBinomialRoots(RationalPolynomial s, Rational h, Expr radius, List<Rational> turns)
    {
        RationalPolynomial shifted = s.Shift(h);
        Expr t = _x + h;
        var logs = new List<(Expr Coefficient, Expr Argument, bool Positive)>();
        var arctangents = new List<(Expr Coefficient, Expr Argument)>();
        foreach (Rational turn in turns)
        {
            Expr re = 0, im = 0;
            for (int j = 0; j <= shifted.Degree; j++)
            {
                var (cos, sin) = CosSin(2 * j * turn);
                re += shifted[j] * new Power(radius, j) * cos;
                im += shifted[j] * new Power(radius, j) * sin;
            }

            Expr cosine = CosSin(2 * turn).Cos;
            if (turn.IsZero || turn == new Rational(1, 2))
            {
                logs.Add((re.Simplify(), t - radius * cosine, false));
                continue;
            }

            // At the angle a of the root, (t - R cos a)/(R sin a) is t csc(a)/R - cot(a).
            bool obtuse = 2 * turn > new Rational(1, 2);
            Expr angle = (obtuse ? 1 - 2 * turn : 2 * turn) * new Pi(), scaled = t * new Csc(angle) / radius, cot = new Cot(angle);
            logs.Add((re.Simplify(), new Power(t, 2) - 2 * radius * cosine * t + new Power(radius, 2), true));
            arctangents.Add(((-2 * im).Simplify(), obtuse ? scaled + cot : scaled - cot));
        }

        Expr sum = Merged(logs);
        foreach (var group in arctangents.Where(atan => !IsZero(atan.Coefficient)).GroupBy(atan => atan.Coefficient))
            sum += group.Key * group.Select(atan => (Expr)new Atan(atan.Argument)).Aggregate((atans, atan) => atans + atan);

        return sum;
    }

    // cos and sin of q pi, through an angle in [0, pi/2].
    private static (Expr Cos, Expr Sin) CosSin(Rational q)
    {
        q -= 2 * (q / 2).Floor();
        int sinSign = q > 1 ? -1 : 1;
        if (q > 1)
            q = 2 - q;
        int cosSign = q > new Rational(1, 2) ? -1 : 1;
        if (q > new Rational(1, 2))
            q = 1 - q;

        return ((cosSign * new Cos(q * new Pi())).Simplify(), (sinSign * new Sin(q * new Pi())).Simplify());
    }

    // c ln|a| with the logarithms of equal and opposite coefficients merged into one.
    private static Expr Merged(List<(Expr Coefficient, Expr Argument, bool Positive)> logs)
    {
        Expr sum = 0;
        var left = logs.Where(log => !IsZero(log.Coefficient)).ToList();
        while (left.Count > 0)
        {
            Expr c = left[0].Coefficient, negated = (-c).Simplify();
            if (c.Evaluate() < 0)
                (c, negated) = (negated, c);

            var up = left.Where(log => log.Coefficient.Equals(c)).ToList();
            var down = left.Where(log => log.Coefficient.Equals(negated)).ToList();
            left.RemoveAll(log => log.Coefficient.Equals(c) || log.Coefficient.Equals(negated));

            Expr argument = up.Count == 0 ? Product(down) : down.Count == 0 ? Product(up) : Product(up) / Product(down);
            Expr ln = new Ln(up.Concat(down).All(log => log.Positive) ? argument : new Abs(argument));
            sum += up.Count == 0 ? negated * ln : c * ln;
        }

        return sum;

        static Expr Product(List<(Expr Coefficient, Expr Argument, bool Positive)> logs) =>
            logs.Select(log => log.Argument).Aggregate((product, argument) => product * argument);
    }

    private static bool IsZero(Expr e) => e is Constant { Value.IsZero: true };

    // The roots of p as R e^(2 pi i turn) - h where p(t - h) divides t^m - a and R = |a|^(1/m): the
    // turns in [0, 1/2], of the real roots and of one root of each complex pair. Which of the m
    // roots of t^m - a are those of p is told by the value of p at them.
    private static (Rational Shift, Expr Radius, List<Rational> Turns)? BinomialRoots(RationalPolynomial p)
    {
        foreach (Rational h in new[] { Rational.Zero, p[p.Degree - 1] / p.Degree }.Distinct())
        {
            RationalPolynomial q = p.Shift(h);
            if (q.BinomialMultiple() is not var (m, a))
                continue;

            double radius = Math.Pow(Math.Abs(a.ToDouble()), 1.0 / m);
            double scale = Enumerable.Range(0, q.Degree + 1).Sum(j => Math.Abs(q[j].ToDouble()) * Math.Pow(radius, j));
            var candidates = Enumerable.Range(0, m)
                .Select(k => new Rational(2 * k + (a.Sign < 0 ? 1 : 0), 2 * m))
                .Select(turn => (Turn: turn, Size: Size(q, radius, turn) / scale))
                .OrderBy(candidate => candidate.Size)
                .ToList();
            if (candidates[q.Degree - 1].Size > 1e-9 || q.Degree < m && candidates[q.Degree].Size < 1e-4)
                return null;

            var turns = candidates.Take(q.Degree).Select(candidate => candidate.Turn > new Rational(1, 2) ? 1 - candidate.Turn : candidate.Turn).Distinct().Order().ToList();
            return (h, new NthRoot(new Constant(a.Abs()), m).Simplify(), turns);
        }

        return null;

        static double Size(RationalPolynomial q, double radius, Rational turn)
        {
            double angle = 2 * Math.PI * turn.ToDouble();
            var (re, im) = q.Evaluate(radius * Math.Cos(angle), radius * Math.Sin(angle));
            return double.Hypot(re, im);
        }
    }
}
