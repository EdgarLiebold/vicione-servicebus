using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a queue send transport context implementation.
/// </summary>
public class QueueSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<ClientContext>
{
    readonly IPipe<ClientContext> _configureTopologyPipe;
    readonly ITransportSetHeaderAdapter<MessageAttributeValue> _headerAdapter;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly IClientContextSupervisor _supervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="configureTopologyPipe">The configure topology pipe value.</param>
    /// <param name="entityName">The entity name value.</param>
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

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public override string EntityName { get; }
    /// <summary>
    /// Gets the activity system value.
    /// </summary>
    public override string ActivitySystem => "aws_sqs";

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(IPipe<ClientContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new AmazonSqsMessageSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        return sendContext;
    }

    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [_supervisor];
    }

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(ClientContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="transportContext">The transport context value.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(ClientContext transportContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested(); AmazonSqsMessageSendContext<T> context = sendContext as AmazonSqsMessageSendContext<T>
                    ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        await _configureTopologyPipe.SendAsync(transportContext).ConfigureAwait(false);

        sendContext.CancellationToken.ThrowIfCancellationRequested();

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

        await transportContext.SendMessageAsync(EntityName, message, sendContext.CancellationToken).ConfigureAwait(false);
    }
}
