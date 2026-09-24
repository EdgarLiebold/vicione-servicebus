using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Converts input contexts and dispatches successful conversions through an observed output tee.</summary>
/// <typeparam name="TInput">The input-context contract.</typeparam>
/// <typeparam name="TOutput">The output-context contract extending the input contract.</typeparam>
public class OutputPipeFilter<TInput, TOutput> :
    IOutputPipeFilter<TInput, TOutput>
    where TInput : class, PipeContext
    where TOutput : class, TInput
{
    readonly IPipeContextConverter<TInput, TOutput> _contextConverter;
    readonly FilterObservable<TOutput> _observers;
    readonly FilterObservable _outerObservers;
    readonly ITeeFilter<TOutput> _output;

    /// <summary>Creates an output adapter with required conversion, shared observation and tee dispatch.</summary>
    /// <param name="contextConverter">The converter selecting contexts compatible with the output contract.</param>
    /// <param name="observers">The observers shared across output contracts.</param>
    /// <param name="outputFilter">The tee receiving converted contexts and the continuation.</param>
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
            await NotifyFaultObserversAsync(pipeContext, ex).ConfigureAwait(false);
            throw;
        }
    }

    async Task NotifyFaultObserversAsync(TOutput pipeContext, Exception dispatchFailure)
    {
        if (_observers.Count > 0)
        {
            try
            {
                var sendFaultTask = _observers.SendFaultAsync(pipeContext, dispatchFailure);
                if (sendFaultTask.Status != TaskStatus.RanToCompletion)
                    await sendFaultTask.ConfigureAwait(false);
            }
            catch (Exception observerFailure)
            {
                LogObserverFailure(observerFailure, "typed");
            }
        }

        if (_outerObservers.Count > 0)
        {
            try
            {
                var sendFaultTask = _outerObservers.SendFaultAsync(pipeContext, dispatchFailure);
                if (sendFaultTask.Status != TaskStatus.RanToCompletion)
                    await sendFaultTask.ConfigureAwait(false);
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
            LogContext.Error?.Log(observerFailure, "An output-pipe {ObserverScope} fault observer failed after dispatch faulted: {OutputType}", observerScope, typeof(TOutput));
        }
        catch
        {
            // Diagnostic logging must not replace the dispatch failure.
        }
    }
}


/// <summary>Converts input contexts and dispatches them through an observed keyed output tee.</summary>
/// <typeparam name="TInput">The input-context contract.</typeparam>
/// <typeparam name="TOutput">The output-context contract extending the input contract.</typeparam>
/// <typeparam name="TKey">The key selecting connected output pipelines.</typeparam>
public class OutputPipeFilter<TInput, TOutput, TKey> :
    OutputPipeFilter<TInput, TOutput>,
    IOutputPipeFilter<TInput, TOutput, TKey>
    where TInput : class, PipeContext
    where TOutput : class, PipeContext, TInput
    where TKey : notnull
{
    readonly ITeeFilter<TOutput, TKey> _outputFilter;

    /// <summary>Creates an output adapter with a keyed tee and shared observers.</summary>
    /// <param name="contextConverter">The converter selecting contexts compatible with the output contract.</param>
    /// <param name="observers">The observers shared across output contracts.</param>
    /// <param name="keyAccessor">The accessor selecting a key from the converted context.</param>
    public OutputPipeFilter(IPipeContextConverter<TInput, TOutput> contextConverter, FilterObservable observers, KeyAccessor<TInput, TKey> keyAccessor)
        : this(contextConverter, observers, new TeeFilter<TOutput, TKey>(keyAccessor))
    {
    }

    /// <summary>Creates an output adapter using a supplied keyed tee.</summary>
    /// <param name="contextConverter">The converter selecting contexts compatible with the output contract.</param>
    /// <param name="observers">The observers shared across output contracts.</param>
    /// <param name="outputFilter">The keyed tee receiving converted contexts and the continuation.</param>
    protected OutputPipeFilter(IPipeContextConverter<TInput, TOutput> contextConverter, FilterObservable observers, ITeeFilter<TOutput, TKey> outputFilter)
        : base(contextConverter, observers, outputFilter)
    {
        _outputFilter = outputFilter;
    }

    /// <summary>Registers a pipeline with the keyed output tee.</summary>
    /// <typeparam name="T">The pipeline context contract accepted by the keyed tee.</typeparam>
    /// <param name="key">The key selecting this pipeline.</param>
    /// <param name="pipe">The pipeline receiving matching converted contexts.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe<T>(TKey key, IPipe<T> pipe)
        where T : class, PipeContext
    {
        return _outputFilter.ConnectPipe(key, pipe);
    }
}
