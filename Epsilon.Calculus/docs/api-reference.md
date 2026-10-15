# API reference

Every public type and member of the `Epsilon.Calculus` package, version 1.0.0 in development.
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
[Symbolic integration](#symbolic-integration)

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
| `Expr Integrate(string variable)` | An antiderivative by the variable from a table of integrals and by substitution, simplified, without a constant; other variables are constants. Throws `NotSupportedException` where none is found. |
| `Expr Integrate()` | The same by the expression's only variable. Throws `InvalidOperationException` if it doesn't have exactly one. |
