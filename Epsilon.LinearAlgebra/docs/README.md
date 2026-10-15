# Epsilon.LinearAlgebra documentation

Guides for the `Epsilon.LinearAlgebra` package, version 1.5.0. For a one-page overview see the
[package README](../README.md). The types are in the namespace `Epsilon.LinearAlgebra`; the
expressions in them come from the core, namespace `Epsilon.Core`, which has
[its own documentation](../../Epsilon.Core/docs/README.md). Every public member also has XML
documentation, which your IDE shows as you type.

## Start here

- [Getting started](getting-started.md) - install the package, solve a system with symbols,
  rationals and doubles, and choose the entry type you need.

## Guides

| Page | What it covers |
|---|---|
| [Matrices and vectors](matrices-and-vectors.md) | Creating and reading `Matrix<T>` and `Vector<T>`: rows, columns, submatrices, blocks, equality, text, the debugger |
| [Arithmetic](arithmetic.md) | Operators, powers, the entry-wise product, the trace, norms and approximate equality |
| [Vector geometry](vector-geometry.md) | Length, normalization, distance, cross product, angles, projection, reflection, interpolation |
| [Symbolic matrices](symbolic-matrices.md) | Matrices of expressions: parsing, LaTeX, exact determinants, characteristic polynomials, inverses and systems, entry-wise operations |
| [Numeric matrices](numeric-matrices.md) | Matrices of doubles: LU decomposition, determinant, inverse, systems, rank, and when a matrix counts as singular |
| [Least squares](least-squares.md) | Fitting lines and curves, systems of any shape, exact and symbolic fits, the solution of least norm |
| [Decompositions](decompositions.md) | QR, Cholesky, eigenvalues of symmetric matrices, singular values, the pseudo-inverse and the condition number |
| [Rational matrices](rational-matrices.md) | Exact fractions: elimination without rounding, conversions to and from doubles and expressions |
| [From formulas to fast code](fast-code.md) | Compiled delegates, Jacobians, symbolic transforms, C# source, Newton's method, inverse kinematics |
| [Fixed-size vectors and matrices](fixed-size.md) | `Vector2/3/4<T>`, `Matrix2x2/3x3/4x4<T>` for games and geometry: transforms, the inverse, System.Numerics, speed |
| [Rotations](rotations.md) | Quaternions, Euler angles, composing and decomposing translation, rotation and scale, normals |
| [Camera](camera.md) | View and projection matrices, depth ranges, a first-person camera, picking with the mouse |

## Reference

- [API reference](api-reference.md) - every public type and member with a one-line
  description.
- [Known limitations](limitations.md) - what the package doesn't do yet.
- [Changelog](../CHANGELOG.md)
- [Contributing](../../CONTRIBUTING.md)

Every code example in these pages is checked by a test in
`Epsilon.Tests/LinearAlgebra/DocsTests.cs`, so the results shown are the results you get.
