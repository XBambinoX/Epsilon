namespace Epsilon.Core;

/// <summary>Multiplying out products and powers of sums.</summary>
public static class Expander
{
    /// <summary>
    /// Multiplies out products and positive integer powers of sums and combines like terms:
    /// <c>(x + 1)^2 - x^2</c> becomes <c>2x + 1</c> and <c>(x + y)*(x - y)</c> becomes
    /// <c>x^2 - y^2</c>. Function arguments are expanded too. A quotient stays one fraction with
    /// its numerator and denominator expanded, except that dividing by a number divides every
    /// term: <c>(x + 1)^2/2</c> becomes <c>x^2/2 + x + 1/2</c>. Negative powers such as
    /// <c>(x + 1)^-2</c> are left as they are.
    /// </summary>
    /// <param name="expr">The expression to expand.</param>
    /// <param name="mode">
    /// The mode the input and the result are simplified in, see
    /// <see cref="Simplifier.Simplify(Expr, SimplifyMode)"/>. In Generic mode
    /// <c>(sqrt(x) + 1)^2</c> becomes <c>x + 2sqrt(x) + 1</c>; in Strict mode <c>sqrt(x)^2</c>
    /// stays, as it is undefined for x &lt; 0.
    /// </param>
    /// <returns>An equivalent expression, simplified.</returns>
    public static Expr Expand(this Expr expr, SimplifyMode mode = SimplifyMode.Generic) =>
        ExpandTerms(expr.Simplify(mode), mode).ToExpr().Simplify(mode);

    private static readonly Constant One = new(1);

    // The input is simplified, so its constants are folded and the factors of its products
    // merged: x*x*(x + 1) arrives as x^2 * (x + 1).
    private static TermSum ExpandTerms(Expr expr, SimplifyMode mode)
    {
        switch (expr)
        {
            case Add(var left, var right):
            {
                TermSum sum = ExpandTerms(left, mode);
                sum.Add(Rational.One, ExpandTerms(right, mode));
                return sum;
            }

            case Subtract(var left, var right):
            {
                TermSum sum = ExpandTerms(left, mode);
                sum.Add(Rational.MinusOne, ExpandTerms(right, mode));
                return sum;
            }

            case Negate(var argument):
            {
                var sum = new TermSum();
                sum.Add(Rational.MinusOne, ExpandTerms(argument, mode));
                return sum;
            }

            case Multiply(var left, var right):
                return Multiply(ExpandTerms(left, mode), ExpandTerms(right, mode), mode);

            case Divide(var numerator, var denominator):
                return ExpandQuotient(numerator, denominator, mode);

            case Power(var baseExpr, Constant exponent) when IsPositiveInt(exponent.Value, out int n):
                return Pow(ExpandTerms(baseExpr, mode), n, mode);

            // Constants, variables, functions, other powers: only the children are expanded.
            default:
                return TermSum.Of(Rational.One, expr.MapChildren(child => ExpandTerms(child, mode).ToExpr()));
        }
    }

    private static bool IsPositiveInt(Rational value, out int n)
    {
        bool fits = value.IsInteger && value.Sign > 0 && value.Numerator <= int.MaxValue;
        n = fits ? (int)value.Numerator : 0;
        return fits;
    }

    private static TermSum Multiply(TermSum left, TermSum right, SimplifyMode mode)
    {
        var product = new TermSum();
        foreach (var (a, x) in left.Terms)
            foreach (var (b, y) in right.Terms)
                product.Add(a * b, MultiplyTerms(x, y, mode));
        return product;
    }

    // Squares and multiplies, combining like terms after every step: (x + 1)^8 is
    // (((x + 1)^2)^2)^2, three multiplications of sums of at most 5 terms.
    private static TermSum Pow(TermSum terms, int n, SimplifyMode mode)
    {
        TermSum? result = null;
        TermSum square = terms;

        while (true)
        {
            if ((n & 1) != 0)
                result = result is null ? square : Multiply(result, square, mode);

            n >>= 1;
            if (n == 0)
                return result!;

            square = Multiply(square, square, mode);
        }
    }

