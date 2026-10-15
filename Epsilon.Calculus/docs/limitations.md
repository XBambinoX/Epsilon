# Known limitations

What `Epsilon.Calculus`, version 1.0.0 in development, doesn't do yet. Each example is checked
by a test (in `Epsilon.Tests/Calculus/DocsTests.cs`), so when a limitation is lifted this page
is updated with it. The expressions have
[limitations of their own](../../Epsilon.Core/docs/limitations.md).

## Symbolic integration

- **No integration by parts yet.** `x*exp(x)` and `sec(x)^3` throw `NotSupportedException`;
  see [What isn't integrated yet](symbolic-integration.md#what-isnt-integrated-yet).
- **Substitution doesn't rewrite the integrand first.** It finds a part whose derivative
  divides the rest as it stands, so `sin(2*x)/(1 + cos(x)^2)` waits for sin(2x) to be written as
  2 sin(x) cos(x). Trigonometric substitutions such as x = sec(t), for
  `1/(x^2*sqrt(x^2 - 1))`, aren't tried.
- **Rational functions with denominators of degree 2 at most.** `1/(x^3 + 1)` needs partial
  fractions.
- **Quadratics with numeric coefficients only.** In `1/(x^2 + a^2)` the sign of the quadratic
  depends on a symbol, and it isn't integrated.
- **Results aren't always in their simplest form, and their derivatives even less.** All are
  right in value, and an antiderivative may carry a constant, such as the 2 in the second one:

  ```csharp
  ExprParser.Parse("min(x, 1)").Integrate("x").Print();
  // ((1/2) * x^2 + x - (1/2) * abs(x - 1) * (x - 1)) / 2
  ExprParser.Parse("1/(1 + sqrt(x))").Integrate("x").Print();
  // -2ln(sqrt(x) + 1) + 2 * (sqrt(x) + 1)
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
