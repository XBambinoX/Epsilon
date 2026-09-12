using System.Numerics;

namespace Epsilon.Core;

public static class Simplifier
{
    public static Expr Simplify(this Expr expr)
    {
        Expr powered = ToPowers(expr.Canonicalize());
        Expr simplified = SimplifyPowers(powered);
        return PreferRoots(simplified).Canonicalize();
    }

    private static Expr SimplifyPowers(Expr expr)
    {
        Expr current = expr;

        for (int i = 0; i < 100; i++)
        {
            Expr next = SimplifyOncePowers(current);

            if (next.Equals(current))
                return next;

            current = next;
        }

        throw new InvalidOperationException("Simplification did not converge after 100 iterations — possible rule cycle.");
    }

    private static Expr SimplifyOncePowers(Expr expr)
    {
        Expr simplified = expr switch
        {
            Add(var l, var r) => new Add(SimplifyPowers(l), SimplifyPowers(r)),
            Subtract(var l, var r) => new Subtract(SimplifyPowers(l), SimplifyPowers(r)),
            Multiply(var l, var r) => new Multiply(SimplifyPowers(l), SimplifyPowers(r)),
            Divide(var n, var d) => new Divide(SimplifyPowers(n), SimplifyPowers(d)),
            Power(var b, var e) => new Power(SimplifyPowers(b), SimplifyPowers(e)),
            Negate(var a) => new Negate(SimplifyPowers(a)),

            Sin(var a) => new Sin(SimplifyPowers(a)),
            Cos(var a) => new Cos(SimplifyPowers(a)),
            Tan(var a) => new Tan(SimplifyPowers(a)),
            Cot(var a) => new Cot(SimplifyPowers(a)),
            Sec(var a) => new Sec(SimplifyPowers(a)),
            Csc(var a) => new Csc(SimplifyPowers(a)),
            Asin(var a) => new Asin(SimplifyPowers(a)),
            Acos(var a) => new Acos(SimplifyPowers(a)),
            Atan(var a) => new Atan(SimplifyPowers(a)),
            Sinh(var a) => new Sinh(SimplifyPowers(a)),
            Cosh(var a) => new Cosh(SimplifyPowers(a)),
            Tanh(var a) => new Tanh(SimplifyPowers(a)),
            Asinh(var a) => new Asinh(SimplifyPowers(a)),
            Acosh(var a) => new Acosh(SimplifyPowers(a)),
            Atanh(var a) => new Atanh(SimplifyPowers(a)),
            Coth(var a) => new Coth(SimplifyPowers(a)),
            Sech(var a) => new Sech(SimplifyPowers(a)),
            Csch(var a) => new Csch(SimplifyPowers(a)),
            Abs(var a) => new Abs(SimplifyPowers(a)),
            Sign(var a) => new Sign(SimplifyPowers(a)),
            Floor(var a) => new Floor(SimplifyPowers(a)),
            Ceiling(var a) => new Ceiling(SimplifyPowers(a)),
            Round(var a) => new Round(SimplifyPowers(a)),
            Min(var l, var r) => new Min(SimplifyPowers(l), SimplifyPowers(r)),
            Max(var l, var r) => new Max(SimplifyPowers(l), SimplifyPowers(r)),

            Sqrt(var a) => new Power(SimplifyPowers(a), new Divide(new Constant(1), new Constant(2))),
            NthRoot(var a, var n) => new Power(SimplifyPowers(a), new Divide(new Constant(1), SimplifyPowers(n))),

            _ => expr
        };

        return ApplyRules(simplified).Canonicalize();
    }

