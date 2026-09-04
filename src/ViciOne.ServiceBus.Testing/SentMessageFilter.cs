namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a sent message filter implementation.
/// </summary>
public class SentMessageFilter
{
    readonly SentMessageFilterSet _excludes = new SentMessageFilterSet();
    readonly SentMessageFilterSet _includes = new SentMessageFilterSet();

    /// <summary>
    /// Gets the includes value.
    /// </summary>
    public SentMessageFilterSet Includes => _includes;

    /// <summary>
    /// Gets the excludes value.
    /// </summary>
    public SentMessageFilterSet Excludes => _excludes;

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="element">The element value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Any(ISentMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    /// <summary>
    /// Performs the none operation.
    /// </summary>
    /// <param name="element">The element value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool None(ISentMessage element)
    {
        return _includes.None(element);
    }
}
