using System.Globalization;
using Xunit;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class FixedVectorTests
{
    [Fact]
    public void Constructors_tuples_and_parts()
    {
        var v = new Vector3<double>(1, 2, 3);
        Vector3<double> fromTuple = (1, 2, 3);
        Vector4<double> h = (1, 2, 3, 4);

        Assert.Equal(v, fromTuple);
        Assert.Equal((1.0, 2.0, 3.0), (v.X, v.Y, v.Z));
        Assert.Equal(v, new Vector3<double>(new Vector2<double>(1, 2), 3));
        Assert.Equal(new Vector4<double>(1, 2, 3, 1), new Vector4<double>(v, 1));
        Assert.Equal(new Vector2<double>(1, 2), v.XY);
        Assert.Equal(v, h.XYZ);
        Assert.Equal(new Vector2<double>(1, 2), h.XY);
        Assert.Equal(Vector3<double>.Zero, default);
    }

    [Fact]
    public void With_changes_one_entry_of_a_copy()
    {
        Vector3<double> v = (1, 2, 3);

        Assert.Equal(new Vector3<double>(1, 0, 3), v with { Y = 0 });
        Assert.Equal(new Vector3<double>(1, 2, 3), v);
    }

    [Fact]
    public void Deconstruction_and_indexer()
    {
        var (x, y, z) = new Vector3<double>(1, 2, 3);
        Vector4<int> v = (5, 6, 7, 8);

        Assert.Equal((1.0, 2.0, 3.0), (x, y, z));
        Assert.Equal([5, 6, 7, 8], new[] { v[0], v[1], v[2], v[3] });

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => v[4]);
        Assert.StartsWith("The vector has 4 entries.", ex.Message);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vector2<double>(1, 2)[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vector3<double>(1, 2, 3)[3]);
    }

    [Fact]
    public void Constants()
    {
        Assert.Equal(new Vector3<double>(0, 0, 0), Vector3<double>.Zero);
        Assert.Equal(new Vector3<double>(1, 1, 1), Vector3<double>.One);
        Assert.Equal(new Vector2<float>(0, 1), Vector2<float>.UnitY);
        Assert.Equal(new Vector3<double>(0, 0, 1), Vector3<double>.UnitZ);
        Assert.Equal(new Vector4<double>(0, 0, 0, 1), Vector4<double>.UnitW);
        Assert.Equal(new Vector4<int>(1, 1, 1, 1), Vector4<int>.One);
    }

    [Fact]
    public void Arithmetic()
    {
        Vector3<double> a = (1, 2, 3), b = (4, 5, 6);

        Assert.Equal(new Vector3<double>(5, 7, 9), a + b);
        Assert.Equal(new Vector3<double>(-3, -3, -3), a - b);
        Assert.Equal(new Vector3<double>(-1, -2, -3), -a);
        Assert.Equal(new Vector3<double>(2, 4, 6), 2 * a);
        Assert.Equal(new Vector3<double>(2, 4, 6), a * 2);
        Assert.Equal(new Vector3<double>(0.5, 1, 1.5), a / 2);
        Assert.Equal(new Vector3<double>(2, 2, 3), a + (1, 0, 0));
        Assert.Equal(new Vector3<double>(4, 10, 18), a.Hadamard(b));
        Assert.Equal(a, a.Hadamard(Vector3<double>.One));
        Assert.Equal(new Vector2<double>(4, 6), new Vector2<double>(1, 2) + (3, 4));
        Assert.Equal(new Vector4<double>(0, 1, 1, 2), new Vector4<double>(1, 2, 3, 4) - (1, 1, 2, 2));
    }

    [Fact]
    public void Dot_and_cross_products()
    {
        Vector3<double> a = (1, 2, 3), b = (4, 5, 6);

        Assert.Equal(32, a.Dot(b));
        Assert.Equal(14, a.NormSquared());
        Assert.Equal(new Vector3<double>(-3, 6, -3), a.Cross(b));
        Assert.Equal(-a.Cross(b), b.Cross(a));
        Assert.Equal(0, a.Cross(b).Dot(a));
        Assert.Equal(0, a.Cross(b).Dot(b));
        Assert.Equal(Vector3<double>.UnitZ, Vector3<double>.UnitX.Cross(Vector3<double>.UnitY));
        Assert.Equal(11, new Vector2<double>(1, 2).Dot((3, 4)));
        Assert.Equal(70, new Vector4<double>(1, 2, 3, 4).Dot((5, 6, 7, 8)));
        Assert.Equal(30, new Vector4<double>(1, 2, 3, 4).NormSquared());
    }

    [Fact]
    public void Cross_product_in_the_plane_tells_the_turn()
    {
        Vector2<double> right = (1, 0);

        Assert.Equal(1, right.Cross((0, 1)));       // counterclockwise
        Assert.Equal(-1, right.Cross((0, -1)));     // clockwise
        Assert.Equal(0, right.Cross((-3, 0)));      // parallel
        Assert.Equal(new Vector3<double>(1, 2, 0).Cross((3, 4, 0)).Z, new Vector2<double>(1, 2).Cross((3, 4)));
    }

    [Fact]
    public void Lerp_between_points()
    {
        Vector2<double> start = (0, 10), end = (10, 20);

        Assert.Equal(start, Vector2<double>.Lerp(start, end, 0));
        Assert.Equal(end, Vector2<double>.Lerp(start, end, 1));
        Assert.Equal(new Vector2<double>(2.5, 12.5), Vector2<double>.Lerp(start, end, 0.25));
        Assert.Equal(new Vector2<double>(20, 30), Vector2<double>.Lerp(start, end, 2));
        Assert.Equal(new Vector3<double>(0.5, 0.5, 0), Vector3<double>.Lerp(Vector3<double>.UnitX, Vector3<double>.UnitY, 0.5));
        Assert.Equal(new Vector4<double>(1, 1, 1, 1), Vector4<double>.Lerp(Vector4<double>.Zero, (2, 2, 2, 2), 0.5));
    }

    [Fact]
    public void Equality_compares_the_entries_with_Equals()
    {
        Vector3<double> nan = (double.NaN, 0, 0), otherNan = (double.NaN, 0, 0);

        Assert.True(nan == otherNan);   // as for Matrix<T>, unlike double.NaN == double.NaN
        Assert.Equal(nan.GetHashCode(), otherNan.GetHashCode());
        Assert.True(new Vector2<double>(0, 0) == new Vector2<double>(-0.0, 0));
        Assert.True(new Vector3<double>(1, 2, 3) != new Vector3<double>(1, 2, 4));
        Assert.True(new Vector4<double>(1, 2, 3, 4).Equals((object)new Vector4<double>(1, 2, 3, 4)));
        Assert.False(new Vector3<double>(1, 2, 3).Equals((object)new Vector3<float>(1, 2, 3)));
    }

    [Fact]
    public void ToString_is_culture_invariant()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal("[1.5, 2, -3]", new Vector3<double>(1.5, 2, -3).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        Assert.Equal("[1, 2]", new Vector2<int>(1, 2).ToString());
        Assert.Equal("[1, 2, 3, 0.25]", new Vector4<float>(1, 2, 3, 0.25f).ToString());
    }

    [Fact]
    public void Integer_entries_for_grid_cells()
    {
        Vector3<int> cell = (7, 8, 9);

        Assert.Equal(new Vector3<int>(8, 8, 9), cell + Vector3<int>.UnitX);
        Assert.Equal(new Vector3<int>(3, 4, 4), cell / 2);   // truncated, as in C#
        Assert.Equal(194, cell.Dot(cell));
        Assert.Equal(new Vector3<int>(0, 0, 1), Vector3<int>.UnitX.Cross(Vector3<int>.UnitY));
    }

    [Fact]
    public void Float_Half_and_decimal_entries()
    {
        Vector3<float> f = (1, 2, 3);
        Vector2<Half> h = ((Half)1, (Half)2);
        Vector2<decimal> d = (0.1m, 0.2m);

        Assert.Equal(new Vector3<float>(2, 4, 6), f * 2);
        Assert.Equal((Half)5, h.NormSquared());
        Assert.Equal(new Vector2<decimal>(0.3m, 0.6m), d + d + d);   // exact in base 10
    }
}

