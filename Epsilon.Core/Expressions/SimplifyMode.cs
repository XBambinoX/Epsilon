namespace Epsilon.Core;

/// <summary>
/// Controls how aggressively <see cref="Simplifier"/> may rewrite an expression
/// with respect to its domain (the set of points where it is defined).
/// </summary>
public enum SimplifyMode
{
    /// <summary>
    /// The result equals the original wherever the original is defined, but may also be
    /// defined at extra points. Examples: x/x = 1, tan(x)*cot(x) = 1, sqrt(x)^2 = x.
    /// Values never change where the original is defined, so sqrt(x^2) still becomes |x|,
    /// and 0/0 is never folded. This is the default - it is what most users expect.
    /// </summary>
    Generic,

    /// <summary>
    /// The result equals the original and has exactly the same domain. Domain-enlarging
    /// rewrites only apply when <see cref="Assumptions"/> prove them safe (e.g. x/x = 1
    /// requires x to be provably nonzero). Use this when singular points matter, such as
    /// root finding or checking where an expression is undefined.
    /// </summary>
    Strict
}
