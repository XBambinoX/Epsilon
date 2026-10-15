using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Calculus;

/// <summary>
/// Divergence and curl of vector fields: vectors of expressions whose entry i is the component
/// along the i-th of the variables.
/// </summary>
public static class VectorFieldExtensions
{
    // Classic extension methods rather than an extension block: with params there, the C# 14
    // compiler of SDK 10.0.1xx reports a false nullability warning (CS8620) at every call.

    /// <summary>
    /// The divergence: the sum of the partial derivatives of entry i by <c>variables[i]</c>,
    /// simplified. For <c>[x * y, y^2]</c> by <c>x</c> and <c>y</c> it is <c>3y</c>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The field and the variables have different lengths, or a name is listed twice.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// An entry contains a function whose derivative is not supported, as for
    /// <see cref="Expr.Differentiate(string)"/>.
    /// </exception>
    public static Expr Divergence(this Vector<Expr> field, params IReadOnlyList<string> variables)
    {
        string[] names = CheckedNames(field, variables);

        Expr sum = 0;
        for (int i = 0; i < names.Length; i++)
            sum += field[i].Differentiate(names[i]);

        return sum.Simplify();
    }

    /// <summary>
    /// The curl of a field in three dimensions, by the variables x, y, z in the order given:
    /// <c>[dFz/dy - dFy/dz, dFx/dz - dFz/dx, dFy/dx - dFx/dy]</c>, simplified. For
    /// <c>[-y, x, 0]</c> it is <c>[0, 0, 2]</c>. For a plane field <c>[P, Q]</c> take
    /// <c>[P, Q, 0]</c>: the last entry is <c>dQ/dx - dP/dy</c>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The field doesn't have 3 entries, the variables are not 3, or a name is listed twice.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// An entry contains a function whose derivative is not supported, as for
    /// <see cref="Expr.Differentiate(string)"/>.
    /// </exception>
    public static Vector<Expr> Curl(this Vector<Expr> field, params IReadOnlyList<string> variables)
    {
        string[] names = CheckedNames(field, variables);
        if (names.Length != 3)
            throw new ArgumentException($"The curl needs a field of 3 entries, not {names.Length}.", nameof(field));

        Expr D(int entry, int variable) => field[entry].Differentiate(names[variable]);

        return Vector.Create(
            (D(2, 1) - D(1, 2)).Simplify(),
            (D(0, 2) - D(2, 0)).Simplify(),
            (D(1, 0) - D(0, 1)).Simplify());
    }

    private static string[] CheckedNames(Vector<Expr> field, IReadOnlyList<string> variables)
    {
        ArgumentNullException.ThrowIfNull(field);
        string[] names = VariableNames.Checked(variables);

        if (field.Length != names.Length)
            throw new ArgumentException(
                $"The field has {field.Length} entries but there are {names.Length} variables.", nameof(variables));

        return names;
    }
}
