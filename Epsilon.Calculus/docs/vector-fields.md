# Vector fields

A vector field gives a vector at every point. Here it is a `Vector<Expr>` whose entry i is the
component along the i-th variable, so the variables are always named, in the order of the
entries.

## Divergence

The divergence is the sum of the derivatives of entry i by variable i:

```csharp
Vector<Expr>.Parse("[x*y, y^2]").Divergence("x", "y").Print();   // 3y
```

Entry i goes with the i-th name in the list, so swapping the names gives another result:

```csharp
Vector<Expr>.Parse("[x*y, y^2]").Divergence("y", "x").Print();   // x
```

## Curl

The curl is defined in three dimensions:

```csharp
Vector<Expr>.Parse("[-y, x, 0]").Curl("x", "y", "z").Print();          // [0, 0, 2]
Vector<Expr>.Parse("[x^2*y, x*y^2, 0]").Curl("x", "y", "z").Print();   // [0, 0, y^2 - x^2]
```

A field in the plane, [P, Q], is written [P, Q, 0]. The last entry of its curl is
dQ/dx - dP/dy, the integrand of Green's theorem.

## Identities

The curl of a gradient is 0, the divergence of a curl is 0, and the divergence of a gradient is
the [Laplacian](hessians.md#the-laplacian):

```csharp
var f = ExprParser.Parse("x*y*z + exp(x)*sin(y) + z^2");
f.Gradient("x", "y", "z").Curl("x", "y", "z").Print();   // [0, 0, 0]

var field = Vector<Expr>.Parse("[x*y*z, x^2 + z, sin(x*y)]");
field.Curl("x", "y", "z").Divergence("x", "y", "z").Print();   // 0

ExprParser.Parse("x^2*y + y^3").Gradient("x", "y").Divergence("x", "y").Print();   // 8y
```

The simplifier doesn't bring fractions to a common denominator, so for some functions the zero
shows only in value, up to rounding:

```csharp
var curl = ExprParser.Parse("atan(y/z)").Gradient("x", "y", "z").Curl("x", "y", "z");
curl.Evaluate(("x", 1), ("y", 2), ("z", 3));   // [6.938893903907228E-18, 0, 0]
```

## Errors

| Call | Throws |
|---|---|
| `Vector<Expr>.Parse("[x, y]").Divergence("x")` | `ArgumentException`: The field has 2 entries but there are 1 variables. |
| `Vector<Expr>.Parse("[x, y]").Curl("x", "y")` | `ArgumentException`: The curl needs a field of 3 entries, not 2. |
| `field.Curl("x", "y", "x")` | `ArgumentException`: Variable 'x' is listed twice. |
