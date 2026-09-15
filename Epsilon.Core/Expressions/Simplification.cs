namespace Epsilon.Core;

public static class Simplifier
{
    public static Expr Simplify(this Expr expr) => expr.Simplify(Assumptions.None);

    public static Expr Simplify(this Expr expr, Assumptions assumptions)
    {
        Expr current = expr.Canonicalize();

        for (int i = 0; i < 100; i++)
        {
            Expr next = SimplifyOnce(current, assumptions);

            if (next.Equals(current))
                return next;

            current = next;
        }

        throw new InvalidOperationException("Simplification did not converge after 100 iterations — possible rule cycle.");
    }

    private static Expr SimplifyOnce(Expr expr, Assumptions assumptions)
    {
        Expr simplifiedChildren = TreeRewriter.RewriteChildren(expr, child => child.Simplify(assumptions));
        return ApplyRules(simplifiedChildren, assumptions).Canonicalize();
    }

    private static Expr ApplyRules(Expr expr, Assumptions assumptions)
    {

        Expr flattened = FlattenAndCombine(expr);
        if (!flattened.Equals(expr))
            return flattened.Canonicalize();

        switch (expr)
        {
            case Add(Constant a, Constant b):
                return new Constant(a.Value + b.Value);

            case Add(var l, var r) when r.Equals(new Constant(0)):
                return l;

            case Add(var l, var r) when l.Equals(new Constant(0)):
                return r;

            case Subtract(Constant a, Constant b):
                return new Constant(a.Value - b.Value);

            case Subtract(var l, var r) when l.Equals(r):
                return new Constant(0);

            case Subtract(Constant zero, var x) when zero.Value.IsZero:
                return new Negate(x);

            case Negate(Constant c):
                return new Constant(-c.Value);

            case Negate(Negate(var a)):
                return a;

            // a - (-b) = a + b
            case Subtract(var a, Negate(var b)):
                return new Add(a, b);

            case Subtract(var l, var r) when r.Equals(new Constant(0)):
                return l;

            case Multiply(Constant a, Constant b):
                return new Constant(a.Value * b.Value);

            case Multiply(var l, var r) when l.Equals(new Constant(0)) || r.Equals(new Constant(0)):
                return new Constant(0);

            case Multiply(Constant one, var r) when one.Value.IsOne:
                return r;

            case Multiply(var l, var r) when r.Equals(new Constant(1)):
                return l;

            case Divide(Constant a, Constant b) when !b.Value.IsZero:
                return new Constant(a.Value / b.Value);

            // 0 / var = 0
            case Divide(Constant zero, var d) when zero.Value.IsZero:
                return new Constant(0);

            case Divide(var n, var d) when n.Equals(d) && d.IsProvablyNonZero(assumptions):
                return new Constant(1);
            case Divide(var n, var d) when d.Equals(new Constant(1)):
                return n;

            case Power(Constant b, Constant e) when e.Value.IsInteger:
                return new Constant(b.Value.Pow((int)e.Value.Numerator));

            case Power(Constant b, Constant e) when !e.Value.IsInteger && b.Value.Sign >= 0:
                return new Constant((Rational)Math.Pow(b.Value.ToDouble(), e.Value.ToDouble()));

            case Power(var b, var e) when e.Equals(new Constant(0)):
                return new Constant(1);

            case Power(var b, var e) when e.Equals(new Constant(1)):
                return b;

            case Power(var b, var e) when b.Equals(new Constant(0)) && e.IsProvablyPositive(assumptions):
                return new Constant(0);

            case Power(Power(var b, var e1), var e2):
                // Safe to collapse unconditionally only when e1 is an odd integer (sign-preserving:
                // x -> x^e1 never erases the sign of b, so composing exponents afterward can't lose it).
                Expr combined = new Power(b, new Multiply(e1, e2));
                if (e1 is Constant ce1 && ce1.Value.IsInteger && !((long)ce1.Value.Numerator % 2 == 0))
                    return combined;
                return b.IsProvablyNonNegative(assumptions) ? combined : expr;

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

            case Divide(Power(var b1, Constant e), Multiply(Constant c, var b2)) when b1.Equals(b2) && b1.IsProvablyNonZero(assumptions):
                return new Divide(new Power(b1, new Constant(e.Value - 1)), c);
            case Divide(Power(var b1, Constant e), Multiply(var b2, Constant c)) when b1.Equals(b2) && b1.IsProvablyNonZero(assumptions):
                return new Divide(new Power(b1, new Constant(e.Value - 1)), c);

            case Divide(Divide(var a, var b), var c):
                return new Divide(a, new Multiply(b, c));

            // x^n / x^m = x^(n-m)
            case Divide(Power(var b1, var e1), Power(var b2, var e2)) when b1.Equals(b2) && b1.IsProvablyNonZero(assumptions):
                return new Power(b1, new Subtract(e1, e2));

            // x^n / x = x^(n-1)
            case Divide(Power(var b1, var e1), var b2) when b1.Equals(b2) && b1.IsProvablyNonZero(assumptions):
                return new Power(b1, new Subtract(e1, new Constant(1)));

            // (x^n * c) / x = c * x^(n-1)
            case Divide(Multiply(Power(var b1, var e1), var c), var b2) when b1.Equals(b2) && b1.IsProvablyNonZero(assumptions):
                return new Multiply(c, new Power(b1, new Subtract(e1, new Constant(1))));

            // (c * x^n) / x = c * x^(n-1)
            case Divide(Multiply(var c, Power(var b1, var e1)), var b2) when b1.Equals(b2) && b1.IsProvablyNonZero(assumptions):
                return new Multiply(c, new Power(b1, new Subtract(e1, new Constant(1))));

            // x / x^n = x^(1-n)
            case Divide(var b1, Power(var b2, var e2)) when b1.Equals(b2) && b1.IsProvablyNonZero(assumptions):
                return new Power(b1, new Subtract(new Constant(1), e2));

            case Sin(Constant c) when c.Value.IsZero:
                return new Constant(0);

            case Cos(Constant c) when c.Value.IsZero:
                return new Constant(1);

            case Tan(Constant c) when c.Value.IsZero:
                return new Constant(0);

            // sin(x)^2 + cos(x)^2 = 1
            case Add(
                Power(Cos(var x1), Constant e1),
                Power(Sin(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Constant(1);

            case Ln(Exp(var a)):
                return a;
            case Exp(Ln(var a)):
                return a;

            // tan(x) = sin(x) / cos(x)
            case Divide(Sin(var x), Cos(var y)) when x.Equals(y):
                return new Tan(x);

            // cot(x) = cos(x) / sin(x)
            case Divide(Cos(var x), Sin(var y)) when x.Equals(y):
                return new Cot(x);

            // tan(x) * cot(x) = 1
            case Multiply(Tan(var x), Cot(var y)) when x.Equals(y):
                return new Constant(1);

            case Multiply(Cot(var x), Tan(var y)) when x.Equals(y):
                return new Constant(1);

            // sin(x)^2 / cos(x)^2 = tan(x)^2
            case Divide(
                Power(Sin(var x1), Constant e1),
                Power(Cos(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Power(new Tan(x1), new Constant(2));

            // cos(x)^2 / sin(x)^2 = cot(x)^2
            case Divide(
                Power(Cos(var x1), Constant e1),
                Power(Sin(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Power(new Cot(x1), new Constant(2));

            // 1 - sin(x)^2 = cos(x)^2
            case Subtract(
                Constant c,
                Power(Sin(var x), Constant e))
                when c.Value == 1 && e.Value == 2:
                return new Power(new Cos(x), new Constant(2));

            // 1 - cos(x)^2 = sin(x)^2
            case Subtract(
                Constant c,
                Power(Cos(var x), Constant e))
                when c.Value == 1 && e.Value == 2:
                return new Power(new Sin(x), new Constant(2));

            // sec(x)^2 - tan(x)^2 = 1
            case Subtract(
                Power(Sec(var x1), Constant e1),
                Power(Tan(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Constant(1);

            // csc(x)^2 - cot(x)^2 = 1
            case Subtract(
                Power(Csc(var x1), Constant e1),
                Power(Cot(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Constant(1);

            case Sqrt(Constant c) when c.Value.Sign >= 0:
                return new Constant((Rational)Math.Sqrt(c.Value.ToDouble()));

            case Sqrt(Power(var b, Constant e)) when e.Value == 2:
                return b.IsProvablyNonNegative(assumptions)
                    ? b
                    : new Abs(b);

            case NthRoot(Constant c, Constant n) when c.Value.Sign >= 0:
                return new Constant((Rational)Math.Pow(c.Value.ToDouble(), 1.0 / n.Value.ToDouble()));

            case NthRoot(Constant c, Constant n) when c.Value.Sign < 0 && n.Value.IsInteger && IsOddInteger(n.Value.ToDouble()):
                return new Constant((Rational)(-Math.Pow(-c.Value.ToDouble(), 1.0 / n.Value.ToDouble())));

            case Abs(Constant c):
                return new Constant(c.Value.Abs());

            case Abs(var a) when a is Abs:
                return a;

            case Abs(var a) when a.IsProvablyNonNegative(assumptions):
                return a;

            case Abs(var a) when a.IsProvablyNegative(assumptions):
                return new Negate(a);

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

    private static bool IsOddInteger(double value) =>
        value == Math.Floor(value) && (long)value % 2 != 0;

    private static (Rational Coefficient, Expr Term) ExtractCoefficient(Expr expr) => expr switch
    {
        Negate(var t) => (Rational.MinusOne, t),
        Multiply(Constant c, var t) => (c.Value, t),
        Multiply(var t, Constant c) => (c.Value, t),
        Divide(var t, Constant c) when !c.Value.IsZero => (Rational.One / c.Value, t),
        _ => (Rational.One, expr)
    };

    // Flattens a chain of Add/Subtract into a flat list of (coefficient, term) pairs.
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

    // Combines like terms across an entire Add/Subtract chain, then rebuilds it.
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