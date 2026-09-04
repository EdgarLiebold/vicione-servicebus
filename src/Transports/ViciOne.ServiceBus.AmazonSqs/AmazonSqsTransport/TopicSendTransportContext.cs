using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a topic send transport context implementation.
/// </summary>
public class TopicSendTransportContext :
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
    public TopicSendTransportContext(IAmazonSqsHostConfiguration hostConfiguration, ReceiveEndpointContext receiveEndpointContext,
        IClientContextSupervisor supervisor, IPipe<ClientContext> configureTopologyPipe, string entityName)
        : base(hostConfiguration, receiveEndpointContext.Serialization)
    {
        _hostConfiguration = hostConfiguration;
        _supervisor = supervisor;
        _configureTopologyPipe = configureTopologyPipe;

        EntityName = entityName;

        _headerAdapter = new TransportSetHeaderAdapter<MessageAttributeValue>(
            new SnsHeaderValueConverter(hostConfiguration.Settings.AllowTransportHeader), TransportHeaderOptions.IncludeFaultMessage);
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
        return new IAgent[] { _supervisor };
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

        AmazonSqsDelay.EnsureNotSetForTopic(context.Delay);

        await _configureTopologyPipe.SendAsync(transportContext).ConfigureAwait(false);

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        var request = new PublishBatchRequestEntry
        {
            Message = context.Body.GetString(),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
        };

        _headerAdapter.Set(request.MessageAttributes, context.Headers);
        _headerAdapter.Set(request.MessageAttributes, MessageHeaders.ContentType, context.ContentType!.ToString());
        _headerAdapter.Set(request.MessageAttributes, nameof(context.CorrelationId), context.CorrelationId);

        if (!string.IsNullOrEmpty(context.DeduplicationId))
            request.MessageDeduplicationId = context.DeduplicationId;

        if (!string.IsNullOrEmpty(context.GroupId))
            request.MessageGroupId = context.GroupId;

        await transportContext.PublishAsync(EntityName, request, context.CancellationToken).ConfigureAwait(false);
    }
}
