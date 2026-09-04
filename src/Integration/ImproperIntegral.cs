namespace Epsilon;

public static class ImproperIntegrator
{
    private const double DefaultTolerance = 1e-6;

    private const double EndpointStep = 1e-6;
    private const double EndpointGrowth = 10.0;
    private const int EndpointSamples = 8;

    private const double TailStart = 10.0;
    private const double TailGrowth = 2.0;
    private const int TailSamples = 12;

    private const double PowerMargin = 0.05;
    private const double LogPowerMargin = 0.15;

    public static bool IsConvergent(
        this Expr expr,
        double a,
        double b,
        double tolerance = DefaultTolerance)
    {
        return AnalyzeConvergence(expr, a, b, tolerance);
    }

    private static bool AnalyzeConvergence(
        Expr expr,
        double a,
        double b,
        double tolerance)
    {
        if (double.IsNaN(a) || double.IsNaN(b))
            throw new ArgumentException("Integration bounds cannot be NaN.");

        if (a == b)
            return true;

        if (a > b)
            return AnalyzeConvergence(expr, b, a, tolerance);

        bool aInfinite = double.IsNegativeInfinity(a);
        bool bInfinite = double.IsPositiveInfinity(b);

        if (aInfinite && bInfinite)
            return AnalyzeTwoSidedInfiniteIntegral(expr, tolerance);

        if (aInfinite)
            return AnalyzeLeftInfiniteIntegral(expr, b, tolerance);

        if (bInfinite)
            return AnalyzeRightInfiniteIntegral(expr, a, tolerance);

        return AnalyzeFiniteIntegral(expr, a, b, tolerance);
    }

    private static bool AnalyzeFiniteIntegral(
        Expr expr,
        double a,
        double b,
        double tolerance)
    {
        var singularities = SingularityAnalyzer.FindSingularities(expr, a, b);

        var interior = singularities
            .Where(x => x > a && x < b)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        if (interior.Length == 0)
        {
            bool left = AnalyzeEndpoint(expr, a, true);
            bool right = AnalyzeEndpoint(expr, b, false);

            return left && right;
        }

        double previous = a;

        foreach (double singularity in interior)
        {
            if (!AnalyzeFiniteIntervalPiece(expr, previous, singularity, tolerance))
                return false;

            previous = singularity;
        }

        return AnalyzeFiniteIntervalPiece(expr, previous, b, tolerance);
    }

    private static bool AnalyzeFiniteIntervalPiece(
        Expr expr,
        double left,
        double right,
        double tolerance)
    {
        if (left == right)
            return true;

        bool leftOk = AnalyzeEndpoint(expr, left, true);
        bool rightOk = AnalyzeEndpoint(expr, right, false);

        return leftOk && rightOk;
    }

    private static bool AnalyzeLeftInfiniteIntegral(
        Expr expr,
        double finiteEnd,
        double tolerance)
    {
        if (!AnalyzeEndpoint(expr, finiteEnd, false))
            return false;

        var singularities = SingularityAnalyzer.FindSingularities(
            expr,
            finiteEnd - 1000.0,
            finiteEnd);

        foreach (double singularity in singularities)
        {
            if (singularity >= finiteEnd)
                continue;

            if (!AnalyzeEndpoint(expr, singularity, false))
                return false;

            if (!AnalyzeEndpoint(expr, singularity, true))
                return false;
        }

        return AnalyzeInfiniteTail(expr, finiteEnd, left: true, tolerance);
    }

    private static bool AnalyzeRightInfiniteIntegral(
        Expr expr,
        double finiteStart,
        double tolerance)
    {
        if (!AnalyzeEndpoint(expr, finiteStart, true))
            return false;

        var singularities = SingularityAnalyzer.FindSingularities(
            expr,
            finiteStart,
            finiteStart + 1000.0);

        foreach (double singularity in singularities)
        {
            if (singularity <= finiteStart)
                continue;

            if (!AnalyzeEndpoint(expr, singularity, false))
                return false;

            if (!AnalyzeEndpoint(expr, singularity, true))
                return false;
        }

        return AnalyzeInfiniteTail(expr, finiteStart, left: false, tolerance);
    }

    private static bool AnalyzeTwoSidedInfiniteIntegral(
        Expr expr,
        double tolerance)
    {
        const double split = 0.0;

        bool left = AnalyzeLeftInfiniteIntegral(expr, split, tolerance);
        bool right = AnalyzeRightInfiniteIntegral(expr, split, tolerance);

        return left && right;
    }