public class FloatingPointVectorTests
{
    [Fact]
    public void Norm_and_distance()
    {
        Assert.Equal(5, new Vector2<double>(3, 4).Norm());
        Assert.Equal(5, new Vector3<double>(3, 0, 4).Norm());
        Assert.Equal(2, new Vector4<double>(1, 1, 1, 1).Norm());
        Assert.Equal(0, Vector3<double>.Zero.Norm());
        Assert.Equal(5f, new Vector3<float>(3, 4, 0).Norm());
        Assert.Equal(5, new Vector2<double>(1, 1).Distance((4, 5)));
        Assert.Equal(3, new Vector3<double>(1, 1, 1).Distance((1, 4, 1)));
        Assert.Equal(2, new Vector4<double>(0, 0, 0, 0).Distance((1, 1, 1, 1)));
    }

    [Fact]
    public void Norm_neither_overflows_nor_underflows()
    {
        // The naive square roots of the sums of squares are infinite or 0.
        Assert.Equal(5e300, new Vector3<double>(3e300, 4e300, 0).Norm());
        Assert.Equal(5e-320, new Vector2<double>(3e-320, 4e-320).Norm());
        Assert.Equal(float.ScaleB(5, 100), new Vector2<float>(float.ScaleB(3, 100), float.ScaleB(4, 100)).Norm());
        Assert.Equal(float.ScaleB(5, -100), new Vector4<float>(float.ScaleB(3, -100), 0, float.ScaleB(4, -100), 0).Norm());
        Assert.Equal((Half)500, new Vector2<Half>((Half)300, (Half)400).Norm());   // 300^2 > Half.MaxValue

        Assert.Equal(double.PositiveInfinity, new Vector3<double>(double.NegativeInfinity, 1, 0).Norm());
        Assert.True(double.IsNaN(new Vector3<double>(double.NaN, 1, 0).Norm()));
    }

