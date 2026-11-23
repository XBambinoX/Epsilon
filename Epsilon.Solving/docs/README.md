# Epsilon.Solving documentation

Guides for the `Epsilon.Solving` package, version 1.0.0. For a one-page overview see the
[package README](../README.md). The types are in the namespace `Epsilon.Solving`. The
expressions come from the core, namespace `Epsilon.Core`, which has
[its own documentation](../../Epsilon.Core/docs/README.md). Every public member also has XML
documentation, which your IDE shows as you type.

## Start here

- [Getting started](getting-started.md) - add the package, solve an equation and read the
  solutions, exactly and as numbers.

## Guides

| Page | What it covers |
|---|---|
| [Equations and solution sets](solution-sets.md) | Writing equations, solving them, what a solution set holds: the solutions, what isn't solved, every value or none, the domain, assumptions about the unknown |
| [Polynomials and rational equations](polynomials.md) | Linear and quadratic equations, cubics and quartics in radicals, factoring, repeated roots, denominators, the complex numbers |
| [Functions and families of solutions](functions.md) | Exponentials, logarithms, roots, absolute values, inverse and hyperbolic functions undone, trigonometric equations as families of solutions, their values one by one and in a range |
| [Roots of the unknown and substitutions](roots-and-substitutions.md) | Raising to powers and the solutions that adds, polynomials in one function of the unknown, exponentials of one base and roots of one radicand |
| [Parameters](parameters.md) | Other variables in the equation, the conditions on them, formulas solved for one of their variables, assumptions |
| [Numeric solutions](numeric-solutions.md) | Solutions as numbers, the roots of any polynomial, scanning a range, guesses, the complex plane |

## Reference

- [API reference](api-reference.md) - every public type and member with a one-line
  description.
- [Known limitations](limitations.md) - what the package doesn't do yet.
- [Contributing](../../CONTRIBUTING.md)

Every code example in these pages is checked by a test in `Epsilon.Tests/Solving/DocsTests.cs`,
so the results shown are the results you get.
