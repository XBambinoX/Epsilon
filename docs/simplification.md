# Simplification

```csharp
Expr Simplify(this Expr expr, SimplifyMode mode = SimplifyMode.Generic)
Expr Simplify(this Expr expr, Assumptions assumptions, SimplifyMode mode = SimplifyMode.Generic)
```

`Simplify` rewrites an expression into a simpler, equivalent one and returns it in
canonical form. It never modifies the original — expressions are immutable.

## The guarantee

**`Simplify` never changes the value of an expression at a point where the expression is
defined.** A rewrite that is valid only under a condition — `sqrt(x^2) = x` needs
`x ≥ 0` — is applied only when the condition is known to hold. When in doubt, the expression
is left alone: a missed simplification is always safer than a wrong one.

```csharp
ExprParser.Parse("sqrt(x^2)").Simplify().Print();     // abs(x)
ExprParser.Parse("(x^2)^(1/2)").Simplify().Print();   // (x^2)^(1/2)
```

## What it does

**Constants are folded exactly**, in rational arithmetic. Roots are computed only when the
result is exact:

```csharp
ExprParser.Parse("1/3 + 1/6").Simplify().Print();    // 1/2
ExprParser.Parse("2^-2").Simplify().Print();         // 1/4
ExprParser.Parse("sqrt(9/4)").Simplify().Print();    // 3/2
ExprParser.Parse("nthroot(-8, 3)").Simplify().Print(); // -2
ExprParser.Parse("round(5/2)").Simplify().Print();   // 3
```

**Like terms and repeated factors are combined** across the whole sum or product, wherever
they are:

```csharp
ExprParser.Parse("2x + 3x").Simplify().Print();       // 5x
ExprParser.Parse("x*y*x*y").Simplify().Print();       // x^2 * y^2
ExprParser.Parse("2*x*3*x").Simplify().Print();       // 6x^2
ExprParser.Parse("x*(-y)*x").Simplify().Print();      // -x^2 * y
ExprParser.Parse("(x + 1) - 1").Simplify().Print();   // x
```

**Common factors cancel** in quotients, and the numbers in them reduce like a fraction:

```csharp
ExprParser.Parse("(2x)/(4x)").Simplify().Print();       // 1/2
ExprParser.Parse("(x^2*y)/(x*y)").Simplify().Print();   // x
ExprParser.Parse("x^3/x").Simplify().Print();           // x^2
ExprParser.Parse("4x/6").Simplify().Print();            // 2x / 3
ExprParser.Parse("x/(-2)").Simplify().Print();          // -x / 2
```

**Identities.** The Pythagorean identities (`sin^2 + cos^2 = 1`, `sec^2 - tan^2 = 1`,
`csc^2 - cot^2 = 1`) work across a whole sum, with any coefficients and other terms in
between:

```csharp
ExprParser.Parse("2sin(x)^2 + 2cos(x)^2 + 1").Simplify().Print();   // 3
ExprParser.Parse("1 - sin(x)^2").Simplify().Print();                // cos(x)^2
ExprParser.Parse("sec(x)^2 - tan(x)^2").Simplify().Print();         // 1
ExprParser.Parse("y - csc(x)^2 + cot(x)^2").Simplify().Print();     // y - 1
ExprParser.Parse("2sec(x)^2 - tan(x)^2").Simplify().Print();        // sec(x)^2 + 1
ExprParser.Parse("sin(x)/cos(x)").Simplify().Print();               // tan(x)
```

Also `ln(exp(x)) = x`, `exp(ln(x)) = x`, `sqrt(x)^2 = x`, `tan(x) * cot(x) = 1`, `abs` of
a non-negative expression, and more.

**Exact values of functions** at the usual points: the trigonometric functions at
multiples of pi/6 and pi/4, `asin`, `acos` and `atan` at the matching values, `ln(1)`,
`ln(e)`, `exp(0)` and the hyperbolic functions at 0. Square roots of rational numbers are
written with the smallest integer under the root, and products, quotients and powers of
them are worked out:

```csharp
ExprParser.Parse("sin(pi/6)").Simplify().Print();    // 1/2
ExprParser.Parse("cos(3pi/4)").Simplify().Print();   // -sqrt(2) / 2
ExprParser.Parse("acos(-1/2)").Simplify().Print();   // 2 * π / 3
ExprParser.Parse("sqrt(8)").Simplify().Print();      // 2sqrt(2)
ExprParser.Parse("1/sqrt(2)").Simplify().Print();    // sqrt(2) / 2
ExprParser.Parse("2sin(pi/3)").Simplify().Print();   // sqrt(3)
ExprParser.Parse("ln(e^2)").Simplify().Print();      // 2
```

