# Functions and families of solutions

## Undoing a function

A function of the unknown is undone by its inverse, outermost first: 3 ln(x - 1) + 2 = 5 is
ln(x - 1) = 1, then x - 1 = e. Each step needs something of the other side, as exp(u) = c a
c > 0, and an equation that can't meet it has no solution:

```csharp
Equation.Parse("exp(x) = 5").Solve().Print();            // {ln(5)}
Equation.Parse("exp(x) = -1").Solve().Print();           // {}
Equation.Parse("3ln(x - 1) + 2 = 5").Solve().Print();    // {1 + e}
Equation.Parse("10^x = 1000").Solve().Print();           // {3}
Equation.Parse("2^x = 5").Solve().Print();               // {ln(5) / ln(2)}
```

Roots, powers and absolute values give a branch for each sign where there is one:

```csharp
Equation.Parse("sqrt(x) = 3").Solve().Print();           // {9}
Equation.Parse("nthroot(x, 3) = -2").Solve().Print();    // {-8}
Equation.Parse("abs(x - 1) = 2").Solve().Print();        // {-1, 3}
Equation.Parse("(x - 1)^4 = 16").Solve().Print();        // {-1, 3}
```

The inverse trigonometric and the hyperbolic functions, and their inverses, are undone too:

```csharp
Equation.Parse("atan(x) = 1").Solve().Print();     // {tan(1)}
Equation.Parse("atan(x) = 2").Solve().Print();     // {}
Equation.Parse("cosh(x) = 2").Solve().Print();     // {-acosh(2), acosh(2)}
Equation.Parse("tanh(x) = 2").Solve().Print();     // {}
```

Where the unknown is on both sides of a function, as in x = cos(x), nothing is undone and the
equation stays [unsolved](solution-sets.md#what-isnt-solved). A polynomial in one function
of the unknown is [substituted](roots-and-substitutions.md#substitutions) instead.

## Families of solutions

sin, cos, tan, cot, sec and csc repeat with their period, so an equation in them has a solution
a period: a family, one value for every integer k. The index is named k, or n, m or j where the
equation has a k:

```csharp
Equation.Parse("sin(x) = 1/2").Solve().Print();
// {π / 6 + 2 * π * k, 5 * π / 6 + 2 * π * k | k in Z}
Equation.Parse("tan(x) + 1 = 0").Solve().Print();     // {-π / 4 + π * k | k in Z}
Equation.Parse("sin(2x) = 1").Solve().Print();       // {π / 4 + π * k | k in Z}
Equation.Parse("cos(x) = 2").Solve().Print();        // {}
```

Families half a period apart are one, with half the period:

```csharp
Equation.Parse("sin(x) = 0").Solve().Print();    // {π * k | k in Z}
```

A `Solution` of a family has its `Value` with the index in it, its `Period`, the distance
between its values, and its `Index`, the name of the integer:

```csharp
Solution family = Equation.Parse("sin(x) = 1/2").Solve().Solutions[1];

family.Value.Print();     // 2 * π * k + 5 * π / 6
family.Print();           // 5 * π / 6 + 2 * π * k
family.Period.Print();    // 2 * π
family.Index;             // k
```

`Period` and `Index` are null for a single value.

## Conditions on the index

Where only some values of a family are solutions, the conditions say which:

```csharp
Equation.Parse("sin(x)/x = 0").Solve().Print();   // {π * k | k in Z, k != 0}
```

## One value of a family

`At` gives the value of a family at one index, null where its conditions leave that index out:

```csharp
Solution multiples = Equation.Parse("sin(x)/x = 0").Solve().Solutions[0];

multiples.At(2).Print();   // 2 * π
multiples.At(0);           // null
family.At(-1).Print();     // -7 * π / 6
```

## The values in a range

`Within` gives the solutions in a range of the real line, with the values of each family there
listed one by one. More than ten thousand values stay a family, with bounds on its index:

```csharp
SolutionSet sines = Equation.Parse("sin(x) = 1/2").Solve();

sines.Within(0, 10).Print();                       // {π / 6, 5 * π / 6, 13 * π / 6, 17 * π / 6}
Equation.Parse("sin(x) = 0").Solve().Within(-4, 4).Print();      // {-π, 0, π}
Equation.Parse("sin(x) = 0").Solve().Within(0, 1e6).Print();     // {π * k | k in Z, k >= 0, k <= 318309}
```

A value too close to an end to tell from it counts as in the range, so 2 * π is in
[0, 2 * Math.PI], though the double is just below 2 * π:

```csharp
Equation.Parse("sin(x) = 0").Solve().Within(0, 2 * Math.PI).Print();   // {0, π, 2 * π}
```
