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

public class FixedMatrixTests
{
    private static readonly Matrix3x3<double> A = new(1, 2, 3, 4, 5, 6, 7, 8, 9);

    // Small integers, so that every product and sum below is exact.
    private static Matrix4x4<double> RandomIntegers(Random random) =>
        Matrix4x4<double>.FromRows(Row(random), Row(random), Row(random), Row(random));

    private static Vector4<double> Row(Random random) =>
        (random.Next(-9, 10), random.Next(-9, 10), random.Next(-9, 10), random.Next(-9, 10));

    private static Matrix<double> General(Matrix4x4<double> m) => Matrix<double>.Create(4, 4, (i, j) => m[i, j]);

    [Fact]
    public void Entries_rows_and_columns()
    {
        Assert.Equal(2, A.M12);
        Assert.Equal(6, A[1, 2]);
        Assert.Equal(new Vector3<double>(4, 5, 6), A.Row(1));
        Assert.Equal(new Vector3<double>(3, 6, 9), A.Column(2));
        Assert.Equal(A, Matrix3x3<double>.FromRows((1, 2, 3), (4, 5, 6), (7, 8, 9)));
        Assert.Equal(A.Transpose(), Matrix3x3<double>.FromRows(A.Column(0), A.Column(1), A.Column(2)));
        Assert.Equal(A, Matrix3x3<double>.FromColumns((1, 4, 7), (2, 5, 8), (3, 6, 9)));
        Assert.Equal(new Vector4<double>(1, 2, 3, 1), Matrix4x4<double>.Translation((1, 2, 3)).Column(3));
    }