Where a function is undefined it stays as it is: `tan(pi/2)` and `csc(0)` are not folded.

What `Simplify` *doesn't* know yet (double-angle formulas, logarithm rules, exact values at
other points such as `sin(pi/12)`) is listed in [limitations](limitations.md#algebra).

## Expanding

```csharp
Expr Expand(this Expr expr, SimplifyMode mode = SimplifyMode.Generic)
```

`Simplify` leaves brackets as they are: `(x + 1)^2` stays a square. `Expand` multiplies out
products and positive integer powers of sums and simplifies the result, so like terms
combine. Arguments of functions are expanded too:

```csharp
ExprParser.Parse("(x + 1)^2 - x^2").Expand().Print();   // 2x + 1
ExprParser.Parse("(x + 1)*(x - 1)").Expand().Print();   // x^2 - 1
ExprParser.Parse("sin((x + 1)^2)").Expand().Print();    // sin(x^2 + 2x + 1)
```

A quotient stays one fraction, with its numerator and denominator expanded, unless the
denominator is a number: then every term is divided. Negative powers are left as they are:

```csharp
ExprParser.Parse("(x + 1)^2/(x - 1)").Expand().Print();   // (x^2 + 2x + 1) / (x - 1)
ExprParser.Parse("(x + 1)^2/2").Expand().Print();         // (1/2) * x^2 + x + 1/2
ExprParser.Parse("(x + 1)^-2").Expand().Print();          // (x + 1)^-2
```

The mode is the one of `Simplify` (see below): `(sqrt(x) + 1)^2` expands to
`x + 2sqrt(x) + 1` in Generic mode, while Strict mode keeps `sqrt(x)^2`, which is undefined
for x < 0.

The result can be much longer than the input: `(x + 1)^n` has n + 1 terms and
`(x + y + z)^n` about n^2/2. `(x + 1)^100` takes about 50 ms.

## Generic and Strict mode

Some rewrites don't change any value but make the expression **defined at more points**.
`x/x` is undefined at 0; `1` is defined everywhere. The mode decides whether that is allowed.

**`SimplifyMode.Generic`** (the default) allows it. The result equals the original
everywhere the original is defined, and may be defined at extra points. This is what most
people expect from a simplifier.

**`SimplifyMode.Strict`** keeps exactly the same domain. Such rewrites are applied only when
[assumptions](assumptions.md) prove them safe:

```csharp
Expr e = ExprParser.Parse("x/x");

e.Simplify().Print();                                              // 1
e.Simplify(SimplifyMode.Strict).Print();                           // x / x
e.Simplify(Assumptions.None.AssumeNonZero("x"), SimplifyMode.Strict).Print();   // 1
```

More examples of the difference:

| Expression | Generic | Strict | Undefined where |
|---|---|---|---|
| `x^3/x` | `x^2` | `x^3 / x` | x = 0 |
| `0/x` | `0` | `0 / x` | x = 0 |
| `1/(2/x)` | `x / 2` | `1 / (2 / x)` | x = 0 |
| `sqrt(x)^2` | `x` | `sqrt(x)^2` | x < 0 |
| `exp(ln(x))` | `x` | `exp(ln(x))` | x ≤ 0 |
| `tan(x)*cot(x)` | `1` | `cot(x) * tan(x)` | multiples of π/2 |

Rewrites that change no domain, like `sin(x)/cos(x) = tan(x)`, happen in both modes.

**Use Strict when the undefined points matter** — for example, when you look for where an
expression is undefined, or before root finding. `SolveNumerically` simplifies `left - right`
in Strict mode internally, so `x^2/x = 0` has no solution at 0.

Even Generic mode never folds a literal `0/0` and never changes a value that is defined:

```csharp
ExprParser.Parse("0/0").Simplify().Print();   // 0 / 0
```

## Cost

`Simplify` repeats its rules until nothing changes. It takes about 20 us for an everyday
formula. A sum is combined in one pass, so its cost grows about in proportion to the number
of terms: 300 terms take under 1 ms.
In a hot loop, simplify once and [evaluate](evaluation.md) many times.
