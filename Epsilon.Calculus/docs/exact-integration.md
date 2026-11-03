# Exact definite integrals

`IntegrateExact(variable, a, b)` computes a definite integral exactly: F(b) - F(a) for the
antiderivative F that [symbolic integration](symbolic-integration.md) finds, with the checks the
formula needs. Where F isn't defined at a limit, its [limit](limits.md) there is taken, at inf
and -inf as well, where it jumps between the limits while the integrand doesn't, the jumps are
taken off, and where the integral diverges, it throws. The integral of an odd integrand over an
interval symmetric about 0 is 0, also where there is no antiderivative, and the classic
integrals of integrands without one, as exp(-x^2) over the line, come from a table.

`Integrate("x", 0, 1)` with numbers for the limits is the
[numeric integral](numeric-integration.md), a `double`; the exact one has its own name.

**Contents:**
[Definite integrals](#definite-integrals) -
[Singular limits and points inside](#singular-limits-and-points-inside) -
[Infinite limits](#infinite-limits) -
[Classic integrals](#classic-integrals) -
[Across periods and jumps](#across-periods-and-jumps) -
[Odd integrands](#odd-integrands) -
[Divergent integrals](#divergent-integrals) -
[Symbols](#symbols) -
[What isn't found](#what-isnt-found) -
[How it works](#how-it-works)

## Definite integrals

```csharp
ExprParser.Parse("x^2").IntegrateExact("x", 0, 1).Print();            // 1/3
ExprParser.Parse("1/(1 + x^2)").IntegrateExact("x", 0, 1).Print();    // (1/4) * π
ExprParser.Parse("1/(x^2 - 1)").IntegrateExact("x", 2, 3).Print();    // (1/2) * ln(3/2)
ExprParser.Parse("1/(x^3 + 1)").IntegrateExact("x", 0, 1).Print();    // π / (3sqrt(3)) + (1/3) * ln(2)
ExprParser.Parse("x^2").IntegrateExact("x", 1, 0).Print();            // -1/3
```

The limits are expressions. A `double` is the decimal it prints as, as for
[limits](limits.md#limits-at-a-point), so give pi as `new Pi()` or `ExprParser.Parse("pi")`.
Without a variable name, the expression's only variable is taken:

```csharp
ExprParser.Parse("sin(x)").IntegrateExact("x", 0, new Pi()).Print();                   // 2
ExprParser.Parse("exp(-x)").IntegrateExact("x", 0, ExprParser.Parse("ln(2)")).Print(); // 1/2
ExprParser.Parse("sin(t)").IntegrateExact(0, new Pi()).Print();                        // 2

ExprParser.Parse("sin(x)").IntegrateExact("x", 0, Math.PI).Print();
// -cos(314159265358979/100000000000000) + 1
```

## Singular limits and points inside

Where the integrand or its antiderivative isn't defined at a limit, the limit of the
antiderivative from inside the interval is taken, and the integral converges where it is
finite. A point inside where the integrand is infinite is the same from each side:

```csharp
ExprParser.Parse("ln(x)").IntegrateExact("x", 0, 1).Print();               // -1
ExprParser.Parse("1/sqrt(x)").IntegrateExact("x", 0, 4).Print();           // 4
ExprParser.Parse("x*ln(x)").IntegrateExact("x", 0, 1).Print();             // -1/4
ExprParser.Parse("1/nthroot(x, 3)").IntegrateExact("x", -1, 8).Print();    // 9/2
```

The integrand 1/(1 + tan(x)) is 0 at pi/2, but its antiderivative has tan(x) in it:

```csharp
var f = ExprParser.Parse("1/(1 + tan(x))");

f.Integrate("x").Print();
// (1/2) * x + (-1/4) * ln(tan(x)^2 + 1) + (1/2) * ln(abs(tan(x) + 1))
f.IntegrateExact("x", 0, ExprParser.Parse("pi/2")).Print();   // (1/4) * π
```

## Infinite limits

A limit may be `double.PositiveInfinity` or `double.NegativeInfinity`, and the limit of the
antiderivative there is taken. Each limit is an `Expr` or a `double`, in any mix, so an exact
limit goes with an infinite one:

```csharp
var inf = double.PositiveInfinity;

ExprParser.Parse("exp(-x)").IntegrateExact("x", 0, inf).Print();                // 1
ExprParser.Parse("1/(1 + x^2)").IntegrateExact("x", -inf, inf).Print();         // π
ExprParser.Parse("exp(-x)*sin(x)").IntegrateExact("x", 0, inf).Print();         // 1/2
ExprParser.Parse("1/(x^4 + 1)").IntegrateExact("x", 0, inf).Print();            // (1/4) * π * sqrt(2)
ExprParser.Parse("1/(sqrt(x)*(1 + x))").IntegrateExact("x", 0, inf).Print();    // π
ExprParser.Parse("exp(-x)").IntegrateExact("x", new Pi(), inf).Print();         // exp(-π)
ExprParser.Parse("1/x").IntegrateExact("x", 1, inf);
// InvalidOperationException: The integral of 1 / x from 1 to inf diverges at inf.
```

1/(sqrt(x) (1 + x)) is infinite at 0 as well, where the limit of its antiderivative is taken
from the right.

## Classic integrals

Some integrals have exact values though their integrands have no antiderivative made of
elementary functions. Where `Integrate` finds none, they are taken from a table, over the
intervals they are known on: the whole line, from 0 to inf, from -inf to 0 by x -> -x, and
[0, 1], [0, pi/2] and [0, pi]. An even integrand over the line is twice that over the half-line,
and back, and a sum is integrated term by term:

```csharp
var inf = double.PositiveInfinity;

ExprParser.Parse("exp(-x^2)").IntegrateExact("x", -inf, inf).Print();               // sqrt(π)
ExprParser.Parse("exp(-x^2/2)/sqrt(2*pi)").IntegrateExact("x", -inf, inf).Print();  // 1
ExprParser.Parse("sin(x)/x").IntegrateExact("x", 0, inf).Print();                   // π / 2
ExprParser.Parse("sqrt(x)*exp(-x)").IntegrateExact("x", 0, inf).Print();            // sqrt(π) / 2
ExprParser.Parse("x^3/(exp(x) - 1)").IntegrateExact("x", 0, inf).Print();           // (1/15) * π^4
ExprParser.Parse("ln(x)/(1 - x)").IntegrateExact("x", 0, 1).Print();                // (-1/6) * π^2
ExprParser.Parse("exp(-x^2) + exp(-x)").IntegrateExact("x", 0, inf).Print();        // (1/2) * sqrt(π) + 1
```

| Integrand | From, to | Value |
|---|---|---|
| `P(x)*exp(-a*x^2 + b*x + c)`, P a polynomial | -inf, inf | sqrt(pi/a) exp(c + b^2/(4a)) for P = 1, and by the moments of the normal distribution for others |
| `cos(k*x + p)*exp(-a*x^2 + b*x + c)`, and with sin | -inf, inf | sqrt(pi/a) exp(c + (b^2 - k^2)/(4a)) cos(k b/(2a) + p), and sin of the same |
| `x^s*exp(-a*x^p)` | 0, inf | Gamma((s + 1)/p) / (p a^((s + 1)/p)), where (s + 1)/p is an integer or an integer and a half |
| `sin(k*x)/x`, `sin(k*x)^2/x^2` | 0, inf | sign(k) pi/2, and abs(k) pi/2 |
| `exp(-a*x)*sin(k*x)/x` | 0, inf | atan(k/a) |
| `sin(k*x^2)`, `cos(k*x^2)` | 0, inf | sqrt(pi/(2k))/2 for k > 0 |
| `x^n/(exp(k*x) - 1)`, `x^n/(exp(k*x) + 1)` for an odd n | 0, inf | n! zeta(n + 1)/k^(n + 1), and that times 1 - 2^-n with + 1 |
| `ln(x)/(1 - x)`, `ln(x)/(1 + x)`, `ln(1 + x)/x`, `ln(1 - x)/x` | 0, 1 | -pi^2/6, -pi^2/12, pi^2/12, -pi^2/6 |
| `ln(sin(x))`, `ln(cos(x))` | 0, pi/2 | -pi ln(2)/2, and ln(sin(x)) over [0, pi] twice that |

The coefficients a and k are numbers: with a symbol the value depends on its sign, and
`IntegrateExact` throws `NotSupportedException`:

```csharp
ExprParser.Parse("exp(-a*x^2)").IntegrateExact("x", double.NegativeInfinity, double.PositiveInfinity);
// NotSupportedException: The integral of exp(-x^2 * a) from -inf to inf isn't found: it depends on the sign of a.
```

The values are written with Gamma at integers and halves and zeta at even numbers, which are
rational multiples of powers of pi and its square root. exp(-x^3) from 0 to inf is Gamma(4/3)
and x^2/(exp(x) - 1) is 2 zeta(3), which have no such form, and these throw.

## Across periods and jumps

Antiderivatives of rational functions of sin and cos are continuous where the integrand is,
from the [table](symbolic-integration.md#trigonometric-and-hyperbolic-functions) and Bioche's
rules, so over a whole period they give what the area is. Those by t = tan(x/2) are undefined
where tan(x/2) is, and there the limits from both sides are taken:

```csharp
ExprParser.Parse("1/(2 + cos(x))").IntegrateExact("x", 0, ExprParser.Parse("2*pi")).Print();
// 2 * π / sqrt(3)
ExprParser.Parse("1/(1 + sin(x))^2").IntegrateExact("x", 0, 3).Print();
// -2 * (3tan(3/2)^2 + 3tan(3/2) + 2) / (3(tan(3/2) + 1)^3) + 4/3
```

Where an antiderivative jumps while the integrand doesn't, the jumps are taken off.
sec(x)^2/(1 + tan(x)^2) is 1 wherever it is defined, and by u = tan(x) its antiderivative is
atan(tan(x)), which falls by pi at each pole of tan:

```csharp
var one = ExprParser.Parse("sec(x)^2/(1 + tan(x)^2)");

one.Integrate("x").Print();                 // atan(tan(x))
one.IntegrateExact("x", 0, 10).Print();     // 10
```

## Odd integrands

An odd integrand, f(-x) = -f(x), over an interval symmetric about 0 gives 0 where it is bounded,
with an antiderivative or without one: sin(x)/(1 + x^2) has none made of elementary functions.
Over the whole line it must also fall off at least as fast as 1/|x|^(3/2). Where it doesn't,
the integral may diverge, and it is taken from the antiderivative:

```csharp
var inf = double.PositiveInfinity;

ExprParser.Parse("x^3*cos(x)").IntegrateExact("x", -2, 2).Print();             // 0
ExprParser.Parse("sin(x)/(1 + x^2)").IntegrateExact("x", -1, 1).Print();       // 0
ExprParser.Parse("atan(1/x)").IntegrateExact("x", -1, 1).Print();              // 0
ExprParser.Parse("sin(x)/(1 + x^2)").IntegrateExact("x", -inf, inf).Print();   // 0
ExprParser.Parse("1/x^3").IntegrateExact("x", -1, 1);
// InvalidOperationException: The integral of 1 / x^3 from -1 to 1 diverges at x = 0.
ExprParser.Parse("x/(1 + x^2)").IntegrateExact("x", -inf, inf);
// InvalidOperationException: The integral of x / (x^2 + 1) from -inf to inf diverges at inf.
```

## Divergent integrals

Where the antiderivative tends to inf or -inf at a limit or at a point inside, the integral
diverges and `IntegrateExact` throws `InvalidOperationException`. There is no principal value:
1/x from -1 to 1 diverges on each side of 0, and x/(1 + x^2) over the whole line at each end.
An integrand that isn't real on a part of the interval throws too:

```csharp
ExprParser.Parse("1/x").IntegrateExact("x", -1, 1);
// InvalidOperationException: The integral of 1 / x from -1 to 1 diverges at x = 0.
ExprParser.Parse("tan(x)").IntegrateExact("x", 0, new Pi());
// InvalidOperationException: The integral of tan(x) from 0 to π diverges at x = (1/2) * π.
ExprParser.Parse("sqrt(x)").IntegrateExact("x", -1, 1);
// InvalidOperationException: The integral of sqrt(x) from -1 to 1 doesn't exist: the integrand isn't real at x = -0.5.
```

## Symbols

Other variables are constants, as in `Integrate`, and the limits may have them where the
antiderivative is continuous everywhere:

```csharp
ExprParser.Parse("exp(c*x)").IntegrateExact("x", 0, 1).Print();               // exp(c) / c - 1 / c
ExprParser.Parse("x^2").IntegrateExact("x", 0, new Variable("c")).Print();    // (1/3) * c^3
ExprParser.Parse("exp(i*x)").IntegrateExact("x", 0, new Pi()).Print();        // 2 * i
```

Where whether the antiderivative is undefined or jumps between the limits depends on a symbol,
`IntegrateExact` throws `NotSupportedException`:

```csharp
ExprParser.Parse("1/x").IntegrateExact("x", 1, new Variable("c"));
// NotSupportedException: The integral of 1 / x from 1 to c isn't found: whether its antiderivative ln(abs(x)) is undefined or jumps between the limits depends on c.
```

## What isn't found

Where the integral isn't found, `IntegrateExact` throws `NotSupportedException`:

| Integrand | From, to | Why |
|---|---|---|
| `sin(x)/x` | 1, 2 | no antiderivative made of elementary functions, and the interval isn't one of the [classic integrals](#classic-integrals); the [numeric integral](numeric-integration.md) gives the number |
| `exp(-x^2)` | 0, 1 | sqrt(pi) erf(1)/2, and erf isn't there |
| `exp(-x^3)`, `x^2/(exp(x) - 1)` | 0, inf | Gamma(4/3) and 2 zeta(3) |
| `cos(x)` | 0, inf | the antiderivative sin(x) oscillates at inf: the integral diverges, but an oscillation isn't told apart from a limit that isn't found, as for [limits](limits.md#oscillations) |
| `exp(-c*x)` | 0, inf | it depends on the sign of c |
| `1/(x + c)` | 0, 1 | whether the antiderivative ln(abs(x + c)) is undefined between the limits depends on c |
| `(1 - sin(x))/(x + cos(x))` | -1, 0 | the antiderivative ln(abs(x + cos(x))) is undefined at -0.739..., which is found only numerically, and a limit at a decimal says nothing |
| `1/(1 + cos(x)^4)` | 0, 10 | at pi/2 the logarithms in its antiderivative cancel, but their coefficients aren't simplified to show it |
| `cos(1/x)/x^2` | -1, 1 | the antiderivative -sin(1/x) oscillates at 0 |

## How it works

The antiderivative F is found as by `Integrate`; where there is none, the integrand is looked up
in the table of classic integrals for the interval, and a sum is taken term by term. The points
between the limits where F may be
undefined or jump are where something in it vanishes: a denominator, the argument of a
logarithm, a root or sign, the base of a fractional or negative power, the cosine under tan
and sec, the sine under cot and csc, and where floor, ceiling and round step. They are found
exactly where that part is linear, a polynomial with rational coefficients with linear and
quadratic factors, or such a polynomial in tan, sin or cos of a linear argument, as tan(x/2) + 1,
which vanishes at 3 pi/2. At each point the limits of F from the left and from the right are
taken: if one is inf or -inf the integral diverges, and if they differ, the jump is taken off.
At an infinite limit, and at a finite one where F is undefined, the limit of F from inside is
taken, and elsewhere its value. The integrand is evaluated between the points where it may be
undefined to check that it is real there. Before all this, an integrand that f(-x) + f(x)
simplifies to 0 for, over a symmetric interval, gives 0 where its limits from inside at the
points it may be undefined at are finite, and over the whole line where x^(3/2) f(x) has a
finite limit at inf too. The result is simplified, with sin(-u) written as -sin(u), logarithms
of rationals merged, ln(1/2) + ln(1/3) as -ln(6), terms that differ by a factor a + b sqrt(d)
added up, and numbers a + b sqrt(d) brought to one fraction.
