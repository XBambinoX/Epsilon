# Symbolic integration

`Integrate("x")` gives an antiderivative: an expression whose derivative is the integrand
wherever the integrand is defined. It comes from the rules of a table of integrals, from
partial fractions, from substitutions, trigonometric ones among them, and from integration by
parts, simplified, without a constant of integration. Where none of them finds one, it throws
`NotSupportedException`.

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
| | `x/(x^2 + 1)` | `(1/2) * ln(x^2 + 1)` |
| Square roots of quadratics | `1/sqrt(1 - x^2)`, `1/sqrt(x^2 + 1)` | `asin(x)`, `asinh(x)` |
| | `sqrt(1 - x^2)` | `(1/2) * sqrt(-x^2 + 1) * x + (1/2) * asin(x)` |

The arguments may be any linear expression a x + b: the exponential, logarithm, trigonometric,
hyperbolic and inverse functions, abs and sign, and the squares, reciprocals and products of the
trigonometric and hyperbolic functions. The quadratics may have any numeric coefficients. Their
forms are 1/q, (a x + b)/q, sqrt(q), 1/sqrt(q) and (a x + b)/sqrt(q), written around the vertex
of q. One with rational coefficients is left to [partial fractions](#rational-functions) when it
has rational roots or is raised to a power. What fits no rule is tried by partial fractions, then
by [substitution](#substitution), by the substitutions for
[sin and cos](#trigonometric-and-hyperbolic-functions) and for
[square roots of quadratics](#square-roots-of-quadratics), and by partial fractions over the
roots of factors of degree 3 and more. Then it is expanded once and tried again, which
integrates products of polynomials, a rational function of exp, sinh and cosh is one of
t = exp(x), and last it is integrated [by parts](#integration-by-parts).

## Rational functions

A quotient of polynomials with rational coefficients is integrated by partial fractions: its
denominator is split into factors over the rationals, and the fraction into a polynomial and a
sum of fractions over them. A linear factor gives a logarithm, a quadratic one a logarithm and
an arctangent:

```csharp
ExprParser.Parse("x/((x - 1)*(x + 2))").Integrate("x").Print();   // (1/3) * ln(abs(x - 1)) + (2/3) * ln(abs(x + 2))
ExprParser.Parse("1/(x*(x^2 + 1))").Integrate("x").Print();      // ln(abs(x)) - (1/2) * ln(x^2 + 1)
ExprParser.Parse("1/(x^3 + 1)").Integrate("x").Print();
// atan((2x - 1) / sqrt(3)) / sqrt(3) + (1/3) * ln(abs(x + 1)) - (1/6) * ln(x^2 - x + 1)
```

Logarithms whose coefficients are equal or opposite are merged into one, of a product or a
quotient. The factors are found in an expanded denominator too, x^4 + x^2 + 1 being
(x^2 + x + 1)(x^2 - x + 1), and x^6 - 5x^3 + 6, a polynomial in x^3, (x^3 - 2)(x^3 - 3):

```csharp
ExprParser.Parse("1/(x^2 - 1)").Integrate("x").Print();     // (1/2) * ln(abs((x - 1) / (x + 1)))
ExprParser.Parse("x^2/(x^3 + 1)").Integrate("x").Print();   // (1/3) * ln(abs(x^3 + 1))
ExprParser.Parse("1/(x^4 + x^2 + 1)").Integrate("x").Print();
// atan((2x + 1) / sqrt(3)) / (2sqrt(3)) + atan((2x - 1) / sqrt(3)) / (2sqrt(3)) + (1/4) * ln((x^2 + x + 1) / (x^2 - x + 1))
```

Repeated factors give a rational part: by Hermite's reduction, one fraction over the repeated
factors to one power less. It doesn't split the factors, so in the last example the roots of
x^3 - 2 aren't needed:

```csharp
ExprParser.Parse("1/(x^2 + 1)^3").Integrate("x").Print();         // (3x^3 + 5x) / (8(x^2 + 1)^2) + (3/8) * atan(x)
ExprParser.Parse("1/(x^4 - 2*x^2 + 1)").Integrate("x").Print();   // -x / (2 * (x + 1) * (x - 1)) + (1/4) * ln(abs((x + 1) / (x - 1)))
ExprParser.Parse("x^2/(x^3 - 2)^2").Integrate("x").Print();       // -1 / (3 * (x^3 - 2))
```

A factor of degree 3 or more is integrated over its roots: each root r gives S(r) ln(x - r),
where S is the numerator over the derivative of the factor, and a pair of complex roots gives
a logarithm and an arctangent. A factor of x^n - a has roots of that binomial, the n-th roots
of a; their angles are written exactly, by values such as sqrt(2) where they are known, and
otherwise as cos(pi/5) is for x^5 - 1:

```csharp
ExprParser.Parse("1/(x^4 + 1)").Integrate("x").Print();
// (1/8) * ln((x^2 + sqrt(2) * x + 1) / (x^2 - sqrt(2) * x + 1)) * sqrt(2) + (1/4) * (atan(sqrt(2) * x + 1) + atan(sqrt(2) * x - 1)) * sqrt(2)
ExprParser.Parse("1/(x^6 + 1)").Integrate("x").Print();
// (1/6) * (atan(2x + sqrt(3)) + atan(2x - sqrt(3))) + (1/3) * atan(x) + (1/12) * ln((x^2 + sqrt(3) * x + 1) / (x^2 - sqrt(3) * x + 1)) * sqrt(3)
```

The roots of any other cubic or quartic come from Cardano's and Ferrari's formulas, three real
roots of a cubic in trigonometric form:

```csharp
ExprParser.Parse("1/(x^3 - 3*x + 1)").Integrate("x").Print();
// ln(abs(x + 2cos((1/9) * π))) * ((-2/9) * cos((1/9) * π) + (2/9) * (-2cos((1/9) * π))^2 - 4/9)
//   + ln(abs(x - 2cos((2/9) * π))) * ((2/9) * cos((2/9) * π) + (2/9) * (2cos((2/9) * π))^2 - 4/9)
//   + ln(abs(x - 2cos((4/9) * π))) * ((2/9) * cos((4/9) * π) + (2/9) * (2cos((4/9) * π))^2 - 4/9)
```

These are exact, but a root in radicals takes room, and it appears in every coefficient. With
the one real root of x^3 - x - 1 the antiderivative is a thousand characters long; its
derivative is still the integrand, here 1/(8 - 2 - 1) at 2:

```csharp
var integral = ExprParser.Parse("1/(x^3 - x - 1)").Integrate("x");

integral.Print().Length;                          // 1012
integral.Differentiate("x").Evaluate(("x", 2));   // 0.20000000000000007
```

Over the roots, partial fractions come after [substitution](#substitution), which writes
x/(1 + x^4) as atan(x^2)/2. A factor of degree 5 or more that divides no x^n - a isn't
integrated: its roots can't be written in radicals in general, see
[what isn't integrated yet](#what-isnt-integrated-yet).

## Substitution

Where the derivative of a part g of the integrand is a factor of the rest, the integral is one
in u = g, which the table may know:

```csharp
ExprParser.Parse("2*x*cos(x^2)").Integrate("x").Print();         // sin(x^2)
ExprParser.Parse("ln(x)/x").Integrate("x").Print();              // ln(x)^2 / 2
ExprParser.Parse("cos(x)*exp(sin(x))").Integrate("x").Print();   // exp(sin(x))
ExprParser.Parse("x*exp(x^2)").Integrate("x").Print();           // (1/2) * exp(x^2)
```

Powers of the part count as powers of u: x^4 is u^2 for u = x^2, and exp(2x) is u^2 for
u = exp(x). For g = p x^k + q, what is left of x in the powers of x^k is written in u as well:

```csharp
ExprParser.Parse("x/(1 + x^4)").Integrate("x").Print();              // (1/2) * atan(x^2)
ExprParser.Parse("exp(x)/(1 + exp(2*x))").Integrate("x").Print();    // atan(exp(x))
ExprParser.Parse("x^3*sqrt(x^2 + 1)").Integrate("x").Print();
// (1/5) * (x^2 + 1)^(5/2) - (1/3) * (x^2 + 1)^(3/2)
```

For a root of a linear expression, x itself is written in u: for u = sqrt(x + 1), x = u^2 - 1,
so the integral is one of a polynomial in u:

```csharp
ExprParser.Parse("x*sqrt(x + 1)").Integrate("x").Print();    // (2/5) * sqrt(x + 1)^5 - (2/3) * sqrt(x + 1)^3
ExprParser.Parse("sqrt(x)/(1 + x)").Integrate("x").Print();  // -2atan(sqrt(x)) + 2sqrt(x)
```

Powers of sin and cos are integrated by sin^2 + cos^2 = 1: an odd power keeps one factor for
du, and even powers are halved by the double angle. Powers of tan and cot are lowered by
tan^2 = sec^2 - 1, and those of sinh and cosh the same way as those of sin and cos:

```csharp
ExprParser.Parse("sin(x)^3").Integrate("x").Print();            // (1/3) * cos(x)^3 - cos(x)
ExprParser.Parse("sin(x)^2*cos(x)^2").Integrate("x").Print();   // (1/8) * x - (1/32) * sin(4x)
ExprParser.Parse("tan(x)^3").Integrate("x").Print();            // ln(abs(cos(x))) + (1/2) * sec(x)^2
```

## Trigonometric and hyperbolic functions

A rational function of sin x and cos x, tan, cot, sec and csc among them, is one of t by
Bioche's rules: t = cos x where the integrand is odd in sin x, t = sin x where it is odd in
cos x, and t = tan x where it is unchanged when both change sign. sin(k x) and cos(k x) are
written in sin x and cos x first, and the argument may be any linear expression. Where several
rules apply, the shortest result is taken:

```csharp
ExprParser.Parse("sin(x)^3/(1 + cos(x)^2)").Integrate("x").Print();    // cos(x) - 2atan(cos(x))
ExprParser.Parse("cos(x)^5/sin(x)^2").Integrate("x").Print();          // (1/3) * sin(x)^3 - 1 / sin(x) - 2sin(x)
ExprParser.Parse("1/(sin(x)^2 + 4*cos(x)^2)").Integrate("x").Print();  // (1/2) * atan(tan(x) / 2)
ExprParser.Parse("sin(2*x)/(1 + cos(x)^2)").Integrate("x").Print();    // -ln(cos(x)^2 + 1)
```

Where no rule applies, t = tan(x/2), with sin x = 2t/(1 + t^2) and cos x = (1 - t^2)/(1 + t^2):

```csharp
ExprParser.Parse("1/(2 + cos(x))").Integrate("x").Print();       // 2atan(tan(x / 2) / sqrt(3)) / sqrt(3)
ExprParser.Parse("sin(x)/(1 + sin(x))").Integrate("x").Print();  // x + 2 / (tan(x / 2) + 1)
```

These are antiderivatives between the points where tan x or tan(x/2) is undefined, and jump at
them, even where the integrand doesn't: F(b) - F(a) across such a point isn't the definite
integral. Over a whole period of 1/(2 + cos x) it is 0, where the integral is 2 pi/sqrt(3):

```csharp
var f = ExprParser.Parse("1/(2 + cos(x))");
var antiderivative = f.Integrate("x");

antiderivative.Evaluate(("x", 2 * Math.PI)) - antiderivative.Evaluate(("x", 0));   // -8.16431199431569E-17
f.Integrate(0, 2 * Math.PI);                                                         // 3.627598728468432
```

A rational function of exp, sinh and cosh is one of t = exp(x), and quotients of equal powers of
sin and cos, or sinh and cosh, are powers of tan, cot, tanh or coth:

```csharp
ExprParser.Parse("1/(2 + cosh(x))").Integrate("x").Print();
// ln(abs((exp(x) - sqrt(3) + 2) / (exp(x) + sqrt(3) + 2))) / sqrt(3)
ExprParser.Parse("cosh(x)^2/sinh(x)^2").Integrate("x").Print();   // x - coth(x)
```

## Square roots of quadratics

A rational function of x and the square root of a quadratic q = p((x - h)^2 + k) becomes one of
sin and cos, or sinh and cosh: x - h = r sin(t) for p, k < 0, r sinh(t) for p, k > 0, and
r cosh(t) for p > 0 > k, with r = sqrt(|k|). The square root is then a multiple of cos(t),
cosh(t) or sinh(t), and the result is written back in x:

```csharp
ExprParser.Parse("x^2/sqrt(1 - x^2)").Integrate("x").Print();       // -(1/2) * sqrt(-x^2 + 1) * x + (1/2) * asin(x)
ExprParser.Parse("1/(x^2*sqrt(4 + x^2))").Integrate("x").Print();   // -sqrt(x^2 + 4) / (4x)
ExprParser.Parse("x^2*sqrt(x^2 + 1)").Integrate("x").Print();
// (1/4) * x^3 * sqrt(x^2 + 1) + (1/8) * sqrt(x^2 + 1) * x - (1/8) * asinh(x)
```

For p > 0 > k, x - h is r cosh(t) on one side of the gap around h and -r cosh(t) on the other.
Both are taken at once, so the result holds on both sides:

```csharp
ExprParser.Parse("1/(x^2*sqrt(x^2 - 1))").Integrate("x").Print();   // sqrt(x^2 - 1) / x
ExprParser.Parse("sqrt(x^2 - 1)/x^2").Integrate("x").Print();       // ln(abs(x + sqrt(x^2 - 1))) - sqrt(x^2 - 1) / x
```

## Integration by parts

The integral of u dv is u v less that of v du. For u the factor whose derivative is simpler is
taken: a logarithm, an inverse function or a polynomial, in that order, and the rest is dv. A
polynomial loses a degree with each step:

```csharp
ExprParser.Parse("x*exp(x)").Integrate("x").Print();     // exp(x) * x - exp(x)
ExprParser.Parse("x^2*cos(x)").Integrate("x").Print();   // x^2 * sin(x) + 2cos(x) * x - 2sin(x)
ExprParser.Parse("x*ln(x)").Integrate("x").Print();      // (1/2) * ln(x) * x^2 - (1/4) * x^2
```

A logarithm or an inverse function alone, or a power of one, is u with dv = dx:

```csharp
ExprParser.Parse("ln(x)^2").Integrate("x").Print();       // 2x + ln(x)^2 * x - 2ln(x) * x
ExprParser.Parse("ln(x^2 + 1)").Integrate("x").Print();   // ln(x^2 + 1) * x - 2x + 2atan(x)
ExprParser.Parse("asin(x)^2").Integrate("x").Print();     // asin(x)^2 * x - 2x + 2asin(x) * (-x^2 + 1)^(1/2)
ExprParser.Parse("x*asin(x)").Integrate("x").Print();     // (1/2) * asin(x) * x^2 + (1/4) * sqrt(-x^2 + 1) * x - (1/4) * asin(x)
```

An exponential times sin or cos, and sin or cos of a logarithm, come back to the integral I
after two steps, as I = A + c I, which is solved for I:

```csharp
ExprParser.Parse("exp(x)*sin(x)").Integrate("x").Print();       // (exp(x) * sin(x) - cos(x) * exp(x)) / 2
ExprParser.Parse("exp(2*x)*cos(3*x)").Integrate("x").Print();   // (2/13) * cos(3x) * exp(2x) + (3/13) * exp(2x) * sin(3x)
ExprParser.Parse("sin(ln(x))").Integrate("x").Print();          // (sin(ln(x)) * x - cos(ln(x)) * x) / 2
```

For sinh and cosh at the rate of the exponential, c is 1 and there is nothing to solve, so they
are written in exponentials instead. Powers of sec and csc follow from their reduction formulas,
which come from integration by parts too, and a substitution and integration by parts combine:

```csharp
ExprParser.Parse("exp(x)*sinh(x)").Integrate("x").Print();   // -(1/2) * x + (1/4) * exp(2x)
ExprParser.Parse("sec(x)^3").Integrate("x").Print();         // (1/2) * ln(abs(sec(x) + tan(x))) + (1/2) * sec(x) * tan(x)
ExprParser.Parse("exp(sqrt(x))").Integrate("x").Print();     // 2exp(sqrt(x)) * sqrt(x) - 2exp(sqrt(x))
```

## Checking a result

The derivative of an antiderivative is the integrand:

```csharp
var integral = ExprParser.Parse("atan(x)").Integrate("x");

integral.Print();                    // atan(x) * x - (1/2) * ln(x^2 + 1)
integral.Differentiate("x").Print(); // atan(x)
```

The simplifier doesn't always bring a derivative back to the form of the integrand, but the
values agree. An antiderivative gives a definite integral as F(b) - F(a), here half the area
of the unit disk. This holds when the integrand is defined on all of [a, b] and the
antiderivative doesn't [jump](#trigonometric-and-hyperbolic-functions) in it, and the numeric
integral agrees:

```csharp
var area = ExprParser.Parse("sqrt(1 - x^2)").Integrate("x");

area.Evaluate(("x", 1)) - area.Evaluate(("x", -1));          // 1.5707963267948966
ExprParser.Parse("sqrt(1 - x^2)").Integrate(-1, 1);           // 1.570796326794897
```

## What isn't integrated yet

Some integrands need what isn't there yet, others have no antiderivative made of elementary
functions at all. Both throw:

```csharp
ExprParser.Parse("exp(x)*cos(x)^2").Integrate("x");
// NotSupportedException: No antiderivative of exp(x) * cos(x)^2 by x was found.
```

| Integrand | Needs |
|---|---|
| `exp(x)*cos(x)^2` | rewriting first by the double angle, which is done only in rational functions of sin and cos |
| `1/(x^2 + a^2)` | the sign of a quadratic with a symbol in it |
| `1/(x^5 - x - 1)` | the roots of a factor of degree 5, which radicals don't write |
| `exp(x^2)`, `x*tan(x)` | nothing: they have no elementary antiderivative |
| `sqrt(1 + x^3)`, `1/sqrt(x^4 + 1)` | nothing either: these square roots of a cubic and a quartic give elliptic integrals |

`exp(x^2)` has no antiderivative made of elementary functions, but its definite integrals are
[numbers](numeric-integration.md) all the same:

```csharp
ExprParser.Parse("exp(x^2)").Integrate(0, 1);   // 1.4626517459071819
```
