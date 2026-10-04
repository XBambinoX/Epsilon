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
