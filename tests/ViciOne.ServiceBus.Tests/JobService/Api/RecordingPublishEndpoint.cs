using ViciOne.ServiceBus.Advanced.Observers;

namespace ViciOne.ServiceBus.Tests.JobService.Api;

internal sealed class RecordingPublishEndpoint : IPublishEndpoint
{
    public object? Message { get; private set; }
    public Type? ContractType { get; private set; }
    public CancellationToken CancellationToken { get; private set; }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        Message = message;
        ContractType = typeof(T);
        CancellationToken = cancellationToken;
        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(options);
        return PublishAsync(message, cancellationToken);
    }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
        throw new NotSupportedException("These API tests do not connect transport observers.");
}
