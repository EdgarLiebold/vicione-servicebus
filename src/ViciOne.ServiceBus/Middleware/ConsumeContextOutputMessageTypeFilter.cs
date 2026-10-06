using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Converts an inbound context type to a pipe context type post-dispatch.</summary>
/// <typeparam name="TMessage">The subsequent pipe context type.</typeparam>
public class ConsumeContextOutputMessageTypeFilter<TMessage> :
    IConsumeContextOutputMessageTypeFilter<TMessage>
    where TMessage : class
{
    readonly ConsumeObservable _consumeObservers;
    readonly ConsumeMessageObservable<TMessage> _observers;
    readonly IRequestIdTeeFilter<TMessage> _output;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="observers">The observers.</param>
    /// <param name="output">The output.</param>
    public ConsumeContextOutputMessageTypeFilter(ConsumeObservable observers, IRequestIdTeeFilter<TMessage> output)
    {
        _output = output;

        _consumeObservers = observers;
        _observers = new ConsumeMessageObservable<TMessage>();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("dispatchPipe");
        scope.Add("outputType", TypeCache<ConsumeContext<TMessage>>.ShortName);

        _output.Probe(scope);
    }

    /// <summary>Dispatches a compatible typed consume context with an awaited continuation, or forwards an incompatible context to the continuation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        return context.TryGetMessage(out ConsumeContext<TMessage>? pipeContext)
            ? SendToOutputAsync(next, pipeContext)
            : next.SendAsync(context);
    }

    /// <summary>Connects consume message observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver(IConsumeMessageObserver<TMessage> observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>Connects pipe.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe(IPipe<ConsumeContext<TMessage>> pipe)
    {
        return _output.ConnectPipe(pipe);
    }

    /// <summary>Connects pipe.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe(Guid key, IPipe<ConsumeContext<TMessage>> pipe)
    {
        return _output.ConnectPipe(key, pipe);
    }

    async Task SendToOutputAsync(IPipe<ConsumeContext> next, ConsumeContext<TMessage> pipeContext)
    {
        if (_observers.Count > 0)
        {
            var preConsumeTask = _observers.PreConsumeAsync(pipeContext);
            if (preConsumeTask.Status != TaskStatus.RanToCompletion)
                await preConsumeTask.ConfigureAwait(false);
        }

        if (_consumeObservers.Count > 0)
        {
            var preConsumeTask = _consumeObservers.PreConsumeAsync(pipeContext);
            if (preConsumeTask.Status != TaskStatus.RanToCompletion)
                await preConsumeTask.ConfigureAwait(false);
        }

        try
        {
            var typedNext = Pipe.ExecuteAwaited<ConsumeContext<TMessage>>(messageContext => next.SendAsync(messageContext.Advanced()));

            await _output.SendAsync(pipeContext, typedNext).ConfigureAwait(false);

            if (_observers.Count > 0)
            {
                var postConsumeTask = _observers.PostConsumeAsync(pipeContext);
                if (postConsumeTask.Status != TaskStatus.RanToCompletion)
                    await postConsumeTask.ConfigureAwait(false);
            }

            if (_consumeObservers.Count > 0)
            {
                var postConsumeTask = _consumeObservers.PostConsumeAsync(pipeContext);
                if (postConsumeTask.Status != TaskStatus.RanToCompletion)
                    await postConsumeTask.ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await NotifyFaultObserversAsync(pipeContext, ex).ConfigureAwait(false);
            throw;
        }
    }

    async Task NotifyFaultObserversAsync(ConsumeContext<TMessage> pipeContext, Exception dispatchFailure)
    {
        if (_observers.Count > 0)
        {
            try
            {
                var consumeFaultTask = _observers.ConsumeFaultAsync(pipeContext, dispatchFailure);
                if (consumeFaultTask.Status != TaskStatus.RanToCompletion)
                    await consumeFaultTask.ConfigureAwait(false);
            }
            catch (Exception observerFailure)
            {
                LogObserverFailure(observerFailure, "typed");
            }
        }

        if (_consumeObservers.Count > 0)
        {
            try
            {
                var consumeFaultTask = _consumeObservers.ConsumeFaultAsync(pipeContext, dispatchFailure);
                if (consumeFaultTask.Status != TaskStatus.RanToCompletion)
                    await consumeFaultTask.ConfigureAwait(false);
            }
            catch (Exception observerFailure)
            {
                LogObserverFailure(observerFailure, "outer");
            }
        }
    }

    static void LogObserverFailure(Exception observerFailure, string observerScope)
    {
        try
        {
            LogContext.Error?.Log(observerFailure, "A consume-output {ObserverScope} fault observer failed after dispatch faulted: {MessageType}", observerScope, typeof(TMessage));
        }
        catch
        {
            // Diagnostic logging must not replace the consume failure.
        }
    }
}
