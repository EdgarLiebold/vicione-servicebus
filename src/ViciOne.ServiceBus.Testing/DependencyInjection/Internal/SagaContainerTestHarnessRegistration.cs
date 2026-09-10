using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Owns the observations and repository decorator registered for one saga state type.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaContainerTestHarnessRegistration<TSaga> :
    ISagaRepositoryDecoratorRegistration<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates a registration using the owning harness's observation policy.</summary>
    /// <param name="testHarness">The harness that supplies timeouts, cancellation, and retention.</param>
    public SagaContainerTestHarnessRegistration(ITestHarness testHarness)
    {
        ArgumentNullException.ThrowIfNull(testHarness);

        TestTimeout = testHarness.TestTimeout;
        TimeProvider = testHarness.TimeProvider;

        Consumed = new ConsumedMessageList(testHarness.TestTimeout, testHarness.InactivityToken, TimeProvider);
        Created = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, TimeProvider);
        Sagas = new SagaList<TSaga>(testHarness.TestTimeout, testHarness.InactivityToken, TimeProvider);

        ((ITestContextRetention)Consumed).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
        ((ITestContextRetention)Created).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
        ((ITestContextRetention)Sagas).ConfigureRetention(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
    }

    /// <summary>Gets the maximum time a saga assertion waits for a match.</summary>
    public TimeSpan TestTimeout { get; }
    /// <summary>Gets the clock used for saga assertion timeouts.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Gets the messages consumed by this saga type.</summary>
    public ConsumedMessageList Consumed { get; }
    /// <summary>Gets saga instances first created by the decorated repository.</summary>
    public SagaList<TSaga> Created { get; }
    /// <summary>Gets saga instances loaded or created by the decorated repository.</summary>
    public SagaList<TSaga> Sagas { get; }

    /// <summary>Wraps a saga repository so its consumptions and instances are recorded.</summary>
    /// <param name="repository">The repository to decorate.</param>
    /// <returns>The recording repository decorator.</returns>
    public ISagaRepository<TSaga> DecorateSagaRepository(ISagaRepository<TSaga> repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        return new TestSagaRepositoryDecorator<TSaga>(repository, Consumed, Created, Sagas);
    }
}
