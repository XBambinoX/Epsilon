# API reference

Every public type and member of the `Epsilon.Calculus` package, version 1.0.0.
All types are in the namespace `Epsilon.Calculus`. Each entry has a one-line description; the
[guides](README.md) explain the details, and your IDE shows the full XML documentation for every
member.

A test (`CalculusApiReferenceDocTests`) checks that every public type and member is listed
here.

**Contents:**
[Gradients](#gradients) -
[Vector fields](#vector-fields) -
[Hessians and critical points](#hessians-and-critical-points) -
[Numeric integration](#numeric-integration) -
[Symbolic integration](#symbolic-integration) -
[Exact definite integrals](#exact-definite-integrals) -
[Series](#series) -
[Limits](#limits)

The operations are extension members, called like members of `Expr` and `Vector<Expr>`:
`f.Gradient("x", "y")`, `field.Curl("x", "y", "z")`. The variables of `Gradient`, `Hessian`,
`Laplacian`, `DirectionalDerivative`, `Divergence` and `Curl` are passed as arguments or as a
list: `f.Gradient("x", "y")` and `f.Gradient(["x", "y"])` are the same. A name listed twice
throws `ArgumentException`. A function without a derivative, such as floor, throws
`NotSupportedException`.

---

## Gradients

### `GradientExtensions` (static class)

| Member | Description |
|---|---|
| `Vector<Expr> Gradient(params IReadOnlyList<string> variables)` | The simplified partial derivatives by the variables, in that order; 0 for a variable the expression doesn't have. |
| `Vector<Expr> Gradient()` | The gradient by every variable of the expression, in ordinal order of the names; empty without variables. |
| `Expr DirectionalDerivative(Vector<Expr> direction, params IReadOnlyList<string> variables)` | The gradient dotted with the direction, which isn't normalized. Throws `ArgumentException` if the lengths differ. |

## Vector fields

### `VectorFieldExtensions` (static class)

Extension members of `Vector<Expr>`, whose entry i is the component along the i-th variable.

| Member | Description |
|---|---|
| `Expr Divergence(params IReadOnlyList<string> variables)` | The sum of the derivatives of entry i by variable i. Throws `ArgumentException` if the lengths differ. |
| `Vector<Expr> Curl(params IReadOnlyList<string> variables)` | The curl of a field of 3 entries by 3 variables; a plane field is [P, Q, 0]. Throws `ArgumentException` for other lengths. |

## Hessians and critical points

### `HessianExtensions` (static class)

| Member | Description |
|---|---|
| `Matrix<Expr> Hessian(params IReadOnlyList<string> variables)` | The symmetric matrix of second partial derivatives; each mixed derivative is taken once. |
| `Matrix<Expr> Hessian()` | The Hessian by every variable of the expression, in ordinal order of the names. |
| `Expr Laplacian(params IReadOnlyList<string> variables)` | The sum of the second derivatives by the variables: the divergence of the gradient. |
| `Expr Laplacian()` | The Laplacian by every variable of the expression. |
| `CriticalPointKind ClassifyCriticalPoint(params (string Name, double Value)[] point)` | The second-derivative test at a point given as pairs, whose names are the variables. Throws `ArgumentException` for a name given twice. |
| `CriticalPointKind ClassifyCriticalPoint(IReadOnlyDictionary<string, double> point)` | The same with the point as a dictionary. Throws `ArgumentException` if the expression has a variable the point doesn't give, `InvalidOperationException` if a second derivative isn't finite there. |

### `CriticalPointKind` (enum)

What the signs of the eigenvalues of the Hessian say about a critical point.

| Value | Description |
|---|---|
| `LocalMinimum` | All eigenvalues are positive: a strict local minimum. |
| `LocalMaximum` | All eigenvalues are negative: a strict local maximum. |
| `Saddle` | Eigenvalues of both signs: neither a minimum nor a maximum. |
| `Inconclusive` | An eigenvalue is 0 and none have opposite signs: the second derivatives can't tell. |

## Numeric integration

### `NumericIntegrationExtensions` (static class)

| Member | Description |
|---|---|
| `double Integrate(string variable, double a, double b, double tolerance = 1e-10)` | The definite integral from a to b, either of which may be infinite, to the tolerance relative to the integral of the absolute value. Throws `ArgumentException` for a NaN limit or another variable in the expression, `ArgumentOutOfRangeException` for a tolerance outside (0, 1), `InvalidOperationException` if the integral diverges or can't be computed. |
| `double Integrate(double a, double b, double tolerance = 1e-10)` | The same by the expression's only variable. Throws `InvalidOperationException` if it has more than one. |

## Symbolic integration

### `SymbolicIntegrationExtensions` (static class)

| Member | Description |
|---|---|
| `Expr Integrate(string variable)` | An antiderivative by the variable from a table of integrals, by partial fractions, by substitutions, trigonometric ones among them, and by parts, simplified, without a constant; other variables are constants. Throws `NotSupportedException` where none is found, or where the imaginary unit is inside a function whose real and imaginary parts aren't written, as in atan(x + i). |
| `Expr Integrate()` | The same by the expression's only variable. Throws `InvalidOperationException` if it doesn't have exactly one. |

## Exact definite integrals

### `ExactIntegrationExtensions` (static class)

| Member | Description |
|---|---|
| `Expr IntegrateExact(string variable, Expr a, Expr b)` | The definite integral from a to b, exact, from the antiderivative: its limits where it is undefined, its jumps taken off, and 0 for a bounded odd integrand over a symmetric interval; other variables are constants. Throws `ArgumentException` if a limit has the variable or isn't real, `InvalidOperationException` if the integral diverges or the integrand isn't real on a part of the interval, `NotSupportedException` where no antiderivative or limit of it is found, or where it is undefined depends on another variable. |
| `Expr IntegrateExact(Expr a, Expr b)` | The same by the expression's only variable. Throws `InvalidOperationException` if it has more than one. |

## Series

### `SeriesExtensions` (static class)

| Member | Description |
|---|---|
| `Series Series(string variable, Expr point, int degree)` | The series at the point, the terms up to (x - a)^degree with exact coefficients, from a negative power at a pole and with fractional powers at a root of something that vanishes there; other variables are constants. Throws `ArgumentException` if the point has the variable, `NotSupportedException` where there is no such series, as for ln(x) at 0. |
| `Series Series(Expr point, int degree)` | The same by the expression's only variable. Throws `InvalidOperationException` if it doesn't have exactly one. |

### `Series` (class)

A series c (x - a)^q + ... + O((x - a)^Order) with rational exponents.

| Member | Description |
|---|---|
| `string Variable` | The variable. |
| `Expr Point` | The point a, simplified. |
| `IReadOnlyList<(Rational Exponent, Expr Coefficient)> Terms` | The terms with a coefficient other than 0, by increasing exponent. |
| `Rational Order` | The exponent of the remainder O((x - a)^Order). |
| `bool RightSided` | Whether the series holds only for x > a, as for sqrt(x) at 0. |
| `Expr Coefficient(Rational exponent)` | The coefficient of (x - a)^exponent, 0 where there is no term. Throws `ArgumentOutOfRangeException` for an exponent of `Order` or more. |
| `Expr ToExpr()` | The sum of the terms as an expression in the variable. |
| `string Print()` | The terms by increasing exponent and the remainder, as in `1 - (1/6) * x^2 + O(x^3)`; `ToString` is the same. |

## Limits

### `LimitExtensions` (static class)

| Member | Description |
|---|---|
| `Limit Limit(string variable, Expr point, LimitDirection direction = LimitDirection.Both)` | The exact limit as the variable tends to the point, from both sides or one; other variables are constants. Throws `ArgumentException` if the point has the variable, `InvalidOperationException` if the two sides differ or the expression isn't real on the side, `NotSupportedException` where the limit isn't found, as where a part oscillates. |
| `Limit Limit(string variable, double point, LimitDirection direction = LimitDirection.Both)` | The same with the point a `double`: `double.PositiveInfinity` and `double.NegativeInfinity` are inf and -inf, and a finite one is its decimal. Throws `ArgumentException` for NaN or a side beyond an infinite point. |
| `Limit Limit(Expr point, LimitDirection direction = LimitDirection.Both)` | The limit by the expression's only variable. Throws `InvalidOperationException` if it has more than one. |
| `Limit Limit(double point, LimitDirection direction = LimitDirection.Both)` | The same with the point a `double`. |

### `Limit` (class)

A limit: an exact value, or inf or -inf.

| Member | Description |
|---|---|
| `LimitKind Kind` | Whether it is a value or an infinity. |
| `Expr Value` | The value of a finite limit. Throws `InvalidOperationException` for inf and -inf. |
| `string Print()` | The value, or `inf` or `-inf`; `ToString` is the same. |

### `LimitKind` (enum)

| Value | Description |
|---|---|
| `Finite` | The expression tends to a value. |
| `PositiveInfinity` | It grows without bound. |
| `NegativeInfinity` | It falls without bound. |

### `LimitDirection` (enum)

| Value | Description |
|---|---|
| `Both` | From both sides: the limit exists where they agree. |
| `Left` | From the left, x < a. |
| `Right` | From the right, x > a. |
