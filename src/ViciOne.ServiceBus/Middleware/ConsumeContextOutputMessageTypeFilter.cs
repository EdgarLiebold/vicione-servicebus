using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Converts an inbound context type to a pipe context type post-dispatch
/// </summary>
/// <typeparam name="TMessage">The subsequent pipe context type</typeparam>
public class ConsumeContextOutputMessageTypeFilter<TMessage> :
    IConsumeContextOutputMessageTypeFilter<TMessage>
    where TMessage : class
{
    readonly ConsumeObservable _consumeObservers;
    readonly ConsumeMessageObservable<TMessage> _observers;
    readonly IRequestIdTeeFilter<TMessage> _output;

    public ConsumeContextOutputMessageTypeFilter(ConsumeObservable observers, IRequestIdTeeFilter<TMessage> output)
    {
        _output = output;

        _consumeObservers = observers;
        _observers = new ConsumeMessageObservable<TMessage>();
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("dispatchPipe");
        scope.Add("outputType", TypeCache<ConsumeContext<TMessage>>.ShortName);

        _output.Probe(scope);
    }

    public Task SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        return context.TryGetMessage(out ConsumeContext<TMessage>? pipeContext)
            ? SendToOutputAsync(next, pipeContext)
            : next.SendAsync(context);
    }

    public ConnectHandle ConnectConsumeMessageObserver(IConsumeMessageObserver<TMessage> observer)
    {
        return _observers.Connect(observer);
    }

    public ConnectHandle ConnectPipe(IPipe<ConsumeContext<TMessage>> pipe)
    {
        return _output.ConnectPipe(pipe);
    }

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
            var typedNext = Pipe.Execute<ConsumeContext<TMessage>>(messageContext => next.SendAsync(messageContext.Advanced()));

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
            if (_observers.Count > 0)
            {
                var consumeFaultTask = _observers.ConsumeFaultAsync(pipeContext, ex);
                if (consumeFaultTask.Status != TaskStatus.RanToCompletion)
                    await consumeFaultTask.ConfigureAwait(false);
            }

            if (_consumeObservers.Count > 0)
            {
                var consumeFaultTask = _consumeObservers.ConsumeFaultAsync(pipeContext, ex);
                if (consumeFaultTask.Status != TaskStatus.RanToCompletion)
                    await consumeFaultTask.ConfigureAwait(false);
            }

            throw;
        }
    }
}
