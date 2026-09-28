namespace Epsilon.Core;

/// <summary>
/// Base for nodes with exactly one sub-expression: negation and the one-argument
/// functions (sin, exp, abs, ...). A derived node only has to say how to build a copy
/// of itself around a new argument (<see cref="WithArgument"/>); the tree plumbing -
/// Children, WithChildren, GetVariables, Substitute, equality - comes from here and Expr.
/// </summary>
public abstract class UnaryExpr(Expr argument) : Expr
{
    private readonly ImmutableArray<Expr> _children = [argument];

    /// <summary>The single sub-expression.</summary>
    public Expr Argument { get; } = argument;

    /// <inheritdoc/>
    public sealed override ImmutableArray<Expr> Children => _children;

    /// <inheritdoc/>
    public sealed override Expr WithChildren(IReadOnlyList<Expr> children)
    {
        if (children.Count != 1)
            throw ChildCountMismatch(1, children.Count);

        return ReferenceEquals(children[0], Argument) ? this : WithArgument(children[0]);
    }

    /// <summary>A node of the same type around <paramref name="argument"/>.</summary>
    protected abstract Expr WithArgument(Expr argument);

    /// <summary>Deconstructs the node for positional patterns, e.g. <c>case Sin(var x):</c>.</summary>
    public void Deconstruct(out Expr argument) => argument = Argument;
}
