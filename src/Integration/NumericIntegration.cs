namespace Epsilon;

public static class NumericIntegration
{
    private const int MaxRefinements = 12;
    private const int MaxSingularityDepth = 12;

    private const double InitialStep = 0.5;
    private const double MinStep = 1.0 / 4096.0;

    private const double TanhSinhLimit = 25.0;

    private const int SingularityProbeCount = 33;

    private const double FiniteValueLimit = 1e300;

    /// <summary>
    /// Numerically integrates an expression over [a, b].
    ///
    /// Supports:
    /// - finite intervals;
    /// - improper integrals;
    /// - infinite bounds;
    /// - endpoint singularities;
    /// - interior singularities;
    /// - reversed integration limits.
    /// </summary>
    public static double Integrate(
        this Expr expr,
        double a,
        double b,
        double tolerance = 1e-8)
    {
        ArgumentNullException.ThrowIfNull(expr);

        if (double.IsNaN(a) || double.IsNaN(b))
            throw new ArgumentException("Integration bounds cannot be NaN.");

        if (double.IsNaN(tolerance) ||
            double.IsInfinity(tolerance) ||
            tolerance <= 0.0)
        {
            throw new ArgumentException(
                "Tolerance must be finite and positive.",
                nameof(tolerance));
        }

        if (a == b)
            return 0.0;

        if (a > b)
            return -Integrate(expr, b, a, tolerance);

        bool leftInfinite = double.IsNegativeInfinity(a);
        bool rightInfinite = double.IsPositiveInfinity(b);

        if (leftInfinite && rightInfinite)
            return IntegrateTwoSidedInfinite(expr, tolerance);

        if (leftInfinite)
            return IntegrateLeftInfinite(expr, b, tolerance);

        if (rightInfinite)
            return IntegrateRightInfinite(expr, a, tolerance);

        return IntegrateFinite(expr, a, b, tolerance);
    }

    // FINITE INTERVAL

    private static double IntegrateFinite(
        Expr expr,
        double a,
        double b,
        double tolerance)
    {
        if (IsFiniteAt(expr, a) &&
            IsFiniteAt(expr, b))
        {
            return IntegrateRegularFinite(
                expr,
                a,
                b,
                tolerance);
        }
        return IntegrateTanhSinh(
            expr,
            a,
            b,
            tolerance);
    }

    private static double IntegrateRegularFinite(
        Expr expr,
        double a,
        double b,
        double tolerance)
    {
        List<double> breakpoints =
            FindInteriorSingularities(expr, a, b);

        if (breakpoints.Count == 0)
        {
            double result = IntegrateTanhSinh(
                expr,
                a,
                b,
                tolerance);

            EnsureFiniteResult(result);

            return result;
        }

        double total = 0.0;

        double left = a;

        for (int i = 0; i < breakpoints.Count; i++)
        {
            double point = breakpoints[i];

            if (point <= left || point >= b)
                continue;

            double localTolerance =
                tolerance / (breakpoints.Count + 1);

            double part = IntegrateTanhSinh(
                expr,
                left,
                point,
                localTolerance);

            EnsureFiniteResult(part);

            total += part;
            left = point;
        }

        if (left < b)
        {
            double localTolerance =
                tolerance / (breakpoints.Count + 1);

            double part = IntegrateTanhSinh(
                expr,
                left,
                b,
                localTolerance);

            EnsureFiniteResult(part);

            total += part;
        }

        EnsureFiniteResult(total);

        return total;
    }

    // TANH-SINH QUADRATURE

    /*
     * Double exponential / tanh-sinh quadrature.
     *
     * Mapping:
     *
     *     x = midpoint + halfWidth * tanh((pi/2) sinh(t))
     *
     * This has an extremely useful property:
     *
     *     t -> -inf  => x -> a
     *     t -> +inf  => x -> b
     *
     * Therefore the actual endpoints are never evaluated.
     */
    private static double IntegrateTanhSinh(
        Expr expr,
        double a,
        double b,
        double tolerance)
    {
        if (!(a < b))
            return 0.0;

        double previous = double.NaN;
        double step = InitialStep;

        for (int refinement = 0;
             refinement < MaxRefinements;
             refinement++)
        {
            double current =
                TanhSinhSum(
                    expr,
                    a,
                    b,
                    step);

            EnsureFiniteResult(current);

            if (double.IsFinite(previous))
            {
                double error =
                    Math.Abs(current - previous);

                double scale =
                    Math.Max(1.0, Math.Abs(current));

                if (error <= tolerance * scale)
                    return current;
            }

            previous = current;
            step *= 0.5;

            if (step < MinStep)
                break;
        }

        /*
         * At this point the result itself is finite, but we did
         * not reach the requested tolerance. Returning the best
         * estimate is more useful than silently returning Infinity.
         */
        EnsureFiniteResult(previous);

        return previous;
    }

