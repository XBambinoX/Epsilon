using Epsilon.Core;

namespace Epsilon.Calculus;

// An integrand that oscillates with a period towards an infinity, through sines and cosines of
// linear arguments whose slopes are rational multiples of each other. With those arguments
// written in a phase y, the integrand is F(x, x), and F(x, y) takes apart exactly, even far out
// where x is rounded to more than the phase may be, its mean over a period and the parts of the
// rest that change sign every half period, every quarter, every eighth and so on: the odd
// harmonics, the harmonics twice an odd number, four times and so on.
internal sealed class Oscillation
{
    private const int MaxHarmonic = 64;
    private const int MaxSamples = 512;
    private const int Levels = 8;
    private const double MachineEpsilon = 2.220446049250313E-16;

    // How far a nonlinear argument may still turn far out for its sine or cosine to count as
    // settled, as sin(1/x) does, rather than oscillating, as sin(x^2) does.
    private const double SettledTurn = 1e-3;

    private readonly Func<double, double, double> _phased;
    private readonly int _samples;

    private Oscillation(Func<double, double, double> phased, double period, int samples)
    {
        _phased = phased;
        Period = period;
        _samples = samples;
    }

    public double Period { get; }

    // The levels of the parts that aren't negligible; the part of level j changes sign every
    // Period / 2^(j + 1).
    public List<int> Parts { get; } = [];

    // Null where the integrand doesn't oscillate towards the infinity in the direction (1 or -1)
    // from the last end, or oscillates in a way this can't take, which oscillates tells: with
    // nonlinear phases, with periods that aren't commensurate, or too far from smooth in the phase.
    public static Oscillation? Of(Expr expr, string variable, double end, int direction, out bool oscillates)
    {
        string phase = variable + "'";
        var slopes = new List<Expr>();
        Expr? phased = Phased(expr, variable, phase, end, direction, slopes);
        oscillates = phased is null || slopes.Count > 0;
        if (phased is null || slopes.Count == 0 || FundamentalSlope(slopes) is not { } slope)
            return null;

        Func<double, double, double> compiled = phased.Compile(variable, phase);
        double period = 2 * Math.PI / slope;
        double[] near = [end + direction * period, end + direction * 1e6 * period];
        int needed = 0;
        foreach (double x in near)
            for (int samples = 8; ; samples *= 2)
            {
                if (samples > MaxSamples)
                    return null;
                double fewer = Mean(compiled, period, x, samples), more = Mean(compiled, period, x, 2 * samples);
                if (!double.IsFinite(more))
                    return null;
                if (Math.Abs(more - fewer) <= 1e-14 * MeanOfAbs(compiled, period, x, 2 * samples))
                {
                    needed = Math.Max(needed, 2 * samples);
                    break;
                }
            }

        var oscillation = new Oscillation(compiled, period, needed);
        foreach (double x in near)
        {
            double amplitude = oscillation.Amplitude(x);
            if (oscillation.Largest(phase => oscillation.Rest(x, phase)) > 1e-14 * amplitude)
                return null;

            // The rounding of the phases leaves about 1e-15 of the amplitude in a part that is 0,
            // as the odd harmonics of sin(x)^2 are.
            for (int level = 0; level < Levels; level++)
                if (!oscillation.Parts.Contains(level) && oscillation.Largest(phase => oscillation.Part(level, x, phase)) > 1e-12 * amplitude)
                    oscillation.Parts.Add(level);
        }

        oscillation.Parts.Sort();
        return oscillation;
    }

    public double Mean(double x) => Mean(_phased, Period, x, _samples);

    // The largest distance of the integrand from its mean over a period at x.
    public double Amplitude(double x)
    {
        double mean = Mean(x);
        return Largest(phase => _phased(x, phase) - mean);
    }

    // The part of level j at x, with the phase given apart: half the difference of the means of
    // F over the 2^j phases a 2^j-th of a period apart, from the phase and half a step on.
    public double Part(int level, double x, double phase)
    {
        int count = 1 << level;
        double step = Period / count, sum = 0;
        for (int i = 0; i < count; i++)
            sum += _phased(x, phase + i * step) - _phased(x, phase + i * step + step / 2);

        return sum / (2 * count);
    }

    // What the parts leave of the integrand less its mean: the harmonics 2^Levels times a whole number.
    private double Rest(double x, double phase)
    {
        int count = 1 << Levels;
        double sum = 0;
        for (int i = 0; i < count; i++)
            sum += _phased(x, phase + i * Period / count);

        return sum / count - Mean(x);
    }

