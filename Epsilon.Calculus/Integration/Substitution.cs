using Epsilon.Core;

namespace Epsilon.Calculus;

internal static class Substitution
{
    internal static Expr TrySubstitution(this Expr expr, int depth)
    {
        if (TryLinearSubstitution(expr, out Expr linear))
            return linear;

        if (TryProductSubstitution(expr, out Expr product))
            return product;

        if (TryUDuSubstitution(expr, out Expr uDu))
            return uDu;

        throw new NotSupportedException($"No substitution rule for '{expr.Print()}'.");
    }

    // Recognizes composite forms: f(argument), returning argument and a way to rebuild f(u)
    private static bool TryGetComposite(Expr expr, out Expr argument, out Func<Expr, Expr> rebuild)
    {
        switch (expr)
        {
            case Sin(var a):
                argument = a; rebuild = x => new Sin(x); return true;
            case Cos(var a):
                argument = a; rebuild = x => new Cos(x); return true;
            case Exp(var a):
                argument = a; rebuild = x => new Exp(x); return true;
            case Divide(Constant one, var a) when one.Value == 1:
                argument = a; rebuild = x => new Divide(new Constant(1), x); return true;
            case Power(var a, Constant n) when n.Value != -1:
                Constant capturedN = n;
                argument = a; rebuild = x => new Power(x, capturedN); return true;
            default:
                argument = null!; rebuild = null!; return false;
        }
    }

    private static bool TryUDuSubstitution(Expr expr, out Expr result)
    {
        result = null!;

        Expr a, b;
        if (expr is Multiply(var l, var r)) { a = l; b = r; }
        else if (expr is Divide(var num, var den)) { a = num; b = new Divide(new Constant(1), den); }
        else return false;

        return TryMatchUDu(a, b, out result) || TryMatchUDu(b, a, out result);
    }

    private static bool TryMatchUDu(Expr u, Expr maybeDerivative, out Expr result)
    {
        result = null!;

        if (u is Variable or Constant)
            return false;

        Expr uPrime = u.Differentiate().Simplify();

        var (coefA, termA) = ExtractCoefficient(maybeDerivative);
        var (coefB, termB) = ExtractCoefficient(uPrime);

        if (!termA.Equals(termB))
            return false;

        double ratio = coefA / coefB;

        Expr uSquaredOverTwo = new Divide(new Power(u, new Constant(2)), new Constant(2));
        result = ratio == 1 ? uSquaredOverTwo : new Multiply(new Constant(ratio), uSquaredOverTwo);
        return true;
    }

    // Recognizes ax+b (or bare x, a=1 b=0). Returns (0, _) if not linear.
    private static (double A, Expr B) ExtractLinear(Expr arg) => arg switch
    {
        Variable => (1, new Constant(0)),
        Add(Multiply(Constant a, Variable), var b) => (a.Value, b),
        Add(var b, Multiply(Constant a, Variable)) => (a.Value, b),
        Add(Variable, var b) => (1, b),
        Subtract(Multiply(Constant a, Variable), var b) => (a.Value, new Subtract(new Constant(0), b)),
        Subtract(Variable, var b) => (1, new Subtract(new Constant(0), b)),
        _ => (0, arg)
    };

    // ∫f(ax+b) dx = F(ax+b) / a  — handles cases like 1/(x+1), sin(2x+1), exp(3x)
    private static bool TryLinearSubstitution(Expr expr, out Expr result)
    {
        result = null!;

        if (!TryGetComposite(expr, out Expr argument, out var rebuild))
            return false;

        if (argument is Variable)
            return false; // already handled directly by TableRule, no substitution needed

        var (a, _) = ExtractLinear(argument);
        if (a == 0)
            return false; // argument isn't linear — can't handle with this method

        Expr uForm = rebuild(new Variable());

        Expr uAntiderivative;
        try { uAntiderivative = SymbolicIntegrator.TableRule(uForm); }
        catch (NotSupportedException) { return false; }

        Expr substituted = SubstituteVariable(uAntiderivative, argument);
        result = a == 1 ? substituted : new Divide(substituted, new Constant(a));
        return true;
    }

    // ∫f(g(x)) * g'(x) dx = F(g(x))  — handles cases like 2x*sin(x^2)
    private static bool TryProductSubstitution(Expr expr, out Expr result)
    {
        result = null!;

        if (expr is not Multiply(var left, var right))
            return false;

        return TryMatch(left, right, out result) || TryMatch(right, left, out result);
    }

    private static bool TryMatch(Expr composite, Expr other, out Expr result)
    {
        result = null!;

        if (!TryGetComposite(composite, out Expr argument, out var rebuild))
            return false;

        if (argument is Variable)
            return false; // not actually composite in the way we care about

        Expr innerDerivative = argument.Differentiate().Simplify();

        var (coefOther, termOther) = ExtractCoefficient(other);
        var (coefDeriv, termDeriv) = ExtractCoefficient(innerDerivative);

        if (!termOther.Equals(termDeriv))
            return false;

        double ratio = coefOther / coefDeriv;

        Expr uForm = rebuild(new Variable());

        Expr uAntiderivative;
        try { uAntiderivative = SymbolicIntegrator.TableRule(uForm); }
        catch (NotSupportedException) { return false; }

        Expr substituted = SubstituteVariable(uAntiderivative, argument);
        result = ratio == 1 ? substituted : new Multiply(new Constant(ratio), substituted);
        return true;
    }

    private static (double Coefficient, Expr Term) ExtractCoefficient(Expr expr) => expr switch
    {
        Multiply(Constant c, var t) => (c.Value, t),
        Multiply(var t, Constant c) => (c.Value, t),
        _ => (1, expr)
    };

    // Recursively replaces every occurrence of Variable with replacement
    private static Expr SubstituteVariable(Expr expr, Expr replacement) => expr switch
    {
        Variable => replacement,
        Constant => expr,
        Add(var l, var r) => new Add(SubstituteVariable(l, replacement), SubstituteVariable(r, replacement)),
        Subtract(var l, var r) => new Subtract(SubstituteVariable(l, replacement), SubstituteVariable(r, replacement)),
        Multiply(var l, var r) => new Multiply(SubstituteVariable(l, replacement), SubstituteVariable(r, replacement)),
        Divide(var n, var d) => new Divide(SubstituteVariable(n, replacement), SubstituteVariable(d, replacement)),
        Power(var b, var e) => new Power(SubstituteVariable(b, replacement), SubstituteVariable(e, replacement)),
        Sin(var a) => new Sin(SubstituteVariable(a, replacement)),
        Cos(var a) => new Cos(SubstituteVariable(a, replacement)),
        Exp(var a) => new Exp(SubstituteVariable(a, replacement)),
        Ln(var a) => new Ln(SubstituteVariable(a, replacement)),
        Sqrt(var a) => new Sqrt(SubstituteVariable(a, replacement)),
        _ => expr
    };
}