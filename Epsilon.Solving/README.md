![Epsilon](https://raw.githubusercontent.com/XBambinoX/Epsilon/main/assets/icon.png)

# Epsilon.Solving

[![NuGet](https://img.shields.io/nuget/v/Epsilon.Solving)](https://www.nuget.org/packages/Epsilon.Solving)
[![CI](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml/badge.svg)](https://github.com/XBambinoX/Epsilon/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](https://github.com/XBambinoX/Epsilon/blob/main/LICENSE.txt)

**Equations for [Epsilon](https://www.nuget.org/packages/Epsilon).** Exact solutions of equations
in one unknown: polynomials and rational equations, in radicals up to the fourth degree; roots of
the unknown; exponentials, logarithms, trigonometric and hyperbolic functions undone by their
inverses, with every branch, and periodic ones as families of solutions; polynomials in one
function of the unknown. The other variables are parameters, and each solution comes with the
conditions on them it needs. Numbers where there is no formula: every root of a polynomial, and
the solutions in a range of the real line or a rectangle of the complex plane.

```csharp
Equation.Parse("x^2 - 5x + 6 = 0").Solve().Print();              // {2, 3}
Equation.Parse("sin(x) = 1/2").Solve().Print();                  // {π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z}
Equation.Parse("a*x = b").Solve("x").Print();                    // {b / a} if a != 0
Equation.Parse("x^5 - x - 1 = 0").SolveNumerically().Print();    // {1.1673039782614187}
```

## Installation

```bash
dotnet add package Epsilon.Solving
```

Requires .NET 10. The core package `Epsilon` comes with it; the namespaces are `Epsilon.Solving`
and `Epsilon.Core`.

## Quick start

```csharp
using Epsilon.Core;
using Epsilon.Solving;

// Exact solutions
Equation.Parse("x^3 - 6x^2 + 11x - 6 = 0").Solve().Print();   // {1, 2, 3}
Equation.Parse("sqrt(x + 1) = x - 1").Solve().Print();         // {3}
Equation.Parse("4^x - 3*2^x + 2 = 0").Solve().Print();         // {0, 1}

// Parameters, and the conditions on them
Equation.Parse("E = m*v^2/2").Solve("v").Print();
// {-sqrt(2E / m), sqrt(2E / m)} if m != 0 and E / m >= 0

// Families of solutions, and their values in a range
SolutionSet sines = Equation.Parse("sin(x) = 1/2").Solve();
sines.Within(0, 10).Print();   // {π / 6, 5 * π / 6, 13 * π / 6, 17 * π / 6}

// Numbers where there is no formula
Equation.Parse("x = cos(x)").SolveNumerically(-10, 10).Print();   // {0.7390851332151607}
```

## Features

### Equations and solution sets

`Equation.Parse` reads two expressions with `=` between them, and `new Equation(left, right)`
takes two expressions. `Solve()` solves for the only variable, `Solve("x")` for the one named,
over the real numbers. The `SolutionSet` it gives lists each solution once, numbers in ascending
order. What wasn't solved is in `Unsolved`, and printed as `...`:

```csharp
SolutionSet set = Equation.Parse("x = cos(x)").Solve();
set.IsComplete;             // false
set.Unsolved[0].Print();    // x - cos(x)
set.Print();                // {...}
```

Nothing the solver does enlarges the domain, so x/x = 1 holds everywhere but at 0, and a
denominator's root is no solution:

```csharp
Equation.Parse("x/x = 1").Solve().Print();                 // (-inf, inf) if x != 0
Equation.Parse("(x^2 - 1)/(x - 1) = 0").Solve().Print();   // {-1}
```

### Polynomials

Linear and quadratic equations, cubics in radicals, and quartics and polynomials of higher
degree where they factor into those. Over the complex numbers with `SolveComplex`:

```csharp
Equation.Parse("x^3 - 3x + 1 = 0").Solve().Print();     // {2cos(10 * π / 9), 2cos(4 * π / 9), 2cos(2 * π / 9)}
Equation.Parse("x^3 = 2").Solve().Print();              // {nthroot(2, 3)}
Equation.Parse("x^5 - x - 1 = 0").Solve().Print();      // {...}
Equation.Parse("x^2 + 1 = 0").SolveComplex().Print();   // {-i, i}
```

### Functions and families of solutions

A function of the unknown is undone by its inverse, branch by branch, each with what it needs:
exp(u) = c has a solution only if c > 0. A periodic function has one solution a period, and a
family of them for every integer k:

```csharp
Equation.Parse("3ln(x - 1) + 2 = 5").Solve().Print();   // {1 + e}
Equation.Parse("abs(x - 1) = 2").Solve().Print();       // {-1, 3}
Equation.Parse("cos(x) = 2").Solve().Print();           // {}
Equation.Parse("sin(x)/x = 0").Solve().Print();         // {π * k | k in Z, k != 0}

Solution family = sines.Solutions[0];
family.Period.Print();   // 2 * π
family.At(1).Print();    // 13 * π / 6
```

### Roots of the unknown and substitutions

Roots are taken out by raising to powers, which keeps out the solutions that adds. An equation
that is a polynomial in one function of the unknown is solved for that first:

```csharp
Equation.Parse("sqrt(x) + sqrt(x + 1) = 3").Solve().Print();    // {16/9}
Equation.Parse("sin(x)^2 - 3*sin(x) + 2 = 0").Solve().Print();  // {π / 2 + 2 * π * k | k in Z}
Equation.Parse("exp(x) + exp(-x) = 4").Solve().Print();         // {-ln(sqrt(3) + 2), -ln(2 - sqrt(3))}
Equation.Parse("x^(2/3) + x^(1/3) = 2").Solve().Print();        // {1}
```

### Parameters

The other variables are parameters. A solution that needs something of them says so, and
assumptions about them leave out what they decide:

```csharp
Equation.Parse("a*x^2 + b*x + c = 0").Solve("x").Print();
// {(-b - sqrt(b^2 - 4a * c)) / (2a), (-b + sqrt(b^2 - 4a * c)) / (2a)} if a != 0 and b^2 - 4a * c >= 0
Equation.Parse("T = 2*pi*sqrt(L/g)").Solve("g").Print();   // {4π^2 * L / T^2} if T > 0 and L != 0
Equation.Parse("x^2 = a").Solve("x", Assumptions.None.AssumePositive("a")).Print();   // {-sqrt(a), sqrt(a)}
```

### Numeric solutions

`SolveNumerically` gives the solutions as numbers: the exact ones evaluated, the roots of
polynomials without a formula, and, in a range, the values of each family there and the
solutions of the rest, found by a scan. `FindRoot` looks near a guess, and
`SolveComplexNumerically` and `FindComplexRoot` do the same over the complex numbers:

```csharp
Equation.Parse("exp(x) = x + 2").SolveNumerically(double.NegativeInfinity, double.PositiveInfinity).Print();
// {-1.8414056604369606, 1.1461932206205825}
Equation.Parse("x = cos(x)").FindRoot(1);   // 0.7390851332151607
Equation.Parse("x^3 - x - 1 = 0").SolveComplexNumerically().Print();
// {-0.662358978622373 - 0.5622795120623012i, -0.662358978622373 + 0.5622795120623012i, 1.324717957244746}
```

## Good to know

- Names longer than one letter must be declared when parsing, all the names then:
  `Equation.Parse("omega*t = phi", "omega", "t", "phi")`. Otherwise `omega` is a product of
  letters, with `e` read as Euler's number and `i` as the imaginary unit. Names are
  [letters only](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Core/docs/parsing.md),
  so `v0` is v times 0.
- The solutions with parameters are those of the general case: a x = b is solved for a != 0,
  and what happens at a = 0 isn't worked out.
- `Solve` never gives a number that isn't exact. A polynomial without a formula, as x^5 - x - 1,
  stays unsolved there, and `SolveNumerically` gives its roots.
- Without a range, `SolveNumerically` doesn't scan: an equation like x = cos(x) needs a range,
  infinite if you like, or a guess for `FindRoot`.

## Documentation

The [guides](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Solving/docs/README.md)
explain every feature in detail, the
[API reference](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Solving/docs/api-reference.md)
lists every public type and member, the
[known limitations](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Solving/docs/limitations.md)
say what isn't done yet, and the
[changelog](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Solving/CHANGELOG.md)
lists the changes. The expressions are documented with the
[core](https://github.com/XBambinoX/Epsilon/blob/main/Epsilon.Core/README.md).

## License

MIT, copyright (c) 2026 Max Zakharov - see
[LICENSE](https://github.com/XBambinoX/Epsilon/blob/main/LICENSE.txt).
