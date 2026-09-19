using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Builds and publishes message requests through an Amazon SNS topic transport.</summary>
public class TopicSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<ClientContext>
{
    const int DefaultMaximumSnsPublishBytes = 256 * 1024;

    readonly IPipe<ClientContext> _configureTopologyPipe;
    readonly ITransportSetHeaderAdapter<MessageAttributeValue> _headerAdapter;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly IClientContextSupervisor _supervisor;

    /// <summary>Initializes an Amazon SNS topic send-transport context.</summary>
    /// <param name="hostConfiguration">The host settings and retry policy.</param>
    /// <param name="receiveEndpointContext">The endpoint that supplies serialization settings.</param>
    /// <param name="supervisor">The client-context supervisor owned by the transport.</param>
    /// <param name="configureTopologyPipe">The pipeline that declares the destination topic before publishing.</param>
    /// <param name="entityName">The logical destination topic name.</param>
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

    /// <summary>Gets the logical destination topic name.</summary>
    public override string EntityName { get; }
    /// <summary>Gets the OpenTelemetry messaging-system identifier for Amazon SNS.</summary>
    public override string ActivitySystem => ServiceBusTelemetry.MessagingSystems.AmazonSns;

    /// <summary>Executes a client-context publish pipeline under the host retry policy.</summary>
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

    /// <summary>Creates an Amazon SNS send context and applies the caller's send pipeline.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message being published.</param>
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

    /// <summary>Creates an Amazon SNS send context for a client-context transport operation.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The active Amazon client context.</param>
    /// <param name="message">The message being published.</param>
    /// <param name="pipe">The send-context pipeline to apply.</param>
    /// <param name="cancellationToken">The token assigned to the send context.</param>
    /// <returns>The configured send context.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(ClientContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>Declares the topic, maps a send context to an Amazon SNS request, and publishes it.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="transportContext">The Amazon client context.</param>
    /// <param name="sendContext">The serialized Amazon SNS send context.</param>
    /// <param name="cancellationToken">The caller token that cancels topology declaration and provider submission.</param>
    /// <returns>A task that completes when Amazon SNS reports the publish result.</returns>
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

        AmazonSqsDelay.EnsureNotSetForTopic(context.Delay);
        string body = context.Body.GetRequiredTransportText();
        AmazonSqsTransportTextAdmission.Validate(context, body);

        operationToken.ThrowIfCancellationRequested();

        await _configureTopologyPipe.SendAsync(operationContext).ConfigureAwait(false);

        operationToken.ThrowIfCancellationRequested();

        var request = new PublishBatchRequestEntry
        {
            Message = body,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
        };

        _headerAdapter.Set(request.MessageAttributes, context.Headers);
        _headerAdapter.Set(request.MessageAttributes, MessageHeaders.ContentType, context.ContentType!.ToString());
        _headerAdapter.Set(request.MessageAttributes, MessageHeaders.CorrelationId, context.CorrelationId);

        if (!string.IsNullOrEmpty(context.DeduplicationId))
            request.MessageDeduplicationId = context.DeduplicationId;

        if (!string.IsNullOrEmpty(context.GroupId))
            request.MessageGroupId = context.GroupId;

        // SNS constructs the subscription notification JSON later; only the publish request is known here.
        long publishBytes = MessageDefaults.Encoding.GetByteCount(request.Message)
            + (long)AmazonMessageAttributeSizeCalculator.Calculate(request.MessageAttributes);
        if (publishBytes > DefaultMaximumSnsPublishBytes)
        {
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.TransportEnvelope,
                publishBytes,
                DefaultMaximumSnsPublishBytes,
                $"Amazon SNS publish message and attributes are {publishBytes} UTF-8 bytes, exceeding the default 256 KiB API limit of {DefaultMaximumSnsPublishBytes} bytes.");
        }

        await operationContext.PublishAsync(EntityName, request, operationToken).ConfigureAwait(false);
    }
}
