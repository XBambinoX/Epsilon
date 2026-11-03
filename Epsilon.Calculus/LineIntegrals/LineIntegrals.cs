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
        return Pieces(Vector.Create(f), curve, variables, (at, velocity) => at[0] * Positive(Root(Pythagorean(velocity.Dot(velocity).Simplify()))));
    }

    // sqrt(c u^2) as sqrt(c) abs(u), which the simplifier leaves as it is.
    private static Expr Root(Expr square)
    {
        var factors = new List<Expr>();
        Factors(square, factors);
        Expr outside = 1, inside = 1;
        foreach (Expr factor in factors)
        {
            if (factor is Power { Exponent: Constant { Value: { IsInteger: true } k } } power && k.Numerator.IsEven && k.Sign > 0)
                outside *= new Power(new Abs(power.Base), new Constant(k / 2));
            else
                inside *= factor;
        }

        return (outside * new Sqrt(inside)).Simplify();
    }

    // abs of cosh, exp and sech, which are positive, as they are.
    private static Expr Positive(Expr e) => e switch
    {
        Abs { Argument: Cosh or Exp or Sech } abs => abs.Argument,
        _ when e.Children.Length > 0 => e.WithChildren([.. e.Children.Select(Positive)]),
        _ => e,
    };

    // c sin(u)^2 + c cos(u)^2 = c and c + c sinh(u)^2 = c cosh(u)^2, which the simplifier sees
    // for a number c only: the speed on a circle of radius R is sqrt(R^2 sin(t)^2 + R^2 cos(t)^2)
    // = abs(R), and on the catenary sqrt(1 + sinh(t)^2) = cosh(t).
    private static Expr Pythagorean(Expr e)
    {
        var terms = new List<Expr>();
        Terms(e, terms);
        var used = new bool[terms.Count];
        Expr sum = 0;
        for (int i = 0; i < terms.Count; i++)
        {
            if (used[i] || Square(terms[i]) is not var (wave, factor))
                continue;

            for (int j = 0; j < terms.Count; j++)
            {
                if (j == i || used[j])
                    continue;

                Expr? pair = wave is Sin or Cos && Square(terms[j]) is var (other, otherFactor) && other is Sin or Cos
                    && other.GetType() != wave.GetType() && other.Argument.Equals(wave.Argument) && otherFactor.Equals(factor)
                    ? factor
                    : wave is Sinh && terms[j].Simplify().Equals(factor) ? factor * new Power(new Cosh(wave.Argument), 2) : null;
                if (pair is not null)
                {
                    (used[i], used[j]) = (true, true);
                    sum += pair;
                    break;
                }
            }
        }

        for (int i = 0; i < terms.Count; i++)
            if (!used[i])
                sum += terms[i];

        return sum.Simplify();
    }

    private static void Terms(Expr e, List<Expr> terms)
    {
        switch (e)
        {
            case Add add:
                Terms(add.Left, terms);
                Terms(add.Right, terms);
                break;
            case Subtract subtract:
                Terms(subtract.Left, terms);
                terms.Add(-subtract.Right);
                break;
            default:
                terms.Add(e);
                break;
        }
    }

    // A term as sin(u)^2, cos(u)^2 or sinh(u)^2 times a factor.
    private static (UnaryExpr Wave, Expr Factor)? Square(Expr term)
    {
        var factors = new List<Expr>();
        Factors(term, factors);
        int i = factors.FindIndex(factor => factor is Power { Base: Sin or Cos or Sinh, Exponent: Constant { Value: var two } } && two == 2);
        if (i < 0)
            return null;

        var wave = (UnaryExpr)((Power)factors[i]).Base;
        factors.RemoveAt(i);
        return (wave, factors.Aggregate((Expr)1, (product, factor) => product * factor).Simplify());
    }

    private static void Factors(Expr e, List<Expr> factors)
    {
        switch (e)
        {
            case Multiply product:
                Factors(product.Left, factors);
                Factors(product.Right, factors);
                break;
            case Negate negate:
                factors.Add(-1);
                Factors(negate.Argument, factors);
                break;
            default:
                factors.Add(e);
                break;
        }
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
