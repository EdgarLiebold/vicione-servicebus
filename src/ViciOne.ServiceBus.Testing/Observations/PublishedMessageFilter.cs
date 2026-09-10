namespace ViciOne.ServiceBus.Testing;

/// <summary>Combines include and exclude predicates for publication observations.</summary>
public sealed class PublishedMessageFilter
{
    readonly PublishedMessageFilterSet _excludes = new PublishedMessageFilterSet();
    readonly PublishedMessageFilterSet _includes = new PublishedMessageFilterSet();

    /// <summary>Gets the predicates of which at least one must match.</summary>
    public PublishedMessageFilterSet Includes => _includes;

    /// <summary>Gets the predicates of which none may match.</summary>
    public PublishedMessageFilterSet Excludes => _excludes;

    /// <summary>Determines whether a publication satisfies the include and exclude sets.</summary>
    /// <param name="element">The publication to evaluate.</param>
    /// <returns><see langword="true"/> when the publication is included and not excluded; otherwise, <see langword="false"/>.</returns>
    public bool Any(IPublishedMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    /// <summary>Determines whether a publication matches none of the include predicates.</summary>
    /// <param name="element">The publication to evaluate.</param>
    /// <returns><see langword="true"/> when no include predicate is configured or none matches; otherwise, <see langword="false"/>.</returns>
    public bool None(IPublishedMessage element)
    {
        return _includes.None(element);
    }
}
