# Getting started

## Install

```bash
dotnet add package Epsilon
```

The package requires .NET 10 and has no dependencies. Everything lives in one namespace:

```csharp
using Epsilon.Core;
```

## A first example

A ball is thrown upwards at 20 m/s. With g ≈ 10 m/s², its height after `t` seconds is
`20t - 5t^2`. Let's ask Epsilon about it.

**Parse the formula.** Passing `"t"` declares the variable, so a typo such as `20r` is an
error instead of a new variable:

```csharp
Expr h = ExprParser.Parse("20t - 5t^2", "t");

h.Print();        // -5t^2 + 20t
h.Evaluate(1);    // 15
```

**Differentiate** to get the velocity, and find when it is zero — the top of the flight:

```csharp
Expr v = h.Differentiate("t");

v.Print();              // -10t + 20
v.FindRealRoots();      // [2]
h.Evaluate(2);          // 20
```

**Find when the ball lands** — the roots of the height — either numerically or exactly by
factoring:

```csharp
h.FindRealRoots(-10, 10);                // [0, 4]
h.TryFactorReal("t").Factored.Print();   // -5 * (t - 4) * t
```

**Write it as LaTeX** for a report:

```csharp
h.ToLatex();   // -5t^{2} + 20t
```

## Building expressions in code

Instead of parsing text you can build expressions with C# operators. Use `Pow` for powers
(`^` is XOR in C#), and call `Simplify` to clean up the result — operators only build the
tree:

```csharp
var t = new Variable("t");
Expr height = 20 * t - 5 * t.Pow(2);

height.Print();                       // -5t^2 + 20t
height.Differentiate("t").Print();    // -10t + 20

(t + t).Print();                      // t + t
(t + t).Simplify().Print();           // 2t
```

## What next

- [Expressions](expressions.md) and [Parsing](parsing.md) — the two ways to create
  expressions, in detail.
- [Simplification](simplification.md) — the heart of the library, and its domain
  guarantee.
- [Root finding](root-finding.md) — solving equations numerically.
- [Known limitations](limitations.md) — before you rely on something, check it's there.
