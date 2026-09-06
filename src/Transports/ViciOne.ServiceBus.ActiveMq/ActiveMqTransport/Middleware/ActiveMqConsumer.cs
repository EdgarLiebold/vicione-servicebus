using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq.Middleware;

/// <summary>Dispatches Apache NMS deliveries to an ActiveMQ receive endpoint.</summary>
public sealed class ActiveMqConsumer :
    ConsumerAgent<string>
{
    readonly ActiveMqReceiveEndpointContext _context;
    readonly TaskExecutor _executor;
    readonly IMessageConsumer _messageConsumer;
    readonly ReceiveSettings _receiveSettings;
    readonly SessionContext _session;

    /// <summary>Creates and starts a delivery agent for a native message consumer.</summary>
    /// <param name="session">The Apache NMS session context.</param>
    /// <param name="messageConsumer">The native consumer that supplies deliveries.</param>
    /// <param name="context">The receive-endpoint context.</param>
    /// <param name="executor">The executor that bounds concurrent message dispatch.</param>
    public ActiveMqConsumer(SessionContext session, IMessageConsumer messageConsumer, ActiveMqReceiveEndpointContext context, TaskExecutor executor)
        : base(context, StringComparer.Ordinal)
    {
        _session = session;
        _messageConsumer = messageConsumer;
        _context = context;
        _executor = executor;

        _receiveSettings = session.GetPayload<ReceiveSettings>();

        messageConsumer.Listener += HandleMessage;

        TrySetManualConsumeTask();

        SetReady();
    }

    void HandleMessage(IMessage message)
    {
        _executor.EnqueueBlocking(async () =>
        {
            if (IsStopping)
                return;

            LogContext.Current = _context.LogContext;

            var context = new ActiveMqReceiveContext(message, _context, _receiveSettings, _session, _session.ConnectionContext);

            try
            {
                await DispatchAsync(message.NMSMessageId, context, new ActiveMqReceiveLockContext(message)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                context.LogTransportFaulted(exception);
            }
            finally
            {
                context.Dispose();
            }
        }, Stopping);
    }

    /// <summary>Stops native delivery, waits for dispatched messages, and closes the consumer.</summary>
    /// <param name="context">The agent stop context.</param>
    /// <returns>A task that completes when consumer shutdown has finished.</returns>
    protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        _messageConsumer.Stop();
        _messageConsumer.Listener -= HandleMessage;
        _messageConsumer.Start();

        await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);

        try
        {
            await _messageConsumer.CloseAsync().ConfigureAwait(false);
            _messageConsumer.Dispose();
        }
        catch (OperationCanceledException)
        {
            LogContext.Warning?.Log("Stop canceled waiting for consumer shutdown: {InputAddress}", _context.InputAddress);
        }
    }
}
