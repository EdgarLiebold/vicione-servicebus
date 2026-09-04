namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a published message filter implementation.
/// </summary>
public class PublishedMessageFilter
{
    readonly PublishedMessageFilterSet _excludes = new PublishedMessageFilterSet();
    readonly PublishedMessageFilterSet _includes = new PublishedMessageFilterSet();

    /// <summary>
    /// Gets the includes value.
    /// </summary>
    public PublishedMessageFilterSet Includes => _includes;

    /// <summary>
    /// Gets the excludes value.
    /// </summary>
    public PublishedMessageFilterSet Excludes => _excludes;

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="element">The element value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Any(IPublishedMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    /// <summary>
    /// Performs the none operation.
    /// </summary>
    /// <param name="element">The element value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool None(IPublishedMessage element)
    {
        return _includes.None(element);
    }
}
