# Getting started

## Install

```bash
dotnet add package Epsilon.LinearAlgebra
```

The package requires .NET 10. The core package `Epsilon`, which provides the expressions, comes
with it. Two namespaces cover everything:

```csharp
using Epsilon.Core;            // Expr, ExprParser, Rational
using Epsilon.LinearAlgebra;   // Matrix<T>, Vector<T>, Vector3<T>, Matrix4x4<T>, ...
```

`System.Numerics` has a `Vector<T>` of its own, so `Vector<double>` is ambiguous in a file that
imports both namespaces. An alias names the one you mean:

```csharp
using RealVector = Epsilon.LinearAlgebra.Vector<double>;
```

## A first example

Where do two lines cross? The lines a x + b y = p and c x + d y = q meet at the solution of a
linear system. Let's solve it once for all lines, with symbols.

**Parse the matrix and solve the system:**

```csharp
var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");
var crossing = m.Solve(Vector<Expr>.Parse("[p, q]"));

crossing.Print();          // [(d * p - b * q) / (a * d - b * c), (a * q - c * p) / (a * d - b * c)]
m.Determinant().Print();   // a * d - b * c
```

The names avoid `e` and `i`, which `Parse` reads as Euler's number and the imaginary unit. The
denominator is the determinant: where a d = b c the lines are parallel, and there is no single
crossing point. Lines with c = 2a and d = 2b are such a pair, and `Solve` says so:

```csharp
var parallel = m.Substitute("c", ExprParser.Parse("2a")).Substitute("d", ExprParser.Parse("2b"));
parallel.Solve(Vector<Expr>.Parse("[p, q]"));
// InvalidOperationException: The matrix is singular: its determinant is 0.
```

**Put in numbers.** The lines 3x + y = 1 and x + 2y = 1 cross at (0.2, 0.4):

```csharp
crossing.Evaluate(("a", 3), ("b", 1), ("c", 1), ("d", 2), ("p", 1), ("q", 1));   // [0.2, 0.4]
```

**Or compute with numbers from the start.** Doubles are fast and rounded, rationals exact:

```csharp
Matrix<double>.FromRows([3, 1], [1, 2]).Solve([1, 1]);     // [0.19999999999999998, 0.4]
Matrix<Rational>.FromRows([3, 1], [1, 2]).Solve([1, 1]);   // [1/5, 2/5]
```

With doubles, a matrix that is singular up to rounding counts as singular:

```csharp
var flat = Matrix<double>.FromRows([1, 2], [2, 4]);
flat.Determinant();   // 0
flat.Solve([1, 1]);   // InvalidOperationException: The matrix is singular: its rank is 1, not 2, to working precision.
```

**Write it as LaTeX** for a report:

```csharp
m.ToLatex();   // \begin{bmatrix} a & b \\ c & d \end{bmatrix}
```

## Which entry type?

`Matrix<T>` and `Vector<T>` hold entries of any type. The arithmetic and the algorithms exist
for three of them:

| | `Matrix<Expr>` | `Matrix<Rational>` | `Matrix<double>` |
|---|---|---|---|
| Entries | Symbols, functions, exact rationals | Exact fractions | Floating point |
| Determinant, inverse, solve | Exact, without division (Berkowitz) | Exact, Gaussian elimination | LU decomposition with partial pivoting |
| Rank | - | Exact | To working precision |
| Cost | Grows with the size of the result | Grows with the digits of the fractions | O(n^3) |

A matrix of numbers as `Expr` computes exactly: an 8 x 8 matrix of integers has its exact
determinant and inverse in tens of milliseconds. `Matrix<Rational>` gives the same exact
results in a fraction of a millisecond, as it never builds expressions. A fully symbolic
determinant has n! terms, so a fully symbolic matrix gets slow beyond about 6 x 6.

For vectors and matrices of 2, 3 or 4 numbers in fast code, such as the transforms of a game,
the package has value types that never allocate:

```csharp
Matrix4x4<double>.Translation((1, 2, 3)).TransformPoint((1, 1, 1));   // [2, 3, 4]
```

Moving between the types takes one call:

```csharp
var symbolic = Matrix<Expr>.Parse("[[x, 1], [2, x]]");
Matrix<double> numbers = symbolic.Evaluate(3);               // [[3, 1], [2, 3]]
Matrix<Rational> exact = numbers.Map(Rational.FromDouble);   // [[3, 1], [2, 3]]; 0.1 would be 1/10
Matrix<Expr> constants = exact.Map(r => (Expr)r);            // back to expressions
constants.Inverse().Print();                                 // [[3/7, -1/7], [-2/7, 3/7]]
Matrix2x2<double> small = (Matrix2x2<double>)numbers;        // a fixed-size matrix
small.ToMatrix() == numbers;                                 // true
```

## What next

- [Matrices and vectors](matrices-and-vectors.md) and [Arithmetic](arithmetic.md) - the
  containers and what every entry type can do with them.
- [Symbolic matrices](symbolic-matrices.md), [Numeric matrices](numeric-matrices.md) and
  [Rational matrices](rational-matrices.md) - determinants, inverses and systems for each entry
  type.
- [From formulas to fast code](fast-code.md) - derive with symbols, run at the speed of
  handwritten code.
- [Fixed-size vectors and matrices](fixed-size.md), [Rotations](rotations.md) and
  [Camera](camera.md) - for games and graphics.
- [Known limitations](limitations.md) - before you rely on something, check it's there.