    [Fact]
    public void Normalize_gives_the_unit_vector()
    {
        Assert.Equal(new Vector2<double>(0.6, 0.8), new Vector2<double>(3, 4).Normalize());
        Assert.Equal(new Vector3<double>(0, -1, 0), new Vector3<double>(0, -7, 0).Normalize());
        Assert.Equal(new Vector4<double>(0.5, 0.5, 0.5, 0.5), new Vector4<double>(2, 2, 2, 2).Normalize());

        // Exact scaling first: the naive norms are infinite and 0, which give zeros and NaN.
        Assert.Equal(1, new Vector3<double>(1.5e308, 1.5e308, 0).Normalize().Norm(), precision: 15);
        Assert.Equal(1, new Vector4<double>(1e-320, 1e-320, 0, 0).Normalize().Norm(), precision: 15);

        var ex = Assert.Throws<InvalidOperationException>(() => Vector3<double>.Zero.Normalize());
        Assert.Equal("Cannot normalize the zero vector.", ex.Message);
        Assert.Throws<InvalidOperationException>(() => Vector2<float>.Zero.Normalize());
    }

    [Fact]
    public void Angle_in_radians()
    {
        Vector2<double> x = (1, 0);

        Assert.Equal(Math.PI / 2, x.Angle((0, 1)));
        Assert.Equal(Math.PI / 4, x.Angle((1, 1)));
        Assert.Equal(Math.PI, x.Angle((-2, 0)));
        Assert.Equal(0, new Vector2<double>(1, 2).Angle((2, 4)));
        Assert.Equal(Math.PI / 2, Vector3<double>.UnitX.Angle(Vector3<double>.UnitZ));
        Assert.Equal(Math.PI / 2, Vector4<double>.UnitX.Angle(Vector4<double>.UnitW));
        Assert.Equal(MathF.PI / 2, new Vector2<float>(1, 0).Angle((0, 1)));
    }

    [Fact]
    public void Small_angles_stay_accurate()
    {
        // acos of the cosine gives 0: the cosine 1 - 5e-21 rounds to 1.
        Assert.Equal(1e-10, new Vector2<double>(1, 0).Angle((1, 1e-10)), 1e-25);
        Assert.Equal(Math.PI - 1e-10, new Vector3<double>(1, 0, 0).Angle((-1, 1e-10, 0)), 1e-15);
    }

