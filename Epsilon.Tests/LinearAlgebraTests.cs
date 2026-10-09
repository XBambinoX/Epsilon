using System.Globalization;
using Xunit;
using Epsilon.Core;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class MatrixTests
{
    private static readonly Matrix<double> A = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]);

    [Fact]
    public void FromRows_takes_the_rows_in_order()
    {
        Assert.Equal(2, A.Rows);
        Assert.Equal(3, A.Columns);
        Assert.False(A.IsSquare);
        Assert.Equal(1, A[0, 0]);
        Assert.Equal(3, A[0, 2]);
        Assert.Equal(4, A[1, 0]);
        Assert.Equal(6, A[1, 2]);
    }

    [Fact]
    public void Rows_of_different_lengths_are_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(() => Matrix<double>.FromRows([1, 2], [3]));
        Assert.StartsWith("Row 1 has 1 entries, but row 0 has 2.", ex.Message);
    }

    [Fact]
    public void Null_entries_are_rejected()
    {
        Expr x = new Variable("x");

        var ex = Assert.Throws<ArgumentNullException>(() => Matrix<Expr>.FromRows([x, null!]));
        Assert.StartsWith("The entry at (0, 1) is null.", ex.Message);
        Assert.Throws<ArgumentNullException>(() => new Matrix<Expr>(new Expr[1, 1]));
        Assert.Throws<ArgumentNullException>(() => Matrix<Expr>.Create(1, 1, (_, _) => null!));
    }

    [Fact]
    public void Constructors_copy_their_arrays()
    {
        var array = new double[,] { { 1, 2 }, { 3, 4 } };
        var fromArray = new Matrix<double>(array);
        double[] row = [1, 2];
        var fromRows = Matrix<double>.FromRows(row);

        array[0, 0] = 9;
        row[0] = 9;

        Assert.Equal(1, fromArray[0, 0]);
        Assert.Equal(1, fromRows[0, 0]);
    }

    [Fact]
    public void Create_calls_the_function_for_every_entry()
    {
        var m = Matrix<int>.Create(2, 3, (i, j) => 10 * i + j);

        Assert.Equal(Matrix<int>.FromRows([0, 1, 2], [10, 11, 12]), m);
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix<int>.Create(-1, 2, (_, _) => 0));
    }

    [Fact]
    public void Empty_matrices_are_allowed()
    {
        var empty = Matrix<double>.FromRows();

        Assert.Equal(0, empty.Rows);
        Assert.Equal(0, empty.Columns);
        Assert.True(empty.IsSquare);
        Assert.Equal("[]", empty.ToString());
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 3)]
    [InlineData(1, -1)]
    // (0, 3) is inside the storage of a 2 x 3 matrix, but outside its first row.
    public void Indices_outside_the_matrix_throw(int row, int column)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => A[row, column]);
    }

    [Fact]
    public void Row_and_column_are_vectors()
    {
        Assert.Equal(Vector.Create<double>(4, 5, 6), A.Row(1));
        Assert.Equal(Vector.Create<double>(3, 6), A.Column(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => A.Row(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => A.Column(3));
    }

    [Fact]
    public void Transpose_swaps_rows_and_columns()
    {
        Assert.Equal(Matrix<double>.FromRows([1, 4], [2, 5], [3, 6]), A.Transpose());
        Assert.Equal(A, A.Transpose().Transpose());
    }

    [Fact]
    public void Map_applies_the_function_to_every_entry()
    {
        Matrix<string> texts = A.Map(v => $"<{v}>");

        Assert.Equal(2, texts.Rows);
        Assert.Equal(3, texts.Columns);
        Assert.Equal("<6>", texts[1, 2]);
    }

    [Fact]
    public void ToArray_returns_a_copy()
    {
        double[,] array = A.ToArray();
        array[0, 0] = 9;

        Assert.Equal(1, A[0, 0]);
        Assert.Equal(6, array[1, 2]);
    }

    [Fact]
    public void Equal_matrices_have_the_same_size_and_entries()
    {
        var same = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]);
        var otherShape = Matrix<double>.FromRows([1, 2], [3, 4], [5, 6]);

        Assert.True(A == same);
        Assert.Equal(A.GetHashCode(), same.GetHashCode());
        Assert.True(A != otherShape);
        Assert.False(A.Equals(null));
        Assert.False(A.Equals(A.Row(0)));
    }

    [Fact]
    public void Expression_entries_compare_structurally()
    {
        var x = new Variable("x");

        Assert.Equal(Matrix<Expr>.FromRows([x + 1, 2]), Matrix<Expr>.FromRows([new Variable("x") + 1, 2]));
        Assert.NotEqual(Matrix<Expr>.FromRows([x + 1]), Matrix<Expr>.FromRows([1 + x]));
    }

    [Fact]
    public void ToString_is_culture_invariant()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA");
            Assert.Equal("[[1.5, -2], [0, 1E-05]]", Matrix<double>.FromRows([1.5, -2], [0, 1e-5]).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Ranges_give_a_submatrix()
    {
        var m = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);

        Assert.Equal(Matrix<double>.FromRows([4, 5], [7, 8]), m[1.., ..2]);
        Assert.Equal(Matrix<double>.FromRows([2], [5]), m[..^1, 1..2]);
        Assert.Equal(m, m[.., ..]);

        Matrix<double> empty = m[0..0, ..];
        Assert.Equal(0, empty.Rows);
        Assert.Equal(3, empty.Columns);
    }

    [Fact]
    public void Ranges_outside_the_matrix_throw()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => A[1..3, ..]);
        Assert.StartsWith("The range 1..3 is outside 0..2.", ex.Message);
        Assert.Equal("rows", ex.ParamName);

        Assert.Throws<ArgumentOutOfRangeException>(() => A[.., 2..1]);
    }

    [Fact]
    public void Diagonal_of_any_shape()
    {
        Assert.Equal([1.0, 5.0], A.Diagonal());
        Assert.Equal([1.0, 5.0], A.Transpose().Diagonal());
        Assert.Equal(0, Matrix<double>.FromRows().Diagonal().Length);
    }

    [Fact]
    public void Blocks_join_side_by_side_and_on_top_of_each_other()
    {
        var a = Matrix<double>.FromRows([1, 2], [3, 4]);
        var i = Matrix<double>.Identity(2);

        Assert.Equal(Matrix<double>.FromRows([1, 2, 1, 0], [3, 4, 0, 1]), Matrix<double>.FromBlocks([a, i]));
        Assert.Equal(Matrix<double>.FromRows([1, 2], [3, 4], [1, 0], [0, 1]), Matrix<double>.FromBlocks([a], [i]));
        Assert.Equal(Matrix<double>.FromRows([1, 2, 0], [3, 4, 0], [0, 0, 9]),
            Matrix<double>.FromBlocks([a, Matrix<double>.Zero(2, 1)], [Matrix<double>.Zero(1, 2), Matrix<double>.FromRows([9])]));
        Assert.Equal(0, Matrix<double>.FromBlocks().Rows);
    }

    [Fact]
    public void A_vector_is_a_block_with_one_column()
    {
        var a = Matrix<double>.FromRows([1, 2], [3, 4]);
        Vector<double> b = [5, 6];

        Assert.Equal(Matrix<double>.FromRows([1, 2, 5], [3, 4, 6]), Matrix<double>.FromBlocks([a, b]));
    }

    [Fact]
    public void Blocks_must_fit_together()
    {
        var a = Matrix<double>.FromRows([1, 2], [3, 4]);

        var ex = Assert.Throws<ArgumentException>(() => Matrix<double>.FromBlocks([a, Matrix<double>.Zero(3, 1)]));
        Assert.StartsWith("Block (0, 1) has 3 rows, but block (0, 0) has 2.", ex.Message);

        ex = Assert.Throws<ArgumentException>(() => Matrix<double>.FromBlocks([a], [Matrix<double>.Zero(1, 3)]));
        Assert.StartsWith("Block row 1 has 3 columns, but block row 0 has 2.", ex.Message);

        ex = Assert.Throws<ArgumentException>(() => Matrix<double>.FromBlocks([a], []));
        Assert.StartsWith("Block row 1 is empty.", ex.Message);

        ex = Assert.Throws<ArgumentNullException>(() => Matrix<double>.FromBlocks([a, null!]));
        Assert.StartsWith("Block (0, 1) is null.", ex.Message);
    }
}

public class VectorTests
{
    [Fact]
    public void A_collection_expression_creates_a_vector()
    {
        Vector<double> v = [1, 2, 3];

        Assert.Equal(3, v.Length);
        Assert.Equal(2, v[1]);
        Assert.Equal([1.0, 2.0, 3.0], v);
        Assert.Equal([1.0, 2.0, 3.0], v.ToArray());
    }

