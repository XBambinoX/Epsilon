![Epsilon](https://raw.githubusercontent.com/XBambinoX/Epsilon/main/assets/icon.png)

# Epsilon

[![NuGet](https://img.shields.io/nuget/v/Epsilon)](https://www.nuget.org/packages/Epsilon)
[![CI](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml/badge.svg)](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](https://github.com/XBambinoX/Epsilon/blob/main/LICENSE.txt)

**Symbolic math for .NET.** Parse formulas, simplify them exactly, differentiate, factor
polynomials and find roots — with no dependencies beyond the .NET base library.

```csharp
Expr f = ExprParser.Parse("x^3 - 2x^2 + x", "x");

f.Differentiate("x").Print();            // 3x^2 - 4x + 1
f.TryFactorReal("x").Factored.Print();   // (x - 1)^2 * x
```

## Installation

```bash
dotnet add package Epsilon
```

Requires .NET 10. The package contains the core library; its namespace is `Epsilon.Core`.

## Quick start

```csharp
using Epsilon.Core;

Expr f = ExprParser.Parse("x^3 - 2x^2 + x", "x");

f.Evaluate(3);                           // 12
f.Differentiate("x").Print();            // 3x^2 - 4x + 1
f.Differentiate("x").ToLatex();          // 3x^{2} - 4x + 1
f.FindRealRoots(-10, 10);                // [0, 1]
f.TryFactorReal("x").Factored.Print();   // (x - 1)^2 * x
```

Expressions can also be built with ordinary C# operators:

```csharp
var x = new Variable("x");

(x * x + 3 * x - x).Simplify().Print();  // x^2 + 2x
(x.Pow(2) + 2 * x + 1).Print();          // x^2 + 2x + 1
```

## Features

### Exact arithmetic

Numbers are arbitrary-precision rationals, never floating point, so nothing is lost along the way:

```csharp
ExprParser.Parse("0.1 + 0.2").Simplify().Print();       // 3/10
ExprParser.Parse("x^2 - 2").TryFactorReal("x").Factored.Print();
                                                         // (x + sqrt(2)) * (x - sqrt(2))
```

Factoring is exact as well: roots are rational or written with square roots, and a
polynomial that can't be factored that way is left alone rather than approximated.

### Simplification that respects the domain

`Simplify` folds constants, combines like terms and factors, cancels common factors and
applies identities such as `sin(x)^2 + cos(x)^2 = 1`. It never changes the value of an
expression where that expression is defined. Two modes decide what happens where it isn't:

```csharp
Expr e = ExprParser.Parse("x/x");

e.Simplify().Print();                     // 1      Generic (default): may become defined at x = 0
e.Simplify(SimplifyMode.Strict).Print();  // x / x  Strict: keeps exactly the same domain
```

`Expand` multiplies out the brackets that `Simplify` keeps:

```csharp
ExprParser.Parse("(x + 1)^2 - x^2").Expand().Print();   // 2x + 1
```

### Assumptions

Tell the simplifier what you know about your variables to unlock rules that are only valid
under conditions:

```csharp
Expr s = ExprParser.Parse("sqrt(x^2)");

s.Simplify().Print();                                        // abs(x)
s.Simplify(Assumptions.None.AssumePositive("x")).Print();    // x
```

Contradictory assumptions (`x > 0` and `x < 0`) are rejected with an error naming the variable.

### Differentiation

Symbolic derivatives, partial derivatives included, returned already simplified:

```csharp
ExprParser.Parse("x^2 * y + y^3").Differentiate("y").Print();   // 3y^2 + x^2
```

### Root finding

All real roots in a range — including infinite ranges, double roots and roots on the edge
of the domain — and complex roots via Newton's method:

```csharp
ExprParser.Parse("sqrt(1 - x^2)").FindRealRoots();              // [-1, 1]
ExprParser.Parse("sin(x)").SolveNumerically(0.5, -4, 4);
                                                                 // ≈ [-3.665, 0.524, 2.618]
ExprParser.Parse("x^2 + 1").FindComplexRoots(-2, 2, -2, 2);      // ≈ [-i, i]
```

Points where the expression is undefined are never reported as roots.

### Real and complex evaluation, text and LaTeX output

```csharp
ExprParser.Parse("x*y + 1").Evaluate(("x", 2), ("y", 3));      // 7
ExprParser.Parse("sqrt(x)").EvaluateComplex(-4);                // ≈ 2i
```

`Print()` writes compact text that `Parse` reads back; `ToLatex()` writes LaTeX.

## Parser syntax

| Kind | Syntax |
|---|---|
| Operators | `+  -  *  /  ^`, parentheses, `\|x\|` for absolute value |
| Implicit multiplication | `2x`, `2(x + 1)`, `x y` |
| Numbers | `12`, `1.5`, `.5`, `1e-5`, `2.5E3` — always a decimal point, never a comma |
| Constants | `pi` (or `π`), `e`, `i` |
| Trigonometric | `sin cos tan cot sec csc`, `asin acos atan` |
| Hyperbolic | `sinh cosh tanh coth sech csch`, `asinh acosh atanh` |
| Other functions | `exp ln sqrt abs sign floor ceiling round` |
| Two arguments | `min(a, b)`, `max(a, b)`, `log(x, base)`, `nthroot(x, n)` |

Without a list of variables, every single letter is a variable and `xy` means `x*y`. Pass
the names to allow longer ones and to catch typos:

```csharp
ExprParser.Parse("theta^2 + 2t", "theta", "t").Print();   // theta^2 + 2t
ExprParser.Parse("sen(x)", "x");
// FormatException: Unknown identifier 'sen' at position 0. Declared variables: x.
```

## Documentation

The [guides](https://github.com/XBambinoX/Epsilon/blob/main/docs/README.md) cover every
feature in detail: [parsing](https://github.com/XBambinoX/Epsilon/blob/main/docs/parsing.md),
[simplification](https://github.com/XBambinoX/Epsilon/blob/main/docs/simplification.md),
[root finding](https://github.com/XBambinoX/Epsilon/blob/main/docs/root-finding.md) and more;
the [API reference](https://github.com/XBambinoX/Epsilon/blob/main/docs/api-reference.md)
lists every public type and member.
Before relying on something, check the
[known limitations](https://github.com/XBambinoX/Epsilon/blob/main/docs/limitations.md);
changes are listed in the [changelog](https://github.com/XBambinoX/Epsilon/blob/main/CHANGELOG.md).

## Design principles

- **Exact first.** Rationals instead of doubles; exact factoring or none at all.
- **Never wrong, sometimes unsimplified.** A rule that is only valid under a condition is
  applied only when the condition is known to hold — a missed simplification is always
  safer than an incorrect one.
- **No dependencies.** Only the .NET base library.
- **Documented.** Every public member has XML documentation for IntelliSense.

## Roadmap

This package is the core. The next areas are built on it as separate packages, in this order:

1. **Calculus** — symbolic and numeric integration (improper integrals included), gradients,
   Hessians, Laplacians. Already in this repository as an experimental project.
   Planned next, as far as time and energy allow: limits (including multivariable limits in
   2D and 3D), double and triple integrals, surface and contour integrals, Lebesgue
   integration and more.
2. **Linear algebra** — symbolic and numeric matrices and the operations on them.
3. **Transforms** — Fourier, wavelets and more.
4. **Probability theory** — under consideration.
5. Further areas as the library grows.

Alongside them the core keeps improving:

- **Equation solver** — symbolic solutions of `f(x) = g(x)` with every branch:
  `sin(x) = 1/2` → `π/6 + 2πk, 5π/6 + 2πk`, verified by substitution, with a clearly
  marked numeric fallback.
- Piecewise expressions (derivatives of `floor`, `min`, `max`) and faster evaluation for
  hot loops.

## Building from source

```bash
git clone https://github.com/XBambinoX/Epsilon.git
cd Epsilon
dotnet build
dotnet test
```

## License

MIT © Max Zakharov — see [LICENSE](https://github.com/XBambinoX/Epsilon/blob/main/LICENSE.txt).
