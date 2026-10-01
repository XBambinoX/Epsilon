# API reference

Every public type and member of the `Epsilon` package, version 1.0.0, grouped by topic. All
types are in the namespace `Epsilon.Core`. Each entry has a one-line description; the linked
guides explain the details, and your IDE shows the full XML documentation for every member.

A test (`ApiReferenceDocTests`) checks that every public type and member is listed here.

**Contents:**
[Parsing](#parsing) ·
[Expr](#expr) ·
[Simplification](#simplification) ·
[Assumptions](#assumptions) ·
[Factoring](#factoring) ·
[Root finding](#root-finding) ·
[Output](#output) ·
[Node types](#node-types) ·
[Rational](#rational) ·
[ComplexNumber](#complexnumber)

---

## Parsing

### `ExprParser` (static class)

| Member | Description |
|---|---|
| `Expr Parse(string input, params string[] variableNames)` | Parses text into a canonical, unsimplified expression. With `variableNames`, only those names are variables and multi-letter names are allowed; without them, every other single letter is a variable. Throws `FormatException` on invalid input. |

Guide: [Parsing](parsing.md).

---

## Expr

### `Expr` (abstract class)

The immutable expression tree that everything else works on. Guide: [Expressions](expressions.md).

**Evaluation** — guide: [Evaluation](evaluation.md)

| Member | Description |
|---|---|
| `double Evaluate(IReadOnlyDictionary<string, double> bindings)` | Real value with the given variable values; NaN or ±∞ where undefined. Throws `ArgumentException` for a missing variable. |
| `double Evaluate(params (string Name, double Value)[] bindings)` | The same, with values as pairs: `Evaluate(("x", 1), ("y", 2))`. |
| `double Evaluate(double x)` | Value of an expression with at most one variable; the argument is ignored for a constant expression. |
| `ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings)` | Complex value, principal branches. Throws `NotSupportedException` for `floor`, `ceiling`, `round`, `sign`, `min`, `max`. |
| `ComplexNumber EvaluateComplex(params (string Name, ComplexNumber Value)[] bindings)` | The same, with values as pairs. |
| `ComplexNumber EvaluateComplex(ComplexNumber x)` | Complex value of an expression with at most one variable. |

**Differentiation** — guide: [Differentiation](differentiation.md)

| Member | Description |
|---|---|
| `Expr Differentiate(string variable)` | Simplified partial derivative with respect to `variable`; `0` if the expression doesn't depend on it. |
| `Expr Differentiate()` | Derivative with respect to the only variable; `0` for a constant expression. |

**Variables and substitution**

| Member | Description |
|---|---|
| `IReadOnlySet<string> GetVariables()` | Names of all variables in the expression. |
| `bool DependsOn(string variable)` | Whether the variable occurs in the expression. |
| `string GetSingleVariable()` | The only variable's name; throws `InvalidOperationException` unless there is exactly one. |
| `Expr Substitute(string variable, Expr replacement)` | Replaces every occurrence of the variable; the result is not simplified. |

**Tree structure**

| Member | Description |
|---|---|
| `ImmutableArray<Expr> Children` | Direct sub-expressions in a fixed order; empty for leaves. |
| `Expr WithChildren(IReadOnlyList<Expr> children)` | A node of the same type with other children; returns the same node if nothing changed. |
| `Expr MapChildren(Func<Expr, Expr> map)` | Applies `map` to every child and rebuilds the node only if a child changed. |

**Building**

| Member | Description |
|---|---|
| `+ - * /` (binary), `-` (unary) | Build `Add`, `Subtract`, `Multiply`, `Divide`, `Negate` nodes, without simplifying. |
| `Expr Pow(Expr exponent)` | Builds a `Power` node: `x.Pow(2)` is `x^2`. |
| implicit from `int`, `long`, `Rational` | An exact `Constant`. |
| implicit from `double` | A `Constant` from the shortest decimal form (`0.1` → `1/10`); throws `ArgumentException` for NaN and infinities. |

**Equality**

| Member | Description |
|---|---|
| `bool Equals(Expr other)` | Structural equality: same node types, values and children in the same order. |
| `int GetHashCode()` | Consistent with `Equals`. Computed once per node and kept. |
| `string ToString()` | The tree with every operation parenthesized, for debugging. Use `Print` for display. |

**For custom node types** (protected) — see [Custom node types](expressions.md#custom-node-types)

| Member | Description |
|---|---|
| `abstract Expr DifferentiateCore(string variable)` | The node's own derivative, unsimplified. Called only when the node depends on the variable. |
| `static Expr DerivativeOf(Expr child, string variable)` | The unsimplified derivative of a child, for use in `DifferentiateCore`. |
| `virtual bool PayloadEquals(Expr other)` | Compares data stored besides the children, such as `Constant.Value`. |
| `virtual int PayloadHashCode()` | Hash of that data, which must not change after the node is built: the hash is kept. |
| `virtual int ComparePayload(Expr other)` | Orders that data, for the canonical order. |
| `static ImmutableArray<Expr> NoChildren` | The `Children` of a leaf. |
| `Expr WithNoChildren(IReadOnlyList<Expr> children)` | `WithChildren` for a leaf. |
| `Expr WithTwoChildren(IReadOnlyList<Expr> children, Func<Expr, Expr, Expr> create)` | `WithChildren` for a two-child node. |
| `ArgumentException ChildCountMismatch(int expected, int actual)` | The exception for a wrong number of children. |

### `UnaryExpr` (abstract class : `Expr`)

Base for nodes with one argument: `Negate` and the one-argument functions.

| Member | Description |
|---|---|
| `UnaryExpr(Expr argument)` | Constructor for derived nodes. |
| `Expr Argument` | The single sub-expression. |
| `abstract Expr WithArgument(Expr argument)` | (protected) A node of the same type around a new argument. |
| `void Deconstruct(out Expr argument)` | Enables patterns such as `case Sin(var x):`. |

---

## Simplification

### `Simplifier` (static class)

| Member | Description |
|---|---|
| `Expr Simplify(this Expr expr, SimplifyMode mode = Generic)` | An equivalent, simpler expression in canonical form. Never changes a value where the expression is defined. |
| `Expr Simplify(this Expr expr, Assumptions assumptions, SimplifyMode mode = Generic)` | The same, also using what is known about the variables. |

### `SimplifyMode` (enum)

| Value | Description |
|---|---|
| `Generic` | Default. The result may be defined at more points than the original (`x/x` → `1`). |
| `Strict` | The result has exactly the same domain; domain-enlarging rewrites need assumptions. |

### `Canonicalizer` (static class)

| Member | Description |
|---|---|
| `Expr Canonicalize(this Expr expr)` | Flattens and sorts sums and products into a fixed order, without simplifying. |

Guide: [Simplification](simplification.md).

---

## Assumptions

### `Assumptions` (class)

An immutable set of facts about variables. Each `Assume*` method returns a new set.
Guide: [Assumptions](assumptions.md).

| Member | Description |
|---|---|
| `static Assumptions None` | The empty set. |
| `Assumptions Assume(string variable, NumberDomain domain = Unknown, Signing signing = Unknown)` | Adds a domain and a sign, combined with what is already known. Throws `ArgumentException` on a contradiction. |
| `Assumptions AssumePositive(string variable)` | v > 0 |
| `Assumptions AssumeNegative(string variable)` | v < 0 |
| `Assumptions AssumeNonZero(string variable)` | v ≠ 0 |
| `Assumptions AssumeNonNegative(string variable)` | v ≥ 0 |
| `Assumptions AssumeNonPositive(string variable)` | v ≤ 0 |
| `Assumptions AssumeReal(string variable)` | v is real. |
| `Assumptions AssumeRational(string variable)` | v is rational (and real). |
| `Assumptions AssumeInteger(string variable)` | v is an integer (and rational, real). |
| `Assumptions AssumeNatural(string variable)` | v is 1, 2, 3, … (and integer, positive). |
| `VariableAssumption Get(string variable)` | Everything assumed about the variable; `VariableAssumption.Unknown` if nothing. |
| `Signing SigningOf(string variable)` | What is assumed about its sign. |
| `bool IsPositive(string variable)` | Whether v > 0 is assumed. |
| `bool IsNegative(string variable)` | Whether v < 0 is assumed. |
| `bool IsNonZero(string variable)` | Whether v ≠ 0 follows from the assumptions. |
| `bool IsNonNegative(string variable)` | Whether v ≥ 0 follows. |
| `bool IsNonPositive(string variable)` | Whether v ≤ 0 follows. |
| `bool Has(string variable, NumberDomain domain)` | Whether v is assumed to be in every set of `domain`. |
| `bool IsReal(string variable)` | Whether v is assumed real. |
| `bool IsInteger(string variable)` | Whether v is assumed an integer. |

### `VariableAssumption` (struct)

| Member | Description |
|---|---|
| `VariableAssumption(Signing signing, NumberDomain domain)` | An assumption with the given sign and domain. Prefer the `Assumptions` methods. |
| `static VariableAssumption Unknown` | Nothing known. |
| `Signing Signing` | What is known about the sign. |
| `NumberDomain Domain` | The number sets the variable belongs to. |
| `VariableAssumption Combine(VariableAssumption other)` | Both must hold: signs narrow, domains accumulate. Throws `ArgumentException` on a contradiction. |

### `Signing` (enum)

`Unknown`, `Positive`, `Negative`, `Zero`, `NonZero`, `NonNegative`, `NonPositive`.

### `NumberDomain` (flags enum)

`Unknown`, `Real`, `Integer`, `Rational`, `Natural`. An integer variable is
`Integer | Rational | Real`.

### `AssumptionAnalysis` (static class)

Conservative proofs about whole expressions: `false` means "not proven", not "false".

| Member | Description |
|---|---|
| `bool IsProvablyPositive(this Expr expr, Assumptions assumptions)` | Proves expr > 0. |
| `bool IsProvablyNegative(this Expr expr, Assumptions assumptions)` | Proves expr < 0. |
| `bool IsProvablyNonZero(this Expr expr, Assumptions assumptions)` | Proves expr ≠ 0 wherever it is defined. |
| `bool IsProvablyNonNegative(this Expr expr, Assumptions assumptions)` | Proves expr ≥ 0. |
| `bool IsProvablyNonPositive(this Expr expr, Assumptions assumptions)` | Proves expr ≤ 0. |

---

## Factoring

### `PolynomialFactoring` (static class)

Guide: [Factoring](factoring.md).

| Member | Description |
|---|---|
| `(Expr Factored, bool Success) TryFactorReal(this Expr expr, string variable)` | Exact factoring over the reals: rational roots and square roots. Returns the original and `false` if nothing could be factored. |
| `(Expr Factored, bool Success) TryFactorComplex(this Expr expr, string variable)` | The same, also splitting with `i`; succeeds only if the polynomial splits into linear factors. |

---

## Root finding

Guide: [Root finding](root-finding.md).

### `RootFindingExtensions` (static class)

| Member | Description |
|---|---|
| `IReadOnlyList<double> FindRealRoots(this Expr expr, double leftLimit = -∞, double rightLimit = +∞, int scanSteps = 200)` | All real roots of `expr = 0` for its only variable, ascending. Undefined points are never roots. |
| `IReadOnlyList<double> FindRealRoots(this Expr expr, string variable, IReadOnlyDictionary<string, double>? fixedBindings = null, double leftLimit = -∞, double rightLimit = +∞, int scanSteps = 200)` | The same for a named variable, with values for the others. |
| `IReadOnlyList<double> FindRealRoots(this Expr left, Expr right, double leftLimit = -∞, double rightLimit = +∞, int scanSteps = 200)` | Real solutions of `left = right`. |
| `IReadOnlyList<double> FindRealRoots(this Expr left, Expr right, string variable, IReadOnlyDictionary<string, double>? fixedBindings = null, double leftLimit = -∞, double rightLimit = +∞, int scanSteps = 200)` | The same for a named variable. |
| `IReadOnlyList<ComplexNumber> FindComplexRoots(this Expr expr, double reMin, double reMax, double imMin, double imMax, int gridSteps = 12)` | Complex roots in a rectangle, by Newton's method from a grid of starting points. |
| `IReadOnlyList<ComplexNumber> FindComplexRoots(this Expr expr, string variable, IReadOnlyDictionary<string, ComplexNumber>? fixedBindings, double reMin, double reMax, double imMin, double imMax, int gridSteps = 12)` | The same for a named variable. |
| `IReadOnlyList<ComplexNumber> FindComplexRoots(this Expr left, Expr right, double reMin, double reMax, double imMin, double imMax, int gridSteps = 12)` | Complex solutions of `left = right`. |

### `RootFinder` (static class)

Newton's method from one starting point. Returns `(null, false)` if it doesn't converge.

| Member | Description |
|---|---|
| `(double? Root, bool Found) TryFindRoot(this Expr expr, double initialGuess, double tolerance = 1e-10, int maxIterations = 100)` | A real root near the guess, for the only variable. |
| `(double? Root, bool Found) TryFindRoot(this Expr expr, string variable, double initialGuess, IReadOnlyDictionary<string, double>? fixedBindings = null, double tolerance = 1e-10, int maxIterations = 100)` | The same for a named variable. |
| `(ComplexNumber? Root, bool Found) TryFindComplexRoot(this Expr expr, ComplexNumber initialGuess, double tolerance = 1e-10, int maxIterations = 100)` | A complex root near the guess. |
| `(ComplexNumber? Root, bool Found) TryFindComplexRoot(this Expr expr, string variable, ComplexNumber initialGuess, IReadOnlyDictionary<string, ComplexNumber>? fixedBindings = null, double tolerance = 1e-10, int maxIterations = 100)` | The same for a named variable. |

---

## Output

Guide: [Output](output.md).

### `Printer` (static class)

| Member | Description |
|---|---|
| `string Print(this Expr expr)` | Compact text, terms by descending degree, readable by `Parse`: `3x^2 - 4x + 1`. |

### `LatexPrinter` (static class)

| Member | Description |
|---|---|
| `string ToLatex(this Expr expr)` | LaTeX math: `\frac{x^{2}}{2} + \sin\left(x\right)`. |

---

## Node types

All nodes are immutable, sealed and derive from `Expr`. Binary nodes and `UnaryExpr` nodes
support positional patterns through `Deconstruct`. Evaluation conventions are in
[Evaluation](evaluation.md#conventions).

### Leaves

| Type | Constructor | Members |
|---|---|---|
| `Constant` | `Constant(Rational value)`, `Constant(int value)`, `Constant(long value)`, `Constant(double value)` | `Rational Value` — the exact value. The `double` constructor uses the shortest decimal form. |
| `Variable` | `Variable(string name)` | `string Name` |
| `Pi` | `Pi()` | π, printed as `π`. |
| `EulerNumber` | `EulerNumber()` | e = 2.718… |
| `ImaginaryUnit` | `ImaginaryUnit()` | i; real evaluation throws `InvalidOperationException`. |

`Pi`, `EulerNumber` and `ImaginaryUnit` have an empty `Deconstruct()` for patterns such as
`case Pi():`.

### Arithmetic

| Type | Constructor | Members |
|---|---|---|
| `Add` | `Add(Expr left, Expr right)` | `Left`, `Right` |
| `Subtract` | `Subtract(Expr left, Expr right)` | `Left`, `Right` |
| `Multiply` | `Multiply(Expr left, Expr right)` | `Left`, `Right` |
| `Divide` | `Divide(Expr numerator, Expr denominator)` | `Numerator`, `Denominator` |
| `Power` | `Power(Expr baseExpr, Expr exponent)` | `Base`, `Exponent` — principal value: a negative base with a non-integer exponent is undefined over the reals. |
| `Negate` | `Negate(Expr argument)` | `Argument` |

### Functions of one argument

All derive from `UnaryExpr`, take `(Expr argument)` and expose `Argument`.

| Group | Types |
|---|---|
| Trigonometric (radians) | `Sin`, `Cos`, `Tan`, `Cot`, `Sec`, `Csc` |
| Inverse trigonometric | `Asin`, `Acos`, `Atan` |
| Hyperbolic | `Sinh`, `Cosh`, `Tanh`, `Coth`, `Sech`, `Csch` |
| Inverse hyperbolic | `Asinh`, `Acosh`, `Atanh` |
| Exponential and logarithm | `Exp`, `Ln` |
| Roots and absolute value | `Sqrt`, `Abs` |
| Rounding and sign | `Floor`, `Ceiling`, `Round`, `Sign` |

### Functions of two arguments

| Type | Constructor | Members |
|---|---|---|
| `NthRoot` | `NthRoot(Expr argument, Expr degree)` | `Argument`, `Degree` — the real root: `nthroot(-8, 3) = -2`. |
| `Min` | `Min(Expr left, Expr right)` | `Left`, `Right` |
| `Max` | `Max(Expr left, Expr right)` | `Left`, `Right` |

---

## Rational

### `Rational` (struct)

An exact fraction of `BigInteger`s, always in lowest terms with a positive denominator.
Guide: [Numbers](numbers.md).

| Member | Description |
|---|---|
| `Rational(BigInteger numerator, BigInteger denominator)` | Reduced automatically; throws `DivideByZeroException` for a zero denominator. |
| `Rational(BigInteger integer)` | An integer. |
| `static Rational Zero`, `One`, `MinusOne` | Common values. |
| `BigInteger Numerator`, `BigInteger Denominator` | The reduced parts; the denominator is positive. |
| `bool IsZero`, `bool IsOne`, `bool IsInteger` | Tests of the value. |
| `int Sign` | -1, 0 or 1. |
| `Rational Abs()` | Absolute value. |
| `Rational Floor()` | Largest integer not greater: `floor(-5/2) = -3`. |
| `Rational Ceiling()` | Smallest integer not less: `ceiling(-5/2) = -2`. |
| `Rational Round()` | Nearest integer, halves away from zero. |
| `Rational Pow(int exponent)` | Exact power; negative exponents allowed, `0^0 = 1`. |
| `double ToDouble()` | Nearest `double`; may lose precision or overflow. |
| `static Rational FromDouble(double value)` | Exact value of the shortest decimal form: `0.1` → `1/10`. Throws `ArgumentException` for NaN and infinities. |
| `static Rational FromDecimalString(string text)` | Parses `"-12.5"`, `"1.5E-20"` exactly, in invariant culture. |
| `+ - * /`, unary `-` | Exact arithmetic; division by zero throws `DivideByZeroException`. |
| `== != < > <= >=`, `CompareTo`, `Equals` | Comparison by value. |
| implicit from `int`, `long`; explicit from `double` | Conversions; the `double` one is `FromDouble`. |
| `string ToString()` | `3/4`, `-3/4` or `5`, culture-invariant. |

---

## ComplexNumber

### `ComplexNumber` (struct)

A complex number with `double` parts; the result of `EvaluateComplex`. Guide:
[Numbers](numbers.md#complexnumber).

| Member | Description |
|---|---|
| `ComplexNumber(double real, double imaginary = 0)` | Constructor. |
| `static ComplexNumber Zero`, `One`, `ImaginaryUnit` | Common values. |
| `double Real`, `double Imaginary` | The parts. |
| `double Magnitude` | \|z\|, without overflow for large parts. |
| `double Phase` | The argument in radians, in (-π, π]. |
| `ComplexNumber Conjugate` | The complex conjugate. |
| `static ComplexNumber FromPolar(double magnitude, double phase)` | From polar form. |
| `static ComplexNumber Exp(ComplexNumber z)`, `Log`, `Sqrt` | Exponential, principal logarithm, principal square root. |
| `static ComplexNumber Pow(ComplexNumber baseValue, ComplexNumber exponent)` | Principal power; for base 0: `1` for exponent 0, `0` for Re > 0, ∞ for a negative real exponent, NaN otherwise. |
| `static ComplexNumber Sin`, `Cos`, `Tan` | Trigonometric functions. |
| `static ComplexNumber Asin`, `Acos`, `Atan` | Inverse trigonometric functions, principal values. |
| `static ComplexNumber Sinh`, `Cosh`, `Tanh`, `Coth`, `Sech`, `Csch` | Hyperbolic functions. |
| `static ComplexNumber Asinh`, `Acosh`, `Atanh` | Inverse hyperbolic functions, principal values. |
| `+ - * /`, unary `-` | Arithmetic; division avoids overflow. |
| `==`, `!=`, `Equals` | Exact equality of both parts. |
| implicit from `double` | A real number. |
| `string ToString()` | `1 - 2i`, `3`, `NaN`; culture-invariant. |
