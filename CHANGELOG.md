# Changelog

All notable changes to the `Epsilon` package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- `Simplify` reuses the results for subexpressions it has already simplified within one
  call. A sum of 300 distinct terms (`1x + 2x^2 + ...`) now takes about 0.25 s instead of
  6 s, and `Differentiate` about 40% less time; results are unchanged.

### Fixed

- `ToLatex` wrote `floor` and `ceiling` without a space after the bracket command
  (`\left\lfloorx\right\rfloor`), which LaTeX read as an unknown command.
- `Parse` overflowed the stack on deeply nested input — a few hundred parentheses were
  enough to terminate the process. It now throws `FormatException` for input nested more
  than 256 levels deep or with a tree more than 500 levels deep (such as a chain of more
  than 500 terms). `Simplify` and `Differentiate` on input near these limits can still
  overflow a 1 MB stack; see [limitations](docs/limitations.md#robustness-and-performance).

## [1.0.0] — 2026-09-29

First public release of the core library, published on NuGet as `Epsilon` (namespace
`Epsilon.Core`). Requires .NET 10, no dependencies beyond the .NET base library.

### Added

- **Expressions.** An immutable expression tree (`Expr`) with structural equality, generic
  traversal (`Children`, `WithChildren`, `MapChildren`), `Substitute`, `GetVariables` and
  `DependsOn`. Expressions can be built with C# operators (`+ - * /`, unary `-`, `Pow`) and
  implicit conversions from `int`, `long`, `double` and `Rational`.
- **Parser.** `ExprParser.Parse` with implicit multiplication (`2x`, `2(x + 1)`), `|x|`,
  scientific notation, the constants `pi`/`π`, `e` and `i`, and trigonometric, hyperbolic
  and other functions. Declared variable names enable multi-letter identifiers and turn
  typos into errors that name the position.
- **Exact arithmetic.** Numbers are arbitrary-precision rationals (`Rational`); decimals such
  as `0.1` are read exactly.
- **Simplification.** Constant folding, combining like terms and repeated factors,
  cancelling common factors, the Pythagorean identity across sums and more.
  `SimplifyMode.Generic` (default) never changes a value where the expression is defined;
  `SimplifyMode.Strict` also keeps the domain unchanged. A rule cycle returns the smallest
  equivalent form instead of throwing.
- **Assumptions.** `Assumptions` (positive, negative, non-zero, integer, natural, ...) unlock
  rules that are valid only under conditions, e.g. `sqrt(x^2) = x` for `x > 0`.
  Contradictory assumptions throw an `ArgumentException` naming the variable.
- **Differentiation.** Symbolic and partial derivatives, returned simplified.
- **Polynomial factoring.** `TryFactorReal` and `TryFactorComplex` factor exactly — rational
  roots, square roots and `i` — and refuse rather than approximate.
- **Root finding.** `FindRealRoots` finds all real roots in a range, including infinite
  ranges, even-multiplicity roots and roots on the edge of the domain; undefined points are
  never reported. `FindComplexRoots` searches a rectangle with Newton's method.
  `RootFinder` finds a single root near a guess.
- **Evaluation.** Real (`Evaluate`) and complex (`EvaluateComplex`, `ComplexNumber`)
  evaluation.
- **Output.** `Print()` writes compact text that `Parse` reads back (culture-invariant);
  `ToLatex()` writes LaTeX. Sums are ordered by descending degree.
- **Package.** XML documentation for every public member, symbols package (`.snupkg`) and
  Source Link.

### Known limitations

See [docs/limitations.md](docs/limitations.md).

[1.0.0]: https://github.com/XBambinoX/Epsilon/releases/tag/v1.0.0
