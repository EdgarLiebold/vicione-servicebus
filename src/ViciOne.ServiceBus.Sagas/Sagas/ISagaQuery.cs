using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// A saga query is used when a LINQ expression is accepted to query
/// the saga repository storage to get zero or more saga instances.
/// </summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaQuery<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// The query expression that returns true if the saga
    /// matches the query.
    /// </summary>
    Expression<Func<TSaga, bool>> FilterExpression { get; }

    /// <summary>
    /// Compiles a function that can be used to programatically
    /// compare a saga instance to the filter expression.
    /// </summary>
    /// <returns>The filter.</returns>
    Func<TSaga, bool> GetFilter();
}
