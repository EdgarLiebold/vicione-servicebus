using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals.Dispatching;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Publishes typed messages through transport-specific send endpoints resolved by a provider.</summary>
public class PublishEndpoint :
    IPublishEndpoint,
    Advanced.IAdvancedPublishEndpoint
{
    IPublishEndpointProvider _publishEndpointProvider;

    /// <summary>Initializes a publish endpoint over a transport endpoint provider.</summary>
    /// <param name="provider">The provider that resolves publish send endpoints and owns publish observers.</param>
    public PublishEndpoint(IPublishEndpointProvider provider)
    {
        _publishEndpointProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Gets the provider used to resolve transport-specific publish endpoints.</summary>
    protected IPublishEndpointProvider PublishEndpointProvider => _publishEndpointProvider;

    /// <summary>Replaces the provider used by subsequent publish operations.</summary>
    /// <param name="provider">The provider that resolves publish send endpoints and owns publish observers.</param>
    protected void SetPublishEndpointProvider(IPublishEndpointProvider provider)
    {
        _publishEndpointProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return PublishInternalAsync(cancellationToken, message);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="publishPipe">The typed pipe that customizes the publish context.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="publishPipe">The untyped pipe that customizes the publish context.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message whose runtime type is the publish contract.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = message.GetType();

        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message whose runtime type is the publish contract.</param>
    /// <param name="publishPipe">The pipe that customizes the publish context.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishPipe);

        var messageType = message.GetType();

        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="publishPipe">The pipe that customizes the publish context.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(publishPipe);
        return PublishEndpointDispatcher.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The message contract to initialize and publish.</typeparam>
    /// <param name="values">The values used to initialize the message contract.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the initialized message.</returns>
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);

        return PublishInternalAsync<T>(cancellationToken, values);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The message contract to initialize and publish.</typeparam>
    /// <param name="values">The values used to initialize the message contract.</param>
    /// <param name="publishPipe">The typed pipe that customizes the publish context.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the initialized message.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(publishPipe);

        return PublishInternalAsync(cancellationToken, values, publishPipe);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The message contract to initialize and publish.</typeparam>
    /// <param name="values">The values used to initialize the message contract.</param>
    /// <param name="publishPipe">The untyped pipe that customizes the publish context.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the transport accepts the initialized message.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(publishPipe);

        return PublishInternalAsync<T>(cancellationToken, values, publishPipe);
    }

    /// <summary>Registers an observer for publish delivery events.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>An idempotent handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return PublishEndpointProvider.ConnectPublishObserver(observer);
    }

    /// <summary>Resolves the transport send endpoint for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task containing the resolved send endpoint.</returns>
    protected virtual Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return PublishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken);
    }

    Task PublishInternalAsync<T>(CancellationToken cancellationToken, T message, IPipe<PublishContext<T>>? pipe = null)
        where T : class
    {
        Task<ISendEndpoint> sendEndpointTask = GetPublishSendEndpointAsync<T>(cancellationToken);
        if (sendEndpointTask.Status == TaskStatus.RanToCompletion)
        {
            var sendEndpoint = sendEndpointTask.Result;

            return pipe != null && pipe.IsNotEmpty()
                ? sendEndpoint.SendAsync(message, new PublishSendPipeAdapter<T>(pipe), cancellationToken)
                : sendEndpoint.SendAsync(message, cancellationToken);
        }

        async Task PublishAsync()
        {
            var sendEndpoint = await sendEndpointTask.ConfigureAwait(false);

            if (pipe != null && pipe.IsNotEmpty())
                await sendEndpoint.SendAsync(message, new PublishSendPipeAdapter<T>(pipe), cancellationToken).ConfigureAwait(false);
            else
                await sendEndpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }

        return PublishAsync();
    }

    Task PublishInternalAsync<T>(CancellationToken cancellationToken, object values, IPipe<PublishContext<T>>? pipe = null)
        where T : class
    {
        Task<ISendEndpoint> sendEndpointTask = GetPublishSendEndpointAsync<T>(cancellationToken);
        if (sendEndpointTask.Status == TaskStatus.RanToCompletion)
        {
            var sendEndpoint = sendEndpointTask.Result;

            return pipe != null && pipe.IsNotEmpty()
                ? sendEndpoint.SendAsync(values, new PublishSendPipeAdapter<T>(pipe), cancellationToken)
                : sendEndpoint.SendAsync<T>(values, cancellationToken);
        }

        async Task PublishAsync()
        {
            var sendEndpoint = await sendEndpointTask.ConfigureAwait(false);

            if (pipe != null && pipe.IsNotEmpty())
                await sendEndpoint.SendAsync(values, new PublishSendPipeAdapter<T>(pipe), cancellationToken).ConfigureAwait(false);
            else
                await sendEndpoint.SendAsync<T>(values, cancellationToken).ConfigureAwait(false);
        }

        return PublishAsync();
    }
}
