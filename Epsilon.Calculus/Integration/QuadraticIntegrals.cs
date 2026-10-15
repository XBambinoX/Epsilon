using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed class QuadraticIntegrals
{
    private readonly Expr _q, _p, _h, _k, _shifted;
    private readonly int _pSign, _kSign;

    private QuadraticIntegrals(Expr q, Expr shifted, Expr p, Expr h, Expr k, int pSign, int kSign)
    {
        _q = q;
        _shifted = shifted;
        _p = p;
        _h = h;
        _k = k;
        _pSign = pSign;
        _kSign = kSign;
    }

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

        Expr b = first.Substitute(variable, 0).Simplify(), c = q.Substitute(variable, 0).Simplify();
        if (b.GetVariables().Count > 0 || c.GetVariables().Count > 0)
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
        > 0 => new Atan(_shifted / new Sqrt(_k)) / (_p * new Sqrt(_k)),
        < 0 => new Ln(new Abs((_shifted - new Sqrt(-_k)) / (_shifted + new Sqrt(-_k)))) / (2 * _p * new Sqrt(-_k)),
        _ => -1 / (_p * _shifted),
    };

    public Expr? InverseSqrt() => (_pSign, _kSign) switch
    {
        (> 0, > 0) => new Asinh(_shifted / new Sqrt(_k)) / new Sqrt(_p),
        (> 0, < 0) => new Ln(new Abs(new Sqrt(_p) * _shifted + new Sqrt(_q))) / new Sqrt(_p),
        (> 0, _) => new Sign(_shifted) * new Ln(new Abs(_shifted)) / new Sqrt(_p),
        (< 0, < 0) => new Asin(_shifted / new Sqrt(-_k)) / new Sqrt(-_p),
        _ => null,
    };

    public Expr? Sqrt() => InverseSqrt() is { } inverse ? _shifted / 2 * new Sqrt(_q) + _k * _p / 2 * inverse : null;

    public Expr LinearOver(Expr alpha, Expr beta) =>
        alpha / (2 * _p) * new Ln(_pSign > 0 && _kSign > 0 ? _q : new Abs(_q)) + (beta + alpha * _h) * Reciprocal();

    public Expr? LinearOverSqrt(Expr alpha, Expr beta) =>
        InverseSqrt() is { } inverse ? alpha / _p * new Sqrt(_q) + (beta + alpha * _h) * inverse : null;

    private static int? SignOf(Expr constant)
    {
        if (constant is Constant { Value.IsZero: true })
            return 0;

        double value = constant.Evaluate();
        return double.IsNaN(value) ? null : Math.Sign(value);
    }
}
