# Limits

`Limit(variable, point)` finds the limit of an expression as the variable tends to a point,
exactly: a value such as 1/2, e or pi/4, or inf or -inf where the expression grows or falls
without bound. The point may be inf or -inf too, and the limit may be taken from both sides of
a point or from one. Limits that ln(x) and exp(1/x) make, which have no
[series](series.md), are found as well: x ln(x) at 0 is 0, and x^7/exp(x) at inf is 0.

**Contents:**
[Limits at a point](#limits-at-a-point) -
[At infinity](#at-infinity) -
[One side](#one-side) -
[Where there is no limit](#where-there-is-no-limit) -
[The result](#the-result) -
[Symbols](#symbols) -
[Oscillations](#oscillations) -
[What isn't found](#what-isnt-found) -
[How it works](#how-it-works)

## Limits at a point

From both sides, unless a side is given:

```csharp
ExprParser.Parse("sin(x)/x").Limit("x", 0).Print();            // 1
ExprParser.Parse("(1 - cos(x))/x^2").Limit("x", 0).Print();    // 1/2
ExprParser.Parse("(x^3 - 8)/(x - 2)").Limit("x", 2).Print();   // 12
ExprParser.Parse("x*ln(x)").Limit("x", 0).Print();             // 0
ExprParser.Parse("cos(x)^(1/x^2)").Limit("x", 0).Print();      // exp(-1/2)
ExprParser.Parse("1/(x - 2)^2").Limit("x", 2).Print();         // inf
```

The point is an `Expr`, or a `double`, which is taken as the decimal it prints as, as for
[series](series.md#taylor-series): `Math.PI` is 314159265358979/100000000000000, so give pi as
`ExprParser.Parse("pi")` or `new Pi()`. Without a variable name, the expression's only variable
is taken:

```csharp
ExprParser.Parse("sin(x)/(x - pi)").Limit("x", ExprParser.Parse("pi")).Print();   // -1
ExprParser.Parse("sin(t)/t").Limit(0).Print();                                    // 1
```

## At infinity

The point `double.PositiveInfinity` is inf, and `double.NegativeInfinity` is -inf, as for the
limits of [numeric integrals](numeric-integration.md#infinite-limits):

```csharp
var inf = double.PositiveInfinity;

ExprParser.Parse("(1 + 1/x)^x").Limit("x", inf).Print();          // e
ExprParser.Parse("sqrt(x^2 + x) - x").Limit("x", inf).Print();    // 1/2
ExprParser.Parse("x^7/exp(x)").Limit("x", inf).Print();           // 0
ExprParser.Parse("ln(x)/x").Limit("x", inf).Print();              // 0
ExprParser.Parse("(2^x + 3^x)^(1/x)").Limit("x", inf).Print();    // 3
ExprParser.Parse("atan(x)").Limit("x", double.NegativeInfinity).Print();    // (-1/2) * π
ExprParser.Parse("x*exp(x)").Limit("x", double.NegativeInfinity).Print();   // 0
```

Exponentials are compared by how fast they grow, also where they are nested and cancel to the
first order:

```csharp
ExprParser.Parse("exp(x)*(exp(1/x - exp(-x)) - exp(1/x))").Limit("x", inf).Print();   // -1
```

## One side

`LimitDirection.Left` takes x < a, and `LimitDirection.Right` x > a:

```csharp
var f = ExprParser.Parse("1/x");

f.Limit("x", 0, LimitDirection.Right).Print();                                    // inf
f.Limit("x", 0, LimitDirection.Left).Print();                                     // -inf
ExprParser.Parse("exp(-1/x)").Limit("x", 0, LimitDirection.Right).Print();        // 0
ExprParser.Parse("floor(x)").Limit("x", 1, LimitDirection.Left).Print();          // 0
ExprParser.Parse("tan(x)").Limit("x", ExprParser.Parse("pi/2"), LimitDirection.Left).Print();   // inf
```

At inf the only side is the left one: `LimitDirection.Right` there, and `LimitDirection.Left`
at -inf, throw `ArgumentException`.

## Where there is no limit

Where the two sides differ, the limit from both doesn't exist, and `Limit` throws
`InvalidOperationException` with the two:

```csharp
ExprParser.Parse("1/x").Limit("x", 0);
// InvalidOperationException: The limit of 1 / x at x = 0 doesn't exist: -inf from the left, inf from the right.
```

At an end of where the expression is real, the limit from both sides is that from the side it
is real on, and from the other side there is none:

```csharp
ExprParser.Parse("sqrt(x)").Limit("x", 0).Print();   // 0
ExprParser.Parse("ln(x)").Limit("x", 0).Print();     // -inf
ExprParser.Parse("sqrt(x)").Limit("x", 0, LimitDirection.Left);
// InvalidOperationException: The limit of sqrt(x) at x = 0 from the left doesn't exist: sqrt(x) isn't real there.
```

## The result

A `Limit` has a `Kind`, `Finite`, `PositiveInfinity` or `NegativeInfinity`, and the `Value` of
a finite one, exact:

```csharp
Limit limit = ExprParser.Parse("(1 + 1/x)^x").Limit("x", double.PositiveInfinity);

limit.Kind;                 // Finite
limit.Value.Evaluate();     // 2.718281828459045

Limit pole = ExprParser.Parse("1/x^2").Limit("x", 0);

pole.Kind;                  // PositiveInfinity
pole.Print();               // inf
pole.Value;                 // InvalidOperationException: The limit is inf, not a value.
```

## Symbols

Other variables are constants, in the expression and in the point:

```csharp
ExprParser.Parse("(1 + a/x)^x").Limit("x", double.PositiveInfinity).Print();          // exp(a)
ExprParser.Parse("(x^2 - a^2)/(x - a)").Limit("x", ExprParser.Parse("a")).Print();   // 2a
ExprParser.Parse("a*x").Limit("x", double.PositiveInfinity);
// NotSupportedException: The limit of a * x as x -> inf depends on the sign of a.
```

A coefficient with a symbol is taken as other than 0, as in [series](series.md#symbols), and
where the limit depends on the sign of a symbol, it throws, as for a*x above.

## Oscillations

sin(x) and cos(x) of something that grows without bound oscillate, and so do floor, ceiling and
round of it less itself. Where a part oscillates, the limit is found if what it is next to
settles it: a bounded factor times something that tends to 0 tends to 0, a sum or a factor of
a product is as large as its unbounded part, x + sin(x) and floor(x) as x, and a sum of terms
that have limits tends to their sum:

```csharp
var inf = double.PositiveInfinity;

ExprParser.Parse("sin(x)/x").Limit("x", inf).Print();            // 0
ExprParser.Parse("x/(x + sin(x))").Limit("x", inf).Print();      // 1
ExprParser.Parse("exp(-x)*floor(x)").Limit("x", inf).Print();    // 0
ExprParser.Parse("x*sin(1/x)").Limit("x", 0).Print();            // 0
ExprParser.Parse("sin(1/x)").Limit("x", 0);
// NotSupportedException: The limit of sin(1 / x) at x = 0 isn't found: a part of it oscillates there.
```

sin(1/x) at 0 has no limit, but an oscillation isn't told apart from one these rules can't
settle, so it throws `NotSupportedException`, not `InvalidOperationException`.

## What isn't found

Where the limit isn't found, `Limit` throws `NotSupportedException`:

| Expression | At | Why |
|---|---|---|
| `sin(x)`, `x*sin(x)` | inf | it oscillates without a limit |
| `x/(2 + sin(x))` | inf | an oscillating factor isn't squeezed unless its unbounded part is separate, as in x + sin(x) |
| `2^x - 3^x`, `x^pi - x^2` | inf | two rates of growth at an irrational ratio in one sum; in one product or under one logarithm they are found, as in 2^x/3^x and (2^x + 3^x)^(1/x) |
| `exp((i - 1)*x)` | inf | the sign of a complex number |
| `x^a` | inf | the sign of a |

## How it works

The limit at a point from the right is that of f(a + 1/t) as t -> inf, from the left that of
f(a - 1/t), and at -inf that of f(-t), so every limit is one at inf, found by Gruntz's algorithm
(D. Gruntz, On Computing Limits in a Symbolic Manipulation System, 1996). Powers with the
variable in the exponent are written as exponentials, b^u = exp(u ln(b)), and so are the
hyperbolic functions, their inverses as logarithms, and abs, sign, min, max, floor, ceiling and
round as what they are for large t. Of the parts of the expression, the variable itself and the
exponentials that grow or vanish, those whose logarithms grow fastest are found by comparing
them, each comparison a limit of its own. They are written as powers of one of them, w, which
tends to 0, and the expression is expanded in w as a [series](series.md), with what grows slower
as constants. Its lowest term c w^e gives the limit: 0 for e > 0, inf or -inf by the sign of c
for e < 0, and for e = 0 the limit of c, which grows slower than the expression did, found the
same way. A limit from both sides is that of each side, and where they differ, the expression is
evaluated next to the point to tell whether it is real there.
