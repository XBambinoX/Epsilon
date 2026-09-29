# Evaluation

## Real values

```csharp
double Evaluate(IReadOnlyDictionary<string, double> bindings)
double Evaluate(params (string Name, double Value)[] bindings)
double Evaluate(double x)   // for an expression with at most one variable
```

```csharp
Expr f = ExprParser.Parse("x*y");

f.Evaluate(new Dictionary<string, double> { ["x"] = 2, ["y"] = 5 });   // 10
f.Evaluate(("x", 2), ("y", 5));                                         // 10
ExprParser.Parse("x^2").Evaluate(3);                                     // 9
ExprParser.Parse("2pi").Evaluate(0);                                     // 6.283185307179586
```

The single-value overload ignores its argument when the expression has no variables.
Evaluation is plain `double` arithmetic over the tree, with no simplification, so it is
fast: simplify once, then evaluate as often as you need.

A missing value throws `ArgumentException`; the single-value overload on an expression with
two or more variables throws `InvalidOperationException`:

```csharp
f.Evaluate(("x", 2));
// ArgumentException: No binding provided for variable 'y'.
```

### Undefined points

Where an expression is undefined over the reals, `Evaluate` returns NaN or an infinity, by
the usual IEEE rules — it doesn't throw:

```csharp
ExprParser.Parse("sqrt(x)").Evaluate(-1);   // NaN
ExprParser.Parse("asin(x)").Evaluate(2);    // NaN
ExprParser.Parse("ln(x)").Evaluate(0);      // -∞
ExprParser.Parse("1/x").Evaluate(0);        // ∞
ExprParser.Parse("x/x").Evaluate(0);        // NaN
```

The constant `i` has no real value: evaluating an expression that contains it throws
`InvalidOperationException`. Use `EvaluateComplex`.

### Conventions

| Function | Convention |
|---|---|
| `x^y` | principal value: a negative base with a non-integer exponent is NaN |
| `nthroot(x, n)` | real root: for odd `n`, `nthroot(-8, 3) = -2` |
| `round` | halves away from zero: `round(2.5) = 3`, `round(-2.5) = -3` |
| `floor`, `ceiling` | `floor(-2.5) = -3`, `ceiling(-2.5) = -2` |
| `sign` | -1, 0 or 1 |
| `asin`, `acos`, `atan` | results in radians, in [-π/2, π/2], [0, π], (-π/2, π/2) |
| trigonometric functions | arguments in radians |

```csharp
ExprParser.Parse("x^(1/3)").Evaluate(-8);         // NaN
ExprParser.Parse("nthroot(x, 3)").Evaluate(-8);   // -2
ExprParser.Parse("round(x)").Evaluate(-2.5);      // -3
```

`Simplify` follows the same conventions, so simplifying never changes a value.

## Complex values

```csharp
ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings)
ComplexNumber EvaluateComplex(params (string Name, ComplexNumber Value)[] bindings)
ComplexNumber EvaluateComplex(ComplexNumber x)
```

`EvaluateComplex` works over the complex numbers, with principal branches for the
multivalued functions. A `double` converts to `ComplexNumber` implicitly:

```csharp
ExprParser.Parse("sqrt(x)").EvaluateComplex(-4);   // ≈ 2i
ExprParser.Parse("ln(x)").EvaluateComplex(-1);     // ≈ 3.141592653589793i
ExprParser.Parse("asin(x)").EvaluateComplex(2);    // ≈ 1.5707963267948966 - 1.3169578969248166i
ExprParser.Parse("e^(i*pi)").EvaluateComplex(0);   // ≈ -1
ExprParser.Parse("abs(x)").EvaluateComplex(new ComplexNumber(3, 4));   // 5

var i = ComplexNumber.ImaginaryUnit;
ExprParser.Parse("x*y").EvaluateComplex(("x", i), ("y", i));   // -1
```

Results are `double`-precision, so expect rounding such as `-1 + 1.2E-16i` for `e^(iπ)`.
`nthroot` keeps its real-root convention (`nthroot(-8, 3) = -2`).

`floor`, `ceiling`, `round`, `sign`, `min` and `max` have no complex evaluation and throw
`NotSupportedException` — see [limitations](limitations.md#complex-numbers).
