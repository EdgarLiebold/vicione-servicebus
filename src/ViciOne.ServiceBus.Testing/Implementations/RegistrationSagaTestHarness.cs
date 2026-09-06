using ViciOne.ServiceBus.DependencyInjection.Testing;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Provides a test harness for registration saga test.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class RegistrationSagaTestHarness<TSaga> :
    BaseSagaTestHarness<TSaga>,
    ISagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="registration">The registration.</param>
    /// <param name="repository">The repository.</param>
    /// <param name="loadRepository">The load repository.</param>
    /// <param name="queryRepository">The query repository.</param>
    public RegistrationSagaTestHarness(SagaContainerTestHarnessRegistration<TSaga> registration, ISagaRepository<TSaga> repository,
        ILoadSagaRepository<TSaga> loadRepository, IQuerySagaRepository<TSaga> queryRepository)
        : base(queryRepository, loadRepository, registration.TestTimeout, registration.TimeProvider)
    {
        Consumed = registration.Consumed;
        Created = registration.Created;
        Sagas = registration.Sagas;
    }

    /// <summary>Gets the consumed.</summary>
    public IReceivedMessageList Consumed { get; }

    /// <summary>Gets the sagas.</summary>
    public ISagaList<TSaga> Sagas { get; }

    /// <summary>Gets the created.</summary>
    public ISagaList<TSaga> Created { get; }
}
