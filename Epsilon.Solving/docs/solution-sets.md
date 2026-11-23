# Equations and solution sets

## Writing an equation

`Equation.Parse` reads two expressions in the syntax of the core's
[parser](../../Epsilon.Core/docs/parsing.md), with `=` between them. Names longer than one letter
are declared as for the parser, all of them then. `new Equation(left, right)` takes two
expressions:

```csharp
Equation sine = Equation.Parse("sin(x) = 1/2");
Equation phase = Equation.Parse("omega*t = phi", "omega", "t", "phi");
Equation square = new Equation(ExprParser.Parse("x^2"), 4);

sine.Print();     // sin(x) = 1 / 2
sine.ToLatex();   // \sin\left(x\right) = \frac{1}{2}
```

`Left` and `Right` are the two sides. An input without `=`, or with two, throws
`FormatException`, with positions counted from the start of the whole input.

## Solving

`Solve()` solves for the only variable of the equation (x for one without any), and throws
`InvalidOperationException` for more than one. `Solve("x")` solves for the one named, and the
others are [parameters](parameters.md). Both solve over the real numbers;
[`SolveComplex`](polynomials.md#over-the-complex-numbers) solves over the complex ones:

```csharp
square.Solve().Print();                    // {-2, 2}
phase.Solve("t").Print();                  // {phi / omega} if omega != 0
```

## The solutions

A `SolutionSet` lists in `Solutions` each solution once, numbers in ascending order. Each
`Solution` has a `Value`, an exact `Expr`, and the `Conditions` it holds under, empty if it
always does:

```csharp
SolutionSet set = Equation.Parse("x^3 - x^2 - x + 1 = 0").Solve();

set.Print();                         // {-1, 1}
set.Solutions.Count;                 // 2
set.Solutions[1].Value.Print();      // 1
```

A solution that repeats, as 1 here, is listed once. Solutions that are families, one value for
every integer, have a [period and an index](functions.md#families-of-solutions).

## What isn't solved

A part of the equation the solver can't solve is listed in `Unsolved`, as an expression that
must be 0, and `IsComplete` is false. The set prints it as `...`, after the solutions it did
find:

```csharp
SolutionSet partly = Equation.Parse("x*(x - cos(x)) = 0").Solve();

partly.Print();                    // {0, ...}
partly.IsComplete;                 // false
partly.Unsolved[0].Print();        // x - cos(x)
```

The [numeric solutions](numeric-solutions.md) of the same equation find what is left.

## No solution, or every value

`IsEmpty` says the equation has no solution, which it knows: the set is complete and has
none. An equation that holds for every value of the unknown has `IsEveryValue`, no
`Solutions`, and in `Conditions` where it holds:

```csharp
Equation.Parse("cos(x) = 2").Solve().IsEmpty;   // true
Equation.Parse("cos(x) = 2").Solve().Print();   // {}

SolutionSet always = Equation.Parse("x + 1 = 1 + x").Solve();
always.IsEveryValue;    // true
always.Print();         // (-inf, inf)

Equation.Parse("a = b").Solve("x").Print();    // (-inf, inf) if a = b
```

## The domain

The solver never enlarges the domain: no step makes the equation defined where it wasn't. A
value where a side is undefined is no solution, and x/x = 1 holds for every x but 0:

```csharp
Equation.Parse("x/x = 1").Solve().Print();                  // (-inf, inf) if x != 0
Equation.Parse("(x^2 - 1)/(x - 1) = 0").Solve().Print();    // {-1}
Equation.Parse("1/x = 0").Solve().Print();                  // {}
Equation.Parse("ln(x) = 2").Solve().Print();                // {exp(2)}
```

## Assumptions about the unknown

`Solve` takes the core's [assumptions](../../Epsilon.Core/docs/assumptions.md). One about the sign
of the unknown leaves out the solutions that don't meet it:

```csharp
var positive = Assumptions.None.AssumePositive("x");

Equation.Parse("x^2 = 4").Solve("x", positive).Print();          // {2}
Equation.Parse("x^2 - 2x - 1 = 0").Solve("x", positive).Print(); // {sqrt(2) + 1}
```

Assumptions about [parameters](parameters.md#assumptions) decide their conditions.

## Text and LaTeX

`Print` writes the set as text and `ToLatex` in LaTeX, with the conditions after `if`:

```csharp
Equation.Parse("x^2 - 2x - 1 = 0").Solve().ToLatex();   // \left\{1 - \sqrt{2}, \sqrt{2} + 1\right\}
Equation.Parse("cos(x) = 2").Solve().ToLatex();         // \emptyset
```
