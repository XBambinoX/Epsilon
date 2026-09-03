namespace Epsilon;

public static class Integrator
{
    public static double Integrate(this Expr expr, double a, double b, int steps = 1000)
    {
        if (steps % 2 != 0)
            throw new ArgumentException("Steps must be even for Simpson's rule.", nameof(steps));

        if (a > b)
            return -Integrate(expr, b, a, steps);

        double h = (b - a) / steps;
        double sum = expr.Evaluate(a) + expr.Evaluate(b);

        for (int i = 1; i < steps; i++)
        {
            double x = a + i * h;
            sum += (i % 2 == 0 ? 2 : 4) * expr.Evaluate(x);
        }

        return sum * h / 3.0;
    }
}