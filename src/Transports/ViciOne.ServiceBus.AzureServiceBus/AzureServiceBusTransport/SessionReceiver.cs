using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a session receiver implementation.
/// </summary>
public class SessionReceiver :
    Receiver
{
    readonly ClientContext _clientContext;
    readonly ServiceBusReceiveEndpointContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="clientContext">The client context value.</param>
    /// <param name="context">The operation context.</param>
    public SessionReceiver(ClientContext clientContext, ServiceBusReceiveEndpointContext context)
        : base(clientContext, context)
    {
        _clientContext = clientContext;
        _context = context;
    }

    /// <summary>
    /// Starts the configured component.
    /// </summary>
    public override void Start()
    {
        _clientContext.OnSessionAsync(OnSessionAsync, ExceptionHandlerAsync);

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
                    cancellationTokenSource = new CancellationTokenSource(_context.ConsumerStopTimeout.Value);
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
            // do NOT let exceptions propagate to the Azure SDK
        }
        finally
        {
            timeoutRegistration.Dispose();
            registration.Dispose();

            cancellationTokenSource?.Dispose();

            context.Dispose();
        }
    }
}
