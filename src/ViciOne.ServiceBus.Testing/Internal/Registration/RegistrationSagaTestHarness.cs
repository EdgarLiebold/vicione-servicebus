namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Exposes observations and repository polling for a dependency-injection registered saga.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class RegistrationSagaTestHarness<TSaga> :
    BaseSagaTestHarness<TSaga>,
    ISagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates a harness over recorded saga activity and registered repository capabilities.</summary>
    /// <param name="registration">The recorded saga activity.</param>
    /// <param name="loadRepository">The optional repository load capability.</param>
    /// <param name="queryRepository">The optional repository query capability.</param>
    public RegistrationSagaTestHarness(SagaContainerTestHarnessRegistration<TSaga> registration,
        ILoadSagaRepository<TSaga>? loadRepository = null, IQuerySagaRepository<TSaga>? queryRepository = null)
        : base(
            queryRepository,
            loadRepository,
            (registration ?? throw new ArgumentNullException(nameof(registration))).TestTimeout,
            registration.TimeProvider)
    {
        Consumed = registration.Consumed;
        Created = registration.Created;
        Sagas = registration.Sagas;
    }

    /// <inheritdoc />
    public IConsumedMessageList Consumed { get; }

    /// <inheritdoc />
    public ISagaList<TSaga> Sagas { get; }

    /// <inheritdoc />
    public ISagaList<TSaga> Created { get; }
}
