using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Dispatches in-process deliveries through one configured receive endpoint.</summary>
public sealed class ReceiveEndpointDispatcher :
    IReceiveEndpointDispatcher
{
    readonly ReceiveEndpointContext _context;
    readonly IReceivePipeDispatcher _dispatcher;

    /// <summary>Initializes a dispatcher for a configured receive endpoint.</summary>
    /// <param name="context">The endpoint context that supplies the receive pipeline and observers.</param>
    public ReceiveEndpointDispatcher(ReceiveEndpointContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        _dispatcher = context.CreateReceivePipeDispatcher()
            ?? throw new InvalidOperationException("The receive endpoint context returned no pipe dispatcher.");
    }

    /// <summary>Adds receive-pipeline diagnostics to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("dispatcher");

        _context.ReceivePipe.Probe(scope);
    }

    /// <summary>Subscribes an observer to receive notifications.</summary>
    /// <param name="observer">The observer that receives the notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectReceiveObserver(observer);
    }

    /// <summary>Subscribes an observer to messages published by the receive pipeline.</summary>
    /// <param name="observer">The observer that receives publish notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectPublishObserver(observer);
    }

    /// <summary>Subscribes an observer to messages sent by the receive pipeline.</summary>
    /// <param name="observer">The observer that receives send notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectSendObserver(observer);
    }

    /// <summary>Subscribes a typed observer to consume notifications.</summary>
    /// <typeparam name="T">The observed message contract.</typeparam>
    /// <param name="observer">The observer that receives typed consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ReceivePipe.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Subscribes an observer to consume notifications.</summary>
    /// <param name="observer">The observer that receives consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ReceivePipe.ConnectConsumeObserver(observer);
    }

    /// <summary>Gets the number of deliveries currently being dispatched.</summary>
    public int ActiveDispatchCount => _dispatcher.ActiveDispatchCount;

    /// <summary>Gets the total number of dispatches started by this instance.</summary>
    public long DispatchCount => _dispatcher.DispatchCount;

    /// <summary>Gets the highest number of concurrent dispatches observed by this instance.</summary>
    public int MaxConcurrentDispatchCount => _dispatcher.MaxConcurrentDispatchCount;

    /// <summary>Occurs after the active dispatch count returns to zero.</summary>
    public event ZeroActivityHandler ZeroActivity
    {
        add => _dispatcher.ZeroActivity += value;
        remove => _dispatcher.ZeroActivity -= value;
    }

    /// <summary>Captures the current cumulative delivery metrics.</summary>
    /// <returns>An immutable metrics snapshot.</returns>
    public IDeliveryMetrics GetMetrics()
    {
        return _dispatcher.GetMetrics();
    }

    /// <summary>Gets the address represented by this dispatcher.</summary>
    public Uri InputAddress => _context.InputAddress;

    /// <summary>Dispatches one serialized message through the configured receive pipeline.</summary>
    /// <param name="body">The serialized message body.</param>
    /// <param name="headers">The transport header values.</param>
    /// <param name="payloads">The additional payloads attached to the receive context.</param>
    /// <param name="cancellationToken">The token that cancels dispatch and lock settlement.</param>
    /// <returns>A task that completes after the message has been dispatched and settled.</returns>
    public async Task DispatchAsync(byte[] body, IReadOnlyDictionary<string, object> headers, object[] payloads,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(payloads);
        cancellationToken.ThrowIfCancellationRequested();

        var context = new ReceiveEndpointDispatcherReceiveContext(_context, body, headers, payloads);

        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
            registration = cancellationToken.Register(context.Cancel);

        try
        {
            await _dispatcher.DispatchAsync(context, NoLockReceiveContext.Instance, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            registration.Dispose();
            context.Dispose();
        }
    }
}


