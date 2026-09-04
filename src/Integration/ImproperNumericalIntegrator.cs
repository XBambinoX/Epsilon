namespace Epsilon;

internal static class ImproperNumericalIntegrator
{
    private const int SimpsonSteps = 1000;
    private const int MaxIterations = 30;

    private const double InitialEpsilon = 1e-4;
    private const double EpsilonFactor = 0.1;

    private const double InitialTailLength = 10.0;
    private const double TailGrowth = 2.0;

    public static double Integrate(
        Expr expr,
        double a,
        double b,
        double tolerance)
    {
        if (a > b)
            return -Integrate(expr, b, a, tolerance);

        bool aInfinite = double.IsNegativeInfinity(a);
        bool bInfinite = double.IsPositiveInfinity(b);

        if (!aInfinite && !bInfinite)
            return IntegrateFinite(expr, a, b, tolerance);

        if (aInfinite && bInfinite)
            return IntegrateTwoSided(expr, tolerance);

        if (aInfinite)
            return IntegrateLeftInfinite(expr, b, tolerance);

        return IntegrateRightInfinite(expr, a, tolerance);
    }

    private static double IntegrateFinite(
        Expr expr,
        double a,
        double b,
        double tolerance)
    {
        var singularities = SingularityAnalyzer
            .FindSingularities(expr, a, b)
            .Where(x => x > a && x < b)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        if (singularities.Length == 0)
            return expr.Integrate(a, b, SimpsonSteps);

        double result = 0.0;
        double left = a;

        foreach (double singularity in singularities)
        {
            result += IntegrateToSingularity(
                expr,
                left,
                singularity,
                tolerance);

            left = singularity;
        }

        result += IntegrateFromSingularity(
            expr,
            left,
            b,
            tolerance);

        return result;
    }

    private static double IntegrateToSingularity(
        Expr expr,
        double a,
        double singularity,
        double tolerance)
    {
        double epsilon = InitialEpsilon;
        double previous = double.NaN;

        for (int i = 0; i < MaxIterations; i++)
        {
            double right = singularity - epsilon;

            if (right <= a)
                epsilon *= EpsilonFactor;
            else
            {
                double current = expr.Integrate(
                    a,
                    right,
                    SimpsonSteps);

                if (double.IsFinite(previous) &&
                    Math.Abs(current - previous) < tolerance)
                    return current;

                previous = current;
                epsilon *= EpsilonFactor;
            }
        }

        return previous;
    }

    private static double IntegrateFromSingularity(
        Expr expr,
        double singularity,
        double b,
        double tolerance)
    {
        double epsilon = InitialEpsilon;
        double previous = double.NaN;

        for (int i = 0; i < MaxIterations; i++)
        {
            double left = singularity + epsilon;

            if (left >= b)
                epsilon *= EpsilonFactor;
            else
            {
                double current = expr.Integrate(
                    left,
                    b,
                    SimpsonSteps);

                if (double.IsFinite(previous) &&
                    Math.Abs(current - previous) < tolerance)
                    return current;

                previous = current;
                epsilon *= EpsilonFactor;
            }
        }

        return previous;
    }

    private static double IntegrateRightInfinite(
        Expr expr,
        double start,
        double tolerance)
    {
        double left = start;
        double length = InitialTailLength;

        double result = 0.0;

        for (int i = 0; i < MaxIterations; i++)
        {
            double right = start + length;

            double part = expr.Integrate(
                left,
                right,
                SimpsonSteps);

            result += part;

            if (Math.Abs(part) < tolerance)
                return result;

            left = right;
            length *= TailGrowth;
        }

        return result;
    }

    private static double IntegrateLeftInfinite(
        Expr expr,
        double end,
        double tolerance)
    {
        double right = end;
        double length = InitialTailLength;

        double result = 0.0;

        for (int i = 0; i < MaxIterations; i++)
        {
            double left = end - length;

            double part = expr.Integrate(
                left,
                right,
                SimpsonSteps);

            result += part;

            if (Math.Abs(part) < tolerance)
                return result;

            right = left;
            length *= TailGrowth;
        }

        return result;
    }

    private static double IntegrateTwoSided(
        Expr expr,
        double tolerance)
    {
        const double split = 0.0;

        double left = IntegrateLeftInfinite(
            expr,
            split,
            tolerance);

        double right = IntegrateRightInfinite(
            expr,
            split,
            tolerance);

        return left + right;
    }
}