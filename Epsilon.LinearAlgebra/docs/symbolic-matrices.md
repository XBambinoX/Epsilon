# Symbolic matrices

Matrices and vectors of expressions, `Matrix<Expr>` and `Vector<Expr>`, compute exactly: with
symbols, with functions such as `sin(x)`, and with numbers, which stay exact rationals. The
examples on this page use these matrices:

```csharp
var m = Matrix<Expr>.Parse("[[a, b], [c, d]]");
var r = Matrix<Expr>.Parse("[[1, 2], [3, 4]]");
var rotation = Matrix<Expr>.Parse("[[cos(x), -sin(x)], [sin(x), cos(x)]]");
```

## Parsing and printing

`Parse` reads a matrix as rows in brackets, `[[a, b], [c, d]]`, and a vector as
`[a, b, c]`. A comma inside parentheses belongs to the entry, so `min(a, b)` and `log(x, 2)`
are one entry each. The entries are read by `ExprParser.Parse` and, as there, not simplified:

```csharp
Matrix<Expr>.Parse("[[1/2, x + x]]").Print();              // [[1 / 2, x + x]]
Matrix<Expr>.Parse("[[1/2, x + x]]").Simplify().Print();   // [[1/2, 2x]]
Matrix<Expr>.Parse("[[theta, 2t]]", ["theta", "t"]).Print();   // [[theta, 2t]]
Vector<Expr>.Parse("[min(a, b), log(x, 2)]").Length;       // 2
```

As with `ExprParser.Parse`, `e`, `i` and `pi` are constants, not variables. With variable names
only those are variables, and they may be longer than a letter. Errors name the place:

| Text | `FormatException` |
|---|---|
| `[[1, 2], [3]]` | `Row 1 has 1 entries, but row 0 has 2.` |
| `[[1, 2], [3, 4]` | `Expected ']' at position 15, found the end of the text.` |
| `[[1]] x` | `Unexpected 'x' at position 6 after the closing ']'.` |
| `[[1, sen(x)]]` with `["x"]` | `Invalid entry at (0, 1), starting at position 5: Unknown identifier 'sen' at position 0. Declared variables: x.` |

`Print()` writes text that `Parse` reads back; after `Simplify()` it is the same matrix
again (a printed number `1/2` is read as a division). `ToLatex()` writes a `bmatrix`, a
vector as one column:

```csharp
rotation.ToLatex();
// \begin{bmatrix} \cos\left(x\right) & -\sin\left(x\right) \\ \sin\left(x\right) & \cos\left(x\right) \end{bmatrix}
Vector<Expr>.Parse("[x^2, 1/2]").Simplify().ToLatex();   // \begin{bmatrix} x^{2} \\ \frac{1}{2} \end{bmatrix}
```

## Determinant and characteristic polynomial

```csharp
m.Determinant().Print();                                                        // a * d - b * c
Matrix<Expr>.Parse("[[x + 1, x - 1], [x - 1, x - 3]]").Determinant().Print();   // -4
rotation.Determinant().Print();                                                 // 1

var p = Matrix<Expr>.Parse("[[2, 1], [1, 2]]").CharacteristicPolynomial("t");
p.Print();           // t^2 - 4t + 3
p.FindRealRoots();   // [1, 3]
```

`CharacteristicPolynomial(t)` is det(t I - A); its roots are the eigenvalues. The variable
must not occur in the matrix: `m.CharacteristicPolynomial("a")` throws `ArgumentException`.
For a matrix of numbers, factoring gives the eigenvalues exactly, where root finding rounds:

```csharp
var s = Matrix<Expr>.Parse("[[2, 0, 0], [0, 3, 4], [0, 4, 9]]");
var cp = s.CharacteristicPolynomial("t");
cp.Print();                           // t^3 - 14t^2 + 35t - 22
cp.TryFactorReal("t").Factored.Print();   // (t - 1) * (t - 2) * (t - 11)
cp.FindRealRoots();                   // [1, 1.9999999999999993, 11]
```

Both come from Berkowitz's algorithm. It builds the characteristic polynomial from the ones
of ever larger submatrices with additions and multiplications only, never dividing, so the
result holds for every value of the symbols. Intermediate values are expanded, so terms that
cancel do so: the determinant above is `-4`, not a product of brackets. The determinant is
`(-1)^n` times the constant term.

