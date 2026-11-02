namespace Epsilon.Calculus;

/// <summary>The side a limit at a point is taken from.</summary>
public enum LimitDirection
{
    /// <summary>From both sides: the limit exists where the two agree.</summary>
    Both,

    /// <summary>From the left, x &lt; a.</summary>
    Left,

    /// <summary>From the right, x &gt; a.</summary>
    Right,
}
