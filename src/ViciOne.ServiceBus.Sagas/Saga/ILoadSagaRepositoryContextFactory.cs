using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates load saga repository context instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ILoadSagaRepositoryContextFactory<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    /// <summary>Create a <see cref="LoadSagaRepositoryContext{TSaga}" /> and send it to the next pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="asyncMethod">The async method.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the execute outcome.</returns>
    Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class;
}
