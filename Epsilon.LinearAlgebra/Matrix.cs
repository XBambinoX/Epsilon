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

    /// <summary>
    /// A matrix assembled from blocks, given row by row like <see cref="FromRows"/>:
    /// <c>FromBlocks([a, b], [c, d])</c>. The blocks of a row are placed side by side and the rows
    /// stacked, so <c>FromBlocks([a, b])</c> joins horizontally and <c>FromBlocks([a], [b])</c>
    /// vertically. A vector is a one-column block: <c>FromBlocks([a, v])</c> is the augmented matrix.
    /// </summary>
    /// <exception cref="ArgumentNullException">A row or a block is null.</exception>
    /// <exception cref="ArgumentException">
    /// A row of blocks is empty, its blocks have different numbers of rows, or the rows of blocks
    /// have different numbers of columns.
    /// </exception>
    public static Matrix<T> FromBlocks(params Matrix<T>[][] rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        int height = 0, width = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            Matrix<T>[] row = rows[i] ?? throw new ArgumentNullException(nameof(rows), $"Block row {i} is null.");
            if (row.Length == 0)
                throw new ArgumentException($"Block row {i} is empty.", nameof(rows));

            int rowHeight = BlockAt(row, i, 0).Rows, rowWidth = 0;
            for (int j = 0; j < row.Length; j++)
            {
                Matrix<T> block = BlockAt(row, i, j);
                if (block.Rows != rowHeight)
                    throw new ArgumentException($"Block ({i}, {j}) has {block.Rows} rows, but block ({i}, 0) has {rowHeight}.", nameof(rows));

                rowWidth = checked(rowWidth + block.Columns);
            }

            if (i > 0 && rowWidth != width)
                throw new ArgumentException($"Block row {i} has {rowWidth} columns, but block row 0 has {width}.", nameof(rows));

            width = rowWidth;
            height = checked(height + rowHeight);
        }

        var entries = new T[checked(height * width)];
        int top = 0;
        foreach (Matrix<T>[] row in rows)
        {
            int left = 0;
            foreach (Matrix<T> block in row)
            {
                for (int r = 0; r < block.Rows; r++)
                    block._entries.AsSpan(r * block.Columns, block.Columns).CopyTo(entries.AsSpan((top + r) * width + left));

                left += block.Columns;
            }

            top += row[0].Rows;
        }

        return new Matrix<T>(height, width, entries);
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

    /// <summary>
    /// The submatrix of the rows and columns in the ranges: <c>m[1.., ..2]</c> drops the first
    /// row and keeps the first two columns.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A range reaches outside the matrix.</exception>
    public Matrix<T> this[Range rows, Range columns]
    {
        get
        {
            var (top, height) = Bounds(rows, Rows, nameof(rows));
            var (left, width) = Bounds(columns, Columns, nameof(columns));

            var entries = new T[height * width];
            for (int i = 0; i < height; i++)
                _entries.AsSpan((top + i) * Columns + left, width).CopyTo(entries.AsSpan(i * width));

            return new Matrix<T>(height, width, entries);
        }
    }

    /// <summary>The entries at (0, 0), (1, 1), ... of the main diagonal, as many as the smaller dimension.</summary>
    public Vector<T> Diagonal()
    {
        var entries = new T[Math.Min(Rows, Columns)];
        for (int i = 0; i < entries.Length; i++)
            entries[i] = _entries[i * Columns + i];

        return new Vector<T>(entries);
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

    // operation starts the message: "The trace", "Solve".
    internal void CheckSquare(string operation)
    {
        if (!IsSquare)
            throw new InvalidOperationException($"{operation} needs a square matrix, not a {Size} matrix.");
    }

    // factor^|exponent| of a square matrix by repeated squaring; the caller passes A^-1 as the
    // factor for a negative exponent. Powers of one matrix commute, so the order of the products
    // does not matter.
    internal static Matrix<T> Power(Matrix<T> factor, int exponent, Matrix<T> identity, Func<Matrix<T>, Matrix<T>, Matrix<T>> multiply)
    {
        // Through long, so that int.MinValue has a magnitude too.
        ulong remaining = (ulong)Math.Abs((long)exponent);
        Matrix<T> square = factor;
        Matrix<T>? result = null;

        while (remaining > 0)
        {
            if ((remaining & 1) != 0)
                result = result is null ? square : multiply(result, square);

            remaining >>= 1;
            if (remaining > 0)
                square = multiply(square, square);
        }

        return result ?? identity;
    }

    // The offset and length of a range of count rows or columns.
    internal static (int Offset, int Length) Bounds(Range range, int count, string paramName)
    {
        int start = range.Start.GetOffset(count), end = range.End.GetOffset(count);
        if ((uint)end > (uint)count || (uint)start > (uint)end)
            throw new ArgumentOutOfRangeException(paramName, $"The range {range} is outside 0..{count}.");

        return (start, end - start);
    }

    internal static T NotNull(T entry, int row, int column) =>
        entry ?? throw new ArgumentNullException(nameof(entry), $"The entry at ({row}, {column}) is null.");

    private static T[] RowAt(T[][] rows, int i) =>
        rows[i] ?? throw new ArgumentNullException(nameof(rows), $"Row {i} is null.");

    private static Matrix<T> BlockAt(Matrix<T>[] row, int i, int j) =>
        row[j] ?? throw new ArgumentNullException("rows", $"Block ({i}, {j}) is null.");

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