    [Fact]
    public void Expression_vectors_accept_numbers()
    {
        Vector<Expr> v = [new Variable("x"), 2];

        Assert.Equal("[x, 2]", v.Map(e => e.Print()).ToString());
    }

    [Fact]
    public void A_vector_is_a_matrix_with_one_column()
    {
        Matrix<double> m = Vector.Create<double>(1, 2, 3);

        Assert.Equal(Matrix<double>.FromRows([1], [2], [3]), m);
    }

    [Fact]
    public void Indices_outside_the_vector_throw()
    {
        Vector<double> v = [1, 2];

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => v[2]);
        Assert.Equal("index", ex.ParamName);
    }

    [Fact]
    public void Create_copies_and_checks_the_entries()
    {
        Expr[] entries = [new Variable("x")];
        var v = Vector.Create<Expr>(entries);
        entries[0] = 1;

        Assert.Equal(new Variable("x"), v[0]);
        Assert.Throws<ArgumentNullException>(() => Vector.Create<Expr>(null!, 1));
    }

    [Fact]
    public void Equality_and_text()
    {
        Vector<double> v = [1, 2.5];

        Assert.True(v == Vector.Create(1.0, 2.5));
        Assert.True(v != Vector.Create(1.0, 2.5, 0));
        Assert.Equal(v.GetHashCode(), Vector.Create(1.0, 2.5).GetHashCode());
        Assert.Equal("[1, 2.5]", v.ToString());
        Assert.Equal("[]", Vector.Create<double>().ToString());
    }

    [Fact]
    public void Indices_and_ranges_from_the_end()
    {
        Vector<double> v = [1, 2, 3, 4];

        Assert.Equal(4, v[^1]);
        Assert.Equal([1.0, 2.0, 3.0], v[..3]);
        Assert.Equal([2.0, 3.0], v[1..^1]);
        Assert.Equal(0, v[4..].Length);
        Assert.Equal([2.0, 3.0], v.Slice(1, 2));
    }

    [Fact]
    public void Ranges_outside_the_vector_throw()
    {
        Vector<double> v = [1, 2, 3, 4];

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => v[2..5]);
        Assert.StartsWith("The range 2..5 is outside 0..4.", ex.Message);
        Assert.Throws<ArgumentOutOfRangeException>(() => v.Slice(3, -1));
    }
}

public class NumericArithmeticTests
{
    private static readonly Matrix<double> A = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]);
    private static readonly Matrix<double> B = Matrix<double>.FromRows([7, 8], [9, 10], [11, 12]);

    [Fact]
    public void Identity_and_zero()
    {
        Assert.Equal(Matrix<double>.FromRows([1, 0], [0, 1]), Matrix<double>.Identity(2));
        Assert.Equal(Matrix<double>.FromRows([0, 0, 0], [0, 0, 0]), Matrix<double>.Zero(2, 3));
        Assert.Equal(0, Matrix<double>.Identity(0).Rows);
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix<double>.Identity(-1));
        Assert.Equal([0.0, 0.0, 0.0], Vector<double>.Zero(3));
    }

    [Fact]
    public void Sum_difference_and_negation_work_entry_by_entry()
    {
        var c = Matrix<double>.FromRows([1, 1, 1], [2, 2, 2]);

        Assert.Equal(Matrix<double>.FromRows([2, 3, 4], [6, 7, 8]), A + c);
        Assert.Equal(Matrix<double>.FromRows([0, 1, 2], [2, 3, 4]), A - c);
        Assert.Equal(Matrix<double>.FromRows([-1, -2, -3], [-4, -5, -6]), -A);
    }

    [Fact]
    public void Sizes_must_agree_for_a_sum()
    {
        var ex = Assert.Throws<ArgumentException>(() => A + A.Transpose());
        Assert.Equal("Cannot add matrices of different sizes: 2 x 3 and 3 x 2.", ex.Message);
    }

    [Fact]
    public void Product_of_matrices()
    {
        Assert.Equal(Matrix<double>.FromRows([58, 64], [139, 154]), A * B);
        Assert.Equal(A, Matrix<double>.Identity(2) * A);
        Assert.Equal(A, A * Matrix<double>.Identity(3));

        var c = Matrix<double>.FromRows([1, -1], [2, 0]);
        Assert.Equal(A * B * c, A * (B * c));
    }

    [Fact]
    public void Inner_sizes_must_agree_for_a_product()
    {
        var ex = Assert.Throws<ArgumentException>(() => A * A);
        Assert.Equal("Cannot multiply a 2 x 3 matrix by a 2 x 3 matrix: 3 columns on the left, 2 rows on the right.", ex.Message);
    }

    [Fact]
    public void Product_of_a_matrix_and_a_vector_is_a_vector()
    {
        Vector<double> v = A * Vector.Create<double>(1, 0, -1);
        Assert.Equal([-2.0, -2.0], v);

        var ex = Assert.Throws<ArgumentException>(() => A * Vector.Create<double>(1, 2));
        Assert.Equal("Cannot multiply a 2 x 3 matrix by a vector of length 2.", ex.Message);
    }

    [Fact]
    public void Scalars_multiply_and_divide_every_entry()
    {
        Assert.Equal(Matrix<double>.FromRows([2, 4, 6], [8, 10, 12]), 2 * A);
        Assert.Equal(2 * A, A * 2);
        Assert.Equal(Matrix<double>.FromRows([0.5, 1, 1.5], [2, 2.5, 3]), A / 2);
    }

    [Fact]
    public void Trace_of_a_square_matrix()
    {
        Assert.Equal(212, (A * B).Trace());
        Assert.Equal(0, Matrix<double>.FromRows().Trace());

        var ex = Assert.Throws<InvalidOperationException>(() => A.Trace());
        Assert.Equal("The trace needs a square matrix, not a 2 x 3 matrix.", ex.Message);
    }

    [Fact]
    public void Vector_arithmetic()
    {
        Vector<double> v = [1, 2, 3];
        Vector<double> w = [4, 5, 6];

        Assert.Equal([5.0, 7.0, 9.0], v + w);
        Assert.Equal([-3.0, -3.0, -3.0], v - w);
        Assert.Equal([-1.0, -2.0, -3.0], -v);
        Assert.Equal([2.0, 4.0, 6.0], 2 * v);
        Assert.Equal(2 * v, v * 2);
        Assert.Equal([0.5, 1.0, 1.5], v / 2);
        Assert.Equal(32, v.Dot(w));

        var ex = Assert.Throws<ArgumentException>(() => v + Vector.Create<double>(1, 2));
        Assert.Equal("Cannot add vectors of different lengths: 3 and 2.", ex.Message);
        Assert.Throws<ArgumentException>(() => v.Dot(Vector.Create<double>(1, 2)));
    }
}

public class SymbolicArithmeticTests
{
    private static readonly Variable X = new("x");

    private static Expr P(string text) => ExprParser.Parse(text).Simplify();

    private static Matrix<Expr> M(params string[][] rows) =>
        Matrix<Expr>.FromRows(rows.Select(row => row.Select(P).ToArray()).ToArray());

    private static readonly Matrix<Expr> Abcd = M(["a", "b"], ["c", "d"]);

    [Fact]
    public void Identity_and_zero()
    {
        Assert.Equal(Matrix<Expr>.FromRows([1, 0], [0, 1]), Matrix<Expr>.Identity(2));
        Assert.Equal(Matrix<Expr>.FromRows([0, 0]), Matrix<Expr>.Zero(1, 2));
        Assert.Equal(Vector.Create<Expr>(0, 0), Vector<Expr>.Zero(2));
    }

    [Fact]
    public void Product_entries_are_simplified()
    {
        Assert.Equal(M(["a*e + b*g", "a*f + b*h"], ["c*e + d*g", "c*f + d*h"]), Abcd * M(["e", "f"], ["g", "h"]));
        Assert.Equal(Abcd, Matrix<Expr>.Identity(2) * Abcd);
    }

    [Fact]
    public void Numbers_stay_exact()
    {
        var m = Matrix<Expr>.FromRows([1, 2], [3, 4]);

        Assert.Equal(Matrix<Expr>.FromRows([7, 10], [15, 22]), m * m);
        Assert.Equal(M(["1/2", "1"], ["3/2", "2"]), m / 2);
    }

    [Fact]
    public void Sum_difference_and_negation_are_simplified()
    {
        Assert.Equal(M(["2a", "2b"], ["2c", "2d"]), Abcd + Abcd);
        Assert.Equal(Matrix<Expr>.Zero(2, 2), Abcd - Abcd);
        Assert.Equal(M(["-a", "-b"], ["-c", "-d"]), -Abcd);
    }

    [Fact]
    public void Scalars_may_be_numbers_or_expressions()
    {
        Assert.Equal(Abcd + Abcd, 2 * Abcd);
        Assert.Equal(M(["a*x", "b*x"], ["c*x", "d*x"]), Abcd * X);
        Assert.Equal(M(["a/x", "b/x"], ["c/x", "d/x"]), Abcd / X);
    }

