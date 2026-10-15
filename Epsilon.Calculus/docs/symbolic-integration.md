# Symbolic integration

`Integrate("x")` gives an antiderivative: an expression whose derivative is the integrand
wherever the integrand is defined. It comes from the rules of a table of integrals, simplified,
without a constant of integration. Where no rule fits, it throws `NotSupportedException`.

## Antiderivatives

```csharp
ExprParser.Parse("x^2").Integrate("x").Print();   // x^3 / 3
ExprParser.Parse("t^2").Integrate().Print();      // t^3 / 3
```

Without a name the integral is by the only variable of the expression. Other variables are
constants:

```csharp
ExprParser.Parse("sin(a*x)").Integrate("x").Print();   // -cos(a * x) / a
ExprParser.Parse("x*y").Integrate("y").Print();        // y^2 * x / 2
```

## Exact results

Exponents stay exact fractions, 1/x gives the logarithm of the absolute value, valid on both
sides of 0, and the integral of a real root is real wherever the root is:

```csharp
ExprParser.Parse("sqrt(x)").Integrate("x").Print();         // 2x^(3/2) / 3
ExprParser.Parse("1/x").Integrate("x").Print();             // ln(abs(x))
ExprParser.Parse("nthroot(x, 3)").Integrate("x").Print();   // 3nthroot(x, 3) * x / 4

ExprParser.Parse("nthroot(x, 3)").Integrate("x").Evaluate(("x", -8));   // 12
```

The cube root of -8 is -2, and x^(4/3) would be NaN there.

## The table

| Form | Integrand | Antiderivative |
|---|---|---|
| Powers of a linear expression | `(2*x + 1)^5` | `(2x + 1)^6 / 12` |
| Exponentials | `exp(-3*x)`, `2^x` | `-exp(-3x) / 3`, `2^x / ln(2)` |
| Logarithms | `ln(x)` | `ln(x) * x - x` |
| Trigonometric functions | `cos(3*x + 1)`, `tan(x)` | `sin(3x + 1) / 3`, `-ln(abs(cos(x)))` |
| | `sec(x)` | `ln(abs(sec(x) + tan(x)))` |
| Inverse functions | `atan(x)` | `atan(x) * x - (1/2) * ln(x^2 + 1)` |
| abs and sign | `abs(x)` | `abs(x) * x / 2` |
| Squares and products | `sin(x)^2` | `(1/2) * x - (1/4) * sin(2x)` |
| | `sec(x)*tan(x)`, `sin(x)*cos(x)` | `sec(x)`, `-cos(2x) / 4` |
| Polynomials | `x*(x + 1)` | `(1/3) * x^3 + (1/2) * x^2` |
| Polynomial division | `x^3/(x - 1)` | `(1/3) * x^3 + (1/2) * x^2 + x + ln(abs(x - 1))` |
| | `x^2/(x^2 + 1)` | `x - atan(x)` |
| Quadratic denominators | `1/(1 + x^2)`, `1/(x^2 + 2*x + 5)` | `atan(x)`, `atan((x + 1) / 2) / 2` |
| | `1/(x^2 - 1)` | `ln(abs((x - 1) / (x + 1))) / 2` |
| | `x/(x^2 + 1)` | `(1/2) * ln(x^2 + 1)` |
| Square roots of quadratics | `1/sqrt(1 - x^2)`, `1/sqrt(x^2 + 1)` | `asin(x)`, `asinh(x)` |
| | `sqrt(1 - x^2)` | `(1/2) * sqrt(-x^2 + 1) * x + (1/2) * asin(x)` |

The arguments may be any linear expression a x + b: the exponential, logarithm, trigonometric,
hyperbolic and inverse functions, abs and sign, and the squares, reciprocals and products of the
trigonometric and hyperbolic functions. The quadratics may have any numeric coefficients. Their
forms are 1/q, (a x + b)/q, sqrt(q), 1/sqrt(q) and (a x + b)/sqrt(q), written around the vertex
of q. What fits no rule is expanded once and tried again, which integrates products of
polynomials.

## Checking a result

The derivative of an antiderivative is the integrand:

```csharp
var integral = ExprParser.Parse("atan(x)").Integrate("x");

integral.Print();                    // atan(x) * x - (1/2) * ln(x^2 + 1)
integral.Differentiate("x").Print(); // atan(x)
```

The simplifier doesn't always bring a derivative back to the form of the integrand, but the
values agree. An antiderivative gives a definite integral as F(b) - F(a), here half the area
of the unit disk. This holds when the integrand is defined on all of [a, b], and the numeric
integral agrees:

```csharp
var area = ExprParser.Parse("sqrt(1 - x^2)").Integrate("x");

area.Evaluate(("x", 1)) - area.Evaluate(("x", -1));          // 1.5707963267948966
ExprParser.Parse("sqrt(1 - x^2)").Integrate(-1, 1);           // 1.570796326794897
```

## What isn't integrated yet

Integration by substitution and by parts is planned, and so are partial fractions of higher
degree. Until then these throw:

```csharp
ExprParser.Parse("x*exp(x)").Integrate("x");
// NotSupportedException: No rule of the table integrates exp(x) * x by x.
```

| Integrand | Needs |
|---|---|
| `x*exp(x)` | integration by parts |
| `2*x*cos(x^2)`, `ln(x)/x` | substitution |
| `sin(x)^3` | substitution or a reduction formula |
| `1/(x^3 + 1)` | partial fractions with a denominator of degree 3 |
| `1/(x^2 + a^2)` | the sign of a quadratic with a symbol in it |
| `exp(x^2)` | nothing: it has no elementary antiderivative |

`exp(x^2)` has no antiderivative made of elementary functions, but its definite integrals are
[numbers](numeric-integration.md) all the same:

```csharp
ExprParser.Parse("exp(x^2)").Integrate(0, 1);   // 1.4626517459071819
```
