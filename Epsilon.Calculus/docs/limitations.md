# Known limitations

What version 1.0.0 of `Epsilon.Calculus` doesn't do yet. Each example is checked
by a test (in `Epsilon.Tests/Calculus/DocsTests.cs`), so when a limitation is lifted this page
is updated with it. The expressions have
[limitations of their own](../../Epsilon.Core/docs/limitations.md).

## Symbolic integration

- **Identities are used only in rational functions of sin and cos.** There sin(k x) and
  cos(k x) are written in sin x and cos x, but elsewhere the integrand is taken as it stands:
  `exp(x)*cos(x)^2` waits for cos(x)^2 to be (1 + cos(2x))/2; see
  [What isn't integrated yet](symbolic-integration.md#what-isnt-integrated-yet).
- **Antiderivatives by t = tan x or t = tan(x/2) jump** where tan is undefined, even where the
  integrand is continuous, as 1/(2 + cos(x)) is, so F(b) - F(a) across such a point isn't the
  definite integral; see
  [Trigonometric and hyperbolic functions](symbolic-integration.md#trigonometric-and-hyperbolic-functions).
  The [numeric integral](numeric-integration.md) is right there.
- **Square roots of one quadratic at a time.** `sqrt(1 + x^3)` and `1/sqrt(x^4 + 1)` aren't
  integrated, having no elementary antiderivative. And where a substitution by cosh leaves the
  side of the gap inside a sum, the two sides are integrated apart and joined by a sign, whose
  derivative the core doesn't have yet: `1/(x*sqrt(x^2 - 2*x))` gives an antiderivative with
  sign(x - 1) in it.
- **Rational functions with a factor of degree 5 or more** over the rationals that divides no
  x^n - a, such as `1/(x^5 - x - 1)`. The antiderivative is a sum over the roots of the factor,
  which can't be written in radicals in general.
- **Expanded products of factors of degree 3 and 4** are split only in a polynomial in a power
  of x, as x^6 - 5x^3 + 6 is (x^3 - 2)(x^3 - 3). `1/(x^6 - x^4 + x^3 - 2*x - 2)` isn't
  integrated, but given factored, `1/((x^3 - x - 1)*(x^3 + 2))` is.
- **Long antiderivatives over cubic and quartic factors.** A root from Cardano's or Ferrari's
  formula is a nested radical that the simplifier doesn't denest, and it is in every
  coefficient: `1/(x^3 - x - 1)` gives about a thousand characters, `1/(x^4 + x + 1)` three
  thousand.
- **Rational functions with symbols or irrational numbers in the denominator.** In
  `1/(x^2 + a^2)` the sign of the quadratic depends on a symbol, and partial fractions need
  rational coefficients, so `1/(x^3 + pi)` isn't integrated either. A quadratic with any numeric
  coefficients, as in 1/(x^2 + pi), is in the table.
- **floor, ceiling and round have no antiderivatives** yet. Their definite integrals are
  [numbers](numeric-integration.md#kinks-jumps-and-peaks) all the same, and sign, min and max
  are integrated through abs.
- **Complex numbers only as constant factors and terms.** The rules are those of real
  functions, so `i*x` and `x + i` are integrated, but `1/(x + i)` throws
  `NotSupportedException`: ln|x + i| isn't its antiderivative.
- **Results aren't always in their simplest form, and their derivatives even less.** All are
  right in value:

  ```csharp
  ExprParser.Parse("min(x, 1)").Integrate("x").Print();
  // ((1/2) * x^2 + x - (1/2) * abs(x - 1) * (x - 1)) / 2
  ExprParser.Parse("1/(x^2 + 1/2)").Integrate("x").Print();
  // 2atan(2x / sqrt(2)) / sqrt(2)
  ExprParser.Parse("x^3/(x - 1)").Integrate("x").Differentiate("x").Print();
  // x^2 + x + (x - 1) / abs(x - 1)^2 + 1
  ```

## Numeric integration

- **A narrow feature far from 0 on an infinite interval can be missed.** The interval is split
  at the peaks of exponentials, but not of other functions. Far out the nodes are too sparse to
  land on a peak this narrow, and every value they see is 0. Split the interval at the peak:

  ```csharp
  var peak = ExprParser.Parse("sech(10*(x - 1000))^2");
  peak.Integrate(double.NegativeInfinity, double.PositiveInfinity);   // 0, but the integral is 0.2
  peak.Integrate(double.NegativeInfinity, 1000) + peak.Integrate(1000, double.PositiveInfinity);   // 0.19999999999998613
  ```

- **Singularities at points that aren't doubles limit the accuracy to about 1e-8**, see
  [The tolerance](numeric-integration.md#the-tolerance).
- **Integrals that converge only by cancellation are reported as divergent**, such as of
  sin(x)/x on [0, infinity).
- **Very long finite intervals don't converge.** Integrate to infinity instead:

  ```csharp
  ExprParser.Parse("exp(-x^2)").Integrate(0, 1e300);   // InvalidOperationException
  ExprParser.Parse("exp(-x^2)").Integrate(0, double.PositiveInfinity);   // 0.8862269254527579
  ```

- **No symbols besides the variable.** Substitute values for them first, see
  [Definite integrals](numeric-integration.md#definite-integrals).

## Derivatives

- **floor, ceiling, round, sign, min and max have no derivatives** in the core yet, so
  gradients and Hessians of expressions with them throw `NotSupportedException`.
- **Fractions aren't brought to a common denominator**, so a Laplacian or a curl that is 0 may
  show only in value:

  ```csharp
  var laplacian = ExprParser.Parse("ln(x^2 + y^2)").Laplacian("x", "y");
  laplacian.Print();   // (-4x^2 + 2 * (x^2 + y^2)) / (x^2 + y^2)^2 + (-4y^2 + 2 * (x^2 + y^2)) / (x^2 + y^2)^2
  laplacian.Evaluate(("x", 1), ("y", 2));   // 0
  ```