    private static Expr ToPowers(Expr expr) => expr switch
    {
        Sqrt(var a) => new Power(ToPowers(a), new Divide(new Constant(1), new Constant(2))),
        NthRoot(var a, var n) => new Power(ToPowers(a), new Divide(new Constant(1), ToPowers(n))),

        Add(var l, var r) => new Add(ToPowers(l), ToPowers(r)),
        Subtract(var l, var r) => new Subtract(ToPowers(l), ToPowers(r)),
        Multiply(var l, var r) => new Multiply(ToPowers(l), ToPowers(r)),
        Divide(var n, var d) => new Divide(ToPowers(n), ToPowers(d)),
        Power(var b, var e) => new Power(ToPowers(b), ToPowers(e)),
        Negate(var a) => new Negate(ToPowers(a)),

        Sin(var a) => new Sin(ToPowers(a)),
        Cos(var a) => new Cos(ToPowers(a)),
        Tan(var a) => new Tan(ToPowers(a)),
        Cot(var a) => new Cot(ToPowers(a)),
        Sec(var a) => new Sec(ToPowers(a)),
        Csc(var a) => new Csc(ToPowers(a)),
        Asin(var a) => new Asin(ToPowers(a)),
        Acos(var a) => new Acos(ToPowers(a)),
        Atan(var a) => new Atan(ToPowers(a)),
        Sinh(var a) => new Sinh(ToPowers(a)),
        Cosh(var a) => new Cosh(ToPowers(a)),
        Tanh(var a) => new Tanh(ToPowers(a)),
        Asinh(var a) => new Asinh(ToPowers(a)),
        Acosh(var a) => new Acosh(ToPowers(a)),
        Atanh(var a) => new Atanh(ToPowers(a)),
        Coth(var a) => new Coth(ToPowers(a)),
        Sech(var a) => new Sech(ToPowers(a)),
        Csch(var a) => new Csch(ToPowers(a)),
        Abs(var a) => new Abs(ToPowers(a)),
        Sign(var a) => new Sign(ToPowers(a)),
        Floor(var a) => new Floor(ToPowers(a)),
        Ceiling(var a) => new Ceiling(ToPowers(a)),
        Round(var a) => new Round(ToPowers(a)),
        Min(var l, var r) => new Min(ToPowers(l), ToPowers(r)),
        Max(var l, var r) => new Max(ToPowers(l), ToPowers(r)),

        _ => expr
    };

    private static Expr PreferRoots(Expr expr) => expr switch
    {
        Power(var b, Constant e) when e.Value == new Rational(1, 2) =>
            new Sqrt(PreferRoots(b)),

        Power(var b, Constant e) when e.Value == new Rational(-1, 2) =>
            new Divide(new Constant(1), new Sqrt(PreferRoots(b))),

        Power(var b, Divide(Constant one, Constant two)) when one.Value.IsOne && two.Value == 2 =>
            new Sqrt(PreferRoots(b)),

        Power(var b, Divide(Constant one, Constant n)) when one.Value.IsOne && n.Value.IsInteger && n.Value.Sign > 0 =>
            new NthRoot(PreferRoots(b), n),

        Add(var l, var r) => new Add(PreferRoots(l), PreferRoots(r)),
        Subtract(var l, var r) => new Subtract(PreferRoots(l), PreferRoots(r)),
        Multiply(var l, var r) => new Multiply(PreferRoots(l), PreferRoots(r)),
        Divide(var n, var d) => new Divide(PreferRoots(n), PreferRoots(d)),
        Power(var b, var e) => new Power(PreferRoots(b), PreferRoots(e)),
        Negate(var a) => new Negate(PreferRoots(a)),

        Sin(var a) => new Sin(PreferRoots(a)),
        Cos(var a) => new Cos(PreferRoots(a)),
        Tan(var a) => new Tan(PreferRoots(a)),
        Cot(var a) => new Cot(PreferRoots(a)),
        Sec(var a) => new Sec(PreferRoots(a)),
        Csc(var a) => new Csc(PreferRoots(a)),
        Asin(var a) => new Asin(PreferRoots(a)),
        Acos(var a) => new Acos(PreferRoots(a)),
        Atan(var a) => new Atan(PreferRoots(a)),
        Sinh(var a) => new Sinh(PreferRoots(a)),
        Cosh(var a) => new Cosh(PreferRoots(a)),
        Tanh(var a) => new Tanh(PreferRoots(a)),
        Asinh(var a) => new Asinh(PreferRoots(a)),
        Acosh(var a) => new Acosh(PreferRoots(a)),
        Atanh(var a) => new Atanh(PreferRoots(a)),
        Coth(var a) => new Coth(PreferRoots(a)),
        Sech(var a) => new Sech(PreferRoots(a)),
        Csch(var a) => new Csch(PreferRoots(a)),
        Abs(var a) => new Abs(PreferRoots(a)),
        Sign(var a) => new Sign(PreferRoots(a)),
        Floor(var a) => new Floor(PreferRoots(a)),
        Ceiling(var a) => new Ceiling(PreferRoots(a)),
        Round(var a) => new Round(PreferRoots(a)),
        Min(var l, var r) => new Min(PreferRoots(l), PreferRoots(r)),
        Max(var l, var r) => new Max(PreferRoots(l), PreferRoots(r)),
        Sqrt(var a) => new Sqrt(PreferRoots(a)),
        NthRoot(var a, var n) => new NthRoot(PreferRoots(a), PreferRoots(n)),

        _ => expr
    };

