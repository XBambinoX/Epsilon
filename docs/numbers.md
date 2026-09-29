# Numbers

## Rational

Every number in an expression is a `Rational`: an exact fraction of arbitrary-size
integers (`System.Numerics.BigInteger`), always in lowest terms with a positive denominator.
There is no rounding, however long a computation gets:

```csharp
new Rational(2, 4);                                   // 1/2
new Rational(1, -2);                                  // -1/2
new Rational(1, 3) + new Rational(1, 6);              // 1/2
new Rational(2, 3).Pow(-2);                           // 9/4
new Rational(BigInteger.Pow(10, 30), 3);              // 1000000000000000000000000000000/3
ExprParser.Parse("2^100").Simplify().Print();         // 1267650600228229401496703205376
```

It supports `+ - * /`, comparison, `Pow(int)`, `Abs`, `Floor`, `Ceiling`, `Round` (halves
away from zero), `ToDouble`, and the properties `Numerator`, `Denominator`, `Sign`,
`IsZero`, `IsOne` and `IsInteger`. A zero denominator throws `DivideByZeroException`.

### From double and text

`Rational.FromDouble` (also the explicit cast, and the implicit `double → Expr` conversion)
uses the **shortest decimal form** of the double — the value you wrote, not the binary
approximation:

```csharp
Rational.FromDouble(0.1);                   // 1/10
Rational.FromDecimalString("1.5E-3");       // 3/2000
new Constant(1.0 / 3).Value;                // 333333333333333/1000000000000000
```

A double such as `1.0 / 3` is already rounded, so its exact value is not 1/3. Write
`new Rational(1, 3)` when you mean a third. NaN and infinities throw `ArgumentException`.

`ToString` writes `3/4`, `-3/4` or `5`, the same in every culture.

## ComplexNumber

`ComplexNumber` is the result of [`EvaluateComplex`](evaluation.md#complex-values): a
complex number with `double` parts. It is named `ComplexNumber` rather than `Complex` so it
can be used next to `System.Numerics` without ambiguity.

```csharp
var z = new ComplexNumber(3, 4);

z.Magnitude;                                   // 5
z.Conjugate;                                   // 3 - 4i
new ComplexNumber(1, 2) * new ComplexNumber(3, 4);   // -5 + 10i
ComplexNumber.Sqrt(-4);                        // ≈ 2i
ComplexNumber.FromPolar(2, Math.PI / 2);       // ≈ 2i
```

- Properties: `Real`, `Imaginary`, `Magnitude`, `Phase`, `Conjugate`.
- Constants: `Zero`, `One`, `ImaginaryUnit`.
- Operators `+ - * /`, `==`; a `double` converts implicitly.
- Functions: `Exp`, `Log`, `Sqrt`, `Pow`, `Sin`, `Cos`, `Tan`, `Asin`, `Acos`, `Atan`, the
  hyperbolic functions and their inverses — all principal values.
- `Pow(0, w)`: 1 for w = 0, 0 for Re(w) > 0, ∞ for a negative real w, NaN otherwise.

## Constants in expressions

| Text | Node | Value |
|---|---|---|
| `pi`, `π` | `Pi` | π, printed as `π` |
| `e` | `EulerNumber` | e = 2.718… |
| `i` | `ImaginaryUnit` | i; only in [complex evaluation](evaluation.md#complex-values) |

They stay symbolic — `2pi` is `2 * π`, not `6.28…` — until you evaluate.
