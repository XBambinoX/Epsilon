# Arithmetic

The arithmetic is defined for matrices and vectors of `double`, of `Rational` and of `Expr`.
Other entry types have no arithmetic: the operators don't compile for them.

The examples on this page use these matrices:

```csharp
var a = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]);
var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");
```

## Operators

| Operation | Result |
|---|---|
| `A + B`, `A - B`, `-A` | Entry by entry; the sizes must agree. |
| `A * B` | The matrix product; `A` needs as many columns as `B` has rows. |
| `A * v` | A matrix times a column vector: a vector. |
| `s * A`, `A * s`, `A / s` | Every entry times or divided by the scalar. |
| `A.Pow(n)` | A^n by repeated squaring; the identity for n = 0, a power of the inverse for negative n. |
| `A.Hadamard(B)` | The entry-wise product; the sizes must agree. |
| `A.Trace()` | The sum of the diagonal; the matrix must be square. |
| `v + w`, `v - w`, `-v`, `s * v`, `v * s`, `v / s` | The same for vectors. |
| `v.Dot(w)`, `v.Hadamard(w)` | The dot product and the entry-wise product. |

With doubles:

```csharp
a * a.Transpose();                                       // [[14, 32], [32, 77]]
a * Matrix<double>.FromRows([1, 0], [0, 1], [1, 1]);     // [[4, 5], [10, 11]]
a * Vector.Create(1.0, 0, -1);                           // [-2, -2]
2 * a;                                                   // [[2, 4, 6], [8, 10, 12]]
a / 2;                                                   // [[0.5, 1, 1.5], [2, 2.5, 3]]

Vector<double> v = [1, 2, 3], w = [4, 5, 6];
v + w;                                                   // [5, 7, 9]
v.Dot(w);                                                // 32
```

With rationals every result is an exact fraction:

```csharp
var q = Matrix<Rational>.FromRows([1, 2], [3, 4]);
q * new Rational(1, 2);   // [[1/2, 1], [3/2, 2]]
q * q;                    // [[7, 10], [15, 22]]
```

With expressions every entry of a result is simplified. The scalars can be numbers or
expressions, and numbers stay exact:

```csharp
(m + m).Print();                                 // [[2a, 2b], [2c, 2d]]
(m * new Variable("x")).Print();                 // [[a * x, b * x], [c * x, d * x]]
(m * Vector<Expr>.Parse("[x, y]")).Print();     // [a * x + b * y, c * x + d * y]
(m * m).Print();                                 // [[b * c + a^2, a * b + b * d], [a * c + c * d, b * c + d^2]]

var r = Matrix<Expr>.Parse("[[1, 2], [3, 4]]");
(r * r).Print();                                 // [[7, 10], [15, 22]]
(r / 2).Print();                                 // [[1/2, 1], [3/2, 2]]
Vector<Expr>.Parse("[x, y]").Dot(Vector<Expr>.Parse("[x, y]")).Print();   // x^2 + y^2
```

The simplification uses the default `Generic` mode of `Simplify`, so an entry `x/x` becomes
`1`: the result may be defined at more points. The operators of `Expr` itself only build a
tree; the operators of matrices compute, as `Differentiate` does.

## Powers

`Pow` multiplies by repeated squaring, so it needs at most 2 log2(n) products:
`Pow(int.MaxValue)` takes 60, not two billion. The powers of `[[1, 1], [1, 0]]` hold the
Fibonacci numbers:

```csharp
Matrix<double>.FromRows([1, 1], [1, 0]).Pow(10);           // [[89, 55], [55, 34]]
Matrix<Expr>.Parse("[[1, x], [0, 1]]").Pow(10).Print();    // [[1, 10x], [0, 1]]
Matrix<Expr>.Parse("[[0, -1], [1, 0]]").Pow(4).Print();    // [[1, 0], [0, 1]]: four quarter turns
```

A negative power is a power of the inverse, so it throws where the inverse does, for a singular
matrix:

