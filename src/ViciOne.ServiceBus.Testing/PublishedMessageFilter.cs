namespace ViciOne.ServiceBus.Testing;

/// <summary>Processes published message pipeline stages.</summary>
public class PublishedMessageFilter
{
    readonly PublishedMessageFilterSet _excludes = new PublishedMessageFilterSet();
    readonly PublishedMessageFilterSet _includes = new PublishedMessageFilterSet();

    /// <summary>Gets the includes.</summary>
    public PublishedMessageFilterSet Includes => _includes;

    /// <summary>Gets the excludes.</summary>
    public PublishedMessageFilterSet Excludes => _excludes;

    /// <summary>Selects any matching value.</summary>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Any(IPublishedMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    /// <summary>Selects no values.</summary>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool None(IPublishedMessage element)
    {
        return _includes.None(element);
    }
}
