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
        Expr recursed = Rewrite(expr, static _ => null, SimplifyPowers);
        return ApplyRules(recursed).Canonicalize();
    }

    private static Expr Rewrite(Expr expr, Func<Expr, Expr?> leafRule, Func<Expr, Expr>? postProcessChild = null)
    {
        Expr? replaced = leafRule(expr);
        if (replaced is not null)
            return Rewrite(replaced, leafRule, postProcessChild);

        Func<Expr, Expr> recurse = postProcessChild ?? (child => Rewrite(child, leafRule));

        return expr switch
        {
            Add(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Add(a, b)),
            Subtract(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Subtract(a, b)),
            Multiply(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Multiply(a, b)),
            Divide(var n, var d) => RebuildBinary(expr, n, d, recurse, static (a, b) => new Divide(a, b)),
            Power(var b, var e) => RebuildBinary(expr, b, e, recurse, static (a, b) => new Power(a, b)),
            Negate(var a) => RebuildUnary(expr, a, recurse, static x => new Negate(x)),

            Sin(var a) => RebuildUnary(expr, a, recurse, static x => new Sin(x)),
            Cos(var a) => RebuildUnary(expr, a, recurse, static x => new Cos(x)),
            Tan(var a) => RebuildUnary(expr, a, recurse, static x => new Tan(x)),
            Cot(var a) => RebuildUnary(expr, a, recurse, static x => new Cot(x)),
            Sec(var a) => RebuildUnary(expr, a, recurse, static x => new Sec(x)),
            Csc(var a) => RebuildUnary(expr, a, recurse, static x => new Csc(x)),
            Asin(var a) => RebuildUnary(expr, a, recurse, static x => new Asin(x)),
            Acos(var a) => RebuildUnary(expr, a, recurse, static x => new Acos(x)),
            Atan(var a) => RebuildUnary(expr, a, recurse, static x => new Atan(x)),
            Sinh(var a) => RebuildUnary(expr, a, recurse, static x => new Sinh(x)),
            Cosh(var a) => RebuildUnary(expr, a, recurse, static x => new Cosh(x)),
            Tanh(var a) => RebuildUnary(expr, a, recurse, static x => new Tanh(x)),
            Asinh(var a) => RebuildUnary(expr, a, recurse, static x => new Asinh(x)),
            Acosh(var a) => RebuildUnary(expr, a, recurse, static x => new Acosh(x)),
            Atanh(var a) => RebuildUnary(expr, a, recurse, static x => new Atanh(x)),
            Coth(var a) => RebuildUnary(expr, a, recurse, static x => new Coth(x)),
            Sech(var a) => RebuildUnary(expr, a, recurse, static x => new Sech(x)),
            Csch(var a) => RebuildUnary(expr, a, recurse, static x => new Csch(x)),
            Exp(var a) => RebuildUnary(expr, a, recurse, static x => new Exp(x)),
            Ln(var a) => RebuildUnary(expr, a, recurse, static x => new Ln(x)),
            Sqrt(var a) => RebuildUnary(expr, a, recurse, static x => new Sqrt(x)),
            Abs(var a) => RebuildUnary(expr, a, recurse, static x => new Abs(x)),
            Sign(var a) => RebuildUnary(expr, a, recurse, static x => new Sign(x)),
            Floor(var a) => RebuildUnary(expr, a, recurse, static x => new Floor(x)),
            Ceiling(var a) => RebuildUnary(expr, a, recurse, static x => new Ceiling(x)),
            Round(var a) => RebuildUnary(expr, a, recurse, static x => new Round(x)),

            Min(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Min(a, b)),
            Max(var l, var r) => RebuildBinary(expr, l, r, recurse, static (a, b) => new Max(a, b)),
            NthRoot(var a, var n) => RebuildBinary(expr, a, n, recurse, static (a2, b) => new NthRoot(a2, b)),

            // Constant, Variable, Pi, E, ImaginaryUnit — leaves, nothing to recurse into
            _ => expr
        };
    }

    private static Expr RebuildUnary(Expr original, Expr child, Func<Expr, Expr> recurse, Func<Expr, Expr> build)
    {
        Expr newChild = recurse(child);
        return ReferenceEquals(newChild, child) ? original : build(newChild);
    }

    private static Expr RebuildBinary(Expr original, Expr left, Expr right, Func<Expr, Expr> recurse, Func<Expr, Expr, Expr> build)
    {
        Expr newLeft = recurse(left);
        Expr newRight = recurse(right);
        return ReferenceEquals(newLeft, left) && ReferenceEquals(newRight, right)
            ? original
            : build(newLeft, newRight);
    }

    // Rewrites Sqrt/NthRoot into an equivalent Power, so the general
    // Power-combination rules in ApplyRules can operate on them uniformly.
    private static Expr ToPowers(Expr expr) => Rewrite(expr, static e => e switch
    {
        Sqrt(var a) => new Power(a, new Divide(new Constant(1), new Constant(2))),
        NthRoot(var a, var n) => new Power(a, new Divide(new Constant(1), n)),
        _ => null
    });

    // Rewrites Power nodes with a root-like exponent (1/2, -1/2, 1/n) back into
    // Sqrt/NthRoot for display and further simplification — the inverse of ToPowers.
    private static Expr PreferRoots(Expr expr) => Rewrite(expr, static e => e switch
    {
        Power(var b, Constant exp) when exp.Value == new Rational(1, 2) =>
            new Sqrt(b),

        Power(var b, Constant exp) when exp.Value == new Rational(-1, 2) =>
            new Divide(new Constant(1), new Sqrt(b)),

        Power(var b, Divide(Constant one, Constant two)) when one.Value.IsOne && two.Value == 2 =>
            new Sqrt(b),

        Power(var b, Divide(Constant one, Constant n)) when one.Value.IsOne && n.Value.IsInteger && n.Value.Sign > 0 =>
            new NthRoot(b, n),

        _ => null
    });

    // Direct pattern checks instead of allocating new Constant(0)/new Constant(1)
    // just to compare against them via Equals.
    private static bool IsZero(this Expr e) => e is Constant c && c.Value.IsZero;
    private static bool IsOne(this Expr e) => e is Constant c && c.Value.IsOne;

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

            case Add(var l, var r) when r.IsZero():
                return l;

            case Add(var l, var r) when l.IsZero():
                return r;

            case Subtract(var l, var r) when l.Equals(r):
                return new Constant(0);

            case Subtract(Constant zero, var x) when zero.Value == 0:
                return new Negate(x);

            case Negate(Negate(var a)):
                return a;

            case Subtract(var a, Negate(var b)):
                return new Add(a, b);

            case Subtract(var l, var r) when r.IsZero():
                return l;

            case Multiply(var l, var r) when l.IsZero() || r.IsZero():
                return new Constant(0);

            case Multiply(Constant one, var r) when one.Value.IsOne:
                return r;

            case Multiply(var l, var r) when r.IsOne():
                return l;

            case Divide(Constant zero, var d) when zero.Value == 0:
                return new Constant(0);

            case Divide(var n, var d) when n.Equals(d):
                return new Constant(1);

            case Divide(var n, var d) when d.IsOne():
                return n;

            // Integer exponent — exact BigInteger power, no floating-point rounding
            case Power(Constant b, Constant e) when e.Value.IsInteger && !(b.Value.IsZero && e.Value.Sign < 0):
                return new Constant(b.Value.Pow((int)e.Value.Numerator));

            // Root exponent (+-1/n) — exact result only if the base is a perfect
            // n-th power.
            case Power(Constant b, Constant e)
                when BigInteger.Abs(e.Value.Numerator) == 1 &&
                     TryExactRoot(b.Value, e.Value.Denominator, out Rational rootValue):
                return e.Value.Numerator.Sign > 0
                    ? new Constant(rootValue)
                    : new Constant(Rational.One / rootValue);

            case Power(var b, var e) when e.IsZero():
                return new Constant(1);

            case Power(var b, var e) when e.IsOne():
                return b;

            case Power(var b, var e) when b.IsZero():
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

    // Exact integer n-th root via binary search on BigInteger.
    // Returns false if `value` is not a perfect n-th power.
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

    // Exact n-th root of a rational number: numerator and denominator (coprime
    // by Rational's construction) must each be a perfect n-th power.
    private static bool TryExactRoot(Rational value, BigInteger n, out Rational root)
    {
        root = default;

        if (n <= 0) return false;
        int nn;
        try { nn = (int)n; }
        catch (OverflowException) { return false; }

        bool negative = value.Sign < 0;
        if (negative && nn % 2 == 0) return false; // even root of a negative number isn't real

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

    // Combines like terms across an Add/Subtract chain. Uses a Dictionary keyed
    // by the term's structural hash for O(1) average lookup instead of a linear List
    private static Expr FlattenAndCombine(Expr expr)
    {
        if (expr is not (Add or Subtract))
            return expr;

        var raw = new List<(Rational Coefficient, Expr Term)>();
        CollectTerms(expr, Rational.One, raw);

        Rational constantSum = Rational.Zero;
        var combined = new List<(Rational Coefficient, Expr Term)>();
        var termIndex = new Dictionary<Expr, int>();

        foreach (var (coef, term) in raw)
        {
            if (term is Constant c)
            {
                constantSum += coef * c.Value;
                continue;
            }

            if (termIndex.TryGetValue(term, out int existingIndex))
            {
                var (existingCoef, existingTerm) = combined[existingIndex];
                combined[existingIndex] = (existingCoef + coef, existingTerm);
            }
            else
            {
                termIndex[term] = combined.Count;
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