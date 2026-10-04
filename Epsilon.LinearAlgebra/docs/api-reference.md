# API reference

Every public type and member of the `Epsilon.LinearAlgebra` package, version 1.0.0. All types
are in the namespace `Epsilon.LinearAlgebra`. Each entry has a one-line description; the
[guide](guide.md) explains the details, and your IDE shows the full XML documentation for
every member.

A test (`LinearAlgebraApiReferenceDocTests`) checks that every public type and member is
listed here.

**Contents:**
[Matrix](#matrix) -
[Vector](#vector) -
[Matrices of double](#matrices-of-double) -
[Vectors of double](#vectors-of-double) -
[Matrices of expressions](#matrices-of-expressions) -
[Vectors of expressions](#vectors-of-expressions)

The operations for `double` and `Expr` entries are C# 14 extension members: they are called
like members of `Matrix<T>` and `Vector<T>` (`m.Determinant()`, `Matrix<double>.Identity(3)`,
`a + b`), but only compile for those entry types.

---

## Matrix

### `Matrix<T>` (sealed class, `where T : notnull`)

An immutable matrix of `Rows` x `Columns` entries.

| Member | Description |
|---|---|
| `Matrix(T[,] entries)` | A matrix with a copy of the array's entries. Throws `ArgumentNullException` for a null entry. |
| `static Matrix<T> FromRows(params T[][] rows)` | A matrix from its rows, which are copied: `Matrix<double>.FromRows([1, 2], [3, 4])`. Throws `ArgumentException` if the rows have different lengths. |
| `static Matrix<T> Create(int rows, int columns, Func<int, int, T> entry)` | The matrix whose entry at (i, j) is `entry(i, j)`. |
| `int Rows`, `int Columns` | The size. |
| `bool IsSquare` | Whether `Rows == Columns`. |
| `T this[int row, int column]` | The entry at zero-based indices; throws `ArgumentOutOfRangeException` outside the matrix. |
| `Vector<T> Row(int row)`, `Vector<T> Column(int column)` | A row or a column as a vector. |
| `Matrix<T> Transpose()` | The transpose. |
| `Matrix<TResult> Map<TResult>(Func<T, TResult> map)` | The matrix of the same size with `map` applied to every entry. |
| `T[,] ToArray()` | A copy of the entries. |
| `bool Equals(Matrix<T>? other)`, `Equals(object?)` | Same size and equal entries. |
| `==`, `!=` | The same as `Equals`. |
| `int GetHashCode()` | Consistent with `Equals`. |
| `string ToString()` | The rows in brackets, culture-invariant: `[[1, 2], [3, 4]]`. |

---

## Vector

### `Vector<T>` (sealed class : `IReadOnlyList<T>`, `where T : notnull`)

An immutable column vector: a matrix with one column. A collection expression creates one:
`Vector<double> v = [1, 2, 3];`.

| Member | Description |
|---|---|
| `int Length` | The number of entries. |
| `T this[int index]` | The entry at a zero-based index; throws `ArgumentOutOfRangeException` outside the vector. |
| `Matrix<T> ToMatrix()` | The `Length` x 1 matrix. |
| implicit to `Matrix<T>` | The same, as a conversion. |
| `Vector<TResult> Map<TResult>(Func<T, TResult> map)` | The vector with `map` applied to every entry. |
| `T[] ToArray()` | A copy of the entries. |
| `IEnumerator<T> GetEnumerator()` | The entries in order. |
| `bool Equals(Vector<T>? other)`, `Equals(object?)`, `==`, `!=`, `GetHashCode()` | Same length and equal entries. |
| `string ToString()` | The entries in brackets, culture-invariant: `[1, 2, 3]`. |

### `Vector` (static class)

| Member | Description |
|---|---|
| `static Vector<T> Create<T>(params ReadOnlySpan<T> entries)` | A vector with a copy of the entries: `Vector.Create(1.0, 2.0)`. Collection expressions call it. |

---

## Matrices of double

### `NumericMatrixExtensions` (static class)

Extension members of `Matrix<double>`.

| Member | Description |
|---|---|
| `static Matrix<double> Identity(int size)` | The identity matrix. |
| `static Matrix<double> Zero(int rows, int columns)` | The zero matrix. |
| `+`, `-` (binary and unary) | Entry by entry. Throws `ArgumentException` for different sizes. |
| `*` (matrix by matrix, matrix by `Vector<double>`) | The matrix product. Throws `ArgumentException` if the inner sizes differ. |
| `*`, `/` with a `double` | Every entry times or divided by the number. |
| `double Trace()` | The sum of the diagonal. Throws `InvalidOperationException` for a non-square matrix. |
| `double Determinant()` | By LU decomposition with partial pivoting; 0 if singular to working precision. |
| `Matrix<double> Inverse()` | Throws `InvalidOperationException` if singular to working precision. |
| `Vector<double> Solve(Vector<double> b)` | The solution of A x = b. |
| `Matrix<double> Solve(Matrix<double> b)` | The solution of A X = B, column by column. |
| `int Rank()` | The rank to working precision, for any shape. |

`Determinant`, `Inverse` and `Solve` need a square matrix, and all four throw
`InvalidOperationException` for an entry that is NaN or infinite.

---

## Vectors of double

### `NumericVectorExtensions` (static class)

Extension members of `Vector<double>`.

| Member | Description |
|---|---|
| `static Vector<double> Zero(int length)` | The zero vector. |
| `+`, `-` (binary and unary) | Entry by entry. Throws `ArgumentException` for different lengths. |
| `*`, `/` with a `double` | Every entry times or divided by the number. |
| `double Dot(Vector<double> other)` | The dot product. |

---

## Matrices of expressions

### `SymbolicMatrixExtensions` (static class)

Extension members of `Matrix<Expr>`. Arithmetic results are simplified in the `Generic` mode.

**Building**

| Member | Description |
|---|---|
| `static Matrix<Expr> Identity(int size)` | The identity matrix. |
| `static Matrix<Expr> Zero(int rows, int columns)` | The zero matrix. |
| `static Matrix<Expr> Parse(string text)` | Reads `[[a, b], [c, d]]`; the entries are not simplified. Throws `FormatException`. |
| `static Matrix<Expr> Parse(string text, string[] variableNames)` | The same with only these variables: `Parse("[[theta, 2t]]", ["theta", "t"])`. |

**Arithmetic**

| Member | Description |
|---|---|
| `+`, `-` (binary and unary) | Entry by entry, simplified. |
| `*` (matrix by matrix, matrix by `Vector<Expr>`) | The matrix product, simplified. |
| `*`, `/` with an `Expr` | Every entry times or divided by the expression; numbers convert to `Expr`. |
| `Expr Trace()` | The simplified sum of the diagonal. |

**Determinant, inverse and systems**

| Member | Description |
|---|---|
| `Expr Determinant()` | Expanded; Berkowitz's algorithm, without division. |
| `Expr CharacteristicPolynomial(string variable)` | det(t I - A) in the variable. Throws `ArgumentException` if the variable occurs in the matrix. |
| `Matrix<Expr> Adjugate()` | The transposed matrix of cofactors, without division; exists for singular matrices. |
| `Matrix<Expr> Inverse()` | adj(A) / det(A), simplified. Throws `InvalidOperationException` if the determinant is 0. |
| `Vector<Expr> Solve(Vector<Expr> b)` | Cramer's rule, adj(A) b / det(A). |
| `Matrix<Expr> Solve(Matrix<Expr> b)` | The same for every column of `b`. |

All of them need a square matrix and throw `InvalidOperationException` otherwise.

**Entry-wise**

| Member | Description |
|---|---|
| `Matrix<Expr> Simplify(SimplifyMode mode = Generic)` | Every entry simplified. |
| `Matrix<Expr> Simplify(Assumptions assumptions, SimplifyMode mode = Generic)` | The same with assumptions. |
| `Matrix<Expr> Expand(SimplifyMode mode = Generic)` | Every entry expanded. |
| `Matrix<Expr> Substitute(string variable, Expr replacement)` | The variable replaced; not simplified. |
| `Matrix<Expr> Differentiate(string variable)` | The simplified partial derivatives. |
| `IReadOnlySet<string> GetVariables()` | The variables of all entries. |
| `Matrix<double> Evaluate(IReadOnlyDictionary<string, double> bindings)` | Real values; NaN or an infinity where an entry is undefined. |
| `Matrix<double> Evaluate(params (string Name, double Value)[] bindings)` | The same with pairs: `Evaluate(("x", 1), ("y", 2))`. |
| `Matrix<double> Evaluate(double x)` | When all entries together have at most one variable. |
| `Matrix<ComplexNumber> EvaluateComplex(...)` | The same three forms over the complex numbers, principal branches. |

**Output**

| Member | Description |
|---|---|
| `string Print()` | The rows with every entry printed, readable by `Parse`: `[[x^2, 1/2]]`. |
| `string ToLatex()` | A LaTeX `bmatrix`. |

---

## Vectors of expressions

### `SymbolicVectorExtensions` (static class)

Extension members of `Vector<Expr>`.

| Member | Description |
|---|---|
| `static Vector<Expr> Zero(int length)` | The zero vector. |
| `static Vector<Expr> Parse(string text)`, `Parse(string text, string[] variableNames)` | Reads `[a, b, c]`; the entries are not simplified. |
| `+`, `-` (binary and unary), `*` and `/` with an `Expr` | Entry by entry, simplified. |
| `Expr Dot(Vector<Expr> other)` | The simplified dot product. |
| `Simplify`, `Expand`, `Substitute`, `Differentiate`, `GetVariables` | Entry-wise, as for matrices. |
| `Evaluate`, `EvaluateComplex` | The same three forms as for matrices, giving `Vector<double>` and `Vector<ComplexNumber>`. |
| `string Print()` | The entries printed, readable by `Parse`: `[x^2, 1/2]`. |
| `string ToLatex()` | A LaTeX `bmatrix` with one column. |