    private static bool AnalyzeEndpoint(
        Expr expr,
        double endpoint,
        bool towardsRight)
    {
        double direction = towardsRight ? 1.0 : -1.0;

        double firstStep = EndpointStep;
        double[] values = new double[EndpointSamples];
        double[] distances = new double[EndpointSamples];

        int count = 0;

        for (int i = 0; i < EndpointSamples; i++)
        {
            double distance = firstStep * Math.Pow(EndpointGrowth, i);
            double x = endpoint + direction * distance;

            if (!double.IsFinite(x))
                break;

            double value;

            try
            {
                value = expr.Evaluate(x);
            }
            catch
            {
                continue;
            }

            if (!double.IsFinite(value))
            {
                distances[count] = distance;
                values[count] = double.PositiveInfinity;
                count++;
                continue;
            }

            if (Math.Abs(value) < 1e-14)
                continue;

            distances[count] = distance;
            values[count] = Math.Abs(value);
            count++;
        }

        if (count < 3)
            return true;

        if (values.All(double.IsFinite))
        {
            double p = EstimatePower(distances, values);

            if (double.IsNaN(p))
                return true;

            return p < 1.0 - PowerMargin;
        }

        int finiteCount = values.Count(double.IsFinite);

        if (finiteCount < 3)
            return false;

        double[] finiteDistances = new double[finiteCount];
        double[] finiteValues = new double[finiteCount];

        int index = 0;

        for (int i = 0; i < count; i++)
        {
            if (!double.IsFinite(values[i]))
                continue;

            finiteDistances[index] = distances[i];
            finiteValues[index] = values[i];
            index++;
        }

        double exponent = EstimatePower(finiteDistances, finiteValues);

        if (double.IsNaN(exponent))
            return false;

        return exponent < 1.0 - PowerMargin;
    }

    private static bool AnalyzeInfiniteTail(
        Expr expr,
        double finiteEnd,
        bool left,
        double tolerance)
    {
        if (IsClearlyDivergentExponential(expr, left))
            return false;

        if (IsClearlyConvergentExponential(expr, left))
            return true;

        if (IsOscillatoryWithDecay(expr))
            return true;

        double sign = left ? -1.0 : 1.0;

        double[] xs = new double[TailSamples];
        double[] values = new double[TailSamples];

        int count = 0;

        for (int i = 0; i < TailSamples; i++)
        {
            double distance = TailStart * Math.Pow(TailGrowth, i);
            double x = finiteEnd + sign * distance;

            double value;

            try
            {
                value = expr.Evaluate(x);
            }
            catch
            {
                continue;
            }

            if (!double.IsFinite(value))
                return false;

            if (Math.Abs(value) < 1e-300)
                value = 1e-300;

            xs[count] = distance;
            values[count] = Math.Abs(value);
            count++;
        }

        if (count < 5)
            return false;

        double p = EstimatePower(xs, values);

        if (double.IsFinite(p) && p > 1.0 + PowerMargin)
        {
            double q = EstimateLogPower(xs, values);

            if (double.IsFinite(q))
                return q > 1.0 + LogPowerMargin;

            return true;
        }

        if (double.IsFinite(p) && p < 1.0 - PowerMargin)
            return false;

        double logPower = EstimateLogPower(xs, values);

        if (double.IsFinite(logPower))
            return logPower > 1.0 + LogPowerMargin;

        return false;
    }

    private static double EstimatePower(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        int n = Math.Min(x.Count, y.Count);

        if (n < 2)
            return double.NaN;

        double sumX = 0;
        double sumY = 0;
        double sumXX = 0;
        double sumXY = 0;

        int count = 0;

        for (int i = 0; i < n; i++)
        {
            if (x[i] <= 0 || y[i] <= 0)
                continue;

            double lx = Math.Log(x[i]);
            double ly = Math.Log(y[i]);

            if (!double.IsFinite(lx) || !double.IsFinite(ly))
                continue;

            sumX += lx;
            sumY += ly;
            sumXX += lx * lx;
            sumXY += lx * ly;
            count++;
        }

        if (count < 2)
            return double.NaN;

        double denominator = count * sumXX - sumX * sumX;

        if (Math.Abs(denominator) < 1e-14)
            return double.NaN;

        double slope = (count * sumXY - sumX * sumY) / denominator;

        return -slope;
    }

    private static double EstimateLogPower(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        int n = Math.Min(x.Count, y.Count);

        if (n < 3)
            return double.NaN;

        double sumX = 0;
        double sumY = 0;
        double sumXX = 0;
        double sumXY = 0;

        int count = 0;

        for (int i = 0; i < n; i++)
        {
            if (x[i] <= 1 || y[i] <= 0)
                continue;

            double lx = Math.Log(Math.Log(x[i]));
            double ly = Math.Log(y[i] * x[i]);

            if (!double.IsFinite(lx) || !double.IsFinite(ly))
                continue;

            sumX += lx;
            sumY += ly;
            sumXX += lx * lx;
            sumXY += lx * ly;
            count++;
        }

        if (count < 2)
            return double.NaN;

        double denominator = count * sumXX - sumX * sumX;

        if (Math.Abs(denominator) < 1e-14)
            return double.NaN;

        double slope = (count * sumXY - sumX * sumY) / denominator;

        return -slope;
    }

