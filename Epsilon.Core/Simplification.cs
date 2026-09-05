namespace Epsilon.Core;

public static class Simplifier
{
    public static Expr Simplify(this Expr expr)
    {
        Expr current = expr.Canonicalize();

        for (int i = 0; i < 100; i++)
        {
            Expr next = SimplifyOnce(current);

            if (next.Equals(current))
                return next;

            current = next;
        }

        throw new InvalidOperationException("Simplification did not converge after 100 iterations — possible rule cycle.");
    }

    private static Expr SimplifyOnce(Expr expr)
    {
        Expr simplified = expr switch
        {
            Add(var l, var r) => new Add(l.Simplify(), r.Simplify()),
            Subtract(var l, var r) => new Subtract(l.Simplify(), r.Simplify()),
            Multiply(var l, var r) => new Multiply(l.Simplify(), r.Simplify()),
            Divide(var n, var d) => new Divide(n.Simplify(), d.Simplify()),
            Power(var b, var e) => new Power(b.Simplify(), e.Simplify()),
            Sin(var a) => new Sin(a.Simplify()),
            Cos(var a) => new Cos(a.Simplify()),
            Tan(var a) => new Tan(a.Simplify()),
            Cot(var a) => new Cot(a.Simplify()),
            Sec(var a) => new Sec(a.Simplify()),
            Csc(var a) => new Csc(a.Simplify()),
            Asin(var a) => new Asin(a.Simplify()),
            Acos(var a) => new Acos(a.Simplify()),
            Atan(var a) => new Atan(a.Simplify()),
            Sinh(var a) => new Sinh(a.Simplify()),
            Cosh(var a) => new Cosh(a.Simplify()),
            Tanh(var a) => new Tanh(a.Simplify()),
            Sqrt(var a) => new Sqrt(a.Simplify()),
            NthRoot(var a, var n) => new NthRoot(a.Simplify(), n.Simplify()),
            _ => expr
        };

        return ApplyRules(simplified).Canonicalize();
    }

    private static Expr ApplyRules(Expr expr)
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

            case Subtract(Constant a, Constant b):
                return new Constant(a.Value - b.Value);

            case Subtract(var l, var r) when l.Equals(r):
                return new Constant(0);

            case Subtract(Constant zero, var x) when zero.Value == 0:
                return new Multiply(new Constant(-1), x).Simplify();

            // a - (-1 * b) = a + b   (double negation via subtraction)
            case Subtract(var a, Multiply(Constant c, var b)) when c.Value == -1:
                return new Add(a, b).Simplify();
            case Subtract(var a, Multiply(var b, Constant c)) when c.Value == -1:
                return new Add(a, b).Simplify();

            case Subtract(var l, var r) when r.Equals(new Constant(0)):
                return l;

            case Multiply(Constant a, Constant b):
                return new Constant(a.Value * b.Value);

            case Multiply(var l, var r) when l.Equals(new Constant(0)) || r.Equals(new Constant(0)):
                return new Constant(0);
            case Multiply(var l, var r) when r.Equals(new Constant(1)):
                return l;

            case Divide(Constant a, Constant b) when b.Value != 0:
                return new Constant(a.Value / b.Value);

            case Divide(var n, var d) when n.Equals(d):
                return new Constant(1);
            case Divide(var n, var d) when d.Equals(new Constant(1)):
                return n;

            case Power(Constant b, Constant e):
                return new Constant(Math.Pow(b.Value, e.Value));

            case Power(var b, var e) when e.Equals(new Constant(0)):
                return new Constant(1);

            case Power(var b, var e) when e.Equals(new Constant(1)):
                return b;

            case Power(var b, var e) when b.Equals(new Constant(0)):
                return new Constant(0);

            case Power(Power(var b, var e1), var e2):
                return new Power(b, new Multiply(e1, e2)).Simplify();

            case Multiply(Power(var b1, var e1), Power(var b2, var e2)) when b1.Equals(b2):
                return new Power(b1, new Add(e1, e2)).Simplify();

            case Multiply(var b, Power(var b2, var e)) when b.Equals(b2):
                return new Power(b, new Add(e, new Constant(1))).Simplify();

            case Multiply(Power(var b, var e), var b2) when b.Equals(b2):
                return new Power(b, new Add(e, new Constant(1))).Simplify();

            case Multiply(var b1, var b2) when b1.Equals(b2) && b1 is not Constant:
                return new Power(b1, new Constant(2)).Simplify();

            case Multiply(Divide(var a, var b), Divide(var c, var d)):
                return new Divide(new Multiply(a, c), new Multiply(b, d)).Simplify();

            case Multiply(Divide(var a, var b), var c) when c is not Divide:
                return new Divide(new Multiply(a, c), b).Simplify();