    private static double TanhSinhSum(
        Expr expr,
        double a,
        double b,
        double step)
    {
        double midpoint = (a + b) * 0.5;
        double halfWidth = (b - a) * 0.5;

        double sum = 0.0;

        /*
         * t = 0 is evaluated once.
         */
        {
            double x = midpoint;

            double derivative =
                halfWidth * Math.PI * 0.5;

            double value =
                EvaluateForQuadrature(expr, x);

            sum += value * derivative;
        }

        /*
         * Positive and negative t are symmetric in the mapping.
         */
        for (int k = 1; k < 100000; k++)
        {
            double t = k * step;

            double sinhT;

            try
            {
                sinhT = Math.Sinh(t);
            }
            catch
            {
                break;
            }

            double z =
                Math.PI * 0.5 * sinhT;

            /*
             * Once z is large enough, sech²(z) is effectively
             * zero in double precision.
             */
            if (z > TanhSinhLimit)
                break;

            double coshT =
                Math.Cosh(t);

            double tanhZ =
                Math.Tanh(z);

            double coshZ =
                Math.Cosh(z);

            double derivative =
                halfWidth *
                Math.PI *
                0.5 *
                coshT /
                (coshZ * coshZ);

            if (derivative == 0.0 ||
                !double.IsFinite(derivative))
            {
                break;
            }

            double rightX =
                midpoint + halfWidth * tanhZ;

            double leftX =
                midpoint - halfWidth * tanhZ;

            /*
             * Floating point arithmetic can eventually make
             * rightX == b or leftX == a. Do not evaluate there.
             */
            if (rightX < b && rightX > a)
            {
                double rightValue =
                    EvaluateForQuadrature(expr, rightX);

                sum += rightValue * derivative;
            }

            if (leftX > a && leftX < b)
            {
                double leftValue =
                    EvaluateForQuadrature(expr, leftX);

                sum += leftValue * derivative;
            }
        }

        double result = sum * step;

        EnsureFiniteResult(result);

        return result;
    }

    // INFINITE INTERVALS

    private static double IntegrateRightInfinite(
        Expr expr,
        double a,
        double tolerance)
    {
        double result =
            IntegrateTanhSinhTransformed(
                expr,
                a,
                false,
                tolerance);

        EnsureFiniteResult(result);

        return result;
    }

    private static double IntegrateLeftInfinite(
        Expr expr,
        double b,
        double tolerance)
    {
        double result =
            IntegrateTanhSinhTransformed(
                expr,
                b,
                true,
                tolerance);

        EnsureFiniteResult(result);

        return result;
    }

    private static double IntegrateTwoSidedInfinite(
        Expr expr,
        double tolerance)
    {
        /*
         * IMPORTANT:
         * This is NOT a Cauchy principal value.
         * Both halves must converge independently.
         */
        double left =
            IntegrateLeftInfinite(
                expr,
                0.0,
                tolerance * 0.5);

        double right =
            IntegrateRightInfinite(
                expr,
                0.0,
                tolerance * 0.5);

        EnsureFiniteResult(left);
        EnsureFiniteResult(right);

        double result = left + right;

        EnsureFiniteResult(result);

        return result;
    }

    private static double IntegrateTanhSinhTransformed(
        Expr expr,
        double finiteBound,
        bool leftInfinite,
        double tolerance)
    {
        double previous = double.NaN;
        double step = InitialStep;

        for (int refinement = 0;
             refinement < MaxRefinements;
             refinement++)
        {
            double current =
                InfiniteTanhSinhSum(
                    expr,
                    finiteBound,
                    leftInfinite,
                    step);

            EnsureFiniteResult(current);

            if (double.IsFinite(previous))
            {
                double error =
                    Math.Abs(current - previous);

                double scale =
                    Math.Max(1.0, Math.Abs(current));

                if (error <= tolerance * scale)
                    return current;
            }

            previous = current;
            step *= 0.5;

            if (step < MinStep)
                break;
        }

        EnsureFiniteResult(previous);

        return previous;
    }

    private static double InfiniteTanhSinhSum(
        Expr expr,
        double finiteBound,
        bool leftInfinite,
        double step)
    {
        /*
         * First transform:
         *     u in (0,1)
         *
         * Second transform:
         *
         *     u = (1 + tanh((pi/2)sinh(t))) / 2
         *
         * Combined:
         *
         *     x = bound +/- tan(pi*u/2)
         * The tanh-sinh part keeps us away from u = 0 and u = 1.
         */

        double sum = 0.0;

        for (int k = 0; k < 100000; k++)
        {
            double t = k * step;

            if (k == 0)
            {
                double u = 0.5;

                double value =
                    EvaluateInfiniteTransformed(
                        expr,
                        finiteBound,
                        leftInfinite,
                        u);

                double derivative =
                    Math.PI * 0.25;

                sum += value * derivative;

                continue;
            }

            double sinhT =
                Math.Sinh(t);

            double z =
                Math.PI * 0.5 * sinhT;

            if (z > TanhSinhLimit)
                break;

            double coshT =
                Math.Cosh(t);

            double tanhZ =
                Math.Tanh(z);

            double coshZ =
                Math.Cosh(z);

            double duDt =
                0.5 *
                Math.PI *
                0.5 *
                coshT /
                (coshZ * coshZ);

            if (duDt == 0.0 ||
                !double.IsFinite(duDt))
            {
                break;
            }

            double rightU =
                0.5 * (1.0 + tanhZ);

            double leftU =
                0.5 * (1.0 - tanhZ);

            if (rightU > 0.0 && rightU < 1.0)
            {
                double value =
                    EvaluateInfiniteTransformed(
                        expr,
                        finiteBound,
                        leftInfinite,
                        rightU);

                sum += value * duDt;
            }

            if (leftU > 0.0 && leftU < 1.0)
            {
                double value =
                    EvaluateInfiniteTransformed(
                        expr,
                        finiteBound,
                        leftInfinite,
                        leftU);

                sum += value * duDt;
            }
        }

        /*
         * The infinite transformation itself already contains
         * the dx/du Jacobian.
         */
        double result = sum * step;

        EnsureFiniteResult(result);

        return result;
    }