    [Fact]
    public void Entries_are_simplified_in_generic_mode()
    {
        // x/x becomes 1, as with Simplify(): the result may be defined at more points.
        Assert.Equal(Matrix<Expr>.FromRows([1]), Matrix<Expr>.FromRows([X / X]) + Matrix<Expr>.Zero(1, 1));
    }

    [Fact]
    public void Trace_is_simplified()
    {
        Assert.Equal(P("a + d"), Abcd.Trace());
        Assert.Equal(P("a^2 + 2b*c + d^2"), (Abcd * Abcd).Trace());
    }

    [Fact]
    public void Vector_arithmetic()
    {
        Vector<Expr> v = [P("a"), P("b")];
        Vector<Expr> w = [X, 1];

        Assert.Equal(P("a*x + b"), v.Dot(w));
        Assert.Equal(Vector.Create(P("a + x"), P("b + 1")), v + w);
        Assert.Equal(Vector.Create(P("a*x + b"), P("c*x + d")), Abcd * w);
        Assert.Equal(Vector.Create(P("2a"), P("2b")), 2 * v);
        Assert.Equal(Vector.Create(P("-a"), P("-b")), -v);
    }
}

public class NumericVectorOperationsTests
{
    [Fact]
    public void Norm_and_its_square()
    {
        Vector<double> v = [3, 4];

        Assert.Equal(5, v.Norm());
        Assert.Equal(25, v.NormSquared());
        Assert.Equal(Math.Sqrt(10), Vector.Create<double>(1, 3).Norm());
        Assert.Equal(0, Vector<double>.Zero(0).Norm());
    }

    [Fact]
    public void Norm_neither_overflows_nor_underflows()
    {
        // The naive sqrt(9e600 + 16e600) is infinite and sqrt(9e-640 + 16e-640) is 0.
        Assert.Equal(5e300, Vector.Create(3e300, 4e300).Norm());
        Assert.Equal(5e-320, Vector.Create(3e-320, 4e-320).Norm());

        Assert.Equal(double.PositiveInfinity, Vector.Create(double.NegativeInfinity, 1).Norm());
        Assert.True(double.IsNaN(Vector.Create(double.NaN, 1).Norm()));
    }

    [Fact]
    public void Normalize_gives_the_unit_vector()
    {
        Assert.Equal([0.6, 0.8], Vector.Create<double>(3, 4).Normalize());
        Assert.Equal([0.0, -1.0, 0.0], Vector.Create<double>(0, -7, 0).Normalize());

        // Exact scaling first: the naive norms are infinite and 0, which give [0, 0] and NaN.
        Assert.Equal(1, Vector.Create(1.5e308, 1.5e308).Normalize().Norm(), precision: 15);
        Assert.Equal(1, Vector.Create(1e-320, 1e-320).Normalize().Norm(), precision: 15);

        var ex = Assert.Throws<InvalidOperationException>(() => Vector<double>.Zero(2).Normalize());
        Assert.Equal("Cannot normalize the zero vector.", ex.Message);
    }

    [Fact]
    public void Distance_between_points()
    {
        Assert.Equal(5, Vector.Create<double>(1, 1).Distance([4, 5]));

        var ex = Assert.Throws<ArgumentException>(() => Vector.Create<double>(1, 1).Distance([1]));
        Assert.Equal("Cannot measure the distance between vectors of different lengths: 2 and 1.", ex.Message);
    }

    [Fact]
    public void Cross_product()
    {
        Vector<double> a = [1, 2, 3];
        Vector<double> b = [4, 5, 6];

        Assert.Equal([0.0, 0.0, 1.0], Vector.Create<double>(1, 0, 0).Cross([0, 1, 0]));
        Assert.Equal([-3.0, 6.0, -3.0], a.Cross(b));
        Assert.Equal(-a.Cross(b), b.Cross(a));
        Assert.Equal(0, a.Cross(b).Dot(a));
        Assert.Equal(0, a.Cross(b).Dot(b));
    }

    [Fact]
    public void Cross_product_needs_three_entries()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Vector.Create<double>(1, 2).Cross([1, 2, 3]));
        Assert.Equal("The cross product needs vectors of length 3, not 2.", ex.Message);

        var argEx = Assert.Throws<ArgumentException>(() => Vector.Create<double>(1, 2, 3).Cross([1, 2, 3, 4]));
        Assert.StartsWith("The cross product needs vectors of length 3, not 4.", argEx.Message);
        Assert.Equal("other", argEx.ParamName);
    }

    [Fact]
    public void Angle_in_radians()
    {
        Vector<double> x = [1, 0];

        Assert.Equal(Math.PI / 2, x.Angle([0, 1]));
        Assert.Equal(Math.PI / 4, x.Angle([1, 1]));
        Assert.Equal(Math.PI, x.Angle([-2, 0]));
        Assert.Equal(0, Vector.Create<double>(1, 2).Angle([2, 4]));
    }

    [Fact]
    public void Small_angles_stay_accurate()
    {
        // acos of the cosine gives 0: the cosine 1 - 5e-21 rounds to 1.
        Assert.Equal(1e-10, Vector.Create<double>(1, 0).Angle([1, 1e-10]), 1e-25);
        Assert.Equal(Math.PI - 1e-10, Vector.Create<double>(1, 0).Angle([-1, 1e-10]), 1e-15);
    }

    [Fact]
    public void Angle_with_the_zero_vector_is_undefined()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Vector<double>.Zero(2).Angle([1, 0]));
        Assert.Equal("The angle with the zero vector is undefined.", ex.Message);

        var argEx = Assert.Throws<ArgumentException>(() => Vector.Create<double>(1, 0).Angle(Vector<double>.Zero(2)));
        Assert.Equal("other", argEx.ParamName);
    }

    [Fact]
    public void Projection_onto_a_line()
    {
        Assert.Equal([2.0, 0.0], Vector.Create<double>(2, 3).ProjectOnto([5, 0]));
        Assert.Equal([0.5, 0.5], Vector.Create<double>(1, 0).ProjectOnto([1, 1]));
        Assert.Equal([0.5e-300, 0.5e-300], Vector.Create(1e-300, 0).ProjectOnto([1e-300, 1e-300]));

        var ex = Assert.Throws<ArgumentException>(() => Vector.Create<double>(1, 0).ProjectOnto([0, 0]));
        Assert.StartsWith("Cannot project onto the zero vector.", ex.Message);
    }

    [Fact]
    public void Reflection_in_a_plane()
    {
        // A ball moving down and right bounces off the floor.
        Assert.Equal([1.0, 1.0], Vector.Create<double>(1, -1).Reflect([0, 1]));
        Assert.Equal([1.0, 1.0], Vector.Create<double>(1, -1).Reflect([0, 5]));
        Assert.Equal([-1.0, -3.0], Vector.Create<double>(3, 1).Reflect([1, 1]));

        var ex = Assert.Throws<ArgumentException>(() => Vector.Create<double>(1, 0).Reflect([0, 0]));
        Assert.StartsWith("The normal is the zero vector.", ex.Message);
    }

    [Fact]
    public void Lerp_between_points()
    {
        Vector<double> start = [0, 10];
        Vector<double> end = [10, 20];

        Assert.Equal(start, Vector<double>.Lerp(start, end, 0));
        Assert.Equal(end, Vector<double>.Lerp(start, end, 1));
        Assert.Equal([2.5, 12.5], Vector<double>.Lerp(start, end, 0.25));
        Assert.Equal([20.0, 30.0], Vector<double>.Lerp(start, end, 2));

        var ex = Assert.Throws<ArgumentException>(() => Vector<double>.Lerp(start, [1], 0.5));
        Assert.Equal("Cannot interpolate between vectors of different lengths: 2 and 1.", ex.Message);
    }

    [Fact]
    public void Outer_product()
    {
        Vector<double> v = [1, 2];
        Vector<double> w = [3, 4, 5];

        Assert.Equal(Matrix<double>.FromRows([3, 4, 5], [6, 8, 10]), v.Outer(w));
        Assert.Equal(v.ToMatrix() * w.ToMatrix().Transpose(), v.Outer(w));
    }
}

public class SymbolicVectorOperationsTests
{
    private static Vector<Expr> V(string text) => Vector<Expr>.Parse(text).Simplify();

    [Fact]
    public void Norm_and_its_square()
    {
        Assert.Equal("sqrt(a^2 + b^2)", V("[a, b]").Norm().Print());
        Assert.Equal("a^2 + b^2", V("[a, b]").NormSquared().Print());
        Assert.Equal("5", V("[3, 4]").Norm().Print());
        Assert.Equal("abs(x)", V("[x, 0]").Norm().Print());
    }

