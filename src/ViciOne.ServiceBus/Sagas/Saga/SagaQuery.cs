using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Queries saga values.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaQuery<TSaga> :
    ISagaQuery<TSaga>
    where TSaga : class, ISaga
{
    readonly Lazy<Func<TSaga, bool>> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filterExpression">The filter expression.</param>
    public SagaQuery(Expression<Func<TSaga, bool>> filterExpression)
    {
        FilterExpression = filterExpression;
        _filter = new Lazy<Func<TSaga, bool>>(filterExpression.Compile);
    }

    /// <summary>Gets filter.</summary>
    /// <returns>The filter.</returns>
    public Func<TSaga, bool> GetFilter()
    {
        return _filter.Value;
    }

    /// <summary>Gets the filter expression.</summary>
    public Expression<Func<TSaga, bool>> FilterExpression { get; }
}
