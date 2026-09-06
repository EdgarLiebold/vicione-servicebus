namespace ViciOne.ServiceBus.Configuration;

/// <summary>Processes composite pipeline stages.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class CompositeFilter<T>
{
    readonly CompositePredicate<T> _excludes = new CompositePredicate<T>();
    readonly CompositePredicate<T> _includes = new CompositePredicate<T>();

    /// <summary>Gets the includes.</summary>
    public CompositePredicate<T> Includes => _includes;

    /// <summary>Gets the excludes.</summary>
    public CompositePredicate<T> Excludes => _excludes;

    /// <summary>Determines whether the supplied value matches.</summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Matches(T target)
    {
        return Includes.MatchesAny(target) && Excludes.DoesNotMatchAny(target);
    }
}
