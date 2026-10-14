# Vector geometry

For vectors of `double` and of `Expr`:

| Method | Result |
|---|---|
| `v.Norm()`, `v.NormSquared()` | The Euclidean length sqrt(v . v), and v . v. |
| `v.Normalize()` | The unit vector in the same direction. |
| `v.Distance(w)` | The length of `v - w`. |
| `v.Cross(w)` | The cross product of two vectors of length 3. |
| `v.Angle(w)` | The angle between the vectors, in radians from 0 to pi. |
| `v.ProjectOnto(w)` | The projection (v . w / w . w) w onto the line through `w`. |
| `v.Reflect(n)` | The mirror image in the plane perpendicular to `n`, which need not have length 1. |
| `Vector<T>.Lerp(start, end, t)` | (1 - t) start + t end: `start` at t = 0, `end` at t = 1. |
| `v.Outer(w)` | The matrix v w^T. |

The length is `Norm` because `Length` is the number of entries. The members that take two
vectors throw `ArgumentException` for different lengths. For vectors of 2, 3 or 4 numbers in
fast code, the [fixed-size vectors](fixed-size.md#fixed-size-vectors) have the same methods.

## With doubles

```csharp
Vector<double> u = [3, 4];
u.Norm();                                          // 5
u.Normalize();                                     // [0.6, 0.8]
Vector.Create<double>(1, 2).Distance([4, 6]);      // 5
Vector.Create<double>(1, 0, 0).Cross([0, 1, 0]);   // [0, 0, 1]
Vector.Create<double>(1, 1).Angle([1, 0]);         // 0.7853981633974483, pi / 4
Vector.Create<double>(2, 3).ProjectOnto([1, 1]);   // [2.5, 2.5]
Vector.Create<double>(1, -1).Reflect([0, 1]);      // [1, 1]: a ball bouncing off the floor
Vector<double>.Lerp([0, 10], [10, 20], 0.25);      // [2.5, 12.5]
Vector.Create<double>(1, 2).Outer([3, 4]);         // [[3, 4], [6, 8]]
```

`Lerp` extrapolates outside 0 to 1: `Lerp([0, 10], [10, 20], 1.5)` is `[15, 25]`.

The cross product is perpendicular to both vectors, and its length is the area of the
parallelogram they span. So three corners of a triangle give its normal and its area:

```csharp
Vector<double> p1 = [1, 0, 0], p2 = [0, 2, 0], p3 = [0, 0, 3];
var normal = (p2 - p1).Cross(p3 - p1);   // [6, 3, 2]
normal.Norm() / 2;                       // 3.5, the area
```

Subtracting the projection leaves the part of a vector that is perpendicular to a direction,
one step of the Gram-Schmidt process:

```csharp
var e1 = Vector.Create<double>(1, 1, 0).Normalize();
Vector<double> v = [1, 0, 1];
var e2 = (v - v.ProjectOnto(e1)).Normalize();   // [0.4082482904638631, -0.4082482904638631, 0.8164965809277261]
e1.Dot(e2);                                     // 0
```

## With expressions

With expressions the results are exact and simplified:

```csharp
Vector<Expr>.Parse("[1, 1]").Normalize().Print();                          // [sqrt(2) / 2, sqrt(2) / 2]
Vector<Expr>.Parse("[1, 0]").Angle(Vector<Expr>.Parse("[1, 1]")).Print();   // π / 4
Vector<Expr>.Parse("[a, b]").Norm().Print();                               // sqrt(a^2 + b^2)
Vector<Expr>.Parse("[a, b, c]").Cross(Vector<Expr>.Parse("[x, y, z]")).Print();
// [b * z - c * y, c * x - a * z, a * y - b * x]
Vector<Expr>.Parse("[x, y]").ProjectOnto(Vector<Expr>.Parse("[1, 1]")).Print();   // [(x + y) / 2, (x + y) / 2]
Vector<Expr>.Parse("[x, y]").Reflect(Vector<Expr>.Parse("[0, 1]")).Print();       // [x, -y]
```

`Angle` of expressions is the arccosine of v . w over the product of the norms, so it is exact
at the angles that `Simplify` knows.

## With rationals

Vectors of `Rational` have the methods whose result stays rational: `NormSquared`, `Cross`,
`ProjectOnto`, `Reflect`, `Lerp` and `Outer`. The norm of `[1, 1]` is sqrt(2), not a rational,
so `Norm`, `Normalize`, `Distance` and `Angle` are missing:

```csharp
Vector<Rational> r = [1, 2];
r.NormSquared();                                         // 5
r.ProjectOnto([1, 1]);                                   // [3/2, 3/2]
r.Reflect([1, 1]);                                       // [-2, -1]
Vector<Rational>.Lerp([0, 0], [1, 3], new Rational(1, 3));   // [1/3, 1]
```

## At the extremes of floating point

The numeric methods hold up where the naive formulas fail:

```csharp
Vector.Create(3e300, 4e300).Norm();               // 5E+300, not infinity
Vector.Create(1e-300, 1e-300).Normalize();        // [0.7071067811865476, 0.7071067811865476], not NaN
Vector.Create<double>(1, 0).Angle([1, 1e-10]);    // 1E-10, not 0
```

`Norm` divides the entries by a power of two before squaring them, which is exact: the result
is the naive one wherever that one neither overflows nor underflows. `Normalize`,
`ProjectOnto` and `Reflect` scale the same way. `Angle` uses Kahan's formula
2 atan2(|a - b|, |a + b|) of the unit vectors a and b, accurate at every angle; the arccosine
of the cosine gives 0 above, because the cosine 1 - 5E-21 rounds to 1.

## The zero vector

The zero vector has no direction, so the methods that need one throw:

| Call | Exception |
|---|---|
| `Vector<double>.Zero(2).Normalize()` | `InvalidOperationException: Cannot normalize the zero vector.` |
| `v.Angle(Vector<double>.Zero(2))` | `ArgumentException: The angle with the zero vector is undefined.` |
| `v.ProjectOnto(Vector<double>.Zero(2))` | `ArgumentException: Cannot project onto the zero vector.` |
| `v.Reflect(Vector<double>.Zero(2))` | `ArgumentException: The normal is the zero vector.` |

`Cross` throws `InvalidOperationException` for vectors that don't have length 3:
`The cross product needs vectors of length 3, not 2.`
