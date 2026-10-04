![Epsilon](https://raw.githubusercontent.com/XBambinoX/Epsilon/main/assets/icon.png)

# Epsilon.LinearAlgebra

[![NuGet](https://img.shields.io/nuget/v/Epsilon.LinearAlgebra)](https://www.nuget.org/packages/Epsilon.LinearAlgebra)
[![CI](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml/badge.svg)](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](https://github.com/XBambinoX/Epsilon/blob/main/LICENSE.txt)

**Linear algebra for [Epsilon](https://www.nuget.org/packages/Epsilon).** Matrices and vectors of
symbolic expressions or of doubles: exact determinants, inverses and solutions of symbolic
systems, and LU decomposition for numeric ones.

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
```

The arithmetic is defined for `double` and `Expr` entries: `+`, `-`, the matrix product,
scalars, `Identity`, `Zero`, `Trace`. With expressions every entry of a result is simplified:

```csharp
(2 * m).Print();                                 // [[2a, 2b], [2c, 2d]]
(m * Vector<Expr>.Parse("[x, y]")).Print();     // [a * x + b * y, c * x + d * y]
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