```csharp
var n = Matrix<double>.FromRows([2, 1], [4, 4]);
n.Pow(-1);                         // [[1, -0.25], [-1, 0.5]]
n.Pow(-1) == n.Inverse();          // true
q.Pow(-2);                         // [[11/2, -5/2], [-15/4, 7/4]]
```

## Entry-wise product and trace

```csharp
a.Hadamard(a);                     // [[1, 4, 9], [16, 25, 36]]
Matrix<Expr>.Parse("[[1, 2], [3, 4]]").Hadamard(Matrix<Expr>.Parse("[[x, y], [z, w]]")).Print();
// [[x, 2y], [3z, 4w]]
m.Trace().Print();                 // a + d
n.Trace();                         // 6
```

## Sizes that don't fit

Sizes that don't fit throw an `ArgumentException` that names them; methods that need a square
matrix throw `InvalidOperationException`:

```csharp
a * a;                                     // ArgumentException: Cannot multiply a 2 x 3 matrix by a 2 x 3 matrix: 3 columns on the left, 2 rows on the right.
v + Vector.Create(1.0, 2.0);               // ArgumentException: Cannot add vectors of different lengths: 3 and 2.
a.Trace();                                 // InvalidOperationException: The trace needs a square matrix, not a 2 x 3 matrix.
```

## Norms

`FrobeniusNorm()` of a `double` or `Expr` matrix is the square root of the sum of the squares
of all its entries: the norm of the entries as one long vector. The numeric one divides the
entries by a power of two before squaring them, which is exact, so it neither overflows nor
underflows:

```csharp
Matrix<double>.FromRows([1, 1], [1, 1]).FrobeniusNorm();   // 2
m.FrobeniusNorm().Print();                                 // sqrt(a^2 + b^2 + c^2 + d^2)
```

The norm of a vector is `Norm()`, see [Vector geometry](vector-geometry.md). The spectral norm of
a matrix of doubles, the most it stretches a vector of length 1, is `SpectralNorm()`, see
[Decompositions](decompositions.md#pseudo-inverse-norm-and-condition-number).

## Approximate equality

Floating point rarely gives the exact zeros of a formula: a rotation by pi/2 has
cos(pi/2) = 6.1E-17 where 0 is expected. `IsApproximately` compares matrices and vectors of
`double` up to rounding:

```csharp
double cos = Math.Cos(Math.PI / 2), sin = Math.Sin(Math.PI / 2);
var turn = Matrix<double>.FromRows([cos, -sin], [sin, cos]);
var exact = Matrix<double>.FromRows([0, -1], [1, 0]);

turn == exact;                  // false
turn.IsApproximately(exact);    // true
Vector.Create(0.1 + 0.2).IsApproximately([0.3]);   // true
```

Two matrices are close when ||A - B|| <= relativeTolerance * max(||A||, ||B||) in the
Frobenius norm, with a relative tolerance of 1e-9 unless you pass another:

```csharp
var one = Matrix<double>.FromRows([1.0]);
one.IsApproximately(Matrix<double>.FromRows([1.001]));                            // false
one.IsApproximately(Matrix<double>.FromRows([1.001]), relativeTolerance: 1e-2);   // true
```

Measured against the whole matrix, an entry that should be 0 doesn't spoil the comparison; a
relative test entry by entry could never accept the 6.1E-17. Against the zero matrix the right
side is 0 too, so pass an absolute tolerance there:

```csharp
var h = Matrix<double>.FromRows([1, 2], [3, 4]);
var residual = h * h.Inverse() - Matrix<double>.Identity(2);
residual.IsApproximately(Matrix<double>.Zero(2, 2));                            // false
residual.IsApproximately(Matrix<double>.Zero(2, 2), absoluteTolerance: 1e-12);  // true
```

Matrices of different sizes are not close, infinite entries must be equal, and NaN is close to
nothing. Matrices of `Rational` and `Expr` compare exactly with `==`.
