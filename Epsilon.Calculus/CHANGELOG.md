# Changelog

All notable changes to the `Epsilon.Calculus` package are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
  poles of tan, cot, sec and csc, at the edges of the domains of the inverse functions, at kinks
  and jumps, and at the peaks of exponentials. Singular ends lose no accuracy: the nodes are
  measured by their distance from the end, and the integrand is shifted to the end
  symbolically. A divergent integral throws `InvalidOperationException` instead of returning a
  finite number, and so does a singularity at a point that isn't a double where it limits the
  accuracy below the tolerance.
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
  - **partial fractions** for every rational function with rational coefficients whose
    denominator has factors of degree 4 at most over the rationals, or factors of x^n - a:
    Hermite's reduction for repeated factors, logarithms and arctangents over linear and
    quadratic factors, and over the roots of the others, by the roots of binomials and by
    Cardano's and Ferrari's formulas; logarithms with coefficients of the same size are merged
    into one;
  - **rational functions of sin and cos** by Bioche's rules (t = cos x, sin x or tan x) and
    otherwise by t = tan(x/2), and of exp, sinh and cosh by t = exp(x);
  - **square roots of quadratics** by x = h + r sin(t), r sinh(t) or r cosh(t), on both sides of
    the gap for the last.

  The imaginary unit is integrated only as a constant factor or term; elsewhere `Integrate`
  throws `NotSupportedException` rather than give an antiderivative of a real function.
- **Documentation**: [getting started](docs/getting-started.md), gradients, vector fields,
  Hessians and critical points, [numeric integration](docs/numeric-integration.md),
  [symbolic integration](docs/symbolic-integration.md), the [API reference](docs/api-reference.md)
  and [known limitations](docs/limitations.md), with every example checked by a test.
- **Package.** XML documentation for every public member, symbols package (`.snupkg`) and
  Source Link.

### Known limitations

See [Known limitations](docs/limitations.md).

[Unreleased]: https://github.com/XBambinoX/Epsilon/compare/calculus-v1.0.0...HEAD
[1.0.0]: https://github.com/XBambinoX/Epsilon/releases/tag/calculus-v1.0.0
