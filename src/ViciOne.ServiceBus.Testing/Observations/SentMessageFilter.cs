namespace ViciOne.ServiceBus.Testing;

/// <summary>Combines include and exclude predicates for send observations.</summary>
public sealed class SentMessageFilter
{
    readonly SentMessageFilterSet _excludes = new SentMessageFilterSet();
    readonly SentMessageFilterSet _includes = new SentMessageFilterSet();

    /// <summary>Gets the predicates of which at least one must match.</summary>
    public SentMessageFilterSet Includes => _includes;

    /// <summary>Gets the predicates of which none may match.</summary>
    public SentMessageFilterSet Excludes => _excludes;

    /// <summary>Determines whether a send satisfies the include and exclude sets.</summary>
    /// <param name="element">The send to evaluate.</param>
    /// <returns><see langword="true"/> when the send is included and not excluded; otherwise, <see langword="false"/>.</returns>
    public bool Any(ISentMessage element)
    {
        return _includes.Any(element) && _excludes.None(element);
    }

    /// <summary>Determines whether a send matches none of the include predicates.</summary>
    /// <param name="element">The send to evaluate.</param>
    /// <returns><see langword="true"/> when no include predicate is configured or none matches; otherwise, <see langword="false"/>.</returns>
    public bool None(ISentMessage element)
    {
        return _includes.None(element);
    }
}
