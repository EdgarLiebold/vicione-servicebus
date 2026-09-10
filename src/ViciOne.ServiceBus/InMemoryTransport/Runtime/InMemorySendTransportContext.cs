using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Serializes messages and delivers transport envelopes to one in-memory exchange.</summary>
internal sealed class InMemorySendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<PipeContext>
{
    readonly IInMemoryDelayProvider _delayProvider;
    readonly IMessageExchange<InMemoryTransportMessage> _exchange;

    /// <summary>Creates a send-transport context for one exchange.</summary>
    /// <param name="hostConfiguration">The host configuration that owns the transport.</param>
    /// <param name="context">The receive endpoint context that supplies serialization.</param>
    /// <param name="exchange">The destination exchange.</param>
    /// <param name="delayProvider">The transport clock used for delayed delivery.</param>
    public InMemorySendTransportContext(IHostConfiguration hostConfiguration, ReceiveEndpointContext context,
        IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
        : base(
            hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration)),
            (context ?? throw new ArgumentNullException(nameof(context))).Serialization)
    {
        _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
    }

    /// <summary>Gets the destination exchange name.</summary>
    public override string EntityName => _exchange.Name;
    /// <summary>Gets the OpenTelemetry messaging-system identifier.</summary>
    public override string ActivitySystem => "in-memory";

    /// <summary>Creates and configures an in-memory send context for a message.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The send pipeline that configures the context.</param>
    /// <param name="cancellationToken">The token that cancels context creation.</param>
    /// <returns>A task that produces the configured send context.</returns>
    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        var sendContext = new InMemorySendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        return sendContext;
    }

    /// <summary>Creates a send context within an existing transport pipeline.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="context">The active transport pipeline context.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The send pipeline that configures the context.</param>
    /// <param name="cancellationToken">The token that cancels context creation.</param>
    /// <returns>A task that produces the configured send context.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(PipeContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>Creates a transport envelope and delivers it to the exchange.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="transportContext">The active transport pipeline context.</param>
    /// <param name="sendContext">The serialized send context.</param>
    /// <param name="cancellationToken">The token that cancels exchange delivery.</param>
    /// <returns>A task that completes when the exchange accepts the envelope.</returns>
    public Task SendAsync<T>(PipeContext transportContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(transportContext);
        ArgumentNullException.ThrowIfNull(sendContext);
        cancellationToken.ThrowIfCancellationRequested();

        InMemorySendContext<T> context = sendContext as InMemorySendContext<T>
            ?? throw new ArgumentException("The send context was not created by the in-memory transport.", nameof(sendContext));

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        var messageId = context.MessageId ?? NewId.NextGuid();

        var body = context.Body ?? throw new InvalidOperationException("The send context body has not been serialized.");
        var contentType = context.ContentType ?? throw new InvalidOperationException("The send context content type has not been set.");
        var transportMessage = new InMemoryTransportMessage(messageId, body.GetBytes(), contentType.ToString())
        {
            Delay = context.Delay,
            RoutingKey = context.RoutingKey
        };

        if (context.TryGetPayload(out InMemoryDurableSendContext? durableSendContext))
            transportMessage.DurableSendContext = durableSendContext;

        InMemoryTransportHeaderMapper.Copy(context.Headers, transportMessage.Headers);

        var deliveryContext = new InMemoryDeliveryContext(transportMessage, _delayProvider.UtcNow, context.CancellationToken);

        return _exchange.DeliverAsync(deliveryContext, cancellationToken: cancellationToken);
    }

    /// <summary>Executes a transport pipeline in a new pipe context.</summary>
    /// <param name="pipe">The transport pipeline to execute.</param>
    /// <param name="cancellationToken">The token that cancels pipeline execution.</param>
    /// <returns>A task that completes when the pipeline completes.</returns>
    public Task SendAsync(IPipe<PipeContext> pipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        var pipeContext = new Context(cancellationToken);

        return pipe.SendAsync(pipeContext);
    }

    /// <summary>Adds the destination exchange to a diagnostic probe.</summary>
    /// <param name="context">The probe that receives the exchange state.</param>
    public void Probe(ProbeContext context)
    {
        _exchange.Probe(context);
    }

    sealed class Context :
        BasePipeContext
    {
        public Context(CancellationToken cancellationToken)
            : base(cancellationToken)
        {
        }
    }
}
