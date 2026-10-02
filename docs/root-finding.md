# Root finding

Epsilon finds roots numerically, in `double` precision. For exact roots of polynomials use
[factoring](factoring.md); a symbolic equation solver is planned.

## All real roots in a range

```csharp
IReadOnlyList<double> FindRealRoots(this Expr expr,
    double leftLimit = -∞, double rightLimit = +∞, int scanSteps = 200)
```

returns every root found in the range, in ascending order, each to about full double
precision:

```csharp
ExprParser.Parse("x^2 - 2").FindRealRoots();        // [-1.4142135623730951, 1.414213562373095]
ExprParser.Parse("x^2 - 2").FindRealRoots(0, 10);   // [1.414213562373095]
```

It handles the cases where simple methods fail:

```csharp
ExprParser.Parse("x^2").FindRealRoots();          // [0]     touches zero without crossing
ExprParser.Parse("sqrt(x)").FindRealRoots();      // [0]     on the edge of the domain
ExprParser.Parse("ln(x)").FindRealRoots();        // [1]     undefined for x ≤ 0
ExprParser.Parse("tan(x)").FindRealRoots(-2, 2);  // [0]     poles at ±π/2 are not roots
ExprParser.Parse("x^2/x").FindRealRoots(-1, 1);   // []      undefined at 0
ExprParser.Parse("1/x").FindRealRoots();          // []
```

**Points where the expression is undefined are never reported as roots.** The expression is
simplified in [Strict mode](simplification.md#generic-and-strict-mode) first, so `x^2/x` keeps
its hole at 0.

### How it works, and its limits

The range is split into `scanSteps` intervals (200 by default). Every sign change, every point
where `|f|` touches zero and every edge of the domain is refined by bisection. An infinite
range is mapped onto a finite one first.

- Roots closer together than the grid spacing can be missed. Increase `scanSteps` or narrow
  the range.
- On an infinite range the grid is dense near 0 and sparse far away, so a periodic function
  shows only its roots near the origin — `sin(x)` gives 7 roots on (-∞, ∞). Use a finite
  range for those.
- Roots closer than `1e-6` to each other are reported once.
- ⚠ A function that underflows to exactly `0.0`, like `exp(x)` far below zero, is taken
  for a root there.

See [limitations](limitations.md#root-finding) for examples.

## Equations

Pass the right-hand side as an `Expr` to solve `left = right`:

```csharp
ExprParser.Parse("x^2").FindRealRoots(ExprParser.Parse("2x"), -10, 10);   // [0, 2]
ExprParser.Parse("cos(x)").FindRealRoots(ExprParser.Parse("x"));          // [0.7390851332151607]
```

⚠ A plain number as the right-hand side binds to the range overload instead: write
`(Expr)0.5` or `ExprParser.Parse("1/2")`, not `0.5`. See
[limitations](limitations.md#-overload-trap-a-number-as-the-right-hand-side).

## Several variables

The overloads above need exactly one variable. With more, name the one to solve for and give
values for the others:

```csharp
var a = new Dictionary<string, double> { ["a"] = 9 };
ExprParser.Parse("x^2 - a").FindRealRoots("x", a);   // [-3, 3]
```

The full signature is `FindRealRoots(variable, fixedBindings, leftLimit, rightLimit,
scanSteps)`, and there is a matching one for equations.

## Complex roots

```csharp
IReadOnlyList<ComplexNumber> FindComplexRoots(this Expr expr,
    double reMin, double reMax, double imMin, double imMax, int gridSteps = 12)
```

searches the rectangle `[reMin, reMax] × [imMin, imMax]i` by starting Newton's method from
every point of a `(gridSteps + 1)²` grid:

```csharp
ExprParser.Parse("x^3 - 1").FindComplexRoots(-2, 2, -2, 2);
// ≈ [-0.5 - 0.866i, 1, -0.5 + 0.866i]
ExprParser.Parse("exp(x)").FindComplexRoots(ExprParser.Parse("1"), -1, 1, -7, 7);
// ≈ [-6.283i, 0, 6.283i]
```

The roots come in no particular order. A root is found only if some starting point
converges to it, so there is no completeness guarantee; increase `gridSteps` for more starting
points.

Each root is reported once, a multiple root too. Roots so close that `|f| < 1e-10` on the
whole segment between them can't be told apart and are reported as one:

```csharp
ExprParser.Parse("x^2 - 2x + 1").FindComplexRoots(-2, 2, -2, 2);        // [1], up to rounding
ExprParser.Parse("(x - 1)*(x - 1.001)").FindComplexRoots(-2, 2, -2, 2); // [1, 1.001]
```

For a multiple root of an expanded polynomial, rounding limits the precision: the triple root
of `x^3 - 3x^2 + 3x - 1` comes out up to about `1e-8` away from 1. `TryFactorComplex` gives
exact roots with their multiplicity.

## A single root near a guess

`TryFindRoot` and `TryFindComplexRoot` run Newton's method from one starting point. Which
root you get depends on the guess, and the method can fail:

```csharp
ExprParser.Parse("x^2 - 2").TryFindRoot(1);    // (1.4142135623746899, true)
ExprParser.Parse("x^2 + 1").TryFindRoot(1);    // (null, false)
ExprParser.Parse("x^2 + 1").TryFindComplexRoot(new ComplexNumber(1, 1));   // (≈ i, true)
```

The result stops once `|f| < tolerance` (default `1e-10`), so it is less precise than
`FindRealRoots`. Prefer `FindRealRoots` unless you already have a good guess.

## Errors

- `ArgumentException` — a limit is NaN, or the left limit is not less than the right one.
- `ArgumentOutOfRangeException` — `scanSteps` below 2, or `gridSteps` below 1.
- `InvalidOperationException` — an overload without a variable name was used on an
  expression with several variables.
