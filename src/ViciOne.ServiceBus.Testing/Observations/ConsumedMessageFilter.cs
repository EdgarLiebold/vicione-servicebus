namespace ViciOne.ServiceBus.Testing;

/// <summary>Combines include and exclude predicates for consumption observations.</summary>
public sealed class ConsumedMessageFilter
{
    readonly ConsumedMessageFilterSet _excludes = new ConsumedMessageFilterSet();
    readonly ConsumedMessageFilterSet _includes = new ConsumedMessageFilterSet();

    /// <summary>Gets the predicates of which at least one must match.</summary>
    public ConsumedMessageFilterSet Includes => _includes;

    /// <summary>Gets the predicates of which none may match.</summary>
    public ConsumedMessageFilterSet Excludes => _excludes;

    /// <summary>Determines whether a consumption satisfies the include and exclude sets.</summary>
    /// <param name="element">The consumption to evaluate.</param>
    /// <returns><see langword="true"/> when the consumption is included and not excluded; otherwise, <see langword="false"/>.</returns>
    public bool Any(IConsumedMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    /// <summary>Determines whether a consumption matches none of the include predicates.</summary>
    /// <param name="element">The consumption to evaluate.</param>
    /// <returns><see langword="true"/> when no include predicate is configured or none matches; otherwise, <see langword="false"/>.</returns>
    public bool None(IConsumedMessage element)
    {
        return _includes.None(element);
    }
}
