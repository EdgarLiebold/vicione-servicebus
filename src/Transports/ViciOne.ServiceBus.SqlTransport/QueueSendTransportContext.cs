using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Carries state for queue send transport operations.</summary>
public class QueueSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<ClientContext>
{
    readonly IPipe<ClientContext> _configureTopologyPipe;
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly IClientContextSupervisor _supervisor;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context.</param>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="configureTopologyPipe">The configure topology pipe.</param>
    /// <param name="entityName">The entity name.</param>
    public QueueSendTransportContext(ISqlHostConfiguration hostConfiguration, ReceiveEndpointContext receiveEndpointContext,
        IClientContextSupervisor supervisor, IPipe<ClientContext> configureTopologyPipe, string entityName)
        : base(hostConfiguration, receiveEndpointContext.Serialization)
    {
        _hostConfiguration = hostConfiguration;
        _supervisor = supervisor;

        _configureTopologyPipe = configureTopologyPipe;
        EntityName = entityName;
    }

    /// <summary>Gets the entity name.</summary>
    public override string EntityName { get; }
    /// <summary>Gets the activity system.</summary>
    public override string ActivitySystem => "db";

    /// <summary>Adds the supervised SQL client lifecycle to the transport probe.</summary>
    /// <param name="context">The probe context to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _supervisor.Probe(context);
    }

    /// <summary>Creates send context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new SqlMessageSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        CopyIncomingIdentifiersIfPresent(sendContext);

        return sendContext;
    }

    /// <summary>Gets the client supervisor owned by this queue transport.</summary>
    /// <returns>The transport's supervised client agent.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [_supervisor];
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(IPipe<ClientContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    /// <summary>Creates send context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(ClientContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="clientContext">The client context.</param>
    /// <param name="sendContext">The send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

        await clientContext.SendAsync(EntityName, context, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    static void CopyIncomingIdentifiersIfPresent<T>(SqlMessageSendContext<T> context)
        where T : class
    {
        if (context.TryGetPayload<ConsumeContext>(out var consumeContext) && consumeContext.TryGetPayload<SqlMessageContext>(out var dbMessageContext))
        {
            if (context.PartitionKey == null && dbMessageContext.PartitionKey != null)
                context.PartitionKey = dbMessageContext.PartitionKey;
        }
    }
}