    private double Largest(Func<double, double> f)
    {
        double largest = 0;
        for (int i = 0; i < _samples; i++)
            largest = Math.Max(largest, Math.Abs(f(i * Period / _samples)));

        return largest;
    }

    // The trapezoidal rule, exact to rounding for a smooth periodic function with enough samples.
    // A mean no larger than the rounding of the samples is 0: the mean of sin(x)/sqrt(x) would
    // be noise that exp-sinh can't integrate, and that diverges, as 1/sqrt(x) does.
    private static double Mean(Func<double, double, double> f, double period, double x, int samples)
    {
        double sum = 0, absoluteSum = 0;
        for (int i = 0; i < samples; i++)
        {
            double value = f(x, i * period / samples);
            sum += value;
            absoluteSum += Math.Abs(value);
        }

        return Math.Abs(sum) <= 32 * MachineEpsilon * absoluteSum ? 0 : sum / samples;
    }

    private static double MeanOfAbs(Func<double, double, double> f, double period, double x, int samples)
    {
        double sum = 0;
        for (int i = 0; i < samples; i++)
            sum += Math.Abs(f(x, i * period / samples));

        return sum / samples;
    }

    private static Expr? Phased(Expr e, string variable, string phase, double end, int direction, List<Expr> slopes)
    {
        if (!e.DependsOn(variable))
            return e;

        if (e is UnaryExpr { Argument: var argument } and (Sin or Cos or Tan or Cot or Sec or Csc))
        {
            if (Slope(argument, variable) is { } slope && !slope.DependsOn(variable))
            {
                if (e is not (Sin or Cos))
                    return null;

                slopes.Add(slope);
                Expr turned = argument.Substitute(variable, new Variable(phase));
                return e is Sin ? new Sin(turned) : new Cos(turned);
            }

            if (!Settles(argument, variable, end, direction))
                return null;
        }

        var children = e.Children;
        var changed = new Expr[children.Length];
        for (int i = 0; i < children.Length; i++)
            if (Phased(children[i], variable, phase, end, direction, slopes) is { } child)
                changed[i] = child;
            else
                return null;

        return e.WithChildren(changed);
    }

    private static bool Settles(Expr argument, string variable, double end, int direction)
    {
        double near = argument.Evaluate((variable, end + direction * 1e6)), far = argument.Evaluate((variable, end + direction * 1e12));
        return Math.Abs(far - near) <= SettledTurn;
    }

    // The largest slope of which all the slopes are whole multiples, at most MaxHarmonic of it.
    private static double? FundamentalSlope(List<Expr> slopes)
    {
        System.Numerics.BigInteger numerators = 0, denominators = 1;
        var ratios = new List<Rational>();
        foreach (Expr slope in slopes)
        {
            if ((slope / slopes[0]).Simplify() is not Constant { Value: { IsZero: false } ratio })
                return null;

            ratios.Add(ratio.Abs());
            numerators = System.Numerics.BigInteger.GreatestCommonDivisor(numerators, ratio.Numerator);
            denominators = denominators * ratio.Denominator / System.Numerics.BigInteger.GreatestCommonDivisor(denominators, ratio.Denominator);
        }

        var fundamental = new Rational(numerators, denominators);
        if (ratios.Any(ratio => ratio / fundamental > MaxHarmonic))
            return null;

        double value = Math.Abs(slopes[0].Evaluate()) * (double)fundamental.Numerator / (double)fundamental.Denominator;
        return double.IsFinite(value) && value > 0 ? value : null;
    }

