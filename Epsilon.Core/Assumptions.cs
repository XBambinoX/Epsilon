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
    /// Merges this assumption with another for the same variable. Signing/Domain from
    /// the incoming assumption wins when both specify something other than Unknown/0,
    /// so calling Assume(...) again for the same variable narrows rather than resets.
    /// </summary>
    public VariableAssumption Combine(VariableAssumption other) => new(
        other.Signing != Signing.Unknown ? other.Signing : Signing,
        Domain | other.Domain
    );
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

    public Assumptions Assume(string variable, NumberDomain domain = NumberDomain.Unknown, Signing Signing = Signing.Unknown)
    {
        var incoming = new VariableAssumption(Signing, domain);
        var existing = _byVariable.TryGetValue(variable, out var current) ? current : VariableAssumption.Unknown;

        var updated = new Dictionary<string, VariableAssumption>(_byVariable)
        {
            [variable] = existing.Combine(incoming)
        };

        return new Assumptions(updated);
    }

    public Assumptions AssumeReal(string variable) => Assume(variable, domain: NumberDomain.Real);
    public Assumptions AssumeInteger(string variable) => Assume(variable, domain: NumberDomain.Integer | NumberDomain.Real);
    public Assumptions AssumeRational(string variable) => Assume(variable, domain: NumberDomain.Rational | NumberDomain.Real);
    public Assumptions AssumeNatural(string variable) => Assume(variable, domain: NumberDomain.Natural | NumberDomain.Integer | NumberDomain.Real, Signing: Signing.Positive);

    public Assumptions AssumePositive(string variable) => Assume(variable, Signing: Signing.Positive);
    public Assumptions AssumeNegative(string variable) => Assume(variable, Signing: Signing.Negative);
    public Assumptions AssumeNonZero(string variable) => Assume(variable, Signing: Signing.NonZero);
    public Assumptions AssumeNonNegative(string variable) => Assume(variable, Signing: Signing.NonNegative);
    public Assumptions AssumeNonPositive(string variable) => Assume(variable, Signing: Signing.NonPositive);

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