using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

/// <summary>
/// Exact arithmetic, dot and cross products and projections of vectors of <see cref="Rational"/>.
/// The norm, normalization and angles lead out of the rationals (the norm of [1, 1] is sqrt(2)),
/// so they are left to vectors of <see cref="double"/> and <see cref="Expr"/>.
/// </summary>
public static class RationalVectorExtensions
{
    extension(Vector<Rational> vector)
    {
        /// <summary>The vector of <paramref name="length"/> zeros.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        public static Vector<Rational> Zero(int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            return new Vector<Rational>(Enumerable.Repeat(Rational.Zero, length).ToArray());
        }

        /// <summary>The point at <paramref name="t"/> on the line through two points: (1 - t) * start + t * end.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<Rational> Lerp(Vector<Rational> start, Vector<Rational> end, Rational t) =>
            start.Combine(end, (a, b) => (1 - t) * a + t * b, "interpolate between");

        /// <summary>The exact dot product: the sum of the products of the entries at the same index.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public Rational Dot(Vector<Rational> other)
        {
            vector.CheckSameLength(other, "multiply");

            Rational sum = Rational.Zero;
            for (int i = 0; i < vector.Length; i++)
                sum += vector[i] * other[i];

            return sum;
        }

        /// <summary>The squared norm v . v, exact; the norm itself is not rational in general.</summary>
        public Rational NormSquared() => vector.Dot(vector);

        /// <summary>The cross product of two vectors of length 3, perpendicular to both.</summary>
        /// <exception cref="InvalidOperationException">The vector has not 3 entries.</exception>
        /// <exception cref="ArgumentException"><paramref name="other"/> has not 3 entries.</exception>
        public Vector<Rational> Cross(Vector<Rational> other)
        {
            NumericVectorExtensions.CheckCrossLengths(vector.Length, other.Length);
            return
            [
                vector[1] * other[2] - vector[2] * other[1],
                vector[2] * other[0] - vector[0] * other[2],
                vector[0] * other[1] - vector[1] * other[0],
            ];
        }

        /// <summary>The exact projection (v . w / w . w) * w onto the line through <paramref name="onto"/>.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths, or <paramref name="onto"/> is zero.</exception>
        public Vector<Rational> ProjectOnto(Vector<Rational> onto)
        {
            vector.CheckSameLength(onto, "project");

            Rational squared = onto.NormSquared();
            if (squared.IsZero)
                throw new ArgumentException("Cannot project onto the zero vector.", nameof(onto));

            return vector.Dot(onto) / squared * onto;
        }

        /// <summary>
        /// The exact mirror image v - 2 (v . n / n . n) * n in the plane through the origin
        /// perpendicular to <paramref name="normal"/>, which need not have length 1.
        /// </summary>
        /// <exception cref="ArgumentException">The vectors have different lengths, or <paramref name="normal"/> is zero.</exception>
        public Vector<Rational> Reflect(Vector<Rational> normal)
        {
            vector.CheckSameLength(normal, "reflect");

            Rational squared = normal.NormSquared();
            if (squared.IsZero)
                throw new ArgumentException("The normal is the zero vector.", nameof(normal));

            return vector - 2 * vector.Dot(normal) / squared * normal;
        }

        /// <summary>The outer product v w^T: the matrix whose entry at (i, j) is v[i] * w[j].</summary>
        public Matrix<Rational> Outer(Vector<Rational> other) =>
            Matrix<Rational>.Create(vector.Length, other.Length, (i, j) => vector[i] * other[j]);

        /// <summary>The Hadamard product: the products of the entries at the same index.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public Vector<Rational> Hadamard(Vector<Rational> other) =>
            vector.Combine(other, (a, b) => a * b, "take the Hadamard product of");

        /// <summary>The entry-wise sum.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<Rational> operator +(Vector<Rational> left, Vector<Rational> right) =>
            left.Combine(right, (a, b) => a + b, "add");

        /// <summary>The entry-wise difference.</summary>
        /// <exception cref="ArgumentException">The vectors have different lengths.</exception>
        public static Vector<Rational> operator -(Vector<Rational> left, Vector<Rational> right) =>
            left.Combine(right, (a, b) => a - b, "subtract");

        /// <summary>Every entry negated.</summary>
        public static Vector<Rational> operator -(Vector<Rational> value) => value.Map(a => -a);

        /// <summary>Every entry multiplied by the number.</summary>
        public static Vector<Rational> operator *(Rational scalar, Vector<Rational> value) => value.Map(a => scalar * a);

        /// <summary>Every entry multiplied by the number.</summary>
        public static Vector<Rational> operator *(Vector<Rational> value, Rational scalar) => value.Map(a => a * scalar);

        /// <summary>Every entry divided by the number.</summary>
        /// <exception cref="DivideByZeroException"><paramref name="scalar"/> is 0.</exception>
        public static Vector<Rational> operator /(Vector<Rational> value, Rational scalar)
        {
            if (scalar.IsZero)
                throw new DivideByZeroException("Cannot divide a vector by 0.");

            return value.Map(a => a / scalar);
        }
    }
}
