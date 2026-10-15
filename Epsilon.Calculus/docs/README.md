# Epsilon.Calculus documentation

Guides for the `Epsilon.Calculus` package, which is in development: version 1.0.0 isn't
released yet, and these pages grow with it. The types are in the namespace `Epsilon.Calculus`.
The expressions come from the core, namespace `Epsilon.Core`, which has
[its own documentation](../../Epsilon.Core/docs/README.md); gradients and Hessians are the
vectors and matrices of [Epsilon.LinearAlgebra](../../Epsilon.LinearAlgebra/docs/README.md).
Every public member also has XML documentation, which your IDE shows as you type.

## Start here

- [Getting started](getting-started.md) - add the package, find and classify the critical
  points of a function, and integrate.

## Guides

| Page | What it covers |
|---|---|
| [Gradients](gradients.md) | Gradients and directional derivatives, numbers from them, gradient descent |
| [Vector fields](vector-fields.md) | Divergence and curl, and the identities between them |
| [Hessians and critical points](hessians.md) | Hessians, Laplacians, the second-derivative test, Newton's method |
| [Numeric integration](numeric-integration.md) | Definite integrals over finite and infinite intervals, singularities, the tolerance, divergent integrals |
| [Symbolic integration](symbolic-integration.md) | Antiderivatives from a table of integrals, by substitution and by parts: the forms, exact results, checking them |

## Reference

- [API reference](api-reference.md) - every public type and member with a one-line
  description.
- [Known limitations](limitations.md) - what the package doesn't do yet.
- [Contributing](../../CONTRIBUTING.md)

Every code example in these pages is checked by a test in `Epsilon.Tests/Calculus/DocsTests.cs`,
so the results shown are the results you get.