    [Fact]
    public void Normalize_is_exact()
    {
        Assert.Equal("[3/5, 4/5]", V("[3, 4]").Normalize().Print());
        Assert.Equal("[sqrt(2) / 2, sqrt(2) / 2]", V("[1, 1]").Normalize().Print());
        Assert.Equal("[a / sqrt(a^2 + b^2), b / sqrt(a^2 + b^2)]", V("[a, b]").Normalize().Print());

        Assert.Throws<InvalidOperationException>(() => V("[x - x, 0]").Normalize());
    }

    [Fact]
    public void Distance_between_points()
    {
        Assert.Equal("sqrt((a - c)^2 + (b - d)^2)", V("[a, b]").Distance(V("[c, d]")).Print());
        Assert.Equal("5", V("[1, 1]").Distance(V("[4, 5]")).Print());
    }

    [Fact]
    public void Cross_product()
    {
        Assert.Equal("[b * z - c * y, c * x - a * z, a * y - b * x]", V("[a, b, c]").Cross(V("[x, y, z]")).Print());
        Assert.Equal("[0, 0, 1]", V("[1, 0, 0]").Cross(V("[0, 1, 0]")).Print());
        Assert.Throws<InvalidOperationException>(() => V("[a, b]").Cross(V("[x, y, z]")));
    }

    [Fact]
    public void Angle_at_the_usual_points_is_exact()
    {
        Assert.Equal("π / 4", V("[1, 0]").Angle(V("[1, 1]")).Print());
        Assert.Equal("π / 2", V("[1, 0]").Angle(V("[0, 1]")).Print());
        Assert.Equal("π", V("[1, 0]").Angle(V("[-1, 0]")).Print());
        Assert.Equal("acos(sqrt(3) / 3)", V("[1, 0, 0]").Angle(V("[1, 1, 1]")).Print());
        Assert.Equal("acos((a * c + b * d) / (sqrt(a^2 + b^2) * sqrt(c^2 + d^2)))", V("[a, b]").Angle(V("[c, d]")).Print());

        var ex = Assert.Throws<ArgumentException>(() => V("[1, 0]").Angle(V("[0, 0]")));
        Assert.StartsWith("The angle with the zero vector is undefined.", ex.Message);
    }

    [Fact]
    public void Projection_and_reflection()
    {
        Assert.Equal("[a, 0]", V("[a, b]").ProjectOnto(V("[1, 0]")).Print());
        Assert.Equal("[(x + y) / 2, (x + y) / 2]", V("[x, y]").ProjectOnto(V("[1, 1]")).Print());
        Assert.Equal("[x, -y]", V("[x, y]").Reflect(V("[0, 1]")).Print());
        Assert.Equal("[-y, -x]", V("[x, y]").Reflect(V("[1, 1]")).Print());

        Assert.Throws<ArgumentException>(() => V("[a, b]").ProjectOnto(V("[0, 0]")));
        Assert.Throws<ArgumentException>(() => V("[a, b]").Reflect(V("[0, 0]")));
    }

    [Fact]
    public void Lerp_with_a_symbolic_parameter()
    {
        var t = new Variable("t");

        Assert.Equal("[t * x, 2t + 1]", Vector<Expr>.Lerp(V("[0, 1]"), V("[x, 3]"), t).Print());
        Assert.Equal(V("[a, b]"), Vector<Expr>.Lerp(V("[a, b]"), V("[c, d]"), 0));
        Assert.Equal(V("[c, d]"), Vector<Expr>.Lerp(V("[a, b]"), V("[c, d]"), 1));
    }

    [Fact]
    public void Outer_product()
    {
        Assert.Equal("[[a * x, a * y, 2a], [b * x, b * y, 2b]]", V("[a, b]").Outer(V("[x, y, 2]")).Print());
    }
}

public class NumericMatrixUtilityTests
{
    private static readonly Matrix<double> A = Matrix<double>.FromRows([1, 2], [3, 4]);

    [Fact]
    public void FromDiagonal_puts_zeros_elsewhere()
    {
        Assert.Equal(Matrix<double>.FromRows([1, 0, 0], [0, 2, 0], [0, 0, 3]), Matrix<double>.FromDiagonal([1, 2, 3]));
        Assert.Equal([1.0, 2.0, 3.0], Matrix<double>.FromDiagonal([1, 2, 3]).Diagonal());
    }

    [Fact]
    public void Pow_by_repeated_squaring()
    {
        Assert.Equal(Matrix<double>.Identity(2), A.Pow(0));
        Assert.Equal(A, A.Pow(1));
        Assert.Equal(A * A * A * A * A, A.Pow(5));

        // Exact: the entry is the exponent, after 2 * 31 products.
        Assert.Equal(Matrix<double>.FromRows([1, int.MaxValue], [0, 1]), Matrix<double>.FromRows([1, 1], [0, 1]).Pow(int.MaxValue));
        Assert.Equal(Matrix<double>.Identity(2), Matrix<double>.Identity(2).Pow(int.MinValue));
    }

    [Fact]
    public void Negative_powers_are_powers_of_the_inverse()
    {
        var b = Matrix<double>.FromRows([2, 0], [0, 4]);

        Assert.Equal(Matrix<double>.FromRows([0.25, 0], [0, 0.0625]), b.Pow(-2));
        Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2], [2, 4]).Pow(-1));
    }

    [Fact]
    public void Pow_needs_a_square_matrix()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Matrix<double>.FromRows([1, 2]).Pow(2));
        Assert.Equal("Pow needs a square matrix, not a 1 x 2 matrix.", ex.Message);
    }

    [Fact]
    public void Hadamard_multiplies_entry_by_entry()
    {
        Assert.Equal(Matrix<double>.FromRows([2, 0], [3, -4]), A.Hadamard(Matrix<double>.FromRows([2, 0], [1, -1])));
        Assert.Equal([2.0, 4.0, 0.0], Vector.Create<double>(1, 2, 3).Hadamard([2, 2, 0]));

        var ex = Assert.Throws<ArgumentException>(() => A.Hadamard(Matrix<double>.Zero(2, 3)));
        Assert.Equal("Cannot take the Hadamard product of matrices of different sizes: 2 x 2 and 2 x 3.", ex.Message);
        Assert.Throws<ArgumentException>(() => Vector.Create<double>(1, 2).Hadamard([1]));
    }

    [Fact]
    public void Frobenius_norm()
    {
        Assert.Equal(Math.Sqrt(30), A.FrobeniusNorm());
        Assert.Equal(5e300, Matrix<double>.FromRows([3e300], [4e300]).FrobeniusNorm());
        Assert.Equal(0, Matrix<double>.Zero(2, 2).FrobeniusNorm());
    }
}

public class SymbolicMatrixUtilityTests
{
    private static Matrix<Expr> M(string text) => Matrix<Expr>.Parse(text).Simplify();

    private static readonly Matrix<Expr> Abcd = M("[[a, b], [c, d]]");

    [Fact]
    public void FromDiagonal_and_Diagonal()
    {
        Assert.Equal("[[a, 0], [0, b]]", Matrix<Expr>.FromDiagonal(Vector<Expr>.Parse("[a, b]")).Print());
        Assert.Equal("[a, d]", Abcd.Diagonal().Print());
    }

    [Fact]
    public void Pow_is_simplified()
    {
        Assert.Equal(Abcd * Abcd, Abcd.Pow(2));
        Assert.Equal("[[1, 10x], [0, 1]]", M("[[1, x], [0, 1]]").Pow(10).Print());
        Assert.Equal("[[1, -3x], [0, 1]]", M("[[1, x], [0, 1]]").Pow(-3).Print());
        Assert.Equal(Matrix<Expr>.Identity(2), Abcd.Pow(0));
    }

    [Fact]
    public void Negative_power_of_a_singular_matrix_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => M("[[1, 2], [2, 4]]").Pow(-1));
        Assert.Equal("The matrix is singular: its determinant is 0.", ex.Message);
    }

    [Fact]
    public void Hadamard_and_Frobenius_norm()
    {
        Assert.Equal("[[a * x, 2b], [0, d * y]]", Abcd.Hadamard(M("[[x, 2], [0, y]]")).Print());
        Assert.Equal("[a * x, 2b]", Vector<Expr>.Parse("[a, b]").Hadamard(Vector<Expr>.Parse("[x, 2]")).Print());
        Assert.Equal("sqrt(a^2 + b^2 + c^2 + d^2)", Abcd.FrobeniusNorm().Print());
        Assert.Equal("5", M("[[1, 2], [2, 4]]").FrobeniusNorm().Print());
    }

    [Fact]
    public void Blocks_and_ranges_work_for_expressions()
    {
        Assert.Equal("[[a, b, x], [c, d, y]]", Matrix<Expr>.FromBlocks([Abcd, Vector<Expr>.Parse("[x, y]")]).Print());
        Assert.Equal("[[c, d]]", Abcd[1.., ..].Print());
    }
}

