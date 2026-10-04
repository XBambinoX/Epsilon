# Contributing to Epsilon

Thanks for your interest! Bug reports, questions and pull requests are welcome on
[GitHub](https://github.com/XBambinoX/Epsilon).

## Reporting a bug

Please include:

- the Epsilon version and .NET version;
- the smallest expression that shows the problem, as code or as text for `ExprParser.Parse`;
- what you got and what you expected.

A wrong result — `Simplify` changing a value, a false root — is the most serious kind of bug
for this library, and gets fixed first. Check [known limitations](Epsilon.Core/docs/limitations.md)
before reporting something that isn't supported yet.

## Building

You need the .NET 10 SDK.

```bash
git clone https://github.com/XBambinoX/Epsilon.git
cd Epsilon
dotnet build
dotnet test
```

| Project | Contents |
|---|---|
| `Epsilon.Core` | The library published as the `Epsilon` package |
| `Epsilon.LinearAlgebra` | The library published as the `Epsilon.LinearAlgebra` package |
| `Epsilon.Tests` | xUnit tests |
| `Epsilon.Benchmarks` | BenchmarkDotNet benchmarks for parsing, simplification, differentiation and evaluation |
| `Epsilon.Calculus` | Experimental module, not packaged yet |

For a change that may affect speed, compare the benchmarks before and after it. They only
give meaningful numbers in Release:

```bash
dotnet run -c Release --project Epsilon.Benchmarks -- --filter '*'
dotnet run -c Release --project Epsilon.Benchmarks -- --filter '*LargeSum*'
```

## Workflow: fork, branch, pull request

You can't push to this repository directly; changes come in as pull requests from your own
copy (a *fork*):

1. **Fork** the repository with the *Fork* button on GitHub. This creates
   `github.com/<you>/Epsilon`.
2. **Clone your fork** and add this repository as `upstream`, so you can stay up to date:

   ```bash
   git clone https://github.com/<you>/Epsilon.git
   cd Epsilon
   git remote add upstream https://github.com/XBambinoX/Epsilon.git
   ```

3. **Create a branch** from the latest `main` for each change:

   ```bash
   git fetch upstream
   git switch -c fix/describe-the-change upstream/main
   ```

4. **Commit and push** to your fork:

   ```bash
   dotnet test
   git commit -am "fix(Simplifier): describe the change"
   git push -u origin fix/describe-the-change
   ```

5. **Open a pull request** on GitHub from your branch into `XBambinoX/Epsilon:main`. CI
   runs the build and tests on it; for a first-time contributor, a maintainer approves the
   run first.

If `main` moves on while your pull request is open, update your branch with
`git fetch upstream && git rebase upstream/main` and `git push --force-with-lease`.

## Pull requests

- **One change per pull request.** One bug fix or one feature, so it can be reviewed on its
  own.
- **Every fix comes with a regression test** that fails without it.
- **Correctness first.** A rewrite rule that is valid only under a condition must check the
  condition (see [Simplification](Epsilon.Core/docs/simplification.md#the-guarantee)); when in doubt,
  leave the expression unsimplified.
- **Document public members.** The build generates XML documentation, and every public
  member needs a `///` comment and a line in the API reference of its package:
  [Epsilon.Core/docs/api-reference.md](Epsilon.Core/docs/api-reference.md) for the core,
  [Epsilon.LinearAlgebra/docs/api-reference.md](Epsilon.LinearAlgebra/docs/api-reference.md)
  for linear algebra (`ApiReferenceDocTests` and `LinearAlgebraApiReferenceDocTests` fail
  until it has one).
- **Keep the docs in sync.** Every example in the READMEs and guides is mirrored by a test:
  `Epsilon.Core/README.md` (the package README) by `ReadmeExamplesTests`, the repository
  `README.md` by `RepositoryReadmeExamplesTests`, `Epsilon.Core/docs/` by `DocsExamplesTests`, and the
  README and guide of `Epsilon.LinearAlgebra` by the tests in `LinearAlgebraDocsTests.cs`.
  If you change a result shown there, update the text and the test together. Lifting a [limitation](Epsilon.Core/docs/limitations.md) means removing
  it from that page.
- Code, comments and commit messages are in English. Commit messages follow
  [Conventional Commits](https://www.conventionalcommits.org/): `fix(Simplifier): ...`,
  `feat(Parser): ...`, `docs: ...`.
- Add a line to the changelog of the package, `Epsilon.Core/CHANGELOG.md` or
  `Epsilon.LinearAlgebra/CHANGELOG.md`, for changes users will notice.

## Worked example: adding a function

This walks through everything a new built-in function touches, using `sinh` — already in the
core, so every snippet below is real code you can look up. Adding, say, `erf` follows the
same steps.

### 1. Branch

In your fork (see [Workflow](#workflow-fork-branch-pull-request)):

```bash
git fetch upstream
git switch -c feature/sinh upstream/main
```

### 2. The node

A function of one argument derives from `UnaryExpr` and implements only its own math.
Traversal, equality, substitution, canonical ordering and `Simplify`'s tree walk come for
free. `sinh` lives with the other trigonometric nodes in
`Epsilon.Core/Functions/Trigonometry.cs`:

```csharp
/// <summary>The hyperbolic sine <c>sinh(x)</c>.</summary>
/// <param name="argument">The argument.</param>
public sealed class Sinh(Expr argument) : UnaryExpr(argument)
{
    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Math.Sinh(Argument.Evaluate(bindings));
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => ComplexNumber.Sinh(Argument.EvaluateComplex(bindings));
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => new Multiply(new Cosh(Argument), DerivativeOf(Argument, variable));
    /// <inheritdoc/>
    protected override Expr WithArgument(Expr argument) => new Sinh(argument);
    /// <inheritdoc/>
    public override string ToString() => $"sinh({Argument})";
}
```

- **`Evaluate`** returns NaN or ±∞ where the function is undefined; it doesn't throw.
- **`EvaluateComplex`** needs a principal-value implementation in `ComplexNumber`
  (`ComplexNumber.Sinh`). If there is no sensible complex extension, throw
  `NotSupportedException` with a message that says why.
- **`DifferentiateCore`** applies the chain rule through `DerivativeOf`, which is
  deliberately unsimplified: `Differentiate` simplifies the whole result once. It is only
  called when the argument depends on the variable, so there is no constant case to handle.
- **`ToString`** is the fully parenthesized debug form.

### 3. The parser

In `Epsilon.Core/Expressions/ExprParser.cs`, add the name to both `ReservedIdentifiers` and
`FunctionNames`, and construct the node in the function `switch`:

```csharp
"sinh" => new Sinh(first),
```

The tokenizer matches the longest name first, so `sinh` wins over `sin` + `h` without
further work.

### 4. Output

Add an arm to `Printer.PrintInternal` and `LatexPrinter.LatexInternal`, and add the type to
`CanBeImplicitFactor` in both files so that `2sinh(x)` is printed without a `*`:

```csharp
// Printer.cs
Sinh(var a) => $"sinh({PrintInternal(a, 0)})",
// Latex.cs
Sinh(var a) => $"\\sinh\\left({LatexInternal(a, 0)}\\right)",
```

Use the standard LaTeX command if there is one, otherwise `\operatorname{name}`.

**Watch the round trip.** `Print` must produce text that `Parse` reads back into the same
tree. A new name can collide with existing ones: `a * sinh(x)` must not be printed as
`asinh(x)`. `PrintRoundTripTests.TrickyExpressions` collects such cases — add yours.

### 5. Simplification (optional)

Only add rules that are always valid, or that check their condition — see
[the guarantee](Epsilon.Core/docs/simplification.md#the-guarantee). Exact values are the safest kind:
`sinh(0) = 0` holds everywhere. They live in `Epsilon.Core/Expressions/ExactValues.cs`. A rule that is valid only on part of the domain, like
`asinh(sinh(x)) = x` for complex `x`, needs a guard or has to wait.

### 6. Tests

Cover evaluation, the derivative, printing, LaTeX and the round trip:

```csharp
public class SinhTests
{
    private static readonly Variable X = new("x");

    [Fact]
    public void Evaluates_over_the_reals_and_complex_numbers()
    {
        Assert.Equal(Math.Sinh(0.7), ExprParser.Parse("sinh(x)").Evaluate(0.7));

        // sinh(i) = i * sin(1)
        ComplexNumber z = ExprParser.Parse("sinh(x)").EvaluateComplex(ComplexNumber.ImaginaryUnit);
        Assert.Equal(0, z.Real, precision: 12);
        Assert.Equal(Math.Sin(1), z.Imaginary, precision: 12);
    }

    [Fact]
    public void Differentiates_with_the_chain_rule()
    {
        Assert.Equal("cosh(x)", ExprParser.Parse("sinh(x)").Differentiate("x").Print());
        Assert.Equal("2cosh(2x)", ExprParser.Parse("sinh(2x)").Differentiate("x").Print());
    }

    [Fact]
    public void Prints_and_parses_back()
    {
        // Not "asinh(x)", which would parse as a different function.
        Expr e = new Multiply(new Variable("a"), new Sinh(X));
        Assert.Equal("a * sinh(x)", e.Print());

        // Parse returns the canonical tree.
        Assert.Equal(e.Canonicalize(), ExprParser.Parse(e.Print()));
        Assert.Equal(@"\sinh\left(x\right)", new Sinh(X).ToLatex());
    }
}
```

### 7. Documentation

- a `///` comment on the class (the build warns about missing ones);
- the function in the syntax table of [Epsilon.Core/docs/parsing.md](Epsilon.Core/docs/parsing.md) and
  [Epsilon.Core/README.md](Epsilon.Core/README.md);
- its derivative in [Epsilon.Core/docs/differentiation.md](Epsilon.Core/docs/differentiation.md);
- the node in [Epsilon.Core/docs/api-reference.md](Epsilon.Core/docs/api-reference.md) and
  [Epsilon.Core/docs/expressions.md](Epsilon.Core/docs/expressions.md#node-types);
- a line under a new version in `Epsilon.Core/CHANGELOG.md`: `- sinh, the hyperbolic sine.`

### 8. Commit and open a pull request

```bash
dotnet test
git add -A
git commit -m "feat(Functions): add sinh"
git push -u origin feature/sinh   # origin is your fork
```

Then open a pull request from `<you>:feature/sinh` into `XBambinoX/Epsilon:main`. CI
builds, tests and packs it.

## Releases

Releases are made by pushing a tag `core-vX.Y.Z` to `main`; the `publish` workflow builds the
`Epsilon` package and publishes it to NuGet. Every package has its own tag prefix, so the
modules built on the core can be released independently. The version follows
[Semantic Versioning](https://semver.org/).

## License

By contributing you agree that your contribution is licensed under the
[MIT License](LICENSE.txt).
