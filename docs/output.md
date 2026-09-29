# Output

An expression can be written out in three ways.

| Method | Output | For |
|---|---|---|
| `Print()` | `3x^2 - 4x + 1` | people, and reading back with `Parse` |
| `ToLatex()` | `3x^{2} - 4x + 1` | documents, web pages (MathJax, KaTeX) |
| `ToString()` | `(((3 * (x ^ 2)) - (4 * x)) + 1)` | debugging: the exact tree |

## Print

`Print` writes compact math with the fewest parentheses needed:

```csharp
ExprParser.Parse("1 - 4x + 3x^2").Simplify().Print();   // 3x^2 - 4x + 1
ExprParser.Parse("x^2/2 + sin(x)").Print();             // x^2 / 2 + sin(x)
ExprParser.Parse("sqrt(x) + |y|").Print();              // abs(y) + sqrt(x)
ExprParser.Parse("2pi").Print();                        // 2 * π
```

- **Terms are ordered by descending degree**, constants last.
- A number is written right before a single variable or function (`3x^2`, `2cos(x) * y`);
  anything else keeps an explicit `*` (`3 * 2^x`, `2 * π`).
- Negative and fractional bases and exponents are parenthesized: `(-2)^x`, `x^(1 / 2)`.
- Numbers are exact rationals, like `1/3`, and are written the same in every culture.

The output of `Print` can always be read back by [`Parse`](parsing.md) into the same
tree.

## ToLatex

```csharp
ExprParser.Parse("x^2/2 + sin(x)").ToLatex();   // \frac{x^{2}}{2} + \sin\left(x\right)
ExprParser.Parse("sqrt(x + 1)").ToLatex();      // \sqrt{x + 1}
ExprParser.Parse("nthroot(x, 3)").ToLatex();    // \sqrt[3]{x}
ExprParser.Parse("|x|").ToLatex();              // \left|x\right|
ExprParser.Parse("exp(x)").ToLatex();           // e^{x}
ExprParser.Parse("asin(x)").ToLatex();          // \arcsin\left(x\right)
ExprParser.Parse("(-2)^x").ToLatex();           // \left(-2\right)^{x}
```

The term order is the same as in `Print`. Functions without a standard LaTeX command are
written with `\operatorname`, e.g. `\operatorname{round}`; `sign` becomes `\operatorname{sgn}`.

## ToString

`ToString` parenthesizes every operation and keeps the tree's own order, so you can see
exactly how an expression is built:

```csharp
ExprParser.Parse("3x^2 - 4x + 1").ToString();   // (((3 * (x ^ 2)) - (4 * x)) + 1)
```

It is meant for debugging; use `Print` for anything a person reads.
