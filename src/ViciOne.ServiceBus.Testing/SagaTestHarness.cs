using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a saga test harness implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class SagaTestHarness<TSaga> :
    BaseSagaTestHarness<TSaga>,
    ISagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    readonly ReceivedMessageList _consumed;
    readonly SagaList<TSaga> _created;
    readonly SagaList<TSaga> _sagas;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="testHarness">The test harness value.</param>
    /// <param name="repository">The repository value.</param>
    /// <param name="querySagaRepository">The query saga repository value.</param>
    /// <param name="loadSagaRepository">The load saga repository value.</param>
    /// <param name="queueName">The queue name value.</param>
    public SagaTestHarness(BusTestHarness testHarness, ISagaRepository<TSaga> repository, IQuerySagaRepository<TSaga>? querySagaRepository,
        ILoadSagaRepository<TSaga>? loadSagaRepository, string? queueName)
        : base(querySagaRepository, loadSagaRepository, testHarness.TestTimeout, testHarness.TimeProvider)
    {
        _consumed = new ReceivedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        _created = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        _sagas = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, testHarness.TimeProvider);
        ((ITestContextRetention)_consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
        ((ITestContextRetention)_created).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
        ((ITestContextRetention)_sagas).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);

        TestRepository = new TestSagaRepositoryDecorator<TSaga>(repository, _consumed, _created, _sagas);

        if (string.IsNullOrWhiteSpace(queueName))
            testHarness.OnConfigureReceiveEndpoint += ConfigureReceiveEndpoint;
        else
            testHarness.OnConfigureBus += configurator => ConfigureNamedReceiveEndpoint(configurator, queueName);
    }

    /// <summary>
    /// Gets the test repository value.
    /// </summary>
    protected TestSagaRepositoryDecorator<TSaga> TestRepository { get; }

    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    public IReceivedMessageList Consumed => _consumed;
    /// <summary>
    /// Gets the sagas value.
    /// </summary>
    public ISagaList<TSaga> Sagas => _sagas;
    /// <summary>
    /// Gets the created value.
    /// </summary>
    public ISagaList<TSaga> Created => _created;

    /// <summary>
    /// Configures receive endpoint.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    protected virtual void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        configurator.Saga(TestRepository);
    }

    /// <summary>
    /// Configures named receive endpoint.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="queueName">The queue name value.</param>
    protected virtual void ConfigureNamedReceiveEndpoint(IBusFactoryConfigurator configurator, string queueName)
    {
        configurator.ReceiveEndpoint(queueName, x =>
        {
            x.Saga(TestRepository);
        });
    }
}
