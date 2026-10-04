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
        Assert.Equal("The trace is defined only for square matrices, not for a 2 x 3 matrix.", ex.Message);
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