## Adjugate, inverse and Solve

The adjugate, the transposed matrix of cofactors, needs no division either and exists for
singular matrices too. The inverse is the adjugate divided by the determinant; `Solve` uses
Cramer's rule, `x = adj(A) b / det(A)`:

```csharp
m.Adjugate().Print();                            // [[d, -b], [-c, a]]
r.Inverse().Print();                             // [[-2, 1], [3/2, -1/2]]
rotation.Inverse().Print();                      // [[cos(x), sin(x)], [-sin(x), cos(x)]]
m.Solve(Vector<Expr>.Parse("[p, q]")).Print();
// [(d * p - b * q) / (a * d - b * c), (a * q - c * p) / (a * d - b * c)]
```

`Solve` with a matrix solves for each of its columns at once.

As nothing was divided on the way, the inverse is undefined exactly where the matrix is
singular, and nowhere else:

```csharp
var inverse = Matrix<Expr>.Parse("[[x, 1], [1, x]]").Inverse();
inverse.Print();   // [[x / (x^2 - 1), -1 / (x^2 - 1)], [-1 / (x^2 - 1), x / (x^2 - 1)]]
inverse.Substitute("x", 0).Simplify().Print();   // [[0, 1], [1, 0]]
inverse.Substitute("x", 1).Simplify().Print();   // [[1 / 0, -1 / 0], [-1 / 0, 1 / 0]]
```

Gaussian elimination would have divided by the first pivot `x` and lost the point `x = 0`,
where the matrix is its own inverse. At `x = 1` the matrix is singular, and the entries are
undefined there; the determinant `x^2 - 1` factors as `(x + 1) * (x - 1)` and says where.

A determinant that simplifies to 0 means the matrix is singular, and `Inverse` and `Solve`
throw `InvalidOperationException`:

```csharp
Matrix<Expr>.Parse("[[x, 2x], [1, 2]]").Inverse();
// InvalidOperationException: The matrix is singular: its determinant is 0.
```

A determinant that is zero but not recognized as such gives entries that are undefined
everywhere, never wrong values.

## Entry-wise operations

These do to every entry what the `Expr` method of the same name does, for matrices and
vectors of expressions:

| Method | Result |
|---|---|
| `Simplify(mode)`, `Simplify(assumptions, mode)` | Every entry simplified. |
| `Expand(mode)` | Every entry expanded. |
| `Substitute(variable, replacement)` | The variable replaced; not simplified. |
| `Differentiate(variable)` | The simplified partial derivatives. |
| `GetVariables()` | The variables of all entries. |
| `Evaluate(bindings)`, `Evaluate(("x", 1), ...)` | A matrix or vector of doubles. |
| `Evaluate(x)` | The same when all entries together have at most one variable. |
| `EvaluateComplex(...)` | The same three forms over the complex numbers. |

```csharp
Matrix<Expr>.Parse("[[(x + 1)^2, (x - 1)*(x + 1)]]").Expand().Print();   // [[x^2 + 2x + 1, x^2 - 1]]
rotation.Differentiate("x").Print();   // [[-sin(x), -cos(x)], [cos(x), -sin(x)]]
m.Substitute("a", 2).Print();          // [[2, b], [c, d]]
string.Join(", ", m.GetVariables().Order());   // a, b, c, d

m.Evaluate(("a", 1), ("b", 2), ("c", 3), ("d", 4));   // [[1, 2], [3, 4]]
Matrix<Expr>.Parse("[[x, x^2]]").Evaluate(3);         // [[3, 9]]
```

`Evaluate(3)` on a matrix with two variables throws `InvalidOperationException`:
`Expected at most 1 variable, found 2: [x, y]. Use the overload with named values for several variables.`

What the simplifier is told about the variables applies to every entry:

```csharp
var roots = Matrix<Expr>.Parse("[[sqrt(x^2), abs(x)]]");
roots.Simplify().Print();                                       // [[abs(x), abs(x)]]
roots.Simplify(Assumptions.None.AssumePositive("x")).Print();   // [[x, x]]
```
