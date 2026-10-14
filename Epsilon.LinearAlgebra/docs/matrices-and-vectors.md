# Matrices and vectors

`Matrix<T>` is an immutable matrix with entries of any type `T`, and `Vector<T>` is a column
vector: a matrix with one column, indexed by one number. Both are containers: the arithmetic
and the algorithms come with the entry types `double`, `Rational` and `Expr`, see
[Arithmetic](arithmetic.md).

## Creating a matrix

```csharp
var a = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]);
var b = new Matrix<double>(new double[,] { { 1, 2 }, { 3, 4 } });
var c = Matrix<double>.Create(2, 3, (i, j) => 10 * i + j);   // [[0, 1, 2], [10, 11, 12]]
var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");

Matrix<double>.Identity(2);                                   // [[1, 0], [0, 1]]
Matrix<Expr>.Zero(1, 2).Print();                              // [[0, 0]]
Matrix<double>.FromDiagonal([1, 2]);                          // [[1, 0], [0, 2]]
```

- The arrays are copied: changing them afterwards doesn't change the matrix.
- All rows must have the same length; `FromRows([1, 2], [3])` throws `ArgumentException`:
  `Row 1 has 1 entries, but row 0 has 2.`
- Entries can't be null.
- Empty matrices are allowed: `FromRows()` is 0 x 0.
- `Identity`, `Zero` and `FromDiagonal` exist for `double`, `Rational` and `Expr` entries,
  the types with arithmetic.

Expressions can be written in C# as well as parsed, and the entries can be of any type at all:

```csharp
var x = new Variable("x");
Matrix<Expr>.FromRows([x, 1], [0, x]).Print();                 // [[x, 1], [0, x]]
Matrix<string>.FromRows(["a", "b"], ["c", "d"]).Transpose();   // [[a, c], [b, d]]
Matrix<int>.Create(3, 3, (i, j) => i == j ? 1 : 0);            // [[1, 0, 0], [0, 1, 0], [0, 0, 1]]
```

## Reading a matrix

```csharp
a.Rows;              // 2
a.Columns;           // 3
a.IsSquare;          // false
a[1, 2];             // 6
a.Row(1);            // [4, 5, 6]
a.Column(2);         // [3, 6]
a.Transpose();       // [[1, 4], [2, 5], [3, 6]]
a.Map(v => v * v);   // [[1, 4, 9], [16, 25, 36]]
a.Map(v => v > 2);   // [[False, False, True], [True, True, True]]
a.ToArray()[1, 0];   // 4
```

Indices start at 0; one outside the matrix throws `ArgumentOutOfRangeException`:
`a[2, 0]` gives `The matrix has 2 rows.` `Row` and `Column` return vectors, `Map` builds a
matrix of the same size with any entry type, and `ToArray()` copies the entries into a `T[,]`.

## Submatrices, blocks and the diagonal

Ranges of rows and columns give a submatrix, with `^1` counting from the end. `FromBlocks`
goes the other way and assembles a matrix from blocks:

```csharp
var g = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
g[1.., ..2];        // [[4, 5], [7, 8]]
g[..^1, 2..];       // [[3], [6]]
g[.., 1..];         // [[2, 3], [5, 6], [8, 9]]
g.Diagonal();       // [1, 5, 9]

var a2 = Matrix<double>.FromRows([1, 2], [3, 4]);
var i2 = Matrix<double>.Identity(2);
Matrix<double>.FromBlocks([a2, i2]);                         // [[1, 2, 1, 0], [3, 4, 0, 1]]
Matrix<double>.FromBlocks([a2], [i2]);                       // [[1, 2], [3, 4], [1, 0], [0, 1]]
Matrix<double>.FromBlocks([a2, Vector.Create(5.0, 6.0)]);    // [[1, 2, 5], [3, 4, 6]]
```

- `FromBlocks` takes the blocks row by row, like `FromRows`: the blocks of a row are placed
  side by side and must have the same number of rows, and every row of blocks must add up to
  the same number of columns. Otherwise it throws `ArgumentException` and names the block:
  `Block (0, 1) has 3 rows, but block (0, 0) has 2.`
- A vector is a block with one column, so the augmented matrix `[A | b]` of a system is
  `FromBlocks([a, b])`.
- `Diagonal()` works for any shape and has as many entries as the smaller dimension.
- A range outside the matrix throws `ArgumentOutOfRangeException`: `The range 1..4 is outside 0..3.`

A block matrix with zeros off the diagonal is built the same way:

```csharp
Matrix<double>.FromBlocks(
    [Matrix<double>.Identity(2), Matrix<double>.Zero(2, 1)],
    [Matrix<double>.Zero(1, 2), Matrix<double>.FromDiagonal([5])]);   // [[1, 0, 0], [0, 1, 0], [0, 0, 5]]
```

## Vectors

A collection expression creates a vector, and a vector converts to its one-column matrix
implicitly:

```csharp
Vector<double> v = [1, 2, 3];
var w = Vector.Create(4.0, 5.0, 6.0);

v.Length;                    // 3
v[0];                        // 1
v[^1];                       // 3
v[..2];                      // [1, 2]
v[1..];                      // [2, 3]
Matrix<double> column = v;   // [[1], [2], [3]]
v.ToMatrix();                // the same
v.Map(e => e * e);           // [1, 4, 9]
```

A range such as `v[..2]` calls `Slice(start, length)`. The length of a vector is `Length`, the
number of entries; its Euclidean length is `Norm()`, see [Vector geometry](vector-geometry.md).
An index outside the vector throws `ArgumentOutOfRangeException`: `The vector has 3 entries.`

A vector is an `IReadOnlyList<T>`, so LINQ and `foreach` work on it:

```csharp
v.Sum();                  // 6
v.Max();                  // 3
string.Join(" ", v);      // 1 2 3
foreach (double entry in w)
    Console.WriteLine(entry);   // 4, 5, 6
```

## Equality and text

Two matrices are equal when they have the same size and equal entries; expressions compare
structurally, like `Expr.Equals`. `==` and `!=` do the same, and `GetHashCode` agrees, so
matrices can be keys of a dictionary. `ToString()` writes the rows in brackets, the same in
every culture:

```csharp
Matrix<double>.FromRows([1.5, 2]).ToString();                               // [[1.5, 2]]
Matrix<double>.FromRows([1, 2]) == Matrix<double>.FromRows([1], [2]);       // false: 1 x 2 and 2 x 1
Matrix<double>.FromRows([double.NaN]) == Matrix<double>.FromRows([double.NaN]);   // true
Matrix<double>.FromRows([0.1 + 0.2]) == Matrix<double>.FromRows([0.3]);     // false
```

Entries compare by their `Equals`, so NaN equals NaN, and doubles must agree in every bit:
0.1 + 0.2 is 0.30000000000000004. To compare up to rounding use `IsApproximately`, see
[Arithmetic](arithmetic.md#approximate-equality).

For a matrix of expressions use `Print()`, which prints the entries as `Expr.Print` does; see
[Symbolic matrices](symbolic-matrices.md#parsing-and-printing).

## In the debugger

The debugger shows a matrix by its size and, up to 4 x 4, its entries: `2 x 3, [[1, 2, 3], [4, 5, 6]]`.
A vector shows its length and entries: `Length = 3, [1, 2.5, 3]`. Larger ones show only the
size, so that a huge matrix stays cheap to look at. Expressions appear printed, `3x^2 - 4x + 1`,
not as their fully parenthesized `ToString`. Expanded, a matrix lists its rows and a vector its
entries. The [fixed-size types](fixed-size.md) show their entries as `ToString` writes them:
`[1, 2, 3]`.
