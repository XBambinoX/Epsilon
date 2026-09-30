# Known limitations

Epsilon follows one rule above all: **never wrong, sometimes unsimplified**. Most of the
limitations below are therefore things the library *doesn't do yet*, not wrong answers. The
few places where a result can surprise you are marked **⚠**.

**All of this is planned to be addressed in upcoming versions of the core** — expansion,
exact function values, more identities, piecewise derivatives, complex rounding functions,
a symbolic equation solver, a safer root-finding API, protection against deep nesting and
faster simplification. The only exception is `min`/`max` over the complex numbers, which
can't exist mathematically. See the [roadmap](../README.md#roadmap) for the order.

Everything here reflects version 1.0.0. Each example is checked by a test
(in `Epsilon.Tests/DocsExamplesTests.cs`), so when a limitation is lifted this page is updated with it.

## Algebra

### No expansion of products and powers

There is no `Expand` operation, and `Simplify` does not multiply out brackets. Two forms of
the same polynomial may therefore not simplify to the same thing:

```csharp
ExprParser.Parse("(x + 1)^2 - x^2").Simplify().Print();   // -x^2 + (x + 1)^2
ExprParser.Parse("(x + 1)*(x - 1)").Simplify().Print();   // (x + 1) * (x - 1)
```

For the same reason `TryFactorReal` and `TryFactorComplex` only accept a polynomial that is
already expanded:

```csharp
ExprParser.Parse("(x - 2)^4").TryFactorReal("x").Success;   // false
```

### Factoring is exact but limited

Factors are found only when their roots are rational numbers, square roots of rationals
or `i`. Anything else is left as it is rather than approximated:

```csharp
ExprParser.Parse("x^4 + 1").TryFactorReal("x").Success;     // false: no rational or sqrt roots
ExprParser.Parse("x^3 + x + 1").TryFactorReal("x").Success; // false: its real root isn't a square root
ExprParser.Parse("x^2 - y^2").TryFactorReal("x").Success;   // false: other variables are not supported
```

### No exact values of functions

Functions of constants are not evaluated symbolically, and radicals are not reduced:

```csharp
ExprParser.Parse("sin(pi/6)").Simplify().Print();   // sin(π / 6)   (not 1/2)
ExprParser.Parse("asin(1/2)").Simplify().Print();   // asin(1/2)    (not π / 6)
ExprParser.Parse("sqrt(8)").Simplify().Print();     // sqrt(8)      (not 2sqrt(2))
```

`Evaluate` still gives the numeric values.

### Few identities

`Simplify` knows the Pythagorean identity (`sin(x)^2 + cos(x)^2 = 1`), `sin/cos = tan` and
similar quotients, and inverse pairs such as `ln(exp(x)) = x`. It does not apply
double-angle formulas or logarithm rules:

```csharp
ExprParser.Parse("2sin(x)cos(x)").Simplify().Print();     // 2cos(x) * sin(x)
ExprParser.Parse("ln(x*y) - ln(x)").Simplify().Print();   // ln(x * y) - ln(x)
```

### Equality is structural

`Equals` on expressions compares trees, not mathematical values. Simplify both sides
first when you want to compare them:

```csharp
ExprParser.Parse("x*x").Equals(ExprParser.Parse("x^2"));              // false
ExprParser.Parse("x*x").Simplify().Equals(ExprParser.Parse("x^2"));   // true
```

Even after `Simplify`, two equal expressions can have different forms (see the sections
above), so `false` means "not shown to be equal", not "different".

### No symbolic equation solver