    [Fact]
    public void Angle_with_the_zero_vector_is_undefined()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Vector3<double>.Zero.Angle(Vector3<double>.UnitX));
        Assert.Equal("The angle with the zero vector is undefined.", ex.Message);

        var argEx = Assert.Throws<ArgumentException>(() => Vector2<double>.UnitX.Angle(Vector2<double>.Zero));
        Assert.Equal("other", argEx.ParamName);
    }

    [Fact]
    public void Projection_onto_a_line()
    {
        Assert.Equal(new Vector2<double>(2, 0), new Vector2<double>(2, 3).ProjectOnto((5, 0)));
        Assert.Equal(new Vector2<double>(0.5, 0.5), new Vector2<double>(1, 0).ProjectOnto((1, 1)));
        Assert.Equal(new Vector2<double>(0.5e-300, 0.5e-300), new Vector2<double>(1e-300, 0).ProjectOnto((1e-300, 1e-300)));
        Assert.Equal(new Vector3<double>(0, 0, 3), new Vector3<double>(1, 2, 3).ProjectOnto((0, 0, -2)));
        Assert.Equal(new Vector4<double>(0, 2, 0, 0), new Vector4<double>(1, 2, 3, 4).ProjectOnto(Vector4<double>.UnitY));

        var ex = Assert.Throws<ArgumentException>(() => new Vector3<double>(1, 0, 0).ProjectOnto(Vector3<double>.Zero));
        Assert.StartsWith("Cannot project onto the zero vector.", ex.Message);
        Assert.Equal("onto", ex.ParamName);
    }

    [Fact]
    public void Reflection_in_a_plane()
    {
        // A ball moving down and right bounces off the floor.
        Assert.Equal(new Vector2<double>(1, 1), new Vector2<double>(1, -1).Reflect((0, 1)));
        Assert.Equal(new Vector2<double>(1, 1), new Vector2<double>(1, -1).Reflect((0, 5)));
        Assert.Equal(new Vector2<double>(-1, -3), new Vector2<double>(3, 1).Reflect((1, 1)));
        Assert.Equal(new Vector3<double>(1, 2, 3), new Vector3<double>(1, -2, 3).Reflect(Vector3<double>.UnitY));
        Assert.Equal(new Vector4<double>(1, 2, 3, -4), new Vector4<double>(1, 2, 3, 4).Reflect(Vector4<double>.UnitW));

        var ex = Assert.Throws<ArgumentException>(() => new Vector3<double>(1, 0, 0).Reflect(Vector3<double>.Zero));
        Assert.StartsWith("The normal is the zero vector.", ex.Message);
        Assert.Equal("normal", ex.ParamName);
    }

    [Fact]
    public void Rounding_errors_are_tolerated()
    {
        // cos(pi/2) is 6.1e-17, not 0.
        Vector3<double> turned = (Math.Cos(Math.PI / 2), 1, 0);
        Vector3<double> a = (1, 2, 3);

        Assert.NotEqual(Vector3<double>.UnitY, turned);
        Assert.True(turned.IsApproximately(Vector3<double>.UnitY));
        Assert.True(a.IsApproximately(a + (1e-12, 0, 0)));
        Assert.False(a.IsApproximately(a + (1e-6, 0, 0)));
        Assert.True(a.IsApproximately(a + (1e-6, 0, 0), relativeTolerance: 1e-3));
        Assert.True(new Vector4<double>(1, 2, 3, 4).IsApproximately((1, 2, 3, 4 + 1e-12)));
    }

    [Fact]
    public void Default_tolerance_follows_the_precision()
    {
        // A relative difference of 5e-8 is rounding for float, a real difference for double.
        Assert.True(new Vector2<float>(1, 2).IsApproximately((1.0000001f, 2)));
        Assert.False(new Vector2<double>(1, 2).IsApproximately((1.0000001, 2)));

        Assert.True(new Vector2<Half>((Half)1, (Half)2).IsApproximately(((Half)1.01, (Half)2)));
        Assert.False(new Vector2<Half>((Half)1, (Half)2).IsApproximately(((Half)1.1, (Half)2)));
    }

    [Fact]
    public void The_zero_vector_needs_an_absolute_tolerance()
    {
        Vector3<double> tiny = (1e-17, 0, 0);

        Assert.False(tiny.IsApproximately(Vector3<double>.Zero));
        Assert.True(tiny.IsApproximately(Vector3<double>.Zero, absoluteTolerance: 1e-15));
    }

    [Fact]
    public void Infinities_must_match_and_NaN_never_does()
    {
        double inf = double.PositiveInfinity;

        Assert.True(new Vector2<double>(inf, 1).IsApproximately((inf, 1)));
        Assert.False(new Vector2<double>(inf, 1).IsApproximately((inf, 2)));
        Assert.False(new Vector2<double>(inf, 1).IsApproximately((-inf, 1)));
        Assert.False(new Vector2<double>(double.NaN, 1).IsApproximately((double.NaN, 1)));
        Assert.False(new Vector2<double>(1e308, 0).IsApproximately((-1e308, 0)));
    }

    [Fact]
    public void Tolerances_must_be_numbers_from_zero_on()
    {
        Vector3<double> a = (1, 2, 3);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => a.IsApproximately(a, -1));
        Assert.Equal("relativeTolerance", ex.ParamName);

        ex = Assert.Throws<ArgumentOutOfRangeException>(() => a.IsApproximately(a, absoluteTolerance: double.NaN));
        Assert.Equal("absoluteTolerance", ex.ParamName);
    }
}

