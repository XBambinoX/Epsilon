# Rational matrices

`Matrix<Rational>` and `Vector<Rational>` compute with the exact fractions of the core by
Gaussian elimination. As nothing is rounded, any pivot that isn't 0 will do, and there is no
tolerance: a matrix is singular exactly when its determinant is 0.

## Determinant, inverse and systems

Integers convert to `Rational`; a fraction is `new Rational(1, 3)`:

```csharp
var q = Matrix<Rational>.FromRows([1, 2], [3, 4]);
q.Determinant();    // -2
q.Inverse();        // [[-2, 1], [3/2, -1/2]]
q.Solve([5, 6]);    // [-4, 9/2]
q / 3;              // [[1/3, 2/3], [1, 4/3]]
q.Pow(3);           // [[37, 54], [81, 118]]
q.Trace();          // 5

var singular = Matrix<Rational>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
singular.Rank();      // 2
singular.Inverse();   // InvalidOperationException: The matrix is singular: its rank is 2, not 3.
```

`Trace`, `Determinant`, `Inverse`, `Solve`, `Rank`, `Pow` and `Hadamard` work as for doubles;
dividing by 0 throws `DivideByZeroException`: `Cannot divide a matrix by 0.` The norms,
`Normalize` and `Angle` are missing, as the norm of `[1, 1]` is sqrt(2) and not a rational;
for the vector methods see [Vector geometry](vector-geometry.md#with-rationals).

## Ill-conditioned matrices

The Hilbert matrix, with entries 1 / (i + j + 1), is the classic ill-conditioned matrix. Its
inverse has integer entries, which doubles get right to only about eight digits at 8 x 8:

```csharp
var hilbert = Matrix<Rational>.Create(8, 8, (i, j) => new Rational(1, i + j + 1));
hilbert.Inverse()[7, 7];                                       // 176679360
hilbert * hilbert.Inverse() == Matrix<Rational>.Identity(8);   // true
hilbert.Map(r => r.ToDouble()).Inverse()[7, 7];                // 176679357.49307457
```

At 12 x 12 doubles count it as singular (see
[Numeric matrices](numeric-matrices.md#when-is-a-matrix-singular)), while rationals give the
exact determinant:

```csharp
var hilbert12 = Matrix<Rational>.Create(12, 12, (i, j) => new Rational(1, i + j + 1));
hilbert12.Rank();                    // 12
hilbert12.Determinant().ToDouble();  // 2.637780651253548E-78
hilbert12.Inverse()[11, 11];         // 11445589052352
```

The fractions grow with the size of the matrix, but an 8 x 8 inverse still takes a fraction of
a millisecond, against tens of milliseconds for the same numbers in a `Matrix<Expr>`.

## Conversions

`Map` converts the entries. `Rational.FromDouble` takes the shortest decimal form of a double,
so 0.1 becomes 1/10 rather than the binary fraction that the double holds:

```csharp
var d = Matrix<double>.FromRows([0.1, 0.2], [0.3, 0.4]);
d.Inverse();                                  // [[-20.000000000000007, 10.000000000000004], [15.000000000000004, -5.000000000000002]]
d.Map(Rational.FromDouble).Inverse();         // [[-20, 10], [15, -5]]
q.Inverse().Map(r => r.ToDouble());           // [[-2, 1], [1.5, -0.5]]
q.Map(r => (Expr)r).CharacteristicPolynomial("t").Print();   // t^2 - 5t - 2
```

A rational converts to an `Expr` constant implicitly, so a `Matrix<Expr>` of it has the
symbolic methods, such as the characteristic polynomial.

## Fixed-size types of rationals

`Rational` works in generic math, so it is an entry type of the
[fixed-size vectors and matrices](fixed-size.md) too. They compute exactly; the inverse goes
through `Matrix<Rational>`:

```csharp
Vector3<Rational> a = (new Rational(1, 2), new Rational(1, 3), 1);
a.Dot((1, 2, 3));                            // 25/6

var m = new Matrix3x3<Rational>(2, 1, 0, 1, 2, 1, 0, 1, 2);
(Matrix3x3<Rational>)m.ToMatrix().Inverse();
// [[3/4, -1/2, 1/4], [-1/2, 1, -1/2], [1/4, -1/2, 3/4]]
```
