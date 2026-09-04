using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for base test harness.
/// </summary>
public interface IBaseTestHarness :
    IConsumeObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector
{
    /// <summary>
    /// Gets or sets the test timeout value.
    /// </summary>
    TimeSpan TestTimeout { get; set; }
    /// <summary>
    /// Gets or sets the test inactivity timeout value.
    /// </summary>
    TimeSpan TestInactivityTimeout { get; set; }
    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    TimeProvider TimeProvider { get; }
    /// <summary>
    /// Gets the context save mode value.
    /// </summary>
    TestContextSaveMode ContextSaveMode { get; }
    /// <summary>
    /// Gets the maximum saved contexts value.
    /// </summary>
    int MaximumSavedContexts { get; }

    /// <summary>
    /// CancellationToken that is canceled when the test is being aborted
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// CancellationToken that is cancelled when the test inactivity timeout has elapsed with no bus activity
    /// </summary>
    CancellationToken InactivityToken { get; }

    /// <summary>
    /// Task that is completed when the bus inactivity timeout has elapsed with no bus activity
    /// </summary>
    public Task InactivityTask { get; }

    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    IReceivedMessageList Consumed { get; }
    /// <summary>
    /// Gets the published value.
    /// </summary>
    IPublishedMessageList Published { get; }
    /// <summary>
    /// Gets the sent value.
    /// </summary>
    ISentMessageList Sent { get; }

    /// <summary>
    /// Sets the <see cref="CancellationToken" />, canceling the test execution
    /// </summary>
    void Cancel();

    /// <summary>
    /// Force the inactivity task to complete
    /// </summary>
    void ForceInactive();
}
