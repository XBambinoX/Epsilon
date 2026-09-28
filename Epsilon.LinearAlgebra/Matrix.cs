namespace Epsilon.LinearAlgebra;

/// <summary>
/// A generic N × M matrix.
/// </summary>
public sealed class Matrix<T>
{
    private readonly T[,] _values;

    /// <summary>
    /// The variable names labelling both rows and columns of a square matrix such as a Hessian;
    /// empty for a matrix created by size.
    /// </summary>
    public IReadOnlyList<string> Variables { get; }
    /// <summary>The number of labelling <see cref="Variables"/>: the side length of a labelled matrix, 0 for one created by size.</summary>
    public int Size => Variables.Count;

    /// <summary>The number of rows.</summary>
    public int Rows => _values.GetLength(0);
    /// <summary>The number of columns.</summary>
    public int Columns => _values.GetLength(1);

    /// <summary>A square matrix whose rows and columns are labelled by <paramref name="variables"/>. Uses <paramref name="values"/> without copying.</summary>
    /// <exception cref="ArgumentException"><paramref name="values"/> is not variables.Count × variables.Count.</exception>
    public Matrix(IReadOnlyList<string> variables, T[,] values)
    {
        if (values.GetLength(0) != variables.Count || values.GetLength(1) != variables.Count)
            throw new ArgumentException("Matrix dimensions must match the number of variables.");

        Variables = variables;
        _values = values;
    }

    /// <summary>An unlabelled <paramref name="rows"/> × <paramref name="columns"/> matrix of default values.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is negative.</exception>
    public Matrix(int rows, int columns)
    {
        if (rows < 0)
            throw new ArgumentOutOfRangeException(nameof(rows));

        if (columns < 0)
            throw new ArgumentOutOfRangeException(nameof(columns));

        _values = new T[rows, columns];
        Variables = Array.Empty<string>();
    }

    /// <summary>The entry at the row and column labelled by the given variables.</summary>
    /// <exception cref="ArgumentException">A name is not one of <see cref="Variables"/>.</exception>
    public T this[string row, string col]
    {
        get => _values[IndexOf(row), IndexOf(col)];
        set => _values[IndexOf(row), IndexOf(col)] = value;
    }

    /// <summary>The entry at the zero-based row and column.</summary>
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

    /// <inheritdoc/>
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