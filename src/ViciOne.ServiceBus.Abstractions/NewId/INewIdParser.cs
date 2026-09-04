namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for new id parser.
/// </summary>
public interface INewIdParser
{
    /// <summary>
    /// Parses the supplied representation.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <returns>The result of the operation.</returns>
    NewId Parse(in string text);
}
