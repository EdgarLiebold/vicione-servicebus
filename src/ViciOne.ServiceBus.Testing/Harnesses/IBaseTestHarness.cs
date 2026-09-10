using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines shared timing, cancellation, observation, and activity-recording services for bus test harnesses.</summary>
public interface IBaseTestHarness :
    IConsumeObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector
{
    /// <summary>Gets or sets the maximum duration of a test scope.</summary>
    TimeSpan TestTimeout { get; set; }
    /// <summary>Gets or sets the period of bus inactivity after which inactivity observers complete.</summary>
    TimeSpan TestInactivityTimeout { get; set; }
    /// <summary>Gets the time provider used by harness timers.</summary>
    TimeProvider TimeProvider { get; }
    /// <summary>Gets the policy that controls which observed message contexts are retained.</summary>
    TestContextSaveMode ContextSaveMode { get; }
    /// <summary>Gets the maximum retained context count used by bounded retention.</summary>
    int MaximumSavedContexts { get; }

    /// <summary>Gets the token canceled when the current test scope expires or is explicitly canceled.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Gets the token canceled after the configured interval contains no observed bus activity.</summary>
    CancellationToken InactivityToken { get; }

    /// <summary>Gets the task that completes after the configured interval contains no observed bus activity.</summary>
    Task InactivityTask { get; }

    /// <summary>Gets messages consumed since the harness was started.</summary>
    IConsumedMessageList Consumed { get; }
    /// <summary>Gets messages published since the harness was started.</summary>
    IPublishedMessageList Published { get; }
    /// <summary>Gets messages sent since the harness was started.</summary>
    ISentMessageList Sent { get; }

    /// <summary>Cancels the current test scope and tasks bound to it.</summary>
    void Cancel();

    /// <summary>Completes the inactivity observer without waiting for its timeout.</summary>
    void ForceInactive();
}
