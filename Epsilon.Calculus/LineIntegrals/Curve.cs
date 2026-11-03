using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Calculus;

/// <summary>
/// A curve to integrate along: one piece or several, each a point whose entries are expressions
/// of a parameter, for the parameter from one value to another, traversed in order.
/// </summary>
public sealed class Curve
{
    private readonly IReadOnlyList<Piece> _pieces;

    private Curve(IReadOnlyList<Piece> pieces) => _pieces = pieces;

    /// <summary>
    /// The curve <paramref name="point"/> for <paramref name="parameter"/> from
    /// <paramref name="from"/> to <paramref name="to"/>, in that direction: the unit circle
    /// counterclockwise is <c>new Curve("t", [cos(t), sin(t)], 0, 2 * new Pi())</c>. The limits
    /// are in increasing order; <see cref="Reversed"/> traverses the curve the other way.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The point has no entries, a limit depends on the parameter, or <paramref name="from"/> is
    /// greater than <paramref name="to"/>.
    /// </exception>
    public Curve(string parameter, Vector<Expr> point, Expr from, Expr to)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        ArgumentNullException.ThrowIfNull(point);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (point.Length == 0)
            throw new ArgumentException("The point of a curve needs at least one entry.", nameof(point));
        if (from.DependsOn(parameter) || to.DependsOn(parameter))
            throw new ArgumentException($"A limit of the parameter depends on {parameter}.");

        Expr low = from.Simplify(), high = to.Simplify();
        if (low.GetVariables().Count == 0 && high.GetVariables().Count == 0 && low.Evaluate() > high.Evaluate())
            throw new ArgumentException(
                $"The parameter runs from {low.Print()} to {high.Print()}: give the limits in increasing order, and take Reversed() for the other direction.");

        _pieces = [new Piece(parameter, point.Map(entry => entry.Simplify()), low, high)];
    }

    /// <summary>The number of entries of its points: 2 in the plane, 3 in space.</summary>
    public int Dimension => _pieces[0].Point.Length;

    /// <summary>The point it starts at.</summary>
    public Vector<Expr> Start => _pieces[0].At(_pieces[0].From);

    /// <summary>The point it ends at.</summary>
    public Vector<Expr> End => _pieces[^1].At(_pieces[^1].To);

    internal IReadOnlyList<Piece> Pieces => _pieces;

    /// <summary>The straight segment from <paramref name="from"/> to <paramref name="to"/>.</summary>
    /// <exception cref="ArgumentException">The points have different numbers of entries, or none.</exception>
    public static Curve Segment(Vector<Expr> from, Vector<Expr> to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (from.Length != to.Length)
            throw new ArgumentException($"The points have {from.Length} and {to.Length} entries.");

        // t, unless the points have a t of their own.
        var t = new Variable(from.Concat(to).Any(entry => entry.DependsOn("t")) ? "t'" : "t");
        return new Curve(t.Name, Vector.Create([.. Enumerable.Range(0, from.Length).Select(i => from[i] + t * (to[i] - from[i]))]), 0, 1);
    }

    /// <summary>
    /// The segments from each point to the next, and with <paramref name="closed"/> from the last
    /// back to the first: <c>Curve.Polygon([[0, 0], [1, 0], [0, 1]], closed: true)</c> is the
    /// boundary of a triangle, counterclockwise.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// There are fewer than 2 points, or they have different numbers of entries.
    /// </exception>
    public static Curve Polygon(IReadOnlyList<Vector<Expr>> points, bool closed = false)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
            throw new ArgumentException("A polygon needs at least 2 points.", nameof(points));

        var segments = Enumerable.Range(0, points.Count - 1).Select(i => Segment(points[i], points[i + 1])).ToList();
        if (closed)
            segments.Add(Segment(points[^1], points[0]));

        return Join(segments);
    }

    /// <summary>The curves one after another, as one curve.</summary>
    /// <exception cref="ArgumentException">There are no curves, or they have different dimensions.</exception>
    public static Curve Join(params IReadOnlyList<Curve> curves)
    {
        ArgumentNullException.ThrowIfNull(curves);
        if (curves.Count == 0)
            throw new ArgumentException("There are no curves to join.", nameof(curves));
        if (curves.Any(curve => curve.Dimension != curves[0].Dimension))
            throw new ArgumentException("The curves have different dimensions.", nameof(curves));

        return new Curve([.. curves.SelectMany(curve => curve._pieces)]);
    }

    /// <summary>
    /// The same curve traversed the other way: each piece at from + to - t, in reverse order.
    /// </summary>
    public Curve Reversed() =>
        new([.. _pieces.Reverse().Select(piece => piece with
        {
            Point = piece.Point.Map(entry => entry.Substitute(piece.Parameter, piece.From + piece.To - new Variable(piece.Parameter)).Simplify()),
        })]);

    internal readonly record struct Piece(string Parameter, Vector<Expr> Point, Expr From, Expr To)
    {
        public Vector<Expr> At(Expr value)
        {
            string parameter = Parameter;
            return Point.Map(entry => entry.Substitute(parameter, value).Simplify());
        }
    }
}
