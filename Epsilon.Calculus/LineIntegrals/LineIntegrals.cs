using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Calculus;

/// <summary>
/// Line integrals along curves: of a scalar field by arc length, and of a vector field along
/// the direction of the curve, the work it does, numeric and exact.
/// </summary>
public static class LineIntegralExtensions
{
    // Classic extension methods, as in VectorFieldExtensions: params in an extension block gives
    // a false CS8620 at every call.
    private const string Parameter = "u'";

    /// <summary>
    /// The integral of <paramref name="f"/> by arc length along the curve, computed numerically:
    /// the sum over its pieces of the integral of f(r(t)) |r'(t)| for t from a to b.
    /// <paramref name="variables"/> name the entries of the curve's points, x and y for
    /// <c>[cos(t), sin(t)]</c>; with f = 1 it is the length of the curve. It doesn't depend on
    /// the direction the curve is traversed in.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The variables and the entries of the curve's points differ in number, a name is listed
    /// twice, the points have one of the variables in them, a limit of the parameter isn't a
    /// number, or the integrand has a variable besides them.
    /// </exception>
    /// <exception cref="InvalidOperationException">The integral diverges or can't be computed, as for numeric integrals.</exception>
    public static double LineIntegral(this Expr f, Curve curve, params IReadOnlyList<string> variables) =>
        Numeric(ByArcLength(f, curve, variables));

    /// <summary>
    /// The integral of <paramref name="f"/> by arc length along the curve, exact: the length of
    /// the unit circle, of f = 1, is 2 pi. See <see cref="LineIntegral(Expr, Curve, IReadOnlyList{string})"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The variables and the entries of the curve's points differ in number, a name is listed
    /// twice, or the points have one of the variables in them.
    /// </exception>
    /// <exception cref="InvalidOperationException">The integral diverges, as for exact integrals.</exception>
    /// <exception cref="NotSupportedException">The integral isn't found, as the length of an ellipse, which is elliptic.</exception>
    public static Expr LineIntegralExact(this Expr f, Curve curve, params IReadOnlyList<string> variables) =>
        Exact(ByArcLength(f, curve, variables));

    /// <summary>
    /// The integral of the vector field along the curve, F . dr, computed numerically: the sum
    /// over its pieces of the integral of F(r(t)) . r'(t) for t from a to b, the work the field
    /// does along it, and around a closed curve its circulation. Entry i of the field is its
    /// component along <c>variables[i]</c>. Traversed the other way, the integral changes sign.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The field, the variables and the entries of the curve's points differ in number, a name is
    /// listed twice, the points have one of the variables in them, a limit of the parameter isn't
    /// a number, or the field has a variable besides them.
    /// </exception>
    /// <exception cref="InvalidOperationException">The integral diverges or can't be computed, as for numeric integrals.</exception>
    public static double LineIntegral(this Vector<Expr> field, Curve curve, params IReadOnlyList<string> variables) =>
        Numeric(Along(field, curve, variables));

    /// <summary>
    /// The integral of the vector field along the curve, F . dr, exact: [-y, x] around the unit
    /// circle counterclockwise is 2 pi. See <see cref="LineIntegral(Vector{Expr}, Curve, IReadOnlyList{string})"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The field, the variables and the entries of the curve's points differ in number, a name is
    /// listed twice, or the points have one of the variables in them.
    /// </exception>
    /// <exception cref="InvalidOperationException">The integral diverges, as for exact integrals.</exception>
    /// <exception cref="NotSupportedException">The integral isn't found.</exception>
    public static Expr LineIntegralExact(this Vector<Expr> field, Curve curve, params IReadOnlyList<string> variables) =>
        Exact(Along(field, curve, variables));

    // A piece of the curve with the integrand in its parameter, and the name the curve gave that.
    private readonly record struct Piece(Expr Integrand, Expr From, Expr To, string Name)
    {
        public string Print() =>
            $"the integral of {Integrand.Substitute(Parameter, new Variable(Name)).Print()} by {Name} from {From.Print()} to {To.Print()}";
    }

    private static List<Piece> ByArcLength(Expr f, Curve curve, IReadOnlyList<string> variables)
    {
        ArgumentNullException.ThrowIfNull(f);
        return Pieces(Vector.Create(f), curve, variables, (at, velocity) => at[0] * new Sqrt(velocity.Dot(velocity)).Simplify());
    }

    private static List<Piece> Along(Vector<Expr> field, Curve curve, IReadOnlyList<string> variables)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(variables);
        if (field.Length != variables.Count)
            throw new ArgumentException($"The field has {field.Length} entries but there are {variables.Count} variables.", nameof(variables));

        return Pieces(field, curve, variables, (at, velocity) => at.Dot(velocity));
    }

    // The values at r(t), put together with r'(t) into the integrand of each piece.
    private static List<Piece> Pieces(Vector<Expr> values, Curve curve, IReadOnlyList<string> variables, Func<Vector<Expr>, Vector<Expr>, Expr> integrand)
    {
        ArgumentNullException.ThrowIfNull(curve);
        string[] names = VariableNames.Checked(variables);
        if (curve.Dimension != names.Length)
            throw new ArgumentException($"The curve's points have {curve.Dimension} entries but there are {names.Length} variables.", nameof(variables));

        var pieces = new List<Piece>();
        foreach (Curve.Piece piece in curve.Pieces)
        {
            Vector<Expr> point = piece.Point.Map(entry => entry.Substitute(piece.Parameter, new Variable(Parameter)));
            if (names.FirstOrDefault(name => point.Any(entry => entry.DependsOn(name))) is { } shared)
                throw new ArgumentException($"The curve's points have the variable {shared} in them: name the parameter and the variables apart.", nameof(curve));

            Vector<Expr> at = values.Map(value => Enumerable.Range(0, names.Length).Aggregate(value, (e, i) => e.Substitute(names[i], point[i])));
            pieces.Add(new Piece(integrand(at, point.Differentiate(Parameter)).Simplify(), piece.From, piece.To, piece.Parameter));
        }

        return pieces;
    }

    private static double Numeric(List<Piece> pieces)
    {
        foreach (Piece piece in pieces)
            if (piece.Integrand.GetVariables().FirstOrDefault(name => name != Parameter) is { } other)
                throw new ArgumentException($"The integrand has the variable {other} besides those of the curve: substitute a value for it first.");

        return pieces.Sum(piece => piece.Integrand.Integrate(Parameter, Number(piece.From), Number(piece.To)));
    }

    private static Expr Exact(List<Piece> pieces) =>
        SeriesExpander.Tidy(pieces.Aggregate((Expr)0, (sum, piece) => sum + Exact(piece)));

    private static Expr Exact(Piece piece)
    {
        try
        {
            return piece.Integrand.IntegrateExact(Parameter, piece.From, piece.To);
        }
        catch (InvalidOperationException e)
        {
            throw new InvalidOperationException($"The line integral doesn't exist: {piece.Print()} diverges or isn't real.", e);
        }
        catch (NotSupportedException e)
        {
            throw new NotSupportedException($"The line integral isn't found: {piece.Print()} isn't found.", e);
        }
    }

    private static double Number(Expr limit) => limit.GetVariables().Count == 0
        ? limit.Evaluate()
        : throw new ArgumentException($"The limit {limit.Print()} of the curve's parameter isn't a number.");
}