    private static Expr ApplyRules(Expr expr)
    {
        Expr flattened = FlattenAndCombine(expr);
        if (!flattened.Equals(expr))
            return flattened.Canonicalize();

        switch (expr)
        {
            case Add(Constant a, Constant b):
                return new Constant(a.Value + b.Value);

            case Subtract(Constant a, Constant b):
                return new Constant(a.Value - b.Value);

            case Multiply(Constant a, Constant b):
                return new Constant(a.Value * b.Value);

            case Divide(Constant a, Constant b) when b.Value != 0:
                return new Constant(a.Value / b.Value);

            // Constant +- (p/q)
            case Add(Constant a, Divide(Constant b, Constant c)) when c.Value != 0:
                return new Constant(a.Value + b.Value / c.Value);

            case Add(Divide(Constant a, Constant b), Constant c) when b.Value != 0:
                return new Constant(a.Value / b.Value + c.Value);

            case Subtract(Constant a, Divide(Constant b, Constant c)) when c.Value != 0:
                return new Constant(a.Value - b.Value / c.Value);

            case Subtract(Divide(Constant a, Constant b), Constant c) when b.Value != 0:
                return new Constant(a.Value / b.Value - c.Value);

            // (p/q) +- (r/s)
            case Add(Divide(Constant a, Constant b), Divide(Constant c, Constant d))
                when b.Value != 0 && d.Value != 0:
                return new Constant((a.Value * d.Value + c.Value * b.Value) / (b.Value * d.Value));

            case Subtract(Divide(Constant a, Constant b), Divide(Constant c, Constant d))
                when b.Value != 0 && d.Value != 0:
                return new Constant((a.Value * d.Value - c.Value * b.Value) / (b.Value * d.Value));

            case Negate(Constant c):
                return new Constant(-c.Value);

            case Negate(Divide(Constant a, Constant b)) when b.Value != 0:
                return new Constant(-a.Value / b.Value);

            case Add(var l, var r) when r.Equals(new Constant(0)):
                return l;

            case Add(var l, var r) when l.Equals(new Constant(0)):
                return r;

            case Subtract(var l, var r) when l.Equals(r):
                return new Constant(0);

            case Subtract(Constant zero, var x) when zero.Value == 0:
                return new Negate(x);

            case Negate(Negate(var a)):
                return a;

            case Subtract(var a, Negate(var b)):
                return new Add(a, b);

            case Subtract(var l, var r) when r.Equals(new Constant(0)):
                return l;

            case Multiply(var l, var r) when l.Equals(new Constant(0)) || r.Equals(new Constant(0)):
                return new Constant(0);

            case Multiply(Constant one, var r) when one.Value == 1:
                return r;

            case Multiply(var l, var r) when r.Equals(new Constant(1)):
                return l;

            case Divide(Constant zero, var d) when zero.Value == 0:
                return new Constant(0);

            case Divide(var n, var d) when n.Equals(d):
                return new Constant(1);

            case Divide(var n, var d) when d.Equals(new Constant(1)):
                return n;

            case Power(Constant b, Constant e) when e.Value.IsInteger && !(b.Value.IsZero && e.Value.Sign < 0):
                return new Constant(b.Value.Pow((int)e.Value.Numerator));

            case Power(Constant b, Constant e)
                when BigInteger.Abs(e.Value.Numerator) == 1 &&
                     TryExactRoot(b.Value, e.Value.Denominator, out Rational rootValue):
                return e.Value.Numerator.Sign > 0
                    ? new Constant(rootValue)
                    : new Constant(Rational.One / rootValue);

            case Power(var b, var e) when e.Equals(new Constant(0)):
                return new Constant(1);

            case Power(var b, var e) when e.Equals(new Constant(1)):
                return b;

            case Power(var b, var e) when b.Equals(new Constant(0)):
                return new Constant(0);

            case Power(Power(var b, var e1), var e2):
                return new Power(b, new Multiply(e1, e2));

            case Multiply(Power(var b1, var e1), Power(var b2, var e2)) when b1.Equals(b2):
                return new Power(b1, new Add(e1, e2));

            case Multiply(var b, Power(var b2, var e)) when b.Equals(b2):
                return new Power(b, new Add(e, new Constant(1)));

            case Multiply(Power(var b, var e), var b2) when b.Equals(b2):
                return new Power(b, new Add(e, new Constant(1)));

            case Multiply(var b1, var b2) when b1.Equals(b2) && b1 is not Constant:
                return new Power(b1, new Constant(2));

            case Multiply(Divide(var a, var b), Divide(var c, var d)):
                return new Divide(new Multiply(a, c), new Multiply(b, d));

            case Multiply(Divide(var a, var b), var c) when c is not Divide:
                return new Divide(new Multiply(a, c), b);

            case Multiply(var c, Divide(var a, var b)) when c is not Divide:
                return new Divide(new Multiply(c, a), b);

            case Divide(Power(var b1, Constant e), Multiply(Constant c, var b2)) when b1.Equals(b2):
                return new Divide(new Power(b1, new Constant(e.Value - 1)), c);

            case Divide(Power(var b1, Constant e), Multiply(var b2, Constant c)) when b1.Equals(b2):
                return new Divide(new Power(b1, new Constant(e.Value - 1)), c);

            case Divide(Divide(var a, var b), var c):
                return new Divide(a, new Multiply(b, c));

            // x^n / x^m = x^(n - m)
            case Divide(Power(var b1, var e1), Power(var b2, var e2)) when b1.Equals(b2):
                return new Power(b1, new Subtract(e1, e2));

            // x^n / x = x^(n - 1)
            case Divide(Power(var b1, var e1), var b2) when b1.Equals(b2):
                return new Power(b1, new Subtract(e1, new Constant(1)));

            // (x^n * c) / x = c * x^(n - 1)
            case Divide(Multiply(Power(var b1, var e1), var c), var b2) when b1.Equals(b2):
                return new Multiply(c, new Power(b1, new Subtract(e1, new Constant(1))));

            // (c * x^n) / x = c * x^(n - 1)
            case Divide(Multiply(var c, Power(var b1, var e1)), var b2) when b1.Equals(b2):
                return new Multiply(c, new Power(b1, new Subtract(e1, new Constant(1))));

            // x / x^n = x^(1 - n)
            case Divide(var b1, Power(var b2, var e2)) when b1.Equals(b2):
                return new Power(b1, new Subtract(new Constant(1), e2));

            case Sin(Constant c) when c.Value == 0:
                return new Constant(0);

            case Cos(Constant c) when c.Value == 0:
                return new Constant(1);

            case Tan(Constant c) when c.Value == 0:
                return new Constant(0);

            case Add(
                Power(Cos(var x1), Constant e1),
                Power(Sin(var x2), Constant e2))
                when e1.Value == 2 && e2.Value == 2 && x1.Equals(x2):
                return new Constant(1);

            case Ln(Exp(var a)):
                return a;

            case Exp(Ln(var a)):
                return a;

            case Divide(Sin(var x), Cos(var y)) when x.Equals(y):
                return new Tan(x);

            case Divide(Cos(var x), Sin(var y)) when x.Equals(y):
                return new Cot(x);

            case Multiply(Tan(var x), Cot(var y)) when x.Equals(y):
                return new Constant(1);

            case Multiply(Cot(var x), Tan(var y)) when x.Equals(y):
                return new Constant(1);

            case Divide(
                Power(Sin(var x1), Constant e1),
                Power(Cos(var x2), Constant e2))
                when e1.Value == 2 && e2.Value == 2 && x1.Equals(x2):
                return new Power(new Tan(x1), new Constant(2));

            case Divide(
                Power(Cos(var x1), Constant e1),
                Power(Sin(var x2), Constant e2))
                when e1.Value == 2 && e2.Value == 2 && x1.Equals(x2):
                return new Power(new Cot(x1), new Constant(2));

            case Subtract(
                Constant c,
                Power(Sin(var x), Constant e))
                when c.Value == 1 && e.Value == 2:
                return new Power(new Cos(x), new Constant(2));

            case Subtract(
                Constant c,
                Power(Cos(var x), Constant e))
                when c.Value == 1 && e.Value == 2:
                return new Power(new Sin(x), new Constant(2));

            case Subtract(
                Power(Sec(var x1), Constant e1),
                Power(Tan(var x2), Constant e2))
                when e1.Value == 2 && e2.Value == 2 && x1.Equals(x2):
                return new Constant(1);

            case Subtract(
                Power(Csc(var x1), Constant e1),
                Power(Cot(var x2), Constant e2))
                when e1.Value == 2 && e2.Value == 2 && x1.Equals(x2):
                return new Constant(1);

            case Abs(Constant c):
                return new Constant(c.Value.Abs());

            case Abs(var a) when a is Abs:
                return a;

            case Sign(Constant c):
                return new Constant(c.Value.Sign);

            case Floor(Constant c):
                return new Constant(c.Value.Floor());

            case Ceiling(Constant c):
                return new Constant(c.Value.Ceiling());

            case Round(Constant c):
                return new Constant(c.Value.Round());

            case Min(Constant a, Constant b):
                return new Constant(a.Value < b.Value ? a.Value : b.Value);

            case Max(Constant a, Constant b):
                return new Constant(a.Value > b.Value ? a.Value : b.Value);

            default:
                return expr;
        }
    }

