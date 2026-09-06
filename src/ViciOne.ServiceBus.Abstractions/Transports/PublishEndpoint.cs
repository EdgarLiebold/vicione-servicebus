using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides an endpoint for publish.</summary>
public class PublishEndpoint :
    IPublishEndpoint,
    Advanced.IAdvancedPublishEndpoint
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public PublishEndpoint(IPublishEndpointProvider provider)
    {
        PublishEndpointProvider = provider;
    }

    /// <summary>Gets or sets the publish endpoint provider.</summary>
    protected IPublishEndpointProvider PublishEndpointProvider { get; set; }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return PublishInternalAsync(cancellationToken, message, publishPipe);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        return PublishEndpointConverterCache.PublishAsync(this, message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync<T>(cancellationToken, values);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync(cancellationToken, values, publishPipe);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return PublishInternalAsync<T>(cancellationToken, values, publishPipe);
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return PublishEndpointProvider.ConnectPublishObserver(observer);
    }

    /// <summary>Gets publish send endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>A task that produces the requested value.</returns>
    protected virtual Task<ISendEndpoint> GetPublishSendEndpointAsync<T>()
        where T : class
    {
        return PublishEndpointProvider.GetPublishSendEndpointAsync<T>();
    }

    Task PublishInternalAsync<T>(CancellationToken cancellationToken, T message, IPipe<PublishContext<T>>? pipe = null)
        where T : class
    {
        Task<ISendEndpoint> sendEndpointTask = GetPublishSendEndpointAsync<T>();
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
        Task<ISendEndpoint> sendEndpointTask = GetPublishSendEndpointAsync<T>();
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
