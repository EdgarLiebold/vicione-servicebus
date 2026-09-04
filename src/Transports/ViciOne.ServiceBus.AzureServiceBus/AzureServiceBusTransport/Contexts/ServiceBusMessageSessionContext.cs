using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus message session context implementation.
/// </summary>
public class ServiceBusMessageSessionContext :
    MessageSessionContext
{
    readonly ProcessSessionMessageEventArgs _session;
    readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ServiceBusMessageSessionContext(ProcessSessionMessageEventArgs session, CancellationToken cancellationToken)
    {
        _session = session;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets state.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<BinaryData?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.BinaryData?>(cancellationToken); return _session.GetSessionStateAsync(_cancellationToken);
    }

    /// <summary>
    /// Sets state.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SetStateAsync(BinaryData? state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _session.SetSessionStateAsync(state, _cancellationToken);
    }

    /// <summary>
    /// Performs the renew lock operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task RenewLockAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the locked until utc value.
    /// </summary>
    public DateTimeOffset LockedUntilUtc => _session.Message.LockedUntil.UtcDateTime;

    /// <summary>
    /// Gets the session id value.
    /// </summary>
    public string SessionId => _session.SessionId;
}