    private static bool TryIntegerNthRoot(BigInteger value, int n, out BigInteger root)
    {
        root = BigInteger.Zero;
        if (value.Sign < 0 || n <= 0) return false;
        if (value.IsZero) return true;
        if (n == 1) { root = value; return true; }

        BigInteger low = 0, high = value;
        while (low <= high)
        {
            BigInteger mid = (low + high) / 2;
            BigInteger midPow = BigInteger.Pow(mid, n);

            if (midPow == value) { root = mid; return true; }
            if (midPow < value) low = mid + 1;
            else high = mid - 1;
        }

        return false;
    }

    private static bool TryExactRoot(Rational value, BigInteger n, out Rational root)
    {
        root = default;

        if (n <= 0) return false;
        int nn;
        try { nn = (int)n; }
        catch (OverflowException) { return false; }

        bool negative = value.Sign < 0;
        if (negative && nn % 2 == 0) return false;

        if (!TryIntegerNthRoot(BigInteger.Abs(value.Numerator), nn, out BigInteger numRoot)) return false;
        if (!TryIntegerNthRoot(value.Denominator, nn, out BigInteger denRoot)) return false;

        Rational result = new Rational(numRoot, denRoot);
        root = negative ? -result : result;
        return true;
    }

    private static (Rational Coefficient, Expr Term) ExtractCoefficient(Expr expr) => expr switch
    {
        Negate(var t) => (Rational.MinusOne, t),
        Multiply(Constant c, var t) => (c.Value, t),
        Multiply(var t, Constant c) => (c.Value, t),
        _ => (Rational.One, expr)
    };

