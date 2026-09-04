using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqsTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public class QueueSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<ClientContext>
{
    readonly IPipe<ClientContext> _configureTopologyPipe;
    readonly ITransportSetHeaderAdapter<MessageAttributeValue> _headerAdapter;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly IClientContextSupervisor _supervisor;

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

    public override string EntityName { get; }
    public override string ActivitySystem => "aws_sqs";

    public Task SendAsync(IPipe<ClientContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }

    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new AmazonSqsMessageSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        return sendContext;
    }

    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [_supervisor];
    }

    public Task<SendContext<T>> CreateSendContextAsync<T>(ClientContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

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