/// <summary>Resolves and exposes the receive dispatcher associated with a registration type.</summary>
/// <typeparam name="T">The registered consumer, saga, or activity type.</typeparam>
public sealed class ReceiveEndpointDispatcher<T> :
    IReceiveEndpointDispatcher<T>
    where T : class
{
    readonly IReceiveEndpointDispatcher _dispatcher;

    /// <summary>Initializes a dispatcher using the default endpoint-name formatter.</summary>
    /// <param name="factory">The factory that resolves registration-owned dispatchers.</param>
    public ReceiveEndpointDispatcher(IReceiveEndpointDispatcherFactory factory)
        : this(factory, DefaultEndpointNameFormatter.Instance)
    {
    }

    /// <summary>Initializes a dispatcher for the registration type.</summary>
    /// <param name="factory">The factory that resolves registration-owned dispatchers.</param>
    /// <param name="formatter">The formatter used to derive the fallback endpoint name.</param>
    public ReceiveEndpointDispatcher(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(formatter);

        _dispatcher = factory.CreateRegistrationReceiver(typeof(T), formatter.Message<T>(), formatter)
            ?? throw new InvalidOperationException("The receive endpoint dispatcher factory returned no dispatcher.");
    }

    /// <summary>Subscribes an observer to consume notifications.</summary>
    /// <param name="observer">The observer that receives consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _dispatcher.ConnectConsumeObserver(observer);
    }

    /// <summary>Subscribes a typed observer to consume notifications.</summary>
    /// <typeparam name="T1">The observed message contract.</typeparam>
    /// <param name="observer">The observer that receives typed consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T1>(IConsumeMessageObserver<T1> observer)
        where T1 : class
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _dispatcher.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Gets the number of deliveries currently being dispatched.</summary>
    public int ActiveDispatchCount => _dispatcher.ActiveDispatchCount;
    /// <summary>Gets the total number of dispatches started by the underlying dispatcher.</summary>
    public long DispatchCount => _dispatcher.DispatchCount;
    /// <summary>Gets the highest number of concurrent dispatches observed by the underlying dispatcher.</summary>
    public int MaxConcurrentDispatchCount => _dispatcher.MaxConcurrentDispatchCount;

    /// <summary>Occurs after the active dispatch count returns to zero.</summary>
    public event ZeroActivityHandler ZeroActivity
    {
        add => _dispatcher.ZeroActivity += value;
        remove => _dispatcher.ZeroActivity -= value;
    }

    /// <summary>Captures the current cumulative delivery metrics.</summary>
    /// <returns>An immutable metrics snapshot.</returns>
    public IDeliveryMetrics GetMetrics()
    {
        return _dispatcher.GetMetrics();
    }

    /// <summary>Subscribes an observer to receive notifications.</summary>
    /// <param name="observer">The observer that receives the notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _dispatcher.ConnectReceiveObserver(observer);
    }

    /// <summary>Subscribes an observer to messages published by the receive pipeline.</summary>
    /// <param name="observer">The observer that receives publish notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _dispatcher.ConnectPublishObserver(observer);
    }

    /// <summary>Subscribes an observer to messages sent by the receive pipeline.</summary>
    /// <param name="observer">The observer that receives send notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _dispatcher.ConnectSendObserver(observer);
    }

    /// <summary>Adds receive-pipeline diagnostics to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _dispatcher.Probe(context);
    }

    /// <summary>Gets the address represented by the underlying dispatcher.</summary>
    public Uri InputAddress => _dispatcher.InputAddress;

    /// <summary>Dispatches one serialized message through the configured receive pipeline.</summary>
    /// <param name="body">The serialized message body.</param>
    /// <param name="headers">The transport header values.</param>
    /// <param name="payloads">The additional payloads attached to the receive context.</param>
    /// <param name="cancellationToken">The token that cancels dispatch and lock settlement.</param>
    /// <returns>A task that completes after the message has been dispatched and settled.</returns>
    public Task DispatchAsync(byte[] body, IReadOnlyDictionary<string, object> headers, object[] payloads,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(payloads);
        cancellationToken.ThrowIfCancellationRequested();
        return _dispatcher.DispatchAsync(body, headers, payloads, cancellationToken);
    }
}
