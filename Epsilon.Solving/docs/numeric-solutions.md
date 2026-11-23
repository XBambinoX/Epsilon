# Numeric solutions

The numeric solvers need an equation in one variable: any other throws
`InvalidOperationException`, so substitute numbers for the parameters first.

## Solutions as numbers

`SolveNumerically` gives the real solutions as numbers in a `NumericSolutionSet<double>`: those
[`Solve`](solution-sets.md) finds, evaluated, and every real root of a polynomial it can't solve
exactly. `Values` lists them in ascending order, to about full double precision:

```csharp
Equation.Parse("x^5 - x - 1 = 0").SolveNumerically().Print();   // {1.1673039782614187}

NumericSolutionSet<double> roots = Equation.Parse("x^2 = 2").SolveNumerically();
roots.Values[1];      // 1.4142135623730951
roots.IsComplete;     // true
```

Like a `SolutionSet` it has `IsComplete`, `Unsolved`, `IsEmpty`, `IsEveryValue` and
`Conditions`. Without a range a family of solutions is too many to list, and x = cos(x) has no
formula to evaluate; both stay unsolved:

```csharp
Equation.Parse("sin(x) = 1/2").SolveNumerically().Print();   // {...}
Equation.Parse("x = cos(x)").SolveNumerically().Print();     // {...}
```

## In a range

`SolveNumerically(from, to)` gives the solutions in [from, to]: those found exactly, the values of
each family there, and the solutions of the rest, found by a scan of the range. A limit may be
infinite:

```csharp
Equation.Parse("sin(x) = 1/2").SolveNumerically(0, 10).Print();
// {0.5235987755982989, 2.6179938779914944, 6.806784082777885, 8.901179185171081}
Equation.Parse("x = cos(x)").SolveNumerically(-10, 10).Print();    // {0.7390851332151607}
Equation.Parse("tan(x) = x").SolveNumerically(0, 10).Print();      // {0, 4.493409457909064, 7.725251836937707}
Equation.Parse("exp(x) = x + 2").SolveNumerically(double.NegativeInfinity, double.PositiveInfinity).Print();
// {-1.8414056604369606, 1.1461932206205825}
```

The scan steps through the range on a grid, finer where the equation oscillates through sines
and cosines, and refines each change of sign by Newton's method. Points where the equation is
undefined are never solutions: tan(x) changes sign at its poles, and they are left out. An
infinite limit is scanned through a change of variable, with steps that get longer far from 0.
Solutions closer together than a step can be missed; see the
[limitations](limitations.md#numeric-solutions).

## Near a guess

`FindRoot` starts Newton's method at a guess, and gives the solution it converges to, or null:

```csharp
Equation.Parse("x = cos(x)").FindRoot(1);       // 0.7390851332151607
Equation.Parse("x^2 + 1 = 0").FindRoot(1);      // null
```

## The complex plane

`SolveComplexNumerically` gives the complex solutions as `ComplexNumber`s: those
[`SolveComplex`](polynomials.md#over-the-complex-numbers) finds, evaluated, and every root of a
polynomial it can't solve exactly. With the corners of a rectangle, lower left and upper right,
it also looks for the solutions of the rest from every point of a grid; `FindComplexRoot` looks
near a guess:

```csharp
Equation.Parse("x^3 - x - 1 = 0").SolveComplexNumerically().Print();
// {-0.662358978622373 - 0.5622795120623012i, -0.662358978622373 + 0.5622795120623012i, 1.324717957244746}
Equation.Parse("exp(x) = 1").SolveComplexNumerically(new ComplexNumber(-1, -10), new ComplexNumber(1, 10)).Print();
// {-6.283185307179586i, 0, 6.283185307179586i}
Equation.Parse("x^2 + 1 = 0").FindComplexRoot(new ComplexNumber(1, 1));   // 1i
```

The search in a rectangle has no completeness guarantee: a solution is found only if some start
of the grid converges to it.