    private static Expr? Slope(Expr u, string variable)
    {
        try
        {
            return u.Differentiate(variable);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}

// The integral of an oscillating integrand from start to the infinity in the direction (1 or
// -1): the integral of its mean over a period by exp-sinh, and of each part of the rest as the
// sum of its integrals over the steps where it keeps its sign. Those alternate and die out, and
// Wynn's epsilon algorithm finds their sum from the first few.
internal sealed class OscillatoryTail
{
    private const int MinPieces = 8;
    private const int MaxPieces = 120;
    private const double PieceTolerance = 1e-14;

    private readonly Oscillation _oscillation;
    private readonly double _start;
    private readonly int _direction;
    private readonly DoubleExponential _mean;

    // The integrals of each part from start to the ends of its steps so far.
    private readonly List<List<double>> _sums = [];
    private double _absoluteSum;

    public OscillatoryTail(Oscillation oscillation, double start, int direction)
    {
        _oscillation = oscillation;
        _start = start;
        _direction = direction;
        _mean = DoubleExponential.SemiInfinite(new IntegrandEnd(d => oscillation.Mean(start + direction * d), 0), start, direction);
        for (int i = 0; i < oscillation.Parts.Count; i++)
        {
            _sums.Add([0]);
            AddPiece(i);
            AddPiece(i);
        }
    }

    // The integral of |f| so far, to share the tolerance between the pieces.
    public double AbsoluteIntegral => _mean.AbsoluteIntegral + _absoluteSum;

    public double Converge(double tolerance, double floor)
    {
        if (!_mean.TryConverge(tolerance, floor, out double sum))
            throw _mean.NotConverged();

        double Allowed() => tolerance * Math.Max(AbsoluteIntegral, floor) / _sums.Count;

        // The integral up to x swings about its limit by about the amplitude times the period,
        // which must die out: summed, the waves of sin(x) would give 1.
        if (!(FarAmplitude() * _oscillation.Period <= tolerance * Math.Max(AbsoluteIntegral, floor)))
            throw new InvalidOperationException(
                $"The integral diverges at {(_direction > 0 ? "infinity" : "-infinity")}, or converges there too slowly for double precision.");

        // Once the estimates agree, each further step still gains a digit or so, until rounding
        // stops it.
        for (int i = 0; i < _sums.Count; i++)
        {
            List<double> sums = _sums[i], estimates = [];
            bool settled = false;
            while (true)
            {
                if (sums.Count > MinPieces)
                {
                    estimates.Add(Epsilon(sums));
                    if (settled && !(Math.Abs(estimates[^1] - estimates[^2]) < Math.Abs(estimates[^2] - estimates[^3])))
                        break;

                    settled |= estimates.Count >= 3 && Math.Abs(estimates[^1] - estimates[^2]) <= Allowed()
                        && Math.Abs(estimates[^2] - estimates[^3]) <= Allowed();
                }

                if (sums.Count > MaxPieces && settled)
                    break;
                if (sums.Count > MaxPieces)
                {
                    double low = _direction > 0 ? _start : double.NegativeInfinity, high = _direction > 0 ? double.PositiveInfinity : _start;
                    throw new InvalidOperationException(FormattableString.Invariant(
                        $"The integral over [{low}, {high}] didn't converge: the estimate {sum + estimates[^1]} may be off by {Math.Abs(estimates[^1] - estimates[^2])}."));
                }

                AddPiece(i);
            }

            sum += estimates[^1];
        }

        return sum;
    }

    // The integral of a part over its next step, by the distance d from the lower end, with the
    // phase counted from start, as the number of steps from it is known: far out x and the
    // length of the step in x are rounded to more than the phase may be.
    private void AddPiece(int part)
    {
        List<double> sums = _sums[part];
        int level = _oscillation.Parts[part], steps = 2 << level, i = sums.Count - 1;
        double step = _oscillation.Period / steps;
        int from = _direction > 0 ? i : -(i + 1);
        double low = _start + from * step;
        double phase = Math.IEEERemainder(_start, _oscillation.Period) + (from % steps + steps) % steps * step;
        var rule = DoubleExponential.Finite(
            new IntegrandEnd(d => _oscillation.Part(level, low + d, phase + d), 0),
            new IntegrandEnd(d => _oscillation.Part(level, low + (step - d), phase + (step - d)), 0), 0, step);
        if (!rule.TryConverge(PieceTolerance, 0, out double piece))
            throw rule.NotConverged();

        sums.Add(sums[^1] + piece);
        _absoluteSum += rule.AbsoluteIntegral;
    }

    // The amplitude as far out as the integrand is a number there.
    private double FarAmplitude()
    {
        foreach (double distance in new[] { 1e300, 1e200, 1e100, 1e50, 1e25, 1e12 })
            if (_oscillation.Amplitude(_start + _direction * distance) is var amplitude && double.IsFinite(amplitude))
                return amplitude;

        return double.NaN;
    }

    // The last entry of the last even column of the epsilon table of the partial sums.
    private static double Epsilon(List<double> sums)
    {
        double[] previous = new double[sums.Count], current = [.. sums];
        double estimate = current[^1];
        for (int k = 1; k < sums.Count; k++)
        {
            var next = new double[current.Length - 1];
            for (int j = 0; j < next.Length; j++)
            {
                double difference = current[j + 1] - current[j];
                if (difference == 0)
                    return k % 2 == 1 ? current[j + 1] : estimate;

                next[j] = previous[j + 1] + 1 / difference;
                if (!double.IsFinite(next[j]))
                    return estimate;
            }

            (previous, current) = (current, next);
            if (k % 2 == 0)
                estimate = current[^1];
        }

        return estimate;
    }
}
