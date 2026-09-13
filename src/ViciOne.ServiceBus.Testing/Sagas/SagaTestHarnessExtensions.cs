using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Creates saga repository observers for a bus test harness.</summary>
public static class SagaTestHarnessExtensions
{
    /// <summary>Registers an in-memory saga repository and records its activity.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="harness">The harness that hosts the saga endpoint.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>The saga harness.</returns>
    public static SagaTestHarness<TSaga> AddSaga<TSaga>(this BusTestHarness harness, string? queueName = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(harness);
        var repository = new InMemorySagaRepository<TSaga>();

        return new SagaTestHarness<TSaga>(harness, repository, repository, repository, queueName);
    }

    /// <summary>Registers a saga repository and records the capabilities it exposes.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="harness">The harness that hosts the saga endpoint.</param>
    /// <param name="repository">The saga repository to decorate.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>The saga harness.</returns>
    public static SagaTestHarness<TSaga> AddSaga<TSaga>(this BusTestHarness harness, ISagaRepository<TSaga> repository,
        string? queueName = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(repository);

        var querySagaRepository = repository as IQuerySagaRepository<TSaga>;
        var loadSagaRepository = repository as ILoadSagaRepository<TSaga>;

        return new SagaTestHarness<TSaga>(harness, repository, querySagaRepository, loadSagaRepository, queueName);
    }
}