    private static bool IsClearlyDivergentExponential(
        Expr expr,
        bool left)
    {
        if (expr is Exp exp)
        {
            if (exp.Argument is Variable)
                return !left;

            if (TryGetLinearCoefficient(exp.Argument, out double coefficient))
            {
                if (!left && coefficient > 0)
                    return true;

                if (left && coefficient < 0)
                    return true;
            }
        }

        return false;
    }

    private static bool IsClearlyConvergentExponential(
        Expr expr,
        bool left)
    {
        if (expr is not Exp exp)
            return false;

        if (TryGetLinearCoefficient(exp.Argument, out double coefficient))
        {
            if (!left && coefficient < 0)
                return true;

            if (left && coefficient > 0)
                return true;
        }

        if (IsNegativePolynomial(exp.Argument))
            return true;

        return false;
    }

    private static bool IsOscillatoryWithDecay(Expr expr)
    {
        if (expr is Sin or Cos)
            return false;

        if (expr is Divide divide)
        {
            if (ContainsSinOrCos(divide.Numerator) &&
                IsGrowingPositivePowerOfX(divide.Denominator))
                return true;

            if (ContainsSinOrCos(divide.Denominator))
                return false;
        }

        if (expr is Multiply multiply)
        {
            return ContainsSinOrCos(multiply.Left) &&
                   IsDecayingPower(multiply.Right)
                   ||
                   ContainsSinOrCos(multiply.Right) &&
                   IsDecayingPower(multiply.Left);
        }

        return false;
    }

    private static bool ContainsSinOrCos(Expr expr)
    {
        return expr switch
        {
            Sin => true,
            Cos => true,
            Add(var left, var right) => ContainsSinOrCos(left) || ContainsSinOrCos(right),
            Subtract(var left, var right) => ContainsSinOrCos(left) || ContainsSinOrCos(right),
            Multiply(var left, var right) => ContainsSinOrCos(left) || ContainsSinOrCos(right),
            Divide(var left, var right) => ContainsSinOrCos(left) || ContainsSinOrCos(right),
            Power(var left, _) => ContainsSinOrCos(left),
            _ => false
        };
    }

    private static bool IsGrowingPositivePowerOfX(Expr expr)
    {
        if (expr is Variable)
            return true;

        if (expr is Power(var baseExpr, var exponent) &&
            baseExpr is Variable &&
            exponent is Constant power)
            return power.Value > 0;

        return false;
    }

    private static bool IsDecayingPower(Expr expr)
    {
        if (expr is Divide(var numerator, var denominator))
        {
            if (numerator is Constant c && c.Value != 0)
                return IsGrowingPositivePowerOfX(denominator);
        }

        if (expr is Power(var baseExpr, var exponent) &&
            baseExpr is Variable &&
            exponent is Constant power)
            return power.Value < 0;

        return false;
    }

    private static bool TryGetLinearCoefficient(
        Expr expr,
        out double coefficient)
    {
        switch (expr)
        {
            case Variable:
                coefficient = 1.0;
                return true;

            case Multiply(var left, var right):
                if (left is Constant lc && right is Variable)
                {
                    coefficient = lc.Value;
                    return true;
                }

                if (left is Variable && right is Constant rc)
                {
                    coefficient = rc.Value;
                    return true;
                }

                break;

            case Add(var left, var right):
                if (TryGetLinearCoefficient(left, out double l) &&
                    right is Constant)
                {
                    coefficient = l;
                    return true;
                }

                if (TryGetLinearCoefficient(right, out double r) &&
                    left is Constant)
                {
                    coefficient = r;
                    return true;
                }

                break;

            case Subtract(var left, var right):
                if (TryGetLinearCoefficient(left, out double sl) &&
                    right is Constant)
                {
                    coefficient = sl;
                    return true;
                }

                if (left is Constant &&
                    TryGetLinearCoefficient(right, out double sr))
                {
                    coefficient = -sr;
                    return true;
                }

                break;
        }

        coefficient = 0;
        return false;
    }

    private static bool IsNegativePolynomial(Expr expr)
    {
        if (expr is Multiply(var left, var right))
        {
            if (left is Constant c &&
                c.Value < 0 &&
                right is Power(var baseExpr, var exponent) &&
                baseExpr is Variable &&
                exponent is Constant p &&
                p.Value > 0)
                return true;

            if (right is Constant c2 &&
                c2.Value < 0 &&
                left is Power(var baseExpr2, var exponent2) &&
                baseExpr2 is Variable &&
                exponent2 is Constant p2 &&
                p2.Value > 0)
                return true;
        }

        if (expr is Subtract(var leftExpr, var rightExpr) &&
            leftExpr is Constant &&
            rightExpr is Power(var baseExpr3, var exponent3) &&
            baseExpr3 is Variable &&
            exponent3 is Constant)
            return true;

        return false;
    }
}