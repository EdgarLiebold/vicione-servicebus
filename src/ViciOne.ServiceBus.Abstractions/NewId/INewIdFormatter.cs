namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Formats the canonical 16-byte representation of a <see cref="NewId" />.
/// </summary>
public interface INewIdFormatter
{
    /// <summary>
    /// Formats one identifier without taking ownership of its bytes.
    /// </summary>
    /// <param name="bytes">The canonical 16-byte identifier representation.</param>
    /// <returns>The encoded identifier text.</returns>
    /// <exception cref="ArgumentException"><paramref name="bytes" /> does not contain exactly 16 bytes.</exception>
    string Format(ReadOnlySpan<byte> bytes);
}
