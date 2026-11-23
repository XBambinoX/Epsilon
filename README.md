<div align="center">

![Epsilon](assets/icon.png)

# Epsilon

[![CI](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml/badge.svg)](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE.txt)

</div>

**Symbolic math for .NET.** An exact core for formulas - parsing, simplification,
derivatives and factoring - and a family of packages built on it, with no dependencies beyond
the .NET base library.

## Packages

| Package | What it does | NuGet |
|---|---|---|
| [Epsilon](Epsilon.Core/README.md) | The core: expressions, parsing, exact simplification, differentiation, polynomial factoring, LaTeX output, compilation to fast delegates | [![NuGet](https://img.shields.io/nuget/v/Epsilon)](https://www.nuget.org/packages/Epsilon) |
| [Epsilon.LinearAlgebra](Epsilon.LinearAlgebra/README.md) | Matrices and vectors of expressions, exact rationals or doubles: exact symbolic determinant, inverse and solve, exact elimination for rationals, LU, QR, Cholesky, eigenvalue and singular value decompositions and least squares for doubles, vector geometry; fixed-size vectors, matrices and quaternions with camera matrices for games and graphics; symbolic matrices compiled to fast delegates or C# source, Jacobians for kinematics | [![NuGet](https://img.shields.io/nuget/v/Epsilon.LinearAlgebra)](https://www.nuget.org/packages/Epsilon.LinearAlgebra) |
| [Epsilon.Calculus](Epsilon.Calculus/README.md) | Gradients, divergence, curl and potentials, Hessians, Laplacians and the second-derivative test; series and exact limits; exact antiderivatives from a table of integrals, by substitution, by parts, by partial fractions and by the substitutions for trigonometric functions and square roots of quadratics; exact definite integrals, improper ones and classic integrals without an antiderivative included; line integrals; numeric definite integrals over infinite intervals and through singularities, with divergent integrals detected | [![NuGet](https://img.shields.io/nuget/v/Epsilon.Calculus)](https://www.nuget.org/packages/Epsilon.Calculus) |
| [Epsilon.Solving](Epsilon.Solving/README.md) | Equations in one unknown: exact solutions of polynomial and rational equations, in radicals up to the fourth degree, of equations with roots, exponentials, logarithms, trigonometric and hyperbolic functions, periodic ones as families of solutions, and of polynomials in one function of the unknown, with the other variables as parameters and the conditions on them; numeric solutions where there is no formula, in a range of the real line or a rectangle of the complex plane | [![NuGet](https://img.shields.io/nuget/v/Epsilon.Solving)](https://www.nuget.org/packages/Epsilon.Solving) |

Every package targets .NET 10, and the packages built on the core bring it with them:

```bash
dotnet add package Epsilon
dotnet add package Epsilon.LinearAlgebra
dotnet add package Epsilon.Calculus
dotnet add package Epsilon.Solving
```

## Core

```csharp
using Epsilon.Core;

Expr f = ExprParser.Parse("x^3 - 2x^2 + x", "x");

f.Differentiate("x").Print();                        // 3x^2 - 4x + 1
f.TryFactorReal("x").Factored.Print();               // x * (x - 1)^2
ExprParser.Parse("cos(3pi/4)").Simplify().Print();   // -sqrt(2) / 2
```

The [core README](Epsilon.Core/README.md) walks through every feature; the
[guides](Epsilon.Core/docs/README.md) and the [API reference](Epsilon.Core/docs/api-reference.md) cover it in detail.

## Linear algebra

```csharp
using Epsilon.LinearAlgebra;

var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");

m.Determinant().Print();                                           // a * d - b * c
Matrix<Expr>.Parse("[[1, 2], [3, 4]]").Inverse().Print();          // [[-2, 1], [3/2, -1/2]]
Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]).Rank();   // 2
Matrix<double>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]).LeastSquares([1, 3, 4, 4]);   // [1.5, 1]
Matrix4x4<double>.Translation((1, 2, 3)).TransformPoint((1, 1, 1));   // [2, 3, 4]
```

The [package README](Epsilon.LinearAlgebra/README.md) shows every feature; the
[guides](Epsilon.LinearAlgebra/docs/README.md) and the
[API reference](Epsilon.LinearAlgebra/docs/api-reference.md) cover it in detail.

## Calculus

```csharp
using Epsilon.Calculus;

var f = ExprParser.Parse("x^3 - 3*x + y^2");

f.Gradient("x", "y").Print();                                // [3x^2 - 3, 2y]
f.ClassifyCriticalPoint(("x", 1), ("y", 0));                 // LocalMinimum
ExprParser.Parse("x*exp(x)").Integrate("x").Print();         // x * exp(x) - exp(x)
ExprParser.Parse("1/(x^2 - 1)").Integrate("x").Print();      // ln(abs((x - 1) / (x + 1))) / 2
ExprParser.Parse("(1 + 1/x)^x").Limit("x", double.PositiveInfinity).Print();   // e
ExprParser.Parse("exp(-x^2)").IntegrateExact("x", double.NegativeInfinity, double.PositiveInfinity).Print();   // sqrt(π)
ExprParser.Parse("ln(x)/sqrt(x)").Integrate(0, 1);             // -4
```

The [package README](Epsilon.Calculus/README.md) shows every feature; the
[guides](Epsilon.Calculus/docs/README.md) and the
[API reference](Epsilon.Calculus/docs/api-reference.md) cover it in detail.

## Equation solving

```csharp
using Epsilon.Solving;

Equation.Parse("x^2 - 5x + 6 = 0").Solve().Print();               // {2, 3}
Equation.Parse("sin(x) = 1/2").Solve().Print();                   // {π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z}
Equation.Parse("4^x - 3*2^x + 2 = 0").Solve().Print();            // {0, 1}
Equation.Parse("E = m*v^2/2").Solve("v").Print();                 // {-sqrt(2E / m), sqrt(2E / m)} if m != 0 and E / m >= 0
Equation.Parse("x = cos(x)").SolveNumerically(-10, 10).Print();   // {0.7390851332151607}
```

The [package README](Epsilon.Solving/README.md) shows every feature; the
[guides](Epsilon.Solving/docs/README.md) and the
[API reference](Epsilon.Solving/docs/api-reference.md) cover it in detail.

## Design principles

- **Exact first.** Rationals instead of doubles; exact factoring or none at all.
- **Never wrong, sometimes unsimplified.** A rule that is only valid under a condition is
  applied only when the condition is known to hold - a missed simplification is always
  safer than an incorrect one.
- **No dependencies.** Only the .NET base library.
- **Documented.** Every public member has XML documentation for IntelliSense, and every
  example in the documentation is checked by a test.

## Roadmap

The areas built on the core as separate packages, in this order:

1. **Equation solving** - available as `Epsilon.Solving`: exact solutions of equations in
   one unknown, with the families of solutions of periodic ones and the conditions on the
   other variables, and numeric solutions in a range or a rectangle of the complex plane.
   Planned next: inequalities and systems of equations.
2. **Calculus** - next in `Epsilon.Calculus`: Fourier series, double integrals over a region
   and triple integrals over a body, surface integrals of the first and second kind, and
   contour integrals.
3. **Measure theory** - `Epsilon.Calculus.Measure`: measures from length up to the Jordan
   and Lebesgue measures, signed measures and the Lebesgue integral.
4. **Transforms** - Fourier, Laplace, wavelets and more.
5. **Probability theory** - under consideration.
6. Further areas as the library grows.

Alongside them the core keeps improving, with piecewise expressions next (see the
[core roadmap](Epsilon.Core/README.md#roadmap)).

## Building from source

```bash
git clone https://github.com/XBambinoX/Epsilon.git
cd Epsilon
dotnet build
dotnet test
```

How to report a bug or send a change is described in [CONTRIBUTING](CONTRIBUTING.md);
changes are listed in the [changelog](CHANGELOG.md).

## License

[MIT](LICENSE.txt), copyright (c) 2026 Max Zakharov.
