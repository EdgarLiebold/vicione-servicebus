using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Registers a saga repository under test and records consumed, created, and existing saga instances.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public class SagaTestHarness<TSaga> :
    BaseSagaTestHarness<TSaga>,
    ISagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    readonly ConsumedMessageList _consumed;
    readonly SagaList<TSaga> _created;
    readonly SagaList<TSaga> _sagas;

    /// <summary>Registers an observed saga repository with either the default endpoint or a named endpoint.</summary>
    /// <param name="testHarness">The bus harness that hosts the saga endpoint.</param>
    /// <param name="repository">The saga repository to decorate.</param>
    /// <param name="querySagaRepository">An optional repository used to query saga identifiers.</param>
    /// <param name="loadSagaRepository">An optional repository used to load saga instances.</param>
    /// <param name="queueName">The named endpoint queue, or <see langword="null"/> for the default endpoint.</param>
    public SagaTestHarness(BusTestHarness testHarness, ISagaRepository<TSaga> repository, IQuerySagaRepository<TSaga>? querySagaRepository,
        ILoadSagaRepository<TSaga>? loadSagaRepository, string? queueName)
        : base(querySagaRepository, loadSagaRepository,
            (testHarness ?? throw new ArgumentNullException(nameof(testHarness))).TestTimeout,
            testHarness.TimeProvider)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _consumed = new ConsumedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        _created = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        _sagas = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)_consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
        ((ITestContextRetention)_created).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
        ((ITestContextRetention)_sagas).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);

        TestRepository = new TestSagaRepositoryDecorator<TSaga>(repository, _consumed, _created, _sagas);

        if (queueName == null)
            testHarness.ReceiveEndpointConfiguring += ConfigureReceiveEndpoint;
        else
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
            testHarness.BusConfiguring += configurator => ConfigureNamedReceiveEndpoint(configurator, queueName);
        }
    }

    /// <summary>Gets the repository that records saga activity before delegating to persistence.</summary>
    protected ISagaRepository<TSaga> TestRepository { get; }

    /// <summary>Gets messages delivered through the saga repository.</summary>
    public IConsumedMessageList Consumed => _consumed;
    /// <summary>Gets saga instances observed by the repository.</summary>
    public ISagaList<TSaga> Sagas => _sagas;
    /// <summary>Gets saga instances created by the repository.</summary>
    public ISagaList<TSaga> Created => _created;

    /// <summary>Attaches the observed saga repository to the default receive endpoint.</summary>
    /// <param name="configurator">The receive-endpoint configurator.</param>
    protected virtual void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.Saga(TestRepository);
    }

    /// <summary>Adds a named receive endpoint containing the observed saga repository.</summary>
    /// <param name="configurator">The bus configurator.</param>
    /// <param name="queueName">The endpoint queue name.</param>
    protected virtual void ConfigureNamedReceiveEndpoint(IBusFactoryConfigurator configurator, string queueName)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        configurator.ReceiveEndpoint(queueName, x =>
        {
            x.Saga(TestRepository);
        });
    }
}
