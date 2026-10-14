# Numeric matrices

For matrices of doubles, Gaussian elimination with partial pivoting, the LU decomposition
P A = L U, gives the determinant, the inverse, the solutions of systems and the rank. Every
result is rounded, so this page also says when a matrix counts as singular.

## Determinant, inverse and systems

```csharp
var n = Matrix<double>.FromRows([2, 1], [4, 4]);
n.Determinant();                                     // 4
n.Solve([4, 12]);                                    // [1, 2]
n.Solve(Matrix<double>.FromRows([4, 1], [12, 0]));   // [[1, 1], [2, -1]]
n.Inverse();                                         // [[1, -0.25], [-1, 0.5]]
n.Rank();                                            // 2
```

`Solve` with a matrix solves for each of its columns at once, from one decomposition. To solve
several systems with the same matrix, that is cheaper than an `Inverse()` followed by
products, and more accurate. `Determinant`, `Inverse` and `Solve` need a square matrix; `Rank`
works for any shape.

A system of equations hides in many problems. The parabola y = c0 + c1 x + c2 x^2 through the
points (1, 2), (2, 3) and (3, 6) has coefficients that solve one, with a row of powers of x
for each point:

```csharp
var powers = Matrix<double>.Create(3, 3, (i, j) => Math.Pow(i + 1, j));   // [[1, 1, 1], [1, 2, 4], [1, 3, 9]]
powers.Solve([2, 3, 6]);                                                  // [3, -2, 1]: y = 3 - 2x + x^2
```

## Rank

The rank is the number of independent rows, which is also the number of independent columns:

```csharp
Matrix<double>.FromRows([1, 2], [3, 4], [5, 6]).Rank();            // 2
Matrix<double>.FromRows([1, 2, 3], [2, 4, 6], [1, 1, 1]).Rank();   // 2: the second row is twice the first
```

## When is a matrix singular?

In floating point, a singular matrix rarely gives an exact zero pivot. The last pivot of
`[[1, 2, 3], [4, 5, 6], [7, 8, 9]]` comes out as about `1.1e-16`, and plain elimination then
reports a determinant of about `6.7e-16` and an inverse with entries near `1e16`. Here every
entry carries, next to its value, the sum of the magnitudes of the terms it was computed
from; a pivot no larger than the rounding error of that sum counts as zero:

```csharp
var s = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
s.Determinant();   // 0
s.Rank();          // 2
s.Inverse();       // InvalidOperationException: The matrix is singular: its rank is 2, not 3, to working precision.

var scaled = Matrix<double>.FromRows([1e-20, 0], [0, 1]);
scaled.Determinant();   // 1E-20
scaled.Inverse();       // [[1E+20, 0], [0, 1]]
```

The second matrix is badly scaled but regular: nothing cancels in it. A rule relative to the
largest entry would call it singular. The same rule decides all four methods, so `Rank() == n`
exactly when `Inverse()` succeeds, and the determinant of a singular matrix is 0.

The determinant of a regular matrix is a product of n pivots, though, and can leave the range
of doubles while the inverse is fine:

```csharp
var tiny = 1e-200 * Matrix<double>.Identity(2);
tiny.Determinant();   // 0: 1e-400 is below the smallest double
tiny.Rank();          // 2
tiny.Inverse();       // [[1E+200, 0], [0, 1E+200]]
```

So test `Rank()` or `Inverse()`, not the determinant, to find out whether a matrix is singular.

Some regular matrices are too close to singular for doubles. The Hilbert matrix, with entries
1 / (i + j + 1), is the classic one; at 12 x 12 its determinant is about 2.6E-78 and the
rounding errors swamp it:

```csharp
var hilbert = Matrix<double>.Create(12, 12, (i, j) => 1.0 / (i + j + 1));
hilbert.Rank();      // 11
hilbert.Inverse();   // InvalidOperationException: The matrix is singular: its rank is 11, not 12, to working precision.
```

[Rational matrices](rational-matrices.md) compute such a matrix exactly.

## Errors

| Call | Exception |
|---|---|
| `Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]).Inverse()` | `InvalidOperationException: The inverse needs a square matrix, not a 2 x 3 matrix.` |
| `n.Solve([1, 2, 3])` | `ArgumentException: The right-hand side has 3 entries, but the matrix has 2 rows.` |
| `Matrix<double>.FromRows([1, double.NaN], [3, 4]).Determinant()` | `InvalidOperationException: The matrix has a non-finite entry at (0, 1): NaN.` |

An entry that is NaN or infinite makes the result meaningless, so `Determinant`, `Inverse`,
`Solve` and `Rank` throw and name the entry.
