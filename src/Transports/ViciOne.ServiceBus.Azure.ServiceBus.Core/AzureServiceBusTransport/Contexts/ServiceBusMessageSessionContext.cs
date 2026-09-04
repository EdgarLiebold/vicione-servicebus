using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ServiceBusMessageSessionContext :
    MessageSessionContext
{
    readonly ProcessSessionMessageEventArgs _session;
    readonly CancellationToken _cancellationToken;

    public ServiceBusMessageSessionContext(ProcessSessionMessageEventArgs session, CancellationToken cancellationToken)
    {
        _session = session;
        _cancellationToken = cancellationToken;
    }

    public Task<BinaryData?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.BinaryData?>(cancellationToken); return _session.GetSessionStateAsync(_cancellationToken);
    }

    public Task SetStateAsync(BinaryData? state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _session.SetSessionStateAsync(state, _cancellationToken);
    }

    public Task RenewLockAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    public DateTimeOffset LockedUntilUtc => _session.Message.LockedUntil.UtcDateTime;

    public string SessionId => _session.SessionId;
}
