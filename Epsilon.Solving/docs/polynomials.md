# Polynomials and rational equations

## Linear and quadratic equations

A quadratic is solved by its discriminant, with the roots written as the centre and the distance
to each, so that numbers cancel:

```csharp
Equation.Parse("2(x - 1) = 3(x + 1)").Solve().Print();   // {-5}
Equation.Parse("x^2 - 5x + 6 = 0").Solve().Print();      // {2, 3}
Equation.Parse("x^2 - 2x - 1 = 0").Solve().Print();      // {1 - sqrt(2), sqrt(2) + 1}
Equation.Parse("x^2 + 1 = 0").Solve().Print();           // {}
```

## Higher degrees

A polynomial with rational coefficients is factored first, into its rational roots and the
quadratics it splits into. Cubics are solved in radicals, by Cardano's formula, or with cosines
where all three roots are real. A quartic is solved where its resolvent cubic has a rational
root, which gives two quadratics:

```csharp
Equation.Parse("x^3 - 6x^2 + 11x - 6 = 0").Solve().Print();   // {1, 2, 3}
Equation.Parse("x^4 - 5x^2 + 4 = 0").Solve().Print();         // {-2, -1, 1, 2}
Equation.Parse("x^3 = 2").Solve().Print();                    // {nthroot(2, 3)}
Equation.Parse("x^3 + x + 1 = 0").Solve().Print();
// {nthroot(sqrt(93) / 18 - 1/2, 3) - nthroot(sqrt(93) / 18 + 1/2, 3)}
Equation.Parse("x^3 - 3x + 1 = 0").Solve().Print();
// {2cos(10 * π / 9), 2cos(4 * π / 9), 2cos(2 * π / 9)}
```

What can't be written in radicals stays [unsolved](solution-sets.md#what-isnt-solved), and
[`SolveNumerically`](numeric-solutions.md) gives its roots as numbers:

```csharp
Equation.Parse("x^5 - x - 1 = 0").Solve().Print();   // {...}
Equation.Parse("x^5 = 2").Solve().Print();           // {nthroot(2, 5)}
```

x^5 = 2 is no exception: a power of the unknown alone is [undone](functions.md) by a root.

## Repeated roots

A root is listed once, however often it repeats:

```csharp
Equation.Parse("(x - 1)^3 = 0").Solve().Print();         // {1}
Equation.Parse("x^4 - 4x^2 + 4 = 0").Solve().Print();    // {-sqrt(2), sqrt(2)}
```

## Rational equations

Fractions are brought over a common denominator, and the numerator solved. A root of a
denominator is no solution:

```csharp
Equation.Parse("1/(x - 1) + 1/(x + 1) = 1").Solve().Print();   // {1 - sqrt(2), sqrt(2) + 1}
Equation.Parse("x + 1/x = 2").Solve().Print();                 // {1}
Equation.Parse("(x^2 - 1)/(x - 1) = 0").Solve().Print();       // {-1}
```

## Over the complex numbers

`SolveComplex` solves over the complex numbers, where a polynomial of degree n has n roots,
counted with their multiplicity. Polynomial and rational equations are solved; other functions of
the unknown are left unsolved:

```csharp
Equation.Parse("x^2 + 1 = 0").SolveComplex().Print();       // {-i, i}
Equation.Parse("x^2 = 2x - 5").SolveComplex().Print();      // {1 - 2 * i, 1 + 2 * i}
Equation.Parse("x^4 + 4 = 0").SolveComplex().Print();       // {-i - 1, i - 1, 1 - i, 1 + i}
Equation.Parse("x^3 + 1 = 0").SolveComplex().Print();       // {-1, 1/2 - i * sqrt(3) / 2, i * sqrt(3) / 2 + 1/2}
```

The solutions are ordered by the real part, then the imaginary part.