    private static void CollectTerms(Expr expr, Rational sign, List<(Rational Coefficient, Expr Term)> terms)
    {
        switch (expr)
        {
            case Add(var l, var r):
                CollectTerms(l, sign, terms);
                CollectTerms(r, sign, terms);
                break;
            case Subtract(var l, var r):
                CollectTerms(l, sign, terms);
                CollectTerms(r, -sign, terms);
                break;
            default:
                var (coef, term) = ExtractCoefficient(expr);
                terms.Add((coef * sign, term));
                break;
        }
    }

    private static Expr FlattenAndCombine(Expr expr)
    {
        if (expr is not (Add or Subtract))
            return expr;

        var raw = new List<(Rational Coefficient, Expr Term)>();
        CollectTerms(expr, Rational.One, raw);

        Rational constantSum = Rational.Zero;
        var combined = new List<(Rational Coefficient, Expr Term)>();

        foreach (var (coef, term) in raw)
        {
            if (term is Constant c)
            {
                constantSum += coef * c.Value;
                continue;
            }

            int existingIndex = combined.FindIndex(t => t.Term.Equals(term));
            if (existingIndex >= 0)
            {
                var (existingCoef, existingTerm) = combined[existingIndex];
                combined[existingIndex] = (existingCoef + coef, existingTerm);
            }
            else
            {
                combined.Add((coef, term));
            }
        }

        combined.RemoveAll(t => t.Coefficient.IsZero);

        Expr Rebuild(Rational coef, Expr term) =>
            coef.IsOne ? term :
            coef.Equals(Rational.MinusOne) ? new Negate(term) :
            new Multiply(new Constant(coef), term);

        if (combined.Count == 0)
            return new Constant(constantSum);

        Expr result = Rebuild(combined[0].Coefficient, combined[0].Term);
        for (int i = 1; i < combined.Count; i++)
        {
            var (coef, term) = combined[i];
            result = coef.Sign < 0
                ? new Subtract(result, Rebuild(-coef, term))
                : new Add(result, Rebuild(coef, term));
        }

        if (!constantSum.IsZero)
        {
            result = constantSum.Sign < 0
                ? new Subtract(result, new Constant(-constantSum))
                : new Add(result, new Constant(constantSum));
        }

        return result;
    }
}