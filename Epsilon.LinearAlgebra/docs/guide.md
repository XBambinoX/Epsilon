# Linear algebra guide

The `Epsilon.LinearAlgebra` package, version 1.0.0, in detail. For a one-page overview see
the [package README](../README.md); every public member is listed in the
[API reference](api-reference.md). The types are in the namespace `Epsilon.LinearAlgebra`;
the expressions in them come from the core, namespace `Epsilon.Core`.

Every example on this page is checked by a test in `Epsilon.Tests/LinearAlgebraDocsTests.cs`,
so the results shown are the results you get.

**Contents:**
[Matrices and vectors](#matrices-and-vectors) -
[Arithmetic](#arithmetic) -
[Symbols or numbers](#symbols-or-numbers) -
[Symbolic determinant, inverse and systems](#symbolic-determinant-inverse-and-systems) -
[Numeric determinant, inverse, systems and rank](#numeric-determinant-inverse-systems-and-rank) -
[Entry-wise operations](#entry-wise-operations) -
[Text and LaTeX](#text-and-latex) -
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
```

- The arrays are copied: changing them afterwards doesn't change the matrix.
- All rows must have the same length; `FromRows([1, 2], [3])` throws `ArgumentException`:
  `Row 1 has 1 entries, but row 0 has 2.`
- Entries can't be null.
- Empty matrices are allowed: `FromRows()` is 0 x 0.
- `Identity` and `Zero` exist for `double` and `Expr` entries, the types with arithmetic.

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

### Vectors

`Vector<T>` is a column vector: a matrix with one column, indexed by one number. A collection
expression creates one, and it converts to that matrix implicitly:

```csharp
Vector<double> v = [1, 2, 3];
var w = Vector.Create(4.0, 5.0, 6.0);

v.Length;                    // 3
v[0];                        // 1
Matrix<double> column = v;   // [[1], [2], [3]]
```

A vector is an `IReadOnlyList<T>`, so LINQ and `foreach` work on it.

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

The arithmetic is defined for matrices and vectors of `double` and of `Expr`. Other entry
types have no arithmetic: the operators don't compile for them.

| Operation | Result |
|---|---|
| `A + B`, `A - B`, `-A` | Entry by entry; the sizes must agree. |
| `A * B` | The matrix product; `A` needs as many columns as `B` has rows. |
| `A * v` | A matrix times a column vector: a vector. |
| `s * A`, `A * s`, `A / s` | Every entry times or divided by the scalar. |
| `A.Trace()` | The sum of the diagonal; the matrix must be square. |
| `v + w`, `v - w`, `-v`, `s * v`, `v * s`, `v / s` | The same for vectors. |
| `v.Dot(w)` | The dot product. |

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
```

The simplification uses the default `Generic` mode of `Simplify`, so an entry `x/x` becomes
`1`: the result may be defined at more points. The operators of `Expr` itself only build a
tree; the operators of matrices compute, as `Differentiate` does.

Sizes that don't fit throw an `ArgumentException` that names them:

```csharp
a * a;   // ArgumentException: Cannot multiply a 2 x 3 matrix by a 2 x 3 matrix: 3 columns on the left, 2 rows on the right.
```

## Symbols or numbers

| | `Matrix<Expr>` | `Matrix<double>` |
|---|---|---|
| Entries | Symbols, functions, exact rationals | Floating point |
| Determinant, inverse, solve | Exact, without division (Berkowitz) | LU decomposition with partial pivoting |
| Rank | - | To working precision |
| Cost | Grows with the size of the result | O(n^3) |

A matrix of numbers as `Expr` computes exactly: an 8 x 8 matrix of integers has its exact
determinant and inverse in tens of milliseconds. A fully symbolic determinant has n! terms,
so a fully symbolic matrix gets slow beyond about 6 x 6. To move from symbols to numbers,
`Evaluate` the matrix; see [Entry-wise operations](#entry-wise-operations).

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

## Limitations

What version 1.0.0 doesn't do yet:

- **Only square systems.** `Solve` needs a square matrix; least squares for other shapes (by
  QR decomposition) is planned.
- **No numeric eigenvalues or SVD.** Eigenvalues are available as the roots of
  `CharacteristicPolynomial`.
- **Rank only for doubles.** The rank of a symbolic matrix depends on the values of its
  symbols.
- **Arithmetic only for `double` and `Expr`.** A matrix of `ComplexNumber`, such as the result
  of `EvaluateComplex`, or of `Rational` is a container without arithmetic.
- **Characteristic polynomials of symbolic matrices print in the core's canonical order**,
  not by powers of the variable: `a * d + t^2 - b * c + (-a - d) * t`. The value is right;
  for numbers the order is the usual `t^2 - 4t + 3`.
- **Variable names for `Parse` are an array**, `["theta", "t"]`, not a list of arguments as
  for `ExprParser.Parse`: with `params` here, the current C# compiler warns at every call.
