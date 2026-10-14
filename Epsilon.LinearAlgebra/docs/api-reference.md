# API reference

Every public type and member of the `Epsilon.LinearAlgebra` package, version 1.4.0. All types
are in the namespace `Epsilon.LinearAlgebra`. Each entry has a one-line description; the
[guides](README.md) explain the details, and your IDE shows the full XML documentation for
every member.

A test (`LinearAlgebraApiReferenceDocTests`) checks that every public type and member is
listed here.

**Contents:**
[Matrix](#matrix) -
[Vector](#vector) -
[Matrices of double](#matrices-of-double) -
[Vectors of double](#vectors-of-double) -
[Matrices of rationals](#matrices-of-rationals) -
[Vectors of rationals](#vectors-of-rationals) -
[Matrices of expressions](#matrices-of-expressions) -
[Vectors of expressions](#vectors-of-expressions) -
[Fixed-size vectors](#fixed-size-vectors) -
[Fixed-size matrices](#fixed-size-matrices) -
[Quaternions](#quaternions) -
[Camera](#camera) -
[System.Numerics](#systemnumerics)

The operations for `double`, `Rational` and `Expr` entries are C# 14 extension members: they
are called like members of `Matrix<T>` and `Vector<T>` (`m.Determinant()`,
`Matrix<double>.Identity(3)`, `a + b`), but only compile for those entry types. The same holds
for the operations of the fixed-size types that need floating point, such as `Norm`,
`Inverse` and the rotations.

---

## Matrix

### `Matrix<T>` (sealed class, `where T : notnull`)

An immutable matrix of `Rows` x `Columns` entries.

| Member | Description |
|---|---|
| `Matrix(T[,] entries)` | A matrix with a copy of the array's entries. Throws `ArgumentNullException` for a null entry. |
| `static Matrix<T> FromRows(params T[][] rows)` | A matrix from its rows, which are copied: `Matrix<double>.FromRows([1, 2], [3, 4])`. Throws `ArgumentException` if the rows have different lengths. |
| `static Matrix<T> FromBlocks(params Matrix<T>[][] rows)` | A matrix from blocks, row by row: `FromBlocks([a, b], [c, d])`; a vector is a block with one column. Throws `ArgumentException` if the blocks do not fit together. |
| `static Matrix<T> Create(int rows, int columns, Func<int, int, T> entry)` | The matrix whose entry at (i, j) is `entry(i, j)`. |
| `int Rows`, `int Columns` | The size. |
| `bool IsSquare` | Whether `Rows == Columns`. |
| `T this[int row, int column]` | The entry at zero-based indices; throws `ArgumentOutOfRangeException` outside the matrix. |
| `Matrix<T> this[Range rows, Range columns]` | The submatrix: `m[1.., ..2]`; throws `ArgumentOutOfRangeException` outside the matrix. |
| `Vector<T> Row(int row)`, `Vector<T> Column(int column)` | A row or a column as a vector. |
| `Vector<T> Diagonal()` | The main diagonal, as many entries as the smaller dimension. |
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
| `T this[int index]` | The entry at a zero-based index, also from the end: `v[^1]`; throws `ArgumentOutOfRangeException` outside the vector. |
| `Vector<T> Slice(int start, int length)` | The entries from `start` on. It makes ranges work: `v[..3]`, `v[1..^1]`. |
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
| `static Matrix<double> FromDiagonal(Vector<double> diagonal)` | The diagonal matrix: `FromDiagonal([1, 2, 3])`. |
| `+`, `-` (binary and unary) | Entry by entry. Throws `ArgumentException` for different sizes. |
| `*` (matrix by matrix, matrix by `Vector<double>`) | The matrix product. Throws `ArgumentException` if the inner sizes differ. |
| `*`, `/` with a `double` | Every entry times or divided by the number. |
| `Matrix<double> Pow(int exponent)` | A^n by repeated squaring: the identity for n = 0, a power of the inverse for negative n. |
| `Matrix<double> Hadamard(Matrix<double> other)` | The entry-wise product. Throws `ArgumentException` for different sizes. |
| `double FrobeniusNorm()` | The square root of the sum of the squares of the entries; neither overflows nor underflows. |
| `bool IsApproximately(Matrix<double> other, double relativeTolerance = 1e-9, double absoluteTolerance = 0)` | Equal up to rounding: the same size and \|\|A - B\|\| at most `relativeTolerance` times the larger norm (Frobenius), or at most `absoluteTolerance`, which is needed against the zero matrix. Infinite entries must be equal; NaN never is. |
| `double Trace()` | The sum of the diagonal. Throws `InvalidOperationException` for a non-square matrix. |
| `double Determinant()` | By LU decomposition with partial pivoting; 0 if singular to working precision. |
| `Matrix<double> Inverse()` | Throws `InvalidOperationException` if singular to working precision. |
| `Vector<double> Solve(Vector<double> b)` | The solution of A x = b. |
| `Matrix<double> Solve(Matrix<double> b)` | The solution of A X = B, column by column. |
| `int Rank()` | The rank to working precision, for any shape. |

`Pow`, `Determinant`, `Inverse` and `Solve` need a square matrix. `Determinant`, `Inverse`,
`Solve` and `Rank` throw `InvalidOperationException` for an entry that is NaN or infinite.

---

## Vectors of double

### `NumericVectorExtensions` (static class)

Extension members of `Vector<double>`.

| Member | Description |
|---|---|
| `static Vector<double> Zero(int length)` | The zero vector. |
| `static Vector<double> Lerp(Vector<double> start, Vector<double> end, double t)` | (1 - t) start + t end: `start` at t = 0, `end` at t = 1; other values extrapolate. |
| `+`, `-` (binary and unary) | Entry by entry. Throws `ArgumentException` for different lengths. |
| `*`, `/` with a `double` | Every entry times or divided by the number. |
| `double Dot(Vector<double> other)` | The dot product. |
| `double Norm()`, `double NormSquared()` | The Euclidean norm (length) and its square. `Norm` neither overflows nor underflows. |
| `Vector<double> Normalize()` | The unit vector in the same direction. Throws `InvalidOperationException` for the zero vector. |
| `double Distance(Vector<double> other)` | The norm of the difference. |
| `Vector<double> Cross(Vector<double> other)` | The cross product of two vectors of length 3. |
| `double Angle(Vector<double> other)` | In radians, from 0 to pi; accurate at small angles too. Throws for a zero vector. |
| `Vector<double> ProjectOnto(Vector<double> onto)` | The projection (v . w / w . w) w onto the line through `onto`. |
| `Vector<double> Reflect(Vector<double> normal)` | The mirror image in the plane perpendicular to `normal`, which need not have length 1. |
| `Matrix<double> Outer(Vector<double> other)` | The outer product v w^T. |
| `Vector<double> Hadamard(Vector<double> other)` | The entry-wise product. |
| `bool IsApproximately(Vector<double> other, double relativeTolerance = 1e-9, double absoluteTolerance = 0)` | Equal up to rounding, as for matrices. Returns `false` for different lengths. |

The members that take two vectors throw `ArgumentException` for different lengths.

---

## Matrices of rationals

### `RationalMatrixExtensions` (static class)

Extension members of `Matrix<Rational>`. The arithmetic is exact, so there is no tolerance: a
matrix is singular exactly when its determinant is 0. Integers convert to `Rational`:
`Matrix<Rational>.FromRows([1, 2], [3, 4])`.

| Member | Description |
|---|---|
| `static Matrix<Rational> Identity(int size)` | The identity matrix. |
| `static Matrix<Rational> Zero(int rows, int columns)` | The zero matrix. |
| `static Matrix<Rational> FromDiagonal(Vector<Rational> diagonal)` | The diagonal matrix. |
| `+`, `-` (binary and unary) | Entry by entry. Throws `ArgumentException` for different sizes. |
| `*` (matrix by matrix, matrix by `Vector<Rational>`) | The matrix product. Throws `ArgumentException` if the inner sizes differ. |
| `*`, `/` with a `Rational` | Every entry times or divided by the number; `/` throws `DivideByZeroException` for 0. |
| `Rational Trace()` | The sum of the diagonal. |
| `Rational Determinant()` | Exact, by Gaussian elimination. |
| `Matrix<Rational> Inverse()` | Exact. Throws `InvalidOperationException` if singular. |
| `Vector<Rational> Solve(Vector<Rational> b)`, `Matrix<Rational> Solve(Matrix<Rational> b)` | The exact solution of A x = b, or of A X = B column by column. |
| `int Rank()` | The exact rank, for any shape. |
| `Matrix<Rational> Pow(int exponent)` | A^n; a power of the inverse for negative n. |
| `Matrix<Rational> Hadamard(Matrix<Rational> other)` | The entry-wise product. |

`Trace`, `Determinant`, `Inverse`, `Solve` and `Pow` need a square matrix and throw
`InvalidOperationException` otherwise.

---

## Vectors of rationals

### `RationalVectorExtensions` (static class)

Extension members of `Vector<Rational>`. The norm, normalization, distance and angles are not
rational in general (the norm of `[1, 1]` is sqrt(2)); use vectors of `double` or `Expr` for them.

| Member | Description |
|---|---|
| `static Vector<Rational> Zero(int length)` | The zero vector. |
| `static Vector<Rational> Lerp(Vector<Rational> start, Vector<Rational> end, Rational t)` | (1 - t) start + t end. |
| `+`, `-` (binary and unary), `*` and `/` with a `Rational` | Entry by entry. |
| `Rational Dot(Vector<Rational> other)`, `Rational NormSquared()` | The dot product, and that of the vector with itself. |
| `Cross`, `ProjectOnto`, `Reflect`, `Outer`, `Hadamard` | As for `double`, exact. |

---

## Matrices of expressions

### `SymbolicMatrixExtensions` (static class)

Extension members of `Matrix<Expr>`. Arithmetic results are simplified in the `Generic` mode.

**Building**

| Member | Description |
|---|---|
| `static Matrix<Expr> Identity(int size)` | The identity matrix. |
| `static Matrix<Expr> Zero(int rows, int columns)` | The zero matrix. |
| `static Matrix<Expr> FromDiagonal(Vector<Expr> diagonal)` | The diagonal matrix. |
| `static Matrix<Expr> Parse(string text)` | Reads `[[a, b], [c, d]]`; the entries are not simplified. Throws `FormatException`. |
| `static Matrix<Expr> Parse(string text, string[] variableNames)` | The same with only these variables: `Parse("[[theta, 2t]]", ["theta", "t"])`. |

**Arithmetic**

| Member | Description |
|---|---|
| `+`, `-` (binary and unary) | Entry by entry, simplified. |
| `*` (matrix by matrix, matrix by `Vector<Expr>`) | The matrix product, simplified. |
| `*`, `/` with an `Expr` | Every entry times or divided by the expression; numbers convert to `Expr`. |
| `Matrix<Expr> Pow(int exponent)` | A^n, simplified; a power of the inverse for negative n. Throws `InvalidOperationException` for a non-square matrix, or for negative n if the determinant is 0. |
| `Matrix<Expr> Hadamard(Matrix<Expr> other)` | The entry-wise product, simplified. |
| `Expr FrobeniusNorm()` | The simplified square root of the sum of the squares of the entries. |
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

**Transforms**

The conventions of the fixed-size matrices: column vectors, right-handed, radians. Built from these, a kinematic chain gives the position of its end with `TransformPoint`, ready for `Jacobian` and `Compile`.

| Member | Description |
|---|---|
| `static Matrix<Expr> Rotation(Expr angle)` | The 2 x 2 rotation of the plane, counterclockwise; simplified, so `Rotation(pi / 2)` is `[[0, -1], [1, 0]]`. |
| `static Matrix<Expr> RotationX(Expr angle)`, `RotationY`, `RotationZ` | The 3 x 3 rotations about the axes, as `Matrix3x3.RotationX` and so on. |
| `static Matrix<Expr> Translation(Vector<Expr> offset)` | The translation in homogeneous coordinates, one size larger than the offset: `[a, b, c]` gives a 4 x 4 matrix. |
| `Matrix<Expr> ToHomogeneous()` | `[[A, 0], [0, 1]]`, to combine a linear map with translations; a scaling is `FromDiagonal(factors).ToHomogeneous()`. |
| `Vector<Expr> TransformPoint(Vector<Expr> point)` | The point extended by 1, multiplied, and divided by the last entry unless it is exactly 1; simplified. Throws `ArgumentException` unless the matrix is one size larger than the point. |
| `Vector<Expr> TransformDirection(Vector<Expr> direction)` | Only the linear part acts, so translations don't move it. |

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

**Compiled evaluation**

| Member | Description |
|---|---|
| `Func<double, Matrix<double>> Compile()` | A compiled delegate of a matrix whose entries have at most one variable: exactly the values of `Evaluate(double x)`, many times faster. Doesn't simplify; a subexpression shared by several entries is computed once. Throws `InvalidOperationException` for two or more variables or for `i`. |
| `Compile(string x)`, `Compile(string x, string y)`, `Compile(string x, string y, string z)` | The same for the named variables, whose values the delegate takes in this order: `Compile("x", "y")(3, 4)`. Unused names are allowed; throws `ArgumentException` for a variable that isn't named or a name given twice. |
| `Func<double[], Matrix<double>> Compile(IReadOnlyList<string> variables)` | The same for any number of variables, with the values in an array in this order. |
| `Action<double[], double[]> CompileEntries(IReadOnlyList<string> variables)` | The same without allocating: writes entry (i, j) to index `i * Columns + j` of an array the caller provides and may reuse. |
| `string ToCSharp(string name, IReadOnlyList<string> variables)` | The source of `public static void name(double x, ..., Span<double> result)`, which writes entry (i, j) to `result[i * Columns + j]`, with shared subexpressions in locals; exactly the values of `Evaluate`, for code without Epsilon. Needs `using System;` and C# 8. Throws `ArgumentException` for a name that isn't a valid C# name, a variable listed twice or not listed, `InvalidOperationException` for `i`, `NotSupportedException` for a node from outside the core. |

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
| `static Vector<Expr> Lerp(Vector<Expr> start, Vector<Expr> end, Expr t)` | (1 - t) start + t end, simplified. |
| `+`, `-` (binary and unary), `*` and `/` with an `Expr` | Entry by entry, simplified. |
| `Expr Dot(Vector<Expr> other)` | The simplified dot product. |
| `Expr Norm()`, `Expr NormSquared()` | sqrt(v . v) and v . v, simplified: `[3, 4]` has norm 5. |
| `Normalize`, `Distance`, `Cross`, `ProjectOnto`, `Reflect`, `Outer`, `Hadamard` | As for `double`, exact and simplified: `[1, 1]` normalizes to `[sqrt(2) / 2, sqrt(2) / 2]`. |
| `Expr Angle(Vector<Expr> other)` | The arccosine of v . w over the product of the norms, simplified: `[1, 0]` and `[1, 1]` give pi / 4. |
| `Simplify`, `Expand`, `Substitute`, `Differentiate`, `GetVariables` | Entry-wise, as for matrices. |
| `Matrix<Expr> Jacobian(IReadOnlyList<string> variables)` | The matrix of the simplified partial derivatives, one row per entry and one column per variable: `[r * cos(t), r * sin(t)]` by `["r", "t"]` gives `[[cos(t), -sin(t) * r], [sin(t), cos(t) * r]]`. Throws `ArgumentException` for a name listed twice. |
| `Evaluate`, `EvaluateComplex` | The same three forms as for matrices, giving `Vector<double>` and `Vector<ComplexNumber>`. |
| `Compile`, `CompileEntries`, `ToCSharp` | The same forms as for matrices, giving `Vector<double>`: a curve `[cos(t), sin(t), t]` compiles with `Compile("t")`, a vector field with `Compile("x", "y", "z")`. |
| `string Print()` | The entries printed, readable by `Parse`: `[x^2, 1/2]`. |
| `string ToLatex()` | A LaTeX `bmatrix` with one column. |

---

## Fixed-size vectors

### `Vector2<T>`, `Vector3<T>`, `Vector4<T>` (readonly structs : `IEquatable`)

Immutable value types for geometry and games. `T` is any number type with the arithmetic
operators (`IAdditionOperators`, `ISubtractionOperators`, `IMultiplyOperators`,
`IDivisionOperators`, `IUnaryNegationOperators`, `IAdditiveIdentity`,
`IMultiplicativeIdentity`): `float`, `double`, `Half`, `decimal`, `int` and the like. The table
shows `Vector3<T>`; the others have the same members for their number of entries, except where
noted.

| Member | Description |
|---|---|
| `Vector3(T x, T y, T z)`, `Vector3(Vector2<T> xy, T z)`, `Vector4(Vector3<T> xyz, T w)` | The vector with these entries. |
| implicit from a tuple | `Vector3<double> v = (1, 2, 3);` |
| `T X`, `T Y`, `T Z`, `T W` | The entries, settable in `with`: `v with { Y = 0 }`. |
| `T this[int index]` | The entry at a zero-based index; throws `ArgumentOutOfRangeException` outside the vector. |
| `Vector2<T> XY`, `Vector3<T> XYZ` | The first two or three entries (`XYZ` of `Vector4<T>` only). |
| `static Zero`, `One`, `UnitX`, `UnitY`, `UnitZ`, `UnitW` | The zero vector, the vector of ones, the unit vectors along the axes. |
| `+`, `-` (binary and unary) | Entry by entry. |
| `*`, `/` with a `T` | Every entry times or divided by the number; integer quotients are truncated. |
| `T Dot(Vector3<T> other)`, `T NormSquared()` | The dot product, and that of the vector with itself. |
| `Vector3<T> Cross(Vector3<T> other)` | The cross product: `UnitX.Cross(UnitY)` is `UnitZ`. |
| `T Cross(Vector2<T> other)` | Of `Vector2<T>`: x1 y2 - y1 x2, positive if `other` points counterclockwise. |
| `Vector3<T> Hadamard(Vector3<T> other)` | The entry-wise product. |
| `static Vector3<T> Lerp(Vector3<T> start, Vector3<T> end, T t)` | (1 - t) start + t end. |
| `Vector3<TResult> Map<TResult>(Func<T, TResult> map)` | `map` applied to every entry; converts too: `v.Map(float.CreateTruncating)`. |
| `Vector<T> ToVector()` | The same entries as a `Vector<T>`, for the general algorithms. |
| explicit from `Vector<T>` | `(Vector3<double>)v`; throws `ArgumentException` unless `v` has 3 entries. |
| `void Deconstruct(out T x, out T y, out T z)` | `var (x, y, z) = v;` |
| `bool Equals(Vector3<T> other)`, `Equals(object?)`, `==`, `!=`, `GetHashCode()` | Equal entries by their `Equals`, so NaN equals NaN. |
| `string ToString()` | The entries in brackets, culture-invariant: `[1, 2, 3]`. |

The entries are stored in order without padding, so a span of numbers can be read as a span of
vectors without copying: `MemoryMarshal.Cast<float, Vector3<float>>(buffer)`.

### `FloatingPointVectorExtensions` (static class)

Extension members of `Vector2<T>`, `Vector3<T>` and `Vector4<T>` for `T : IFloatingPointIeee754<T>`:
`float`, `double` and `Half`. Shown for `Vector3<T>`.

| Member | Description |
|---|---|
| `T Norm()` | The Euclidean norm (length); neither overflows nor underflows. |
| `Vector3<T> Normalize()` | The unit vector in the same direction. Throws `InvalidOperationException` for the zero vector. |
| `T Distance(Vector3<T> other)` | The norm of the difference. |
| `T Angle(Vector3<T> other)` | In radians, from 0 to pi; accurate at small angles too. Throws for a zero vector. |
| `Vector3<T> ProjectOnto(Vector3<T> onto)` | The projection (v . w / w . w) w. Throws `ArgumentException` if `onto` is zero. |
| `Vector3<T> Reflect(Vector3<T> normal)` | The mirror image in the plane perpendicular to `normal`, which need not have length 1. |
| `bool IsApproximately(Vector3<T> other, T? relativeTolerance = null, T? absoluteTolerance = null)` | Equal up to rounding, as for `Vector<double>`; the relative tolerance defaults to 1e-9 for `double`, 1e-5 for `float`, 1e-2 for `Half`. |

### `ComponentWiseVectorExtensions` (static class)

Extension members of `Vector2<T>`, `Vector3<T>` and `Vector4<T>` for `T : INumber<T>`. Shown for
`Vector3<T>`.

| Member | Description |
|---|---|
| `Vector3<T> Min(Vector3<T> other)`, `Vector3<T> Max(Vector3<T> other)` | The smaller or larger entry at each index. |
| `Vector3<T> Abs()` | The absolute value of every entry. |
| `Vector3<T> Clamp(Vector3<T> min, Vector3<T> max)` | Every entry limited to its range. Throws `ArgumentException` if a minimum is above its maximum. |

---

## Fixed-size matrices

### `Matrix2x2<T>`, `Matrix3x3<T>`, `Matrix4x4<T>` (readonly structs : `IEquatable`)

Immutable value types for transformations, with the same `T` as the fixed-size vectors. Vectors
are columns: `m * v` maps v, `a * b` applies b first, and a translation is in the last column.
The table shows `Matrix4x4<T>`; the others have the same members for their size, except where
noted. `Matrix2x2<T>` is for linear maps of the plane (`m * v`), so it has `Scale(Vector2<T>)` but
no `Translation`, `TransformPoint` or `TransformDirection`.

| Member | Description |
|---|---|
| `Matrix4x4(T m11, T m12, ..., T m44)` | The entries row by row. |
| `Matrix4x4(Matrix3x3<T> linear)`, `Matrix3x3(Matrix2x2<T> linear)` | The linear map in homogeneous coordinates: `linear` in the top left, 1 in the corner. |
| `M11`, `M12`, `M13`, `M14`, `M21`, `M22`, `M23`, `M24`, `M31`, `M32`, `M33`, `M34`, `M41`, `M42`, `M43`, `M44` | The entries of type `T`; Mij is in row i, column j. Settable in `with`. |
| `static Identity`, `Zero` | The identity and the zero matrix. |
| `static FromRows(...)`, `static FromColumns(...)` | The matrix from its rows or columns as vectors. |
| `static Matrix4x4<T> Translation(Vector3<T> offset)` | The translation; for `Matrix3x3<T>`, `Translation(Vector2<T>)` translates the plane in homogeneous coordinates. |
| `static Matrix4x4<T> Scale(Vector3<T> factors)`, `Scale(T factor)` | diag(x, y, z, 1). `Matrix3x3<T>` has `Scale(Vector3<T>)` for space and `Scale(Vector2<T>)`, diag(x, y, 1), for the plane. |
| `T this[int row, int column]` | The entry at zero-based indices: `m[0, 3]` is `M14`. |
| `Vector4<T> Row(int index)`, `Vector4<T> Column(int index)` | A row or a column, zero-based. |
| `Matrix4x4<T> Transpose()`, `T Trace()` | The transpose and the sum of the diagonal. |
| `T Determinant()` | By the closed formula; exact for integers. |
| `+`, `-` (binary and unary), `*` and `/` with a `T` | Entry by entry. |
| `*` (matrix by matrix, matrix by `Vector4<T>`) | The matrix product. |
| `Vector3<T> TransformPoint(Vector3<T> point)` | (x, y, z, 1) mapped and divided by w unless w is 1; for `Matrix3x3<T>` a `Vector2<T>`. |
| `Vector3<T> TransformDirection(Vector3<T> direction)` | (x, y, z, 0) mapped: translations leave it alone. |
| `void TransformPoints(ReadOnlySpan<Vector3<T>> points, Span<Vector3<T>> destination)` | `TransformPoint` of every point, into `destination`, which may be `points` itself; for `Matrix3x3<T>` spans of `Vector2<T>`. Throws `ArgumentException` if `destination` is shorter or overlaps `points` at another position. |
| `void TransformDirections(ReadOnlySpan<Vector3<T>> directions, Span<Vector3<T>> destination)` | The same with `TransformDirection`. |
| `void Transform(ReadOnlySpan<Vector4<T>> vectors, Span<Vector4<T>> destination)` | The same with `m * v`; the only one of the three for `Matrix2x2<T>`. |
| `Matrix4x4<TResult> Map<TResult>(Func<T, TResult> map)` | `map` applied to every entry: `m.Map(float.CreateTruncating)`. |
| `Matrix<T> ToMatrix()` | The same entries as a `Matrix<T>`, for the general algorithms: `m.ToMatrix().Rank()`. |
| explicit from `Matrix<T>` | `(Matrix4x4<double>)m`; throws `ArgumentException` unless `m` is 4 x 4. |
| `bool Equals(Matrix4x4<T> other)`, `Equals(object?)`, `==`, `!=`, `GetHashCode()` | Equal entries by their `Equals`. |
| `string ToString()` | The rows in brackets, culture-invariant: `[[1, 0], ...]`. |

### `FloatingPointMatrix2x2Extensions`, `FloatingPointMatrix3x3Extensions`, `FloatingPointMatrix4x4Extensions` (static classes)

Extension members of `Matrix2x2<T>`, `Matrix3x3<T>` and `Matrix4x4<T>` for
`T : IFloatingPointIeee754<T>`. Angles are in radians.

| Member | Description |
|---|---|
| `static RotationX(T angle)`, `RotationY(T angle)`, `RotationZ(T angle)` | The rotation about an axis: `RotationZ` turns `UnitX` toward `UnitY`. |
| `static Matrix2x2<T> Rotation(T angle)` | Of `Matrix2x2<T>`: the rotation of the plane, turning `UnitX` toward `UnitY`. |
| `static FromEulerAngles(Vector3<T> angles, EulerOrder order)` | Of `Matrix3x3<T>` and `Matrix4x4<T>`: the rotation by `angles.X` about x, `angles.Y` about y and `angles.Z` about z, in the given order about the fixed axes; `XYZ` gives Rz Ry Rx. |
| `Vector3<T> ToEulerAngles(EulerOrder order)` | The Euler angles of a rotation matrix (the top left corner of a 4 x 4 one): the middle one in [-pi/2, pi/2], the others in [-pi, pi]; at gimbal lock the last one is 0. |
| `static Rotation(Quaternion<T> rotation)` | Of `Matrix3x3<T>` and `Matrix4x4<T>`: the matrix of a quaternion's rotation, which need not have norm 1; `Rotation(p * q)` is `Rotation(p) * Rotation(q)`. Throws `ArgumentException` for the zero quaternion. |
| `static Rotation(Vector3<T> axis, T angle)` | The rotation about any axis, counterclockwise seen from its tip. Throws `ArgumentException` for the zero axis. |
| `static Matrix4x4<T> Compose(Vector3<T> translation, Quaternion<T> rotation, Vector3<T> scale)` | T R S: scale, then turn, then move. Throws `ArgumentException` for the zero quaternion. |
| `bool TryDecompose(out Vector3<T> translation, out Quaternion<T> rotation, out Vector3<T> scale)` | Splits an affine map into T R S; a mirror image has a negative `scale.X`. `false` for a shear, a zero scale, a projective map or a NaN or infinite entry. |
| `static Matrix3x3<T> Compose(Vector2<T> translation, T angle, Vector2<T> scale)`, `bool TryDecompose(out Vector2<T> translation, out T angle, out Vector2<T> scale)` | Of `Matrix3x3<T>`: the same for affine maps of the plane. |
| `Matrix3x3<T> NormalMatrix()` | Of `Matrix4x4<T>`: the inverse transpose of the top left corner, which maps normals. Throws `InvalidOperationException` if that corner has no inverse. |
| `Vector3<T> TransformNormal(Vector3<T> normal)` | The normal mapped by `NormalMatrix()`, not normalized; it stays perpendicular to the mapped surface. |
| `void TransformNormals(ReadOnlySpan<Vector3<T>> normals, Span<Vector3<T>> destination)` | The same for a span, with the normal matrix computed once. |
| `bool TryInverse(out Matrix4x4<T> inverse)` | The inverse, unless the matrix is singular to working precision (its determinant within the rounding error of the formula) or has a NaN or infinite entry. Neither overflows nor underflows. |
| `Matrix4x4<T> Inverse()` | The same; throws `InvalidOperationException` where `TryInverse` returns `false`. |
| `bool IsApproximately(Matrix4x4<T> other, T? relativeTolerance = null, T? absoluteTolerance = null)` | Equal up to rounding in the Frobenius norm, with the same defaults as for vectors. |

---

## Quaternions

### `Quaternion<T>` (readonly struct : `IEquatable`)

The quaternion x i + y j + z k + w, for rotations: (sin(a/2) u, cos(a/2)) turns by the angle a
about the unit axis u. `T` as for the fixed-size vectors.

| Member | Description |
|---|---|
| `Quaternion(T x, T y, T z, T w)`, `Quaternion(Vector3<T> xyz, T w)` | The entries in the order of System.Numerics. |
| `T X`, `T Y`, `T Z`, `T W` | The entries, settable in `with`. |
| `static Quaternion<T> Identity` | 1, the rotation by 0. |
| `Vector3<T> XYZ` | The vector part. |
| `Quaternion<T> Conjugate()` | -x i - y j - z k + w. |
| `T Dot(Quaternion<T> other)`, `T NormSquared()` | As vectors of four numbers. |
| `*` (quaternion by quaternion) | The Hamilton product: `p * q` applies q first, like matrices. |
| `+`, `-` (binary and unary), `*` and `/` with a `T` | Entry by entry; `-q` is the same rotation as q. |
| `Quaternion<TResult> Map<TResult>(Func<T, TResult> map)` | `map` applied to every entry. |
| `void Deconstruct(out T x, out T y, out T z, out T w)` | `var (x, y, z, w) = q;` |
| `bool Equals(Quaternion<T> other)`, `Equals(object?)`, `==`, `!=`, `GetHashCode()` | Equal entries by their `Equals`; q and -q are not equal. |
| `string ToString()` | The entries x, y, z, w: `[0, 0, 0, 1]`. |

### `FloatingPointQuaternionExtensions` (static class)

Extension members of `Quaternion<T>` for `T : IFloatingPointIeee754<T>`. Angles are in radians.
Wherever a quaternion stands for a rotation it need not have norm 1.

| Member | Description |
|---|---|
| `static Rotation(Vector3<T> axis, T angle)` | The rotation about any axis by the right-hand rule, as `Matrix3x3<T>.Rotation`. Throws `ArgumentException` for the zero axis. |
| `static RotationX(T angle)`, `RotationY(T angle)`, `RotationZ(T angle)` | The rotations about the axes. |
| `static FromEulerAngles(Vector3<T> angles, EulerOrder order)`, `Vector3<T> ToEulerAngles(EulerOrder order)` | Euler angles, as for the matrices. `ToEulerAngles` throws `InvalidOperationException` for the zero quaternion. |
| `static FromToRotation(Vector3<T> from, Vector3<T> to)` | The shortest rotation from one direction to the other, a half turn for opposite ones; accurate for nearly opposite ones. Throws `ArgumentException` for a zero vector. |
| `static FromRotationMatrix(Matrix3x3<T> matrix)`, `FromRotationMatrix(Matrix4x4<T> matrix)` | The unit quaternion of a rotation matrix (the top left corner of a 4 x 4 one), by Shepperd's method. |
| `T Norm()`, `Quaternion<T> Normalize()` | The norm without overflow or underflow, and the unit quaternion. `Normalize` throws `InvalidOperationException` for 0. |
| `Quaternion<T> Inverse()` | The conjugate over the squared norm. Throws `InvalidOperationException` for 0. |
| `Vector3<T> Rotate(Vector3<T> vector)` | q v q^-1. Throws `InvalidOperationException` for the zero quaternion. |
| `(Vector3<T> Axis, T Angle) ToAxisAngle()` | The unit axis and the angle from 0 to pi; `UnitX` and 0 for the identity. |
| `T Angle(Quaternion<T> other)` | The angle of the rotation between them, from 0 to pi; accurate for small angles. |
| `static Slerp(Quaternion<T> start, Quaternion<T> end, T t)` | Spherical linear interpolation, the shorter way, at constant speed. |
| `static Nlerp(Quaternion<T> start, Quaternion<T> end, T t)` | Normalized linear interpolation: the same path, faster, at a varying speed. |
| `bool IsApproximately(Quaternion<T> other, T? relativeTolerance = null, T? absoluteTolerance = null)` | Equal up to rounding as vectors of four numbers; q and -q are not close. |

### `EulerOrder` (enum)

The order of Euler angles: the rotations turn about the fixed axes of space, first about the
first axis of the name, so `XYZ` turns about x, then y, then z, and its matrix is Rz Ry Rx; the
same rotation is the reverse order about the axes that turn with the object. The angles
(pitch, yaw, roll) in the order `ZXY` give `CreateFromYawPitchRoll` of System.Numerics.

| Value | Turns about | Matrix |
|---|---|---|
| `XYZ` | x, then y, then z | Rz Ry Rx |
| `XZY` | x, then z, then y | Ry Rz Rx |
| `YXZ` | y, then x, then z | Rz Rx Ry |
| `YZX` | y, then z, then x | Rx Rz Ry |
| `ZXY` | z, then x, then y | Ry Rx Rz |
| `ZYX` | z, then y, then x | Rx Ry Rz |

---

## Camera

### `CameraMatrixExtensions` (static class)

View and projection matrices, static extension members of `Matrix4x4<T>` for
`T : IFloatingPointIeee754<T>`. The plain ones are right-handed: the camera looks along -z, with
x to the right and y up. The `LeftHanded` ones look along +z. After `TransformPoint` divides by w,
a projection maps what the camera sees to x and y in [-1, 1] and to the depths of a `DepthRange`.

| Member | Description |
|---|---|
| `static LookAt(Vector3<T> eye, Vector3<T> target, Vector3<T> up)` | The view matrix: the camera at the origin, looking along -z, y up. Throws `ArgumentException` if `target` is `eye` or `up` is parallel to the direction of view. |
| `static LookAtLeftHanded(Vector3<T> eye, Vector3<T> target, Vector3<T> up)` | The same, looking along +z. |
| `static Perspective(T fieldOfViewY, T aspectRatio, T near, T far, DepthRange depthRange = ZeroToOne)` | The perspective projection: the vertical angle of view in radians, width over height, the near and far distances; `far` may be infinite. Throws `ArgumentOutOfRangeException` unless 0 < angle < pi, aspect > 0 and 0 < near < far. |
| `static PerspectiveLeftHanded(...)` | The same, looking along +z. |
| `static Orthographic(T width, T height, T near, T far, DepthRange depthRange = ZeroToOne)` | The orthographic projection of a box around the axis of view: no perspective. |
| `static OrthographicOffCenter(T left, T right, T bottom, T top, T near, T far, DepthRange depthRange = ZeroToOne)` | The same for any box: `OrthographicOffCenter(0, 1920, 1080, 0, -1, 1)` maps pixels to the screen. |
| `static OrthographicLeftHanded(...)`, `static OrthographicOffCenterLeftHanded(...)` | The same, looking along +z. |

The orthographic ones throw `ArgumentOutOfRangeException` for a box of size 0 or a value that is
not finite.

### `DepthRange` (enum)

| Value | Near, far | For |
|---|---|---|
| `ZeroToOne` | 0, 1 | Direct3D, Vulkan, Metal, WebGPU; the default |
| `MinusOneToOne` | -1, 1 | OpenGL, WebGL |
| `OneToZero` | 1, 0 | Reverse-Z, for float depth buffers |

---

## System.Numerics

### `SystemNumericsExtensions` (static class)

Conversions between the fixed-size types of `float` and those of System.Numerics. System.Numerics
multiplies row vectors from the left (`v * M`), so its matrices are the transposes of these: the
conversions transpose, a map stays the same map, and `a * b` here is `b * a` there.

| Member | Description |
|---|---|
| `System.Numerics.Vector3 ToSystemNumerics()` | Of `Vector2<float>`, `Vector3<float>`, `Vector4<float>`: the same vector. |
| `static Vector3<float> FromSystemNumerics(System.Numerics.Vector3 value)` | And back, for all three sizes. |
| `System.Numerics.Quaternion ToSystemNumerics()`, `static FromSystemNumerics(System.Numerics.Quaternion value)` | Of `Quaternion<float>`: the same entries in the same order; the products agree too. |
| `System.Numerics.Matrix4x4 ToSystemNumerics()`, `static FromSystemNumerics(System.Numerics.Matrix4x4 value)` | Of `Matrix4x4<float>`: the transpose, so the translation moves from the last column to the last row. |
| `System.Numerics.Matrix3x2 ToSystemNumerics()`, `static FromSystemNumerics(System.Numerics.Matrix3x2 value)` | Of `Matrix3x3<float>`: an affine map of the plane. `ToSystemNumerics` throws `InvalidOperationException` unless the last row is (0, 0, 1). |