There is no way yet to solve `sin(x) = 1/2` symbolically as `π/6 + 2πk, 5π/6 + 2πk`. Use
`FindRealRoots` for numeric solutions in a range. A symbolic solver is planned for 1.1 (see
the [roadmap](../README.md#roadmap)).

## Parsing and output

- Variable names consist of letters only: `v0`, `x_1` or `x'` can't be declared.

## Differentiation

Derivatives that need piecewise expressions throw `NotSupportedException` with a message
explaining why:

| Expression | Why |
|---|---|
| `floor`, `ceiling`, `round`, `sign` | 0 between the jumps, undefined at them |
| `min(a, b)`, `max(a, b)` | left and right derivatives differ where `a = b` |
| `nthroot(x, n)` with `n` depending on the variable | not implemented; use a constant degree |

```csharp
ExprParser.Parse("floor(x)").Differentiate("x");   // NotSupportedException
```

`abs(x)` is supported: its derivative is `x / abs(x)`, undefined at 0.

## Complex numbers

- `floor`, `ceiling`, `round` and `sign` can't be evaluated with `EvaluateComplex` yet and
  throw `NotSupportedException` (planned). `min` and `max` never will: complex numbers are
  not ordered.
- **⚠** Division by zero differs between real and complex evaluation. Real `1/0` is
  `+∞` (IEEE rules); complex `1/0` is `NaN`:

```csharp
ExprParser.Parse("1/x").Evaluate(0);          // ∞
ExprParser.Parse("1/x").EvaluateComplex(0);   // NaN
```

## Root finding

Both root finders are numeric and work in `double` precision.

### Real roots

`FindRealRoots` scans a grid (200 intervals by default) and refines every sign change,
touching point and domain edge. Roots can be missed where the function oscillates faster
than the grid, and roots closer than `1e-6` are reported once:

```csharp
ExprParser.Parse("sin(1/x)").FindRealRoots(0.01, 1).Count;          // 11 of the 31 roots
ExprParser.Parse("sin(1/x)").FindRealRoots(0.01, 1, 5000).Count;     // 31
ExprParser.Parse("(x - 1)*(x - 1.0000001)").FindRealRoots(-10, 10);  // [1]
```

Increase `scanSteps` or narrow the range when that matters.

### ⚠ Underflow is reported as a root

Where a function is positive but so small that it becomes exactly `0.0` in `double`
arithmetic, `FindRealRoots` takes that point for a root. `exp(x)` has no roots, but underflows
to 0 below x ≈ -745:

```csharp
ExprParser.Parse("exp(x)").FindRealRoots();              // [-500000013.6409661]
ExprParser.Parse("exp(-x^2)").FindRealRoots().Count;     // 4, all spurious
```

Keep the range where the function is representable, or check a root by evaluating
something that doesn't underflow (for `exp`, its logarithm).

### ⚠ Overload trap: a number as the right-hand side

A plain number passed as the right-hand side of an equation binds to the **range** overload,
because `double` is a better match than the implicit conversion to `Expr`:

```csharp
Expr s = ExprParser.Parse("sin(x)");

s.FindRealRoots(-1, 2, 3);          // roots of sin(x) = 0 in [-1, 2] with 3 steps: [0]
s.FindRealRoots((Expr)(-1), 2, 3);  // roots of sin(x) = -1 in [2, 3]: []
```

Cast the number to `Expr` (or use `ExprParser.Parse("-1")`) to get the equation overload.
The same applies to `FindComplexRoots`. The API will be changed in a future version.

### ⚠ Complex roots: repeated roots are reported many times

`FindComplexRoots` starts Newton's method from every point of a grid and merges results
closer than `1e-6`. Near a root of multiplicity 2 or more, Newton converges slowly and stops
at many slightly different points, so the result contains dozens of near-duplicates:

```csharp
ExprParser.Parse("x^2 - 2x + 1").FindComplexRoots(-2, 2, -2, 2).Count;   // 98, all ≈ 1
ExprParser.Parse("x^3 - 1").FindComplexRoots(-2, 2, -2, 2).Count;        // 3: simple roots are fine
```

For polynomials, `TryFactorComplex` gives exact roots with their multiplicity.

## Robustness and performance

- **⚠ Deep nesting can overflow the stack.** Parsing, simplifying, differentiating and
  evaluating are recursive. On a 1 MB stack (the default for threads on Windows) a few
  hundred nested parentheses in `Parse` are enough for a `StackOverflowException`, which
  terminates the process and can't be caught. If you parse **untrusted input**, limit its
  length or nesting depth first, or parse on a thread with a larger stack.
- **Large expressions are slow to simplify.** `Simplify` has no caching; a sum of 300 terms
  takes about 100 ms. `Evaluate` is fast (about 90 ns for a 25-node expression), so for hot
  loops simplify once and evaluate many times.

## Not in the package

The repository also contains `Epsilon.Calculus` (integration, gradients, Hessians) and
`Epsilon.LinearAlgebra` (matrices). They are experimental and not part of the `Epsilon`
package; their APIs will change before they are published.
