namespace Epsilon.LinearAlgebra;

/// <summary>Arithmetic on vectors of <see cref="double"/>.</summary>
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
}
