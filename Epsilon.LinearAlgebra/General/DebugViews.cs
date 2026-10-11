namespace Epsilon.LinearAlgebra;

// What the debugger shows when a matrix is expanded: its rows, each expandable in turn.
internal sealed class MatrixDebugView<T>(Matrix<T> matrix) where T : notnull
{
    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
    public Vector<T>[] Rows => [.. Enumerable.Range(0, matrix.Rows).Select(matrix.Row)];
}

// What the debugger shows when a vector is expanded: its entries at their indices, as for a list.
internal sealed class VectorDebugView<T>(Vector<T> vector) where T : notnull
{
    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
    public T[] Entries => [.. vector];
}