    private static double EvaluateInfiniteTransformed(
        Expr expr,
        double finiteBound,
        bool leftInfinite,
        double u)
    {

        double angle =
            Math.PI * 0.5 * u;

        double cosine =
            Math.Cos(angle);

        if (Math.Abs(cosine) < 1e-14) return 0.0;

        double tangent =
            Math.Tan(angle);

        double x;

        if (leftInfinite)
            x = finiteBound - tangent;
        else
            x = finiteBound + tangent;

        if (!double.IsFinite(x))
            return 0.0;

        double jacobian =
            (Math.PI * 0.5) /
            (cosine * cosine);

        if (!double.IsFinite(jacobian))
            return 0.0;

        double value =
            EvaluateForQuadrature(expr, x);

        return value * jacobian;
    }

    // SINGULARITY DETECTION

    /*
     * Numerical singularity detection.
     *
     * We deliberately do NOT declare every large function value
     * to be a singularity. Instead, we look for points where the
     * expression cannot be evaluated as a finite real number.
     */
    private static List<double> FindInteriorSingularities(
        Expr expr,
        double a,
        double b)
    {
        var singularities = new List<double>();

        double length = b - a;

        for (int i = 1;
             i < SingularityProbeCount;
             i++)
        {
            double x =
                a + length *
                i / (double)SingularityProbeCount;

            if (!IsFiniteAt(expr, x))
                singularities.Add(x);
        }

        FindSingularitiesRecursive(
            expr,
            a,
            b,
            0,
            singularities);

        singularities.Sort();

        RemoveDuplicatePoints(
            singularities,
            Math.Abs(b - a) * 1e-12);

        return singularities;
    }

    private static void FindSingularitiesRecursive(
        Expr expr,
        double a,
        double b,
        int depth,
        List<double> singularities)
    {
        if (depth >= MaxSingularityDepth)
            return;

        double midpoint =
            (a + b) * 0.5;

        bool leftFinite =
            IsFiniteAt(expr, a);

        bool middleFinite =
            IsFiniteAt(expr, midpoint);

        bool rightFinite =
            IsFiniteAt(expr, b);

        if (!middleFinite)
        {
            if (midpoint > a && midpoint < b)
                singularities.Add(midpoint);

            FindSingularitiesRecursive(
                expr,
                a,
                midpoint,
                depth + 1,
                singularities);

            FindSingularitiesRecursive(
                expr,
                midpoint,
                b,
                depth + 1,
                singularities);

            return;
        }

        if (!leftFinite)
        {
            if (a > double.NegativeInfinity)
                singularities.Add(a);
        }

        if (!rightFinite)
        {
            if (b < double.PositiveInfinity)
                singularities.Add(b);
        }
    }

    private static void RemoveDuplicatePoints(
        List<double> points,
        double tolerance)
    {
        if (points.Count < 2)
            return;

        int writeIndex = 1;

        for (int i = 1; i < points.Count; i++)
        {
            if (Math.Abs(points[i] - points[writeIndex - 1])
                > tolerance)
            {
                points[writeIndex++] = points[i];
            }
        }

        if (writeIndex < points.Count)
            points.RemoveRange(
                writeIndex,
                points.Count - writeIndex);
    }

    // EVALUATION

    private static double EvaluateForQuadrature(
        Expr expr,
        double x)
    {
        double value;

        try
        {
            value = expr.Evaluate(x);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to evaluate expression at x = {x}.",
                ex);
        }

        if (double.IsNaN(value))
        {
            throw new InvalidOperationException(
                $"Expression evaluated to NaN at x = {x}.");
        }

        if (double.IsInfinity(value))
        {
            throw new InvalidOperationException(
                $"Expression is singular at x = {x}.");
        }

        if (Math.Abs(value) > FiniteValueLimit)
        {
            throw new InvalidOperationException(
                $"Expression is numerically unstable at x = {x}.");
        }

        return value;
    }

    private static bool IsFiniteAt(
        Expr expr,
        double x)
    {
        if (!double.IsFinite(x))
            return false;

        try
        {
            double value =
                expr.Evaluate(x);

            return double.IsFinite(value);
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureFiniteResult(
        double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException(
                "The integral does not have a finite numerical value.");
        }
    }
}