namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a composite filter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class CompositeFilter<T>
{
    readonly CompositePredicate<T> _excludes = new CompositePredicate<T>();
    readonly CompositePredicate<T> _includes = new CompositePredicate<T>();

    /// <summary>
    /// Gets the predicates that select values for evaluation.
    /// </summary>
    public CompositePredicate<T> Includes => _includes;

    /// <summary>
    /// Gets the predicates that reject selected values.
    /// </summary>
    public CompositePredicate<T> Excludes => _excludes;

    /// <summary>
    /// Performs the matches operation.
    /// </summary>
    /// <param name="target">The target value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Matches(T target)
    {
        return Includes.MatchesAny(target) && Excludes.DoesNotMatchAny(target);
    }
}