            case Multiply(var c, Divide(var a, var b)) when c is not Divide:
                return new Divide(new Multiply(c, a), b).Simplify();

            case Divide(Power(var b1, Constant e), Multiply(Constant c, var b2)) when b1.Equals(b2):
                return new Divide(new Power(b1, new Constant(e.Value - 1)), c).Simplify();
            case Divide(Power(var b1, Constant e), Multiply(var b2, Constant c)) when b1.Equals(b2):
                return new Divide(new Power(b1, new Constant(e.Value - 1)), c).Simplify();

            case Divide(Divide(var a, var b), var c):
                return new Divide(a, new Multiply(b, c)).Simplify();

            case Add(var l, var r) when
                !(l is Constant) && !(r is Constant) &&
                ExtractCoefficient(l).Term.Equals(ExtractCoefficient(r).Term):
                {
                    var (c1, term) = ExtractCoefficient(l);
                    var (c2, _) = ExtractCoefficient(r);
                    double sum = c1 + c2;
                    return sum == 1
                        ? term
                        : new Multiply(new Constant(sum), term).Simplify();
                }

            case Sin(Constant c) when c.Value == 0:
                return new Constant(0);

            case Cos(Constant c) when c.Value == 0:
                return new Constant(1);

            case Tan(Constant c) when c.Value == 0:
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
                return new Tan(x).Simplify();

            // cot(x) = cos(x) / sin(x)
            case Divide(Cos(var x), Sin(var y)) when x.Equals(y):
                return new Cot(x).Simplify();

            // tan(x) * cot(x) = 1
            case Multiply(Tan(var x), Cot(var y)) when x.Equals(y):
                return new Constant(1);

            case Multiply(Cot(var x), Tan(var y)) when x.Equals(y):
                return new Constant(1);

            // sin(x) / cos(x) = tan(x)
            case Divide(Sin(var x), Cos(var y)) when x.Equals(y):
                return new Tan(x);

            // cos(x) / sin(x) = cot(x)
            case Divide(Cos(var x), Sin(var y)) when x.Equals(y):
                return new Cot(x);

            // sin(x)^2 / cos(x)^2 = tan(x)^2
            case Divide(
                Power(Sin(var x1), Constant e1),
                Power(Cos(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Power(new Tan(x1), new Constant(2)).Simplify();

            // cos(x)^2 / sin(x)^2 = cot(x)^2
            case Divide(
                Power(Cos(var x1), Constant e1),
                Power(Sin(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Power(new Cot(x1), new Constant(2)).Simplify();

            // 1 - sin(x)^2 = cos(x)^2
            case Subtract(
                Constant c,
                Power(Sin(var x), Constant e))
                when c.Value == 1 && e.Value == 2:
                return new Power(new Cos(x), new Constant(2)).Simplify();

            // 1 - cos(x)^2 = sin(x)^2
            case Subtract(
                Constant c,
                Power(Cos(var x), Constant e))
                when c.Value == 1 && e.Value == 2:
                return new Power(new Sin(x), new Constant(2)).Simplify();

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

            case Sqrt(Constant c) when c.Value >= 0:
                return new Constant(Math.Sqrt(c.Value));

            case Sqrt(Power(var b, Constant e)) when e.Value == 2:
                return b; // sqrt(x^2) = x (ignoring |x| domain nuance)

            case NthRoot(Constant c, Constant n):
                return new Constant(Math.Pow(c.Value, 1.0 / n.Value));

            default:
                return expr;
        }
    }

    // Helper method to extract the coefficient and the term from an expression
    private static (double Coefficient, Expr Term) ExtractCoefficient(Expr expr) => expr switch
    {
        Multiply(Constant c, var t) => (c.Value, t),
        Multiply(var t, Constant c) => (c.Value, t),
        _ => (1, expr)
    };

    // Flattens a chain of Add/Subtract into a flat list of (coefficient, term) pairs.
    private static void CollectTerms(Expr expr, double sign, List<(double Coefficient, Expr Term)> terms)
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

        var raw = new List<(double Coefficient, Expr Term)>();
        CollectTerms(expr, 1, raw);

        var combined = new List<(double Coefficient, Expr Term)>();
        foreach (var (coef, term) in raw)
        {
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

        // Drop terms that cancelled out to zero.
        combined.RemoveAll(t => t.Coefficient == 0);

        if (combined.Count == 0)
            return new Constant(0);

        Expr Rebuild(double coef, Expr term) =>
            coef == 1 ? term : new Multiply(new Constant(coef), term);

        Expr result = Rebuild(combined[0].Coefficient, combined[0].Term);
        for (int i = 1; i < combined.Count; i++)
        {
            var (coef, term) = combined[i];
            result = coef < 0
                ? new Subtract(result, Rebuild(-coef, term))
                : new Add(result, Rebuild(coef, term));
        }

        return result;
    }
}