# Decompositions

A decomposition writes a matrix of doubles as a product of simpler ones, and with them come the
eigenvalues, the singular values, the pseudo-inverse and the condition number. Every one of
them works to working precision and reports what it can't do, such as a matrix that is not
symmetric, rather than return an answer that is wrong.

| Member | Result |
|---|---|
| `QR()` | `(Q, R)`: A = Q R, Q with orthonormal columns, R upper triangular. |
| `Cholesky()`, `TryCholesky(out L)` | L lower triangular with A = L L^T, for a symmetric positive definite matrix. |
| `SymmetricEigen()` | `(Values, Vectors)`: A = V diag(values) V^T for a symmetric matrix; also for `Matrix3x3<T>`. |
| `Svd()` | `(U, S, V)`: A = U diag(S) V^T for any matrix. |
| `PseudoInverse()` | The Moore-Penrose pseudo-inverse, for any shape and rank. |
| `SpectralNorm()`, `ConditionNumber()` | The largest singular value, and the largest over the smallest. |

The LU decomposition behind `Determinant`, `Inverse` and `Solve` is on the page
[Numeric matrices](numeric-matrices.md); `LeastSquares`, which uses QR, has a page of its own,
[Least squares](least-squares.md).

## QR

`QR()` gives the thin decomposition by Householder reflections: for an m x n matrix and
k = min(m, n), Q is m x k with orthonormal columns and R is k x n and upper triangular, with a
diagonal that is not negative:

```csharp
var (q, r) = Matrix<double>.FromRows([3, 0], [4, 5]).QR();
r;                                                         // [[5, 4], [0, 3]]
q.IsApproximately(Matrix<double>.FromRows([0.6, -0.8], [0.8, 0.6]));   // true
(q * r).IsApproximately(Matrix<double>.FromRows([3, 0], [4, 5]));     // true
```

The columns of Q are an orthonormal basis of the space the columns of A span, the result of
the Gram-Schmidt process, computed more accurately.

## Cholesky

A symmetric positive definite matrix, such as a covariance or the matrix of an energy, is
L L^T for a lower triangular L with a positive diagonal. `TryCholesky` doubles as the test
whether a matrix is positive definite, as the Hessian at a minimum of a function is:

```csharp
var covariance = Matrix<double>.FromRows([4, 2], [2, 3]);
var l = covariance.Cholesky();                              // [[2, 0], [1, 1.4142135623730951]]
Matrix<double>.FromRows([2, -1], [-1, 2]).TryCholesky(out _);   // true
Matrix<double>.FromRows([1, 2], [2, 1]).TryCholesky(out _);     // false: indefinite
```

L turns independent random numbers into correlated ones: for a vector z of standard normal
numbers, `mean + l * z` has the covariance above. The matrix must be symmetric up to rounding;
the entries below the diagonal are used. One that is singular, positive semidefinite at best,
counts as not positive definite:

```csharp
Matrix<double>.FromRows([1, 1], [1, 1]).Cholesky();
// InvalidOperationException: The matrix is not positive definite to working precision.
```

## Symmetric eigenvalues

`SymmetricEigen()` gives the eigenvalues of a symmetric matrix in ascending order and unit
eigenvectors as the columns of V, by the Jacobi method. The ellipse x^2 + x y + y^2 = 1 is
v^T A v = 1 for A = [[1, 0.5], [0.5, 1]]; its axes are the eigenvectors, and its semi-axes
1 / sqrt of the eigenvalues:

```csharp
var ellipse = Matrix<double>.FromRows([1, 0.5], [0.5, 1]);
var (values, vectors) = ellipse.SymmetricEigen();
values;                                  // [0.5, 1.5]
vectors;                                 // [[0.7071067811865476, 0.7071067811865475], [-0.7071067811865475, 0.7071067811865476]]
values.Map(v => 1 / Math.Sqrt(v));       // [1.414213562373095, 0.8164965809277261]: along x = -y and x = y
```

The columns are orthonormal, and the entry of largest magnitude of each is positive. With
integers that have exact eigenvalues the results are often exact:
`[[2, 0, 0], [0, 3, 4], [0, 4, 9]]` gives `[1, 2, 11]`.

`Matrix3x3<T>` of `float`, `double` and `Half` has the same method without allocating, and its
vectors are the columns of a rotation, with determinant 1. The covariance of a cloud of points
gives its main directions, and with them a box around the points that fits better than one
along the axes:

