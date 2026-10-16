namespace Epsilon.Calculus;

internal static class VariableNames
{
    // The same checks and messages as Jacobian of Epsilon.LinearAlgebra.
    internal static string[] Checked(IReadOnlyList<string> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        string[] names = [.. variables];

        var seen = new HashSet<string>();
        foreach (string name in names)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(variables), "A variable name is null.");
            if (!seen.Add(name))
                throw new ArgumentException($"Variable '{name}' is listed twice.", nameof(variables));
        }

        return names;
    }
}
