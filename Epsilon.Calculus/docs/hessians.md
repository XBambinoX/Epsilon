# Hessians and critical points

## The Hessian

The Hessian is the matrix of second partial derivatives, a `Matrix<Expr>`. Entry (i, j) is the
derivative by the i-th and the j-th variable:

```csharp
var f = ExprParser.Parse("x^2*y + sin(y)");

f.Hessian("x", "y").Print();   // [[2y, 2x], [2x, -sin(y)]]
f.Hessian().Print();           // [[2y, 2x], [2x, -sin(y)]]
```

Without a list it is by every variable, in ordinal order of the names, as for the
[gradient](gradients.md). The matrix is symmetric: each mixed derivative is taken once and
mirrored. The simplifier doesn't always bring both orders of differentiation to one form, as for
x^y, and the [second-derivative test](#critical-points) needs a symmetric matrix:

```csharp
var h = ExprParser.Parse("x^y").Hessian("x", "y");
h == h.Transpose();   // true
```

Numbers come from `Evaluate` and `Compile` of the matrix, as for the gradient.

## The Laplacian

The Laplacian is the sum of the second derivatives by each variable: the trace of the Hessian,
and the divergence of the gradient.

```csharp
f.Laplacian("x", "y").Print();   // 2y - sin(y)
f.Laplacian().Print();           // 2y - sin(y)
```

A function whose Laplacian is 0 is harmonic:

```csharp
ExprParser.Parse("exp(x)*sin(y)").Laplacian("x", "y").Print();   // 0
ExprParser.Parse("x^3 - 3*x*y^2").Laplacian("x", "y").Print();   // 0
```

For rational functions the zero may show only in value, because the simplifier doesn't bring
fractions to a common denominator. The potential 1/r in three dimensions is harmonic:

```csharp
ExprParser.Parse("1/sqrt(x^2 + y^2 + z^2)").Laplacian("x", "y", "z").Evaluate(("x", 1), ("y", 2), ("z", 3));   // 0
```

## Critical points

At a critical point the gradient is 0, and the signs of the eigenvalues of the Hessian there
tell what kind of point it is. `ClassifyCriticalPoint` computes them with `SymmetricEigen` of
Epsilon.LinearAlgebra:

```csharp
var g = ExprParser.Parse("x^3 - 3*x + y^2");

g.ClassifyCriticalPoint(("x", 1), ("y", 0));    // LocalMinimum
g.ClassifyCriticalPoint(("x", -1), ("y", 0));   // Saddle
ExprParser.Parse("-x^2 - 3*y^2 + x*y").ClassifyCriticalPoint(("x", 0), ("y", 0));   // LocalMaximum
ExprParser.Parse("x^4 + y^4").ClassifyCriticalPoint(("x", 0), ("y", 0));            // Inconclusive
```

| Result | The eigenvalues |
|---|---|
| `LocalMinimum` | all positive: a strict local minimum |
| `LocalMaximum` | all negative: a strict local maximum |
| `Saddle` | of both signs: neither |
| `Inconclusive` | one is 0 to working precision, and none have opposite signs: the second derivatives can't tell |

x^4 + y^4 has a minimum at the origin, but its Hessian there is 0; x^3 and -x^4 are
inconclusive in the same way. Eigenvalues of both signs decide even when another is 0:

```csharp
ExprParser.Parse("x^2 - y^2 + z^4").ClassifyCriticalPoint(("x", 0), ("y", 0), ("z", 0));   // Saddle
```

The variables are those of the point, given as pairs or as a dictionary. As a function of x and
y, x^2 is constant along y, so the origin is not a strict minimum:

```csharp
ExprParser.Parse("x^2").ClassifyCriticalPoint(("x", 0));             // LocalMinimum
ExprParser.Parse("x^2").ClassifyCriticalPoint(("x", 0), ("y", 0));   // Inconclusive
g.ClassifyCriticalPoint(new Dictionary<string, double> { ["x"] = 1, ["y"] = 0 });   // LocalMinimum
```

The point isn't checked to be critical: find it first, by solving gradient = 0 or numerically
as below. Where an eigenvalue is 0 is decided relative to the largest one, so the result doesn't
depend on the scale of the function.

| Call | Throws |
|---|---|
| `g.ClassifyCriticalPoint(("x", 1))` | `ArgumentException`: The point has no value for variable 'y'. |
| `g.ClassifyCriticalPoint(("x", 1), ("y", 0), ("x", 2))` | `ArgumentException`: Variable 'x' is given twice. |
| `ExprParser.Parse("x*ln(x)").ClassifyCriticalPoint(("x", 0))` | `InvalidOperationException`: The second derivative by x and x is Infinity at the point. |

## Newton's method

Newton's method for a minimum steps by the Hessian solved against the gradient, and converges in
a few steps. The Rosenbrock function has its minimum at the end of a long curved valley:

```csharp
var rosenbrock = ExprParser.Parse("(1 - x)^2 + 100*(y - x^2)^2");
var gradient = rosenbrock.Gradient("x", "y").Compile("x", "y");
var hessian = rosenbrock.Hessian("x", "y").Compile("x", "y");

Vector<double> p = [-1.2, 1.0];
for (int i = 0; i < 8; i++)
    p -= hessian(p[0], p[1]).Solve(gradient(p[0], p[1]));

p;                                                             // [1, 1]
rosenbrock.ClassifyCriticalPoint(("x", p[0]), ("y", p[1]));   // LocalMinimum
```
