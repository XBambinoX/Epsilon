using Epsilon.Core;

namespace Epsilon.Solving;

internal static class NumericRoots
{
    private const int MaxIterations = 500;

    // All roots of the polynomial with these coefficients, from the constant term up, at once by
    // the Aberth-Ehrlich method; null if they don't settle. Repeated roots settle slowly and
    // inexactly, so the polynomial shouldn't have any.
    internal static ComplexNumber[]? Of(ComplexNumber[] p)
    {
        int n = p.Length - 1;

        // Every root lies within Cauchy's bound; the start is on that circle, off the axes, so that
        // no two guesses are conjugate.
        double radius = 1 + p[..n].Max(coefficient => (coefficient / p[n]).Magnitude);
        var z = new ComplexNumber[n];
        for (int k = 0; k < n; k++)
            z[k] = ComplexNumber.FromPolar(radius, 2 * Math.PI * k / n + 0.4);

        double largest = double.PositiveInfinity;
        for (int iteration = 0; iteration < MaxIterations && largest > 1e-15; iteration++)
        {
            largest = 0;
            for (int k = 0; k < n; k++)
            {
                var (value, slope) = Horner(p, z[k]);
                if (value == ComplexNumber.Zero)
                    continue;

                ComplexNumber newton = value / slope, repulsion = ComplexNumber.Zero;
                for (int j = 0; j < n; j++)
                    if (j != k)
                        repulsion += ComplexNumber.One / (z[k] - z[j]);
                ComplexNumber step = newton / (ComplexNumber.One - newton * repulsion);
                z[k] -= step;
                largest = Math.Max(largest, step.Magnitude / Math.Max(1, z[k].Magnitude));
            }
        }

        // Rounding can keep the last steps from getting smaller than 1e-15.
        return largest < 1e-9 ? z : null;
    }

    private static (ComplexNumber Value, ComplexNumber Slope) Horner(ComplexNumber[] p, ComplexNumber z)
    {
        ComplexNumber value = p[^1], slope = ComplexNumber.Zero;
        for (int k = p.Length - 2; k >= 0; k--)
        {
            slope = slope * z + value;
            value = value * z + p[k];
        }

        return (value, slope);
    }
}
