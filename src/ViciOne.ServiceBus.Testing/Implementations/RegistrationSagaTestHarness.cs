using ViciOne.ServiceBus.DependencyInjection.Testing;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a registration saga test harness implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class RegistrationSagaTestHarness<TSaga> :
    BaseSagaTestHarness<TSaga>,
    ISagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    /// <param name="repository">The repository value.</param>
    /// <param name="loadRepository">The load repository value.</param>
    /// <param name="queryRepository">The query repository value.</param>
    public RegistrationSagaTestHarness(SagaContainerTestHarnessRegistration<TSaga> registration, ISagaRepository<TSaga> repository,
        ILoadSagaRepository<TSaga> loadRepository, IQuerySagaRepository<TSaga> queryRepository)
        : base(queryRepository, loadRepository, registration.TestTimeout, registration.TimeProvider)
    {
        Consumed = registration.Consumed;
        Created = registration.Created;
        Sagas = registration.Sagas;
    }

    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    public IReceivedMessageList Consumed { get; }

    /// <summary>
    /// Gets the sagas value.
    /// </summary>
    public ISagaList<TSaga> Sagas { get; }

    /// <summary>
    /// Gets the created value.
    /// </summary>
    public ISagaList<TSaga> Created { get; }
}
