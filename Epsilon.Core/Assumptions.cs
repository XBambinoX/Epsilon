namespace Epsilon.Core;

/// <summary>What is known about the sign of a variable.</summary>
public enum Signing
{
    /// <summary>Nothing is known.</summary>
    Unknown,
    /// <summary>Greater than 0.</summary>
    Positive,
    /// <summary>Less than 0.</summary>
    Negative,
    /// <summary>Exactly 0.</summary>
    Zero,
    /// <summary>Positive or negative.</summary>
    NonZero,
    /// <summary>Zero or positive.</summary>
    NonNegative,
    /// <summary>Zero or negative.</summary>
    NonPositive
}

/// <summary>
/// The number sets a variable is known to belong to. A flags enum: an integer variable is
/// <c>Integer | Rational | Real</c>.
/// </summary>
[Flags]
public enum NumberDomain
{
    /// <summary>Nothing is known.</summary>
    Unknown = 0,
    /// <summary>A real number.</summary>
    Real = 1,
    /// <summary>An integer.</summary>
    Integer = 2,
    /// <summary>A rational number.</summary>
    Rational = 4,
    /// <summary>A positive integer (1, 2, 3, ...).</summary>
    Natural = 8 // positive integers (1, 2, 3, ...)
}

/// <summary>
/// A single variable's known properties: its Signing and which number domains it belongs to.
/// Immutable - combine via VariableAssumption.Combine when merging multiple Assume() calls
/// for the same variable.
/// </summary>
public readonly struct VariableAssumption
{
    /// <summary>What is known about the sign.</summary>
    public Signing Signing { get; }
    /// <summary>The number sets the variable is known to belong to.</summary>
    public NumberDomain Domain { get; }

    /// <summary>An assumption with the given sign and domain. Prefer the <see cref="Assumptions"/> builder methods.</summary>
    public VariableAssumption(Signing signing, NumberDomain domain)
    {
        Signing = signing;
        Domain = domain;
    }

    /// <summary>No knowledge about the variable.</summary>
    public static readonly VariableAssumption Unknown = new(Signing.Unknown, NumberDomain.Unknown);

    /// <summary>
    /// Merges this assumption with another for the same variable. Both must hold at once,
    /// so the result is always at least as narrow as either input: Positive + NonZero is
    /// Positive, NonNegative + NonPositive is Zero. Domains accumulate (Integer + Real is
    /// both), and Natural implies Positive.
    /// </summary>
    /// <exception cref="ArgumentException">The two assumptions contradict each other,
    /// e.g. Positive + Negative, or Natural + NonPositive.</exception>
    public VariableAssumption Combine(VariableAssumption other)
    {
        NumberDomain domain = Domain | other.Domain;

        SignSet signs = ToSignSet(Signing) & ToSignSet(other.Signing);
        if ((domain & NumberDomain.Natural) != 0)
            signs &= SignSet.Positive;

        if (signs == SignSet.None)
            throw new ArgumentException(
                $"Contradictory assumptions: {Describe(this)} and {Describe(other)} cannot both hold.");

        return new VariableAssumption(FromSignSet(signs), domain);
    }

    // Signing as the set of signs a value may still have; combining is set intersection.
    [Flags]
    private enum SignSet
    {
        None = 0,
        Negative = 1,
        Zero = 2,
        Positive = 4,
        Any = Negative | Zero | Positive
    }

    private static SignSet ToSignSet(Signing signing) => signing switch
    {
        Signing.Positive => SignSet.Positive,
        Signing.Negative => SignSet.Negative,
        Signing.Zero => SignSet.Zero,
        Signing.NonZero => SignSet.Negative | SignSet.Positive,
        Signing.NonNegative => SignSet.Zero | SignSet.Positive,
        Signing.NonPositive => SignSet.Negative | SignSet.Zero,
        _ => SignSet.Any
    };

    private static Signing FromSignSet(SignSet signs) => signs switch
    {
        SignSet.Positive => Signing.Positive,
        SignSet.Negative => Signing.Negative,
        SignSet.Zero => Signing.Zero,
        SignSet.Negative | SignSet.Positive => Signing.NonZero,
        SignSet.Zero | SignSet.Positive => Signing.NonNegative,
        SignSet.Negative | SignSet.Zero => Signing.NonPositive,
        _ => Signing.Unknown
    };

    private static string Describe(VariableAssumption a) =>
        a.Domain == NumberDomain.Unknown ? a.Signing.ToString() : $"{a.Signing} {a.Domain}";
}

/// <summary>
/// An immutable set of assumptions about variables - e.g. "x is a positive real number",
/// "n is an integer". Used to justify simplification steps that are only valid under
/// certain conditions (e.g. sqrt(x^2) = x requires x >= 0; without that assumption the
/// only generally correct simplification is sqrt(x^2) = |x|).
///
/// Building an Assumptions set never mutates an existing one - each Assume* call
/// returns a new instance, the same immutability philosophy as Expr itself.
/// </summary>
public sealed class Assumptions
{
    /// <summary>The empty set of assumptions: nothing is known about any variable.</summary>
    public static readonly Assumptions None = new(new Dictionary<string, VariableAssumption>());

    private readonly IReadOnlyDictionary<string, VariableAssumption> _byVariable;

    private Assumptions(IReadOnlyDictionary<string, VariableAssumption> byVariable) =>
        _byVariable = byVariable;

