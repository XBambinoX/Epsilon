# Changelog

All notable changes to the `Epsilon` package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.1] - 2026-10-04

The library is unchanged; this release updates the package README.

### Fixed

- The links in the package README to the guides, the API reference and the known
  limitations, which moved to `Epsilon.Core/docs/` in the repository.

### Changed

- The package README links to the roadmap of the repository instead of repeating it.

## [1.1.0] - 2026-10-03

### Added

- `SolveNumerically` and `SolveComplexNumerically` solve `left = right` numerically, like the
  equation overloads of `FindRealRoots` and `FindComplexRoots` did, but the right side may be
  a plain number: `ExprParser.Parse("sin(x)").SolveNumerically(0.5, -4, 4)` gives the three
  solutions of `sin(x) = 0.5` in [-4, 4].
- `Expand` multiplies out products and positive integer powers of sums and simplifies the
  result, so like terms combine: `(x + 1)^2 - x^2` gives `2x + 1`, `(x + 1)*(x - 1)` gives
  `x^2 - 1`. Function arguments are expanded too. A quotient stays one fraction with its
  numerator and denominator expanded, unless the denominator is a number: `(x + 1)^2/2`
  gives `(1/2) * x^2 + x + 1/2`.
- Exact values of functions. `Simplify` evaluates the trigonometric functions at multiples
  of pi/6 and pi/4 (`sin(pi/6)` gives `1/2`, `cos(3pi/4)` gives `-sqrt(2) / 2`), `asin`,
  `acos` and `atan` at the matching values (`acos(-1/2)` gives `2 * π / 3`), `ln(1)`,
  `ln(e)`, `ln(e^x)`, `exp(0)`, `sign(0)` and the hyperbolic functions at 0. Square roots of
  rational numbers are reduced and computed with exactly: `sqrt(8)` gives `2sqrt(2)`,
  `1/sqrt(2)` gives `sqrt(2) / 2`, `sqrt(2)*sqrt(3)` gives `sqrt(6)`, `2sin(pi/3)` gives
  `sqrt(3)`; `nthroot(16, 3)` gives `2nthroot(2, 3)`. Where a function is undefined
  (`tan(pi/2)`, `csc(0)`) it is left as it is.

### Changed

- `TryFactorReal` and `TryFactorComplex` expand their input first, so the polynomial no
  longer has to be written out: `(x - 2)^4` and `(x^2 - 1)*(x - 1)` are factored instead of
  returning `false`.
- `Simplify` handles a sum as a whole: each term is simplified once, then all of them are
  combined in one pass. Before, every shorter sum inside it (the first two terms, the first
  three, ...) was simplified as well, so the time grew with the square of the number of
  terms. A sum of 300 distinct terms now takes 0.8 ms instead of 88 ms and allocates 1.3 MB
  instead of 126 MB; 300 like terms take 0.3 ms instead of 8.5 ms. Everyday formulas run at
  about the same speed with 30% fewer allocations, second derivatives about 10% faster.
  The terms of a simplified sum keep the order in which they first appear, so some results
  print with their terms in a different order.

### Deprecated

- The equation overloads `FindRealRoots(this Expr left, Expr right, ...)` and
  `FindComplexRoots(this Expr left, Expr right, ...)`; use `SolveNumerically` and
  `SolveComplexNumerically`. A plain number as the right side never reached them: C# picked
  the range overload instead, so `sin.FindRealRoots(-1, 2, 3)` searched `sin(x) = 0` on
  [-1, 2] and `sin.FindRealRoots(0.5, -4, 4)` threw. They still work and will be removed in
  a future major version.

### Fixed

- The numbers in a quotient were never reduced: `2x/2` stayed `2x / 2`, `4x/6` stayed
  `4x / 6` and `2*(pi/6)` gave `2 * π / 6`. They now reduce like a fraction, with the sign
  in the numerator: `x`, `2x / 3`, `π / 3`, and `x/(-2)` gives `-x / 2`. A number in a sum
  is not factored out: `(2x + 2)/2` stays as it is.
- A power of a power was only combined when the inner exponent was odd: `(x^2)^3` stayed
  as it was, and `Expand` gave `x^6` for it while `Simplify` didn't. Two integer exponents
  are now always multiplied: `(x^2)^3` gives `x^6`, `(x^2)^-1` gives `x^-2`. A non-integer
  outer exponent still keeps the inner power, as `(x^2)^(1/2)` is `|x|`, not `x`.
- `Simplify` didn't know that `i^2 = -1`: `i*i` stayed `i^2`, and `(x + i)*(x - i)` expanded
  to `x^2 - i^2`. Integer powers of `i` now reduce with period 4 (`i^3` gives `-i`,
  `i^-1` gives `-i`), an `i` in a denominator moves up (`1/(2i)` gives `-i / 2`), and
  `(x + i)*(x - i)` expands to `x^2 + 1`.
- Dividing by a quotient was not simplified: `1/(x/2)` stayed as it was. Now `a/(b/c)` becomes
  `a*c/b`, so `1/(x/2)` gives `2 / x` and `y/(x/y)` gives `y^2 / x`. The left side is undefined
  where `c = 0`, so in Strict mode this needs `c` to be provably nonzero: `1/(2/x)` gives
  `x / 2` in Generic mode and stays in Strict mode.
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
- `sec(x)^2 - tan(x)^2 = 1` and `csc(x)^2 - cot(x)^2 = 1` were only applied to two terms
  standing next to each other in this order, so `tan(x)^2 - sec(x)^2` and
  `y - csc(x)^2 + cot(x)^2` stayed as they were. Like `sin(x)^2 + cos(x)^2 = 1`, they now work
  across the whole sum with any coefficients: those two give `-1` and `y - 1`, and
  `2sec(x)^2 - tan(x)^2` gives `sec(x)^2 + 1` (in Strict mode too, as `sec(x)^2` keeps the
  domain). Of the two squares of an identity, the one with the smaller coefficient is the one
  replaced: `3sin(x)^2 + 2cos(x)^2` gives `sin(x)^2 + 2` instead of `-cos(x)^2 + 3`.
- A negated multiple was not combined with like terms: `-(2x) + x` stayed `-2x + x`. It now
  gives `-x`.

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
  overflow a 1 MB stack; see [limitations](docs/limitations.md#robustness).

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

[Unreleased]: https://github.com/XBambinoX/Epsilon/compare/core-v1.1.1...HEAD
[1.1.1]: https://github.com/XBambinoX/Epsilon/releases/tag/core-v1.1.1
[1.1.0]: https://github.com/XBambinoX/Epsilon/releases/tag/core-v1.1.0
[1.0.1]: https://github.com/XBambinoX/Epsilon/releases/tag/core-v1.0.1
[1.0.0]: https://github.com/XBambinoX/Epsilon/releases/tag/v1.0.0
