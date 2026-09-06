using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides extension methods for saga test harness.</summary>
public static class SagaTestHarnessExtensions
{
    /// <summary>Applies the saga configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The saga test harness produced by the operation.</returns>
    public static SagaTestHarness<T> Saga<T>(this BusTestHarness harness, string? queueName = null)
        where T : class, ISaga
    {
        var repository = new InMemorySagaRepository<T>();

        return new SagaTestHarness<T>(harness, repository, repository, repository, queueName);
    }

    /// <summary>Applies the saga configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="repository">The repository.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The saga test harness produced by the operation.</returns>
    public static SagaTestHarness<T> Saga<T>(this BusTestHarness harness, ISagaRepository<T> repository, string? queueName = null)
        where T : class, ISaga
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        var querySagaRepository = repository as IQuerySagaRepository<T>;
        var loadSagaRepository = repository as ILoadSagaRepository<T>;

        return new SagaTestHarness<T>(harness, repository, querySagaRepository, loadSagaRepository, queueName);
    }
}
