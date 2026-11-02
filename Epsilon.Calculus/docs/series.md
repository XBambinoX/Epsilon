# Series

`Series(variable, point, degree)` expands an expression at a point a into powers of x - a, up to
(x - a)^degree, with exact coefficients, and says how small the rest is: O((x - a)^n). Where the
expression has a pole at a, the powers start below 0, as in a Laurent series; where it has a root
of something that vanishes there, they are fractions, as in a Puiseux series.

**Contents:**
[Taylor series](#taylor-series) -
[The terms](#the-terms) -
[Poles: Laurent series](#poles-laurent-series) -
[Roots: Puiseux series](#roots-puiseux-series) -
[Symbols](#symbols) -
[What has no series](#what-has-no-series) -
[How it works](#how-it-works)

## Taylor series

The terms go up to the power given, and the remainder is of the next one:

```csharp
ExprParser.Parse("sin(x)/x").Series("x", 0, 4).Print();   // 1 - (1/6) * x^2 + (1/120) * x^4 + O(x^5)
ExprParser.Parse("exp(x)").Series("x", 0, 4).Print();     // 1 + x + (1/2) * x^2 + (1/6) * x^3 + (1/24) * x^4 + O(x^5)
ExprParser.Parse("tan(x)").Series("x", 0, 5).Print();     // x + (1/3) * x^3 + (2/15) * x^5 + O(x^6)
```

sin(x)/x is 0/0 at 0, but its series is that of sin(x) divided by x term by term. At any other
point the powers are of x - a:

```csharp
ExprParser.Parse("ln(x)").Series("x", 1, 3).Print();
// (x - 1) - (1/2) * (x - 1)^2 + (1/3) * (x - 1)^3 + O((x - 1)^4)
ExprParser.Parse("sin(x)").Series("x", ExprParser.Parse("pi/2"), 2).Print();
// 1 - (1/2) * (x - (1/2) * π)^2 + O((x - (1/2) * π)^3)
```

The point is an `Expr`. A `double` is taken as the decimal it prints as, so `Math.PI / 2` is
15707963267949/10000000000000, not pi/2: give pi as `ExprParser.Parse("pi/2")` or `new Pi() / 2`.
Without a variable name, the expression's only variable is taken:

```csharp
ExprParser.Parse("sin(t)").Series(0, 3).Print();   // t - (1/6) * t^3 + O(t^4)
```

## The terms

`Terms` lists the exponents and coefficients other than 0 by increasing exponent, and
`Coefficient` gives any one below `Order`, the exponent of the remainder. `ToExpr` is the sum of
the terms, a polynomial for a Taylor series, which approximates the expression next to the point:

```csharp
var series = ExprParser.Parse("cos(x)").Series("x", 0, 4);

series.Coefficient(2).Print();               // -1/2
series.Coefficient(3).Print();               // 0
series.Order;                                // 5
series.Terms.Count;                          // 3
series.ToExpr().Print();                     // (1/24) * x^4 + (-1/2) * x^2 + 1
series.ToExpr().Evaluate(("x", 0.1));        // 0.9950041666666667
Math.Cos(0.1);                               // 0.9950041652780258
```

`Coefficient(5)` throws `ArgumentOutOfRangeException`: the series doesn't know it.

## Poles: Laurent series

Quotients are divided from their lowest terms, so a pole gives negative powers:

```csharp
ExprParser.Parse("1/sin(x)").Series("x", 0, 3).Print();         // x^-1 + (1/6) * x + (7/360) * x^3 + O(x^4)
ExprParser.Parse("1/(exp(x) - 1)").Series("x", 0, 3).Print();   // x^-1 - 1/2 + (1/12) * x - (1/720) * x^3 + O(x^4)
ExprParser.Parse("tan(x)").Series("x", ExprParser.Parse("pi/2"), 1).Print();
// -(x - (1/2) * π)^-1 + (1/3) * (x - (1/2) * π) + O((x - (1/2) * π)^2)
```

## Roots: Puiseux series

A square root, or any power that isn't an integer, of something that vanishes at the point
gives powers that are fractions, and the order is the next one that can come:

```csharp
var root = ExprParser.Parse("sqrt(x + x^2)").Series("x", 0, 2);

root.Print();        // x^(1/2) + (1/2) * x^(3/2) + O(x^(5/2))
root.Order;          // 5/2
root.RightSided;     // True
```

Such a series holds for x > a only, which `RightSided` says. A fractional power of x - a isn't
real for x < a, and where the powers come out integers, the series may still be that of the
right side: sqrt(x^2) is |x|, whose series is x there. abs and sign of something that vanishes at
the point are taken on the right side too, but for an even power, and the real root `nthroot` of
an odd degree holds on both sides:

```csharp
ExprParser.Parse("sqrt(x^2)").Series("x", 0, 2).Print();               // x + O(x^3)
ExprParser.Parse("sqrt(x^2)").Series("x", 0, 2).RightSided;            // True
ExprParser.Parse("nthroot(x^3 + x^4, 3)").Series("x", 0, 2).Print();   // x + (1/3) * x^2 + O(x^3)
ExprParser.Parse("nthroot(x^3 + x^4, 3)").Series("x", 0, 2).RightSided;   // False
```

At a branch point of an inverse function, where its derivative has a root, the series is that
of the side it is real on, if that is the right one:

```csharp
ExprParser.Parse("acosh(x)").Series("x", 1, 2).Print();
// sqrt(2) * (x - 1)^(1/2) - (sqrt(2) / 12) * (x - 1)^(3/2) + O((x - 1)^(5/2))
```

## Symbols

Other variables are constants, in the expression and in the point:

```csharp
ExprParser.Parse("exp(a*x)").Series("x", 0, 2).Print();    // 1 + a * x + (1/2) * a^2 * x^2 + O(x^3)
ExprParser.Parse("1/(x - a)").Series("x", 0, 2).Print();   // -1 / a - (1 / a^2) * x - (1 / a^3) * x^2 + O(x^3)
ExprParser.Parse("sin(x)").Series("x", ExprParser.Parse("a"), 2).Print();
// sin(a) + cos(a) * (x - a) - (sin(a) / 2) * (x - a)^2 + O((x - a)^3)
```

A coefficient with a symbol is taken as other than 0, so `1/(x - a)` at 0 holds for a other than
0. Where a sign is needed and depends on a symbol, as for abs(a x) at 0, it throws.

## What has no series

Where the expression has no series of this kind at the point, `Series` throws
`NotSupportedException` with the part that has none:

```csharp
ExprParser.Parse("ln(x)").Series("x", 0, 3);
// NotSupportedException: ln(x) has a logarithm of x in its series at x = 0.
```

| Expression | At | Why |
|---|---|---|
| `ln(x)`, `x*ln(x)` | 0 | ln(x) is in the expansion, which isn't a power of x |
| `exp(1/x)`, `sin(1/x)` | 0 | an essential singularity: infinitely many negative powers |
| `asin(x)` | 1 | not real for x > 1, the side a series at a branch point is taken on |
| `sqrt(x - 2)` | 0 | not real there |
| `floor(x)` | 1 | a jump |

Their [limits](limits.md) are found all the same: x ln(x) at 0 is 0, and exp(1/x) at 0 is 0
from the left and inf from the right.

A point given as a `double` that isn't the exact point, as `Math.PI / 2` for tan(x), gives the
series at that decimal, with coefficients such as tan(15707963267949/10000000000000).

## How it works

The series is built up from those of the parts of the expression, at a precision each part
needs: sums term by term, products from the lowest terms of the factors, so that each factor is
expanded only as far as the product needs, and quotients the same way, with the inverse of the
denominator as a geometric series. A power c^k t^(v k) (1 + u)^k of b = c t^v (1 + u) is the
binomial series, and tan, sec and their kin are quotients of sin and cos, so poles come out of
the division. An inverse function f of s is f(s0) plus the integral of f'(s) s', with s0 the
constant term of s, since its derivative is algebraic. Any other function f of an argument
s0 + h is f(s0) + f'(s0) h + f''(s0) h^2/2 + ..., with the derivatives taken symbolically. A sum whose terms
cancel is expanded further until a term other than 0 is found, as many as 32 powers past its
parts; one that cancels further, as sin(x)^2 + cos(x)^2 - 1 does, is taken as 0.