public class ApproximateEqualityTests
{
    private static readonly Matrix<double> A = Matrix<double>.FromRows([1, 2], [3, 4]);

    [Fact]
    public void Rounding_errors_are_tolerated()
    {
        // cos(pi/2) is 6.1e-17, not 0: hopeless for a relative test entry by entry.
        double c = Math.Cos(Math.PI / 2), s = Math.Sin(Math.PI / 2);
        var rotation = Matrix<double>.FromRows([c, -s], [s, c]);
        var exact = Matrix<double>.FromRows([0, -1], [1, 0]);

        Assert.NotEqual(exact, rotation);
        Assert.True(rotation.IsApproximately(exact));
        Assert.True((A * A.Inverse()).IsApproximately(Matrix<double>.Identity(2)));
    }

    [Fact]
    public void Differences_above_the_tolerance_count()
    {
        var shifted = A + 1e-6 * Matrix<double>.Identity(2);

        Assert.False(A.IsApproximately(shifted));
        Assert.True(A.IsApproximately(shifted, relativeTolerance: 1e-3));
        Assert.True(A.IsApproximately(A + 1e-12 * Matrix<double>.Identity(2)));
        Assert.False(A.IsApproximately(Matrix<double>.Zero(2, 3)));
    }

    [Fact]
    public void The_zero_matrix_needs_an_absolute_tolerance()
    {
        var residual = A * A.Inverse() - Matrix<double>.Identity(2);

        Assert.False(residual.IsApproximately(Matrix<double>.Zero(2, 2)));
        Assert.True(residual.IsApproximately(Matrix<double>.Zero(2, 2), absoluteTolerance: 1e-12));
    }

    [Fact]
    public void Infinities_must_match_and_NaN_never_does()
    {
        double inf = double.PositiveInfinity;

        Assert.True(Matrix<double>.FromRows([inf, 1]).IsApproximately(Matrix<double>.FromRows([inf, 1])));
        Assert.False(Matrix<double>.FromRows([inf, 1]).IsApproximately(Matrix<double>.FromRows([inf, 2])));
        Assert.False(Matrix<double>.FromRows([inf]).IsApproximately(Matrix<double>.FromRows([-inf])));
        Assert.False(Matrix<double>.FromRows([double.NaN]).IsApproximately(Matrix<double>.FromRows([double.NaN])));
        Assert.False(Matrix<double>.FromRows([1e308]).IsApproximately(Matrix<double>.FromRows([-1e308])));
    }

    [Fact]
    public void Vectors()
    {
        Assert.True(Vector.Create(1, 1e-17).IsApproximately([1, 0]));
        Assert.False(Vector.Create(1e-17).IsApproximately([0]));
        Assert.True(Vector.Create(1e-17).IsApproximately([0], absoluteTolerance: 1e-15));
        Assert.False(Vector.Create<double>(1, 2).IsApproximately([1, 2, 0]));
    }

    [Fact]
    public void Tolerances_must_be_numbers_from_zero_on()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => A.IsApproximately(A, -1));
        Assert.Equal("relativeTolerance", ex.ParamName);

        ex = Assert.Throws<ArgumentOutOfRangeException>(() => A.IsApproximately(A, absoluteTolerance: double.NaN));
        Assert.Equal("absoluteTolerance", ex.ParamName);
    }
}

public class DebuggerViewTests
{
    [Fact]
    public void Small_matrices_show_their_entries()
    {
        Assert.Equal("2 x 3, [[1, 2, 3], [4, 5, 6]]", Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]).DebuggerDisplay);
        Assert.Equal("4 x 4, [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]", Matrix<double>.Identity(4).DebuggerDisplay);
        Assert.Equal("5 x 5", Matrix<double>.Identity(5).DebuggerDisplay);
    }

    [Fact]
    public void Expressions_are_printed()
    {
        // ToString would give (((3 * (x ^ 2)) - (4 * x)) + 1).
        Assert.Equal("1 x 2, [[3x^2 - 4x + 1, 1/2]]", Matrix<Expr>.Parse("[[3x^2 - 4x + 1, 1/2]]").Simplify().DebuggerDisplay);
        Assert.Equal("Length = 2, [x^2, 1/2]", Vector<Expr>.Parse("[x^2, 1/2]").Simplify().DebuggerDisplay);
    }

    [Fact]
    public void Vectors_show_their_length()
    {
        Assert.Equal("Length = 3, [1, 2.5, 3]", Vector.Create(1, 2.5, 3).DebuggerDisplay);
        Assert.Equal("Length = 17", Vector<double>.Zero(17).DebuggerDisplay);
    }

    [Fact]
    public void Expanding_shows_rows_and_entries()
    {
        var m = Matrix<double>.FromRows([1, 2], [3, 4]);

        Assert.Equal([m.Row(0), m.Row(1)], new MatrixDebugView<double>(m).Rows);
        Assert.Equal([1.0, 2.0], new VectorDebugView<double>(m.Row(0)).Entries);
    }

    [Fact]
    public void The_debugger_finds_the_views()
    {
        Assert.Equal("{DebuggerDisplay,nq}", DisplayOf(typeof(Matrix<>)));
        Assert.Equal("{DebuggerDisplay,nq}", DisplayOf(typeof(Vector<>)));
        Assert.Equal(typeof(MatrixDebugView<>), ProxyOf(typeof(Matrix<>)));
        Assert.Equal(typeof(VectorDebugView<>), ProxyOf(typeof(Vector<>)));
    }

    private static string? DisplayOf(Type type) =>
        type.GetCustomAttributes(typeof(System.Diagnostics.DebuggerDisplayAttribute), false)
            .Cast<System.Diagnostics.DebuggerDisplayAttribute>().Single().Value;

    private static Type? ProxyOf(Type type) =>
        Type.GetType(type.GetCustomAttributes(typeof(System.Diagnostics.DebuggerTypeProxyAttribute), false)
            .Cast<System.Diagnostics.DebuggerTypeProxyAttribute>().Single().ProxyTypeName);
}