    [Fact]
    public void Indices_outside_the_matrix_are_rejected()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => A[3, 0]);
        Assert.Equal("row", ex.ParamName);
        Assert.StartsWith("The matrix has 3 rows.", ex.Message);

        ex = Assert.Throws<ArgumentOutOfRangeException>(() => Matrix4x4<double>.Identity[0, 4]);
        Assert.Equal("column", ex.ParamName);
        Assert.StartsWith("The matrix has 4 columns.", ex.Message);

        Assert.Throws<ArgumentOutOfRangeException>(() => A.Row(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix4x4<double>.Identity.Column(4));
    }

    [Fact]
    public void Identity_zero_and_with()
    {
        Assert.Equal(Matrix3x3<double>.FromRows(Vector3<double>.UnitX, Vector3<double>.UnitY, Vector3<double>.UnitZ), Matrix3x3<double>.Identity);
        Assert.Equal(new Matrix3x3<double>(0, 0, 0, 0, 0, 0, 0, 0, 0), Matrix3x3<double>.Zero);
        Assert.Equal(Matrix4x4<double>.Translation((5, 0, 0)), Matrix4x4<double>.Identity with { M14 = 5 });
        Assert.Equal(Matrix4x4<double>.Zero, default);
        Assert.Equal(Matrix4x4<double>.Identity, new Matrix4x4<double>(Matrix3x3<double>.Identity));
    }

    [Fact]
    public void Arithmetic()
    {
        var b = Matrix3x3<double>.Identity;

        Assert.Equal(new Matrix3x3<double>(2, 2, 3, 4, 6, 6, 7, 8, 10), A + b);
        Assert.Equal(new Matrix3x3<double>(0, 2, 3, 4, 4, 6, 7, 8, 8), A - b);
        Assert.Equal(new Matrix3x3<double>(-1, -2, -3, -4, -5, -6, -7, -8, -9), -A);
        Assert.Equal(A + A, 2 * A);
        Assert.Equal(A + A, A * 2);
        Assert.Equal(A, (A * 2) / 2);
        Assert.Equal(Matrix4x4<double>.Scale(4), Matrix4x4<double>.Scale(2) + Matrix4x4<double>.Scale(2) - new Matrix4x4<double>(Matrix3x3<double>.Zero));
    }

    [Fact]
    public void Products_match_the_general_matrices()
    {
        var random = new Random(1);
        for (int k = 0; k < 20; k++)
        {
            Matrix4x4<double> a = RandomIntegers(random), b = RandomIntegers(random);
            Vector4<double> v = Row(random);

            Assert.Equal(General(a) * General(b), General(a * b));
            Assert.Equal(General(a) * Vector.Create(v.X, v.Y, v.Z, v.W), Vector.Create((a * v).X, (a * v).Y, (a * v).Z, (a * v).W));
            Assert.Equal(General(a).Transpose(), General(a.Transpose()));
            Assert.Equal(General(a).Trace(), a.Trace());
            Assert.Equal(General(-a), -General(a));
            Assert.Equal(General(a + b), General(a) + General(b));
        }
    }

    [Fact]
    public void Product_applies_the_right_factor_first()
    {
        var scale = Matrix4x4<double>.Scale(2);
        var move = Matrix4x4<double>.Translation((1, 0, 0));

        Assert.Equal(new Vector3<double>(3, 0, 0), (move * scale).TransformPoint((1, 0, 0)));   // scaled, then moved
        Assert.Equal(new Vector3<double>(4, 0, 0), (scale * move).TransformPoint((1, 0, 0)));   // moved, then scaled
    }

    [Fact]
    public void Determinants_are_exact_for_integers()
    {
        var random = new Random(2);
        for (int k = 0; k < 20; k++)
        {
            var m = RandomIntegers(random).Map(x => (int)x);
            var exact = Matrix<Epsilon.Core.Rational>.Create(4, 4, (i, j) => m[i, j]);

            Assert.Equal(exact.Determinant(), m.Determinant());
            Assert.Equal(exact[..3, ..3].Determinant(), new Matrix3x3<int>(m.M11, m.M12, m.M13, m.M21, m.M22, m.M23, m.M31, m.M32, m.M33).Determinant());
        }

        Assert.Equal(49, new Matrix3x3<double>(2, -3, 1, 2, 0, -1, 1, 4, 5).Determinant());
        Assert.Equal(0, A.Determinant());
        Assert.Equal(24, Matrix4x4<int>.Scale((2, 3, 4)).Determinant());
    }

    [Fact]
    public void Points_move_and_directions_do_not()
    {
        var move = Matrix4x4<double>.Translation((1, 2, 3));

        Assert.Equal(new Vector3<double>(2, 3, 4), move.TransformPoint((1, 1, 1)));
        Assert.Equal(new Vector3<double>(1, 1, 1), move.TransformDirection((1, 1, 1)));
        Assert.Equal(new Vector4<double>(2, 3, 4, 1), move * new Vector4<double>(1, 1, 1, 1));
        Assert.Equal(new Vector3<double>(2, 3, 4), Matrix4x4<double>.Scale((2, 3, 4)).TransformPoint((1, 1, 1)));
        Assert.Equal(new Vector3<double>(3, 4, 5), (move * Matrix4x4<double>.Scale(2)).TransformPoint((1, 1, 1)));
    }

    [Fact]
    public void Points_are_divided_by_w_after_a_projective_map()
    {
        // w = z, as in a perspective projection.
        var project = Matrix4x4<double>.Identity with { M43 = 1, M44 = 0 };

        Assert.Equal(new Vector3<double>(1, 2, 1), project.TransformPoint((2, 4, 2)));
        Assert.Equal(new Vector2<double>(1, 2), (Matrix3x3<double>.Identity with { M31 = 1, M33 = 0 }).TransformPoint((2, 4)));   // w = x
    }

    [Fact]
    public void Affine_maps_of_the_plane()
    {
        var move = Matrix3x3<double>.Translation((5, 0));

        Assert.Equal(new Vector2<double>(6, 1), move.TransformPoint((1, 1)));
        Assert.Equal(new Vector2<double>(1, 1), move.TransformDirection((1, 1)));
        Assert.Equal(new Vector2<double>(2, 3), Matrix3x3<double>.Scale((2, 3)).TransformPoint((1, 1)));
        Assert.Equal(new Vector2<double>(7, 3), (move * Matrix3x3<double>.Scale((2, 3))).TransformPoint((1, 1)));
        Assert.Equal(Matrix3x3<double>.Scale((2, 3, 1)), Matrix3x3<double>.Scale((2, 3)));
    }

    [Fact]
    public void Integer_entries()
    {
        var m = new Matrix3x3<int>(1, 2, 0, 0, 1, 0, 0, 0, 1);

        Assert.Equal(new Matrix3x3<int>(1, 4, 0, 0, 1, 0, 0, 0, 1), m * m);
        Assert.Equal(new Vector3<int>(5, 2, 3), m * new Vector3<int>(1, 2, 3));
        Assert.Equal(new Vector2<int>(6, 3), Matrix3x3<int>.Translation((5, 2)).TransformPoint((1, 1)));
    }

    [Fact]
    public void Map_equality_and_text()
    {
        Assert.Equal(new Matrix3x3<float>(1, 2, 3, 4, 5, 6, 7, 8, 9), A.Map(float.CreateTruncating));
        Assert.Equal(Matrix4x4<double>.Scale(2), Matrix4x4<double>.Identity.Map(x => 2 * x) with { M44 = 1 });
        Assert.Throws<ArgumentNullException>(() => A.Map<double>(null!));

        Assert.True(A == new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9));
        Assert.True(A != Matrix3x3<double>.Identity);
        Assert.Equal(A.GetHashCode(), new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9).GetHashCode());
        Assert.False(A.Equals((object)Matrix4x4<double>.Identity));
        Assert.Equal("[[1, 2, 3], [4, 5, 6], [7, 8, 9]]", A.ToString());
        Assert.Equal("[[1, 0, 0, 0.5], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]", Matrix4x4<double>.Translation((0.5, 0, 0)).ToString());
    }
}

