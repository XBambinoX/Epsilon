![Epsilon](assets/icon.png)

# Epsilon

[![CI](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml/badge.svg)](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE.txt)

**Symbolic math for .NET.** An exact core for formulas - parsing, simplification,
derivatives, factoring and roots - and a family of packages built on it, with no
dependencies beyond the .NET base library.

## Packages

| Package | What it does | NuGet |
|---|---|---|
| [Epsilon](Epsilon.Core/README.md) | The core: expressions, parsing, exact simplification, differentiation, polynomial factoring, real and complex roots, LaTeX output | [![NuGet](https://img.shields.io/nuget/v/Epsilon)](https://www.nuget.org/packages/Epsilon) |

Linear algebra and calculus are in development in this repository; see the
[roadmap](#roadmap). Every package targets .NET 10:

```bash
dotnet add package Epsilon
```

## Core

```csharp
using Epsilon.Core;

Expr f = ExprParser.Parse("x^3 - 2x^2 + x", "x");

f.Differentiate("x").Print();                        // 3x^2 - 4x + 1
f.TryFactorReal("x").Factored.Print();               // (x - 1)^2 * x
f.FindRealRoots(-10, 10);                            // [0, 1]
ExprParser.Parse("cos(3pi/4)").Simplify().Print();   // -sqrt(2) / 2
```

The [core README](Epsilon.Core/README.md) walks through every feature; the
[guides](docs/README.md) and the [API reference](docs/api-reference.md) cover it in detail.

## Design principles

- **Exact first.** Rationals instead of doubles; exact factoring or none at all.
- **Never wrong, sometimes unsimplified.** A rule that is only valid under a condition is
  applied only when the condition is known to hold - a missed simplification is always
  safer than an incorrect one.
- **No dependencies.** Only the .NET base library.
- **Documented.** Every public member has XML documentation for IntelliSense, and every
  example in the documentation is checked by a test.

## Roadmap

The next areas are built on the core as separate packages, in this order:

1. **Linear algebra** - symbolic and numeric matrices and the operations on them.
2. **Calculus** - symbolic and numeric integration (improper integrals included), gradients,
   Hessians, Laplacians. Already in this repository as an experimental project.
   Planned next, as far as time and energy allow: limits (including multivariable limits in
   2D and 3D), double and triple integrals, surface and contour integrals, Lebesgue
   integration and more.
3. **Transforms** - Fourier, wavelets and more.
4. **Probability theory** - under consideration.
5. Further areas as the library grows.

Alongside them the core keeps improving: a symbolic equation solver, piecewise expressions
and faster evaluation (see the [core roadmap](Epsilon.Core/README.md#roadmap)).

## Building from source

```bash
git clone https://github.com/XBambinoX/Epsilon.git
cd Epsilon
dotnet build
dotnet test
```

The projects in the solution and how to contribute are described in
[CONTRIBUTING](CONTRIBUTING.md); changes are listed in the [changelog](CHANGELOG.md).

## License

[MIT](LICENSE.txt), copyright (c) 2026 Max Zakharov.
