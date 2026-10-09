# Linear algebra guide

The `Epsilon.LinearAlgebra` package, version 1.1.0, in detail. For a one-page overview see
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
numbers, `Evaluate` the matrix; see [Entry-wise operations](#entry-wise-operations).

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

## In the debugger

The debugger shows a matrix by its size and, up to 4 x 4, its entries: `2 x 3, [[1, 2, 3], [4, 5, 6]]`.
A vector shows its length and entries: `Length = 3, [1, 2.5, 3]`. Larger ones show only the
size, so that a huge matrix stays cheap to look at. Expressions appear printed, `3x^2 - 4x + 1`,
not as their fully parenthesized `ToString`. Expanded, a matrix lists its rows and a vector its
entries.

## Limitations

What version 1.1.0 doesn't do yet:

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
- **Variable names for `Parse` are an array**, `["theta", "t"]`, not a list of arguments as
  for `ExprParser.Parse`: with `params` here, the current C# compiler warns at every call.
