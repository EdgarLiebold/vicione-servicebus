namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Parses a textual <see cref="NewId" /> representation.
/// </summary>
public interface INewIdParser
{
    /// <summary>
    /// Parses the supplied representation.
    /// </summary>
    /// <param name="text">The encoded identifier text.</param>
    /// <returns>The parsed identifier.</returns>
    NewId Parse(ReadOnlySpan<char> text);
}
