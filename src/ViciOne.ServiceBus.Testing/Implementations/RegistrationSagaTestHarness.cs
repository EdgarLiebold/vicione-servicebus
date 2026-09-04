using ViciOne.ServiceBus.DependencyInjection.Testing;

namespace ViciOne.ServiceBus.Testing.Implementations;

public class RegistrationSagaTestHarness<TSaga> :
    BaseSagaTestHarness<TSaga>,
    ISagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    public RegistrationSagaTestHarness(SagaContainerTestHarnessRegistration<TSaga> registration, ISagaRepository<TSaga> repository,
        ILoadSagaRepository<TSaga> loadRepository, IQuerySagaRepository<TSaga> queryRepository)
        : base(queryRepository, loadRepository, registration.TestTimeout, registration.TimeProvider)
    {
        Consumed = registration.Consumed;
        Created = registration.Created;
        Sagas = registration.Sagas;
    }

    public IReceivedMessageList Consumed { get; }

    public ISagaList<TSaga> Sagas { get; }

    public ISagaList<TSaga> Created { get; }
}