public class RationalMatrixTests
{
    private static readonly Matrix<Rational> A = Matrix<Rational>.FromRows([1, 2], [3, 4]);
    private static readonly Matrix<Rational> Singular3 = Matrix<Rational>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);

    private static Rational Q(int numerator, int denominator) => new(numerator, denominator);

    [Fact]
    public void Identity_zero_and_diagonal()
    {
        Assert.Equal(Matrix<Rational>.FromRows([1, 0], [0, 1]), Matrix<Rational>.Identity(2));
        Assert.Equal(Matrix<Rational>.FromRows([0, 0, 0]), Matrix<Rational>.Zero(1, 3));
        Assert.Equal(Matrix<Rational>.FromRows([1, 0], [0, Q(1, 2)]), Matrix<Rational>.FromDiagonal([1, Q(1, 2)]));
        Assert.Equal([Rational.Zero, Rational.Zero], Vector<Rational>.Zero(2));
    }

    [Fact]
    public void Arithmetic_is_exact()
    {
        Assert.Equal(Matrix<Rational>.FromRows([7, 10], [15, 22]), A * A);
        Assert.Equal(Matrix<Rational>.FromRows([Q(1, 3), Q(2, 3)], [1, Q(4, 3)]), A / 3);
        Assert.Equal(A / 3, A * Q(1, 3));
        Assert.Equal(Matrix<Rational>.FromRows([2, 4], [6, 8]), 2 * A);
        Assert.Equal(Matrix<Rational>.Zero(2, 2), A - A);
        Assert.Equal(-A, Matrix<Rational>.Zero(2, 2) - A);
        Assert.Equal([5, 11], A * Vector.Create<Rational>(1, 2));
        Assert.Equal(5, A.Trace());
    }

    [Fact]
    public void Division_by_zero_throws()
    {
        var ex = Assert.Throws<DivideByZeroException>(() => A / 0);
        Assert.Equal("Cannot divide a matrix by 0.", ex.Message);
        Assert.Throws<DivideByZeroException>(() => Vector.Create<Rational>(1) / 0);
    }

    [Fact]
    public void Determinant_inverse_and_solve()
    {
        Assert.Equal(-2, A.Determinant());
        Assert.Equal(Matrix<Rational>.FromRows([-2, 1], [Q(3, 2), Q(-1, 2)]), A.Inverse());
        Assert.Equal([-4, Q(9, 2)], A.Solve([5, 6]));
        Assert.Equal(A.Inverse(), A.Solve(Matrix<Rational>.Identity(2)));

        // A zero first pivot needs a row swap, which flips the sign.
        var swapped = Matrix<Rational>.FromRows([0, 1], [1, 0]);
        Assert.Equal(-1, swapped.Determinant());
        Assert.Equal(swapped, swapped.Inverse());
    }

    [Fact]
    public void The_Hilbert_matrix_is_inverted_exactly()
    {
        // In doubles the inverse of this famously ill-conditioned matrix is off from the 7th digit on.
        const int n = 8;
        var hilbert = Matrix<Rational>.Create(n, n, (i, j) => Q(1, i + j + 1));

        Matrix<Rational> inverse = hilbert.Inverse();
        Assert.Equal(64, inverse[0, 0]);
        Assert.Equal(176679360, inverse[7, 7]);
        Assert.Equal(Matrix<Rational>.Identity(n), hilbert * inverse);
        Assert.Equal(new Rational(1, System.Numerics.BigInteger.Parse("365356847125734485878112256000000")), hilbert.Determinant());
    }

    [Fact]
    public void Singular_matrices_are_recognized_exactly()
    {
        Assert.Equal(0, Singular3.Determinant());
        Assert.Equal(2, Singular3.Rank());

        var ex = Assert.Throws<InvalidOperationException>(() => Singular3.Inverse());
        Assert.Equal("The matrix is singular: its rank is 2, not 3.", ex.Message);
        Assert.Throws<InvalidOperationException>(() => Singular3.Solve([1, 2, 3]));
        Assert.Throws<InvalidOperationException>(() => Singular3.Pow(-1));
    }

    [Fact]
    public void Rank_of_any_shape()
    {
        Assert.Equal(1, Matrix<Rational>.FromRows([1, 2, 3], [2, 4, 6]).Rank());
        Assert.Equal(2, Matrix<Rational>.FromRows([0, 1], [0, 2], [1, 0]).Rank());
        Assert.Equal(0, Matrix<Rational>.Zero(2, 2).Rank());
        Assert.Equal(0, Matrix<Rational>.Zero(0, 3).Rank());
    }

    [Fact]
    public void Square_matrices_are_required()
    {
        var wide = Matrix<Rational>.FromRows([1, 2, 3]);

        Assert.Equal("The determinant needs a square matrix, not a 1 x 3 matrix.",
            Assert.Throws<InvalidOperationException>(() => wide.Determinant()).Message);
        Assert.Throws<InvalidOperationException>(() => wide.Inverse());
        Assert.Throws<InvalidOperationException>(() => wide.Trace());
        Assert.Throws<ArgumentException>(() => A.Solve([1, 2, 3]));
    }

    [Fact]
    public void Powers_and_Hadamard()
    {
        Assert.Equal(A * A * A, A.Pow(3));
        Assert.Equal(A.Inverse() * A.Inverse(), A.Pow(-2));
        Assert.Equal(Matrix<Rational>.Identity(2), A.Pow(0));
        Assert.Equal(Matrix<Rational>.FromRows([1, 4], [9, 16]), A.Hadamard(A));
    }
}

public class RationalVectorTests
{
    private static readonly Vector<Rational> V = [1, 2, 3];
    private static readonly Vector<Rational> W = [4, 5, 6];

    [Fact]
    public void Products()
    {
        Assert.Equal(32, V.Dot(W));
        Assert.Equal(14, V.NormSquared());
        Assert.Equal([-3, 6, -3], V.Cross(W));
        Assert.Equal([4, 10, 18], V.Hadamard(W));
        Assert.Equal(Matrix<Rational>.FromRows([1, 2], [2, 4], [3, 6]), V.Outer([1, 2]));
    }

    [Fact]
    public void Projection_reflection_and_lerp_are_exact()
    {
        Assert.Equal([new Rational(3, 2), new Rational(3, 2), 0], V.ProjectOnto([1, 1, 0]));
        Assert.Equal([1, 1, 0], Vector.Create<Rational>(1, -1, 0).Reflect([0, 2, 0]));
        Assert.Equal([2, 3, 4], Vector<Rational>.Lerp(V, W, new Rational(1, 3)));

        Assert.Throws<ArgumentException>(() => V.ProjectOnto(Vector<Rational>.Zero(3)));
        Assert.Throws<ArgumentException>(() => V.Reflect(Vector<Rational>.Zero(3)));
    }

    [Fact]
    public void Arithmetic()
    {
        Assert.Equal([5, 7, 9], V + W);
        Assert.Equal([-3, -3, -3], V - W);
        Assert.Equal([-1, -2, -3], -V);
        Assert.Equal([new Rational(1, 2), 1, new Rational(3, 2)], V / 2);
        Assert.Equal(2 * V, V * 2);
    }
}

public class NumericLuTests
{
    private static readonly Matrix<double> Singular3 = Matrix<double>.FromRows([1, 2, 3], [4, 5, 6], [7, 8, 9]);
    private static readonly Matrix<double> BadlyScaled = Matrix<double>.FromRows([1e-20, 0], [0, 1]);

    private static void AssertClose(Matrix<double> expected, Matrix<double> actual, double tolerance = 1e-12)
    {
        Assert.Equal(expected.Rows, actual.Rows);
        Assert.Equal(expected.Columns, actual.Columns);
        for (int i = 0; i < expected.Rows; i++)
            for (int j = 0; j < expected.Columns; j++)
                Assert.True(Math.Abs(expected[i, j] - actual[i, j]) <= tolerance,
                    $"Entry ({i}, {j}): expected {expected[i, j]}, got {actual[i, j]}.");
    }

    private static Matrix<double> Hilbert(int n) => Matrix<double>.Create(n, n, (i, j) => 1.0 / (i + j + 1));

    [Fact]
    public void Determinant_of_regular_matrices()
    {
        Assert.Equal(-6, Matrix<double>.FromRows([4, 3], [6, 3]).Determinant(), precision: 12);
        Assert.Equal(49, Matrix<double>.FromRows([2, -3, 1], [2, 0, -1], [1, 4, 5]).Determinant(), precision: 12);
        Assert.Equal(1, Matrix<double>.Identity(4).Determinant());
        Assert.Equal(-1, Matrix<double>.FromRows([0, 1], [1, 0]).Determinant());
        Assert.Equal(1, Matrix<double>.FromRows().Determinant());
    }

    [Fact]
    public void Singular_matrices_have_determinant_zero()
    {
        Assert.Equal(0, Matrix<double>.FromRows([1, 2], [2, 4]).Determinant());
        // The last pivot comes out as about 1e-16; it is rounding noise, so the determinant is 0.
        Assert.Equal(0, Singular3.Determinant());
        Assert.Equal(0, Matrix<double>.Zero(3, 3).Determinant());
    }

    [Fact]
    public void Badly_scaled_matrices_are_not_singular()
    {
        Assert.Equal(1e-20, BadlyScaled.Determinant());
        Assert.Equal(2, BadlyScaled.Rank());
        Assert.Equal(Matrix<double>.FromRows([1e20, 0], [0, 1]), BadlyScaled.Inverse());
    }

    [Fact]
    public void Determinant_inverse_and_solve_need_a_square_matrix()
    {
        var wide = Matrix<double>.Zero(2, 3);

        Assert.Equal("The determinant needs a square matrix, not a 2 x 3 matrix.",
            Assert.Throws<InvalidOperationException>(() => wide.Determinant()).Message);
        Assert.Equal("The inverse needs a square matrix, not a 2 x 3 matrix.",
            Assert.Throws<InvalidOperationException>(() => wide.Inverse()).Message);
        Assert.Equal("Solve needs a square matrix, not a 2 x 3 matrix.",
            Assert.Throws<InvalidOperationException>(() => wide.Solve([1, 2])).Message);
    }

    [Fact]
    public void Solve_a_system()
    {
        var m = Matrix<double>.FromRows([4, 3], [6, 3]);

        AssertClose(Matrix<double>.FromRows([1], [2]), m.Solve([10, 12]));
        // A zero in the corner needs a row swap; nothing is rounded here.
        Assert.Equal([3.0, 2.0], Matrix<double>.FromRows([0, 1], [1, 0]).Solve([2, 3]));
    }

    [Fact]
    public void Solve_several_right_hand_sides_at_once()
    {
        var a = Matrix<double>.FromRows([2, -3, 1], [2, 0, -1], [1, 4, 5]);
        var x = Matrix<double>.FromRows([1, 0], [2, -1], [3, 4]);

        AssertClose(x, a.Solve(a * x));
    }

