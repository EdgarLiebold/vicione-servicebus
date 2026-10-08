using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Runs a session-aware Azure Service Bus processor and dispatches its deliveries.</summary>
public class SessionReceiver :
    Receiver
{
    readonly ClientContext _clientContext;
    readonly ServiceBusReceiveEndpointContext _context;

    /// <summary>Creates a session receiver for a processor client and receive endpoint.</summary>
    /// <param name="clientContext">The session processor client context.</param>
    /// <param name="context">The receive endpoint context that owns dispatch.</param>
    public SessionReceiver(ClientContext clientContext, ServiceBusReceiveEndpointContext context)
        : base(clientContext, context)
    {
        _clientContext = clientContext;
        _context = context;
    }

    /// <summary>Registers session callbacks and starts the Azure Service Bus session processor.</summary>
    public override void Start()
    {
        _clientContext.ConfigureSessionProcessor(OnSessionAsync, ExceptionHandlerAsync);

        SetReady(_clientContext.StartAsync());
    }

    async Task OnSessionAsync(ProcessSessionMessageEventArgs messageSession, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        if (IsStopping)
            return;

        MessageLockContext lockContext = new ServiceBusSessionMessageLockContext(messageSession, message, Stopped);
        MessageSessionContext sessionContext = new ServiceBusMessageSessionContext(messageSession, Stopped);
        var context = new ServiceBusReceiveContext(message, _context, lockContext, _clientContext, sessionContext);

        CancellationTokenSource? cancellationTokenSource = null;
        CancellationTokenRegistration timeoutRegistration = default;
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            void Callback()
            {
                if (_context.ConsumerStopTimeout.HasValue)
                {
                    cancellationTokenSource = new CancellationTokenSource(_context.ConsumerStopTimeout.Value, _context.GetTimeProvider());
                    timeoutRegistration = cancellationTokenSource.Token.Register(context.Cancel);
                }
                else
                    context.Cancel();
            }

            registration = cancellationToken.Register(Callback);
        }

        try
        {
            await DispatchAsync(message, context, lockContext).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The receiver callback owns dispatch failures so they cannot escape into the Azure SDK pump.
        }
        finally
        {
            // Join the broker callback before disposing the timeout registration it may publish.
            registration.Dispose();
            timeoutRegistration.Dispose();

            cancellationTokenSource?.Dispose();

            context.Dispose();
        }
    }
}
