# Assumptions

Many useful rewrites are valid only under conditions: `sqrt(x^2) = x` needs `x ≥ 0`,
`abs(x) = -x` needs `x ≤ 0`. By default [`Simplify`](simplification.md) knows nothing
about your variables, so it doesn't apply them. `Assumptions` tells it what you know.

```csharp
var a = Assumptions.None.AssumePositive("x");

ExprParser.Parse("sqrt(x^2)").Simplify(a).Print();   // x
```

## Building a set

`Assumptions` is immutable, like `Expr`. Start from `Assumptions.None` and chain calls;
each returns a new set:

```csharp
var a = Assumptions.None
    .AssumePositive("x")
    .AssumeNegative("y")
    .AssumeInteger("n");
```

| Method | Meaning |
|---|---|
| `AssumePositive(v)` | v > 0 |
| `AssumeNegative(v)` | v < 0 |
| `AssumeNonZero(v)` | v ≠ 0 |
| `AssumeNonNegative(v)` | v ≥ 0 |
| `AssumeNonPositive(v)` | v ≤ 0 |
| `AssumeReal(v)` | v is real |
| `AssumeRational(v)` | v is rational (and real) |
| `AssumeInteger(v)` | v is an integer (and rational, real) |
| `AssumeNatural(v)` | v is 1, 2, 3, … (and positive) |
| `Assume(v, domain, signing)` | any combination of `NumberDomain` flags and a `Signing` |

## Combining and contradictions

Assuming something about a variable that already has an assumption **narrows** it — both
must hold:

```csharp
Assumptions.None.AssumeNonNegative("x").AssumeNonZero("x").SigningOf("x");       // Positive
Assumptions.None.AssumeNonNegative("x").AssumeNonPositive("x").SigningOf("x");   // Zero
Assumptions.None.AssumeNatural("n").IsPositive("n");                              // true
```

Assumptions that can't both hold throw `ArgumentException` naming the variable:

```csharp
Assumptions.None.AssumePositive("x").AssumeNegative("x");
// ArgumentException: Variable 'x': Contradictory assumptions: Positive and Negative
// cannot both hold. (Parameter 'variable')
```

## What they unlock

```csharp
var neg = Assumptions.None.AssumeNegative("x");
ExprParser.Parse("abs(x)").Simplify(neg).Print();                     // -x

var pos = Assumptions.None.AssumePositive("x");
ExprParser.Parse("sign(x)").Simplify(pos).Print();                    // 1
ExprParser.Parse("0^x").Simplify(pos).Print();                        // 0
ExprParser.Parse("(x^2)^(1/2)").Simplify(pos).Print();                // x

var xy = Assumptions.None.AssumePositive("x").AssumeNegative("y");
ExprParser.Parse("sqrt(x^2)*sqrt(y^2)").Simplify(xy).Print();         // -x * y
```

In [Strict mode](simplification.md#generic-and-strict-mode) they also allow the rewrites
that would otherwise enlarge the domain — `x/x = 1` with `x ≠ 0`, `exp(ln(x)) = x` with
`x > 0`:

```csharp
ExprParser.Parse("exp(ln(x))").Simplify(pos, SimplifyMode.Strict).Print();   // x
```

## Reasoning about whole expressions

The simplifier asks questions about sub-expressions, not just variables. You can ask them
too, with the extension methods `IsProvablyPositive`, `IsProvablyNegative`,
`IsProvablyNonZero`, `IsProvablyNonNegative` and `IsProvablyNonPositive`:

```csharp
ExprParser.Parse("x^2 + 1").IsProvablyPositive(Assumptions.None);   // true

var both = Assumptions.None.AssumePositive("x").AssumePositive("y");
ExprParser.Parse("x + y").IsProvablyPositive(both);   // true
ExprParser.Parse("x - y").IsProvablyPositive(both);   // false
```

Some facts need no assumptions at all — squares are non-negative, `exp` is positive:

```csharp
ExprParser.Parse("abs(x^2 + 1)").Simplify().Print();   // x^2 + 1
ExprParser.Parse("sign(exp(x))").Simplify().Print();   // 1
```

The analysis is conservative: **`false` means "not proven", not "false"**. `x - y` may well
be positive; it just can't be shown from what is known.
