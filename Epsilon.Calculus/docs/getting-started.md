# Getting started

## Install

Version 1.0.0 of `Epsilon.Calculus` isn't on NuGet yet. Until it is, reference the project from
a clone of the repository:

```bash
git clone https://github.com/XBambinoX/Epsilon.git
dotnet add reference Epsilon/Epsilon.Calculus/Epsilon.Calculus.csproj
```

It requires .NET 10, and brings the core and `Epsilon.LinearAlgebra` with it. Three namespaces
cover everything:

```csharp
using Epsilon.Core;            // Expr, ExprParser
using Epsilon.LinearAlgebra;   // Vector<Expr>, Matrix<Expr>: gradients and Hessians
using Epsilon.Calculus;        // Gradient, Hessian, Integrate, ...
```

The operations are extension members: they are called like members of `Expr` and
`Vector<Expr>`, as `f.Gradient("x", "y")` and `f.Integrate("x")`.

## A first example

Where does f(x, y) = x^3 - 3x + y^2 have a minimum? Where the gradient is 0, and the Hessian
says what kind of point that is:

```csharp
var f = ExprParser.Parse("x^3 - 3*x + y^2");

f.Gradient("x", "y").Print();   // [3x^2 - 3, 2y]
f.Hessian("x", "y").Print();    // [[6x, 0], [0, 2]]
```

The gradient is 0 where 3x^2 = 3 and y = 0, at (1, 0) and (-1, 0). The second-derivative test
tells the two apart:

```csharp
f.ClassifyCriticalPoint(("x", 1), ("y", 0));    // LocalMinimum
f.ClassifyCriticalPoint(("x", -1), ("y", 0));   // Saddle
```

**Integrate**, to a formula or to a number:

```csharp
ExprParser.Parse("3*x^2 - 3").Integrate("x").Print();   // x^3 - 3x

ExprParser.Parse("exp(-x^2)").Integrate(double.NegativeInfinity, double.PositiveInfinity);
// 1.7724538509055159, the square root of pi
ExprParser.Parse("ln(x)").Integrate(0, 1);   // -1, though ln(x) goes to -infinity at 0
```

## Where next

- [Gradients](gradients.md) and [vector fields](vector-fields.md) for first derivatives,
  [Hessians and critical points](hessians.md) for second ones.
- [Numeric integration](numeric-integration.md) for definite integrals, singular and over
  infinite intervals too, and [symbolic integration](symbolic-integration.md) for
  antiderivatives.
