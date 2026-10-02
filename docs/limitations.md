# Known limitations

Epsilon follows one rule above all: **never wrong, sometimes unsimplified**. Most of the
limitations below are therefore things the library *doesn't do yet*, not wrong answers. The
few places where a result can surprise you are marked **⚠**.

**All of this is planned to be addressed in upcoming versions of the core** — exact
values at more points, more identities, piecewise derivatives, complex rounding functions,
a symbolic equation solver, a safer root-finding API, protection against deep nesting and
faster simplification. The only exception is `min`/`max` over the complex numbers, which
can't exist mathematically. See the [roadmap](../README.md#roadmap) for the order.

Everything here reflects version 1.0.0. Each example is checked by a test
(in `Epsilon.Tests/DocsExamplesTests.cs`), so when a limitation is lifted this page is updated with it.

## Algebra

### Factoring is exact but limited

Factors are found only when their roots are rational numbers, square roots of rationals
or `i`. Anything else is left as it is rather than approximated:

```csharp
ExprParser.Parse("x^4 + 1").TryFactorReal("x").Success;     // false: no rational or sqrt roots
ExprParser.Parse("x^3 + x + 1").TryFactorReal("x").Success; // false: its real root isn't a square root
ExprParser.Parse("x^2 - y^2").TryFactorReal("x").Success;   // false: other variables are not supported
```

### Exact values only at the usual points

`Simplify` knows the trigonometric functions at multiples of pi/6 and pi/4, their inverses at
the matching values, and square roots of rational numbers
([details](simplification.md#what-it-does)). Other angles, logarithms of other numbers and
roots written as powers are left as they are:

```csharp
ExprParser.Parse("sin(pi/12)").Simplify().Print();   // sin(π / 12)   (not (sqrt(6) - sqrt(2)) / 4)
ExprParser.Parse("ln(8)").Simplify().Print();        // ln(8)         (not 3ln(2))
ExprParser.Parse("8^(1/2)").Simplify().Print();      // 8^(1/2)       (not 2sqrt(2))
```

`Evaluate` still gives the numeric values.

### Few identities

`Simplify` knows the Pythagorean identities (`sin(x)^2 + cos(x)^2 = 1` and the `sec`/`tan`,
`csc`/`cot` forms), `sin/cos = tan` and similar quotients, and inverse pairs such as
`ln(exp(x)) = x`. It does not apply double-angle formulas or logarithm rules:

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
`SolveNumerically` for numeric solutions in a range. A symbolic solver is planned for 1.1 (see
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

## Robustness

- **⚠ Very deep expressions can still overflow the stack in `Simplify` and `Differentiate`.**
  `Parse` rejects input nested more than 256 levels deep or with a tree more than 500 levels
  deep, and whatever it accepts can be evaluated and printed safely on a 1 MB stack (the
  default for threads on Windows). `Simplify` and `Differentiate` build deeper trees than
  their input, so on input near those limits — or on trees built in code, which aren't
  checked — they can still throw `StackOverflowException`, which terminates the process and
  can't be caught. If you simplify or differentiate **untrusted input**, keep it well below
  the limits or run on a thread with a larger stack.

## Not in the package

The repository also contains `Epsilon.Calculus` (integration, gradients, Hessians) and
`Epsilon.LinearAlgebra` (matrices). They are experimental and not part of the `Epsilon`
package; their APIs will change before they are published.
