namespace Epsilon.Core;

public enum Signing
{
    Unknown,
    Positive,
    Negative,
    Zero,
    NonZero,
    NonNegative,
    NonPositive
}

[Flags]
public enum NumberDomain
{
    Unknown = 0,
    Real = 1,
    Integer = 2,
    Rational = 4,
    Natural = 8 // positive integers (1, 2, 3, ...)
}

/// <summary>
/// A single variable's known properties: its Signing and which number domains it belongs to.
/// Immutable - combine via VariableAssumption.Combine when merging multiple Assume() calls
/// for the same variable.
/// </summary>
public readonly struct VariableAssumption
{
    public Signing Signing { get; }
    public NumberDomain Domain { get; }

    public VariableAssumption(Signing signing, NumberDomain domain)
    {
        Signing = signing;
        Domain = domain;
    }

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
    public static readonly Assumptions None = new(new Dictionary<string, VariableAssumption>());

    private readonly IReadOnlyDictionary<string, VariableAssumption> _byVariable;

    private Assumptions(IReadOnlyDictionary<string, VariableAssumption> byVariable) =>
        _byVariable = byVariable;

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
    public Assumptions AssumeReal(string variable) => Assume(variable, domain: NumberDomain.Real);
    public Assumptions AssumeInteger(string variable) => Assume(variable, domain: NumberDomain.Integer | NumberDomain.Rational | NumberDomain.Real);
    public Assumptions AssumeRational(string variable) => Assume(variable, domain: NumberDomain.Rational | NumberDomain.Real);
    public Assumptions AssumeNatural(string variable) => Assume(variable, domain: NumberDomain.Natural | NumberDomain.Integer | NumberDomain.Rational | NumberDomain.Real, signing: Signing.Positive);

    public Assumptions AssumePositive(string variable) => Assume(variable, signing: Signing.Positive);
    public Assumptions AssumeNegative(string variable) => Assume(variable, signing: Signing.Negative);
    public Assumptions AssumeNonZero(string variable) => Assume(variable, signing: Signing.NonZero);
    public Assumptions AssumeNonNegative(string variable) => Assume(variable, signing: Signing.NonNegative);
    public Assumptions AssumeNonPositive(string variable) => Assume(variable, signing: Signing.NonPositive);

    public VariableAssumption Get(string variable) =>
        _byVariable.TryGetValue(variable, out var a) ? a : VariableAssumption.Unknown;

    public Signing SigningOf(string variable) => Get(variable).Signing;

    public bool IsPositive(string variable) => SigningOf(variable) == Signing.Positive;
    public bool IsNegative(string variable) => SigningOf(variable) == Signing.Negative;

    public bool IsNonNegative(string variable) =>
        SigningOf(variable) is Signing.Positive or Signing.NonNegative or Signing.Zero;

    public bool IsNonPositive(string variable) =>
        SigningOf(variable) is Signing.Negative or Signing.NonPositive or Signing.Zero;

    public bool IsNonZero(string variable) =>
        SigningOf(variable) is Signing.Positive or Signing.Negative or Signing.NonZero;

    public bool Has(string variable, NumberDomain domain) =>
        (Get(variable).Domain & domain) == domain;

    public bool IsReal(string variable) => Has(variable, NumberDomain.Real);
    public bool IsInteger(string variable) => Has(variable, NumberDomain.Integer);
}