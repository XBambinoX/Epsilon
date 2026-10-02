# Changelog

All notable changes to the `Epsilon` package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `SolveNumerically` and `SolveComplexNumerically` solve `left = right` numerically, like the
  equation overloads of `FindRealRoots` and `FindComplexRoots` did, but the right side may be
  a plain number: `ExprParser.Parse("sin(x)").SolveNumerically(0.5, -4, 4)` gives the three
  solutions of `sin(x) = 0.5` in [-4, 4].

### Deprecated

- The equation overloads `FindRealRoots(this Expr left, Expr right, ...)` and
  `FindComplexRoots(this Expr left, Expr right, ...)`; use `SolveNumerically` and
  `SolveComplexNumerically`. A plain number as the right side never reached them: C# picked
  the range overload instead, so `sin.FindRealRoots(-1, 2, 3)` searched `sin(x) = 0` on
  [-1, 2] and `sin.FindRealRoots(0.5, -4, 4)` threw. They still work and will be removed in
  a future major version.

### Fixed

- Complex division by zero returned NaN while real division returned an infinity, so
  `EvaluateComplex` disagreed with `Evaluate` at poles: `1/x` at 0 gave NaN instead of inf.
  Now `z / 0` is infinite in the direction of `z` (`1/0` = inf, `-1/0` = -inf,
  `i/0` = inf*i) and `0/0` stays NaN. Dividing an infinite value by a real or imaginary
  number also keeps it infinite instead of producing NaN, so `atanh(1)`, `atanh(-1)`,
  `csch(0)` and `coth(0)` now give inf or -inf in complex evaluation too.
- `FindComplexRoots` reported a root of multiplicity 2 or more dozens of times, each copy
  slightly off: `(x - 1)^2` gave 80 roots around 0.99999 and `x^5` gave 161 up to 0.009
  away from 0. Every root found is now refined by a method that converges fast whatever the
  multiplicity, and results are merged when `|f| < 1e-10` on the whole segment between them,
  so each root is reported once: `(x - 1)^2` gives `[1]`, `x^5` gives `[0]`. Simple roots
  also come out more precise (`x^2 + 1`: real part about `1e-17` instead of `1e-11`).

## [1.0.1] — 2026-10-01

### Added

- Package icon.

### Changed

- **Faster `Simplify` and `Differentiate`; results are unchanged.** `Simplify` reuses the
  results for subexpressions it has already simplified within one call, every expression
  computes its hash only once, and `Canonicalize` keeps the parts of a tree that don't
  change instead of rebuilding them. A sum of 300 distinct terms (`1x + 2x^2 + ...`) now
  simplifies in about 0.1 s instead of 5.5 s and allocates 126 MB instead of 5 GB; a second
  derivative takes a third of the time.
- `Expr.GetHashCode` is computed once per node and kept, so a custom node must not change
  its payload (the data behind `PayloadEquals` and `PayloadHashCode`) after it is built.
- `Canonicalize` returns the same instance for a tree that is already canonical and reuses
  the unchanged parts of any other tree.

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

[Unreleased]: https://github.com/XBambinoX/Epsilon/compare/core-v1.0.1...HEAD
[1.0.1]: https://github.com/XBambinoX/Epsilon/releases/tag/core-v1.0.1
[1.0.0]: https://github.com/XBambinoX/Epsilon/releases/tag/v1.0.0
