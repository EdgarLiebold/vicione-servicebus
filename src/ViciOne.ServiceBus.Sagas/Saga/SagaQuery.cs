using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Retains a required saga predicate expression and lazily caches its compiled delegate.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
public class SagaQuery<TSaga> :
    ISagaQuery<TSaga>
    where TSaga : class, ISaga
{
    readonly Lazy<Func<TSaga, bool>> _filter;

    /// <summary>Retains the required expression without evaluating or copying saga state.</summary>
    /// <param name="filterExpression">The required predicate evaluated against each actual state.</param>
    public SagaQuery(Expression<Func<TSaga, bool>> filterExpression)
    {
        FilterExpression = filterExpression ?? throw new ArgumentNullException(nameof(filterExpression));
        _filter = new Lazy<Func<TSaga, bool>>(filterExpression.Compile);
    }

    /// <summary>Gets the lazily compiled predicate, reusing the same delegate on subsequent calls.</summary>
    /// <returns>The predicate that evaluates the supplied referenced state.</returns>
    public Func<TSaga, bool> GetFilter()
    {
        return _filter.Value;
    }

    /// <summary>Gets the exact expression supplied at construction.</summary>
    public Expression<Func<TSaga, bool>> FilterExpression { get; }
}
