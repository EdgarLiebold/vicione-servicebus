using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus message receiver implementation.
/// </summary>
public class ServiceBusMessageReceiver :
    IServiceBusMessageReceiver
{
    readonly ReceiveEndpointContext _context;
    readonly IReceivePipeDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public ServiceBusMessageReceiver(ReceiveEndpointContext context)
    {
        _context = context;

        _dispatcher = context.CreateReceivePipeDispatcher();
    }

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
