# Differentiation

```csharp
Expr Differentiate(string variable)
Expr Differentiate()   // for an expression with at most one variable
```

`Differentiate` returns the symbolic derivative, already [simplified](simplification.md)
in the default Generic mode:

```csharp
ExprParser.Parse("sin(x^2)").Differentiate("x").Print();     // 2cos(x^2) * x
ExprParser.Parse("x/(x + 1)").Differentiate("x").Print();    // 1 / (x + 1)^2
ExprParser.Parse("x^x").Differentiate("x").Print();          // (ln(x) + 1) * x^x
```

## Partial and higher derivatives

With several variables, the others are treated as constants. Chain calls for higher and
mixed derivatives:

```csharp
Expr f = ExprParser.Parse("x^2*y^3");

f.Differentiate("x").Differentiate("y").Print();                // 6y^2 * x
ExprParser.Parse("x^4").Differentiate("x").Differentiate("x").Print();   // 12x^2
```

The derivative with respect to a variable the expression doesn't contain is exactly `0`.

## Without a variable name

`Differentiate()` uses the expression's only variable, returns `0` for a constant
expression, and throws for two or more variables:

```csharp
ExprParser.Parse("x^3").Differentiate().Print();   // 3x^2
ExprParser.Parse("2pi").Differentiate().Print();   // 0
ExprParser.Parse("x*y").Differentiate();
// InvalidOperationException: Expected exactly 1 variable, found 2: [x, y]. Use the
// explicit-variable overload for multivariable expressions.
```

## Supported functions

| Function | Derivative |
|---|---|
| `x^n`, `a^x`, `u^v` | power rule, exponential rule, and the general rule for `u^v` |
| `exp`, `ln`, `log(x, b)` | `exp(x)`, `1 / x`, `1 / (ln(b) * x)` |
| `sqrt`, `nthroot(x, n)` | `1 / (2sqrt(x))`, `1 / (n * nthroot(x, n)^(n-1))` for constant `n` |
| `sin cos tan cot sec csc` | the usual formulas, e.g. `tan → 1 / cos(x)^2` |
| `asin acos atan` | e.g. `atan → 1 / (x^2 + 1)` |
| all hyperbolic functions and their inverses | e.g. `sinh → cosh(x)` |
| `abs` | `x / abs(x)` — undefined at 0, like the true derivative |

```csharp
ExprParser.Parse("sqrt(x)").Differentiate("x").Print();        // 1 / (2sqrt(x))
ExprParser.Parse("log(x, 2)").Differentiate("x").Print();      // 1 / (ln(2) * x)
ExprParser.Parse("nthroot(x, 3)").Differentiate("x").Print();  // 1 / (3nthroot(x, 3)^2)
```

The derivatives of `floor`, `ceiling`, `round`, `sign`, `min`, `max`, and of `nthroot` with a
degree that depends on the variable, need piecewise expressions and throw
`NotSupportedException` for now — see [limitations](limitations.md#differentiation).

## Example: a tangent line

The tangent to `f` at `a` is `f(a) + f'(a)(x - a)`:

```csharp
Expr f = ExprParser.Parse("x^2", "x");
Expr df = f.Differentiate("x");

double a = 3;
Expr tangent = f.Evaluate(a) + df.Evaluate(a) * (new Variable("x") - a);
tangent.Simplify().Print();   // 6 * (x - 3) + 9
tangent.Evaluate(4);          // 15
```

`Simplify` doesn't multiply out brackets yet, so the result stays in point-slope form.
