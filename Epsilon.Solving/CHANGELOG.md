# Changelog

All notable changes to the `Epsilon.Solving` package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-23

The first release. It needs `Epsilon` 1.4.0 or later, whose root finding it takes over.

### Added

- **Equations.** `Equation`, from two expressions or read by `Equation.Parse("sin(x) = 1/2")`,
  with its `Left` and `Right` sides, `Print` and `ToLatex`.
- **Exact solutions.** `Solve()` and `Solve(variable, assumptions)` over the real numbers, and
  `SolveComplex()` and `SolveComplex(variable)` over the complex ones, give a `SolutionSet`: the
  solutions each once, numbers in ascending order, or every value with the conditions where
  the equation holds, and in `Unsolved` the parts that weren't solved. No step enlarges the
  domain, so a root of a denominator, or a value where a logarithm is undefined, is no
  solution. Solved are:
  - polynomial and rational equations: linear and quadratic ones, cubics in radicals by
    Cardano's formula or with cosines, quartics whose resolvent cubic has a rational root, and
    polynomials of higher degree that factor into those, a repeated root once;
  - equations in exp, ln, powers, roots, abs, the inverse trigonometric functions and the
    hyperbolic functions and their inverses, undone by their inverses branch by branch, each
    with what it needs of the other side, as exp(u) = c a c > 0;
  - equations in sin, cos, tan, cot, sec and csc, as families of solutions, one value for every
    integer k, with families half a period apart merged into one, and conditions on k where
    only some of its values are solutions;
  - equations with roots of the unknown, raised to powers until a polynomial is left, without
    the solutions that adds;
  - polynomials in one function of the unknown, solved for that function first, with
    exponentials of one base, as 4^x and 2^x or exp(2x) and exp(-x), and roots of one
    radicand, as x^(2/3) and x^(1/3) or sqrt(x) and x, brought to one of them.
- **Parameters.** The other variables of an equation are parameters, and each solution has in
  its `Conditions` those on them it needs, as the core's `Condition`: `{b / a} if a != 0`. They
  are put as simply as found, and assumptions about the parameters decide them. An assumption
  about the sign of the unknown leaves out the solutions that don't meet it.
- **Families of solutions.** A `Solution` that is a family has its `Period` and its `Index`,
  and `At(index)` gives one of its values. `SolutionSet.Within(from, to)` lists the values in a
  range.
- **Numeric solutions.** `SolveNumerically` and `SolveComplexNumerically` give a
  `NumericSolutionSet<double>` or `NumericSolutionSet<ComplexNumber>`: the exact solutions
  evaluated and every root of a polynomial without a formula, and with a range of the real
  line, infinite limits allowed, or a rectangle of the complex plane, the values of each family
  there and the solutions of the rest, found by a scan or by Newton's method from a grid.
  `FindRoot` and `FindComplexRoot` look near a guess. They take over the root finding of
  `Epsilon` 1.3.0; the [core changelog](../Epsilon.Core/CHANGELOG.md#140---2026-11-23) has the
  replacement of each method.

### Known limitations

See [Known limitations](docs/limitations.md).

[Unreleased]: https://github.com/XBambinoX/Epsilon/compare/solving-v1.0.0...HEAD
[1.0.0]: https://github.com/XBambinoX/Epsilon/releases/tag/solving-v1.0.0
