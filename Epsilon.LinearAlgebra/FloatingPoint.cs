namespace Epsilon.LinearAlgebra;

// Constants and checks shared by the floating-point operations of the fixed-size types.
internal static class FloatingPoint<T> where T : struct, System.Numerics.IFloatingPointIeee754<T>
{
    // The gap between 1 and the next number: 2^-52 for double, 2^-23 for float, 2^-10 for Half.
    // (T.Epsilon is something else: the smallest positive subnormal number.)
    private static readonly T MachineEpsilon = T.BitIncrement(T.One) - T.One;

    // The smallest sum of at most four squares that is accurate to rounding. A square below the
    // smallest normal number keeps only part of its bits and is off by up to T.Epsilon / 2; four
    // such errors stay below a quarter of the last bit of a sum from 8 T.Epsilon / MachineEpsilon
    // on, which is 2^-1019 for double.
    private static readonly T SafeMinimum = T.Epsilon / MachineEpsilon * T.CreateTruncating(8);

    internal static readonly T Two = T.One + T.One;

    // The relative tolerance of IsApproximately when none is given: 1e-9 for double, as for
    // Vector<double>, 1e-5 for float and 1e-2 for Half.
    internal static readonly T DefaultTolerance = T.CreateTruncating(double.CreateTruncating(MachineEpsilon) switch
    {
        <= 1e-15 => 1e-9,
        <= 1e-6 => 1e-5,
        _ => 1e-2,
    });

    // Whether a sum of squares neither overflowed nor lost accuracy to underflow, so that its
    // square root is the norm.
    internal static bool IsSafe(T sumOfSquares) => T.IsFinite(sumOfSquares) && sumOfSquares >= SafeMinimum;

    // The binary exponent of the largest magnitude; dividing by 2 to its power is exact and brings
    // that magnitude into [1, 2). 0 if it is zero, infinite or NaN, which then pass through the
    // arithmetic unscaled.
    internal static int ScaleExponent(T largest)
    {
        T magnitude = T.Abs(largest);
        return magnitude == T.Zero || !T.IsFinite(magnitude) ? 0 : T.ILogB(magnitude);
    }

    // The tolerances of IsApproximately, with the defaults in place of null.
    internal static (T Relative, T Absolute) Tolerances(T? relativeTolerance, T? absoluteTolerance)
    {
        T relative = relativeTolerance ?? DefaultTolerance;
        T absolute = absoluteTolerance ?? T.Zero;
        CheckTolerance(relative, nameof(relativeTolerance));
        CheckTolerance(absolute, nameof(absoluteTolerance));
        return (relative, absolute);
    }

    // Splits a pair of entries for IsApproximately. Infinite entries must be equal and count as 0
    // in the norms, where they would make every finite difference look small; NaN is never close
    // to anything.
    internal static bool FiniteParts(T left, T right, out T difference, out T finiteLeft, out T finiteRight)
    {
        if (T.IsFinite(left) && T.IsFinite(right))
        {
            difference = left - right;
            finiteLeft = left;
            finiteRight = right;
            return true;
        }

        difference = finiteLeft = finiteRight = T.Zero;
        return left == right;   // false when either is NaN
    }

    // The test of IsApproximately on the norms of the finite parts.
    internal static bool IsClose(T differenceNorm, T leftNorm, T rightNorm, T relative, T absolute) =>
        differenceNorm <= T.Max(relative * T.Max(leftNorm, rightNorm), absolute);

    private static void CheckTolerance(T tolerance, string paramName)
    {
        if (!(tolerance >= T.Zero))
            throw new ArgumentOutOfRangeException(paramName, tolerance, "A tolerance must be a number >= 0.");
    }
}
