![Epsilon](https://raw.githubusercontent.com/XBambinoX/Epsilon/main/assets/icon.png)

# Epsilon.LinearAlgebra

[![NuGet](https://img.shields.io/nuget/v/Epsilon.LinearAlgebra)](https://www.nuget.org/packages/Epsilon.LinearAlgebra)
[![CI](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml/badge.svg)](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](https://github.com/XBambinoX/Epsilon/blob/main/LICENSE.txt)

**Linear algebra for [Epsilon](https://www.nuget.org/packages/Epsilon).** Matrices and vectors of
symbolic expressions, exact rationals or doubles: exact determinants, inverses and solutions of
symbolic systems, exact elimination for rationals, LU decomposition for doubles, and vector
geometry.

```csharp
var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");

m.Determinant().Print();   // a * d - b * c
m.Adjugate().Print();      // [[d, -b], [-c, a]]
```

## Installation

```bash
dotnet add package Epsilon.LinearAlgebra
```

Requires .NET 10. The core package `Epsilon` comes with it; the namespaces are
`Epsilon.LinearAlgebra` and `Epsilon.Core`.

## Quick start

```csharp
using Epsilon.Core;
using Epsilon.LinearAlgebra;

// Symbols
var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");
m.Determinant().Print();                        // a * d - b * c
m.Trace().Print();                              // a + d

// Exact numbers: the entries are rationals, never rounded
var r = Matrix<Expr>.Parse("[[1, 2], [3, 4]]");
r.Inverse().Print();                            // [[-2, 1], [3/2, -1/2]]
r.CharacteristicPolynomial("t").Print();        // t^2 - 5t - 2

// Doubles
var n = Matrix<double>.FromRows([2, 1], [4, 4]);
n.Determinant();                                // 4
n.Solve([4, 12]);                               // [1, 2]

// Vectors
Vector<double> u = [3, 4];
u.Norm();                                       // 5
Vector.Create<double>(1, 0, 0).Cross([0, 1, 0]);   // [0, 0, 1]
```

## Features

### Matrices and vectors

`Matrix<T>` is an immutable matrix of any entry type; `Vector<T>` is a column vector, a
matrix with one column, and converts to one implicitly. Build them from rows, from a
function of the indices, from text, or with a collection expression:

```csharp
Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]).Transpose();   // [[1, 4], [2, 5], [3, 6]]
Matrix<double>.Create(2, 3, (i, j) => 10 * i + j);            // [[0, 1, 2], [10, 11, 12]]
Vector<double> v = [1, 2, 3];
v.Dot([4, 5, 6]);                                            // 32
v[..2];                                                      // [1, 2]
```

Ranges give submatrices, and `FromBlocks` assembles a matrix from blocks row by row:

```csharp
var g = Matrix<double>.FromRows([1, 2], [3, 4]);
g[1.., ..];                                         // [[3, 4]]
Matrix<double>.FromBlocks([g, Vector.Create(5.0, 6.0)]);   // [[1, 2, 5], [3, 4, 6]]
```

The arithmetic is defined for `double`, `Rational` and `Expr` entries: `+`, `-`, the matrix
product, scalars, `Pow`, `Hadamard`, `Identity`, `Zero`, `FromDiagonal`, `Trace`. With
expressions every entry of a result is simplified:

```csharp
(2 * m).Print();                                 // [[2a, 2b], [2c, 2d]]
(m * Vector<Expr>.Parse("[x, y]")).Print();     // [a * x + b * y, c * x + d * y]
Matrix<Expr>.Parse("[[1, x], [0, 1]]").Pow(10).Print();   // [[1, 10x], [0, 1]]
```

### Vector geometry

`Norm`, `Normalize`, `Distance`, `Cross`, `Angle`, `ProjectOnto`, `Reflect`, `Lerp` and `Outer`
for vectors of doubles and of expressions. The numeric ones hold up at the extremes of floating
point; the symbolic ones are exact:

```csharp
Vector.Create<double>(1, -1).Reflect([0, 1]);          // [1, 1]
Vector.Create(3e300, 4e300).Norm();                    // 5E+300, not infinity
Vector.Create<double>(1, 0).Angle([1, 1e-10]);         // 1E-10, where acos gives 0
Vector<Expr>.Parse("[1, 0]").Angle(Vector<Expr>.Parse("[1, 1]")).Print();   // π / 4
```

### Symbolic results that hold wherever they are defined

The determinant and the characteristic polynomial come from Berkowitz's algorithm, which never
divides, so no entry has to be assumed nonzero. The inverse is the adjugate divided by the
determinant: it is undefined exactly where the matrix is singular, and nowhere else.

```csharp
var inverse = Matrix<Expr>.Parse("[[x, 1], [1, x]]").Inverse();
inverse.Print();             // [[x / (x^2 - 1), -1 / (x^2 - 1)], [-1 / (x^2 - 1), x / (x^2 - 1)]]
inverse.Substitute("x", 0).Simplify().Print();  // [[0, 1], [1, 0]]
```

Elimination with `x` as the first pivot would have divided by `x` and lost the point `x = 0`.
Terms cancel exactly, functions included:

```csharp
Matrix<Expr>.Parse("[[x + 1, x - 1], [x - 1, x - 3]]").Determinant().Print();    // -4
Matrix<Expr>.Parse("[[cos(x), -sin(x)], [sin(x), cos(x)]]").Inverse().Print();
                                                 // [[cos(x), sin(x)], [-sin(x), cos(x)]]
```

### Numeric results that know when they can't be trusted

For doubles, LU decomposition with partial pivoting gives the determinant, the inverse,
solutions and the rank. A pivot that is no larger than the rounding error of the terms it was
computed from counts as zero, so a matrix that is singular stays singular:

```csharp
var s = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
s.Determinant();   // 0, not the 6.7e-16 that plain elimination gives
s.Rank();          // 2
s.Inverse();       // InvalidOperationException: The matrix is singular: its rank is 2, not 3, to working precision.
```

A badly scaled matrix such as `[[1e-20, 0], [0, 1]]` is still regular: nothing cancels there.

To compare results, `IsApproximately` measures the difference against the norm of the whole
matrix, so the `6.1E-17` that `cos(pi/2)` leaves where a rotation has 0 doesn't matter:

```csharp
var turn = Matrix<double>.FromRows([Math.Cos(Math.PI / 2), -1], [1, Math.Cos(Math.PI / 2)]);
turn.IsApproximately(Matrix<double>.FromRows([0, -1], [1, 0]));   // true
```

### Exact rational matrices

`Matrix<Rational>` computes with exact fractions by Gaussian elimination: no rounding, no
tolerance, and much faster than the same numbers as expressions. Even the ill-conditioned
Hilbert matrix inverts exactly:

```csharp
var hilbert = Matrix<Rational>.Create(8, 8, (i, j) => new Rational(1, i + j + 1));
hilbert.Inverse()[7, 7];                                       // 176679360
hilbert * hilbert.Inverse() == Matrix<Rational>.Identity(8);   // true
Matrix<Rational>.FromRows([1, 2], [3, 4]).Inverse();           // [[-2, 1], [3/2, -1/2]]
```

### Entry-wise operations, text and LaTeX

`Simplify`, `Expand`, `Substitute`, `Differentiate`, `Evaluate` and `EvaluateComplex` work on
every entry. `Print()` writes text that `Parse` reads back; `ToLatex()` writes a `bmatrix`:

```csharp
var rotation = Matrix<Expr>.Parse("[[cos(x), -sin(x)], [sin(x), cos(x)]]");
rotation.Differentiate("x").Print();   // [[-sin(x), -cos(x)], [cos(x), -sin(x)]]
m.Evaluate(("a", 1), ("b", 2), ("c", 3), ("d", 4));    // [[1, 2], [3, 4]]
Vector<Expr>.Parse("[x^2, 1/2]").Simplify().ToLatex();
                                       // \begin{bmatrix} x^{2} \\ \frac{1}{2} \end{bmatrix}
```

## Good to know

- `System.Numerics` has a `Vector<T>` of its own, so `Vector<double>` is ambiguous in a file
  that uses both namespaces. Name the type you need with an alias,
  `using RealVector = Epsilon.LinearAlgebra.Vector<double>;`, or write `System.Numerics.BigInteger`
  in full instead of importing the namespace.
- Like `ExprParser.Parse`, `Parse` doesn't simplify: `[[1/2, x + x]]` keeps `1 / 2` and
  `x + x` until `Simplify()`.
- The operators on `Expr` matrices simplify in the default `Generic` mode, so an entry `x/x`
  becomes `1`.
- `default(Rational)` is not a valid 0 in the core up to 1.1.1, so `new Rational[2, 2]` doesn't
  make a zero matrix; use `Matrix<Rational>.Zero(2, 2)` or `FromRows`.

## Documentation

The [guide](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.LinearAlgebra/docs/guide.md)
explains every feature in detail, the
[API reference](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.LinearAlgebra/docs/api-reference.md)
lists every public type and member, and the
[changelog](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.LinearAlgebra/CHANGELOG.md)
lists the changes. The expressions themselves are documented with the
[core](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Core/README.md).

## License

MIT, copyright (c) 2026 Max Zakharov - see
[LICENSE](https://github.com/XBambinoX/Epsilon/blob/main/LICENSE.txt).
