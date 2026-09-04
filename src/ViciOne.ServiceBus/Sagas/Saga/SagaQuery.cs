using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Provides a saga query implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class SagaQuery<TSaga> :
    ISagaQuery<TSaga>
    where TSaga : class, ISaga
{
    readonly Lazy<Func<TSaga, bool>> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filterExpression">The filter expression value.</param>
    public SagaQuery(Expression<Func<TSaga, bool>> filterExpression)
    {
        FilterExpression = filterExpression;
        _filter = new Lazy<Func<TSaga, bool>>(filterExpression.Compile);
    }

    /// <summary>
    /// Gets filter.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Func<TSaga, bool> GetFilter()
    {
        return _filter.Value;
    }

    /// <summary>
    /// Gets the filter expression value.
    /// </summary>
    public Expression<Func<TSaga, bool>> FilterExpression { get; }
}
