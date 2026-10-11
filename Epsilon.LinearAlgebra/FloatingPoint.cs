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

    // The binary exponent of the smallest normal number: -1022 for double.
    private static readonly int MinNormalExponent = T.ILogB(T.Epsilon) - T.ILogB(MachineEpsilon);

    // A sixth of the largest binary exponent, which is 1 - MinNormalExponent in IEEE 754: 170 for
    // double, 21 for float, 2 for Half. With entries up to 2 to this power, no product of four of
    // them, nor a sum of 24 such products, overflows; and when the sum of the magnitudes of the
    // terms of a determinant is at least 2 to its negative, products that underflow change no
    // result by more than rounding.
    private static readonly int SafeExponent = (1 - MinNormalExponent) / 6;

    internal static readonly T Two = T.One + T.One;

    // The relative tolerance of IsApproximately when none is given: 1e-9 for double, as for
    // Vector<double>, 1e-5 for float and 1e-2 for Half.
    internal static readonly T DefaultTolerance = T.CreateTruncating(double.CreateTruncating(MachineEpsilon) switch
    {
        <= 1e-15 => 1e-9,
        <= 1e-6 => 1e-5,
        _ => 1e-2,
    });

    // The largest entry magnitude for the closed-form inverse without scaling; see SafeExponent.
    internal static readonly T SafeLargest = T.ScaleB(T.One, SafeExponent);

    // The smallest sum of the magnitudes of the terms of a determinant for the closed-form inverse
    // without scaling; see SafeExponent.
    internal static readonly T SafeSmallest = T.ScaleB(T.One, -SafeExponent);

    // First-order bounds on the rounding error of the closed-form determinants, as multiples of the
    // machine epsilon times the sum of the magnitudes of their terms: 5 for the cofactor expansion
    // of a 3 x 3 matrix (2 roundings in a minor, 1 in the product, 2 in the sum) and 10 for the
    // Laplace expansion of a 4 x 4 one (2 in each of two minors, 1 in their product, 5 in the sum).
    internal static readonly T DeterminantError3 = T.CreateTruncating(5) * MachineEpsilon;
    internal static readonly T DeterminantError4 = T.CreateTruncating(10) * MachineEpsilon;

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

    // The power of two that brings a row with this largest magnitude into [1, 2) when multiplied
    // by it, which is exact. For a subnormal row it stops at the smallest normal exponent, where
    // the factor would overflow.
    internal static T RowScale(T largest) =>
        T.ScaleB(T.One, -Math.Max(ScaleExponent(largest), MinNormalExponent));

    // Whether a determinant is 0 to working precision: not larger than the bound on its rounding
    // error, errorBound times the sum of the magnitudes of its terms (the permanent of |A|). So a
    // singular matrix whose determinant comes out as 1e-17 instead of 0 counts as singular, while
    // diag(1e-20, 1, 1), where nothing cancels, does not. NaN counts as 0.
    internal static bool IsNegligible(T determinant, T permanent, T errorBound) =>
        !(T.Abs(determinant) > errorBound * permanent);

    // Whether two lists of entries are equal up to rounding, measured by Euclidean norms:
    // |a - b| <= max(relative * max(|a|, |b|), absolute). Infinite entries must be equal and count
    // as 0 in the norms, where they would make every finite difference look small; NaN is never
    // close to anything.
    internal static bool AreClose(ReadOnlySpan<T> left, ReadOnlySpan<T> right, T? relativeTolerance, T? absoluteTolerance)
    {
        T relative = relativeTolerance ?? DefaultTolerance;
        T absolute = absoluteTolerance ?? T.Zero;
        CheckTolerance(relative, nameof(relativeTolerance));
        CheckTolerance(absolute, nameof(absoluteTolerance));

        // The largest magnitudes first, to scale the sums of squares by (see ScaleExponent).
        T largestDifference = T.Zero, largestLeft = T.Zero, largestRight = T.Zero;
        for (int k = 0; k < left.Length; k++)
        {
            if (T.IsFinite(left[k]) && T.IsFinite(right[k]))
            {
                largestDifference = T.MaxMagnitude(largestDifference, left[k] - right[k]);
                largestLeft = T.MaxMagnitude(largestLeft, left[k]);
                largestRight = T.MaxMagnitude(largestRight, right[k]);
            }
            else if (left[k] != right[k])   // also when either is NaN
            {
                return false;
            }
        }

        int differenceExponent = ScaleExponent(largestDifference);
        int leftExponent = ScaleExponent(largestLeft), rightExponent = ScaleExponent(largestRight);
        T differenceSum = T.Zero, leftSum = T.Zero, rightSum = T.Zero;
        for (int k = 0; k < left.Length; k++)
        {
            if (!T.IsFinite(left[k]) || !T.IsFinite(right[k]))
                continue;

            T difference = T.ScaleB(left[k] - right[k], -differenceExponent);
            T l = T.ScaleB(left[k], -leftExponent), r = T.ScaleB(right[k], -rightExponent);
            differenceSum += difference * difference;
            leftSum += l * l;
            rightSum += r * r;
        }

        T differenceNorm = T.ScaleB(T.Sqrt(differenceSum), differenceExponent);
        T scale = T.Max(T.ScaleB(T.Sqrt(leftSum), leftExponent), T.ScaleB(T.Sqrt(rightSum), rightExponent));
        return differenceNorm <= T.Max(relative * scale, absolute);
    }

    private static void CheckTolerance(T tolerance, string paramName)
    {
        if (!(tolerance >= T.Zero))
            throw new ArgumentOutOfRangeException(paramName, tolerance, "A tolerance must be a number >= 0.");
    }
}
