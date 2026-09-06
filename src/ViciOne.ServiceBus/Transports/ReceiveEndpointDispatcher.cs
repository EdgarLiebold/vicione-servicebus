using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Dispatches receive endpoint operations.</summary>
public class ReceiveEndpointDispatcher :
    IReceiveEndpointDispatcher
{
    readonly ReceiveEndpointContext _context;
    readonly IReceivePipeDispatcher _dispatcher;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public ReceiveEndpointDispatcher(ReceiveEndpointContext context)
    {
        _context = context;

        _dispatcher = context.CreateReceivePipeDispatcher();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("dispatcher");

        _context.ReceivePipe.Probe(scope);
    }

    /// <summary>Connects receive observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _context.ConnectReceiveObserver(observer);
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _context.ConnectPublishObserver(observer);
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _context.ConnectSendObserver(observer);
    }

    /// <summary>Connects consume message observer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return _context.ReceivePipe.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Connects consume observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _context.ReceivePipe.ConnectConsumeObserver(observer);
    }

    /// <summary>Gets the active dispatch count.</summary>
    public int ActiveDispatchCount => _dispatcher.ActiveDispatchCount;

    /// <summary>Gets the dispatch count.</summary>
    public long DispatchCount => _dispatcher.DispatchCount;

    /// <summary>Gets the max concurrent dispatch count.</summary>
    public int MaxConcurrentDispatchCount => _dispatcher.MaxConcurrentDispatchCount;

    /// <summary>Occurs when zero activity.</summary>
    public event ZeroActiveDispatchHandler ZeroActivity
    {
        add => _dispatcher.ZeroActivity += value;
        remove => _dispatcher.ZeroActivity -= value;
    }

    /// <summary>Gets metrics.</summary>
    /// <returns>The metrics.</returns>
    public DeliveryMetrics GetMetrics()
    {
        return _dispatcher.GetMetrics();
    }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress => _context.InputAddress;

    /// <summary>Dispatches the current message.</summary>
    /// <param name="body">The body.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="payloads">The payloads.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DispatchAsync(byte[] body, IReadOnlyDictionary<string, object> headers, object[] payloads,
        CancellationToken cancellationToken = default)
    {
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


/// <summary>Dispatches receive endpoint operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ReceiveEndpointDispatcher<T> :
    IReceiveEndpointDispatcher<T>
    where T : class
{
    readonly IReceiveEndpointDispatcher _dispatcher;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    public ReceiveEndpointDispatcher(IReceiveEndpointDispatcherFactory factory)
        : this(factory, DefaultEndpointNameFormatter.Instance)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="formatter">The formatter.</param>
    public ReceiveEndpointDispatcher(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
    {
        _dispatcher = factory.CreateRegistrationReceiver(typeof(T), formatter.Message<T>(), formatter);
    }

    /// <summary>Connects consume observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _dispatcher.ConnectConsumeObserver(observer);
    }

    /// <summary>Connects consume message observer.</summary>
    /// <typeparam name="T1">The 1 type.</typeparam>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T1>(IConsumeMessageObserver<T1> observer)
        where T1 : class
    {
        return _dispatcher.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Gets the active dispatch count.</summary>
    public int ActiveDispatchCount => _dispatcher.ActiveDispatchCount;
    /// <summary>Gets the dispatch count.</summary>
    public long DispatchCount => _dispatcher.DispatchCount;
    /// <summary>Gets the max concurrent dispatch count.</summary>
    public int MaxConcurrentDispatchCount => _dispatcher.MaxConcurrentDispatchCount;

    /// <summary>Occurs when zero activity.</summary>
    public event ZeroActiveDispatchHandler ZeroActivity
    {
        add => _dispatcher.ZeroActivity += value;
        remove => _dispatcher.ZeroActivity -= value;
    }

    /// <summary>Gets metrics.</summary>
    /// <returns>The metrics.</returns>
    public DeliveryMetrics GetMetrics()
    {
        return _dispatcher.GetMetrics();
    }

    /// <summary>Connects receive observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _dispatcher.ConnectReceiveObserver(observer);
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _dispatcher.ConnectPublishObserver(observer);
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _dispatcher.ConnectSendObserver(observer);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _dispatcher.Probe(context);
    }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress => _dispatcher.InputAddress;

    /// <summary>Dispatches the current message.</summary>
    /// <param name="body">The body.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="payloads">The payloads.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DispatchAsync(byte[] body, IReadOnlyDictionary<string, object> headers, object[] payloads,
        CancellationToken cancellationToken = default)
    {
        return _dispatcher.DispatchAsync(body, headers, payloads, cancellationToken);
    }
}
