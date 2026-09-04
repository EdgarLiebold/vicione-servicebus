using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a queue send transport context implementation.
/// </summary>
public class QueueSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<ClientContext>
{
    readonly IPipe<ClientContext> _configureTopologyPipe;
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly IClientContextSupervisor _supervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="configureTopologyPipe">The configure topology pipe value.</param>
    /// <param name="entityName">The entity name value.</param>
    public QueueSendTransportContext(ISqlHostConfiguration hostConfiguration, ReceiveEndpointContext receiveEndpointContext,
        IClientContextSupervisor supervisor, IPipe<ClientContext> configureTopologyPipe, string entityName)
        : base(hostConfiguration, receiveEndpointContext.Serialization)
    {
        _hostConfiguration = hostConfiguration;
        _supervisor = supervisor;

        _configureTopologyPipe = configureTopologyPipe;
        EntityName = entityName;
    }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public override string EntityName { get; }
    /// <summary>
    /// Gets the activity system value.
    /// </summary>
    public override string ActivitySystem => "db";

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
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
        var sendContext = new SqlMessageSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        CopyIncomingIdentifiersIfPresent(sendContext);

        return sendContext;
    }

    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return new IAgent[] { };
    }

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
    /// <param name="clientContext">The client context value.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
