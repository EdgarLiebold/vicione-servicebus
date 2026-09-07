using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Runs a non-session Azure Service Bus processor and dispatches its deliveries.</summary>
public class Receiver :
    ConsumerAgent<long>,
    IReceiver
{
    readonly ClientContext _clientContext;
    readonly ServiceBusReceiveEndpointContext _context;

    /// <summary>Creates a receiver for a processor client and receive endpoint.</summary>
    /// <param name="clientClientContext">The processor client context.</param>
    /// <param name="context">The receive endpoint context that owns dispatch.</param>
    public Receiver(ClientContext clientClientContext, ServiceBusReceiveEndpointContext context)
        : base(context)
    {
        _clientContext = clientClientContext;
        _context = context;

        TrySetManualConsumeTask();
    }

    /// <summary>Registers processor callbacks and starts the Azure Service Bus processor.</summary>
    public virtual void Start()
    {
        _clientContext.ConfigureMessageProcessor(OnMessageAsync, ExceptionHandlerAsync);

        SetReady(_clientContext.StartAsync());
    }

    /// <summary>Classifies an Azure processor error and faults the client supervisor when recycling is required.</summary>
    /// <param name="args">The processor error and its source.</param>
    /// <returns>A task that completes after any required fault notification.</returns>
    protected async Task ExceptionHandlerAsync(ProcessErrorEventArgs args)
    {
        var requiresRecycle = args.Exception switch
        {
            MessageTimeToLiveExpiredException _ => false,
            MessageLockExpiredException _ => false,

            ServiceBusException { Reason: ServiceBusFailureReason.MessageLockLost } => false,
            ServiceBusException { Reason: ServiceBusFailureReason.SessionLockLost } => false,

            ServiceBusException { Reason: ServiceBusFailureReason.ServiceCommunicationProblem } => true,
            ServiceBusException { Reason: ServiceBusFailureReason.MessagingEntityNotFound } => true,
            ServiceBusException { Reason: ServiceBusFailureReason.MessagingEntityDisabled } => false,

            ServiceBusException { IsTransient: true } => false,

            _ => true
        };

        switch (args.Exception)
        {
            case ServiceBusException { IsTransient: true, Reason: ServiceBusFailureReason.ServiceCommunicationProblem }:
                LogContext.Debug?.Log(args.Exception,
                    "ServiceBusException on Receiver {InputAddress} during {Action} ActiveDispatchCount({activeDispatch}) ErrorRequiresRecycle({requiresRecycle})",
                    _clientContext.InputAddress, args.ErrorSource, ActiveDispatchCount, requiresRecycle);
                break;
            case WebSocketException exception:
                LogContext.Debug?.Log(exception,
                    "WebSocketException on Receiver {InputAddress} code {Code} ActiveDispatchCount({activeDispatch}) ErrorRequiresRecycle({requiresRecycle})",
                    _clientContext.InputAddress, exception.WebSocketErrorCode, ActiveDispatchCount, requiresRecycle);
                break;
            case ObjectDisposedException { ObjectName: "$cbs" }:
            case ServiceBusException { Reason: ServiceBusFailureReason.MessageLockLost }:
            case ServiceBusException { Reason: ServiceBusFailureReason.SessionLockLost }:
            case ServiceBusException { Reason: ServiceBusFailureReason.MessagingEntityDisabled }:
                // These expected lifecycle conditions require no additional receiver log entry.
                break;
            default:
                {
                    if (!(args.Exception is OperationCanceledException) && !(args.Exception.InnerException is TimeoutException))
                    {
                        EnabledLogger? logger = requiresRecycle ? LogContext.Error : LogContext.Warning;

                        logger?.Log(args.Exception,
                            "Exception on Receiver {InputAddress} during {Action} ActiveDispatchCount({activeDispatch}) ErrorRequiresRecycle({requiresRecycle})",
                            _clientContext.InputAddress, args.ErrorSource, ActiveDispatchCount, requiresRecycle);
                    }

                    break;
                }
        }

        if (requiresRecycle)
        {
            await _clientContext.NotifyFaultedAsync(args.Exception, args.EntityPath).ConfigureAwait(false);

            TrySetConsumeException(args.Exception);
        }
    }

    /// <summary>Shuts down dispatch, waits for active agents, and closes the Azure processor context.</summary>
    /// <param name="context">The stop context.</param>
    /// <returns>A task that completes after receiver shutdown.</returns>
    protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        await _clientContext.ShutdownAsync().ConfigureAwait(false);

        await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);

        try
        {
            await _clientContext.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Failed to close the message receiver context: {InputAddress}", _clientContext.InputAddress);
        }
    }

    async Task OnMessageAsync(ProcessMessageEventArgs messageReceiver, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        if (IsStopping)
            return;

        MessageLockContext lockContext = new ServiceBusMessageLockContext(messageReceiver, message, Stopped);
        var context = new ServiceBusReceiveContext(message, _context, cancellationToken, lockContext, _clientContext);

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
            // The receiver callback owns dispatch failures so they cannot escape into the Azure SDK pump.
        }
        finally
        {
            timeoutRegistration.Dispose();
            registration.Dispose();

            cancellationTokenSource?.Dispose();

            context.Dispose();
        }
    }

    /// <summary>Dispatches a delivery under a renewable Azure Service Bus receive lock.</summary>
    /// <param name="message">The broker delivery.</param>
    /// <param name="context">The transport receive context.</param>
    /// <param name="lockContext">The settlement context for the delivery.</param>
    /// <returns>A task that completes when dispatch and settlement handling finish.</returns>
    protected async Task DispatchAsync(ServiceBusReceivedMessage message, ServiceBusReceiveContext context, MessageLockContext lockContext)
    {
        try
        {
            await DispatchAsync(context.SequenceNumber, context,
                    new ServiceBusReceiveLockContext(_context.InputAddress, lockContext, message, _context.GetTimeProvider()))
                .ConfigureAwait(false);
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
    }
}