    [Fact]
    public void Solve_rejects_singular_matrices_and_wrong_lengths()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Singular3.Solve([1, 2, 3]));
        Assert.Equal("The matrix is singular: its rank is 2, not 3, to working precision.", ex.Message);

        var lengths = Assert.Throws<ArgumentException>(() => Matrix<double>.Identity(3).Solve([1, 2]));
        Assert.StartsWith("The right-hand side has 2 entries, but the matrix has 3 rows.", lengths.Message);
    }

    [Fact]
    public void Inverse_of_regular_matrices()
    {
        var m = Matrix<double>.FromRows([4, 7], [2, 6]);

        AssertClose(Matrix<double>.FromRows([0.6, -0.7], [-0.2, 0.4]), m.Inverse());
        AssertClose(Matrix<double>.Identity(2), m * m.Inverse());
        Assert.Equal(0, Matrix<double>.FromRows().Inverse().Rows);
        Assert.Throws<InvalidOperationException>(() => Singular3.Inverse());
    }

    [Fact]
    public void Ill_conditioned_but_regular_matrices_are_inverted()
    {
        // The Hilbert matrix of size 6 has a condition number of about 1.5e7.
        var h = Hilbert(6);

        Assert.Equal(6, h.Rank());
        AssertClose(Matrix<double>.Identity(6), h * h.Inverse(), tolerance: 1e-6);
        Assert.Equal(36, h.Inverse()[0, 0], precision: 4);
    }

    [Fact]
    public void Rank_of_any_shape()
    {
        Assert.Equal(2, Singular3.Rank());
        Assert.Equal(1, Matrix<double>.FromRows([1, 2], [2, 4], [3, 6]).Rank());
        Assert.Equal(2, Matrix<double>.FromRows([1, 2, 3], [4, 5, 6]).Rank());
        Assert.Equal(2, Matrix<double>.FromRows([0, 1, 2], [0, 2, 5]).Rank());
        Assert.Equal(0, Matrix<double>.Zero(3, 3).Rank());
        Assert.Equal(0, Matrix<double>.FromRows().Rank());
        Assert.Equal(5, Matrix<double>.Identity(5).Rank());
    }

    [Fact]
    public void Solution_of_a_random_system_has_a_small_residual()
    {
        var random = new Random(1);
        var a = Matrix<double>.Create(30, 30, (_, _) => random.NextDouble() * 2 - 1);
        var b = Vector.Create(Enumerable.Range(0, 30).Select(_ => random.NextDouble()).ToArray());

        var residual = a * a.Solve(b) - b;

        Assert.All(residual, r => Assert.True(Math.Abs(r) < 1e-12, $"Residual {r}."));
    }

    [Fact]
    public void Non_finite_entries_are_rejected()
    {
        var nan = Matrix<double>.FromRows([1, 2], [double.NaN, 4]);
        var infinite = Matrix<double>.FromRows([1, double.PositiveInfinity]);

        Assert.Equal("The matrix has a non-finite entry at (1, 0): NaN.",
            Assert.Throws<InvalidOperationException>(() => nan.Determinant()).Message);
        Assert.Equal("The matrix has a non-finite entry at (0, 1): Infinity.",
            Assert.Throws<InvalidOperationException>(() => infinite.Rank()).Message);
    }
}

public class SymbolicDeterminantTests
{
    private static Expr P(string text) => ExprParser.Parse(text).Simplify();

    private static Matrix<Expr> M(params string[][] rows) =>
        Matrix<Expr>.FromRows(rows.Select(row => row.Select(P).ToArray()).ToArray());

    // Whether two expressions are equal as polynomials (or after Expand cancels their difference).
    private static void AssertSame(Expr expected, Expr actual) =>
        Assert.True((actual - expected).Expand() is Constant { Value.IsZero: true },
            $"Expected {expected.Print()}, got {actual.Print()}.");

    private static readonly Matrix<Expr> Abcd = M(["a", "b"], ["c", "d"]);
    private static readonly Matrix<Expr> Regular3 = Matrix<Expr>.FromRows([2, -3, 1], [2, 0, -1], [1, 4, 5]);

    [Fact]
    public void Determinant_of_symbolic_matrices()
    {
        Assert.Equal("a * d - b * c", Abcd.Determinant().Print());
        AssertSame(P("a*e*k + b*f*g + c*d*h - c*e*g - b*d*k - a*f*h"),
            M(["a", "b", "c"], ["d", "e", "f"], ["g", "h", "k"]).Determinant());
        AssertSame(P("a*d*f"), M(["a", "b", "c"], ["0", "d", "e"], ["0", "0", "f"]).Determinant());
    }

    [Fact]
    public void Determinant_of_numbers_is_exact()
    {
        Assert.Equal(new Constant(49), Regular3.Determinant());

        var hilbert = Matrix<Expr>.Create(4, 4, (i, j) => new Constant(new Rational(1, i + j + 1)));
        Assert.Equal(new Constant(new Rational(1, 6048000)), hilbert.Determinant());
    }

    [Fact]
    public void Determinant_cancels_what_cancels()
    {
        // (x + 1)(x - 3) - (x - 1)^2
        Assert.Equal(new Constant(-4), M(["x + 1", "x - 1"], ["x - 1", "x - 3"]).Determinant());
        Assert.Equal(new Constant(1), M(["cos(x)", "-sin(x)"], ["sin(x)", "cos(x)"]).Determinant());
        Assert.Equal(new Constant(0), M(["x", "x + 1"], ["2x", "2x + 2"]).Determinant());
    }

    [Fact]
    public void Determinant_of_small_sizes()
    {
        Assert.Equal(new Constant(1), Matrix<Expr>.FromRows().Determinant());
        Assert.Equal(P("x^2"), M(["x^2"]).Determinant());
        Assert.Equal("The determinant needs a square matrix, not a 1 x 2 matrix.",
            Assert.Throws<InvalidOperationException>(() => M(["a", "b"]).Determinant()).Message);
    }

    [Fact]
    public void Characteristic_polynomial()
    {
        var p = Matrix<Expr>.FromRows([2, 1], [1, 2]).CharacteristicPolynomial("t");

        Assert.Equal("t^2 - 4t + 3", p.Print());
        Assert.Equal([1.0, 3.0], p.FindRealRoots());
        AssertSame(P("t^2 - (a + d)*t + a*d - b*c"), Abcd.CharacteristicPolynomial("t"));
        Assert.Equal(new Constant(1), Matrix<Expr>.FromRows().CharacteristicPolynomial("t"));
    }

    [Fact]
    public void Characteristic_polynomial_needs_a_new_variable()
    {
        var ex = Assert.Throws<ArgumentException>(() => Abcd.CharacteristicPolynomial("c"));
        Assert.StartsWith("The variable 'c' occurs in the entry at (1, 0).", ex.Message);
    }

    [Fact]
    public void Adjugate_exists_for_singular_matrices_too()
    {
        Assert.Equal(M(["d", "-b"], ["-c", "a"]), Abcd.Adjugate());
        Assert.Equal(Matrix<Expr>.FromRows([4, -2], [-2, 1]), Matrix<Expr>.FromRows([1, 2], [2, 4]).Adjugate());
        Assert.Equal(Matrix<Expr>.FromRows([1]), M(["x"]).Adjugate());
        Assert.Equal(49 * Matrix<Expr>.Identity(3), Regular3 * Regular3.Adjugate());
    }

    [Fact]
    public void Inverse_of_numbers_is_exact()
    {
        Assert.Equal(M(["-2", "1"], ["3/2", "-1/2"]), Matrix<Expr>.FromRows([1, 2], [3, 4]).Inverse());
        Assert.Equal(Matrix<Expr>.Identity(3), Regular3 * Regular3.Inverse());
    }

    [Fact]
    public void Inverse_of_a_symbolic_matrix()
    {
        Matrix<Expr> inverse = Abcd.Inverse();
        var numeric = Matrix<double>.FromRows([1, 2], [3, 5]).Inverse();

        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                Assert.Equal(numeric[i, j], inverse[i, j].Evaluate(("a", 1), ("b", 2), ("c", 3), ("d", 5)), precision: 12);
    }

    [Fact]
    public void Inverse_is_undefined_only_where_the_matrix_is_singular()
    {
        // Elimination with x as the pivot would divide by x; [[0, 1], [1, 0]] is its own inverse.
        Matrix<Expr> inverse = M(["x", "1"], ["1", "x"]).Inverse();

        Assert.Equal(Matrix<double>.FromRows([0, 1], [1, 0]), inverse.Map(e => e.Evaluate(0)));
        Assert.True(double.IsNaN(inverse[0, 1].Evaluate(1)) || double.IsInfinity(inverse[0, 1].Evaluate(1)));
    }

    [Fact]
    public void Inverse_of_a_rotation()
    {
        Assert.Equal(M(["cos(x)", "sin(x)"], ["-sin(x)", "cos(x)"]),
            M(["cos(x)", "-sin(x)"], ["sin(x)", "cos(x)"]).Inverse());
    }