```csharp
using Vec3 = Epsilon.LinearAlgebra.Vector3<double>;

Vec3[] points = [(0, 0, 0), (2, 2, 0), (4, 4, 1), (6, 6, 0)];
Vec3 mean = points.Aggregate(Vec3.Zero, (sum, p) => sum + p) / points.Length;   // [3, 3, 0.25]
var spread = Matrix3x3<double>.Zero;
foreach (Vec3 p in points)
{
    Vec3 d = p - mean;
    spread += Matrix3x3<double>.FromColumns(d * d.X, d * d.Y, d * d.Z);   // d d^T
}

var (variances, axes) = (spread / points.Length).SymmetricEigen();
variances;          // [0, 0.17477764162066062, 10.012722358379339]
axes.Column(2);     // [0.7066494206079106, 0.7066494206079107, 0.035960988710105474]: the main direction
```

The points lie in a plane, so the smallest variance is 0. `axes.Transpose() * (p - mean)` gives
a point in the frame of the box, and `Quaternion<double>.FromRotationMatrix(axes)` its
orientation. The same method gives the principal axes of an inertia tensor.

## Singular values

`Svd()` gives the thin singular value decomposition of any matrix by one-sided Jacobi: U and V
with min(m, n) orthonormal columns and the singular values S in descending order:

```csharp
var (u, s, v) = Matrix<double>.FromRows([3, 0], [4, 5]).Svd();
s;   // [6.708203932499369, 2.2360679774997894]: 3 sqrt(5) and sqrt(5)
(u * Matrix<double>.FromDiagonal(s) * v.Transpose()).IsApproximately(Matrix<double>.FromRows([3, 0], [4, 5]));   // true
```

A matrix has rank r when r singular values are not 0, and the first k terms of
s_i u_i v_i^T give its best approximation of rank k. A table whose middle row is almost twice
the first:

```csharp
var table = Matrix<double>.FromRows([1, 2, 3], [2, 4, 6.1], [3, 6, 9]);
var (u3, s3, v3) = table.Svd();
s3;                                                             // [14.043057523168343, 0.05035276541593254, 0]
var nearest = s3[0] * u3.Column(0).Outer(v3.Column(0));         // the closest matrix of rank 1
(table - nearest).SpectralNorm();                               // 0.0503527654159321: the second singular value
```

One-sided Jacobi keeps even the small singular values accurate where the columns are of very
different sizes: `[[1e300, 0], [0, 1e-300]]` gives `[1E+300, 1E-300]`, and
`[[1e20, 1e9], [1e9, 1]]` the second singular value 0.99 to all digits.

## Pseudo-inverse, norm and condition number

```csharp
Matrix<double>.FromRows([1, 2], [2, 4]).PseudoInverse();   // [[0.04, 0.08], [0.08, 0.16]]

var m = Matrix<double>.FromRows([1, 2], [3, 4]);
m.SpectralNorm();       // 5.4649857042190435: the most it stretches a vector of length 1
m.ConditionNumber();    // 14.933034373659256

Matrix<double>.Create(8, 8, (i, j) => 1.0 / (i + j + 1)).ConditionNumber();   // 15257577275.315193
Matrix<double>.FromRows([1, 2], [2, 4]).ConditionNumber();                   // infinity
```

The pseudo-inverse V diag(1 / S) U^T inverts the singular values above max(m, n) eps times the
largest and leaves the others 0, as NumPy and MATLAB do; `A.PseudoInverse() * b` is the
least-squares solution of the smallest norm for any rank. The condition number says about how
many times a relative error in b can grow in the solution of A x = b: the 8 x 8 Hilbert matrix
can cost 10 of the 16 digits of a double. Unlike singularity it depends on the scale, so
`[[1e-20, 0], [0, 1]]` has the condition number 1E+20, although it inverts exactly.

## Speed

The Jacobi methods are simple and accurate, but for large matrices slower than the tridiagonal
QR iteration of LAPACK. In a quick measurement on an x86-64 processor, `SymmetricEigen` took
0.6 microseconds for a 3 x 3 matrix, as long for `Matrix3x3<double>`, which doesn't allocate,
10 milliseconds for 100 x 100 and 1.4 seconds for 400 x 400; `Svd` took 12 milliseconds for
100 x 100, `QR` 70 milliseconds for 500 x 300 and `Cholesky` 12 milliseconds for 400 x 400.
