using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Dispatches an input context to the registered output-context pipelines.
/// </summary>
/// <typeparam name="TInput">The context contract shared by the output pipelines.</typeparam>
public class DynamicFilter<TInput> :
    IDynamicFilter<TInput>
    where TInput : class, PipeContext
{
    readonly IPipe<TInput> _empty;
    readonly Dictionary<Type, IOutputFilter> _outputPipes;
    /// <summary>The factory supplying converters for registered output-context contracts.</summary>
    protected readonly IPipeContextConverterFactory<TInput> ConverterFactory;
    /// <summary>The observers notified across all output-context contracts.</summary>
    protected readonly FilterObservable Observers;

    IOutputFilter[] _outputPipeArray;

    /// <summary>Creates an empty dispatcher with the required context-converter factory.</summary>
    /// <param name="converterFactory">The factory supplying a converter for each connected output-context contract.</param>
    public DynamicFilter(IPipeContextConverterFactory<TInput> converterFactory)
    {
        ConverterFactory = converterFactory ?? throw new ArgumentNullException(nameof(converterFactory));

        _outputPipes = new Dictionary<Type, IOutputFilter>();
        _outputPipeArray = [];

        Observers = new FilterObservable();
        _empty = Pipe.Empty<TInput>();
    }

    ConnectHandle IFilterObserverConnector.ConnectObserver<T>(IFilterObserver<T> observer)
    {
        return GetPipe<T>().ConnectObserver(observer);
    }

    ConnectHandle IFilterObserverConnector.ConnectObserver(IFilterObserver observer)
    {
        return Observers.Connect(observer);
    }

    /// <summary>Registers a pipeline for a compatible output-context contract.</summary>
    /// <typeparam name="T">The output-context contract extending the input contract.</typeparam>
    /// <param name="pipe">The required pipeline receiving successfully converted contexts.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe<T>(IPipe<T> pipe)
        where T : class, PipeContext
    {
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        IPipeConnector<T> pipeConnector = GetPipe<T, IPipeConnector<T>>();

        return pipeConnector.ConnectPipe(pipe);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        foreach (IOutputFilter pipe in Volatile.Read(ref _outputPipeArray))
            pipe.Probe(context);
    }

    /// <summary>Dispatches the context through the current output-pipeline array.</summary>
    /// <param name="context">The input context offered to each output pipeline.</param>
    /// <param name="next">The continuation passed directly to one output, or invoked after multiple outputs succeed.</param>
    /// <returns>A completed task for no outputs, one output's task, or the task awaiting all outputs and the continuation.</returns>
    /// <remarks>
    /// With no registered outputs, dispatch completes without invoking the continuation. Multiple outputs receive an
    /// empty continuation. Every registered output is visited and every started task is awaited before a failure
    /// escapes; the continuation runs only when all outputs succeed.
    /// </remarks>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync(TInput context, IPipe<TInput> next)
    {
        IOutputFilter[] outputPipes = Volatile.Read(ref _outputPipeArray);

        if (outputPipes.Length == 0)
            return Task.CompletedTask;

        if (outputPipes.Length == 1)
            return InvokeOutput(outputPipes[0], next);

        async Task SendAsync()
        {
            var outputTasks = new List<Task>(outputPipes.Length);
            for (var i = 0; i < outputPipes.Length; i++)
            {
                Task outputTask = InvokeOutput(outputPipes[i], _empty);

                if (outputTask.Status == TaskStatus.RanToCompletion)
                    continue;

                outputTasks.Add(outputTask);
            }

            await Task.WhenAll(outputTasks).ConfigureAwait(false);
            await next.SendAsync(context).ConfigureAwait(false);
        }

        Task InvokeOutput(IOutputFilter outputPipe, IPipe<TInput> continuation)
        {
            try
            {
                return outputPipe.SendAsync(context, continuation)
                    ?? Task.FromException(new InvalidOperationException("An output filter returned a null task."));
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        return SendAsync();
    }

    /// <summary>Gets or creates the output filter and requests its underlying connector contract.</summary>
    /// <typeparam name="T">The output-context contract extending the input contract.</typeparam>
    /// <typeparam name="TResult">The connector contract required from the output filter.</typeparam>
    /// <returns>The underlying filter implementing the requested connector contract.</returns>
    protected TResult GetPipe<T, TResult>()
        where T : class, PipeContext
        where TResult : class
    {
        return GetPipe<T>().As<TResult>();
    }

    /// <summary>Gets or creates one output filter per output-context contract under the registration lock.</summary>
    /// <typeparam name="T">The output-context contract extending the input contract.</typeparam>
    /// <returns>The existing or newly registered output filter.</returns>
    protected IOutputFilter GetPipe<T>()
        where T : class, PipeContext
    {
        lock (_outputPipes)
        {
            if (_outputPipes.TryGetValue(typeof(T), out var outputPipe))
                return outputPipe;

            outputPipe = CreateOutputPipe<T>();

            _outputPipes.Add(typeof(T), outputPipe);

            Volatile.Write(ref _outputPipeArray, _outputPipes.Values.ToArray());

            return outputPipe;
        }
    }

    /// <summary>Creates a compatible output filter using the context-converter factory.</summary>
    /// <typeparam name="T">The output-context contract extending the input contract.</typeparam>
    /// <returns>An output filter that converts contexts and dispatches them to connected pipelines.</returns>
    protected virtual IOutputFilter CreateOutputPipe<T>()
        where T : class, PipeContext
    {
        EnsureCompatibleOutputType<T>();

        IPipeContextConverter<TInput, T> converter = ConverterFactory.GetConverter<T>()
            ?? throw new InvalidOperationException($"The converter factory returned null for output context type {TypeCache<T>.ShortName}.");

        return (IOutputFilter)(Activator.CreateInstance(typeof(OutputFilter<>).MakeGenericType(typeof(TInput), typeof(T)), Observers, converter)
            ?? throw new InvalidOperationException($"The output filter could not be created for context type {TypeCache<T>.ShortName}."));
    }

    /// <summary>Rejects an output-context contract that does not extend the input-context contract.</summary>
    /// <typeparam name="T">The output-context contract to check.</typeparam>
    protected static void EnsureCompatibleOutputType<T>()
        where T : class, PipeContext
    {
        if (!typeof(TInput).IsAssignableFrom(typeof(T)))
        {
            throw new ArgumentException(
                $"The output context type {TypeCache<T>.ShortName} must implement {TypeCache<TInput>.ShortName}.",
                nameof(T));
        }
    }


    /// <summary>Combines input-context dispatch, observer registration and access to an underlying connector.</summary>
    protected interface IOutputFilter :
        IFilter<TInput>,
        IFilterObserverConnector
    {
        /// <summary>Requests a connector contract implemented by the underlying output filter.</summary>
        /// <typeparam name="TResult">The required connector contract.</typeparam>
        /// <returns>The underlying filter implementing the requested contract.</returns>
        TResult As<TResult>()
            where TResult : class;
    }


    /// <summary>Adapts one output-context pipeline to input-context dispatch and observer registration.</summary>
    /// <typeparam name="TOutput">The output-context contract extending the input contract.</typeparam>
    protected class OutputFilter<TOutput> :
        IOutputFilter
        where TOutput : class, TInput
    {
        /// <summary>The converter producing the output context from the input context.</summary>
        protected readonly IPipeContextConverter<TInput, TOutput> ContextConverter;
        /// <summary>The observers shared with the containing dispatcher.</summary>
        protected readonly FilterObservable Observers;

        /// <summary>Creates an output-context filter with an unkeyed tee and shared observers.</summary>
        /// <param name="observers">The dispatcher observers notified by the output filter.</param>
        /// <param name="contextConverter">The converter producing this output-context contract.</param>
        public OutputFilter(FilterObservable observers, IPipeContextConverter<TInput, TOutput> contextConverter)
        {
            ContextConverter = contextConverter;
            Observers = observers;

            Filter = new OutputPipeFilter<TInput, TOutput>(ContextConverter, Observers, new TeeFilter<TOutput>());
        }

        /// <summary>Gets the filter responsible for conversion, observation and output-pipeline dispatch.</summary>
        protected virtual IOutputPipeFilter<TInput, TOutput> Filter { get; }

        TResult IOutputFilter.As<TResult>()
        {
            return Filter as TResult
                ?? throw new InvalidOperationException($"The output filter does not implement {TypeCache<TResult>.ShortName}.");
        }

        ConnectHandle IFilterObserverConnector.ConnectObserver<T>(IFilterObserver<T> observer)
        {
            if (Filter is IFilterObserverConnector<T> connector)
                return connector.ConnectObserver(observer);

            throw new ArgumentException($"The filter is not of the specified type: {typeof(T).Name}", nameof(observer));
        }

        /// <summary>Registers an observer shared across the dispatcher's output-context contracts.</summary>
        /// <param name="observer">The observer receiving output-filter notifications.</param>
        /// <returns>A handle that disconnects the registration.</returns>
        public ConnectHandle ConnectObserver(IFilterObserver observer)
        {
            return Observers.Connect(observer);
        }

        /// <summary>Delegates context conversion and dispatch to the underlying output filter.</summary>
        /// <param name="context">The input context offered to the converter.</param>
        /// <param name="next">The continuation supplied to the underlying output filter.</param>
        /// <returns>The task returned by the underlying output filter.</returns>
        public Task SendAsync(TInput context, IPipe<TInput> next)
        {
            return Filter.SendAsync(context, next);
        }

        /// <summary>Delegates diagnostic probing to the underlying output filter.</summary>
        /// <param name="context">The probe receiving the output-filter entries.</param>
        public void Probe(ProbeContext context)
        {
            Filter.Probe(context);
        }
    }
}


/// <summary>Dispatches input contexts to output pipelines registered for a selected key.</summary>
/// <typeparam name="TInput">The context contract shared by the output pipelines.</typeparam>
/// <typeparam name="TKey">The key selected from each converted output context through its input-context contract.</typeparam>
public class DynamicFilter<TInput, TKey> :
    DynamicFilter<TInput>,
    IDynamicFilter<TInput, TKey>
    where TInput : class, PipeContext
    where TKey : notnull
{
    readonly KeyAccessor<TInput, TKey> _keyAccessor;

    /// <summary>Creates an empty keyed dispatcher with the required converter factory and key accessor.</summary>
    /// <param name="converterFactory">The factory supplying a converter for each connected output-context contract.</param>
    /// <param name="keyAccessor">The accessor selecting a dispatch key from each converted output context.</param>
    public DynamicFilter(IPipeContextConverterFactory<TInput> converterFactory, KeyAccessor<TInput, TKey> keyAccessor)
        : base(converterFactory)
    {
        _keyAccessor = keyAccessor ?? throw new ArgumentNullException(nameof(keyAccessor));
    }

    /// <summary>Registers an output-context pipeline for a dispatch key.</summary>
    /// <typeparam name="T">The output-context contract extending the input contract.</typeparam>
    /// <param name="key">The selected key that routes contexts to this pipeline.</param>
    /// <param name="pipe">The required pipeline receiving matching converted contexts.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe<T>(TKey key, IPipe<T> pipe)
        where T : class, PipeContext
    {
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        IKeyPipeConnector<TKey> pipeConnector = GetPipe<T, IKeyPipeConnector<TKey>>();

        return pipeConnector.ConnectPipe(key, pipe);
    }

    /// <summary>Creates a compatible keyed output filter using the converter factory and key accessor.</summary>
    /// <typeparam name="T">The output-context contract extending the input contract.</typeparam>
    /// <returns>An output filter that converts contexts and dispatches them by their selected key.</returns>
    protected override IOutputFilter CreateOutputPipe<T>()
    {
        EnsureCompatibleOutputType<T>();

        var dynamicType = typeof(KeyOutputFilter<>).MakeGenericType(typeof(TInput), typeof(TKey), typeof(T));

        IPipeContextConverter<TInput, T> converter = ConverterFactory.GetConverter<T>()
            ?? throw new InvalidOperationException($"The converter factory returned null for output context type {TypeCache<T>.ShortName}.");

        return (IOutputFilter)(Activator.CreateInstance(dynamicType, Observers, converter, _keyAccessor)
            ?? throw new InvalidOperationException($"The keyed output filter could not be created for context type {TypeCache<T>.ShortName}."));
    }


    /// <summary>Adapts keyed output-context dispatch to the containing input-context dispatcher.</summary>
    /// <typeparam name="TOutput">The output-context contract extending the input contract.</typeparam>
    protected class KeyOutputFilter<TOutput> :
        OutputFilter<TOutput>
        where TOutput : class, TInput
    {
        /// <summary>Creates an output filter whose tee routes converted contexts by key.</summary>
        /// <param name="observers">The dispatcher observers notified by the output filter.</param>
        /// <param name="contextConverter">The converter producing this output-context contract.</param>
        /// <param name="keyAccessor">The accessor selecting the dispatch key from the converted output context.</param>
        public KeyOutputFilter(FilterObservable observers, IPipeContextConverter<TInput, TOutput> contextConverter, KeyAccessor<TInput, TKey> keyAccessor)
            : base(observers, contextConverter)
        {
            Filter = new OutputPipeFilter<TInput, TOutput, TKey>(ContextConverter, Observers, keyAccessor);
        }

        /// <summary>Gets the keyed filter responsible for conversion, observation and output-pipeline dispatch.</summary>
        protected override IOutputPipeFilter<TInput, TOutput> Filter { get; }
    }
}
