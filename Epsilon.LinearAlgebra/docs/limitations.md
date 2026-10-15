# Known limitations

What version 1.5.0 of `Epsilon.LinearAlgebra` doesn't do yet. The results it gives are right;
the items below are missing features and the places where an exact result is longer than it
could be. Each example is checked by a test (in `Epsilon.Tests/LinearAlgebra/DocsTests.cs`), so
when a limitation is lifted this page is updated with it. The expressions have
[limitations of their own](../../Epsilon.Core/docs/limitations.md).

## Numeric matrices

- **Eigenvalues of symmetric matrices only.** Those of other matrices of numbers, which may be
  complex, are the roots of `CharacteristicPolynomial`, see
  [Symbolic matrices](symbolic-matrices.md#determinant-and-characteristic-polynomial).
- **The decompositions are for doubles.** QR, Cholesky, the eigenvalues and the singular values
  need square roots, which rationals and expressions don't keep exact; `LeastSquares` exists for
  all three.
- **The Jacobi methods are slow for large matrices.** `SymmetricEigen` of a 400 x 400 matrix
  takes about 1.4 seconds; see [Speed](decompositions.md#speed).
- **No arithmetic for `ComplexNumber`.** A matrix of complex numbers, such as the result of
  `EvaluateComplex`, is a container without arithmetic: `z * z` doesn't compile for a
  `Matrix<ComplexNumber>`.

## Symbolic matrices

- **No rank for symbolic matrices.** It depends on the values of the symbols; matrices of
  doubles and rationals have one.
- **Characteristic polynomials of symbolic matrices print in the core's canonical order**,
  not by powers of the variable. The value is right; for numbers the order is the usual
  `t^2 - 4t + 3`:

  ```csharp
  Matrix<Expr>.Parse("[[a, b], [c, d]]").CharacteristicPolynomial("t").Print();
  // a * d + t^2 - b * c + (-a - d) * t
  ```

- **The simplifier knows few trigonometric identities.** It knows sin^2 + cos^2 = 1, but not
  when the two terms share a symbolic factor, and it doesn't know the addition formulas such as
  cos(a) cos(b) - sin(a) sin(b) = cos(a + b). So products of rotations stay expanded, and the
  determinant of the Jacobian of polar coordinates is longer than `r`. The values are right,
  and compiled code computes each sine and cosine once:

  ```csharp
  var polar = Vector<Expr>.Parse("[r * cos(t), r * sin(t)]", ["r", "t"]);
  polar.Jacobian(["r", "t"]).Determinant().Print();   // cos(t)^2 * r + sin(t)^2 * r

  (Matrix<Expr>.Rotation(new Variable("a")) * Matrix<Expr>.Rotation(new Variable("b")))[0, 0].Print();
  // cos(a) * cos(b) - sin(a) * sin(b)
  ```

- **Symbolic rotations turn about the coordinate axes only.** A rotation about another axis
  exists for numbers, as the fixed-size `Rotation(axis, angle)`.
- **`ToCSharp` writes `double` code** that calls `Math`; there is no `float` variant and no
  shader language. A node type from outside the core has no C# form.
- **Variable names for `Parse` are an array**, `["theta", "t"]`, not a list of arguments as
  for `ExprParser.Parse`: with `params` here, the current C# compiler warns at every call.

## Fixed-size types

- **No SIMD.** The 4 x 4 product and inverse take about 5 times as long as in System.Numerics;
  see [Speed](fixed-size.md#speed).
- **Euler angles turn about three different axes** (the six orders of `EulerOrder`); the
  classic orders that repeat an axis, such as z-x-z, are missing.
- **No off-center perspective** for virtual reality and stereo, and no `LookRotation` that turns
  an object to face a direction with an up vector; `FromToRotation` and
  `FromRotationMatrix(Matrix3x3<T>.FromColumns(right, up, forward))` cover most uses.
