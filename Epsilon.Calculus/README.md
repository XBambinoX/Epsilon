![Epsilon](https://raw.githubusercontent.com/XBambinoX/Epsilon/main/assets/icon.png)

# Epsilon.Calculus

[![NuGet](https://img.shields.io/nuget/v/Epsilon.Calculus)](https://www.nuget.org/packages/Epsilon.Calculus)
[![CI](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml/badge.svg)](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](https://github.com/XBambinoX/Epsilon/blob/main/LICENSE.txt)

**Calculus for [Epsilon](https://www.nuget.org/packages/Epsilon).** Gradients, divergence and
curl, Hessians, Laplacians and the second-derivative test for functions of several variables.
Exact antiderivatives from a table of integrals, by substitution, by parts, by partial fractions
and by the substitutions for trigonometric functions and square roots of quadratics. Numeric
definite integrals over infinite intervals and through singularities, to all 16 digits as a
rule, that throw for a divergent integral instead of returning a number.

```csharp
var f = ExprParser.Parse("x^3 - 3*x + y^2");

f.Gradient("x", "y").Print();                    // [3x^2 - 3, 2y]
f.ClassifyCriticalPoint(("x", 1), ("y", 0));     // LocalMinimum
ExprParser.Parse("x*exp(x)").Integrate("x").Print();   // exp(x) * x - exp(x)
```

## Installation

```bash
dotnet add package Epsilon.Calculus
```

Requires .NET 10. The core package `Epsilon` and `Epsilon.LinearAlgebra` come with it; the
namespaces are `Epsilon.Calculus`, `Epsilon.Core` and `Epsilon.LinearAlgebra`.

## Quick start

```csharp
using Epsilon.Core;
using Epsilon.LinearAlgebra;
using Epsilon.Calculus;

var f = ExprParser.Parse("x^2*y + sin(y)");

// Derivatives as vectors and matrices of expressions
f.Gradient("x", "y").Print();     // [2x * y, x^2 + cos(y)]
f.Hessian("x", "y").Print();      // [[2y, 2x], [2x, -sin(y)]]
f.Laplacian("x", "y").Print();    // 2y - sin(y)

// Antiderivatives
ExprParser.Parse("x*ln(x)").Integrate("x").Print();       // (1/2) * ln(x) * x^2 - (1/4) * x^2
ExprParser.Parse("1/(x^2 - 1)").Integrate("x").Print();   // (1/2) * ln(abs((x - 1) / (x + 1)))

// Definite integrals, numerically
ExprParser.Parse("exp(-x^2)").Integrate(double.NegativeInfinity, double.PositiveInfinity);
// 1.7724538509055159, the square root of pi
ExprParser.Parse("ln(x)").Integrate(0, 1);   // -1, though ln(x) goes to -infinity at 0
```

The operations are extension members, called like members of `Expr` and `Vector<Expr>`.

## Features

### Gradients and vector fields

`Gradient` gives a `Vector<Expr>` of simplified partial derivatives, in the order of the
variables. `Evaluate` gives numbers at a point, and `Compile` a delegate that computes them many
times faster. `DirectionalDerivative`, `Divergence` and `Curl` complete the first derivatives:

```csharp
var gradient = f.Gradient("x", "y");
gradient.Evaluate(("x", 1), ("y", 2));        // [4, 0.5838531634528576]
gradient.Compile("x", "y")(1, 2);             // [4, 0.5838531634528576]

ExprParser.Parse("x^2*y").DirectionalDerivative([1, 1], "x", "y").Print();   // 2x * y + x^2
Vector<Expr>.Parse("[x*y, y^2]").Divergence("x", "y").Print();               // 3y
Vector<Expr>.Parse("[-y, x, 0]").Curl("x", "y", "z").Print();                // [0, 0, 2]
```

### Hessians and critical points

`Hessian` gives a symmetric `Matrix<Expr>`, and `ClassifyCriticalPoint` tells a minimum from a
maximum or a saddle by the signs of its eigenvalues:

```csharp
var g = ExprParser.Parse("x^3 - 3*x + y^2");
g.ClassifyCriticalPoint(("x", 1), ("y", 0));    // LocalMinimum
g.ClassifyCriticalPoint(("x", -1), ("y", 0));   // Saddle
ExprParser.Parse("x^4 + y^4").ClassifyCriticalPoint(("x", 0), ("y", 0));   // Inconclusive
```

Compiled, the gradient and the Hessian give Newton's method, here to the minimum of the
Rosenbrock function at the end of its long curved valley:

```csharp
var rosenbrock = ExprParser.Parse("(1 - x)^2 + 100*(y - x^2)^2");
var slope = rosenbrock.Gradient("x", "y").Compile("x", "y");
var hessian = rosenbrock.Hessian("x", "y").Compile("x", "y");

Vector<double> p = [-1.2, 1.0];
for (int i = 0; i < 8; i++)
    p -= hessian(p[0], p[1]).Solve(slope(p[0], p[1]));

p;   // [1, 1]
```

### Antiderivatives

`Integrate("x")` gives an exact, simplified antiderivative. Exponents stay exact fractions,
1/x gives ln|x|, and roots stay real:

```csharp
ExprParser.Parse("2*x*cos(x^2)").Integrate("x").Print();          // sin(x^2)
ExprParser.Parse("exp(x)*sin(x)").Integrate("x").Print();         // (exp(x) * sin(x) - cos(x) * exp(x)) / 2
ExprParser.Parse("nthroot(x, 3)").Integrate("x").Print();         // 3nthroot(x, 3) * x / 4
ExprParser.Parse("1/(x^2*sqrt(4 + x^2))").Integrate("x").Print(); // -sqrt(x^2 + 4) / (4x)
```

Rational functions are integrated by partial fractions, with Hermite's reduction for repeated
factors, over the roots of cubic and quartic factors in radicals, and rational functions of sin
and cos by Bioche's rules or by t = tan(x/2):

```csharp
ExprParser.Parse("x/((x - 1)*(x + 2))").Integrate("x").Print();   // (1/3) * ln(abs(x - 1)) + (2/3) * ln(abs(x + 2))
ExprParser.Parse("1/(x^2 + 1)^3").Integrate("x").Print();         // (3x^3 + 5x) / (8(x^2 + 1)^2) + (3/8) * atan(x)
ExprParser.Parse("sin(x)^3/(1 + cos(x)^2)").Integrate("x").Print();   // cos(x) - 2atan(cos(x))
ExprParser.Parse("1/(2 + cos(x))").Integrate("x").Print();        // 2atan(tan(x / 2) / sqrt(3)) / sqrt(3)
```

Where no antiderivative is found, or none made of elementary functions exists, `Integrate`
throws `NotSupportedException`.

### Definite integrals

`Integrate(a, b)` computes a definite integral numerically, over finite and infinite
intervals. The interval is split where the expression may be singular, has a kink or a jump, or
an exponential has its peak, and singular ends lose no accuracy. A divergent integral throws:

```csharp
ExprParser.Parse("1/sqrt(1 - x^2)").Integrate(-1, 1);    // 3.141592653589763
ExprParser.Parse("abs(x)^(-2/3)").Integrate(-1, 8);      // 8.999999999999998
ExprParser.Parse("exp(-(x - 200)^2)").Integrate(double.NegativeInfinity, double.PositiveInfinity);   // 1.772453850905513
ExprParser.Parse("exp(x^2)").Integrate(0, 1);            // 1.4626517459071819, with no antiderivative

ExprParser.Parse("1/x").Integrate(0, 1);
// InvalidOperationException: The integral diverges at x = 0, or converges there too slowly for double precision.
```

## Good to know

- Names longer than one letter must be declared when parsing:
  `ExprParser.Parse("omega*sigma^2", "omega", "sigma")`. Otherwise `omega` is a product of
  letters, with `e` read as Euler's number and `i` as the imaginary unit.
- Antiderivatives by t = tan x or t = tan(x/2) jump where tan is undefined, even where the
  integrand is continuous, so F(b) - F(a) across such a point isn't the definite integral.
  `Integrate(a, b)` is right there.
- floor, ceiling, round, sign, min and max have no derivatives in the core yet, so gradients and
  Hessians of them throw. `Integrate(a, b)` takes all of them, and `Integrate("x")` sign, min
  and max.
- The last digit or two of a numeric integral can differ from one system to another, whose C
  libraries round sinh, cosh and tanh differently in the last bit.
- Gradients are the `Vector<T>` of Epsilon.LinearAlgebra, which clashes with that of
  System.Numerics in a file that uses both namespaces; the
  [linear algebra README](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.LinearAlgebra/README.md#good-to-know)
  has the alias for it.

## Documentation

The [guides](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Calculus/docs/README.md)
explain every feature in detail, the
[API reference](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Calculus/docs/api-reference.md)
lists every public type and member, the
[known limitations](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Calculus/docs/limitations.md)
say what isn't done yet, and the
[changelog](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Calculus/CHANGELOG.md)
lists the changes. The expressions are documented with the
[core](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Core/README.md), the vectors and
matrices with [Epsilon.LinearAlgebra](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.LinearAlgebra/README.md).

## License

MIT, copyright (c) 2026 Max Zakharov - see
[LICENSE](https://github.com/XBambinoX/Epsilon/blob/main/LICENSE.txt).
