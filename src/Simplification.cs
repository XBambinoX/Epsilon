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

            default:
                return expr;
        }
    }
}