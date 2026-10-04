namespace Epsilon.LinearAlgebra;

/// <summary>
/// An immutable matrix of <see cref="Rows"/> x <see cref="Columns"/> entries. The arithmetic is
/// defined separately for each entry type, so only the types it is defined for support it.
/// </summary>
/// <typeparam name="T">The type of the entries.</typeparam>
public sealed class Matrix<T> : IEquatable<Matrix<T>> where T : notnull
{
    // Row-major: the entry at (row, column) is _entries[row * Columns + column].
    private readonly T[] _entries;

    /// <summary>The number of rows.</summary>
    public int Rows { get; }

    /// <summary>The number of columns.</summary>
    public int Columns { get; }

    /// <summary>Whether the matrix has as many rows as columns.</summary>
    public bool IsSquare => Rows == Columns;

    /// <summary>A matrix with the entries of a two-dimensional array, which is copied.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> or one of its entries is null.</exception>
    public Matrix(T[,] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Rows = entries.GetLength(0);
        Columns = entries.GetLength(1);
        _entries = new T[Rows * Columns];

        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                _entries[i * Columns + j] = NotNull(entries[i, j], i, j);
    }

    // Takes ownership of the row-major entries without copying or checking them.
    internal Matrix(int rows, int columns, T[] entries)
    {
        Rows = rows;
        Columns = columns;
        _entries = entries;
    }

    /// <summary>A matrix from its rows, which are copied: <c>Matrix&lt;double&gt;.FromRows([1, 2], [3, 4])</c>.</summary>
    /// <exception cref="ArgumentNullException">A row or an entry is null.</exception>
    /// <exception cref="ArgumentException">The rows have different lengths.</exception>
    public static Matrix<T> FromRows(params T[][] rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        int columns = rows.Length == 0 ? 0 : RowAt(rows, 0).Length;
        var entries = new T[rows.Length * columns];

        for (int i = 0; i < rows.Length; i++)
        {
            T[] row = RowAt(rows, i);
            if (row.Length != columns)
                throw new ArgumentException($"Row {i} has {row.Length} entries, but row 0 has {columns}.", nameof(rows));

            for (int j = 0; j < columns; j++)
                entries[i * columns + j] = NotNull(row[j], i, j);
        }

        return new Matrix<T>(rows.Length, columns, entries);
    }

