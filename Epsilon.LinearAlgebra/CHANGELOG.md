# Changelog

All notable changes to the `Epsilon.LinearAlgebra` package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.0] - 2026-10-11

It depends on `Epsilon` 1.1.1 or later, as before.

### Added

- **Fixed-size vectors** `Vector2<T>`, `Vector3<T>` and `Vector4<T>`: immutable value types for
  geometry and games, of `float`, `double` or any other number type with the arithmetic
  operators (`Half`, `decimal`, `int`, ...). A tuple converts to them,
  `Vector3<double> v = (1, 2, 3);`, `with` changes one entry, and they deconstruct. `Dot`,
  `NormSquared`, `Cross` (in the plane the number x1 y2 - y1 x2), `Hadamard`, `Lerp` and `Map`
  work for every entry type; `Min`, `Max`, `Abs` and `Clamp` for ordered ones.
- **Geometry of floating-point vectors.** `Norm`, `Normalize`, `Distance`, `Angle`,
  `ProjectOnto`, `Reflect` and `IsApproximately` for `float`, `double` and `Half`, as accurate
  as for `Vector<double>`: `(3e300, 4e300, 0)` has norm `5E+300`, and a `Vector2<Half>` of
  `(300, 400)` has norm 500 although 300^2 overflows `Half`. The default relative tolerance of
  `IsApproximately` follows the precision: 1e-9 for `double`, 1e-5 for `float`, 1e-2 for `Half`.
- **Fixed-size matrices** `Matrix2x2<T>`, `Matrix3x3<T>` and `Matrix4x4<T>` with column
  vectors, as for `Matrix<T>`: `m * v` maps v, `a * b` applies b first, a translation is in the
  last column. `Translation`, `Scale`, `TransformPoint` (divided by w unless w is 1, so it is
  ready for projective maps), `TransformDirection`, `Transpose`, `Trace` and `Determinant`,
  exact for integers. `Matrix3x3<T>` also maps the plane in homogeneous coordinates.
- **Rotations and a closed-form inverse** for `float`, `double` and `Half`: `RotationX`,
  `RotationY`, `RotationZ` and `Rotation(axis, angle)` by the right-hand rule, and `Inverse` and
  `TryInverse`, which call a matrix singular when its determinant is within the rounding error
  of the formula. So `[[0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]]`, whose determinant
  comes out as `1.7E-17`, has no inverse, while `diag(1e-20, 1, 1)` inverts exactly; rows of
  huge or tiny entries are scaled by powers of two first, so neither overflow nor underflow
  spoils the result.
- **Conversions.** `ToVector()` and `ToMatrix()` to the general types, and explicit conversions
  back; `ToSystemNumerics()` and `FromSystemNumerics()` for the `float` types, which transpose
  the matrices because System.Numerics multiplies row vectors; `Map(float.CreateTruncating)`
  between number types.
- **Batch transforms of spans.** `TransformPoints`, `TransformDirections` and `Transform` from a
  `ReadOnlySpan` into a `Span`, in place too. The entries are stored in order without padding,
  so a `float[]` vertex buffer can be read as `Vector3<float>` without copying.
- **Benchmarks** against System.Numerics in `Epsilon.Benchmarks/FixedSizeBenchmarks.cs`. Batch
  transforms of points take about as long; the 4 x 4 product and inverse, scalar here and SIMD
  there, about 5 times as long.

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

[Unreleased]: https://github.com/XBambinoX/Epsilon/compare/linalg-v1.2.0...HEAD
[1.2.0]: https://github.com/XBambinoX/Epsilon/releases/tag/linalg-v1.2.0
[1.1.0]: https://github.com/XBambinoX/Epsilon/releases/tag/linalg-v1.1.0
[1.0.0]: https://github.com/XBambinoX/Epsilon/releases/tag/linalg-v1.0.0
