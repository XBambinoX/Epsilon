using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Calculus;

/// <summary>
/// Divergence, curl and potentials of vector fields: vectors of expressions whose entry i is the
/// component along the i-th of the variables.
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

    /// <summary>
    /// A potential of the field: a function whose gradient by the variables is the field,
    /// simplified, without a constant. For <c>[2x * y, x^2]</c> by <c>x</c> and <c>y</c> it is
    /// <c>x^2 * y</c>. The field is a gradient where the derivative of entry i by
    /// <c>variables[j]</c> is that of entry j by <c>variables[i]</c>; then the potential is the
    /// integral of entry 0 by the first variable, plus that of what entry 1 adds by the second,
    /// and so on. It holds where the field is defined around the curve: [-y, x]/(x^2 + y^2) has
    /// equal derivatives but no potential around 0.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The field and the variables have different lengths, or a name is listed twice.
    /// </exception>
    /// <exception cref="InvalidOperationException">The field isn't a gradient: two of those derivatives differ.</exception>
    /// <exception cref="NotSupportedException">
    /// An antiderivative isn't found, or whether two of the derivatives agree isn't decided, as
    /// with other variables in them.
    /// </exception>
    public static Expr Potential(this Vector<Expr> field, params IReadOnlyList<string> variables)
    {
        string[] names = CheckedNames(field, variables);
        for (int i = 0; i < names.Length; i++)
            for (int j = i + 1; j < names.Length; j++)
            {
                Expr difference = field[i].Differentiate(names[j]) - field[j].Differentiate(names[i]);
                if (Vanishes(difference, names) is not { } vanishes)
                    throw new NotSupportedException(
                        $"Whether the derivative of {field[i].Print()} by {names[j]} is that of {field[j].Print()} by {names[i]} isn't decided.");
                if (!vanishes)
                    throw new InvalidOperationException(
                        $"The field {field.Print()} isn't a gradient: the derivative of {field[i].Print()} by {names[j]} isn't that of {field[j].Print()} by {names[i]}.");
            }

        // What entry k adds is independent of the variables before it, which a simplifier that
        // leaves it in a form with them is told by setting them to a point where it is defined.
        Expr potential = 0;
        for (int k = 0; k < names.Length; k++)
        {
            Expr rest = (field[k] - potential.Differentiate(names[k])).Simplify();
            if (names.Take(k).Any(rest.DependsOn))
                rest = rest.Expand().Simplify();
            if (names.Take(k).Any(rest.DependsOn))
                rest = Fixed(rest, names.Take(k)) ?? throw new NotSupportedException(
                    $"What {field[k].Print()} adds to the potential by {names[k]} isn't found free of {string.Join(", ", names.Take(k))}.");

            potential = (potential + rest.Integrate(names[k])).Simplify();
        }

        return potential;
    }

    // Whether e is 0: true or false where it simplifies to 0 or is told by its values at a few
    // points, null where it has other variables than names and doesn't simplify.
    private static bool? Vanishes(Expr e, string[] names)
    {
        if (e.Simplify() is Constant { Value.IsZero: true } || e.Expand().Simplify() is Constant { Value.IsZero: true })
            return true;
        if (e.GetVariables().Any(name => !names.Contains(name)))
            return null;

        int defined = 0;
        for (int k = 0; k < 8; k++)
        {
            double value = e.Evaluate([.. names.Select((name, i) => (name, Sample(i, k)))]);
            if (!double.IsFinite(value))
                continue;
            if (Math.Abs(value) > 1e-9)
                return false;
            defined++;
        }

        return defined >= 3 ? true : null;
    }

    private static double Sample(int variable, int point) => 0.3 + 0.37 * point + 0.23 * variable * (point + 1);

    // e with the names set to 0, or to 1 where it isn't defined at 0.
    private static Expr? Fixed(Expr e, IEnumerable<string> names)
    {
        foreach (int value in new[] { 0, 1 })
        {
            Expr fixedAt = names.Aggregate(e, (current, name) => current.Substitute(name, value));
            try
            {
                fixedAt = fixedAt.Simplify();
                if (!fixedAt.GetVariables().Any() || double.IsFinite(fixedAt.Evaluate([.. fixedAt.GetVariables().Select(name => (name, 0.5))])))
                    return fixedAt;
            }
            catch (ArithmeticException)
            {
            }
        }

        return null;
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
