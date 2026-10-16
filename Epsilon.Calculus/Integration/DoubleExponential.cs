namespace Epsilon.Calculus;

internal sealed class DoubleExponential
{
    private const double MaxLogDistance = 708.3964185322641;
    private const int FirstLevel = 2;
    private const int MaxLevel = 8;

    // How far out in t a value that isn't finite ends a half instead of failing: under- and
    // overflow cut the tails of convergent integrals short there (exp(-1/x) / x^2 is 0/0 near 0).
    private const double FarOut = 4;

    // The nodes from the middle of the piece out to End, an end of the piece or an infinity; At
    // takes the distance from the end the half is measured from, and Limit is the largest t
    // whose distance is a normal double.
    private readonly record struct Half(
        IntegrandEnd At,
        Func<double, double> Position,
        double End,
        Func<double, (double Distance, double Weight)> Node,
        double Limit);

    private readonly Half[] _halves;
    private readonly double[] _reach = new double[2];
    private readonly double[] _last = new double[2];
    private double _step, _sum, _absoluteSum, _largestTerm, _error = double.PositiveInfinity;

    // Walks both halves out on steps of 1/4 in t as far as the distances are doubles.
    private DoubleExponential(Half first, Half second)
    {
        _halves = [first, second];
        _step = Math.ScaleB(1, -FirstLevel);
        double center = Term(first, 0, farOutEnds: false);
        _sum = center;
        _absoluteSum = Math.Abs(center);

        for (int i = 0; i < 2; i++)
        {
            _last[i] = center;
            int j = 1;
            for (; j * _step <= _halves[i].Limit; j++)
            {
                double term = Term(_halves[i], j * _step, farOutEnds: true);
                if (double.IsNaN(term))
                    break;

                _sum += term;
                _absoluteSum += Math.Abs(term);
                _last[i] = term;
            }

            _reach[i] = (j - 1) * _step;
        }
    }

    public static DoubleExponential Finite(IntegrandEnd nearP, IntegrandEnd nearQ, double p, double q)
    {
        double halfWidth = q / 2 - p / 2;
        double limit = Math.Asinh((Math.Log(2 * halfWidth) + MaxLogDistance) / Math.PI);
        (double, double) Node(double t) => TanhSinh(t, halfWidth);

        return new DoubleExponential(
            new Half(nearP, d => p + d, p, Node, limit),
            new Half(nearQ, d => q - d, q, Node, limit));
    }

    // The piece from p towards the infinity in the direction (1 or -1); near takes the distance from p.
    public static DoubleExponential SemiInfinite(IntegrandEnd near, double p, int direction)
    {
        double limit = Math.Asinh(2 * MaxLogDistance / Math.PI);

        return new DoubleExponential(
            new Half(near, d => p + direction * d, p, t => ExpSinh(-t), limit),
            new Half(near with { Unresolved = 0 }, d => p + direction * d, direction * double.PositiveInfinity, ExpSinh, limit));
    }

    // The integral of |f| so far, to share the tolerance between the pieces.
    public double AbsoluteIntegral => _step * _absoluteSum;

    // Where the largest weighted value is, which is where the integral has its weight.
    public double Peak { get; private set; }

    // Halves the step until two estimates agree to the tolerance times the integral of |f|, or
    // times the floor where that is smaller. The last term of each half must be negligible first:
    // a divergent integral has terms that don't die out, and summed to any cut-off it would look finite.
    public bool TryConverge(double tolerance, double floor, out double estimate)
    {
        double Allowed() => tolerance * Math.Max(_step * _absoluteSum, floor);

        for (int i = 0; i < 2; i++)
        {
            Half half = _halves[i];
            if (double.IsPositiveInfinity(half.At.Unresolved) || Math.Abs(_last[i]) > Allowed())
                throw new InvalidOperationException(
                    $"The integral diverges at {Where(half.End)}, or converges there too slowly for double precision.");
            if (half.At.Unresolved > Allowed())
                throw new InvalidOperationException(FormattableString.Invariant(
                    $"The integrand is singular next to x = {half.End}, at a point that isn't a double, which limits the accuracy to about {half.At.Unresolved:G2}."));
        }

        estimate = _step * _sum;
        for (int level = FirstLevel + 1; level <= MaxLevel; level++)
        {
            _step /= 2;
            for (int i = 0; i < 2; i++)
                for (int j = 1; j * _step <= _reach[i]; j += 2)
                {
                    double term = Term(_halves[i], j * _step, farOutEnds: false);
                    _sum += term;
                    _absoluteSum += Math.Abs(term);
                }

            double next = _step * _sum;
            _error = Math.Abs(next - estimate);
            estimate = next;
            if (_error <= Allowed())
                return true;
        }

        return false;
    }

    public InvalidOperationException NotConverged()
    {
        double low = Math.Min(_halves[0].End, _halves[1].End), high = Math.Max(_halves[0].End, _halves[1].End);
        return new InvalidOperationException(FormattableString.Invariant(
            $"The integral over [{low}, {high}] didn't converge: the estimate {_step * _sum} may be off by {_error}."));
    }

    // x = mid + halfWidth * tanh(pi/2 sinh t), as the distance 2 halfWidth / (1 + e^(pi sinh |t|))
    // from the nearer end, with the weight dx/dt.
    private static (double, double) TanhSinh(double t, double halfWidth)
    {
        double z = Math.PI * Math.Sinh(t);
        double e = Math.Exp(-z);
        double distance = Math.Exp(Math.Log(2 * halfWidth) - z) / (1 + e);
        return (distance, Math.PI * Math.Cosh(t) * distance / (1 + e));
    }

    // x = p + e^(pi/2 sinh t), with the weight dx/dt.
    private static (double, double) ExpSinh(double t)
    {
        double distance = Math.Exp(Math.PI / 2 * Math.Sinh(t));
        return (distance, Math.PI / 2 * Math.Cosh(t) * distance);
    }

    // The weighted value at t, or NaN where it isn't finite far out and farOutEnds allows that.
    private double Term(Half half, double t, bool farOutEnds)
    {
        var (distance, weight) = half.Node(t);
        double value = half.At.At(distance);
        double term = weight * value;
        if (double.IsFinite(term))
        {
            if (Math.Abs(term) > _largestTerm)
            {
                _largestTerm = Math.Abs(term);
                Peak = half.Position(distance);
            }

            return term;
        }
        if (farOutEnds && t >= FarOut)
            return double.NaN;

        string what = double.IsNaN(value) ? "not a real number" : "infinite";
        throw new InvalidOperationException(FormattableString.Invariant(
            $"The integrand is {what} at x = {half.Position(distance)}."));
    }

    private static string Where(double end) =>
        double.IsInfinity(end) ? (end > 0 ? "infinity" : "-infinity") : FormattableString.Invariant($"x = {end}");
}
