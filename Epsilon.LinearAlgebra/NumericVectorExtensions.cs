namespace Epsilon.LinearAlgebra;

/// <summary>Arithmetic, norms, angles and projections of vectors of <see cref="double"/>.</summary>
public static class NumericVectorExtensions
{
    extension(Vector<double> vector)
    {
        /// <summary>The vector of <paramref name="length"/> zeros.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        public static Vector<double> Zero(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            return new Vector<double>(new double[length]);
        }

        /// <summary>
        /// The point at <paramref name="t"/> on the line through two points: (1 - t) * start + t * end,
        /// so <c>start</c> at t = 0 and <c>end</c> at t = 1; other values extrapolate.
        /// </summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<double> Lerp(Vector<double> start, Vector<double> end, double t) =>
            start.Combine(end, (a, b) => double.Lerp(a, b, t), "interpolate between");

        /// <summary>The dot product: the sum of the products of the entries at the same index.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public double Dot(Vector<double> other)
        {
            vector.CheckSameLength(other, "multiply");

            double sum = 0;
            for (int i = 0; i < vector.Length; i++)
                sum += vector[i] * other[i];

            return sum;
        }

        /// <summary>
        /// The Euclidean norm (length) sqrt(v . v). It is the naive sum of squares where that one
        /// is accurate, but neither overflows for huge entries nor underflows for tiny ones.
        /// </summary>
        public double Norm() => EuclideanNorm(vector.ToMatrix().Entries);

        /// <summary>The squared norm v . v; cheaper than <see cref="Norm"/> for comparing lengths.</summary>
        public double NormSquared() => vector.Dot(vector);

        /// <summary>The unit vector in the same direction, accurate for huge and tiny entries too.</summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        public Vector<double> Normalize()
        {
            Vector<double> scaled = Scaled(vector);
            double norm = scaled.Norm();
            if (norm == 0)
                throw new InvalidOperationException("Cannot normalize the zero vector.");

            return scaled / norm;
        }

        /// <summary>The Euclidean distance between two points: the norm of their difference.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public double Distance(Vector<double> other)
        {
            vector.CheckSameLength(other, "measure the distance between");
            return (vector - other).Norm();
        }

        /// <summary>The cross product of two vectors of length 3, perpendicular to both.</summary>
        /// <exception cref="InvalidOperationException">The vector has not 3 entries.</exception>
        /// <exception cref="ArgumentException"><paramref name="other"/> has not 3 entries.</exception>
        public Vector<double> Cross(Vector<double> other)
        {
            CheckCrossLengths(vector.Length, other.Length);
            return
            [
                vector[1] * other[2] - vector[2] * other[1],
                vector[2] * other[0] - vector[0] * other[2],
                vector[0] * other[1] - vector[1] * other[0],
            ];
        }

        /// <summary>
        /// The angle between two vectors in radians, from 0 to pi. Kahan's formula
        /// 2 atan2(|a - b|, |a + b|) of the unit vectors a and b keeps it accurate at every angle,
        /// where the arccosine of the cosine loses small angles and those near pi.
        /// </summary>
        /// <exception cref="InvalidOperationException">The vector is zero.</exception>
        /// <exception cref="ArgumentException">The vectors have different lengths, or <paramref name="other"/> is zero.</exception>
        public double Angle(Vector<double> other)
        {
            vector.CheckSameLength(other, "measure the angle between");
            if (IsZero(vector))
                throw new InvalidOperationException(UndefinedAngle);
            if (IsZero(other))
                throw new ArgumentException(UndefinedAngle, nameof(other));

            Vector<double> a = vector.Normalize(), b = other.Normalize();
            return 2 * Math.Atan2((a - b).Norm(), (a + b).Norm());
        }

        /// <summary>
        /// The projection onto the line through <paramref name="onto"/>: (v . w / w . w) * w, the
        /// part of the vector in that direction.
        /// </summary>
        /// <exception cref="ArgumentException">The vectors have different lengths, or <paramref name="onto"/> is zero.</exception>
        public Vector<double> ProjectOnto(Vector<double> onto)
        {
            vector.CheckSameLength(onto, "project");

            // Scaling is exact and keeps w . w away from overflow and underflow.
            Vector<double> direction = Scaled(onto);
            double squared = direction.NormSquared();
            if (squared == 0)
                throw new ArgumentException("Cannot project onto the zero vector.", nameof(onto));

            return vector.Dot(direction) / squared * direction;
        }

