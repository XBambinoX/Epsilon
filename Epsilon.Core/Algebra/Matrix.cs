namespace Epsilon.Core;

/// <summary>
/// A generic N × M matrix.
/// </summary>
public sealed class Matrix<T>
{
    private readonly T[,] _values;

    public IReadOnlyList<string> Variables { get; }
    public int Size => Variables.Count;

    public int Rows => _values.GetLength(0);
    public int Columns => _values.GetLength(1);

    public Matrix(IReadOnlyList<string> variables, T[,] values)
    {
        if (values.GetLength(0) != variables.Count || values.GetLength(1) != variables.Count)
            throw new ArgumentException("Matrix dimensions must match the number of variables.");

        Variables = variables;
        _values = values;
    }

    public Matrix(int rows, int columns)
    {
        if (rows < 0)
            throw new ArgumentOutOfRangeException(nameof(rows));

        if (columns < 0)
            throw new ArgumentOutOfRangeException(nameof(columns));

        _values = new T[rows, columns];
        Variables = Array.Empty<string>();
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
        var rows = new string[Rows];
        for (int i = 0; i < Rows; i++)
        {
            var cells = new string[Columns];
            for (int j = 0; j < Columns; j++)
                cells[j] = _values[i, j]?.ToString() ?? "null";
            rows[i] = string.Join("  ", cells);
        }
        return string.Join("\n", rows);
    }
}