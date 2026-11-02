namespace Epsilon.Calculus;

/// <summary>Whether a limit is a value or an infinity.</summary>
public enum LimitKind
{
    /// <summary>The expression tends to a value, <see cref="Limit.Value"/>.</summary>
    Finite,

    /// <summary>The expression grows without bound: inf.</summary>
    PositiveInfinity,

    /// <summary>The expression falls without bound: -inf.</summary>
    NegativeInfinity,
}
