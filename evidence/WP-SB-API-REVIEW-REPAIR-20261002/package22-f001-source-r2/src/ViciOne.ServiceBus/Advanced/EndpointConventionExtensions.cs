using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Sends messages through the convention route owned by the active bus.</summary>
public static class EndpointConventionExtensions
{
    /// <summary>Sends a typed message to its configured bus-owned route.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution or sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static async Task SendAsync<T>(this ISendEndpointProvider provider, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(message);

        Uri destinationAddress = EndpointConvention.GetDestinationAddress<T>(provider);

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a typed message through a context pipeline to its configured bus-owned route.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static async Task SendAsync<T>(this ISendEndpointProvider provider, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        Uri destinationAddress = EndpointConvention.GetDestinationAddress<T>(provider);

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a typed message through an untyped context pipeline to its configured bus-owned route.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task SendAsync<T>(this ISendEndpointProvider provider, T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return SendAsync(provider, message, (IPipe<SendContext<T>>)pipe, cancellationToken);
    }

    /// <summary>Sends a runtime-typed message to its configured bus-owned route.</summary>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static async Task SendAsync(this ISendEndpointProvider provider, object message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(message);

        var messageType = message.GetType();

        Uri destinationAddress = EndpointConvention.GetDestinationAddress(provider, messageType);

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a message under an explicit contract type to its configured bus-owned route.</summary>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static async Task SendAsync(this ISendEndpointProvider provider, object message, Type messageType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        Uri destinationAddress = EndpointConvention.GetDestinationAddress(provider, messageType);

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a runtime-typed message through a context pipeline to its configured bus-owned route.</summary>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static async Task SendAsync(this ISendEndpointProvider provider, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        var messageType = message.GetType();

        Uri destinationAddress = EndpointConvention.GetDestinationAddress(provider, messageType);

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a message under an explicit contract type through a context pipeline.</summary>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static async Task SendAsync(this ISendEndpointProvider provider, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        Uri destinationAddress = EndpointConvention.GetDestinationAddress(provider, messageType);

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, messageType, pipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initializes and sends a message to its configured bus-owned route.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task SendAsync<T>(this ISendEndpointProvider provider, object values, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(values);

        return SendAsync(provider, values, Pipe.Empty<SendContext<T>>(), cancellationToken);
    }

    /// <summary>Initializes and sends a message through a typed context pipeline.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static async Task SendAsync<T>(this ISendEndpointProvider provider, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        Uri destinationAddress = EndpointConvention.GetDestinationAddress<T>(provider);

        var endpoint = await provider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        (var message, IPipe<SendContext<T>> sendPipe) = provider is ConsumeContext consumeContext
            ? await MessageInitializerCache<T>.InitializeMessageAsync(consumeContext, values, pipe, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initializes and sends a message through an untyped context pipeline.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="provider">The endpoint provider whose bus owns the route.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public static Task SendAsync<T>(this ISendEndpointProvider provider, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        return SendAsync(provider, values, (IPipe<SendContext<T>>)pipe, cancellationToken);
    }
}
