using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Converts an inbound context type to a pipe context type post-dispatch
/// </summary>
/// <typeparam name="TInput">The pipe context type</typeparam>
/// <typeparam name="TOutput">The subsequent pipe context type</typeparam>
public class OutputPipeFilter<TInput, TOutput> :
    IOutputPipeFilter<TInput, TOutput>
    where TInput : class, PipeContext
    where TOutput : class, TInput
{
    readonly IPipeContextConverter<TInput, TOutput> _contextConverter;
    readonly FilterObservable<TOutput> _observers;
    readonly FilterObservable _outerObservers;
    readonly ITeeFilter<TOutput> _output;

    public OutputPipeFilter(IPipeContextConverter<TInput, TOutput> contextConverter, FilterObservable observers, ITeeFilter<TOutput> outputFilter)
    {
        _outerObservers = observers ?? throw new ArgumentNullException(nameof(observers));
        _contextConverter = contextConverter ?? throw new ArgumentNullException(nameof(contextConverter));

        _output = outputFilter ?? throw new ArgumentNullException(nameof(outputFilter));

        _observers = new FilterObservable<TOutput>();
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("dispatchPipe");
        scope.Add("outputType", TypeCache<TOutput>.ShortName);

        _output.Probe(scope);
    }

    Task IFilter<TInput>.SendAsync(TInput context, IPipe<TInput> next)
    {
        if (!_contextConverter.TryConvert(context, out var pipeContext))
            return next.SendAsync(context);

        if (pipeContext == null)
            throw new InvalidOperationException($"The context converter returned success with a null {TypeCache<TOutput>.ShortName} context.");

        return SendToOutputAsync(next, pipeContext);
    }

    ConnectHandle IFilterObserverConnector<TOutput>.ConnectObserver(IFilterObserver<TOutput> observer)
    {
        return _observers.Connect(observer);
    }

    ConnectHandle IPipeConnector<TOutput>.ConnectPipe(IPipe<TOutput> pipe)
    {
        return _output.ConnectPipe(pipe);
    }

    async Task SendToOutputAsync(IPipe<TInput> next, TOutput pipeContext)
    {
        if (_observers.Count > 0)
        {
            var preSendTask = _observers.PreSendAsync(pipeContext);
            if (preSendTask.Status != TaskStatus.RanToCompletion)
                await preSendTask.ConfigureAwait(false);
        }

        if (_outerObservers.Count > 0)
        {
            var preSendTask = _outerObservers.PreSendAsync(pipeContext);
            if (preSendTask.Status != TaskStatus.RanToCompletion)
                await preSendTask.ConfigureAwait(false);
        }

        try
        {
            await _output.SendAsync(pipeContext, next).ConfigureAwait(false);

            if (_observers.Count > 0)
            {
                var postSendTask = _observers.PostSendAsync(pipeContext);
                if (postSendTask.Status != TaskStatus.RanToCompletion)
                    await postSendTask.ConfigureAwait(false);
            }

            if (_outerObservers.Count > 0)
            {
                var postSendTask = _outerObservers.PostSendAsync(pipeContext);
                if (postSendTask.Status != TaskStatus.RanToCompletion)
                    await postSendTask.ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (_observers.Count > 0)
            {
                var sendFaultTask = _observers.SendFaultAsync(pipeContext, ex);
                if (sendFaultTask.Status != TaskStatus.RanToCompletion)
                    await sendFaultTask.ConfigureAwait(false);
            }

            if (_outerObservers.Count > 0)
            {
                var sendFaultTask = _outerObservers.SendFaultAsync(pipeContext, ex);
                if (sendFaultTask.Status != TaskStatus.RanToCompletion)
                    await sendFaultTask.ConfigureAwait(false);
            }

            throw;
        }
    }
}


public class OutputPipeFilter<TInput, TOutput, TKey> :
    OutputPipeFilter<TInput, TOutput>,
    IOutputPipeFilter<TInput, TOutput, TKey>
    where TInput : class, PipeContext
    where TOutput : class, PipeContext, TInput
    where TKey : notnull
{
    readonly ITeeFilter<TOutput, TKey> _outputFilter;

    public OutputPipeFilter(IPipeContextConverter<TInput, TOutput> contextConverter, FilterObservable observers, KeyAccessor<TInput, TKey> keyAccessor)
        : this(contextConverter, observers, new TeeFilter<TOutput, TKey>(keyAccessor))
    {
    }

    protected OutputPipeFilter(IPipeContextConverter<TInput, TOutput> contextConverter, FilterObservable observers, ITeeFilter<TOutput, TKey> outputFilter)
        : base(contextConverter, observers, outputFilter)
    {
        _outputFilter = outputFilter;
    }

    public ConnectHandle ConnectPipe<T>(TKey key, IPipe<T> pipe)
        where T : class, PipeContext
    {
        return _outputFilter.ConnectPipe(key, pipe);
    }
}
