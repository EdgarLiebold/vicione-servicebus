using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.DependencyInjection.Testing;

/// <summary>Registers saga container test harness services.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaContainerTestHarnessRegistration<TSaga> :
    ISagaRepositoryDecoratorRegistration<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="testHarness">The test harness.</param>
    public SagaContainerTestHarnessRegistration(ITestHarness testHarness)
    {
        TestTimeout = testHarness.TestTimeout;
        TimeProvider = testHarness.TimeProvider;

        Consumed = new ReceivedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, TimeProvider);
        Created = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, TimeProvider);
        Sagas = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, TimeProvider);

        ((ITestContextRetention)Consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
        ((ITestContextRetention)Created).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
        ((ITestContextRetention)Sagas).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
    }

    /// <summary>Gets the test timeout.</summary>
    public TimeSpan TestTimeout { get; }
    /// <summary>Gets the time provider.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Gets the consumed.</summary>
    public ReceivedMessageList Consumed { get; }
    /// <summary>Gets the created.</summary>
    public SagaList<TSaga> Created { get; }
    /// <summary>Gets the sagas.</summary>
    public SagaList<TSaga> Sagas { get; }

    /// <summary>Decorates saga repository.</summary>
    /// <param name="repository">The repository.</param>
    /// <returns>The saga repository produced by the operation.</returns>
    public ISagaRepository<TSaga> DecorateSagaRepository(ISagaRepository<TSaga> repository)
    {
        return new TestSagaRepositoryDecorator<TSaga>(repository, Consumed, Created, Sagas);
    }
}
