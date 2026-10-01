# Parsing

`ExprParser.Parse` turns text into an [`Expr`](expressions.md):

```csharp
Expr f = ExprParser.Parse("2x^2 + sin(x) - 1", "x");
```

The first argument is the formula, the optional rest are the variable names. The result is
canonical (terms in a fixed order) but **not simplified**:

```csharp
ExprParser.Parse("1 + x").Print();   // x + 1
ExprParser.Parse("x + x").Print();   // x + x
```

## Syntax

| Kind | Syntax |
|---|---|
| Operators | `+  -  *  /  ^`, parentheses |
| Absolute value | `\|x - 1\|`, same as `abs(x - 1)` |
| Implicit multiplication | `2x`, `2(x + 1)`, `x y`, `2sin(x)` |
| Numbers | `12`, `1.5`, `.5`, `1e-5`, `2.5E3` |
| Constants | `pi` or `π`, `e`, `i` |
| Trigonometric | `sin cos tan cot sec csc`, `asin acos atan` |
| Hyperbolic | `sinh cosh tanh coth sech csch`, `asinh acosh atanh` |
| Other functions | `exp ln sqrt abs sign floor ceiling round` |
| Two arguments | `min(a, b)`, `max(a, b)`, `log(x, base)`, `nthroot(x, n)` |

Numbers are read exactly, as [rationals](numbers.md), and always use a decimal **point** —
the parser ignores the current culture:

```csharp
ExprParser.Parse("1e-5").Print();    // 1/100000
ExprParser.Parse("2.5E3").Print();   // 2500
```

`log(x, b)` is shorthand for `ln(x) / ln(b)`:

```csharp
ExprParser.Parse("log(8, 2)").Print();      // ln(8) / ln(2)
ExprParser.Parse("log(8, 2)").Evaluate(0);  // 3
```

Function arguments always need parentheses: `sinx` is an error, not `sin(x)`.

## Precedence

From loosest to tightest:

1. `+` and `-`
2. `*`, `/` and implicit multiplication, left to right
3. unary minus
4. `^`, right-associative

So `-x^2` is `-(x^2)`, `2^3^2` is `2^9`, and implicit multiplication binds exactly like
`*` — `x/2y` is `(x/2)*y`, not `x/(2y)`:

```csharp
ExprParser.Parse("-x^2").Evaluate(3);     // -9
ExprParser.Parse("2^3^2").Evaluate(0);    // 512
ExprParser.Parse("x/2y").Print();         // (x / 2) * y
```

Write `x/(2y)` when you mean it.

## Variables

**Without variable names**, every letter that isn't part of a function or constant name is
its own variable, and letters next to each other are multiplied:

```csharp
ExprParser.Parse("xy").Print();   // x * y
ExprParser.Parse("ab").Print();   // a * b
```

That is convenient for short formulas, but a longer word silently becomes a product — and
`e` and `i` inside it are constants:

```csharp
ExprParser.Parse("theta").Print();   // e * a * h * t * t
```

**With variable names**, multi-letter names work, only the listed names are variables, and
anything else is an error that names the position. This catches typos:

```csharp
ExprParser.Parse("theta^2 + 2t", "theta", "t").Print();   // theta^2 + 2t
ExprParser.Parse("sen(x)", "x");
// FormatException: Unknown identifier 'sen' at position 0. Declared variables: x.
```

The longest match wins, so a variable named `second` is not read as `sec` + `ond`.
Variable names consist of letters only — `v0` or `x_1` can't be declared.

**Pass variable names whenever the input comes from a user.**

## Errors

Invalid input throws `FormatException` with a message saying what is wrong:

| Input | Message |
|---|---|
| `2 3` | Missing operator between numbers '2' and '3'. |
| `1,5` | Unexpected token ','. |
| `x # 2` | Unexpected character '#' at position 2. |
| `(x+1` | Expected closing ')'. |
| `x +` | Unexpected end of expression. |
| `sinx` | Expected '(' after function name 'sin'. |
| `sin(x, y)` | Function 'sin' takes exactly 1 argument. |
| `min(x)` | min requires exactly 2 arguments: min(a, b). |
| 257 nested `(` | Expression is nested too deeply (at most 256 levels of parentheses, functions, signs and powers). |
| `x + x + ...` with 501 terms | Expression is too deep (at most 500 levels); split it into smaller parts. |

## Round trip

[`Print`](output.md) writes text that `Parse` reads back into the same tree, in any culture:

```csharp
Expr e = ExprParser.Parse("x^(1/2) + (-2)^x - sin(x)/3");
ExprParser.Parse(e.Print()).Equals(e);   // true
```

## Untrusted input

`Parse` rejects input that is too deep with a `FormatException` instead of overflowing the
stack:

- more than 256 levels of nesting — parentheses, functions, signs and powers
  (`((((x))))`, `sin(sin(x))`, `--x`, `x^x^x`);
- a tree more than 500 levels deep, such as a chain of more than 500 terms (`x + x + ... + x`).

Anything `Parse` accepts can be evaluated and printed even on a 1 MB stack. `Simplify` and
`Differentiate` on input near these limits can still overflow it; see
[limitations](limitations.md#robustness-and-performance).
