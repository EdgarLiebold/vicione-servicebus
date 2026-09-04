using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.DependencyInjection.Testing;

/// <summary>
/// Provides a saga container test harness registration implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class SagaContainerTestHarnessRegistration<TSaga> :
    ISagaRepositoryDecoratorRegistration<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="testHarness">The test harness value.</param>
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

    /// <summary>
    /// Gets the test timeout value.
    /// </summary>
    public TimeSpan TestTimeout { get; }
    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    public ReceivedMessageList Consumed { get; }
    /// <summary>
    /// Gets the created value.
    /// </summary>
    public SagaList<TSaga> Created { get; }
    /// <summary>
    /// Gets the sagas value.
    /// </summary>
    public SagaList<TSaga> Sagas { get; }

    /// <summary>
    /// Performs the decorate saga repository operation.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <returns>The result of the operation.</returns>
    public ISagaRepository<TSaga> DecorateSagaRepository(ISagaRepository<TSaga> repository)
    {
        return new TestSagaRepositoryDecorator<TSaga>(repository, Consumed, Created, Sagas);
    }
}