public class FloatingPointMatrixTests
{
    private static Matrix4x4<double> RandomMatrix(Random random) => Matrix4x4<double>.FromRows(
        (random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10),
        (random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10),
        (random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10),
        (random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10, random.NextDouble() * 20 - 10));

    private static Matrix3x3<double> UpperLeft(Matrix4x4<double> m) =>
        new(m.M11, m.M12, m.M13, m.M21, m.M22, m.M23, m.M31, m.M32, m.M33);

    [Fact]
    public void Rotations_about_the_axes()
    {
        double quarter = Math.PI / 2;

        Assert.True((Matrix3x3<double>.RotationX(quarter) * Vector3<double>.UnitY).IsApproximately(Vector3<double>.UnitZ));
        Assert.True((Matrix3x3<double>.RotationY(quarter) * Vector3<double>.UnitZ).IsApproximately(Vector3<double>.UnitX));
        Assert.True((Matrix3x3<double>.RotationZ(quarter) * Vector3<double>.UnitX).IsApproximately(Vector3<double>.UnitY));
        Assert.True(Matrix4x4<double>.RotationZ(quarter).TransformPoint((1, 0, 0)).IsApproximately((0, 1, 0)));
        Assert.Equal(Matrix3x3<double>.Identity, Matrix3x3<double>.RotationX(0));
        Assert.Equal(Matrix4x4<double>.Identity, Matrix4x4<double>.RotationY(0));
        Assert.True((Matrix3x3<float>.RotationZ(MathF.PI / 2) * Vector3<float>.UnitX).IsApproximately(Vector3<float>.UnitY));
    }

    [Fact]
    public void Rotation_about_any_axis()
    {
        // A third of a turn about the diagonal cycles the axes.
        var cycle = Matrix3x3<double>.Rotation((1, 1, 1), 2 * Math.PI / 3);

        Assert.True((cycle * Vector3<double>.UnitX).IsApproximately(Vector3<double>.UnitY));
        Assert.True((cycle * Vector3<double>.UnitY).IsApproximately(Vector3<double>.UnitZ));
        Assert.True(Matrix3x3<double>.Rotation((0, 0, 5), 0.3).IsApproximately(Matrix3x3<double>.RotationZ(0.3)));
        Assert.True(Matrix3x3<double>.Rotation((-2, 0, 0), 0.3).IsApproximately(Matrix3x3<double>.RotationX(-0.3)));
        Assert.Equal(new Matrix4x4<double>(Matrix3x3<double>.Rotation((1, 2, 3), 1)), Matrix4x4<double>.Rotation((1, 2, 3), 1));

        // Rotations keep lengths and orientation: R R^T = I, det R = 1.
        var r = Matrix3x3<double>.Rotation((1, -2, 0.5), 2.5);
        Assert.True((r * r.Transpose()).IsApproximately(Matrix3x3<double>.Identity));
        Assert.Equal(1, r.Determinant(), 1e-15);

        var ex = Assert.Throws<ArgumentException>(() => Matrix4x4<double>.Rotation(Vector3<double>.Zero, 1));
        Assert.Equal("axis", ex.ParamName);
    }