    /// <summary>
    /// A copy of these assumptions that also assumes <paramref name="domain"/> and
    /// <paramref name="signing"/> for <paramref name="variable"/>, combined with what is already
    /// known about it (see <see cref="VariableAssumption.Combine"/>).
    /// </summary>
    /// <example><c>Assumptions.None.Assume("x", signing: Signing.Positive)</c></example>
    /// <exception cref="ArgumentException">The new assumption contradicts an existing one for the variable.</exception>
    public Assumptions Assume(string variable, NumberDomain domain = NumberDomain.Unknown, Signing signing = Signing.Unknown)
    {
        var incoming = new VariableAssumption(signing, domain);
        var existing = _byVariable.TryGetValue(variable, out var current) ? current : VariableAssumption.Unknown;

        VariableAssumption combined;
        try
        {
            combined = existing.Combine(incoming);
        }
        catch (ArgumentException ex)
        {
            // Same error, but naming the variable - Combine itself doesn't know it.
            throw new ArgumentException($"Variable '{variable}': {ex.Message}", nameof(variable), ex);
        }

        var updated = new Dictionary<string, VariableAssumption>(_byVariable)
        {
            [variable] = combined
        };

        return new Assumptions(updated);
    }

    // Every integer is rational and every natural number is an integer, so the wider
    // domains are always included - otherwise Has(n, Rational) would be false for an integer n.
    /// <summary>A copy that also assumes <paramref name="variable"/> is real.</summary>
    public Assumptions AssumeReal(string variable) => Assume(variable, domain: NumberDomain.Real);
    /// <summary>A copy that also assumes <paramref name="variable"/> is an integer (and so rational and real).</summary>
    public Assumptions AssumeInteger(string variable) => Assume(variable, domain: NumberDomain.Integer | NumberDomain.Rational | NumberDomain.Real);
    /// <summary>A copy that also assumes <paramref name="variable"/> is rational (and so real).</summary>
    public Assumptions AssumeRational(string variable) => Assume(variable, domain: NumberDomain.Rational | NumberDomain.Real);
    /// <summary>A copy that also assumes <paramref name="variable"/> is a positive integer.</summary>
    /// <exception cref="ArgumentException">The variable is already assumed to be zero or negative.</exception>
    public Assumptions AssumeNatural(string variable) => Assume(variable, domain: NumberDomain.Natural | NumberDomain.Integer | NumberDomain.Rational | NumberDomain.Real, signing: Signing.Positive);

    /// <summary>A copy that also assumes <paramref name="variable"/> &gt; 0.</summary>
    /// <exception cref="ArgumentException">This contradicts an existing assumption for the variable.</exception>
    public Assumptions AssumePositive(string variable) => Assume(variable, signing: Signing.Positive);
    /// <summary>A copy that also assumes <paramref name="variable"/> &lt; 0.</summary>
    /// <exception cref="ArgumentException">This contradicts an existing assumption for the variable.</exception>
    public Assumptions AssumeNegative(string variable) => Assume(variable, signing: Signing.Negative);
    /// <summary>A copy that also assumes <paramref name="variable"/> ≠ 0.</summary>
    /// <exception cref="ArgumentException">This contradicts an existing assumption for the variable.</exception>
    public Assumptions AssumeNonZero(string variable) => Assume(variable, signing: Signing.NonZero);
    /// <summary>A copy that also assumes <paramref name="variable"/> ≥ 0.</summary>
    /// <exception cref="ArgumentException">This contradicts an existing assumption for the variable.</exception>
    public Assumptions AssumeNonNegative(string variable) => Assume(variable, signing: Signing.NonNegative);
    /// <summary>A copy that also assumes <paramref name="variable"/> ≤ 0.</summary>
    /// <exception cref="ArgumentException">This contradicts an existing assumption for the variable.</exception>
    public Assumptions AssumeNonPositive(string variable) => Assume(variable, signing: Signing.NonPositive);

    /// <summary>Everything assumed about <paramref name="variable"/>; <see cref="VariableAssumption.Unknown"/> if nothing.</summary>
    public VariableAssumption Get(string variable) =>
        _byVariable.TryGetValue(variable, out var a) ? a : VariableAssumption.Unknown;

    /// <summary>What is assumed about the sign of <paramref name="variable"/>.</summary>
    public Signing SigningOf(string variable) => Get(variable).Signing;

    /// <summary>Whether <paramref name="variable"/> is assumed &gt; 0.</summary>
    public bool IsPositive(string variable) => SigningOf(variable) == Signing.Positive;
    /// <summary>Whether <paramref name="variable"/> is assumed &lt; 0.</summary>
    public bool IsNegative(string variable) => SigningOf(variable) == Signing.Negative;

    /// <summary>Whether <paramref name="variable"/> is assumed ≥ 0 (positive, zero or non-negative).</summary>
    public bool IsNonNegative(string variable) =>
        SigningOf(variable) is Signing.Positive or Signing.NonNegative or Signing.Zero;

    /// <summary>Whether <paramref name="variable"/> is assumed ≤ 0 (negative, zero or non-positive).</summary>
    public bool IsNonPositive(string variable) =>
        SigningOf(variable) is Signing.Negative or Signing.NonPositive or Signing.Zero;

    /// <summary>Whether <paramref name="variable"/> is assumed ≠ 0 (positive, negative or non-zero).</summary>
    public bool IsNonZero(string variable) =>
        SigningOf(variable) is Signing.Positive or Signing.Negative or Signing.NonZero;

    /// <summary>Whether <paramref name="variable"/> is assumed to be in every set of <paramref name="domain"/>.</summary>
    public bool Has(string variable, NumberDomain domain) =>
        (Get(variable).Domain & domain) == domain;

    /// <summary>Whether <paramref name="variable"/> is assumed real.</summary>
    public bool IsReal(string variable) => Has(variable, NumberDomain.Real);
    /// <summary>Whether <paramref name="variable"/> is assumed an integer.</summary>
    public bool IsInteger(string variable) => Has(variable, NumberDomain.Integer);
}