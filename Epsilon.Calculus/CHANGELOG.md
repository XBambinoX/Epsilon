# Changelog

All notable changes to the `Epsilon.Calculus` package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-11-03

Series, limits, exact definite integrals and line integrals. It depends on `Epsilon` 1.2.0 or
later and `Epsilon.LinearAlgebra` 1.5.0 or later, as 1.0.0 did.

### Added

- **Series.** `Series(variable, point, degree)` and `Series(point, degree)` expand an
  expression at a point with exact coefficients up to (x - a)^n and the order of the
  remainder: sin(x)/x at 0 to degree 4 is 1 - (1/6) x^2 + (1/120) x^4 + O(x^5). A `Series` has
  its `Terms`, `Coefficient(k)`, `ToExpr()` and `Print()`. At a pole it starts from a negative
  power, and at a root of something under a fractional power it goes in fractional powers, on
  the right side, as acosh(x) at 1 does.
- **Limits.** `Limit(variable, point, direction)` and `Limit(point, direction)` find a limit
  exactly, at a point from both sides or from one (`LimitDirection`), and at inf and -inf,
  given as `double.PositiveInfinity` and `double.NegativeInfinity`, by Gruntz's algorithm,
  which compares how fast exponentials and logarithms grow: x ln(x) at 0 is 0, and
  (1 + 1/x)^x at inf is e. A `Limit` has a `Kind`, finite, inf or -inf, and the exact `Value`.
  Where the two sides differ, `Limit` throws `InvalidOperationException` with both. An
  oscillating part is settled by a few rules, so sin(x)/x at inf is 0; otherwise it throws
  `NotSupportedException`.
- **Exact definite integrals.** `IntegrateExact(variable, a, b)` and `IntegrateExact(a, b)` give
  F(b) - F(a) for the antiderivative that `Integrate` finds, with the checks the formula
  needs: where F is undefined at a limit or between the limits its limits there are taken,
  where it jumps while the integrand doesn't the jumps are taken off, and a divergent
  integral, or an integrand that isn't real on a part of the interval, throws
  `InvalidOperationException`. Each limit is an `Expr` or a `double`, in any mix, and may be
  inf or -inf: ln(x) from 0 to 1 is -1, and exp(-x) from pi to inf is exp(-pi).
  - An odd integrand over an interval symmetric about 0 gives 0 where it is bounded, and over
    the whole line where it falls off fast enough, with an antiderivative or without one.
  - Integrals of integrands without an elementary antiderivative come from a table: Gaussian
    integrals and their moments, the gamma function at integers and halves, Dirichlet's and
    Fresnel's integrals, the Bose and Fermi integrals by zeta at even numbers, ln(x)/(1 - x)
    and its kin on [0, 1], and ln(sin(x)) and ln(cos(x)) on [0, pi/2]. exp(-x^2) over the
    line is sqrt(pi), and sin(x)/x from 0 to inf is pi/2. A sum is integrated term by term.
  - The result is simplified beyond what the core does: sin(-u) as -sin(u), logarithms of
    rationals merged, and terms that differ by a factor a + b sqrt(d) added up.
- **Line integrals.** A `Curve` is one piece or several, each a point whose entries are
  expressions of a parameter, from `new Curve(parameter, point, from, to)`, `Curve.Segment`,
  `Curve.Polygon`, open or closed, and `Curve.Join`, traversed the other way by `Reversed()`.
  `LineIntegral(curve, variables)` and `LineIntegralExact(curve, variables)` integrate an
  expression by arc length, and a vector field along the curve, F . dr, numerically and
  exactly: the length of the unit circle is 2 pi, and the circulation of [-y, x] around it 2 pi.
  `Potential(variables)` finds a function whose gradient a field is, and throws
  `InvalidOperationException` for a field that isn't a gradient.
- **Antiderivatives** of more integrands:
  - floor, ceiling and round of a linear argument, continuous across their jumps;
  - a quotient of p + q cos u + r sin u by a + b cos u + c sin u with any numeric
    coefficients, sqrt(2) and pi among them, continuous wherever the integrand is;
  - products and powers of sines and cosines, as exp(x) cos(x)^2, by the product-to-sum
    formulas, with sinh and cosh next to them written in exponentials, and sinh and cosh
    times sin or cos by parts, as cosh(x) cos(x);
  - with the imaginary unit inside a function, the real and imaginary parts of the integrand
    for a real variable, as 1/(x + i) = (1/2) ln(x^2 + 1) - i atan(x), and a power of a complex
    linear expression whose values miss the cut of the principal power, as sqrt(x + i);
  - 1/(x sqrt(x^2 - 2x)) and other square roots of quadratics whose two sides of the gap give
    different results, by Euler's substitution t = x + sqrt(q/p) on both at once.
