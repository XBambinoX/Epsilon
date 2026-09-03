namespace Epsilon;

public static class Simplifier
{
    public static Expr Simplify(this Expr expr)
    {
        Expr current = expr;

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
            _ => expr
        };

        return ApplyRules(simplified);
    }

    private static Expr ApplyRules(Expr expr)
    {
        switch (expr)
        {
            case Add(Constant a, Constant b):
                return new Constant(a.Value + b.Value);

            case Add(var l, var r) when l.Equals(new Constant(0)):
                return r;
            case Add(var l, var r) when r.Equals(new Constant(0)):
                return l;

            case Subtract(Constant a, Constant b):
                return new Constant(a.Value - b.Value);

            case Subtract(var l, var r) when l.Equals(r):
                return new Constant(0);
            case Subtract(var l, var r) when r.Equals(new Constant(0)):
                return l;

            case Multiply(Constant a, Constant b):
                return new Constant(a.Value * b.Value);

            case Multiply(var l, var r) when l.Equals(new Constant(0)) || r.Equals(new Constant(0)):
                return new Constant(0);
            case Multiply(var l, var r) when l.Equals(new Constant(1)):
                return r;
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
}