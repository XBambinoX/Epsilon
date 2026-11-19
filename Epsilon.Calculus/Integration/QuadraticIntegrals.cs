using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed class QuadraticIntegrals
{
    private readonly Expr _q, _p, _h, _k, _shifted, _cleared;
    private readonly Rational _m;
    private readonly int _pSign, _kSign;

    private QuadraticIntegrals(Expr q, Expr shifted, Expr p, Expr h, Expr k, int pSign, int kSign)
    {
        _q = q;
        _shifted = shifted;
        // m (x - h) for the denominator m of h: atan((2x - 1)/sqrt(3)) rather than atan((x - 1/2)/sqrt(3/4)).
        _m = h is Constant { Value: var center } ? new Rational(center.Denominator) : Rational.One;
        _cleared = _m.IsOne ? shifted : (_m * shifted).Expand();
        _p = p;
        _h = h;
        _k = k;
        _pSign = pSign;
        _kSign = kSign;
    }

    // q = p((x - h)^2 + k).
    public (Expr P, Expr H, Expr K, int PSign, int KSign) Vertex => (_p, _h, _k, _pSign, _kSign);

    public static QuadraticIntegrals? Of(Expr q, string variable)
    {
        Expr first, second;
        try
        {
            first = q.Differentiate(variable);
            second = first.Differentiate(variable);
        }
        catch (NotSupportedException)
        {
            return null;
        }

        Expr p = (second / 2).Simplify();
        if (p.GetVariables().Count > 0)
            return null;

        Expr b = first.Substitute(variable, 0).Simplify();
        Expr c = q.Substitute(variable, 0).Simplify();
        if (b.GetVariables().Count > 0 || c.GetVariables().Count > 0)
            return null;

        // Not a polynomial, as ((u + 1/u)/2 + 2) u: undefined at 0, with 0^-1 in c.
        if (!double.IsFinite(b.Evaluate()) || !double.IsFinite(c.Evaluate()))
            return null;

        Expr h = (-b / (2 * p)).Simplify();
        Expr k = (c / p - new Power(h, 2)).Simplify();
        int? pSign = SignOf(p), kSign = SignOf(k);
        if (pSign is not (1 or -1) || kSign is null)
            return null;

        return new QuadraticIntegrals(q, (new Variable(variable) - h).Simplify(), p, h, k, pSign.Value, kSign.Value);
    }

    public Expr Reciprocal() => _kSign switch
    {
        > 0 => Over(new Atan(Over(_cleared, new Sqrt(Cleared(_k)))), _p * new Sqrt(_k)),
        < 0 => Over(Logarithm(_cleared, new Sqrt(Cleared(-_k))), 2 * _p * new Sqrt((-_k).Simplify())),
        _ => -1 / (_p * _shifted),
    };

    // ln|(t - r)/(t + r)|, with the denominator of r = n sqrt(d)/m cleared: (2x - sqrt(2))/(2x + sqrt(2)).
    private static Expr Logarithm(Expr t, Expr r)
    {
        if (QuadraticSurd.Of(r) is { A.IsZero: true, B: var b } surd && !b.Denominator.IsOne)
        {
            var m = new Rational(b.Denominator);
            (t, r) = ((m * t).Expand(), new QuadraticSurd(0, b * m, surd.D).ToExpr());
        }

        return new Ln(new Abs((t - r) / (t + r)));
    }

    // e/r for a constant r: times n sqrt(d) where 1/r is that for an integer n, sqrt(2) x for
    // x/sqrt(1/2), else over r, x/sqrt(2).
    private static Expr Over(Expr e, Expr r) =>
        QuadraticSurd.Of(1 / r) is { A.IsZero: true, B: { IsInteger: true, IsZero: false } } inverse ? inverse.ToExpr() * e : e / r;

    // The reduction: that of 1/q^n is (t/q^(n-1) + (2n - 3) times that of 1/q^(n-1)) / (2(n - 1) k p), t = x - h.
    public Expr ReciprocalPower(int n) => n == 1 ? Reciprocal()
        : _kSign == 0 ? -1 / ((2 * n - 1) * new Power(_p, n) * new Power(_shifted, 2 * n - 1))
        : (_cleared / new Power(_q, n - 1) / _m + (2 * n - 3) * ReciprocalPower(n - 1)) / (2 * (n - 1) * _k * _p);

    public Expr? InverseSqrt() => (_pSign, _kSign) switch
    {
        (> 0, > 0) => Over(new Asinh(Over(_cleared, new Sqrt(Cleared(_k)))), new Sqrt(_p)),
        (> 0, < 0) => Over(new Ln(new Abs(new Sqrt(_p) * _shifted + new Sqrt(_q))), new Sqrt(_p)),
        (> 0, _) => Over(new Sign(_shifted) * new Ln(new Abs(_shifted)), new Sqrt(_p)),
        (< 0, < 0) => Over(new Asin(Over(_cleared, new Sqrt(Cleared(-_k)))), new Sqrt((-_p).Simplify())),
        _ => null,
    };

    public Expr? Sqrt() => InverseSqrt() is { } inverse ? _shifted / 2 * new Sqrt(_q) + _k * _p / 2 * inverse : null;

    public Expr LinearOver(Expr alpha, Expr beta) =>
        alpha / (2 * _p) * new Ln(_pSign > 0 && _kSign > 0 ? _q : new Abs(_q)) + (beta + alpha * _h) * Reciprocal();

    public Expr? LinearOverSqrt(Expr alpha, Expr beta) =>
        InverseSqrt() is { } inverse ? alpha / _p * new Sqrt(_q) + (beta + alpha * _h) * inverse : null;

    private Expr Cleared(Expr k) => (_m.IsOne ? k : _m * _m * k).Simplify();

    private static int? SignOf(Expr constant)
    {
        if (constant is Constant { Value.IsZero: true })
            return 0;

        double value = constant.Evaluate();
        return double.IsNaN(value) ? null : Math.Sign(value);
    }
}
