using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

public class TopicSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<ClientContext>
{
    readonly IPipe<ClientContext> _configureTopologyPipe;
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly IClientContextSupervisor _supervisor;

    public TopicSendTransportContext(ISqlHostConfiguration hostConfiguration, ReceiveEndpointContext receiveEndpointContext,
        IClientContextSupervisor supervisor, IPipe<ClientContext> configureTopologyPipe, string entityName)
        : base(hostConfiguration, receiveEndpointContext.Serialization)
    {
        _hostConfiguration = hostConfiguration;
        _supervisor = supervisor;

        _configureTopologyPipe = configureTopologyPipe;
        EntityName = entityName;
    }

    public override string EntityName { get; }
    public override string ActivitySystem => "db";

    public void Probe(ProbeContext context)
    {
    }

    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new SqlMessageSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        return sendContext;
    }

    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [];
    }

    public Task SendAsync(IPipe<ClientContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    public Task<SendContext<T>> CreateSendContextAsync<T>(ClientContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    public async Task SendAsync<T>(ClientContext clientContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        SqlMessageSendContext<T> context = sendContext as SqlMessageSendContext<T>
            ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        await _configureTopologyPipe.SendAsync(clientContext).ConfigureAwait(false);

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        if (Activity.Current?.IsAllDataRequested ?? false)
        {
            if (!string.IsNullOrWhiteSpace(context.RoutingKey))
                Activity.Current.SetTag(nameof(context.RoutingKey), context.RoutingKey);
            if (!string.IsNullOrWhiteSpace(context.PartitionKey))
                Activity.Current.SetTag(nameof(context.PartitionKey), context.PartitionKey);
        }

        await clientContext.PublishAsync(EntityName, context, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
