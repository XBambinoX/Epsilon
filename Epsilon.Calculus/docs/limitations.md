# Known limitations

What version 1.1.0 of `Epsilon.Calculus` doesn't do yet. Each example is checked
by a test (in `Epsilon.Tests/Calculus/DocsTests.cs`), so when a limitation is lifted this page
is updated with it. The expressions have
[limitations of their own](../../Epsilon.Core/docs/limitations.md).

## Symbolic integration

- **Square roots of one quadratic at a time.** `sqrt(1 + x^3)` and `1/sqrt(x^4 + 1)` aren't
  integrated, having no elementary antiderivative. And where the two sides of the gap of a
  square root differ and Euler's substitution leaves irrational coefficients, they are
  integrated apart and joined by a sign, whose derivative the core doesn't have yet:
  `1/((x - pi)*sqrt(x^2 - 2*x))` gives an antiderivative with sign(x - 1) in it.
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
  coefficients, as in 1/(x^2 + pi), is in the table, and so is 1/(sqrt(2) + cos(x)), but Bioche's
  rules and t = tan(x/2) leave fractions with irrational coefficients that the core doesn't bring
  to a common denominator: `1/(sqrt(2) + cos(x))^2` and `1/(pi + cos(x)^2)` aren't integrated.
- **floor, ceiling and round are integrated only alone and of linear arguments**: `x*floor(x)`,
  `floor(x)^2` and `floor(x^2)` throw `NotSupportedException`. Their definite integrals are
  [numbers](numeric-integration.md#kinks-jumps-and-peaks) all the same.
- **Complex numbers only where the integrand splits into real and imaginary parts**, each
  integrated by the rules of real functions; see
  [The imaginary unit](symbolic-integration.md#the-imaginary-unit). `atan(x + i)` and
  `ln(x + i*x)` throw `NotSupportedException`, `sech(x + i)` splits into parts with cos(2) next
  to cosh(2x) in a denominator, which aren't integrated, as above, and the results keep that form:
  `exp(i*x)` gives sin(x) - i cos(x), not -i exp(i x).
- **Results aren't always in their simplest form, and their derivatives even less.** All are
  right in value:

  ```csharp
  ExprParser.Parse("min(x, 1)").Integrate("x").Print();
  // ((1/2) * x^2 + x - (1/2) * abs(x - 1) * (x - 1)) / 2
  ExprParser.Parse("1/(x^2 + 1/2)^2").Integrate("x").Print();
  // 2atan(sqrt(2) * x) / sqrt(2) + 2x / (2x^2 + 1)
  ExprParser.Parse("x^3/(x - 1)").Integrate("x").Differentiate("x").Print();
  // x^2 + x + (x - 1) / abs(x - 1)^2 + 1
  ```

## Numeric integration

- **Singularities at points that aren't doubles limit the accuracy to about 1e-8**, at the
  limits, as Math.PI does for pi, and inside the interval unless the points are found exactly:
  the zeros of sines and cosines of linear arguments and the roots of quadratics are, the root
  of x^4 - 2 isn't. See [The tolerance](numeric-integration.md#the-tolerance).

  ```csharp
  ExprParser.Parse("1/sqrt(abs(x^4 - 2))").Integrate(0, 2);
  // InvalidOperationException: The integrand is singular next to x = 1.189207115002721, at a point that isn't a double, which limits the accuracy to about 2.5E-08.
  ```

- **Oscillation towards an infinity is taken apart only with linear phases and commensurate
  periods**, see [Oscillating integrands](numeric-integration.md#oscillating-integrands).
  Otherwise the integral is computed where the integrand also decays fast, and throws where it
  doesn't: sin(x^2) on [0, infinity) converges to sqrt(pi/8), and sin(x^2)/x^2 and
  sin(x)sin(sqrt(2) x)/x^2 on [1, infinity) converge too.

  ```csharp
  ExprParser.Parse("sin(x^2)*exp(-x)").Integrate(0, double.PositiveInfinity);   // 0.2705135801622119
  ExprParser.Parse("sin(x^2)").Integrate(0, double.PositiveInfinity);           // InvalidOperationException
  ExprParser.Parse("sin(x^2)/x^2").Integrate(1, double.PositiveInfinity);
  // InvalidOperationException: The integrand oscillates towards an infinity in a way that isn't taken apart yet: ...
  ```

- **No symbols besides the variable.** Substitute values for them first, see
  [Definite integrals](numeric-integration.md#definite-integrals).

## Exact definite integrals

- **Without an elementary antiderivative, only the [classic integrals](exact-integration.md#classic-integrals)**:
  `exp(-x^2)` from 0 to 1, which is sqrt(pi) erf(1)/2, `sin(x)/x` from 1 to inf, `exp(-x^3)`
  from 0 to inf, Gamma(4/3), and `x^2/(exp(x) - 1)`, 2 zeta(3), throw `NotSupportedException`;
  the [numeric integral](numeric-integration.md) gives them.
- **An antiderivative that oscillates at inf**: `cos(x)` from 0 to inf, which diverges, throws
  `NotSupportedException` rather than `InvalidOperationException`, since the limit of sin(x)
  there isn't found.
- **A point found only numerically**: where the antiderivative may be undefined at a root that
  isn't found exactly, as ln(abs(x + cos(x))) at -0.739..., `IntegrateExact` throws
  `NotSupportedException`: a limit at a decimal next to a pole is finite.
- **Constants that cancel aren't always seen to**: `1/(1 + cos(x)^4)` from 0 to 10 throws, since
  at pi/2 the coefficients of the logarithms in its antiderivative add up to 0 without the
  simplifier showing it.
- **Symbols in the limits only where the antiderivative is continuous everywhere**: `1/x` from
  1 to c, which is ln(c) for c > 0, throws.
- **Results aren't always in their simplest form**: `1/(1 + exp(x))` from -1 to 1 is
  ln(exp(1) / (exp(1) + 1)) - ln(exp(-1) / (exp(-1) + 1)), which is 1.

## Line integrals

- **Exact arc lengths that are elliptic aren't found**: the length of the ellipse
  [2 cos(t), sin(t)] throws `NotSupportedException` exactly; `LineIntegral` gives it as a number.
- **The limits of a curve's parameter are taken as increasing**: with symbols in them, as from 0
  to a, a is taken to be at least 0.
- **A potential holds where the field is defined**: [-y, x]/(x^2 + y^2) has equal derivatives
  and no potential around 0, and with a symbol, as [a*y, x], whether a field is a gradient isn't
  decided and `Potential` throws `NotSupportedException`.
- **Only curves**: integrals over surfaces and solids aren't there yet.

## Series

- **No logarithms of the variable and no essential singularities in a series**: `ln(x)` and
  `x*ln(x)` at 0, whose expansions have ln(x) in them, and `exp(1/x)` at 0 throw
  `NotSupportedException`. See [What has no series](series.md#what-has-no-series).
- **Series at a branch point only on the right side**: `acosh(x)` at 1 is
  sqrt(2) (x - 1)^(1/2) + ..., but `asin(x)` at 1, real only for x < 1, throws, though it is
  pi/2 - sqrt(2) (1 - x)^(1/2) + ... there.
- **A coefficient with a symbol in it is taken as other than 0**: `1/(x - a)` at 0 starts with
  -1/a, which doesn't hold for a = 0.
- **A point given as a double is its decimal**, as for the core's numbers: `tan(x)` at
  `Math.PI / 2` is expanded at 15707963267949/10000000000000, without its pole.

## Limits

- **Oscillations are settled by a few rules only**, see [Oscillations](limits.md#oscillations):
  `x/(2 + sin(x))` at inf, which is inf, throws `NotSupportedException`, and so does an
  oscillation without a limit, `sin(1/x)` at 0, where `InvalidOperationException` would say
  that there is none.
- **Rates of growth at an irrational ratio in one sum**: `2^x - 3^x` and `x^pi - x^2` at inf
  throw `NotSupportedException`. In one product or under one logarithm they are found:
  2^x/3^x at inf is 0, and (2^x + 3^x)^(1/x) is 3.
- **Real expressions only**: `exp((i - 1)*x)` at inf, which is 0, throws.
- **A coefficient with a symbol is taken as other than 0**, as in series, and with symbols the
  side an expression isn't real on isn't left out: `sqrt(x - a)` at x = a throws, though from
  the right it is 0.
- **A point given as a double is its decimal**: `tan(x)` at `Math.PI / 2` is
  tan(15707963267949/10000000000000), a number, not a limit from two sides of a pole.

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
