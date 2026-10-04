# Epsilon documentation

Guides for the `Epsilon` package (namespace `Epsilon.Core`), version 1.1.0. For a
one-page overview see the [package README](../README.md). Every public member also has XML
documentation, which your IDE shows as you type.

## Start here

- [Getting started](getting-started.md) — install the package and work through one example
  end to end.

## Guides

| Page | What it covers |
|---|---|
| [Expressions](expressions.md) | The `Expr` tree: building expressions with C# operators, node types, equality, traversal, substitution |
| [Parsing](parsing.md) | The text syntax: operators, precedence, functions, constants, variables, error messages |
| [Simplification](simplification.md) | What `Simplify` does, `Expand`, `Generic` vs `Strict` mode, the domain guarantee |
| [Assumptions](assumptions.md) | Telling the simplifier what you know about variables |
| [Differentiation](differentiation.md) | Derivatives, partial and higher derivatives, supported functions |
| [Factoring](factoring.md) | Exact polynomial factoring over the reals and the complex numbers |
| [Root finding](root-finding.md) | Real and complex roots, equations, Newton's method, accuracy |
| [Evaluation](evaluation.md) | Real and complex evaluation, undefined points, branch conventions |
| [Output](output.md) | `Print`, `ToString` and `ToLatex` |
| [Numbers](numbers.md) | `Rational`, `ComplexNumber` and the constants |

## Reference

- [API reference](api-reference.md) — every public type and member with a one-line
  description.
- [Known limitations](limitations.md) — what the core doesn't do yet, and where results can
  surprise you.
- [Changelog](../CHANGELOG.md)
- [Contributing](../../CONTRIBUTING.md)

Every code example in these pages is checked by a test in
`Epsilon.Tests/DocsExamplesTests.cs`, so the results shown are the results you get.
