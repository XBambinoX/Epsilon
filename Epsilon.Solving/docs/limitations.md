# Known limitations

What version 1.0.0 of `Epsilon.Solving` doesn't do yet. Each example is checked by a test (in
`Epsilon.Tests/Solving/DocsTests.cs`), so when a limitation is lifted this page is updated with
it. The expressions have [limitations of their own](../../Epsilon.Core/docs/limitations.md).

## What is solved

- **One equation in one unknown.** Inequalities and systems of equations aren't solved yet.
- **Polynomials without a formula.** A quartic is solved only where its resolvent cubic has a
  rational root, and a polynomial of degree 5 or more only where it factors into polynomials of
  degree 4 or less: `x^4 + x + 1 = 0` and `x^5 - x - 1 = 0` stay unsolved, and
  [`SolveNumerically`](numeric-solutions.md) gives their roots.
- **Nested radicals stay nested.** `x^4 - 10x^2 + 1 = 0` has the roots +-sqrt(2) +- sqrt(3),
  given as `sqrt(8sqrt(6) + 20) / 2` and the like.
- **The unknown inside and outside a function.** x = cos(x), x exp(x) = 1, whose solution is a
  value of Lambert's W function, and x^x = 2 aren't solved exactly.
- **Different functions of the unknown together**, unless they are powers of one: sin(x) and
  cos(x) aren't brought to one by identities, `ln(x) = ln(2x - 1)` isn't brought to x = 2x - 1,
  and exp(x) = 2^x, of different bases, stays as it is.
- **Partly solved substitutions.** Where the polynomial in a function of the unknown is solved
  only in part, the whole equation stays unsolved: `sqrt(x) + nthroot(x, 3) = x` is
  t^2 (t^4 - t - 1) = 0 in t = nthroot(x, 6), and its solution 0 isn't listed either.
- **Over the complex numbers only polynomial and rational equations**: `exp(x) = 1` is
  unsolved by `SolveComplex`, and `SolveComplexNumerically` with a rectangle finds its solutions
  there.

## Parameters

- **The general case only.** The conditions say when the solution given is right, not what
  happens where they fail: a x = b is `{b / a} if a != 0`, and at a = 0, where there is no
  solution or every x is one, the set says nothing.
- **Values as the core simplifies them**, which isn't always the simplest form:
  `sqrt(2*x + a) = x - 1` gives (2sqrt(a + 3) + 4) / 2 for sqrt(a + 3) + 2.
- **A range for a family with parameters in its period** doesn't list its values, which
  depend on the parameters: `sin(a*x) = 0` within [0, 10] stays a family, with the bounds on
  its index as conditions.

## Numeric solutions

- **Solutions closer together than a step of the scan can be missed.** Two solutions in one
  step, where the equation doesn't change sign between them, aren't found: x = cos(x) and
  x - cos(x) = 1e-4 together give nothing in [0, 1].
- **Solutions closer than a millionth, relatively, are one.** x = cos(x) and
  x - cos(x) = 1e-7 give a single solution.
- **Underflow can look like a solution.** Where a factor of the equation underflows to 0 in
  double precision, the scan of an infinite range can find a solution there: exp(x) is 0 for x
  below about -745, and exp(x)(x - cos(x)) = 0 on (-inf, inf) gives -500000013.6409661 next to
  0.7390851332151607.
- **The complex search has no completeness guarantee**: a solution is found only if some start
  of the grid converges to it.
