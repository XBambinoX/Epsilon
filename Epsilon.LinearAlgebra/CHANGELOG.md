# Changelog

All notable changes to the `Epsilon.LinearAlgebra` package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-10-09

It depends on `Epsilon` 1.1.1 or later, as 1.0.0 does.

### Added

- **Vector geometry** for vectors of `double` and `Expr`: `Norm`, `NormSquared`, `Normalize`,
  `Distance`, `Cross`, `Angle`, `ProjectOnto`, `Reflect`, `Outer` and a static `Lerp`. The
  numeric norm scales the entries by a power of two before squaring, which is exact, so
  `[3e300, 4e300]` has norm `5E+300` instead of infinity; `Angle` uses Kahan's formula and
  stays accurate for small angles and those near pi. With expressions the results are exact:
  `[1, 1]` normalizes to `[sqrt(2) / 2, sqrt(2) / 2]`, and the angle between `[1, 0]` and
  `[1, 1]` is `π / 4`.
- **Exact matrices and vectors of `Rational`.** Arithmetic, `Trace`, `Determinant`, `Inverse`,
  `Solve`, `Rank`, `Pow` and `Hadamard` by exact Gaussian elimination, with no tolerance: a
  matrix is singular exactly when its determinant is 0. The inverse of the 8 x 8 Hilbert
  matrix is exact, and takes a fraction of a millisecond instead of the tens of milliseconds
  of a `Matrix<Expr>` with the same numbers. Vectors of rationals have the operations whose
  results stay rational.
- **Submatrices, blocks and slices.** `m[1.., ..2]` for a submatrix, `Matrix<T>.FromBlocks`
  to assemble a matrix from blocks row by row (a vector is a one-column block, so
  `FromBlocks([a, b])` is the augmented matrix), `Diagonal()`, and `Vector<T>.Slice`, which
  makes `v[..3]` work.
- **Powers, norms and products.** `Pow(n)` by repeated squaring, with negative powers of the
  inverse; `Hadamard`, the entry-wise product of matrices and of vectors; `FrobeniusNorm`;
  `FromDiagonal`.
- **Approximate equality.** `IsApproximately` for matrices and vectors of `double` compares
  in the Frobenius norm, relative to the larger of the two, so `cos(pi/2) = 6.1E-17` where a
  rotation should have 0 does not spoil the comparison; an absolute tolerance covers
  comparisons with the zero matrix.
- **Debugger display.** Matrices show their size and, up to 4 x 4, their entries; vectors
  their length; expressions are printed. Expanded, a matrix lists its rows and a vector its
  entries.

## [1.0.0] - 2026-10-04

The first release. It depends on `Epsilon` 1.1.1 or later.

### Added

- **Matrices and vectors.** `Matrix<T>`, an immutable matrix of any entry type, built from
  rows, a two-dimensional array, a function of the indices or text, with rows, columns,
  transpose, `Map` and structural equality. `Vector<T>`, a column vector that converts to a
  one-column matrix; collection expressions create one: `Vector<double> v = [1, 2, 3];`.
- **Arithmetic** for `double` and `Expr` entries: `+`, `-`, the matrix product, matrix times
  vector, scalars, `Identity`, `Zero`, `Trace`, `Dot`. With expressions every entry of a
  result is simplified, and numbers stay exact rationals.
- **Symbolic determinant, inverse and systems.** `Determinant` and
  `CharacteristicPolynomial` by Berkowitz's algorithm, `Adjugate` by Cayley-Hamilton, all
  without division, so they hold for every value of the symbols. `Inverse` and `Solve` divide
  by the determinant only, so they are undefined exactly where the matrix is singular:
  `[[x, 1], [1, x]]` has an inverse at `x = 0` too.
- **Numeric determinant, inverse, systems and rank** by LU decomposition with partial
  pivoting. A pivot within the rounding error of the terms it came from counts as zero, so
  `[[1, 2, 3], [4, 5, 6], [7, 8, 9]]` has determinant 0 and rank 2, while the badly scaled
  `[[1e-20, 0], [0, 1]]` stays regular.
- **Entry-wise operations** on matrices and vectors of expressions: `Simplify`, `Expand`,
  `Substitute`, `Differentiate`, `GetVariables`, `Evaluate`, `EvaluateComplex`.
- **Text and LaTeX.** `Parse("[[a, b], [c, d]]")`, `Print()`, which `Parse` reads back, and
  `ToLatex()` as a `bmatrix`.
- **Package.** XML documentation for every public member, symbols package (`.snupkg`) and
  Source Link.

### Known limitations

See [Limitations](docs/guide.md#limitations) in the guide.

[Unreleased]: https://github.com/XBambinoX/Epsilon/compare/linalg-v1.1.0...HEAD
[1.1.0]: https://github.com/XBambinoX/Epsilon/releases/tag/linalg-v1.1.0
[1.0.0]: https://github.com/XBambinoX/Epsilon/releases/tag/linalg-v1.0.0
