using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides extension methods for saga test harness.
/// </summary>
public static class SagaTestHarnessExtensions
{
    /// <summary>
    /// Performs the saga operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public static SagaTestHarness<T> Saga<T>(this BusTestHarness harness, string? queueName = null)
        where T : class, ISaga
    {
        var repository = new InMemorySagaRepository<T>();

        return new SagaTestHarness<T>(harness, repository, repository, repository, queueName);
    }

    /// <summary>
    /// Performs the saga operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="harness">The harness value.</param>
    /// <param name="repository">The repository value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
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
