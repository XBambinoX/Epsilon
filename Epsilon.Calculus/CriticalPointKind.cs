namespace Epsilon.Calculus;

/// <summary>What the second-derivative test tells about a critical point.</summary>
public enum CriticalPointKind
{
    /// <summary>Every eigenvalue of the Hessian is positive: a strict local minimum.</summary>
    LocalMinimum,

    /// <summary>Every eigenvalue of the Hessian is negative: a strict local maximum.</summary>
    LocalMaximum,

    /// <summary>The Hessian has eigenvalues of both signs: neither a minimum nor a maximum.</summary>
    Saddle,

    /// <summary>
    /// The Hessian has an eigenvalue of 0 and none of opposite signs, so the second derivatives
    /// can't tell: <c>x^4</c>, <c>-x^4</c> and <c>x^3</c> at 0 all land here.
    /// </summary>
    Inconclusive,
}
