# Factoring

```csharp
(Expr Factored, bool Success) TryFactorReal(this Expr expr, string variable)
(Expr Factored, bool Success) TryFactorComplex(this Expr expr, string variable)
```

Both factor a polynomial in one variable with rational coefficients — **exactly**. No
approximated root ever appears in the result: a factor that can't be written exactly is
kept as a polynomial.

```csharp
ExprParser.Parse("x^2 - 5x + 6").TryFactorReal("x").Factored.Print();   // (x - 2) * (x - 3)
ExprParser.Parse("x^3 - x").TryFactorReal("x").Factored.Print();        // (x + 1) * (x - 1) * x
```

The polynomial doesn't have to be written out: it is [expanded](simplification.md#expanding)
first.

```csharp
ExprParser.Parse("(x^2 - 1)*(x - 1)").TryFactorReal("x").Factored.Print();   // (x + 1) * (x - 1)^2
```

## What it finds

**Rational roots** become linear factors, with their multiplicity. A leading coefficient
other than 1 is kept in front:

```csharp
ExprParser.Parse("x^3 - 3x^2 + 3x - 1").TryFactorReal("x").Factored.Print();   // (x - 1)^3
ExprParser.Parse("2x^2 - x - 1").TryFactorReal("x").Factored.Print();          // 2 * (x + 1/2) * (x - 1)
ExprParser.Parse("0.5x^2 - 0.5").TryFactorReal("x").Factored.Print();          // (1/2) * (x + 1) * (x - 1)
```

**Quadratic factors** with rational coefficients are found even without rational roots.
`TryFactorReal` splits those with real roots using square roots:

```csharp
ExprParser.Parse("x^4 + x^2 + 1").TryFactorReal("x").Factored.Print();
// (x^2 - x + 1) * (x^2 + x + 1)
ExprParser.Parse("x^4 - 5x^2 + 6").TryFactorReal("x").Factored.Print();
// (x + sqrt(2)) * (x + sqrt(3)) * (x - sqrt(2)) * (x - sqrt(3))
ExprParser.Parse("x^2 - x - 1").TryFactorReal("x").Factored.Print();
// (x + (1/2) * sqrt(5) - 1/2) * (x - (1/2) * sqrt(5) - 1/2)
```

## Real or complex

Over the reals, a quadratic without real roots stays as it is. `TryFactorComplex` splits it
with `i`, and succeeds only if the polynomial splits completely into linear factors:

```csharp
ExprParser.Parse("x^4 - 1").TryFactorReal("x").Factored.Print();
// (x^2 + 1) * (x + 1) * (x - 1)
ExprParser.Parse("x^4 - 1").TryFactorComplex("x").Factored.Print();
// (x + 1) * (x + i) * (x - 1) * (x - i)
```

## When it fails

`Success` is `false` and `Factored` is the original expression when:

- the expression isn't a polynomial in the variable (`sin(x)`), or it's a constant;
- other variables appear in it (`x^2 - a`);
- nothing could be split off exactly (`x^3 - 2`, whose roots are cube roots).

```csharp
var (factored, success) = ExprParser.Parse("x^3 - 2").TryFactorReal("x");
success;             // false
factored.Print();    // x^3 - 2
```

For numeric roots of any expression use [root finding](root-finding.md). For what may come
later, see [limitations](limitations.md#factoring-is-exact-but-limited).
