using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Supplies a predicate expression and delegate for matching actual saga state.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
public interface ISagaQuery<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Gets the required expression that evaluates whether the supplied state matches.</summary>
    Expression<Func<TSaga, bool>> FilterExpression { get; }

    /// <summary>Gets a non-null predicate delegate evaluated against each actual referenced state.</summary>
    /// <returns>The matching predicate; implementations may cache its compiled delegate.</returns>
    Func<TSaga, bool> GetFilter();
}
