# Fixed-size vectors and matrices

`Matrix<T>` and `Vector<T>` hold any number of entries on the heap. For geometry, games and
real-time code, where vectors have 2, 3 or 4 entries, the package has value types:
`Vector2<T>`, `Vector3<T>`, `Vector4<T>`, `Matrix2x2<T>`, `Matrix3x3<T>` and `Matrix4x4<T>`,
and `Quaternion<T>` for rotations (see [Rotations](rotations.md)). They never allocate and,
like everything in the package, are immutable.

## Entry types

The entries can be of any number type with the arithmetic operators: `float`, `double`,
`Half`, `decimal`, `int`, `long` and the like. What needs square roots or trigonometry, such as
`Norm`, `Normalize`, `Angle`, the rotations and the inverse, exists for `float`, `double` and
`Half` only, and doesn't compile for other types. So the cells of a grid can be `Vector3<int>`:

```csharp
Vector3<int> cell = (7, 8, 9);
cell + Vector3<int>.UnitX;   // [8, 8, 9]
cell / 2;                    // [3, 4, 4]: truncated, as in C#
new Vector2<int>(3, 4).NormSquared();                           // 25
new Matrix3x3<int>(2, 0, 0, 0, 3, 0, 0, 0, 1).Determinant();    // 6, exact
```

