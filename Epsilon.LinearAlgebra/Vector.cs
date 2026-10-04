namespace Epsilon.LinearAlgebra;

/// <summary>
/// An immutable column vector: a <see cref="Matrix{T}"/> with one column, indexed by a single
/// number. Converts implicitly to that matrix. A collection expression creates one:
/// <c>Vector&lt;double&gt; v = [1, 2, 3];</c>
/// </summary>
/// <typeparam name="T">The type of the entries.</typeparam>
[System.Runtime.CompilerServices.CollectionBuilder(typeof(Vector), nameof(Vector.Create))]
public sealed class Vector<T> : IReadOnlyList<T>, IEquatable<Vector<T>> where T : notnull
{
    // Length x 1; row-major storage of a single column is the plain list of entries.
    private readonly Matrix<T> _column;

    // Takes ownership of the entries without copying or checking them.
    internal Vector(T[] entries) : this(new Matrix<T>(entries.Length, 1, entries))
    {
    }

    private Vector(Matrix<T> column)
    {
        _column = column;
    }

    /// <summary>The number of entries.</summary>
    public int Length => _column.Rows;

    int IReadOnlyCollection<T>.Count => Length;

    /// <summary>The entry at the zero-based index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the vector.</exception>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"The vector has {Length} entries.");

            return _column[index, 0];
        }
    }

    /// <summary>The vector as a <see cref="Length"/> x 1 matrix.</summary>
    public Matrix<T> ToMatrix() => _column;

    /// <summary>The vector as a <see cref="Length"/> x 1 matrix.</summary>
    public static implicit operator Matrix<T>(Vector<T> vector) => vector.ToMatrix();

    /// <summary>A vector of the same length with <paramref name="map"/> applied to every entry.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is null or returns null.</exception>
    public Vector<TResult> Map<TResult>(Func<T, TResult> map) where TResult : notnull =>
        new(_column.Map(map));

    /// <summary>A copy of the entries as an array.</summary>
    public T[] ToArray()
    {
        var entries = new T[Length];
        for (int i = 0; i < entries.Length; i++)
            entries[i] = _column[i, 0];

        return entries;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Length; i++)
            yield return _column[i, 0];
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Whether both vectors have the same length and equal entries.</summary>
    public bool Equals(Vector<T>? other) => other is not null && _column.Equals(other._column);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Vector<T>);

    /// <summary>A hash of the length and the entries, consistent with <see cref="Equals(Vector{T})"/>.</summary>
    public override int GetHashCode() => _column.GetHashCode();

    /// <summary>Whether both vectors are null or equal.</summary>
    public static bool operator ==(Vector<T>? left, Vector<T>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether the vectors are not equal.</summary>
    public static bool operator !=(Vector<T>? left, Vector<T>? right) => !(left == right);

    /// <summary>The entries in brackets, culture-invariant: <c>[1, 2, 3]</c>.</summary>
    public override string ToString() => "[" + string.Join(", ", this.Select(Matrix<T>.Format)) + "]";
}

/// <summary>Creates vectors; collection expressions such as <c>[1, 2, 3]</c> use it too.</summary>
public static class Vector
{
    /// <summary>A vector with the given entries, which are copied: <c>Vector.Create(1.0, 2.0)</c>.</summary>
    /// <exception cref="ArgumentNullException">An entry is null.</exception>
    public static Vector<T> Create<T>(params ReadOnlySpan<T> entries) where T : notnull
    {
        var copy = entries.ToArray();
        for (int i = 0; i < copy.Length; i++)
            if (copy[i] is null)
                throw new ArgumentNullException(nameof(entries), $"The entry at index {i} is null.");

        return new Vector<T>(copy);
    }
}
