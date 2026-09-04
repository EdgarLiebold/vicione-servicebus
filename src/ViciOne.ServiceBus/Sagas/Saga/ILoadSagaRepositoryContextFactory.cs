using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public interface ILoadSagaRepositoryContextFactory<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    /// <summary>
    /// Create a <see cref="LoadSagaRepositoryContext{TSaga}" /> and send it to the next pipe.
    /// </summary>
    /// <param name="asyncMethod"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class;
}
