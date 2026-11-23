# Getting started

## Install

```bash
dotnet add package Epsilon.Solving
```

It requires .NET 10, and brings the core with it. Two namespaces cover everything:

```csharp
using Epsilon.Core;      // Expr, ExprParser, Assumptions
using Epsilon.Solving;   // Equation, SolutionSet, Solution
```

## A first equation

An equation is two expressions with `=` between them. `Solve` gives its solutions, exact:

```csharp
Equation equation = Equation.Parse("x^2 - 2x - 1 = 0");
SolutionSet solutions = equation.Solve();

solutions.Print();                          // {1 - sqrt(2), sqrt(2) + 1}
solutions.Solutions[0].Value.Evaluate();    // -0.41421356237309515
```

Each solution is an `Expr`, which prints, evaluates and simplifies like any other. A periodic
function has a solution a period, so its solutions are families, one value for every integer k:

```csharp
Equation.Parse("2sin(x) + 1 = 0").Solve().Print();
// {-5 * π / 6 + 2 * π * k, -π / 6 + 2 * π * k | k in Z}
```

## Other variables

With more than one variable, name the unknown. The others are parameters, and a solution that
needs something of them says what:

```csharp
Equation.Parse("x^2 = a").Solve("x").Print();   // {-sqrt(a), sqrt(a)} if a >= 0
```

## Numbers

Where there is no formula, `SolveNumerically` gives numbers. An equation like x = cos(x) needs
a range to look in:

```csharp
Equation.Parse("x^5 - x - 1 = 0").Solve().Print();                // {...}
Equation.Parse("x^5 - x - 1 = 0").SolveNumerically().Print();     // {1.1673039782614187}
Equation.Parse("x = cos(x)").SolveNumerically(-10, 10).Print();   // {0.7390851332151607}
```

## Where next

- [Equations and solution sets](solution-sets.md) for everything a solution set tells you.
- [Polynomials](polynomials.md), [functions](functions.md) and
  [roots and substitutions](roots-and-substitutions.md) for the kinds of equations solved
  exactly.
- [Parameters](parameters.md) for formulas solved for one of their variables.
- [Numeric solutions](numeric-solutions.md) for numbers, in a range and in the complex plane.
