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
ExprParser.Parse("(x^2)^(1/3)").Simplify().Print();   // (x^2)^(1/3)   (x^(2/3) is undefined for x < 0)
ExprParser.Parse("(x^2)^3").Simplify().Print();       // x^6
```

## What it does

**Constants are folded exactly**, in rational arithmetic. Roots are computed only when the
result is exact:

```csharp
ExprParser.Parse("1/3 + 1/6").Simplify().Print();    // 1/2
ExprParser.Parse("2^-2").Simplify().Print();         // 1/4
ExprParser.Parse("sqrt(9/4)").Simplify().Print();    // 3/2
ExprParser.Parse("8^(-2/3)").Simplify().Print();     // 1/4
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

A number anywhere in a term counts as its coefficient, square roots of numbers included, so
terms that differ only by one add up. A term without such a partner keeps its form:

```csharp
ExprParser.Parse("x/(2*y) + x/(3*y)").Simplify().Print();               // 5x / (6y)
ExprParser.Parse("x*sqrt(2) + x/sqrt(2)").Simplify().Print();           // 3sqrt(2) * x / 2
ExprParser.Parse("(1/6)*pi*sqrt(3) + pi/(6*sqrt(3))").Simplify().Print(); // (2/9) * π * sqrt(3)
ExprParser.Parse("x/sqrt(3)").Simplify().Print();                       // x / sqrt(3)
```

A power 1/2 is written as a square root, and a square root counts as the power 1/2 among the
other factors:

```csharp
ExprParser.Parse("x^(1/2)").Simplify().Print();             // sqrt(x)
ExprParser.Parse("x*sqrt(x)").Simplify().Print();           // x^(3/2)
ExprParser.Parse("sqrt(y) - y^(1/2)").Simplify().Print();   // 0
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
`csc^2 - cot^2 = 1`) and their hyperbolic forms (`cosh^2 - sinh^2 = 1`, `sech^2 + tanh^2 = 1`,
`coth^2 - csch^2 = 1`) work across a whole sum, with any coefficients, a common factor and
other terms in between:

```csharp
ExprParser.Parse("2sin(x)^2 + 2cos(x)^2 + 1").Simplify().Print();   // 3
ExprParser.Parse("1 - sin(x)^2").Simplify().Print();                // cos(x)^2
ExprParser.Parse("sec(x)^2 - tan(x)^2").Simplify().Print();         // 1
ExprParser.Parse("y - csc(x)^2 + cot(x)^2").Simplify().Print();     // y - 1
ExprParser.Parse("2sec(x)^2 - tan(x)^2").Simplify().Print();        // sec(x)^2 + 1
ExprParser.Parse("1 + tan(x)^2").Simplify().Print();                // sec(x)^2
ExprParser.Parse("r^2*cos(t)^2 + r^2*sin(t)^2").Simplify().Print(); // r^2
ExprParser.Parse("1 + sinh(t)^2").Simplify().Print();               // cosh(t)^2
ExprParser.Parse("sin(x)/cos(x)").Simplify().Print();               // tan(x)
```

Also `ln(exp(x)) = x`, `exp(ln(x)) = x`, `exp(2ln(x)) = x^2`, `sqrt(x)^2 = x`,
`tan(x) * cot(x) = 1`, `abs` of a non-negative expression, and more.

**An inverse function after its function** gives the argument back: `tan(atan(x)) = x`,
`asinh(sinh(x)) = x`, `acosh(cosh(x)) = abs(x)` and the like. Where the inverse is defined only
on part of the line, as for `sin(asin(x))`, that holds in Generic mode. At a number, the
inverse trigonometric functions land in their principal range:

```csharp
ExprParser.Parse("tan(atan(x))").Simplify().Print();    // x
ExprParser.Parse("atan(tan(10))").Simplify().Print();   // 10 - 3 * π
ExprParser.Parse("acos(cos(10))").Simplify().Print();   // 4 * π - 10
```

**Odd and even functions** take the minus sign out of the argument: sin, tan, cot, csc, their
hyperbolic versions, asin, atan, asinh, atanh, sign and odd roots are odd; cos, sec, cosh,
sech and abs are even, and so are even powers. An argument that is a sum keeps its form, so
`sin(1 - x)` stays:

```csharp
ExprParser.Parse("sin(-x) + sin(x)").Simplify().Print();   // 0
ExprParser.Parse("cos(-2x)").Simplify().Print();           // cos(2x)
ExprParser.Parse("atan(-x/2)").Simplify().Print();         // -atan(x / 2)
ExprParser.Parse("(-x)^3").Simplify().Print();             // -x^3
```

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
ExprParser.Parse("12^(3/2)").Simplify().Print();     // 24sqrt(3)
ExprParser.Parse("2sin(pi/3)").Simplify().Print();   // sqrt(3)
ExprParser.Parse("ln(e^2)").Simplify().Print();      // 2
```

Logarithms of rational numbers take out a perfect power and write `ln(1/n)` as `-ln(n)`, and
in a sum they merge where the coefficients are multiples of the smallest one:

```csharp
ExprParser.Parse("ln(9)").Simplify().Print();                           // 2ln(3)
ExprParser.Parse("ln(1/8)").Simplify().Print();                         // -3ln(2)
ExprParser.Parse("(1/2)*ln(1/2) - (1/2)*ln(1/3)").Simplify().Print();   // (1/2) * ln(3/2)
ExprParser.Parse("ln(6) - ln(2) - ln(3)").Simplify().Print();           // 0
```

A number divided by a sum with a square root, such as `sqrt(3) + 2`, is multiplied by the
conjugate, so no root is left in the denominator, and the sign of such a number is known
exactly. Roots of two different numbers are left as they are:

```csharp
ExprParser.Parse("1/(sqrt(3) + 2)").Simplify().Print();          // -sqrt(3) + 2
ExprParser.Parse("-2/(2*sqrt(2) + 4) + 1").Simplify().Print();   // sqrt(2) / 2
ExprParser.Parse("abs(1 - sqrt(2))").Simplify().Print();         // sqrt(2) - 1
ExprParser.Parse("1/(sqrt(2) + sqrt(3))").Simplify().Print();    // 1 / (sqrt(2) + sqrt(3))
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
| `sin(asin(x))` | `x` | `sin(asin(x))` | abs(x) > 1 |
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
