namespace ViciOne.ServiceBus.Testing;

/// <summary>Processes sent message pipeline stages.</summary>
public class SentMessageFilter
{
    readonly SentMessageFilterSet _excludes = new SentMessageFilterSet();
    readonly SentMessageFilterSet _includes = new SentMessageFilterSet();

    /// <summary>Gets the includes.</summary>
    public SentMessageFilterSet Includes => _includes;

    /// <summary>Gets the excludes.</summary>
    public SentMessageFilterSet Excludes => _excludes;

    /// <summary>Selects any matching value.</summary>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Any(ISentMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    /// <summary>Selects no values.</summary>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool None(ISentMessage element)
    {
        return _includes.None(element);
    }
}
