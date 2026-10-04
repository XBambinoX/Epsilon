namespace Epsilon.LinearAlgebra;

// Splits "[[a, b], [c, d]]" and "[a, b]" into the texts of their entries. A comma separates
// entries only outside parentheses, so min(a, b) stays one entry; the entries themselves are
// left to ExprParser.
internal sealed class MatrixParser(string text)
{
    private int _position;

    public static List<List<(string Text, int Position)>> ParseRows(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var parser = new MatrixParser(text);
        var rows = new List<List<(string, int)>>();

        parser.Expect('[');
        if (!parser.TryRead(']'))
        {
            do
                rows.Add(parser.ReadEntries());
            while (parser.TryRead(','));

            parser.Expect(']');
        }

        parser.ExpectEnd();
        return rows;
    }

    public static List<(string Text, int Position)> ParseVector(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var parser = new MatrixParser(text);
        var entries = parser.ReadEntries();
        parser.ExpectEnd();
        return entries;
    }

    // "[e, e, ...]" or "[]": the entry texts, untrimmed, with the position where each starts.
    private List<(string Text, int Position)> ReadEntries()
    {
        var entries = new List<(string, int)>();

        Expect('[');
        if (TryRead(']'))
            return entries;

        while (true)
        {
            int start = _position;
            int depth = 0;

            for (; _position < text.Length; _position++)
            {
                char c = text[_position];
                if (c == '(')
                    depth++;
                else if (c == ')')
                    depth--;
                else if (c == '[')
                    throw new FormatException($"Unexpected '[' at position {_position}.");
                else if (depth <= 0 && (c == ',' || c == ']'))
                    break;
            }

            if (_position == text.Length)
                throw new FormatException($"Missing ']' at position {_position}.");

            entries.Add((text[start.._position], start));
            if (text[_position++] == ']')
                return entries;
        }
    }

    private bool TryRead(char expected)
    {
        SkipSpaces();
        if (_position < text.Length && text[_position] == expected)
        {
            _position++;
            return true;
        }

        return false;
    }

    private void Expect(char expected)
    {
        if (!TryRead(expected))
            throw new FormatException(_position < text.Length
                ? $"Expected '{expected}' at position {_position}, found '{text[_position]}'."
                : $"Expected '{expected}' at position {_position}, found the end of the text.");
    }

    private void ExpectEnd()
    {
        SkipSpaces();
        if (_position < text.Length)
            throw new FormatException($"Unexpected '{text[_position]}' at position {_position} after the closing ']'.");
    }

    private void SkipSpaces()
    {
        while (_position < text.Length && char.IsWhiteSpace(text[_position]))
            _position++;
    }
}
