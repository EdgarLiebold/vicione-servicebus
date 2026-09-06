namespace ViciOne.ServiceBus.Testing;

/// <summary>Processes received message pipeline stages.</summary>
public class ReceivedMessageFilter
{
    readonly ReceivedMessageFilterSet _excludes = new ReceivedMessageFilterSet();
    readonly ReceivedMessageFilterSet _includes = new ReceivedMessageFilterSet();

    /// <summary>Gets the includes.</summary>
    public ReceivedMessageFilterSet Includes => _includes;

    /// <summary>Gets the excludes.</summary>
    public ReceivedMessageFilterSet Excludes => _excludes;

    /// <summary>Selects any matching value.</summary>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Any(IReceivedMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    /// <summary>Selects no values.</summary>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool None(IReceivedMessage element)
    {
        return _includes.None(element);
    }
}