The entries can also be exact fractions, `Rational`; see
[Rational matrices](rational-matrices.md#fixed-size-types-of-rationals).

An alias keeps the names short. The examples on this page and the next ones use these two:

```csharp
using Vec3 = Epsilon.LinearAlgebra.Vector3<double>;
using Mat4 = Epsilon.LinearAlgebra.Matrix4x4<double>;
```

The names don't clash with `Vector3` and `Matrix4x4` of System.Numerics, which have no type
parameter.

## Fixed-size vectors

A tuple converts to a vector, `with` changes one entry of a copy, and a vector deconstructs:

```csharp
Vec3 p = (1, 2, 3);
var q = new Vec3(4, 5, 6);
var (x, y, z) = p;          // 1, 2, 3
p with { Y = 0 };           // [1, 0, 3]
p[2];                       // 3
p.XY;                       // [1, 2]
p.Dot(q);                   // 32
p.Cross(q);                 // [-3, 6, -3]
p.Hadamard((2, 2, 2));      // [2, 4, 6]
Vec3.Lerp(p, q, 0.5);       // [2.5, 3.5, 4.5]
new Vector2<double>(1, 0).Cross((0, 1));   // 1: the second vector is counterclockwise
new Vector4<double>(1, 2, 3, 4).XYZ;        // [1, 2, 3]
new Vec3(new Vector2<double>(1, 2), 3);     // [1, 2, 3]
```

| Member | Result |
|---|---|
| `X`, `Y`, `Z`, `W`, `v[i]` | The entries; they change only in `with`. |
| `Zero`, `One`, `UnitX` ... `UnitW` | Constant vectors. |
| `+`, `-`, and `*`, `/` with a number | Entry by entry. |
| `Dot`, `NormSquared`, `Cross`, `Hadamard`, `Lerp`, `Map` | For every entry type. In the plane `Cross` is the number x1 y2 - y1 x2. |
| `Norm`, `Normalize`, `Distance`, `Angle`, `ProjectOnto`, `Reflect`, `IsApproximately` | For `float`, `double` and `Half`. |
| `Min`, `Max`, `Abs`, `Clamp` | Entry by entry, for ordered number types. |

The floating-point methods are as accurate as those of `Vector<double>` (see
[Vector geometry](vector-geometry.md#at-the-extremes-of-floating-point)): the norm neither
overflows nor underflows, and the angle uses Kahan's formula. They take the fast naive formula
wherever it is accurate:

```csharp
Vec3 u = (3, 4, 0);
u.Norm();                                         // 5
u.Normalize();                                    // [0.6, 0.8, 0]
new Vec3(1, -1, 0).Reflect(Vec3.UnitY);           // [1, 1, 0]
Vec3.UnitX.Angle(Vec3.UnitY);                     // 1.5707963267948966, pi / 2
new Vec3(3e300, 4e300, 0).Norm();                 // 5E+300
new Vector2<Half>((Half)300, (Half)400).Norm();   // 500, although 300^2 overflows Half
new Vec3(-5, 3, 12).Clamp(Vec3.Zero, (10, 10, 10));   // [0, 3, 10]
```

`Min` and `Max` take the smaller or larger entry at each index, so they give the bounding box
of a set of points:

```csharp
Vec3[] points = [(1, 5, -2), (4, -1, 0), (-3, 2, 7)];
Vec3 min = points[0], max = points[0];
foreach (Vec3 point in points)
{
    min = min.Min(point);
    max = max.Max(point);
}
// min: [-3, -1, -2], max: [4, 5, 7], center (min + max) / 2: [0.5, 2, 2.5]
```

`IsApproximately` compares as for `Vector<double>`, with a relative tolerance that follows the
precision of the entries unless you pass one: 1e-9 for `double`, 1e-5 for `float` and 1e-2 for
`Half`. `==` compares the entries exactly, with NaN equal to NaN, as for `Matrix<T>`:

```csharp
Vec3 turned = (Math.Cos(Math.PI / 2), 1, 0);
turned == Vec3.UnitY;                  // false: cos(pi/2) is 6.1E-17
turned.IsApproximately(Vec3.UnitY);    // true
new Vector3<float>(1, 0, 0).IsApproximately((1.000001f, 0, 0));   // true
new Vec3(1, 0, 0).IsApproximately((1.000001, 0, 0));              // false
```

`Normalize` and `Angle` throw for the zero vector, as do `ProjectOnto` onto it and `Reflect` in
it, as for `Vector<double>`.

## Fixed-size matrices and transforms

Vectors are columns, as for `Matrix<T>`: `m * v` maps v, `a * b` applies b first, and a
translation is in the last column. So a transform is built from right to left:

```csharp
var model = Mat4.Translation((10, 0, 5))   // 3. move
          * Mat4.RotationY(Math.PI / 2)    // 2. turn a quarter about the y axis
          * Mat4.Scale(2);                 // 1. scale
model.TransformPoint((1, 0, 0));       // [10, 0, 3]
model.TransformDirection((0, 0, 1));   // [2, 0, 1.2246467991473532E-16]: no translation
```

The `1.2E-16` is `2 cos(pi/2)`: the cosine of the rounded pi/2 is not 0. Compare such results
with `IsApproximately`.

| Member | Result |
|---|---|
| `Translation(offset)` | Of space for `Matrix4x4<T>`; of the plane, in homogeneous coordinates, for `Matrix3x3<T>`. |
| `Scale(factors)`, `Scale(factor)` | Along the axes; the second, uniform one for `Matrix4x4<T>`. |
| `RotationX(angle)`, `RotationY`, `RotationZ`, `Rotation(axis, angle)` | In radians, by the right-hand rule: counterclockwise seen from the tip of the axis, so `RotationZ` turns `UnitX` toward `UnitY`. `Matrix2x2<T>` has `Rotation(angle)`. |
| `Rotation(quaternion)`, `FromEulerAngles(angles, order)` | From a quaternion or Euler angles; see [Rotations](rotations.md). |
| `FromRows`, `FromColumns` | From vectors; the columns are the images of the unit vectors. |
| `TransformPoint(p)` | (x, y, z, 1) mapped and divided by w unless w is 1, so projective maps work too. |
| `TransformDirection(d)` | (x, y, z, 0) mapped: translations leave it alone. |
| `M11` ... `M44`, `m[i, j]`, `Row(i)`, `Column(j)` | The entries; Mij is in row i, column j, and the indices of `m[i, j]` start at 0. |
| `Transpose()`, `Trace()`, `Determinant()` | The determinant by the closed formula, exact for integers. |
| `SymmetricEigen()` | Of `Matrix3x3<T>`: the eigenvalues and the axes of a symmetric matrix, such as an inertia tensor; see [Decompositions](decompositions.md#symmetric-eigenvalues). |

```csharp
var m = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
m.M12;             // 2
m[1, 2];           // 6, that is M23
m.Column(2);       // [3, 6, 9]
m.Determinant();   // 0

Mat4.Translation((1, 2, 3)).Column(3);   // [1, 2, 3, 1]: the translation in the last column

var project = Mat4.Identity with { M43 = 1, M44 = 0 };   // w = z
project.TransformPoint((2, 4, 2));                       // [1, 2, 1]
```

`Matrix3x3<T>` maps the plane in homogeneous coordinates as well, as a 2D game needs, and
`Matrix2x2<T>` holds its linear maps, which `new Matrix3x3<double>(m2)` embeds:

```csharp
var sprite = Matrix3x3<double>.Translation((100, 50)) * Matrix3x3<double>.RotationZ(Math.PI / 2);
sprite.TransformPoint((1, 0)).IsApproximately((100, 51));   // true

var turn = Matrix2x2<double>.Rotation(Math.PI / 2);
(turn * new Vector2<double>(1, 0)).IsApproximately((0, 1));   // true
new Matrix3x3<double>(turn) == Matrix3x3<double>.RotationZ(Math.PI / 2);   // true
```

## The inverse

`Inverse()` and `TryInverse(out inverse)` of `float`, `double` and `Half` matrices use the
closed formula, the adjugate over the determinant:

```csharp
model.Inverse().TransformPoint((10, 0, 3)).IsApproximately((1, 0, 0));   // true
Mat4.Scale((1, 1, 0)).TryInverse(out _);                                 // false: it flattens space

var noisy = new Matrix3x3<double>(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9);
noisy.Determinant();                                 // 1.734723475976807E-17, not 0
noisy.TryInverse(out _);                             // false
Matrix3x3<double>.Scale((1e-20, 1, 1)).Inverse();    // [[1E+20, 0, 0], [0, 1, 0], [0, 0, 1]]
```

As with the LU decomposition (see
[When is a matrix singular?](numeric-matrices.md#when-is-a-matrix-singular)), rounding rarely
leaves an exact 0 for a singular matrix. A matrix counts as singular when its determinant is no
larger than the bound on the rounding error of the formula: 2, 5 or 10 times the machine
epsilon (for 2 x 2, 3 x 3 and 4 x 4) times the sum of the magnitudes of the terms of the
determinant. The rule doesn't depend on the scale, so `diag(1e-20, 1, 1)`, where nothing
cancels, inverts exactly. Rows of huge or tiny entries are first scaled by powers of two, which
is exact, so neither overflow nor underflow spoils the result. Where `TryInverse` returns
`false`, also for an entry that is NaN or infinite, `Inverse` throws
`InvalidOperationException`: `The matrix is singular to working precision.`

## Conversions and System.Numerics

`Map` converts the entries to another number type, as C# allows no conversion operators for
generic types here. `ToVector()` and `ToMatrix()` give the general types, with all their
algorithms, and an explicit conversion goes back:

```csharp
Vector3<float> f = p.Map(float.CreateTruncating);                         // [1, 2, 3] of float
Vector3<int> c = new Vec3(1.5, -2.7, 3).Map(x => (int)Math.Floor(x));    // [1, -3, 3]
model.ToMatrix().Rank();                                                  // 4
p.ToVector().Outer(q.ToVector());                                         // a Matrix<double>, 3 x 3
(Mat4)Matrix<double>.Identity(4) == Mat4.Identity;                        // true
```

System.Numerics multiplies row vectors from the left, `v * M`, so its matrices are the
transposes of these. `ToSystemNumerics()` and `FromSystemNumerics()` of the `float` types
transpose, so a map stays the same map; `a * b` here is `b * a` there. A `Quaternion<float>`
converts as it is, with the same order of entries and the same product:

```csharp
var numerics = Matrix4x4<float>.Translation((1, 2, 3)).ToSystemNumerics();
numerics.M43;                                                                       // 3: in the last row
Matrix4x4<float>.FromSystemNumerics(numerics) == Matrix4x4<float>.Translation((1, 2, 3));   // true
```

`TransformPoints`, `TransformDirections` and `Transform` map a span of vectors into another,
or into itself:

```csharp
Vec3[] corners = [(0, 0, 0), (1, 0, 0)];
var moved = new Vec3[2];
Mat4.Translation((1, 2, 3)).TransformPoints(corners, moved);       // moved: [1, 2, 3], [2, 2, 3]
Mat4.Translation((1, 2, 3)).TransformDirections(corners, moved);   // moved: [0, 0, 0], [1, 0, 0]
```

The entries are stored in order without padding, so a buffer of numbers can be read as
vectors without copying:

```csharp
float[] buffer = [1, 2, 3, 4, 5, 6];
var vertices = MemoryMarshal.Cast<float, Vector3<float>>(buffer.AsSpan());
Matrix4x4<float>.Translation((1, 0, 0)).TransformPoints(vertices, vertices);
// buffer: [2, 2, 3, 5, 5, 6]
```

## Speed

Measured with BenchmarkDotNet against System.Numerics, which has `float` only and uses SIMD,
on an x86-64 processor: transforming many points takes about as long (1.1 to 1.3 times),
normalizing 1.5 times as long for `float` and 2 times for `double`, and the product and the
inverse of 4 x 4 matrices about 5 times as long, around 15 and 60 nanoseconds, as the code
here is scalar. The benchmarks are in `Epsilon.Benchmarks/FixedSizeBenchmarks.cs`.
