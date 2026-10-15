# Numeric integration

`Integrate(a, b)` computes a definite integral numerically: to about 10 digits by default,
usually to all 16, or it throws. It doesn't return a number for an integral that diverges.

## Definite integrals

```csharp
ExprParser.Parse("x^2").Integrate(0, 1);                          // 0.33333333333333326
ExprParser.Parse("exp(-x^2/2)/sqrt(2*pi)").Integrate(-1, 1);      // 0.6826894921370859
ExprParser.Parse("x^2").Integrate(1, 0);                          // -0.33333333333333326
```

For b < a the integral is the negative of the one from b to a. With the variable named, the
expression may have other names than x:

```csharp
ExprParser.Parse("exp(-t^2)").Integrate("t", double.NegativeInfinity, double.PositiveInfinity);   // 1.7724538509055159
```

The expression must not have other variables, which have no values to compute with; substitute
them first:

```csharp
ExprParser.Parse("a*x").Integrate("x", 0, 1);
// ArgumentException: The expression has the variable 'a' besides 'x': substitute a value for it first.
ExprParser.Parse("a*x").Substitute("a", 3).Integrate("x", 0, 1);   // 1.4999999999999998
```

## Infinite limits

```csharp
ExprParser.Parse("exp(-x)").Integrate(0, double.PositiveInfinity);                            // 0.9999999999999994
ExprParser.Parse("1/(1 + x^2)").Integrate(double.NegativeInfinity, double.PositiveInfinity);   // 3.1415926535897927
```

## Singularities

The integrand may be infinite at the limits or inside the interval, as long as the integral
converges:

```csharp
ExprParser.Parse("ln(x)/sqrt(x)").Integrate(0, 1);         // -4
ExprParser.Parse("x^(-0.9)").Integrate(0, 1);              // 10
ExprParser.Parse("(1 - x)^(-1/2)").Integrate(0, 1);        // 2
ExprParser.Parse("1/sqrt(1 - x^2)").Integrate(-1, 1);      // 3.141592653589763

ExprParser.Parse("abs(x)^(-2/3)").Integrate(-1, 8);        // 8.999999999999998
ExprParser.Parse("1/sqrt(abs(x))").Integrate(-1, 4);       // 6
ExprParser.Parse("abs(x - 0.3)^(-1/2)").Integrate(0, 1);   // 2.768765168078484
ExprParser.Parse("sin(x)/x").Integrate(-1, 1);             // 1.8921661407343664
```

The points inside are found in the expression. The interval is split at the zeros of
denominators, of the bases of fractional and negative powers and of the arguments of roots,
logarithms and abs, at the poles of tan, cot, sec and csc, and at the edges of the domains of
the inverse functions. These zeros are exact where the part that vanishes is linear, and are
found numerically otherwise.

## Kinks, jumps and peaks

The interval is also split at kinks and jumps (abs, sign, min and max, and floor, ceiling and
round of linear arguments), and at the peaks of exponentials, so a normal density far from 0 is
found:

```csharp
ExprParser.Parse("abs(x)").Integrate(-1, 2);                    // 2.499999999999999
ExprParser.Parse("min(x, 1 - x)").Integrate(0, 1);             // 0.24999999999999992
ExprParser.Parse("floor(x)").Integrate(0, 3.5);                 // 4.500000000000001
ExprParser.Parse("exp(-(x - 200)^2)").Integrate(double.NegativeInfinity, double.PositiveInfinity);   // 1.772453850905513
```

A piece that doesn't converge, around a sharp peak, is split in two until the peak is next to
the end of a piece. The exact value of this one is 3139.59265425646:

```csharp
ExprParser.Parse("1/(x^2 + 1e-6)").Integrate(-1, 1);   // 3139.5926542564584
```

## Divergent integrals

An integral that diverges throws, at any tolerance, where a sum cut off at some point would
give a finite number:

```csharp
ExprParser.Parse("1/x").Integrate(0, 1);
// InvalidOperationException: The integral diverges at x = 0, or converges there too slowly for double precision.
ExprParser.Parse("1/x").Integrate(1, double.PositiveInfinity);
// InvalidOperationException: The integral diverges at infinity, or converges there too slowly for double precision.
ExprParser.Parse("tan(x)").Integrate(0, 2);
// InvalidOperationException: The integral diverges at x = 1.5707963267948966, or converges there too slowly for double precision.
```

Two kinds of convergent integrals get the same message. x^(-0.999) on [0, 1] converges to 1000,
but most of that lies closer to 0 than the smallest double. sin(x)/x on [0, infinity)
converges only because its waves cancel, and its terms never die out:

```csharp
ExprParser.Parse("x^(-0.999)").Integrate(0, 1);                         // InvalidOperationException
ExprParser.Parse("sin(x)/x").Integrate(0, double.PositiveInfinity);     // InvalidOperationException
```

## The tolerance

The last argument is the accuracy asked for, relative to the integral of the absolute value of
the integrand; by default 1e-10. Next to a singularity at a point that isn't a double, such as
pi, x can't come closer to it than its rounding, which limits the accuracy to about 1e-8. Asked
for more, `Integrate` says so; asked for less, it gives the result:

```csharp
ExprParser.Parse("1/sqrt(sin(x))").Integrate(0, Math.PI);
// InvalidOperationException: The integrand is singular next to x = 3.141592653589793, at a point that isn't a double, which limits the accuracy to about 1.1E-07.
ExprParser.Parse("1/sqrt(sin(x))").Integrate(0, Math.PI, 1e-6);   // 5.2441150944578965
```

The exact value is 5.244115108584239.

## Other errors

| Call | Throws |
|---|---|
| `ExprParser.Parse("sqrt(x)").Integrate(-1, 1)` | `InvalidOperationException`: The integrand is not a real number at x = -0.5. |
| `ExprParser.Parse("sin(1/x)").Integrate(0, 1)` | `InvalidOperationException`: The integral over [0, 9.313225746154785E-10] didn't converge: ... |
| `ExprParser.Parse("x*y").Integrate(0, 1)` | `InvalidOperationException`: the expression has two variables: name the one to integrate by |
| `ExprParser.Parse("x^2").Integrate(0, 1, 0)` | `ArgumentOutOfRangeException`: The tolerance must be between 0 and 1. |

## How it works

The interval is split at the points above, and each piece is integrated by the tanh-sinh rule,
or by exp-sinh towards an infinity. Their nodes crowd towards the ends of a piece double
exponentially, so a singularity there costs only a few more of them. The nodes are measured by
their distance from an end, which is exact down to the smallest double. Where the integrand is
singular at an end, it is shifted there symbolically: next to 1, (1 - x)^(-1/2) becomes d^(-1/2)
of the distance d, where 1 - x would be 0 for every d below 1e-16. The step is halved until two
estimates agree. Before that, the last term towards each end must be negligible, since a
divergent integral has terms that don't die out.
