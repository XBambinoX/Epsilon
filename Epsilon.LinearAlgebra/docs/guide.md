# Linear algebra guide

The `Epsilon.LinearAlgebra` package, version 1.2.0, in detail. For a one-page overview see
the [package README](../README.md); every public member is listed in the
[API reference](api-reference.md). The types are in the namespace `Epsilon.LinearAlgebra`;
the expressions in them come from the core, namespace `Epsilon.Core`.

Every example on this page is checked by a test in `Epsilon.Tests/LinearAlgebraDocsTests.cs`,
so the results shown are the results you get.

**Contents:**
[Matrices and vectors](#matrices-and-vectors) -
[Arithmetic](#arithmetic) -
[Vector geometry](#vector-geometry) -
[Norms and approximate equality](#norms-and-approximate-equality) -
[Symbols, rationals or numbers](#symbols-rationals-or-numbers) -
[Symbolic determinant, inverse and systems](#symbolic-determinant-inverse-and-systems) -
[Numeric determinant, inverse, systems and rank](#numeric-determinant-inverse-systems-and-rank) -
[Exact rational matrices](#exact-rational-matrices) -
[Entry-wise operations](#entry-wise-operations) -
[Text and LaTeX](#text-and-latex) -
[Fixed-size vectors and matrices](#fixed-size-vectors-and-matrices) -
[In the debugger](#in-the-debugger) -
[Limitations](#limitations)

## Matrices and vectors

### Creating a matrix

`Matrix<T>` is an immutable matrix with entries of any type `T`:

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

### Reading a matrix

```csharp
a.Rows;              // 2
a.Columns;           // 3
a[1, 2];             // 6
a.Row(1);            // [4, 5, 6]
a.Column(2);         // [3, 6]
a.Transpose();       // [[1, 4], [2, 5], [3, 6]]
a.Map(v => v * v);   // [[1, 4, 9], [16, 25, 36]]
```

Indices start at 0; one outside the matrix throws `ArgumentOutOfRangeException`. `Row` and
`Column` return vectors, `Map` builds a matrix of the same size with any entry type, and
`ToArray()` copies the entries into a `T[,]`.

### Submatrices, blocks and the diagonal

Ranges of rows and columns give a submatrix, with `^1` counting from the end. `FromBlocks`
goes the other way and assembles a matrix from blocks:

```csharp
var g = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
g[1.., ..2];        // [[4, 5], [7, 8]]
g[..^1, 2..];       // [[3], [6]]
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

### Vectors

`Vector<T>` is a column vector: a matrix with one column, indexed by one number. A collection
expression creates one, and it converts to that matrix implicitly:

```csharp
Vector<double> v = [1, 2, 3];
var w = Vector.Create(4.0, 5.0, 6.0);

v.Length;                    // 3
v[0];                        // 1
v[^1];                       // 3
v[..2];                      // [1, 2]
Matrix<double> column = v;   // [[1], [2], [3]]
```

A vector is an `IReadOnlyList<T>`, so LINQ and `foreach` work on it. A range such as `v[..2]`
calls `Slice(start, length)`.

### Equality and text

Two matrices are equal when they have the same size and equal entries; expressions compare
structurally, like `Expr.Equals`. `==` and `!=` do the same. `ToString()` writes the rows in
brackets, the same in every culture:

```csharp
Matrix<double>.FromRows([1.5, 2]).ToString();   // [[1.5, 2]]
```

For a matrix of expressions use `Print()`, which prints the entries as `Expr.Print` does; see
[Text and LaTeX](#text-and-latex).

## Arithmetic

The arithmetic is defined for matrices and vectors of `double`, of `Rational` and of `Expr`.
Other entry types have no arithmetic: the operators don't compile for them.

| Operation | Result |
|---|---|
| `A + B`, `A - B`, `-A` | Entry by entry; the sizes must agree. |
| `A * B` | The matrix product; `A` needs as many columns as `B` has rows. |
| `A * v` | A matrix times a column vector: a vector. |
| `s * A`, `A * s`, `A / s` | Every entry times or divided by the scalar. |
| `A.Pow(n)` | A^n by repeated squaring; the identity for n = 0, a power of the inverse for negative n. |
| `A.Hadamard(B)` | The entry-wise product; the sizes must agree. |
| `A.Trace()` | The sum of the diagonal; the matrix must be square. |
| `v + w`, `v - w`, `-v`, `s * v`, `v * s`, `v / s` | The same for vectors. |
| `v.Dot(w)`, `v.Hadamard(w)` | The dot product and the entry-wise product. |

With expressions every entry of a result is simplified. The scalars can be numbers or
expressions, and numbers stay exact:

```csharp
(m + m).Print();                                 // [[2a, 2b], [2c, 2d]]
(m * new Variable("x")).Print();                 // [[a * x, b * x], [c * x, d * x]]
(m * Vector<Expr>.Parse("[x, y]")).Print();     // [a * x + b * y, c * x + d * y]
m.Trace().Print();                               // a + d

var r = Matrix<Expr>.Parse("[[1, 2], [3, 4]]");
(r * r).Print();                                 // [[7, 10], [15, 22]]
(r / 2).Print();                                 // [[1/2, 1], [3/2, 2]]

Matrix<Expr>.Parse("[[1, x], [0, 1]]").Pow(10).Print();   // [[1, 10x], [0, 1]]
```

`Pow` needs at most 2 log2(n) products, so `Pow(int.MaxValue)` takes 60, not two billion.

The simplification uses the default `Generic` mode of `Simplify`, so an entry `x/x` becomes
`1`: the result may be defined at more points. The operators of `Expr` itself only build a
tree; the operators of matrices compute, as `Differentiate` does.

Sizes that don't fit throw an `ArgumentException` that names them:

```csharp
a * a;   // ArgumentException: Cannot multiply a 2 x 3 matrix by a 2 x 3 matrix: 3 columns on the left, 2 rows on the right.
```

## Vector geometry

For vectors of `double` and of `Expr`:

| Method | Result |
|---|---|
| `v.Norm()`, `v.NormSquared()` | The Euclidean length sqrt(v . v), and v . v. |
| `v.Normalize()` | The unit vector in the same direction. |
| `v.Distance(w)` | The length of `v - w`. |
| `v.Cross(w)` | The cross product of two vectors of length 3. |
| `v.Angle(w)` | The angle between the vectors, in radians from 0 to pi. |
| `v.ProjectOnto(w)` | The projection (v . w / w . w) w onto the line through `w`. |
| `v.Reflect(n)` | The mirror image in the plane perpendicular to `n`, which need not have length 1. |
| `Vector<T>.Lerp(start, end, t)` | (1 - t) start + t end: `start` at t = 0, `end` at t = 1. |
| `v.Outer(w)` | The matrix v w^T. |

The length is `Norm` because `Length` is the number of entries.

```csharp
Vector<double> u = [3, 4];
u.Norm();                                          // 5
u.Normalize();                                     // [0.6, 0.8]
Vector.Create<double>(1, 0, 0).Cross([0, 1, 0]);   // [0, 0, 1]
Vector.Create<double>(1, -1).Reflect([0, 1]);      // [1, 1]: a ball bouncing off the floor
Vector<double>.Lerp([0, 10], [10, 20], 0.25);      // [2.5, 12.5]
```

With expressions the results are exact:

```csharp
Vector<Expr>.Parse("[1, 1]").Normalize().Print();                          // [sqrt(2) / 2, sqrt(2) / 2]
Vector<Expr>.Parse("[1, 0]").Angle(Vector<Expr>.Parse("[1, 1]")).Print();   // π / 4
Vector<Expr>.Parse("[a, b, c]").Cross(Vector<Expr>.Parse("[x, y, z]")).Print();
// [b * z - c * y, c * x - a * z, a * y - b * x]
```

The numeric methods hold up at the extremes of floating point:

```csharp
Vector.Create(3e300, 4e300).Norm();               // 5E+300, not infinity
Vector.Create<double>(1, 0).Angle([1, 1e-10]);    // 1E-10, not 0
```

`Norm` divides the entries by a power of two before squaring them, which is exact: the result
is the naive one wherever that one neither overflows nor underflows. `Normalize`,
`ProjectOnto` and `Reflect` scale the same way. `Angle` uses Kahan's formula
2 atan2(|a - b|, |a + b|) of the unit vectors a and b, accurate at every angle; the arccosine
of the cosine gives 0 above, because the cosine 1 - 5E-21 rounds to 1.

`Normalize` and `Angle` throw for the zero vector, as do `ProjectOnto` onto it and `Reflect`
in it: the direction is undefined. Vectors of `Rational` have the methods whose result stays
rational: `NormSquared`, `Cross`, `ProjectOnto`, `Reflect`, `Lerp` and `Outer`.

## Norms and approximate equality

`FrobeniusNorm()` of a `double` or `Expr` matrix is the square root of the sum of the squares
of all its entries: the norm of the entries as one long vector. The numeric one scales like
`Norm`.

Floating point rarely gives the exact zeros of a formula: a rotation by pi/2 has
cos(pi/2) = 6.1E-17 where 0 is expected. `IsApproximately` compares matrices and vectors of
`double` up to rounding:

```csharp
double cos = Math.Cos(Math.PI / 2), sin = Math.Sin(Math.PI / 2);
var turn = Matrix<double>.FromRows([cos, -sin], [sin, cos]);
var exact = Matrix<double>.FromRows([0, -1], [1, 0]);

turn == exact;                  // false
turn.IsApproximately(exact);    // true
```

Two matrices are close when ||A - B|| <= relativeTolerance * max(||A||, ||B||) in the
Frobenius norm, with a relative tolerance of 1e-9 unless you pass another. Measured against
the whole matrix, an entry that should be 0 doesn't spoil the comparison; a relative test entry
by entry could never accept the 6.1E-17. Against the zero matrix the right side is 0 too, so
pass an absolute tolerance there:

```csharp
var h = Matrix<double>.FromRows([1, 2], [3, 4]);
var residual = h * h.Inverse() - Matrix<double>.Identity(2);
residual.IsApproximately(Matrix<double>.Zero(2, 2));                            // false
residual.IsApproximately(Matrix<double>.Zero(2, 2), absoluteTolerance: 1e-12);  // true
```

Matrices of different sizes are not close, infinite entries must be equal, and NaN is close to
nothing. Matrices of `Rational` and `Expr` compare exactly with `==`.

## Symbols, rationals or numbers

| | `Matrix<Expr>` | `Matrix<Rational>` | `Matrix<double>` |
|---|---|---|---|
| Entries | Symbols, functions, exact rationals | Exact fractions | Floating point |
| Determinant, inverse, solve | Exact, without division (Berkowitz) | Exact, Gaussian elimination | LU decomposition with partial pivoting |
| Rank | - | Exact | To working precision |
| Cost | Grows with the size of the result | Grows with the digits of the fractions | O(n^3) |

A matrix of numbers as `Expr` computes exactly: an 8 x 8 matrix of integers has its exact
determinant and inverse in tens of milliseconds. `Matrix<Rational>` gives the same exact
results in a fraction of a millisecond, as it never builds expressions; see
[Exact rational matrices](#exact-rational-matrices). A fully symbolic determinant has n!
terms, so a fully symbolic matrix gets slow beyond about 6 x 6. To move from symbols to
numbers, `Evaluate` the matrix; see [Entry-wise operations](#entry-wise-operations). For
small vectors and matrices of numbers in fast code, such as the transforms of a game, see
[Fixed-size vectors and matrices](#fixed-size-vectors-and-matrices).

## Symbolic determinant, inverse and systems

### Determinant and characteristic polynomial

```csharp
m.Determinant().Print();                                                        // a * d - b * c
Matrix<Expr>.Parse("[[x + 1, x - 1], [x - 1, x - 3]]").Determinant().Print();   // -4

var p = Matrix<Expr>.Parse("[[2, 1], [1, 2]]").CharacteristicPolynomial("t");
p.Print();           // t^2 - 4t + 3
p.FindRealRoots();   // [1, 3]
```

`CharacteristicPolynomial(t)` is det(t I - A); its roots are the eigenvalues. The variable
must not occur in the matrix: `m.CharacteristicPolynomial("a")` throws `ArgumentException`.

Both come from Berkowitz's algorithm. It builds the characteristic polynomial from the ones
of ever larger submatrices with additions and multiplications only, never dividing, so the
result holds for every value of the symbols. Intermediate values are expanded, so terms that
cancel do so: the determinant above is `-4`, not a product of brackets. The determinant is
`(-1)^n` times the constant term.

### Adjugate, inverse and Solve

The adjugate, the transposed matrix of cofactors, needs no division either and exists for
singular matrices too. The inverse is the adjugate divided by the determinant; `Solve` uses
Cramer's rule, `x = adj(A) b / det(A)`:

```csharp
m.Adjugate().Print();                            // [[d, -b], [-c, a]]
r.Inverse().Print();                             // [[-2, 1], [3/2, -1/2]]
m.Solve(Vector<Expr>.Parse("[e, f]")).Print();
// [(-b * f + e * d) / (a * d - b * c), (a * f - e * c) / (a * d - b * c)]
```

So the inverse is undefined exactly where the matrix is singular, and nowhere else:

```csharp
var inverse = Matrix<Expr>.Parse("[[x, 1], [1, x]]").Inverse();
inverse.Print();   // [[x / (x^2 - 1), -1 / (x^2 - 1)], [-1 / (x^2 - 1), x / (x^2 - 1)]]
inverse.Substitute("x", 0).Simplify().Print();   // [[0, 1], [1, 0]]
inverse.Substitute("x", 1).Simplify().Print();   // [[1 / 0, -1 / 0], [-1 / 0, 1 / 0]]
```

Gaussian elimination would have divided by the first pivot `x` and lost the point `x = 0`,
where the matrix is its own inverse. At `x = 1` the matrix is singular, and the entries are
undefined there.

A determinant that simplifies to 0 means the matrix is singular, and `Inverse` and `Solve`
throw `InvalidOperationException`: `The matrix is singular: its determinant is 0.` A
determinant that is zero but not recognized as such gives entries that are undefined
everywhere, never wrong values.

## Numeric determinant, inverse, systems and rank

For doubles, Gaussian elimination with partial pivoting (P A = L U) gives all four:

```csharp
var n = Matrix<double>.FromRows([2, 1], [4, 4]);
n.Determinant();                                  // 4
n.Solve([4, 12]);                                 // [1, 2]
n.Solve(Matrix<double>.FromRows([4, 1], [12, 0]));   // [[1, 1], [2, -1]]
n.Inverse();                                      // [[1, -0.25], [-1, 0.5]]
n.Rank();                                         // 2
```

`Solve` with a matrix solves for each of its columns at once. `Determinant`, `Inverse` and
`Solve` need a square matrix; `Rank` works for any shape.

### When is a matrix singular?

In floating point, a singular matrix rarely gives an exact zero pivot. The last pivot of
`[[1, 2, 3], [4, 5, 6], [7, 8, 9]]` comes out as about `1.1e-16`, and plain elimination then
reports a determinant of about `6.7e-16` and an inverse with entries near `1e16`. Here every
entry carries, next to its value, the sum of the magnitudes of the terms it was computed
from; a pivot no larger than the rounding error of that sum counts as zero:

```csharp
var s = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
s.Determinant();   // 0
s.Rank();          // 2
s.Inverse();       // InvalidOperationException: The matrix is singular: its rank is 2, not 3, to working precision.

var scaled = Matrix<double>.FromRows([1e-20, 0], [0, 1]);
scaled.Determinant();   // 1E-20
scaled.Inverse();       // [[1E+20, 0], [0, 1]]
```

The second matrix is badly scaled but regular: nothing cancels in it. A rule relative to the
largest entry would call it singular. The same rule decides all four methods, so
`Rank() == n` exactly when `Determinant() != 0` and `Inverse()` succeeds.

An entry that is NaN or infinite makes the result meaningless, so the four methods throw
`InvalidOperationException` and name the entry: `The matrix has a non-finite entry at (0, 1): NaN.`

## Exact rational matrices

`Matrix<Rational>` computes with the exact fractions of the core by Gaussian elimination. As
nothing is rounded, any pivot that isn't 0 will do, and there is no tolerance: a matrix is
singular exactly when its determinant is 0. Integers convert to `Rational`; a fraction is
`new Rational(1, 3)`:

```csharp
var q = Matrix<Rational>.FromRows([1, 2], [3, 4]);
q.Determinant();    // -2
q.Inverse();        // [[-2, 1], [3/2, -1/2]]
q.Solve([5, 6]);    // [-4, 9/2]
q / 3;              // [[1/3, 2/3], [1, 4/3]]

var singular = Matrix<Rational>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
singular.Rank();      // 2
singular.Inverse();   // InvalidOperationException: The matrix is singular: its rank is 2, not 3.
```

The Hilbert matrix, with entries 1 / (i + j + 1), is the classic ill-conditioned matrix. Its
inverse has integer entries, which doubles get right to only about eight digits:

```csharp
var hilbert = Matrix<Rational>.Create(8, 8, (i, j) => new Rational(1, i + j + 1));
hilbert.Inverse()[7, 7];                                       // 176679360
hilbert * hilbert.Inverse() == Matrix<Rational>.Identity(8);   // true
```

`Trace`, `Determinant`, `Inverse`, `Solve`, `Rank`, `Pow` and `Hadamard` work as for doubles;
dividing by 0 throws `DivideByZeroException`. The norms, `Normalize` and `Angle` are missing,
as the norm of `[1, 1]` is sqrt(2) and not a rational. The fractions grow with the size of the
matrix, but an 8 x 8 inverse still takes a fraction of a millisecond, against tens of
milliseconds for the same numbers in a `Matrix<Expr>`.

## Entry-wise operations

These do to every entry what the `Expr` method of the same name does, for matrices and
vectors of expressions:

| Method | Result |
|---|---|
| `Simplify(mode)`, `Simplify(assumptions, mode)` | Every entry simplified. |
| `Expand(mode)` | Every entry expanded. |
| `Substitute(variable, replacement)` | The variable replaced; not simplified. |
| `Differentiate(variable)` | The simplified partial derivatives. |
| `GetVariables()` | The variables of all entries. |
| `Evaluate(bindings)`, `Evaluate(("x", 1), ...)` | A matrix or vector of doubles. |
| `Evaluate(x)` | The same when all entries together have at most one variable. |
| `EvaluateComplex(...)` | The same three forms over the complex numbers. |

```csharp
Matrix<Expr>.Parse("[[(x + 1)^2, (x - 1)*(x + 1)]]").Expand().Print();   // [[x^2 + 2x + 1, x^2 - 1]]

var rotation = Matrix<Expr>.Parse("[[cos(x), -sin(x)], [sin(x), cos(x)]]");
rotation.Differentiate("x").Print();   // [[-sin(x), -cos(x)], [cos(x), -sin(x)]]
rotation.Determinant().Print();        // 1

m.Evaluate(("a", 1), ("b", 2), ("c", 3), ("d", 4));   // [[1, 2], [3, 4]]
Matrix<Expr>.Parse("[[x, x^2]]").Evaluate(3);         // [[3, 9]]
```

`Evaluate(3)` on a matrix with two variables throws `InvalidOperationException`:
`Expected at most 1 variable, found 2: [x, y]. Use the overload with named values for several variables.`

## Text and LaTeX

`Parse` reads a matrix as rows in brackets, `[[a, b], [c, d]]`, and a vector as
`[a, b, c]`. A comma inside parentheses belongs to the entry, so `min(a, b)` and `log(x, 2)`
are one entry each. The entries are read by `ExprParser.Parse` and, as there, not simplified:

```csharp
Matrix<Expr>.Parse("[[1/2, x + x]]").Print();              // [[1 / 2, x + x]]
Matrix<Expr>.Parse("[[1/2, x + x]]").Simplify().Print();   // [[1/2, 2x]]
Matrix<Expr>.Parse("[[theta, 2t]]", ["theta", "t"]).Print();   // [[theta, 2t]]
```

With variable names only those are variables, and they may be longer than a letter, as with
`ExprParser.Parse`. Errors name the place:

| Text | `FormatException` |
|---|---|
| `[[1, 2], [3]]` | `Row 1 has 1 entries, but row 0 has 2.` |
| `[[1, 2], [3, 4]` | `Expected ']' at position 15, found the end of the text.` |
| `[[1]] x` | `Unexpected 'x' at position 6 after the closing ']'.` |
| `[[1, sen(x)]]` with `["x"]` | `Invalid entry at (0, 1), starting at position 5: Unknown identifier 'sen' at position 0. Declared variables: x.` |

`Print()` writes text that `Parse` reads back; after `Simplify()` it is the same matrix
again (a printed number `1/2` is read as a division). `ToLatex()` writes a `bmatrix`, a
vector as one column:

```csharp
rotation.ToLatex();
// \begin{bmatrix} \cos\left(x\right) & -\sin\left(x\right) \\ \sin\left(x\right) & \cos\left(x\right) \end{bmatrix}
Vector<Expr>.Parse("[x^2, 1/2]").Simplify().ToLatex();   // \begin{bmatrix} x^{2} \\ \frac{1}{2} \end{bmatrix}
```

## Fixed-size vectors and matrices

`Matrix<T>` and `Vector<T>` hold any number of entries on the heap. For geometry, games and
real-time code, where vectors have 2, 3 or 4 entries, the package has value types:
`Vector2<T>`, `Vector3<T>`, `Vector4<T>`, `Matrix2x2<T>`, `Matrix3x3<T>` and `Matrix4x4<T>`.
They never allocate and, like everything in the package, are immutable.

### Entry types

The entries can be of any number type with the arithmetic operators: `float`, `double`,
`Half`, `decimal`, `int`, `long` and the like. What needs square roots or trigonometry, such as
`Norm`, `Normalize`, `Angle`, the rotations and the inverse, exists for `float`, `double` and
`Half` only, and doesn't compile for other types. So the cells of a grid can be `Vector3<int>`:

```csharp
Vector3<int> cell = (7, 8, 9);
cell + Vector3<int>.UnitX;   // [8, 8, 9]
cell / 2;                    // [3, 4, 4]: truncated, as in C#
```

An alias keeps the names short. The examples below use these two:

```csharp
using Vec3 = Epsilon.LinearAlgebra.Vector3<double>;
using Mat4 = Epsilon.LinearAlgebra.Matrix4x4<double>;
```

The names don't clash with `Vector3` and `Matrix4x4` of System.Numerics, which have no type
parameter.

### Fixed-size vectors

A tuple converts to a vector, `with` changes one entry of a copy, and a vector deconstructs:

```csharp
Vec3 p = (1, 2, 3);
var q = new Vec3(4, 5, 6);
var (x, y, z) = p;          // 1, 2, 3
p with { Y = 0 };           // [1, 0, 3]
p[2];                       // 3
p.XY;                       // [1, 2]
p.Dot(q);                   // 32
p.Cross(q);                 // [-3, 6, -3]
Vec3.Lerp(p, q, 0.5);       // [2.5, 3.5, 4.5]
new Vector2<double>(1, 0).Cross((0, 1));   // 1: the second vector is counterclockwise
```

| Member | Result |
|---|---|
| `X`, `Y`, `Z`, `W`, `v[i]` | The entries; they change only in `with`. |
| `Zero`, `One`, `UnitX` ... `UnitW` | Constant vectors. |
| `+`, `-`, and `*`, `/` with a number | Entry by entry. |
| `Dot`, `NormSquared`, `Cross`, `Hadamard`, `Lerp`, `Map` | For every entry type. In the plane `Cross` is the number x1 y2 - y1 x2. |
| `Norm`, `Normalize`, `Distance`, `Angle`, `ProjectOnto`, `Reflect`, `IsApproximately` | For `float`, `double` and `Half`. |
| `Min`, `Max`, `Abs`, `Clamp` | Entry by entry, for ordered number types. |

The floating-point methods are as accurate as those of `Vector<double>` (see
[Vector geometry](#vector-geometry)): the norm neither overflows nor underflows, and the angle
uses Kahan's formula. They take the fast naive formula wherever it is accurate:

```csharp
Vec3 u = (3, 4, 0);
u.Norm();                                         // 5
u.Normalize();                                    // [0.6, 0.8, 0]
new Vec3(1, -1, 0).Reflect(Vec3.UnitY);           // [1, 1, 0]
Vec3.UnitX.Angle(Vec3.UnitY);                     // 1.5707963267948966, pi / 2
new Vec3(3e300, 4e300, 0).Norm();                 // 5E+300
new Vector2<Half>((Half)300, (Half)400).Norm();   // 500, although 300^2 overflows Half
new Vec3(-5, 3, 12).Clamp(Vec3.Zero, (10, 10, 10));   // [0, 3, 10]
```

`IsApproximately` compares as for `Vector<double>`, with a relative tolerance that follows the
precision of the entries unless you pass one: 1e-9 for `double`, 1e-5 for `float` and 1e-2 for
`Half`. `==` compares the entries exactly, with NaN equal to NaN, as for `Matrix<T>`:

```csharp
Vec3 turned = (Math.Cos(Math.PI / 2), 1, 0);
turned == Vec3.UnitY;                  // false: cos(pi/2) is 6.1E-17
turned.IsApproximately(Vec3.UnitY);    // true
```

`Normalize` and `Angle` throw for the zero vector, as do `ProjectOnto` onto it and `Reflect` in
it, as for `Vector<double>`.

### Fixed-size matrices and transforms

Vectors are columns, as for `Matrix<T>`: `m * v` maps v, `a * b` applies b first, and a
translation is in the last column. So a transform is built from right to left:

```csharp
var model = Mat4.Translation((10, 0, 5))   // 3. move
          * Mat4.RotationY(Math.PI / 2)    // 2. turn a quarter about the y axis
          * Mat4.Scale(2);                 // 1. scale
model.TransformPoint((1, 0, 0));       // [10, 0, 3]
model.TransformDirection((0, 0, 1));   // [2, 0, 1.2246467991473532E-16]: no translation
```

The `1.2E-16` is `2 cos(pi/2)`: the cosine of the rounded pi/2 is not 0. Compare such results
with `IsApproximately`, see below.

| Member | Result |
|---|---|
| `Translation(offset)` | Of space for `Matrix4x4<T>`; of the plane, in homogeneous coordinates, for `Matrix3x3<T>`. |
| `Scale(factors)`, `Scale(factor)` | Along the axes; the second, uniform one for `Matrix4x4<T>`. |
| `RotationX(angle)`, `RotationY`, `RotationZ`, `Rotation(axis, angle)` | In radians, by the right-hand rule: counterclockwise seen from the tip of the axis, so `RotationZ` turns `UnitX` toward `UnitY`. `Matrix2x2<T>` has `Rotation(angle)`. |
| `FromRows`, `FromColumns` | From vectors; the columns are the images of the unit vectors. |
| `TransformPoint(p)` | (x, y, z, 1) mapped and divided by w unless w is 1, so projective maps work too. |
| `TransformDirection(d)` | (x, y, z, 0) mapped: translations leave it alone. |
| `M11` ... `M44`, `m[i, j]`, `Row(i)`, `Column(j)` | The entries; Mij is in row i, column j, and the indices of `m[i, j]` start at 0. |
| `Transpose()`, `Trace()`, `Determinant()` | The determinant by the closed formula, exact for integers. |

```csharp
var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
m.M12;             // 2
m[1, 2];           // 6, that is M23
m.Column(2);       // [3, 6, 9]
m.Determinant();   // 0

var project = Mat4.Identity with { M43 = 1, M44 = 0 };   // w = z
project.TransformPoint((2, 4, 2));                       // [1, 2, 1]
```

`Matrix3x3<T>` maps the plane in homogeneous coordinates as well, as a 2D game needs, and
`Matrix2x2<T>` holds its linear maps, which `new Matrix3x3<double>(m2)` embeds:

```csharp
var sprite = Matrix3x3<double>.Translation((100, 50)) * Matrix3x3<double>.RotationZ(Math.PI / 2);
sprite.TransformPoint((1, 0)).IsApproximately((100, 51));   // true
```

### The inverse

`Inverse()` and `TryInverse(out inverse)` of `float`, `double` and `Half` matrices use the
closed formula, the adjugate over the determinant:

```csharp
model.Inverse().TransformPoint((10, 0, 3)).IsApproximately((1, 0, 0));   // true
Mat4.Scale((1, 1, 0)).TryInverse(out _);                                 // false: it flattens space

var noisy = new Matrix3x3<double>(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9);
noisy.Determinant();                                 // 1.734723475976807E-17, not 0
noisy.TryInverse(out _);                             // false
Matrix3x3<double>.Scale((1e-20, 1, 1)).Inverse();    // [[1E+20, 0, 0], [0, 1, 0], [0, 0, 1]]
```

As with the LU decomposition (see [When is a matrix singular?](#when-is-a-matrix-singular)),
rounding rarely leaves an exact 0 for a singular matrix. A matrix counts as singular when its
determinant is no larger than the bound on the rounding error of the formula: 2, 5 or 10 times
the machine epsilon (for 2 x 2, 3 x 3 and 4 x 4) times the sum of the magnitudes of the terms
of the determinant. The rule doesn't depend on the scale, so `diag(1e-20, 1, 1)`, where nothing
cancels, inverts exactly. Rows of huge or tiny entries are first scaled by powers of two, which
is exact, so neither overflow nor underflow spoils the result. Where `TryInverse` returns
`false`, also for an entry that is NaN or infinite, `Inverse` throws
`InvalidOperationException`: `The matrix is singular to working precision.`

### Conversions and System.Numerics

`Map` converts the entries to another number type, as C# allows no conversion operators for
generic types here. `ToVector()` and `ToMatrix()` give the general types, with all their
algorithms, and an explicit conversion goes back:

```csharp
Vector3<float> f = p.Map(float.CreateTruncating);                         // [1, 2, 3] of float
Vector3<int> c = new Vec3(1.5, -2.7, 3).Map(x => (int)Math.Floor(x));    // [1, -3, 3]
model.ToMatrix().Rank();                                                  // 4
(Mat4)Matrix<double>.Identity(4) == Mat4.Identity;                        // true
```

System.Numerics multiplies row vectors from the left, `v * M`, so its matrices are the
transposes of these. `ToSystemNumerics()` and `FromSystemNumerics()` of the `float` types
transpose, so a map stays the same map; `a * b` here is `b * a` there:

```csharp
var numerics = Matrix4x4<float>.Translation((1, 2, 3)).ToSystemNumerics();
numerics.M43;                                                                       // 3: in the last row
Matrix4x4<float>.FromSystemNumerics(numerics) == Matrix4x4<float>.Translation((1, 2, 3));   // true
```

`TransformPoints`, `TransformDirections` and `Transform` map a span of vectors into another,
or into itself. The entries are stored in order without padding, so a buffer of numbers can be
read as vectors without copying:

```csharp
float[] buffer = [1, 2, 3, 4, 5, 6];
var vertices = MemoryMarshal.Cast<float, Vector3<float>>(buffer.AsSpan());
Matrix4x4<float>.Translation((1, 0, 0)).TransformPoints(vertices, vertices);
// buffer: [2, 2, 3, 5, 5, 6]
```

### Speed

Measured with BenchmarkDotNet against System.Numerics, which has `float` only and uses SIMD,
on an x86-64 processor: transforming many points takes about as long (1.1 to 1.3 times),
normalizing 1.5 times as long for `float` and 2 times for `double`, and the product and the
inverse of 4 x 4 matrices about 5 times as long, around 15 and 60 nanoseconds, as the code
here is scalar. The benchmarks are in `Epsilon.Benchmarks/FixedSizeBenchmarks.cs`.

## In the debugger

The debugger shows a matrix by its size and, up to 4 x 4, its entries: `2 x 3, [[1, 2, 3], [4, 5, 6]]`.
A vector shows its length and entries: `Length = 3, [1, 2.5, 3]`. Larger ones show only the
size, so that a huge matrix stays cheap to look at. Expressions appear printed, `3x^2 - 4x + 1`,
not as their fully parenthesized `ToString`. Expanded, a matrix lists its rows and a vector its
entries. The fixed-size types show their entries as `ToString` writes them: `[1, 2, 3]`.

## Limitations

What version 1.2.0 doesn't do yet:

- **Only square systems.** `Solve` needs a square matrix; least squares for other shapes (by
  QR decomposition) is planned.
- **No numeric eigenvalues or SVD.** Eigenvalues are available as the roots of
  `CharacteristicPolynomial`.
- **No rank for symbolic matrices.** It depends on the values of the symbols; matrices of
  doubles and rationals have one.
- **No arithmetic for `ComplexNumber`.** A matrix of complex numbers, such as the result of
  `EvaluateComplex`, is a container without arithmetic.
- **`default(Rational)` is not a valid 0.** In the core up to version 1.1.1 the default value
  of `Rational` has denominator 0: it prints as `0/0`, isn't equal to `Rational.Zero`, and
  adding it throws `DivideByZeroException`. So `new Matrix<Rational>(new Rational[2, 2])` is
  not the zero matrix: build rational matrices with `FromRows`, `Create`, `Zero` or
  `Identity`, or fill arrays with `Rational.Zero`.
- **Characteristic polynomials of symbolic matrices print in the core's canonical order**,
  not by powers of the variable: `a * d + t^2 - b * c + (-a - d) * t`. The value is right;
  for numbers the order is the usual `t^2 - 4t + 3`.
- **No SIMD in the fixed-size types.** The 4 x 4 product and inverse take about 5 times as long
  as in System.Numerics; see [Speed](#speed).
- **No quaternions, Euler angles, `LookAt` or projections yet.** They are planned next, with
  transforms built from translation, rotation and scale.
- **No fixed-size types of `Rational`.** `Rational` doesn't implement the generic math
  interfaces in the core up to version 1.1.1; it is planned with the next core version.
- **Variable names for `Parse` are an array**, `["theta", "t"]`, not a list of arguments as
  for `ExprParser.Parse`: with `params` here, the current C# compiler warns at every call.
