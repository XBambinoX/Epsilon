namespace Epsilon.Core;

/// <summary>Algebraic simplification of expressions.</summary>
public static class Simplifier
{
    /// <summary>
    /// Simplifies without any assumptions about the variables: folds constants, combines like terms
    /// and factors (<c>2x + 3x = 5x</c>, <c>x*x = x^2</c>), cancels common factors and applies
    /// identities such as <c>sin(x)^2 + cos(x)^2 = 1</c>.
    /// </summary>
    /// <param name="expr">The expression to simplify.</param>
    /// <param name="mode">
    /// Whether rewrites may enlarge the domain (<see cref="SimplifyMode.Generic"/>, the default:
    /// <c>x/x = 1</c>) or must keep it exactly (<see cref="SimplifyMode.Strict"/>).
    /// </param>
    /// <returns>An equivalent expression in canonical form.</returns>
    public static Expr Simplify(this Expr expr, SimplifyMode mode = SimplifyMode.Generic) =>
        expr.Simplify(Assumptions.None, mode);

    /// <summary>
    /// Simplifies using what is known about the variables, which unlocks rules that are only valid
    /// under conditions: with x &gt; 0, <c>sqrt(x^2) = x</c> instead of <c>|x|</c>.
    /// </summary>
    /// <param name="expr">The expression to simplify.</param>
    /// <param name="assumptions">What is known about the variables.</param>
    /// <param name="mode">See <see cref="Simplify(Expr, SimplifyMode)"/>.</param>
    /// <returns>An equivalent expression in canonical form.</returns>
    public static Expr Simplify(this Expr expr, Assumptions assumptions, SimplifyMode mode = SimplifyMode.Generic)
    {
        return IterateToFixpoint(expr.Canonicalize(), e => SimplifyOnce(e, assumptions, mode));
    }

    private const int MaxSimplifyIterations = 100;
    private const int UntrackedIterations = 8;

    // Applies `step` until the expression stops changing. Every step is an equivalent rewrite,
    // so if two rules undo each other (a cycle) or the steps never settle, any expression seen
    // so far is a correct answer: the smallest one is returned instead of throwing at the user.
    // A repeated expression ends the loop at once, and the choice doesn't depend on where in
    // the cycle it was noticed.
    internal static Expr IterateToFixpoint(Expr start, Func<Expr, Expr> step, int maxIterations = MaxSimplifyIterations)
    {
        // Fast path: almost every call settles within a step or two. Simplify runs this for
        // every subtree, so the history below is only built for the rare slow cases.
        Expr current = start;
        int untracked = Math.Min(UntrackedIterations, maxIterations);
        for (int i = 0; i < untracked; i++)
        {
            Expr next = step(current);
            if (next.Equals(current))
                return next;
            current = next;
        }

        // Walks on until an expression repeats, so a cycle is always seen in full, whichever
        // of its expressions the fast path stopped at.
        var seen = new List<Expr> { current };
        var seenSet = new HashSet<Expr> { current };

        for (int i = untracked; i < maxIterations; i++)
        {
            Expr next = step(current);

            if (next.Equals(current))
                return next;

            if (!seenSet.Add(next))
                break;

            seen.Add(next);
            current = next;
        }

        // The start is a candidate too: if the steps only ever grow it, it's the best answer.
        // Ties are broken by the printed form, not by order, which depends on the entry point.
        return seen.Append(start)
            .OrderBy(NodeCount)
            .ThenBy(e => e.ToString(), StringComparer.Ordinal)
            .First();
    }

    private static int NodeCount(Expr expr)
    {
        int count = 1;
        foreach (Expr child in expr.Children)
            count += NodeCount(child);
        return count;
    }

    private static Expr SimplifyOnce(Expr expr, Assumptions assumptions, SimplifyMode mode)
    {
        Expr simplifiedChildren = expr.MapChildren(child => child.Simplify(assumptions, mode));
        return ApplyRules(simplifiedChildren, assumptions, mode).Canonicalize();
    }

