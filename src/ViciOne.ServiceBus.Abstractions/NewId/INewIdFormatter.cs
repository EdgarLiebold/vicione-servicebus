namespace ViciOne.ServiceBus.Advanced;

/// <summary>Formats the canonical 16-byte representation of a <see cref="NewId" />.</summary>
public interface INewIdFormatter
{
    /// <summary>Formats one identifier without taking ownership of its bytes.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The formatted value.</returns>
    /// <exception cref="ArgumentException"><paramref name="bytes" /> does not contain exactly 16 bytes.</exception>
    string Format(ReadOnlySpan<byte> bytes);
}
