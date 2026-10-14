# Least squares

A system with more equations than unknowns has, as a rule, no solution: measured points don't
lie exactly on a line. Least squares finds the x that makes ||A x - b|| as small as possible.
`LeastSquares` does so for matrices of doubles, by QR decomposition, and exactly for rationals
and expressions, from the normal equations A^T A x = A^T b.

## Fitting a line

The line y = c0 + c1 x through the points (0, 1), (1, 3), (2, 4) and (3, 4) would solve one
equation c0 + c1 x = y per point, a row (1, x) of the matrix each:

```csharp
var a = Matrix<double>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]);
Vector<double> y = [1, 3, 4, 4];

var c = a.LeastSquares(y);   // [1.5, 1]: the line y = 1.5 + x
y - a * c;                   // [-0.5, 0.5, 0.5, -0.5]: the residuals
a.Transpose() * (y - a * c); // [0, 0]
```

The residuals are perpendicular to the columns of the matrix: that is what makes their sum of
squares smallest.

## Any function with linear coefficients

The columns can hold any functions of x, as long as the unknowns are their coefficients. A
parabola y = c0 + c1 x + c2 x^2 takes the powers of x:

```csharp
double[] xs = [0, 1, 2, 3, 4], ys = [1.1, 1.9, 5.2, 9.8, 17.1];
var powers = Matrix<double>.Create(5, 3, (i, j) => Math.Pow(xs[i], j));   // [[1, 0, 0], [1, 1, 1], [1, 2, 4], ...]
powers.LeastSquares(Vector.Create(ys));   // [1.082857142857144, -0.0957142857142874, 1.021428571428572]
```

## Exact and symbolic fits

With rationals the result is exact; `Rational.FromDouble` reads the measurements as the decimals
they were written as:

```csharp
Matrix<Rational>.FromRows([1, 0], [1, 1], [1, 2], [1, 3]).LeastSquares([1, 3, 4, 4]);   // [3/2, 1]

var exactPowers = Matrix<Rational>.Create(5, 3, (i, j) => new Rational((int)Math.Pow(xs[i], j)));
exactPowers.LeastSquares(Vector.Create(ys).Map(Rational.FromDouble));   // [379/350, -67/700, 143/140]
```

With expressions, the fit becomes a formula. Symbols for the measurements give the weights of
each one; a parabola through five equally spaced points, evaluated at the middle one, is the
smoothing filter of Savitzky and Golay:

```csharp
var parabola = Matrix<Expr>.Create(5, 3, (i, j) => (Expr)(int)Math.Pow(i - 2, j));   // x = -2 ... 2
parabola.LeastSquares(Vector<Expr>.Parse("[a, b, c, d, f]"))[0].Print();
// (12/35) * b + (12/35) * d + (17/35) * c - (3/35) * a - (3/35) * f
```

Symbols in the matrix give the general formula, here of a line through the origin, y = k x:

```csharp
Matrix<Expr>.Parse("[[p], [q], [r]]").LeastSquares(Vector<Expr>.Parse("[u, v, w]")).Print();
// [(p * u + q * v + r * w) / (p^2 + q^2 + r^2)]
```

The names avoid `e`, which `Parse` reads as Euler's number.

## More unknowns than equations

A matrix with more columns than rows has many exact solutions, and `LeastSquares` gives the one
of the smallest norm: of all points on the plane x + 2y + 2z = 9, the one nearest the origin.
A square matrix gives the solution of `Solve`:

```csharp
Matrix<Rational>.FromRows([1, 2, 2]).LeastSquares([9]);   // [1, 2, 2]
Matrix<double>.FromRows([1, 2, 2]).LeastSquares([9]);     // [0.9999999999999991, 1.9999999999999993, 1.9999999999999993]
```

`Solve` itself keeps to square matrices and points here:

```csharp
Matrix<double>.Zero(2, 3).Solve([1, 2]);
// InvalidOperationException: Solve needs a square matrix, not a 2 x 3 matrix. LeastSquares takes any shape.
```

## Dependent columns

When the columns are linearly dependent, many x fit equally well, and `LeastSquares` throws.
The pseudo-inverse picks the one of the smallest norm, for any rank (see
[Decompositions](decompositions.md#pseudo-inverse-norm-and-condition-number)):

```csharp
var dependent = Matrix<double>.FromRows([1, 2], [2, 4], [3, 6]);
dependent.LeastSquares([1, 2, 3]);
// InvalidOperationException: The columns of the matrix are linearly dependent to working precision. PseudoInverse handles any rank.
dependent.PseudoInverse() * Vector.Create<double>(1, 2, 3);   // [0.2, 0.4]
```

For doubles, the columns count as dependent when some combination of them, each scaled to
length 1, is shorter than the rounding error: the test doesn't change when a column is scaled,
as by a change of units. Rationals and expressions are dependent exactly when A^T A is singular.

## Why QR for doubles

The normal equations square the condition number of the matrix, and with it the rounding
error. In the matrix of Lauchli, 1 + 1e-16 rounds to 1 in A^T A, which becomes singular, while
QR works on the matrix itself:

```csharp
double eps = 1e-8;
var lauchli = Matrix<double>.FromRows([1, 1], [eps, 0], [0, eps]);
Vector<double> b = [2, eps, eps];
lauchli.LeastSquares(b);                                        // [0.9999999999999998, 1.0000000000000002]
(lauchli.Transpose() * lauchli).Solve(lauchli.Transpose() * b);
// InvalidOperationException: The matrix is singular: its rank is 1, not 2, to working precision.
```

Rationals don't round, so for them the normal equations are exact.
