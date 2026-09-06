namespace ViciOne.ServiceBus.Advanced;

/// <summary>Parses a textual <see cref="NewId" /> representation.</summary>
public interface INewIdParser
{
    /// <summary>Parses the supplied representation.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The parsed value.</returns>
    NewId Parse(ReadOnlySpan<char> text);
}
