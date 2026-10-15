using Epsilon.Core;

namespace Epsilon.Tests.Other;

// Deliberately shares its short name with Epsilon.Core.Sin, to test that canonical
// ordering tells node types apart by full name.
public sealed class Sin(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        Math.Sin(Argument.Evaluate(bindings));

    protected override Expr DifferentiateCore(string variable) =>
        throw new NotSupportedException("Test-only node.");

    protected override Expr WithArgument(Expr argument) => new Sin(argument);
}
