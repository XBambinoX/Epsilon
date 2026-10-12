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

## Compiled evaluation

To evaluate the same expression many times, as for a plot, a scan or a simulation step,
compile it once into a delegate:

```csharp
Func<double, double> Compile()   // for an expression with at most one variable
Func<double, double> Compile(string x)
Func<double, double, double> Compile(string x, string y)
Func<double, double, double, double> Compile(string x, string y, string z)
Func<double[], double> Compile(IReadOnlyList<string> variables)
```

```csharp
Func<double, double> f = ExprParser.Parse("x^2 * sin(x)").Compile();
f(2);                                                   // 3.637189707302727

var g = ExprParser.Parse("x^2 + y^2").Compile("x", "y");
g(3, 4);                                                // 25

var h = ExprParser.Parse("a*b - c/d").Compile(["a", "b", "c", "d"]);
h([1, 2, 3, 4]);                                        // 1.25
```

The delegate returns exactly what `Evaluate` returns, bit for bit, NaN and infinities
included. Like `Evaluate` it doesn't simplify: simplify first, then compile. The values come
in the order of the names you give; a name the expression doesn't use is allowed.

Compiling takes about 0.1 ms, so it pays off after a few hundred calls. A call is then about
5 times as fast as `Evaluate` for an expression full of functions such as `sin` and `exp`,
whose own cost stays, and up to 30 times as fast for plain arithmetic. A subexpression that
occurs more than once is computed once. The delegate can be called from many threads at once.

### Several expressions at once

`Expr.Compile` turns several expressions of the same variables into one delegate that writes
their values into an array, such as the entries of a gradient:

```csharp
static Action<double[], double[]> Compile(IReadOnlyList<Expr> expressions, IReadOnlyList<string> variables)
```

```csharp
Expr f = ExprParser.Parse("x^2 * y + sin(x*y)");
var gradient = Expr.Compile([f.Differentiate("x"), f.Differentiate("y")], ["x", "y"]);

var results = new double[2];
gradient([1, 2], results);   // results: about [3.1677, 0.5839]
```

`cos(x*y)`, which both derivatives contain, is computed once. All values are read before the
first result is written, so `results` may be the array of values itself.

### Errors

Mistakes show up when compiling, not at every call:

```csharp
ExprParser.Parse("x*y").Compile("x");
// ArgumentException: The expression has the variable 'y', which is not in the list of variables [x].
```

`Compile()` throws `InvalidOperationException` for an expression with two or more
variables, like `Evaluate(double x)`; every overload throws it for `i`. The delegates that
take arrays throw `ArgumentException` when an array doesn't have one entry per variable or
per expression.

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
