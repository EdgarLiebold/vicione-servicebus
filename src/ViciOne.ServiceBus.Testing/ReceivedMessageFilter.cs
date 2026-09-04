namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a received message filter implementation.
/// </summary>
public class ReceivedMessageFilter
{
    readonly ReceivedMessageFilterSet _excludes = new ReceivedMessageFilterSet();
    readonly ReceivedMessageFilterSet _includes = new ReceivedMessageFilterSet();

    /// <summary>
    /// Gets the includes value.
    /// </summary>
    public ReceivedMessageFilterSet Includes => _includes;

    /// <summary>
    /// Gets the excludes value.
    /// </summary>
    public ReceivedMessageFilterSet Excludes => _excludes;

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="element">The element value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Any(IReceivedMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    /// <summary>
    /// Performs the none operation.
    /// </summary>
    /// <param name="element">The element value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool None(IReceivedMessage element)
    {
        return _includes.None(element);
    }
}