        /// <summary>
        /// The mirror image in the plane through the origin perpendicular to <paramref name="normal"/>:
        /// v - 2 (v . n / n . n) * n, as a velocity bounces off a wall. The normal need not have length 1.
        /// </summary>
        /// <exception cref="ArgumentException">The vectors have different lengths, or <paramref name="normal"/> is zero.</exception>
        public Vector<double> Reflect(Vector<double> normal)
        {
            vector.CheckSameLength(normal, "reflect");

            Vector<double> direction = Scaled(normal);
            double squared = direction.NormSquared();
            if (squared == 0)
                throw new ArgumentException("The normal is the zero vector.", nameof(normal));

            return vector - 2 * vector.Dot(direction) / squared * direction;
        }

        /// <summary>The outer product v w^T: the matrix whose entry at (i, j) is v[i] * w[j].</summary>
        public Matrix<double> Outer(Vector<double> other) =>
            Matrix<double>.Create(vector.Length, other.Length, (i, j) => vector[i] * other[j]);

        /// <summary>The Hadamard product: the products of the entries at the same index, as when scaling by a vector.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public Vector<double> Hadamard(Vector<double> other) =>
            vector.Combine(other, (a, b) => a * b, "take the Hadamard product of");

        /// <summary>The entry-wise sum.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<double> operator +(Vector<double> left, Vector<double> right) =>
            left.Combine(right, (a, b) => a + b, "add");

        /// <summary>The entry-wise difference.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<double> operator -(Vector<double> left, Vector<double> right) =>
            left.Combine(right, (a, b) => a - b, "subtract");

        /// <summary>Every entry negated.</summary>
        public static Vector<double> operator -(Vector<double> value) => value.Map(a => -a);

        /// <summary>Every entry multiplied by the number.</summary>
        public static Vector<double> operator *(double scalar, Vector<double> value) => value.Map(a => scalar * a);

        /// <summary>Every entry multiplied by the number.</summary>
        public static Vector<double> operator *(Vector<double> value, double scalar) => value.Map(a => a * scalar);

        /// <summary>Every entry divided by the number.</summary>
        public static Vector<double> operator /(Vector<double> value, double scalar) => value.Map(a => a / scalar);
    }

    internal const string UndefinedAngle = "The angle with the zero vector is undefined.";

    // As with Solve, a receiver of the wrong size is an invalid operation and an argument of the
    // wrong size an invalid argument.
    internal static void CheckCrossLengths(int length, int otherLength)
    {
        if (length != 3)
            throw new InvalidOperationException($"The cross product needs vectors of length 3, not {length}.");
        if (otherLength != 3)
            throw new ArgumentException($"The cross product needs vectors of length 3, not {otherLength}.", "other");
    }

    // sqrt of the sum of squares, after scaling by a power of two (see Scaled): the naive result
    // wherever that one neither overflows nor underflows.
    internal static double EuclideanNorm(ReadOnlySpan<double> entries)
    {
        int exponent = ScaleExponent(entries);

        double sum = 0;
        foreach (double entry in entries)
        {
            double scaled = Math.ScaleB(entry, -exponent);
            sum += scaled * scaled;
        }

        return Math.ScaleB(Math.Sqrt(sum), exponent);
    }

    private static bool IsZero(Vector<double> vector) => vector.All(entry => entry == 0);

    // The vector divided by a power of two that brings its largest magnitude into [1, 2). The
    // division is exact, so the direction stays the same, while sums of squares of the entries
    // can neither overflow nor underflow.
    private static Vector<double> Scaled(Vector<double> vector)
    {
        int exponent = ScaleExponent(vector.ToMatrix().Entries);
        return exponent == 0 ? vector : vector.Map(a => Math.ScaleB(a, -exponent));
    }

    // The binary exponent of the largest magnitude; 0 if all entries are zero or one is infinite
    // or NaN, which then pass through the arithmetic unscaled.
    private static int ScaleExponent(ReadOnlySpan<double> entries)
    {
        double largest = 0;
        foreach (double entry in entries)
            largest = Math.Max(largest, Math.Abs(entry));

        return largest == 0 || !double.IsFinite(largest) ? 0 : Math.ILogB(largest);
    }
}
