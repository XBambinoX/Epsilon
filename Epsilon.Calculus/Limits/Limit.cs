using Epsilon.Core;

namespace Epsilon.Calculus;

/// <summary>
/// The limit of an expression: an exact value, or inf or -inf where the expression grows or falls
/// without bound.
/// </summary>
public sealed class Limit
{
    private readonly Expr? _value;

    private Limit(LimitKind kind, Expr? value)
    {
        Kind = kind;
        _value = value;
    }

    /// <summary>Whether the limit is a value or an infinity.</summary>
    public LimitKind Kind { get; }

    /// <summary>The value of a finite limit, simplified.</summary>
    /// <exception cref="InvalidOperationException">The limit is inf or -inf.</exception>
    public Expr Value => _value ?? throw new InvalidOperationException($"The limit is {Print()}, not a value.");

    /// <summary>The value, or <c>inf</c> or <c>-inf</c>.</summary>
    public string Print() => _value?.Print() ?? (Kind == LimitKind.PositiveInfinity ? "inf" : "-inf");

    /// <inheritdoc/>
    public override string ToString() => Print();

    internal static Limit Finite(Expr value) => new(LimitKind.Finite, value);

    internal static Limit Infinite(int sign) => new(sign > 0 ? LimitKind.PositiveInfinity : LimitKind.NegativeInfinity, null);

    internal Limit Negated() => _value is { } value ? Finite(SeriesExpander.Tidy(-value)) : Infinite(Kind == LimitKind.PositiveInfinity ? -1 : 1);
}
