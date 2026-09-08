namespace Epsilon.Core;

/// <summary>
/// A minimal square matrix indexed by variable name pairs, generic over the cell type.
/// Plain data container — no linear algebra operations (determinant,
/// inverse, multiplication) yet; those can be added later as needed.
/// </summary>
public sealed class Matrix<T>
{
    private readonly T[,] _values;

    public IReadOnlyList<string> Variables { get; }
    public int Size => Variables.Count;

    public Matrix(IReadOnlyList<string> variables, T[,] values)
    {
        if (values.GetLength(0) != variables.Count || values.GetLength(1) != variables.Count)
            throw new ArgumentException("Matrix dimensions must match the number of variables.");

        Variables = variables;
        _values = values;
    }

    public T this[string row, string col]
    {
        get => _values[IndexOf(row), IndexOf(col)];
        set => _values[IndexOf(row), IndexOf(col)] = value;
    }

    public T this[int row, int col]
    {
        get => _values[row, col];
        set => _values[row, col] = value;
    }

    private int IndexOf(string variable)
    {
        for (int i = 0; i < Variables.Count; i++)
            if (Variables[i] == variable)
                return i;

        throw new ArgumentException($"'{variable}' is not one of the matrix's variables.", nameof(variable));
    }

    public override string ToString()
    {
        var rows = new string[Size];
        for (int i = 0; i < Size; i++)
        {
            var cells = new string[Size];
            for (int j = 0; j < Size; j++)
                cells[j] = _values[i, j]?.ToString() ?? "null";
            rows[i] = string.Join("  ", cells);
        }
        return string.Join("\n", rows);
    }
}