    [Fact]
    public void Rotation_in_the_plane()
    {
        var turn = Matrix3x3<double>.RotationZ(Math.PI / 2);
        var turnThenMove = Matrix3x3<double>.Translation((1, 0)) * turn;

        Assert.True(turn.TransformPoint((1, 0)).IsApproximately((0, 1)));
        Assert.True(turnThenMove.TransformPoint((1, 0)).IsApproximately((1, 1)));
    }

    [Fact]
    public void Inverse_undoes_the_map()
    {
        var random = new Random(3);
        for (int k = 0; k < 50; k++)
        {
            Matrix4x4<double> m = RandomMatrix(random);
            Matrix3x3<double> m3 = UpperLeft(m);

            Assert.True((m * m.Inverse()).IsApproximately(Matrix4x4<double>.Identity, absoluteTolerance: 1e-12));
            Assert.True((m.Inverse() * m).IsApproximately(Matrix4x4<double>.Identity, absoluteTolerance: 1e-12));
            Assert.True((m3 * m3.Inverse()).IsApproximately(Matrix3x3<double>.Identity, absoluteTolerance: 1e-12));
        }
    }

    [Fact]
    public void Inverse_of_simple_maps()
    {
        Assert.Equal(Matrix4x4<double>.Scale((0.5, 0.25, 0.125)), Matrix4x4<double>.Scale((2, 4, 8)).Inverse());
        Assert.Equal(Matrix4x4<double>.Translation((-1, -2, -3)), Matrix4x4<double>.Translation((1, 2, 3)).Inverse());

        var rotation = Matrix3x3<double>.Rotation((1, 2, 3), 0.7);
        Assert.True(rotation.Inverse().IsApproximately(rotation.Transpose()));

        // (T R S)^-1 = S^-1 R^-1 T^-1.
        var move = Matrix4x4<double>.Translation((4, -1, 2));
        var turn = Matrix4x4<double>.Rotation((0, 1, 1), 1.2);
        var scale = Matrix4x4<double>.Scale((2, 3, 0.5));
        Assert.True((move * turn * scale).Inverse().IsApproximately(scale.Inverse() * turn.Inverse() * move.Inverse()));
    }

    [Fact]
    public void Singular_matrices_have_no_inverse()
    {
        var exactlySingular = new Matrix3x3<double>(1, 2, 3, 4, 5, 6, 7, 8, 9);
        var roundedSingular = new Matrix3x3<double>(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9);
        Vector4<double> a = (0.1, 0.7, 0.3, 1.9), b = (0.3, 0.6, 0.9, 0.7);
        var dependentRows = Matrix4x4<double>.FromRows(a, b, a + b, (1, 2, 3, 4));

        Assert.False(exactlySingular.TryInverse(out var inverse));
        Assert.Equal(Matrix3x3<double>.Zero, inverse);

        // Rounding leaves determinants of about 1e-17 and 1e-16 instead of 0.
        Assert.NotEqual(0, roundedSingular.Determinant());
        Assert.False(roundedSingular.TryInverse(out _));
        Assert.NotEqual(0, dependentRows.Determinant());
        Assert.False(dependentRows.TryInverse(out _));
        Assert.False(Matrix4x4<double>.Scale((1, 1, 0)).TryInverse(out _));
        Assert.False(Matrix4x4<double>.Zero.TryInverse(out _));

        var ex = Assert.Throws<InvalidOperationException>(() => exactlySingular.Inverse());
        Assert.Equal("The matrix is singular to working precision.", ex.Message);
    }

