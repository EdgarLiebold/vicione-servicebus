using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Builds and sends message requests through an Amazon SQS queue transport.</summary>
public class QueueSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<ClientContext>
{
    readonly IPipe<ClientContext> _configureTopologyPipe;
    readonly ITransportSetHeaderAdapter<MessageAttributeValue> _headerAdapter;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly IClientContextSupervisor _supervisor;

    /// <summary>Initializes an Amazon SQS queue send-transport context.</summary>
    /// <param name="hostConfiguration">The host settings and retry policy.</param>
    /// <param name="receiveEndpointContext">The endpoint that supplies serialization settings.</param>
    /// <param name="supervisor">The client-context supervisor owned by the transport.</param>
    /// <param name="configureTopologyPipe">The pipeline that declares the destination queue before sending.</param>
    /// <param name="entityName">The logical destination queue name.</param>
    public QueueSendTransportContext(IAmazonSqsHostConfiguration hostConfiguration, ReceiveEndpointContext receiveEndpointContext,
        IClientContextSupervisor supervisor, IPipe<ClientContext> configureTopologyPipe, string entityName)
        : base(hostConfiguration, receiveEndpointContext.Serialization)
    {
        _hostConfiguration = hostConfiguration;
        _supervisor = supervisor;

        _configureTopologyPipe = configureTopologyPipe;
        EntityName = entityName;

        _headerAdapter = new TransportSetHeaderAdapter<MessageAttributeValue>(
            new SqsHeaderValueConverter(hostConfiguration.Settings.AllowTransportHeader), TransportHeaderOptions.IncludeFaultMessage);
    }

    /// <summary>Gets the logical destination queue name.</summary>
    public override string EntityName { get; }
    /// <summary>Gets the OpenTelemetry messaging-system identifier for Amazon SQS.</summary>
    public override string ActivitySystem => ServiceBusTelemetry.MessagingSystems.AmazonSqs;

    /// <summary>Executes a client-context send pipeline under the host retry policy.</summary>
    /// <param name="pipe">The client-context pipeline to execute.</param>
    /// <param name="cancellationToken">The token used to cancel retries and pipeline execution.</param>
    /// <returns>A task that completes when the pipeline succeeds.</returns>
    public Task SendAsync(IPipe<ClientContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    /// <summary>Adds client-supervisor diagnostics to a probe.</summary>
    /// <param name="context">The probe context.</param>
    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }

    /// <summary>Creates an Amazon SQS send context and applies the caller's send pipeline.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message being sent.</param>
    /// <param name="pipe">The send-context pipeline to apply.</param>
    /// <param name="cancellationToken">The token assigned to the send context.</param>
    /// <returns>The configured send context.</returns>
    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new AmazonSqsMessageSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        return sendContext;
    }

    /// <summary>Gets the client supervisor owned by this transport.</summary>
    /// <returns>The transport's agent handles.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [_supervisor];
    }

    /// <summary>Creates an Amazon SQS send context for a client-context transport operation.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The active Amazon client context.</param>
    /// <param name="message">The message being sent.</param>
    /// <param name="pipe">The send-context pipeline to apply.</param>
    /// <param name="cancellationToken">The token assigned to the send context.</param>
    /// <returns>The configured send context.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(ClientContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>Declares the queue, maps a send context to an Amazon SQS request, and sends it.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="transportContext">The Amazon client context.</param>
    /// <param name="sendContext">The serialized Amazon SQS send context.</param>
    /// <param name="cancellationToken">The caller token that cancels topology declaration and provider submission.</param>
    /// <returns>A task that completes when Amazon SQS reports the send result.</returns>
    public async Task SendAsync<T>(ClientContext transportContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = sendContext as AmazonSqsMessageSendContext<T>
            ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        using var sendLifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, sendContext.CancellationToken);
        using var operationContext = new ScopeClientContext(transportContext, sendLifetime.Token);
        CancellationToken operationToken = operationContext.CancellationToken;

        operationToken.ThrowIfCancellationRequested();

        await _configureTopologyPipe.SendAsync(operationContext).ConfigureAwait(false);

        operationToken.ThrowIfCancellationRequested();

        var message = new SendMessageBatchRequestEntry("", context.Body.GetString())
        {
            Id = sendContext.MessageId.ToString(),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
        };

        _headerAdapter.Set(message.MessageAttributes, context.Headers);
        _headerAdapter.Set(message.MessageAttributes, MessageHeaders.ContentType, context.ContentType!.ToString());
        _headerAdapter.Set(message.MessageAttributes, MessageHeaders.CorrelationId, context.CorrelationId);

        if (!string.IsNullOrEmpty(context.DeduplicationId))
            message.MessageDeduplicationId = context.DeduplicationId;

        if (!string.IsNullOrEmpty(context.GroupId))
            message.MessageGroupId = context.GroupId;

        var delaySeconds = AmazonSqsDelay.ForQueue(context.Delay, AmazonSqsEndpointAddress.IsFifo(EntityName));
        if (delaySeconds.HasValue)
            message.DelaySeconds = delaySeconds.Value;

        await operationContext.SendMessageAsync(EntityName, message, operationToken).ConfigureAwait(false);
    }
}
