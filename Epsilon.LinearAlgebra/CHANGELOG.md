# Changelog

All notable changes to the `Epsilon.LinearAlgebra` package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

The first release, 1.0.0. It needs `Epsilon` 1.1.0 or later.

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

[Unreleased]: https://github.com/XBambinoX/Epsilon/commits/main/Epsilon.LinearAlgebra