    [Fact]
    public void Small_entries_are_no_reason_for_singularity()
    {
        // Nothing cancels in the determinant, so 1e-20 is no rounding noise.
        Assert.Equal(Matrix3x3<double>.Scale((1 / 1e-20, 1, 1)), Matrix3x3<double>.Scale((1e-20, 1, 1)).Inverse());

        // The same matrix with its second column scaled down.
        var scaledColumn = Matrix4x4<double>.FromRows((1, 1e-20, 0, 0), (1, 2e-20, 0, 0), (0, 0, 1, 0), (0, 0, 0, 1));
        Assert.True((scaledColumn * scaledColumn.Inverse()).IsApproximately(Matrix4x4<double>.Identity));
    }

    [Fact]
    public void Inverse_neither_overflows_nor_underflows()
    {
        var random = new Random(4);
        Matrix4x4<double> m = RandomMatrix(random);

        // The naive determinants are about 1e800 and 1e-800: infinite and 0.
        foreach (double size in new[] { 1e200, 1e-200 })
        {
            Matrix4x4<double> scaled = size * m;
            Assert.True((scaled * scaled.Inverse()).IsApproximately(Matrix4x4<double>.Identity, absoluteTolerance: 1e-12));
            Assert.True(scaled.Inverse().IsApproximately(m.Inverse() / size));
        }

        Assert.Equal(Matrix4x4<double>.Scale((1e-300, 1e300, 1)), Matrix4x4<double>.Scale((1e300, 1e-300, 1)).Inverse());

        // Products of four floats of 1e10 overflow.
        var big = Matrix4x4<float>.Scale(1e10f) with { M12 = 3e10f, M43 = -2e10f };
        Assert.True((big * big.Inverse()).IsApproximately(Matrix4x4<float>.Identity, absoluteTolerance: 1e-6f));
    }

    [Fact]
    public void Float_and_Half_entries()
    {
        var turn = Matrix4x4<float>.Rotation((1, 1, 0), 0.5f) * Matrix4x4<float>.Translation((1, 2, 3));
        Assert.True((turn * turn.Inverse()).IsApproximately(Matrix4x4<float>.Identity, absoluteTolerance: 1e-6f));

        var half = Matrix3x3<Half>.Scale(((Half)2, (Half)4, (Half)0.5));
        Assert.Equal(Matrix3x3<Half>.Scale(((Half)0.5, (Half)0.25, (Half)2)), half.Inverse());
    }

    [Fact]
    public void Entries_that_are_NaN_or_infinite_have_no_inverse()
    {
        var nan = Matrix4x4<double>.Identity with { M23 = double.NaN };
        var infinite = Matrix3x3<double>.Identity with { M11 = double.PositiveInfinity };

        Assert.False(nan.TryInverse(out _));
        Assert.False(infinite.TryInverse(out _));
        var ex = Assert.Throws<InvalidOperationException>(() => nan.Inverse());
        Assert.Equal("The matrix has an entry that is NaN or infinite.", ex.Message);
    }

    [Fact]
    public void Approximate_equality()
    {
        // cos(pi/2) is 6.1e-17, not 0.
        var turn = Matrix3x3<double>.RotationZ(Math.PI / 2);
        var exact = new Matrix3x3<double>(0, -1, 0, 1, 0, 0, 0, 0, 1);

        Assert.NotEqual(exact, turn);
        Assert.True(turn.IsApproximately(exact));
        Assert.False(exact.IsApproximately(exact + 1e-6 * Matrix3x3<double>.Identity));
        Assert.True(exact.IsApproximately(exact + 1e-6 * Matrix3x3<double>.Identity, relativeTolerance: 1e-3));
        Assert.True(Matrix4x4<double>.Identity.IsApproximately(Matrix4x4<double>.Identity with { M14 = 1e-12 }));

        var residual = turn * turn.Transpose() - Matrix3x3<double>.Identity;
        Assert.True(residual.IsApproximately(Matrix3x3<double>.Zero, absoluteTolerance: 1e-15));

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => exact.IsApproximately(exact, -1));
        Assert.Equal("relativeTolerance", ex.ParamName);
    }
}
