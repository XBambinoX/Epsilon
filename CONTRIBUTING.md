# Contributing to Epsilon

Bug reports, questions and pull requests are welcome on
[GitHub](https://github.com/XBambinoX/Epsilon).

## Reporting a bug

Include the package version, the smallest code or expression that shows the problem, and
what you got and expected. A wrong result, such as `Simplify` changing a value, is the most
serious kind of bug here. Check the [known limitations](Epsilon.Core/docs/limitations.md) first.

## Pull requests

You need the .NET 10 SDK; run `dotnet build` and `dotnet test` in the repository root.

- One change per pull request, with a test that fails without it.
- When in doubt, leave an expression unsimplified: a rule that holds only under a condition
  must check that condition.
- A public member needs a `///` comment and a line in the `docs/api-reference.md` of its
  package. The examples in the READMEs and guides are checked by tests, so change both together.
- Code, comments and commit messages are in English; commit messages follow
  [Conventional Commits](https://www.conventionalcommits.org/), such as `fix(Simplifier): ...`.

## License

By contributing you agree that your contribution is licensed under the
[MIT License](LICENSE.txt).