    /// <summary>A <paramref name="rows"/> x <paramref name="columns"/> matrix whose entry at (i, j) is <c>entry(i, j)</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is null or returns null.</exception>
    public static Matrix<T> Create(int rows, int columns, Func<int, int, T> entry)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rows);
        ArgumentOutOfRangeException.ThrowIfNegative(columns);
        ArgumentNullException.ThrowIfNull(entry);

        var entries = new T[checked(rows * columns)];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < columns; j++)
                entries[i * columns + j] = NotNull(entry(i, j), i, j);

        return new Matrix<T>(rows, columns, entries);
    }

    /// <summary>The entry at the zero-based row and column.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The row or the column is outside the matrix.</exception>
    public T this[int row, int column]
    {
        get
        {
            CheckRow(row);
            CheckColumn(column);
            return _entries[row * Columns + column];
        }
    }

    /// <summary>The entries of a row, as a vector.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The row is outside the matrix.</exception>
    public Vector<T> Row(int row)
    {
        CheckRow(row);
        return new Vector<T>(_entries.AsSpan(row * Columns, Columns).ToArray());
    }

    /// <summary>The entries of a column, as a vector.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The column is outside the matrix.</exception>
    public Vector<T> Column(int column)
    {
        CheckColumn(column);

        var entries = new T[Rows];
        for (int i = 0; i < Rows; i++)
            entries[i] = _entries[i * Columns + column];

        return new Vector<T>(entries);
    }

    /// <summary>The transpose: the entry at (i, j) is this matrix's entry at (j, i).</summary>
    public Matrix<T> Transpose()
    {
        var entries = new T[_entries.Length];
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                entries[j * Rows + i] = _entries[i * Columns + j];

        return new Matrix<T>(Columns, Rows, entries);
    }

    /// <summary>A matrix of the same size with <paramref name="map"/> applied to every entry.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null or returns null.</exception>
    public Matrix<TResult> Map<TResult>(Func<T, TResult> map) where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(map);

        var entries = new TResult[_entries.Length];
        for (int k = 0; k < entries.Length; k++)
            entries[k] = Matrix<TResult>.NotNull(map(_entries[k]), k / Columns, k % Columns);

        return new Matrix<TResult>(Rows, Columns, entries);
    }

    /// <summary>A copy of the entries as a two-dimensional array.</summary>
    public T[,] ToArray()
    {
        var array = new T[Rows, Columns];
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                array[i, j] = _entries[i * Columns + j];

        return array;
    }

    /// <summary>Whether both matrices have the same size and equal entries.</summary>
    public bool Equals(Matrix<T>? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || Rows != other.Rows || Columns != other.Columns)
            return false;

        var comparer = EqualityComparer<T>.Default;
        for (int k = 0; k < _entries.Length; k++)
            if (!comparer.Equals(_entries[k], other._entries[k]))
                return false;

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Matrix<T>);

    /// <summary>A hash of the size and the entries, consistent with <see cref="Equals(Matrix{T})"/>.</summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Rows);
        hash.Add(Columns);
        foreach (T entry in _entries)
            hash.Add(entry);

        return hash.ToHashCode();
    }

    /// <summary>Whether both matrices are null or equal.</summary>
    public static bool operator ==(Matrix<T>? left, Matrix<T>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether the matrices are not equal.</summary>
    public static bool operator !=(Matrix<T>? left, Matrix<T>? right) => !(left == right);

    /// <summary>The rows in brackets, culture-invariant: <c>[[1, 2], [3, 4]]</c>.</summary>
    public override string ToString() =>
        "[" + string.Join(", ", Enumerable.Range(0, Rows).Select(i => Row(i).ToString())) + "]";

    internal static string Format(T entry) => FormattableString.Invariant($"{entry}");

    // The row-major entries, for the arithmetic of each entry type.
    internal ReadOnlySpan<T> Entries => _entries;

    // The size for error messages: "2 x 3".
    internal string Size => $"{Rows} x {Columns}";

    // Applies combine to the entries at the same position of two matrices of the same size.
    internal Matrix<T> Combine(Matrix<T> other, Func<T, T, T> combine, string operation)
    {
        if (Rows != other.Rows || Columns != other.Columns)
            throw new ArgumentException($"Cannot {operation} matrices of different sizes: {Size} and {other.Size}.");

        var entries = new T[_entries.Length];
        for (int k = 0; k < entries.Length; k++)
            entries[k] = combine(_entries[k], other._entries[k]);

        return new Matrix<T>(Rows, Columns, entries);
    }

    internal static void CheckProduct(Matrix<T> left, Matrix<T> right)
    {
        if (left.Columns != right.Rows)
            throw new ArgumentException(
                $"Cannot multiply a {left.Size} matrix by a {right.Size} matrix: {left.Columns} columns on the left, {right.Rows} rows on the right.");
    }

    internal static void CheckProduct(Matrix<T> matrix, Vector<T> vector)
    {
        if (matrix.Columns != vector.Length)
            throw new ArgumentException($"Cannot multiply a {matrix.Size} matrix by a vector of length {vector.Length}.");
    }

    internal void CheckSquare(string operation)
    {
        if (!IsSquare)
            throw new InvalidOperationException($"The {operation} is defined only for square matrices, not for a {Size} matrix.");
    }

    internal static T NotNull(T entry, int row, int column) =>
        entry ?? throw new ArgumentNullException(nameof(entry), $"The entry at ({row}, {column}) is null.");

    private static T[] RowAt(T[][] rows, int i) =>
        rows[i] ?? throw new ArgumentNullException(nameof(rows), $"Row {i} is null.");

    private void CheckRow(int row)
    {
        if ((uint)row >= (uint)Rows)
            throw new ArgumentOutOfRangeException(nameof(row), row, $"The matrix has {Rows} rows.");
    }

    private void CheckColumn(int column)
    {
        if ((uint)column >= (uint)Columns)
            throw new ArgumentOutOfRangeException(nameof(column), column, $"The matrix has {Columns} columns.");
    }
}