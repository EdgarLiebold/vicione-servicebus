using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Dispatches an Azure Functions trigger message through a configured receive pipeline.</summary>
public class ServiceBusMessageReceiver :
    IServiceBusMessageReceiver
{
    readonly ReceiveEndpointContext _context;
    readonly IReceivePipeDispatcher _dispatcher;

    /// <summary>Creates a dispatcher for a configured receive endpoint.</summary>
    /// <param name="context">The receive endpoint context that supplies pipeline and transport settings.</param>
    public ServiceBusMessageReceiver(ReceiveEndpointContext context)
    {
        _context = context;

        _dispatcher = context.CreateReceivePipeDispatcher();
    }

    /// <summary>Wraps and dispatches a received Azure Service Bus message without broker settlement.</summary>
    /// <param name="message">The trigger delivery to dispatch.</param>
    /// <param name="cancellationToken">Cancels receive-pipeline dispatch.</param>
    /// <returns>The dispatcher task for the trigger message's receive context.</returns>
    public async Task HandleAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var context = new ServiceBusReceiveContext(message, _context);

        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
            registration = cancellationToken.Register(context.Cancel);

        try
        {
            await _dispatcher.DispatchAsync(context, NoLockReceiveContext.Instance, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.SessionLockLost)
        {
            LogContext.Error?.Log("Session Lock Lost: {InputAddress} {MessageId} {SequenceNumber} ({SessionId})", _context.InputAddress,
                message.MessageId, message.SequenceNumber, message.SessionId);

            throw;
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageLockLost)
        {
            LogContext.Error?.Log("Message Lock Lost: {InputAddress} {MessageId} {SequenceNumber}", _context.InputAddress, message.MessageId,
                message.SequenceNumber);

            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            context.LogTransportFaulted(exception);
            throw;
        }
        finally
        {
            registration.Dispose();
            context.Dispose();
        }
    }
}