    // The product of two terms of expanded sums, expanded.
    private static TermSum MultiplyTerms(Expr x, Expr y, SimplifyMode mode)
    {
        // The only constant term is 1 (TermSum keeps the value in the coefficient).
        if (x is Constant)
            return TermSum.Of(Rational.One, y);
        if (y is Constant)
            return TermSum.Of(Rational.One, x);

        // Simplify would put the product of the numerators together without multiplying it
        // out: x * (x + 1)/y -> (x + 1) * x / y. So fractions are multiplied here.
        if (x is Divide || y is Divide)
        {
            var (xNumerator, xDenominator) = x is Divide(var n1, var d1) ? (n1, d1) : (x, (Expr)One);
            var (yNumerator, yDenominator) = y is Divide(var n2, var d2) ? (n2, d2) : (y, (Expr)One);
            return ExpandQuotient(
                new Multiply(xNumerator, yNumerator), new Multiply(xDenominator, yDenominator), mode);
        }

        // Simplify merges the factors: x * x^2 -> x^3, y * x -> x * y (one order for like terms).
        // Merged powers can bring back something to multiply out: in Generic mode
        // (x + 1)^(1/2) * (x + 1)^(3/2) is (x + 1)^2.
        Expr product = new Multiply(x, y).Simplify(mode);
        return HasSomethingToExpand(product)
            ? ExpandTerms(product, mode)
            : TermSum.Of(Rational.One, product);
    }

    private static bool HasSomethingToExpand(Expr product) => product switch
    {
        Add or Subtract or Divide => true,
        Negate(var argument) => HasSomethingToExpand(argument),
        Multiply(var left, var right) => HasSomethingToExpand(left) || HasSomethingToExpand(right),
        Power(var baseExpr, Constant exponent) when IsPositiveInt(exponent.Value, out _) => HasSomethingToExpand(baseExpr),
        _ => false
    };

    // A quotient stays one fraction, with the numerator and the denominator expanded on their own.
    // Dividing by a number is a coefficient, though: (x^2 + 2x + 1)/2 -> x^2/2 + x + 1/2.
    private static TermSum ExpandQuotient(Expr numerator, Expr denominator, SimplifyMode mode)
    {
        TermSum top = ExpandTerms(numerator, mode);
        Expr bottom = ExpandTerms(denominator, mode).ToExpr();

        if (bottom is Constant c && !c.Value.IsZero)
        {
            var scaled = new TermSum();
            scaled.Add(Rational.One / c.Value, top);
            return scaled;
        }

        // A single term keeps its coefficient out of the fraction, so 2x/y and x/y are like terms.
        var terms = top.Terms.ToList();
        return terms.Count == 1
            ? TermSum.Of(terms[0].Coefficient, new Divide(terms[0].Term, bottom))
            : TermSum.Of(Rational.One, new Divide(top.ToExpr(), bottom));
    }

    // A sum being expanded: distinct terms with rational coefficients, in order of first
    // appearance. Like terms are added up as they come, which keeps powers of sums from
    // growing exponentially. The constant part is the coefficient of the term 1.
    private sealed class TermSum
    {
        private readonly List<(Rational Coefficient, Expr Term)> _terms = [];
        private readonly Dictionary<Expr, int> _index = [];

        public static TermSum Of(Rational coefficient, Expr term)
        {
            var sum = new TermSum();
            sum.Add(coefficient, term);
            return sum;
        }

        public IEnumerable<(Rational Coefficient, Expr Term)> Terms =>
            _terms.Where(t => !t.Coefficient.IsZero);

        // A constant factor goes into the coefficient, so 2x and -x are like terms of x.
        public void Add(Rational coefficient, Expr term)
        {
            switch (term)
            {
                case Constant c:
                    Accumulate(coefficient * c.Value, One);
                    break;

                case Negate(var argument):
                    Add(-coefficient, argument);
                    break;

                case Multiply(Constant c, var rest):
                    Add(coefficient * c.Value, rest);
                    break;

                default:
                    Accumulate(coefficient, term);
                    break;
            }
        }

        public void Add(Rational factor, TermSum other)
        {
            foreach (var (coefficient, term) in other.Terms)
                Add(factor * coefficient, term);
        }

        private void Accumulate(Rational coefficient, Expr term)
        {
            if (coefficient.IsZero)
                return;

            if (_index.TryGetValue(term, out int i))
            {
                _terms[i] = (_terms[i].Coefficient + coefficient, _terms[i].Term);
            }
            else
            {
                _index[term] = _terms.Count;
                _terms.Add((coefficient, term));
            }
        }

        public Expr ToExpr()
        {
            Rational constant = Rational.Zero;
            var terms = new List<(Rational Coefficient, Expr Term)>();

            foreach (var (coefficient, term) in Terms)
            {
                if (term is Constant)
                    constant += coefficient;
                else
                    terms.Add((coefficient, term));
            }

            return Simplifier.BuildSum(terms, constant);
        }
    }
}