- **Numeric integrals.** Towards an infinity, an integrand that oscillates through sines and
  cosines of linear arguments with commensurate periods is integrated by its mean over a
  period and the waves left, summed over half periods and accelerated: sin(x)/x from 0 to inf
  is pi/2. The interval is also split at the peaks and steps of sech, cosh, tanh, atan and of
  any exponential, and where the polynomials among its parts turn, so narrow peaks far from 0
  are found on intervals of any length, and very long ones, as exp(-x^2) on [0, 1e300],
  converge.
- **Documentation**: the new guides [series](docs/series.md), [limits](docs/limits.md),
  [exact definite integrals](docs/exact-integration.md) and
  [line integrals](docs/line-integrals.md), potentials in [vector fields](docs/vector-fields.md),
  and the other guides, the [API reference](docs/api-reference.md) and the
  [known limitations](docs/limitations.md) brought up to date, with every example checked by a
  test.

### Changed

- Antiderivatives by t = tan x and t = tan(x/2) no longer jump where tan is undefined while the
  integrand doesn't, so F(b) - F(a) is the definite integral across those points too:
  1/(2 + cos(x)) gives (x + 2atan(-sin(x)/(cos(x) + sqrt(3) + 2)))/sqrt(3), not
  2atan(tan(x/2)/sqrt(3))/sqrt(3).
- Some antiderivatives come in another form: sin(x)/(1 + sin(x)) gives x + cos(x)/(sin(x) + 1)
  instead of x + 2/(tan(x/2) + 1), 1/(x^2 + 1/2) gives atan(sqrt(2) x) sqrt(2) instead of
  2atan(2x/sqrt(2))/sqrt(2), and exp(x) exp(2x) gives exp(2x) exp(x)/3 instead of exp(x)^3/3.

### Fixed

- Products of exponentials of linear arguments with different bases, as 2^x 3^x, had no
  antiderivative; it is 2^x 3^x/ln(6).
- Numeric integrals:
  - a slowly decaying tail, split again and again, threw on its last piece, as for
    sin(x)^2 exp(-x/100) on [0, inf), and oscillating integrands that converge absolutely took
    minutes or threw: cos(x)/(1 + x^2) on [0, inf) took 43 s;
  - inf/inf and 0 inf from over- and underflow after the terms had died out threw, as for
    cosh(x)/cosh(2x) on the whole line;
  - a narrow peak or a near pole next to a nonzero end was measured with x rounded:
    1/(1 + 1e10 (x - 1000)^2) was called singular, and exp(-1e10 (x - 1000)^2) came out
    4e-10 off;
  - the zeros of sines and cosines were looked for on a grid too coarse for long intervals:
    abs(sin(x)) on [0, 1000] took 24 s, and sign(sin(x)) there threw;
  - singularities at points that aren't doubles, the zeros of sines and cosines and the roots
    of quadratics, are found exactly, so 1/sqrt(abs(sin(x))) on [0, 4] and
    1/sqrt(abs(x^2 - 2)) on [0, 2] no longer throw, nor do 1/sqrt(abs(sin(x) - 0.5)) on [0, 1]
    and 1/sqrt(abs(x^3 - 2)) on [0, 2], whose singular points are found numerically;
  - a short piece far from 0 is measured from the doubles at its ends: the normal density
    with mean 1e8 and sigma 0.5 over the mean +- sigma was 2.8e-10 off;
  - a divergent integral whose integrand rounds to 0 far out, as 1/sqrt(x^2 + 1) does where
    x^2 overflows, came out as 355.5.

### Known limitations

See [Known limitations](docs/limitations.md).

## [1.0.0] - 2026-10-16

The first release. It depends on `Epsilon` 1.2.0 or later and `Epsilon.LinearAlgebra` 1.5.0 or
later.

### Added

- **Gradients.** `Gradient(variables)` gives the simplified partial derivatives as a
  `Vector<Expr>`, in the order of the variables, and `Gradient()` takes every variable of the
  expression. Numbers come from `Evaluate` and `Compile` of the vector.
  `DirectionalDerivative(direction, variables)` is the gradient dotted with a direction, which
  isn't normalized and may be symbolic.
- **Vector fields.** `Divergence(variables)` and `Curl(variables)` of a `Vector<Expr>` whose
  entry i is the component along the i-th variable; the curl in three dimensions, a field in
  the plane as [P, Q, 0].
