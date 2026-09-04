using Epsilon.Core;

namespace Epsilon.Calculus;

public static class SingularityAnalyzer
{
    private const int RootSamples = 256;
    private const int BisectionIterations = 80;

    public static IReadOnlyList<double> FindSingularities(
        this Expr expr,
        double a,
        double b)
    {
        if (a > b)
            (a, b) = (b, a);

        var result = new List<double>();

        FindSingularities(expr, a, b, result);

        return result
            .Where(x => x >= a && x <= b)
            .DistinctBy(x => Math.Round(x, 12))
            .OrderBy(x => x)
            .ToList();
    }

    public static bool HasInteriorSingularity(
        this Expr expr,
        double a,
        double b)
    {
        return expr.FindSingularities(a, b)
            .Any(x => x > a && x < b);
    }

    private static void FindSingularities(
        Expr expr,
        double a,
        double b,
        List<double> result)
    {
        switch (expr)
        {
            case Divide(var numerator, var denominator):
                FindSingularities(
                    numerator,
                    a,
                    b,
                    result);

                FindSingularities(
                    denominator,
                    a,
                    b,
                    result);

                FindZeros(
                    denominator,
                    a,
                    b,
                    result);

                break;

            case Power(var baseExpr, Constant exponent)
                when exponent.Value < 0:

                FindSingularities(
                    baseExpr,
                    a,
                    b,
                    result);

                FindZeros(
                    baseExpr,
                    a,
                    b,
                    result);

                break;

            case Sqrt(var inner):
                FindSingularities(
                    inner,
                    a,
                    b,
                    result);

                FindZeros(
                    inner,
                    a,
                    b,
                    result);

                break;

            case Ln(var inner):
                FindSingularities(
                    inner,
                    a,
                    b,
                    result);

                FindZeros(
                    inner,
                    a,
                    b,
                    result);

                break;

            case Add(var left, var right):
                FindSingularities(left, a, b, result);
                FindSingularities(right, a, b, result);
                break;

            case Subtract(var left, var right):
                FindSingularities(left, a, b, result);
                FindSingularities(right, a, b, result);
                break;

            case Multiply(var left, var right):
                FindSingularities(left, a, b, result);
                FindSingularities(right, a, b, result);
                break;
        }
    }

    private static void FindZeros(
        Expr expr,
        double a,
        double b,
        List<double> result)
    {
        if (a > b)
            (a, b) = (b, a);

        // Constant

        if (expr is Constant c)
        {
            if (c.Value == 0.0)
                result.Add(a);

            return;
        }

        // x

        if (expr is Variable)
        {
            if (a <= 0.0 && b >= 0.0)
                result.Add(0.0);

            return;
        }

        // x + c

        if (expr is Add(Variable, Constant c1))
        {
            double root = -c1.Value;

            if (root >= a && root <= b)
                result.Add(root);

            return;
        }

        if (expr is Add(Constant c2, Variable))
        {
            double root = -c2.Value;

            if (root >= a && root <= b)
                result.Add(root);

            return;
        }

        // x - c

        if (expr is Subtract(Variable, Constant c3))
        {
            double root = c3.Value;

            if (root >= a && root <= b)
                result.Add(root);

            return;
        }

        // c - x

        if (expr is Subtract(Constant c4, Variable))
        {
            double root = c4.Value;

            if (root >= a && root <= b)
                result.Add(root);

            return;
        }

        // x^n

        if (expr is Power(Variable, Constant exponent))
        {
            if (exponent.Value > 0 &&
                a <= 0.0 &&
                b >= 0.0)
            {
                result.Add(0.0);
            }

            return;
        }

        // x² + c

        if (expr is Add(
                Power(Variable, Constant power),
                Constant constant)
            && power.Value == 2.0)
        {
            if (constant.Value > 0.0)
                return;

            if (constant.Value == 0.0)
            {
                if (a <= 0.0 && b >= 0.0)
                    result.Add(0.0);

                return;
            }

            double root = Math.Sqrt(-constant.Value);

            if (root >= a && root <= b)
                result.Add(root);

            if (-root >= a && -root <= b)
                result.Add(-root);

            return;
        }

        // c + x²

        if (expr is Add(
                Constant constant2,
                Power(Variable, Constant power2))
            && power2.Value == 2.0)
        {

            if (constant2.Value > 0.0)
                return;

            if (constant2.Value == 0.0)
            {
                if (a <= 0.0 && b >= 0.0)
                    result.Add(0.0);

                return;
            }

            double root = Math.Sqrt(-constant2.Value);

            if (root >= a && root <= b)
                result.Add(root);

            if (-root >= a && -root <= b)
                result.Add(-root);

            return;
        }

        // Generic numerical root search

        FindNumericalZeros(
            expr,
            a,
            b,
            result);
    }

    private static void FindNumericalZeros(
        Expr expr,
        double a,
        double b,
        List<double> result)
    {
        if (!double.IsFinite(a) ||
            !double.IsFinite(b))
        {
            return;
        }

        if (a == b)
        {
            double value = SafeEvaluate(expr, a);

            if (double.IsFinite(value) &&
                Math.Abs(value) < 1e-12)
            {
                result.Add(a);
            }

            return;
        }

        double step = (b - a) / RootSamples;

        double previousX = a;
        double previousY = SafeEvaluate(expr, previousX);

        for (int i = 1; i <= RootSamples; i++)
        {
            double x = a + i * step;
            double y = SafeEvaluate(expr, x);

            if (double.IsFinite(previousY) &&
                double.IsFinite(y))
            {
                if (Math.Abs(previousY) < 1e-12)
                    result.Add(previousX);

                if (Math.Abs(y) < 1e-12)
                    result.Add(x);

                if (Math.Sign(previousY) != Math.Sign(y))
                {
                    double? root = FindRoot(
                        expr,
                        previousX,
                        x);

                    if (root.HasValue)
                        result.Add(root.Value);
                }
            }

            previousX = x;
            previousY = y;
        }
    }

    private static double? FindRoot(
        Expr expr,
        double a,
        double b)
    {
        double fa = SafeEvaluate(expr, a);
        double fb = SafeEvaluate(expr, b);

        if (!double.IsFinite(fa) ||
            !double.IsFinite(fb))
        {
            return null;
        }

        if (fa == 0.0)
            return a;

        if (fb == 0.0)
            return b;

        if (Math.Sign(fa) == Math.Sign(fb))
            return null;

        for (int i = 0; i < BisectionIterations; i++)
        {
            double mid = (a + b) * 0.5;
            double fm = SafeEvaluate(expr, mid);

            if (!double.IsFinite(fm))
                return null;

            if (Math.Abs(fm) < 1e-12)
                return mid;

            if (Math.Sign(fa) != Math.Sign(fm))
            {
                b = mid;
                fb = fm;
            }
            else
            {
                a = mid;
                fa = fm;
            }
        }

        return (a + b) * 0.5;
    }

    private static double SafeEvaluate(
        Expr expr,
        double x)
    {
        try
        {
            return expr.Evaluate(x);
        }
        catch
        {
            return double.NaN;
        }
    }
}