public class ComponentWiseVectorTests
{
    [Fact]
    public void Min_max_abs_and_clamp()
    {
        Vector3<double> a = (1, -5, 3), b = (2, -6, 0);

        Assert.Equal(new Vector3<double>(1, -6, 0), a.Min(b));
        Assert.Equal(new Vector3<double>(2, -5, 3), a.Max(b));
        Assert.Equal(new Vector3<double>(1, 5, 3), a.Abs());
        Assert.Equal(new Vector3<double>(1, -1, 1), a.Clamp((-1, -1, -1), Vector3<double>.One));
        Assert.Equal(new Vector2<int>(5, 0), new Vector2<int>(7, -3).Clamp(Vector2<int>.Zero, (5, 5)));
        Assert.Equal(new Vector4<float>(1, 0, 0, -1), new Vector4<float>(1, 2, 0, -1).Min((3, 0, 0, 0)));
        Assert.Equal(new Vector2<double>(3, 4), new Vector2<double>(-3, 4).Abs().Max((1, 1)));

        Assert.Throws<ArgumentException>(() => a.Clamp(Vector3<double>.One, Vector3<double>.Zero));
    }

}

public class FixedVectorMapTests
{
    [Fact]
    public void Map_applies_a_function_to_every_entry()
    {
        Assert.Equal(new Vector3<double>(1, -3, 3), new Vector3<double>(1.5, -2.7, 3).Map(Math.Floor));
        Assert.Equal(new Vector2<double>(1, 4), new Vector2<double>(1, 2).Map(x => x * x));
        Assert.Equal(new Vector4<int>(2, 3, 4, 5), new Vector4<int>(1, 2, 3, 4).Map(x => x + 1));
        Assert.Throws<ArgumentNullException>(() => Vector3<double>.Zero.Map<double>(null!));
    }

    [Fact]
    public void Map_converts_the_entries()
    {
        Assert.Equal(new Vector3<double>(1.1f, 2.5, -3), new Vector3<float>(1.1f, 2.5f, -3).Map(double.CreateTruncating));
        Assert.Equal(new Vector3<float>(1.1f, 2.5f, -3), new Vector3<double>(1.1, 2.5, -3).Map(float.CreateTruncating));
        Assert.Equal(new Vector2<int>(1, -3), new Vector2<double>(1.5, -2.7).Map(x => (int)Math.Floor(x)));   // the grid cell
        Assert.Equal(new Vector4<double>(1, 2, 3, 4), new Vector4<int>(1, 2, 3, 4).Map(x => (double)x));
        Assert.True(Half.IsPositiveInfinity(new Vector2<double>(1e5, 0).Map(Half.CreateTruncating).X));
    }
}