- **Hessians and Laplacians.** `Hessian(variables)` as a symmetric `Matrix<Expr>`, each mixed
  derivative taken once and mirrored, and `Laplacian(variables)`, the divergence of the
  gradient; both also by every variable of the expression.
- **The second-derivative test.** `ClassifyCriticalPoint(point)` gives `LocalMinimum`,
  `LocalMaximum`, `Saddle` or `Inconclusive` by the signs of the eigenvalues of the Hessian at a
  point, with an eigenvalue counted as 0 relative to the largest one, so the result doesn't
  depend on the scale of the function.
- **Numeric definite integrals.** `Integrate(a, b, tolerance)` and
  `Integrate(variable, a, b, tolerance)`, to about 10 digits by default and usually to all 16,
  over finite and infinite intervals, by the tanh-sinh and exp-sinh rules. The interval is split
  where the expression may be singular or not smooth: at the zeros of denominators, of the bases
  of fractional and negative powers and of the arguments of roots, logarithms and abs, at the
  poles of tan, cot, sec and csc, at the edges of the domains of the inverse functions, at the
  kinks and jumps of abs, sign, min and max and of floor, ceiling and round of linear arguments,
  and at the peaks of exponentials. Singular ends lose no accuracy: the nodes are measured by
  their distance from the end, and the integrand is shifted to the end symbolically. A
  divergent integral throws `InvalidOperationException` instead of returning a finite number,
  and so does a singularity at a point that isn't a double where it limits the accuracy below
  the tolerance.
- **Antiderivatives.** `Integrate(variable)` and `Integrate()` give an exact, simplified
  antiderivative, without a constant of integration, and throw `NotSupportedException` where
  none is found. Exponents stay exact fractions, 1/x gives ln|x|, valid on both sides of 0, and
  the integral of a real root is real wherever the root is. They come from:
  - a **table of integrals**: powers, the exponential, logarithm, trigonometric, hyperbolic and
    inverse functions of a linear argument, abs and sign, squares, reciprocals and products of
    the trigonometric and hyperbolic functions, polynomial division, and quadratic denominators
    and square roots of quadratics with any numeric coefficients;
  - **substitution**: a part whose derivative divides the rest, with its powers and the rest of
    x written in it, x in terms of a root of a linear expression, and powers of sin and cos,
    sinh and cosh, tan and cot;
  - **integration by parts**, with u a logarithm, an inverse function or a polynomial, integrals
    that come back to themselves, such as exp(x) sin(x), solved for, and the reduction formulas
    of sec, csc, sech and csch;
  - **partial fractions** for rational functions with rational coefficients: Hermite's
    reduction for repeated factors, logarithms and arctangents over the linear and quadratic
    factors over the rationals, and over the roots of the others, those of degree 3 and 4 by
    Cardano's and Ferrari's formulas and those of any degree that divide a binomial x^n - a by
    its roots; logarithms with coefficients of the same size are merged into one. A denominator
    whose factors over the rationals are of degree 4 at most is integrated when it is given
    factored; expanded, two factors of degree 3 or 4 are told apart only in a polynomial in a
    power of x, as x^6 - 5x^3 + 6 is (x^3 - 2)(x^3 - 3);
  - **rational functions of sin and cos** by Bioche's rules (t = cos x, sin x or tan x) and
    otherwise by t = tan(x/2), and of exp, sinh and cosh by t = exp(x);
  - **square roots of quadratics** by x = h + r sin(t), r sinh(t) or r cosh(t), on both sides of
    the gap for the last.

  The imaginary unit is integrated only as a constant factor or term; elsewhere `Integrate`
  throws `NotSupportedException` rather than give an antiderivative of a real function.
- **Documentation**: the [package README](README.md) and the guides
  [getting started](docs/getting-started.md), gradients, vector fields, Hessians and critical
  points, [numeric integration](docs/numeric-integration.md),
  [symbolic integration](docs/symbolic-integration.md), the [API reference](docs/api-reference.md)
  and [known limitations](docs/limitations.md), with every example checked by a test.
- **Package.** XML documentation for every public member, symbols package (`.snupkg`) and
  Source Link.

### Known limitations

See [Known limitations](docs/limitations.md).

[Unreleased]: https://github.com/XBambinoX/Epsilon/compare/calculus-v1.1.0...HEAD
[1.1.0]: https://github.com/XBambinoX/Epsilon/releases/tag/calculus-v1.1.0
[1.0.0]: https://github.com/XBambinoX/Epsilon/releases/tag/calculus-v1.0.0
