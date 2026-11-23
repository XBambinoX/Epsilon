# Roots of the unknown and substitutions

## Raising to powers

An equation with roots of the unknown in it is raised to powers, a root at a time, until a
polynomial is left. That can add solutions: sqrt(x + 1) = x - 1 squared is
x + 1 = (x - 1)^2, solved by 0 too, where the sides are 1 and -1. A square root is never
negative, which keeps them out:

```csharp
Equation.Parse("sqrt(x + 1) = x - 1").Solve().Print();          // {3}
Equation.Parse("sqrt(x) + sqrt(x + 1) = 3").Solve().Print();    // {16/9}
Equation.Parse("sqrt(x) + sqrt(x + 1) = 0").Solve().Print();    // {}
Equation.Parse("nthroot(x, 3) = x").Solve().Print();            // {-1, 0, 1}
Equation.Parse("sqrt(x + sqrt(x)) = 2").Solve().Print();        // {9/2 - sqrt(17) / 2}
```

## Substitutions

An equation that is a polynomial in one function of the unknown is solved for that function
first, as t^2 - 3t + 2 = 0 for t = sin(x), at 1 and 2, and then sin(x) = 1 and sin(x) = 2,
which has no solution:

```csharp
Equation.Parse("sin(x)^2 - 3*sin(x) + 2 = 0").Solve().Print();   // {π / 2 + 2 * π * k | k in Z}
Equation.Parse("2*cos(x)^2 - cos(x) - 1 = 0").Solve().Print();
// {-2 * π / 3 + 2 * π * k, 2 * π * k, 2 * π / 3 + 2 * π * k | k in Z}
Equation.Parse("sin(x)^3 = sin(x)").Solve().Print();             // {π * k / 2 | k in Z}
Equation.Parse("ln(x)^2 = ln(x) + 2").Solve().Print();           // {exp(-1), exp(2)}
```

The families of the values merge where they can: sin(x)^3 = sin(x) is sin(x) = 0, 1 or -1, every
multiple of π / 2.

## Several powers of one function

Exponentials of one base are powers of one of them: in t = 2^x, 4^x is t^2 and 2^(x + 1) is 2t;
in t = exp(x), exp(2x) is t^2 and exp(-x) is 1 / t:

```csharp
Equation.Parse("exp(2*x) - 3*exp(x) + 2 = 0").Solve().Print();   // {0, ln(2)}
Equation.Parse("4^x - 3*2^x + 2 = 0").Solve().Print();           // {0, 1}
Equation.Parse("2^(x + 1) + 2^x = 12").Solve().Print();          // {2}
Equation.Parse("2^x + 2^(-x) = 5/2").Solve().Print();            // {-1, 1}
Equation.Parse("exp(x) + exp(-x) = 4").Solve().Print();          // {-ln(sqrt(3) + 2), -ln(2 - sqrt(3))}
```

Roots of one radicand are powers of one root, and the unknown itself, where the radicand is
linear in it: in t = sqrt(x), x is t^2; in t = nthroot(x, 3), x^(2/3) is t^2:

```csharp
Equation.Parse("x - 3*sqrt(x) + 2 = 0").Solve().Print();         // {1, 4}
Equation.Parse("sqrt(x + 1) + x = 5").Solve().Print();           // {3}
Equation.Parse("x^(2/3) - 3*x^(1/3) + 2 = 0").Solve().Print();   // {1, 8}
Equation.Parse("x^(2/3) + x^(1/3) = 2").Solve().Print();         // {1}
```

x^(2/3) is defined for x >= 0 only, as every power with an exponent that isn't a whole number,
so t = -2, which is x = -8, is no solution of the last one.

## What isn't substituted

The unknown outside the function, functions of different arguments, and exponentials of
different bases stay as they are. So does a polynomial in the function without a solution in
radicals, whose roots [`SolveNumerically`](numeric-solutions.md) finds:

```csharp
Equation.Parse("sin(x)^2 + sin(x) = x").Solve().Print();     // {...}
Equation.Parse("sin(x)^2 + cos(x) = 0").Solve().Print();     // {...}
Equation.Parse("exp(x) = 2^x").Solve().Print();              // {...}
Equation.Parse("exp(x)^5 - exp(x) - 1 = 0").SolveNumerically().Print();   // {0.15469679775892686}
```