    [Fact]
    public void Singular_matrices_have_no_inverse()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => M(["x", "x + 1"], ["2x", "2x + 2"]).Inverse());
        Assert.Equal("The matrix is singular: its determinant is 0.", ex.Message);
        Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.FromRows([1, 2], [2, 4]).Solve([1, 2]));
    }

    [Fact]
    public void Solve_by_Cramers_rule()
    {
        Assert.Equal(Vector.Create<Expr>(1, 2), Matrix<Expr>.FromRows([4, 3], [6, 3]).Solve([10, 12]));

        Vector<Expr> x = Abcd.Solve([P("e"), P("f")]);
        AssertSame(P("(d*e - b*f) / (a*d - b*c)"), x[0]);
        AssertSame(P("(a*f - c*e) / (a*d - b*c)"), x[1]);
    }

    [Fact]
    public void Solve_several_right_hand_sides_at_once()
    {
        var x = Matrix<Expr>.FromRows([1, 0], [2, -1], [3, 4]);

        Assert.Equal(x, Regular3.Solve(Regular3 * x));
        Assert.StartsWith("The right-hand side has 2 rows, but the matrix has 3.",
            Assert.Throws<ArgumentException>(() => Regular3.Solve(Matrix<Expr>.Zero(2, 1))).Message);
    }

    [Fact]
    public void Exact_results_agree_with_floating_point()
    {
        var random = new Random(7);
        var numbers = Matrix<double>.Create(8, 8, (_, _) => random.Next(-9, 10));
        var exact = numbers.Map(v => (Expr)(int)v);

        Assert.Equal(new Constant(418040793), exact.Determinant());
        Assert.Equal(418040793, numbers.Determinant(), precision: 4);
        Assert.Equal(Matrix<Expr>.Identity(8), exact * exact.Inverse());
    }
}

public class SymbolicEntryWiseTests
{
    private static Expr P(string text) => ExprParser.Parse(text).Simplify();

    [Fact]
    public void Parse_reads_rows_of_expressions()
    {
        var m = Matrix<Expr>.Parse("[[x^2, min(a, b)], [ log(x, 2) , 1/2 ]]");

        Assert.Equal(2, m.Rows);
        Assert.Equal(2, m.Columns);
        Assert.Equal(ExprParser.Parse("min(a, b)"), m[0, 1]);
        Assert.Equal(ExprParser.Parse("log(x, 2)"), m[1, 0]);
        // Not simplified, as with ExprParser.Parse.
        Assert.Equal("1 / 2", m[1, 1].Print());
    }

    [Fact]
    public void Parse_with_declared_variables()
    {
        Assert.Equal("[[theta, 2t]]", Matrix<Expr>.Parse("[[theta, 2t]]", ["theta", "t"]).Print());
        Assert.Equal("[theta^2]", Vector<Expr>.Parse("[theta^2]", ["theta"]).Print());
    }

    [Fact]
    public void Parse_empty_matrices_and_vectors()
    {
        Assert.Equal(0, Matrix<Expr>.Parse("[]").Rows);
        Assert.Equal((1, 0), (Matrix<Expr>.Parse("[[]]").Rows, Matrix<Expr>.Parse("[[]]").Columns));
        Assert.Equal(0, Vector<Expr>.Parse(" [ ] ").Length);
    }

    [Theory]
    [InlineData("[[1, 2], [3]]", "Row 1 has 1 entries, but row 0 has 2.")]
    [InlineData("[[1, 2], [3, 4]", "Expected ']' at position 15, found the end of the text.")]
    [InlineData("[[1]] x", "Unexpected 'x' at position 6 after the closing ']'.")]
    [InlineData("[[1, [2]]]", "Unexpected '[' at position 5.")]
    [InlineData("1, 2", "Expected '[' at position 0, found '1'.")]
    [InlineData("[[1, sen(x)]]", "Invalid entry at (0, 1), starting at position 5: Unknown identifier 'sen' at position 0. Declared variables: x.")]
    public void Parse_errors_say_where(string text, string message)
    {
        var ex = Assert.Throws<FormatException>(() => Matrix<Expr>.Parse(text, ["x"]));
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Vector_parse_errors_name_the_index()
    {
        var ex = Assert.Throws<FormatException>(() => Vector<Expr>.Parse("[1, , 2]"));
        Assert.StartsWith("Invalid entry at index 1, starting at position 4:", ex.Message);
    }

    [Fact]
    public void Print_is_read_back_by_Parse()
    {
        var inverse = Matrix<Expr>.Parse("[[a, b], [c, d]]").Inverse();
        Vector<Expr> v = [P("x^2"), P("1/2")];

        Assert.Equal(inverse, Matrix<Expr>.Parse(inverse.Print()));
        Assert.Equal("[x^2, 1/2]", v.Print());
        // Parse reads 1/2 as a division, as ExprParser does; Simplify makes it the number again.
        Assert.Equal(v, Vector<Expr>.Parse(v.Print()).Simplify());
    }

    [Fact]
    public void LaTeX_uses_bmatrix()
    {
        Assert.Equal(@"\begin{bmatrix} x^{2} & \frac{1}{2} \\ 0 & 1 \end{bmatrix}",
            Matrix<Expr>.Parse("[[x^2, 1/2], [0, 1]]").Simplify().ToLatex());
        Assert.Equal(@"\begin{bmatrix} x^{2} \\ \frac{1}{2} \end{bmatrix}",
            Vector<Expr>.Parse("[x^2, 1/2]").Simplify().ToLatex());
    }

    [Fact]
    public void Simplify_and_expand_every_entry()
    {
        Assert.Equal("[[1, 3/10]]", Matrix<Expr>.Parse("[[x/x, 0.1 + 0.2]]").Simplify().Print());
        Assert.Equal("[[x / x]]", Matrix<Expr>.Parse("[[x/x]]").Simplify(SimplifyMode.Strict).Print());
        Assert.Equal("[[x]]", Matrix<Expr>.Parse("[[sqrt(x^2)]]").Simplify(Assumptions.None.AssumePositive("x")).Print());
        Assert.Equal("[[x^2 + 2x + 1]]", Matrix<Expr>.Parse("[[(x + 1)^2]]").Expand().Print());
        Assert.Equal("[2x + 1]", Vector<Expr>.Parse("[(x + 1)^2 - x^2]").Expand().Print());
    }

    [Fact]
    public void Differentiate_and_substitute_every_entry()
    {
        Assert.Equal("[[2x, cos(x)], [y, 0]]", Matrix<Expr>.Parse("[[x^2, sin(x)], [x*y, 1]]").Differentiate("x").Print());
        Assert.Equal("[2x, 0]", Vector<Expr>.Parse("[x^2 + y, y]").Differentiate("x").Print());
        // Not simplified, as with Expr.Substitute.
        Assert.Equal("[[y + 1, (y + 1)^2]]", Matrix<Expr>.Parse("[[x, x^2]]").Substitute("x", ExprParser.Parse("y + 1")).Print());
    }

    [Fact]
    public void Variables_of_all_entries()
    {
        Assert.Equal(["x", "y", "z"], Matrix<Expr>.Parse("[[x, y], [1, z]]").GetVariables().Order());
        Assert.Empty(Vector<Expr>.Parse("[1, pi]").GetVariables());
    }

    [Fact]
    public void Evaluate_every_entry()
    {
        var m = Matrix<Expr>.Parse("[[x, x*y], [1, y^2]]");

        Assert.Equal(Matrix<double>.FromRows([2, 6], [1, 9]), m.Evaluate(("x", 2), ("y", 3)));
        Assert.Equal(Matrix<double>.FromRows([2, 6], [1, 9]), m.Evaluate(new Dictionary<string, double> { ["x"] = 2, ["y"] = 3 }));
        Assert.Equal(Matrix<double>.FromRows([3, 9]), Matrix<Expr>.Parse("[[x, x^2]]").Evaluate(3));
        Assert.Equal([2.0, 5.0], Vector<Expr>.Parse("[2, x]").Evaluate(5));
        Assert.Equal([2.0, 1.0], Vector<Expr>.Parse("[2, 1]").Evaluate(7));
    }

    [Fact]
    public void Evaluate_with_one_value_needs_at_most_one_variable()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Matrix<Expr>.Parse("[[x, y]]").Evaluate(3));
        Assert.Equal("Expected at most 1 variable, found 2: [x, y]. Use the overload with named values for several variables.", ex.Message);
        Assert.Throws<ArgumentException>(() => Matrix<Expr>.Parse("[[x, y]]").Evaluate(("x", 1)));
    }

    [Fact]
    public void Evaluate_over_the_complex_numbers()
    {
        ComplexNumber z = Vector<Expr>.Parse("[sqrt(x)]").EvaluateComplex(-4)[0];
        Assert.Equal(0, z.Real, precision: 12);
        Assert.Equal(2, z.Imaginary, precision: 12);

        Matrix<ComplexNumber> m = Matrix<Expr>.Parse("[[x + y]]").EvaluateComplex(("x", ComplexNumber.ImaginaryUnit), ("y", 1));
        Assert.Equal(new ComplexNumber(1, 1), m[0, 0]);
    }
}
