namespace Epsilon.LinearAlgebra;

// Checks the destination of the batch operations of the fixed-size matrices.
internal static class Destination
{
    // The destination must have room for the whole source. It may be the source itself, but must
    // not overlap it at another position, where results would overwrite vectors not yet read.
    internal static void Check<TItem>(ReadOnlySpan<TItem> source, Span<TItem> destination)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException($"The destination has room for {destination.Length} vectors, not {source.Length}.", nameof(destination));
        if (source.Overlaps(destination, out int offset) && offset != 0)
            throw new ArgumentException("The destination overlaps the source at another position.", nameof(destination));
    }
}