    // Domain guards. A rewrite that only enlarges the domain (x/x -> 1 gains x = 0) is
    // allowed in Generic mode, but in Strict mode needs a proof from the assumptions.
    // Generic mode still rejects a literal 0, so 0/0 and similar are never folded.
    private static bool NonZeroForDomain(Expr expr, Assumptions assumptions, SimplifyMode mode) =>
        mode == SimplifyMode.Generic
            ? !(expr is Constant c && c.Value.IsZero)
            : expr.IsProvablyNonZero(assumptions);

    private static bool NonNegativeForDomain(Expr expr, Assumptions assumptions, SimplifyMode mode) =>
        mode == SimplifyMode.Generic
            ? !expr.IsProvablyNegative(assumptions)
            : expr.IsProvablyNonNegative(assumptions);

    private static bool PositiveForDomain(Expr expr, Assumptions assumptions, SimplifyMode mode) =>
        mode == SimplifyMode.Generic
            ? !expr.IsProvablyNonPositive(assumptions)
            : expr.IsProvablyPositive(assumptions);

    private static Expr ApplyRules(Expr expr, Assumptions assumptions, SimplifyMode mode)
    {
        Expr flattened = FlattenAndCombine(expr);
        if (!flattened.Equals(expr))
            return flattened.Canonicalize();

        Expr combinedProduct = CombineProductFactors(expr, assumptions, mode);
        if (!combinedProduct.Equals(expr))
            return combinedProduct.Canonicalize();

        switch (expr)
        {
            case Add(Constant a, Constant b):
                return new Constant(a.Value + b.Value);

            case Add(var l, var r) when r.Equals(new Constant(0)):
                return l;

            case Add(var l, var r) when l.Equals(new Constant(0)):
                return r;

            case Subtract(Constant a, Constant b):
                return new Constant(a.Value - b.Value);

            case Subtract(var l, var r) when l.Equals(r):
                return new Constant(0);

            case Subtract(Constant zero, var x) when zero.Value.IsZero:
                return new Negate(x);

            case Negate(Constant c):
                return new Constant(-c.Value);

            case Negate(Negate(var a)):
                return a;

            // a - (-b) = a + b
            case Subtract(var a, Negate(var b)):
                return new Add(a, b);

            case Subtract(var l, var r) when r.Equals(new Constant(0)):
                return l;

            // Constant folding, 1 * x and equal-base merging for products are all done by
            // CombineProductFactors above; only 0 * x is deliberately left to this rule.
            case Multiply(var l, var r) when l.Equals(new Constant(0)) || r.Equals(new Constant(0)):
                return new Constant(0);

            case Divide(var numerator, var denominator)
                when TryCancelCommonFactors(numerator, denominator, assumptions, mode, out Expr? cancelled):
                return cancelled!;

            case Divide(Constant a, Constant b) when !b.Value.IsZero:
                return new Constant(a.Value / b.Value);

            // 0 / d = 0 - enlarges the domain by the zeros of d, so Strict mode needs d provably
            // nonzero. Never applies to a literal 0/0, in either mode.
            case Divide(Constant zero, var d) when zero.Value.IsZero && NonZeroForDomain(d, assumptions, mode):
                return new Constant(0);

            case Divide(var n, var d) when d.Equals(new Constant(1)):
                return n;

            // Integer exponent: exact BigInteger power. 0^negative is undefined
            // (division by zero), so that combination is excluded and left symbolic.
            case Power(Constant b, Constant e)
                when e.Value.IsInteger && !(b.Value.IsZero && e.Value.Sign < 0):
                return new Constant(b.Value.Pow((int)e.Value.Numerator));

            // Root exponent (+-1/n): exact result only if b is a perfect n-th power.
            // NOT approximated via Math.Pow - an inexact root stays symbolic rather than
            // silently becoming a "precise-looking" but wrong Rational. Negative bases are
            // left alone too: Power is the principal value, unlike the real-valued NthRoot.
            case Power(Constant b, Constant e)
                when System.Numerics.BigInteger.Abs(e.Value.Numerator) == 1 &&
                     TryExactRoot(b.Value, e.Value.Denominator, out Rational rootValue):
                return e.Value.Numerator.Sign > 0
                    ? new Constant(rootValue)
                    : new Constant(Rational.One / rootValue);

            case Power(var b, var e) when e.Equals(new Constant(0)):
                return new Constant(1);

            case Power(var b, var e) when e.Equals(new Constant(1)):
                return b;

            case Power(var b, var e) when b.Equals(new Constant(0)) && e.IsProvablyPositive(assumptions):
                return new Constant(0);

            // sqrt(a)^2 = a; the left side is undefined for a < 0, so this enlarges the domain.
            case Power(Sqrt(var a), Constant e) when e.Value == 2 && NonNegativeForDomain(a, assumptions, mode):
                return a;

            case Power(Power(var b, var e1), var e2):
                // Safe to collapse unconditionally only when e1 is an odd integer (sign-preserving:
                // x -> x^e1 never erases the sign of b, so composing exponents afterward can't lose it).
                // Additionally, (x^-1)^-1 = x would become defined at x = 0, so a negative inner
                // exponent needs either a nonzero base or a positive outer exponent.
                Expr combined = new Power(b, new Multiply(e1, e2));
                bool zeroBaseSafe = NonZeroForDomain(b, assumptions, mode) ||
                                    e1.IsProvablyPositive(assumptions) ||
                                    e2.IsProvablyPositive(assumptions);
                if (!zeroBaseSafe)
                    return expr;
                // Power is the principal value: a non-integer exponent of a negative base is
                // undefined over the reals. So (x^3)^(1/3) is undefined for x < 0 while x is not -
                // an odd inner exponent is only a domain enlargement unless e2 is an integer.
                bool outerIsInteger = e2 is Constant ce2 && ce2.Value.IsInteger;
                if (e1 is Constant ce1 && IsOddInteger(ce1.Value) && (outerIsInteger || mode == SimplifyMode.Generic))
                    return combined;
                // A non-integer inner exponent (x^(1/2), x^(1/3)) is undefined for b < 0, so
                // collapsing only enlarges the domain - fine in Generic mode. An even integer
                // inner exponent is different: (x^2)^(1/2) = |x|, not x, so no shortcut.
                bool innerUndefinedForNegativeBase = e1 is Constant ce1NonInt && !ce1NonInt.Value.IsInteger;
                return b.IsProvablyNonNegative(assumptions) ||
                       (mode == SimplifyMode.Generic && innerUndefinedForNegativeBase)
                    ? combined
                    : expr;

            // Still reachable: when CombineProductFactors may not add the exponents (Strict mode,
            // x^(1/2) * x^(1/2) would gain x < 0), the equal factors are squared as a whole instead.
            case Multiply(var b1, var b2) when b1.Equals(b2) && b1 is not Constant:
                return new Power(b1, new Constant(2));

            case Multiply(Divide(var a, var b), Divide(var c, var d)):
                return new Divide(new Multiply(a, c), new Multiply(b, d));

            case Multiply(Divide(var a, var b), var c) when c is not Divide:
                return new Divide(new Multiply(a, c), b);

            case Multiply(var c, Divide(var a, var b)) when c is not Divide:
                return new Divide(new Multiply(c, a), b);
                
            case Divide(Divide(var a, var b), var c):
                return new Divide(a, new Multiply(b, c));

            // x^n / x^m = x^(n-m)
            case Divide(Power(var b1, var e1), Power(var b2, var e2))
                when b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Power(b1, new Subtract(e1, e2));

            // x^n / x = x^(n-1)
            case Divide(Power(var b1, var e1), var b2)
                when b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Power(b1, new Subtract(e1, new Constant(1)));

            // (x^n * c) / x = c * x^(n-1)
            case Divide(Multiply(Power(var b1, var e1), var c), var b2)
                when b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Multiply(c, new Power(b1, new Subtract(e1, new Constant(1))));

            // (c * x^n) / x = c * x^(n-1)
            case Divide(Multiply(var c, Power(var b1, var e1)), var b2)
                when b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Multiply(c, new Power(b1, new Subtract(e1, new Constant(1))));

            // x / x^n = x^(1-n)
            case Divide(var b1, Power(var b2, var e2))
                when b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Power(b1, new Subtract(new Constant(1), e2));

            case Sin(Constant c) when c.Value.IsZero:
                return new Constant(0);

            case Cos(Constant c) when c.Value.IsZero:
                return new Constant(1);

            case Tan(Constant c) when c.Value.IsZero:
                return new Constant(0);

            // sin(x)^2 + cos(x)^2 = 1 and its variants live in FlattenAndCombine
            // (ApplyPythagoreanIdentity), where the whole sum is visible at once.

            // ln(exp(a)) = a: exp(a) is always strictly positive for real a,
            // so ln is always defined on its result - no assumption needed.
            case Ln(Exp(var a)):
                return a;

            case Exp(Ln(var a)) when PositiveForDomain(a, assumptions, mode):
                return a;

            // tan(x) = sin(x) / cos(x)
            case Divide(Sin(var x), Cos(var y)) when x.Equals(y):
                return new Tan(x);

            // cot(x) = cos(x) / sin(x)
            case Divide(Cos(var x), Sin(var y)) when x.Equals(y):
                return new Cot(x);

            // tan(x) * cot(x) = 1 only where both are defined: sin(x) != 0 and cos(x) != 0.
            case Multiply(Tan(var x), Cot(var y))
                when x.Equals(y) && NonZeroForDomain(new Sin(x), assumptions, mode) && NonZeroForDomain(new Cos(x), assumptions, mode):
                return new Constant(1);

            case Multiply(Cot(var x), Tan(var y))
                when x.Equals(y) && NonZeroForDomain(new Sin(x), assumptions, mode) && NonZeroForDomain(new Cos(x), assumptions, mode):
                return new Constant(1);

            // sin(x)^2 / cos(x)^2 = tan(x)^2
            case Divide(
                Power(Sin(var x1), Constant e1),
                Power(Cos(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Power(new Tan(x1), new Constant(2));

            // cos(x)^2 / sin(x)^2 = cot(x)^2
            case Divide(
                Power(Cos(var x1), Constant e1),
                Power(Sin(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Power(new Cot(x1), new Constant(2));

            // sec(x)^2 - tan(x)^2 = 1, valid only where cos(x) != 0
            case Subtract(
                Power(Sec(var x1), Constant e1),
                Power(Tan(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2) &&
                    NonZeroForDomain(new Cos(x1), assumptions, mode):
                return new Constant(1);

            // csc(x)^2 - cot(x)^2 = 1, valid only where sin(x) != 0
            case Subtract(
                Power(Csc(var x1), Constant e1),
                Power(Cot(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2) &&
                    NonZeroForDomain(new Sin(x1), assumptions, mode):
                return new Constant(1);

            // Exact perfect-square root; not a perfect square stays symbolic (falls through).
            case Sqrt(Constant c)
                when c.Value.Sign >= 0 && TryExactRoot(c.Value, 2, out Rational sqrtValue):
                return new Constant(sqrtValue);

            case Sqrt(Power(var b, Constant e)) when e.Value == 2:
                return b.IsProvablyNonNegative(assumptions)
                    ? b
                    : new Abs(b);

            // Exact real n-th root; anything inexact stays symbolic (falls through).
            case NthRoot(Constant c, Constant n) when TryExactRealRoot(c.Value, n.Value, out Rational nthRootValue):
                return new Constant(nthRootValue);

            case Abs(Constant c):
                return new Constant(c.Value.Abs());

            case Abs(var a) when a is Abs:
                return a;

            case Abs(var a) when a.IsProvablyNonNegative(assumptions):
                return a;

            case Abs(var a) when a.IsProvablyNegative(assumptions):
                return new Negate(a);

            case Sign(var a) when a.IsProvablyPositive(assumptions):
                return new Constant(1);

            case Sign(var a) when a.IsProvablyNegative(assumptions):
                return new Constant(-1);

            case Floor(Constant c):
                return new Constant(c.Value.Floor());

            case Ceiling(Constant c):
                return new Constant(c.Value.Ceiling());

            case Round(Constant c):
                return new Constant(c.Value.Round());

            case Min(Constant a, Constant b):
                return new Constant(a.Value < b.Value ? a.Value : b.Value);

            case Max(Constant a, Constant b):
                return new Constant(a.Value > b.Value ? a.Value : b.Value);

            default:
                return expr;
        }
    }

    // BigInteger.IsEven instead of a (long) cast, which overflows for huge integers.
    private static bool IsOddInteger(Rational value) =>
        value.IsInteger && !value.Numerator.IsEven;

    // Guards c^q in TryExactRealRoot against building astronomically large numbers.
    private const int MaxExactRootPower = 1024;

    // nthroot(c, p/q) = c^(q/p), computed exactly: raise |c| to q, take the |p|-th root,
    // invert for negative p. A negative c only has a real root for an odd integer degree
    // (same convention as NthRoot.Evaluate); otherwise it's undefined and left symbolic.
    private static bool TryExactRealRoot(Rational value, Rational degree, out Rational root)
    {
        root = default;
        if (degree.IsZero)
            return false;

        bool negativeBase = value.Sign < 0;
        if (negativeBase && !IsOddInteger(degree))
            return false;

        if (degree.Denominator > MaxExactRootPower)
            return false;

        Rational powered = value.Abs().Pow((int)degree.Denominator);
        if (!TryExactRoot(powered, System.Numerics.BigInteger.Abs(degree.Numerator), out Rational magnitude))
            return false;

        if (degree.Sign < 0)
        {
            if (magnitude.IsZero)
                return false; // nthroot(0, -n) = 1/0
            magnitude = Rational.One / magnitude;
        }

        root = negativeBase ? -magnitude : magnitude;
        return true;
    }

    // x^p * x^q = x^(p+q) must not enlarge the domain: x^-1 * x^2 is undefined at x = 0
    // but x^1 isn't, and x^(1/2) * x^(1/2) is undefined for x < 0 but x isn't.
    // Returns true only when both sides are defined at exactly the same points.
    private static bool CanMergeExponents(Expr baseExpr, Expr e1, Expr e2, Assumptions assumptions, SimplifyMode mode)
    {
        // Generic mode: x^p * x^q = x^(p+q) holds wherever the left side is defined.
        if (mode == SimplifyMode.Generic)
            return true;

        // Every real power of a positive base is defined.
        if (baseExpr.IsProvablyPositive(assumptions))
            return true;

        if (e1 is not Constant c1 || e2 is not Constant c2)
            return false; // symbolic exponents: sign/integrality unknown

        Rational p = c1.Value, q = c2.Value;

        if (p.IsInteger && q.IsInteger)
        {
            // Integer powers are defined for any nonzero base. At 0, both sides agree
            // as long as the exponents share a sign (both defined or both undefined).
            bool sameSign = (p.Sign >= 0 && q.Sign >= 0) || (p.Sign <= 0 && q.Sign <= 0);
            return sameSign || baseExpr.IsProvablyNonZero(assumptions);
        }

        if (p.Sign >= 0 && q.Sign >= 0)
        {
            // Non-negative exponents are all defined at 0 and for a non-negative base.
            if (baseExpr.IsProvablyNonNegative(assumptions))
                return true;

            // For a negative base: a non-integer sum means at least one factor was already
            // non-integer, so the left side is no more defined than the right side.
            // An integer sum (1/2 + 1/2) would newly define x^1 for x < 0 - not safe.
            return !(p + q).IsInteger;
        }

        return false;
    }

    private static bool TryIntegerNthRoot(System.Numerics.BigInteger value, int n, out System.Numerics.BigInteger root)
    {
        root = System.Numerics.BigInteger.Zero;
        if (value.Sign < 0 || n <= 0) return false;
        if (value.IsZero) return true;
        if (n == 1) { root = value; return true; }

        System.Numerics.BigInteger low = 0, high = value;
        while (low <= high)
        {
            System.Numerics.BigInteger mid = (low + high) / 2;
            System.Numerics.BigInteger midPow = System.Numerics.BigInteger.Pow(mid, n);

            if (midPow == value) { root = mid; return true; }
            if (midPow < value) low = mid + 1;
            else high = mid - 1;
        }

        return false;
    }

    // Exact n-th root of a non-negative rational: numerator and denominator
    // (coprime by Rational's construction) must each be a perfect n-th power.
    private static bool TryExactRoot(Rational value, System.Numerics.BigInteger n, out Rational root)
    {
        root = default;
        if (value.Sign < 0 || n <= 0) return false;

        int nn;
        try { nn = (int)n; }
        catch (OverflowException) { return false; }

        if (!TryIntegerNthRoot(value.Numerator, nn, out System.Numerics.BigInteger numRoot)) return false;
        if (!TryIntegerNthRoot(value.Denominator, nn, out System.Numerics.BigInteger denRoot)) return false;

        root = new Rational(numRoot, denRoot);
        return true;
    }

    private static (Rational Coefficient, Expr Term) ExtractCoefficient(Expr expr) => expr switch
    {
        Negate(var t) => (Rational.MinusOne, t),
        Multiply(Constant c, var t) => (c.Value, t),
        Multiply(var t, Constant c) => (c.Value, t),
        Divide(var t, Constant c) when !c.Value.IsZero => (Rational.One / c.Value, t),
        _ => (Rational.One, expr)
    };

    // Flattens a chain of Add/Subtract into a flat list of (coefficient, term) pairs.
    private static void CollectTerms(Expr expr, Rational sign, List<(Rational Coefficient, Expr Term)> terms)
    {
        switch (expr)
        {
            case Add(var l, var r):
                CollectTerms(l, sign, terms);
                CollectTerms(r, sign, terms);
                break;
            case Subtract(var l, var r):
                CollectTerms(l, sign, terms);
                CollectTerms(r, -sign, terms);
                break;
            default:
                var (coef, term) = ExtractCoefficient(expr);
                terms.Add((coef * sign, term));
                break;
        }
    }

    // Combines like terms across an entire Add/Subtract chain, then rebuilds it.
    private static bool IsSinSquared(Expr term, out Expr argument)
    {
        if (term is Power(Sin(var a), Constant e) && e.Value == 2)
        {
            argument = a;
            return true;
        }

        argument = null!;
        return false;
    }

    private static bool IsCosSquared(Expr term, out Expr argument)
    {
        if (term is Power(Cos(var a), Constant e) && e.Value == 2)
        {
            argument = a;
            return true;
        }

        argument = null!;
        return false;
    }

    // sin(x)^2 + cos(x)^2 = 1, applied to the whole flattened sum rather than to a fixed
    // two-node shape, so it works with other terms in between, any coefficients and any
    // order: sin^2 + cos^2 + 1 -> 2, 2sin^2 + 3cos^2 -> 2 + cos^2, 1 - sin^2 -> cos^2.
    // Holds for every real x, so it is valid in both Strict and Generic mode.
    private static void ApplyPythagoreanIdentity(
        List<(Rational Coefficient, Expr Term)> terms, Dictionary<Expr, int> termIndex, ref Rational constant)
    {
        // Step 1: a*sin^2 + b*cos^2 = a + (b - a)*cos^2 - eliminates the sin^2 term.
        for (int i = 0; i < terms.Count; i++)
        {
            var (sinCoef, sinTerm) = terms[i];
            if (sinCoef.IsZero || !IsSinSquared(sinTerm, out Expr x))
                continue;

            Expr cosSquared = new Power(new Cos(x), new Constant(2));
            if (!termIndex.TryGetValue(cosSquared, out int j) || terms[j].Coefficient.IsZero)
                continue;

            constant += sinCoef;
            terms[j] = (terms[j].Coefficient - sinCoef, terms[j].Term);
            terms[i] = (Rational.Zero, sinTerm);
        }

        // Step 2: c - c*sin^2 = c*cos^2 and c - c*cos^2 = c*sin^2. Runs after step 1, so
        // at most one of sin^2/cos^2 is left per argument and this can't undo step 1.
        for (int i = 0; i < terms.Count && !constant.IsZero; i++)
        {
            var (coef, term) = terms[i];
            if (coef != -constant)
                continue;

            Expr? swapped =
                IsSinSquared(term, out Expr sinArg) ? new Power(new Cos(sinArg), new Constant(2)) :
                IsCosSquared(term, out Expr cosArg) ? new Power(new Sin(cosArg), new Constant(2)) :
                null;

            if (swapped is null)
                continue;

            terms[i] = (constant, swapped);
            constant = Rational.Zero;
        }
    }

    // The product counterpart of FlattenAndCombine: works on a whole Multiply chain instead
    // of one binary node, so repeated factors merge even when they aren't adjacent in the
    // tree - x*y*x*y -> x^2*y^2 and 2*x*3*x -> 6*x^2 (the binary rules only caught the
    // first pair). Constants are multiplied together; equal bases get their exponents
    // added, subject to the same domain rules as the binary rules (CanMergeExponents).
    private static Expr CombineProductFactors(Expr expr, Assumptions assumptions, SimplifyMode mode)
    {
        if (expr is not Multiply)
            return expr;

        var factors = new List<Expr>();
        Rational coefficient = Rational.One;
        FlattenProduct(expr, factors, ref coefficient);

        var groups = new List<(Expr Base, Expr Exponent)>();

        foreach (Expr factor in factors)
        {
            if (factor is Constant c)
            {
                coefficient *= c.Value;
                continue;
            }

            var (baseExpr, exponent) = factor is Power(var b, var e) ? (b, e) : (factor, new Constant(1));

            // Merge into the first group with the same base that allows it; otherwise start
            // a new group (e.g. x and x^-1 stay apart in Strict mode unless x != 0 is known).
            int index = groups.FindIndex(g =>
                g.Base.Equals(baseExpr) && CanMergeExponents(baseExpr, g.Exponent, exponent, assumptions, mode));

            if (index >= 0)
                groups[index] = (baseExpr, AddExponents(groups[index].Exponent, exponent));
            else
                groups.Add((baseExpr, exponent));
        }

        // 0 * anything is left to the dedicated rule in ApplyRules.
        if (coefficient.IsZero)
            return expr;

        // A lone -1 is written as a negation (-(x^2*y)) rather than -1 * x^2 * y.
        bool negate = coefficient == Rational.MinusOne;
        Expr? result = coefficient.IsOne || negate ? null : new Constant(coefficient);
        foreach (var (baseExpr, exponent) in groups)
        {
            Expr term = exponent is Constant e && e.Value.IsOne ? baseExpr : new Power(baseExpr, exponent);
            result = result is null ? term : new Multiply(result, term);
        }

        result ??= new Constant(negate ? Rational.One : coefficient);
        return negate ? new Negate(result) : result;
    }

    // Negations anywhere in the chain are pulled out into the coefficient, so that
    // -x * y * x merges to -(x^2*y) and x * (-y) * x * (-y) to x^2 * y^2.
    private static void FlattenProduct(Expr expr, List<Expr> factors, ref Rational coefficient)
    {
        switch (expr)
        {
            case Multiply(var l, var r):
                FlattenProduct(l, factors, ref coefficient);
                FlattenProduct(r, factors, ref coefficient);
                break;

            case Negate(var inner):
                coefficient = -coefficient;
                FlattenProduct(inner, factors, ref coefficient);
                break;

            default:
                factors.Add(expr);
                break;
        }
    }

    private static Expr AddExponents(Expr e1, Expr e2) =>
        e1 is Constant c1 && e2 is Constant c2
            ? new Constant(c1.Value + c2.Value)
            : new Add(e1, e2);

    private static Expr FlattenAndCombine(Expr expr)
    {
        if (expr is not (Add or Subtract))
            return expr;

        var raw = new List<(Rational Coefficient, Expr Term)>();
        CollectTerms(expr, Rational.One, raw);

        Rational constantSum = Rational.Zero;
        var combined = new List<(Rational Coefficient, Expr Term)>();
        var termIndex = new Dictionary<Expr, int>();

        foreach (var (coef, term) in raw)
        {
            if (term is Constant c)
            {
                constantSum += coef * c.Value;
                continue;
            }

            if (termIndex.TryGetValue(term, out int existingIndex))
            {
                var (existingCoef, existingTerm) = combined[existingIndex];
                combined[existingIndex] = (existingCoef + coef, existingTerm);
            }
            else
            {
                termIndex[term] = combined.Count;
                combined.Add((coef, term));
            }
        }

        ApplyPythagoreanIdentity(combined, termIndex, ref constantSum);

        combined.RemoveAll(t => t.Coefficient.IsZero);

        Expr Rebuild(Rational coef, Expr term) =>
            coef.IsOne ? term :
            coef.Equals(Rational.MinusOne) ? new Negate(term) :
            new Multiply(new Constant(coef), term);

        if (combined.Count == 0)
            return new Constant(constantSum);

        Expr result = Rebuild(combined[0].Coefficient, combined[0].Term);
        for (int i = 1; i < combined.Count; i++)
        {
            var (coef, term) = combined[i];
            result = coef.Sign < 0
                ? new Subtract(result, Rebuild(-coef, term))
                : new Add(result, Rebuild(coef, term));
        }

        if (!constantSum.IsZero)
        {
            result = constantSum.Sign < 0
                ? new Subtract(result, new Constant(-constantSum))
                : new Add(result, new Constant(constantSum));
        }

        return result;
    }

    private static void CollectFactors(Expr expr, Dictionary<Expr, Rational> factors, ref Rational coefficient)
    {
        switch (expr)
        {
            case Multiply(var l, var r):
                CollectFactors(l, factors, ref coefficient);
                CollectFactors(r, factors, ref coefficient);
                break;

            case Negate(var inner):
                coefficient = -coefficient;
                CollectFactors(inner, factors, ref coefficient);
                break;

            case Constant c:
                coefficient *= c.Value;
                break;

            case Power(var b, Constant e) when e.Value.IsInteger:
                AddExponent(factors, b, e.Value);
                break;

            default:
                AddExponent(factors, expr, Rational.One);
                break;
        }
    }

    private static void AddExponent(Dictionary<Expr, Rational> factors, Expr baseExpr, Rational exponent)
    {
        factors[baseExpr] = factors.TryGetValue(baseExpr, out Rational existing)
            ? existing + exponent
            : exponent;
    }

    private static (Rational Coefficient, Dictionary<Expr, Rational> Factors) ExtractFactors(Expr expr)
    {
        var factors = new Dictionary<Expr, Rational>();
        Rational coefficient = Rational.One;
        CollectFactors(expr, factors, ref coefficient);
        return (coefficient, factors);
    }

    private static bool IsConstantOne(Expr e) => e is Constant c && c.Value.IsOne;

    private static Expr BuildProduct(Rational coefficient, Dictionary<Expr, Rational> factors)
    {
        Expr? result = coefficient.IsOne ? null : new Constant(coefficient);

        foreach (var (baseExpr, exponent) in factors)
        {
            Expr term = exponent.IsOne ? baseExpr : new Power(baseExpr, new Constant(exponent));
            result = result is null ? term : new Multiply(result, term);
        }

        return result ?? new Constant(coefficient); // everything cancelled — pure coefficient (often 1)
    }

    private static bool TryCancelCommonFactors(Expr numerator, Expr denominator, Assumptions assumptions, SimplifyMode mode, out Expr? result)
    {
        result = null;

        var (numCoefficient, numFactors) = ExtractFactors(numerator);
        var (denCoefficient, denFactors) = ExtractFactors(denominator);

        var cancellable = new List<Expr>();
        foreach (Expr baseExpr in numFactors.Keys)
            if (denFactors.ContainsKey(baseExpr) && NonZeroForDomain(baseExpr, assumptions, mode))
                cancellable.Add(baseExpr);

        if (cancellable.Count == 0)
            return false; // nothing safe to cancel - leave the whole node alone

        foreach (Expr baseExpr in cancellable)
        {
            Rational net = numFactors[baseExpr] - denFactors[baseExpr];
            numFactors.Remove(baseExpr);
            denFactors.Remove(baseExpr);

            if (net.IsZero) continue;
            if (net.Sign > 0) numFactors[baseExpr] = net;
            else denFactors[baseExpr] = -net;
        }

        Expr newNumerator = BuildProduct(numCoefficient, numFactors);
        Expr newDenominator = BuildProduct(denCoefficient, denFactors);

        result = IsConstantOne(newDenominator)
            ? newNumerator.Simplify(assumptions, mode)
            : new Divide(newNumerator, newDenominator).Simplify(assumptions, mode);

        return true;
    }
}