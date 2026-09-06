using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Dispatches an inbound pipe to one or more output pipes based on a dispatch
/// type.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
public class DynamicFilter<TInput> :
    IDynamicFilter<TInput>
    where TInput : class, PipeContext
{
    readonly IPipe<TInput> _empty;
    readonly Dictionary<Type, IOutputFilter> _outputPipes;
    /// <summary>Exposes the converter factory used by the containing type.</summary>
    protected readonly IPipeContextConverterFactory<TInput> ConverterFactory;
    /// <summary>Exposes the observers used by the containing type.</summary>
    protected readonly FilterObservable Observers;

    IOutputFilter[] _outputPipeArray;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converterFactory">The converter factory.</param>
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

    /// <summary>Connects pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
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
        foreach (var pipe in _outputPipes.Values)
            pipe.Probe(context);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync(TInput context, IPipe<TInput> next)
    {
        IOutputFilter[] outputPipes = _outputPipeArray;

        if (outputPipes.Length == 0)
            return Task.CompletedTask;

        if (outputPipes.Length == 1)
            return outputPipes[0].SendAsync(context, next);

        async Task SendAsync()
        {
            var outputTasks = new List<Task>(outputPipes.Length);
            for (var i = 0; i < outputPipes.Length; i++)
            {
                var outputTask = outputPipes[i].SendAsync(context, _empty);
                if (outputTask.Status == TaskStatus.RanToCompletion)
                    continue;

                outputTasks.Add(outputTask);
            }

            await Task.WhenAll(outputTasks).ConfigureAwait(false);
            await next.SendAsync(context).ConfigureAwait(false);
        }

        return SendAsync();
    }

    /// <summary>Gets pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <returns>The pipe.</returns>
    protected TResult GetPipe<T, TResult>()
        where T : class, PipeContext
        where TResult : class
    {
        return GetPipe<T>().As<TResult>();
    }

    /// <summary>Gets pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The pipe.</returns>
    protected IOutputFilter GetPipe<T>()
        where T : class, PipeContext
    {
        lock (_outputPipes)
        {
            if (_outputPipes.TryGetValue(typeof(T), out var outputPipe))
                return outputPipe;

            outputPipe = CreateOutputPipe<T>();

            _outputPipes.Add(typeof(T), outputPipe);

            _outputPipeArray = _outputPipes.Values.ToArray();

            return outputPipe;
        }
    }

    /// <summary>Creates output pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created output pipe.</returns>
    protected virtual IOutputFilter CreateOutputPipe<T>()
        where T : class, PipeContext
    {
        EnsureCompatibleOutputType<T>();

        IPipeContextConverter<TInput, T> converter = ConverterFactory.GetConverter<T>()
            ?? throw new InvalidOperationException($"The converter factory returned null for output context type {TypeCache<T>.ShortName}.");

        return (IOutputFilter)(Activator.CreateInstance(typeof(OutputFilter<>).MakeGenericType(typeof(TInput), typeof(T)), Observers, converter)
            ?? throw new InvalidOperationException($"The output filter could not be created for context type {TypeCache<T>.ShortName}."));
    }

    /// <summary>Ensures compatible output type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
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


    /// <summary>Processes output pipeline stages.</summary>
    protected interface IOutputFilter :
        IFilter<TInput>,
        IFilterObserverConnector
    {
        /// <summary>Projects the current value as the requested type.</summary>
        /// <typeparam name="TResult">The result produced by the operation.</typeparam>
        /// <returns>The t result produced by the operation.</returns>
        TResult As<TResult>()
            where TResult : class;
    }


    /// <summary>Processes output pipeline stages.</summary>
    /// <typeparam name="TOutput">The output type.</typeparam>
    protected class OutputFilter<TOutput> :
        IOutputFilter
        where TOutput : class, TInput
    {
        /// <summary>Exposes the context converter used by the containing type.</summary>
        protected readonly IPipeContextConverter<TInput, TOutput> ContextConverter;
        /// <summary>Exposes the observers used by the containing type.</summary>
        protected readonly FilterObservable Observers;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="observers">The observers.</param>
        /// <param name="contextConverter">The context converter.</param>
        public OutputFilter(FilterObservable observers, IPipeContextConverter<TInput, TOutput> contextConverter)
        {
            ContextConverter = contextConverter;
            Observers = observers;

            Filter = new OutputPipeFilter<TInput, TOutput>(ContextConverter, Observers, new TeeFilter<TOutput>());
        }

        /// <summary>Gets the filter.</summary>
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

        /// <summary>Connects observer.</summary>
        /// <param name="observer">The observer to connect.</param>
        /// <returns>A handle that disconnects the registration.</returns>
        public ConnectHandle ConnectObserver(IFilterObserver observer)
        {
            return Observers.Connect(observer);
        }

        /// <summary>Sends a message to the configured destination.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="next">The next pipeline stage to invoke.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SendAsync(TInput context, IPipe<TInput> next)
        {
            return Filter.SendAsync(context, next);
        }

        /// <summary>Writes diagnostic information to the probe context.</summary>
        /// <param name="context">The context associated with the operation.</param>
        public void Probe(ProbeContext context)
        {
            Filter.Probe(context);
        }
    }
}


/// <summary>Processes dynamic pipeline stages.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public class DynamicFilter<TInput, TKey> :
    DynamicFilter<TInput>,
    IDynamicFilter<TInput, TKey>
    where TInput : class, PipeContext
    where TKey : notnull
{
    readonly KeyAccessor<TInput, TKey> _keyAccessor;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converterFactory">The converter factory.</param>
    /// <param name="keyAccessor">The key accessor.</param>
    public DynamicFilter(IPipeContextConverterFactory<TInput> converterFactory, KeyAccessor<TInput, TKey> keyAccessor)
        : base(converterFactory)
    {
        _keyAccessor = keyAccessor ?? throw new ArgumentNullException(nameof(keyAccessor));
    }

    /// <summary>Connects pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPipe<T>(TKey key, IPipe<T> pipe)
        where T : class, PipeContext
    {
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        IKeyPipeConnector<TKey> pipeConnector = GetPipe<T, IKeyPipeConnector<TKey>>();

        return pipeConnector.ConnectPipe(key, pipe);
    }

    /// <summary>Creates output pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created output pipe.</returns>
    protected override IOutputFilter CreateOutputPipe<T>()
    {
        EnsureCompatibleOutputType<T>();

        var dynamicType = typeof(KeyOutputFilter<>).MakeGenericType(typeof(TInput), typeof(TKey), typeof(T));

        IPipeContextConverter<TInput, T> converter = ConverterFactory.GetConverter<T>()
            ?? throw new InvalidOperationException($"The converter factory returned null for output context type {TypeCache<T>.ShortName}.");

        return (IOutputFilter)(Activator.CreateInstance(dynamicType, Observers, converter, _keyAccessor)
            ?? throw new InvalidOperationException($"The keyed output filter could not be created for context type {TypeCache<T>.ShortName}."));
    }


    /// <summary>Processes key output pipeline stages.</summary>
    /// <typeparam name="TOutput">The output type.</typeparam>
    protected class KeyOutputFilter<TOutput> :
        OutputFilter<TOutput>
        where TOutput : class, TInput
    {
        /// <summary>Initializes a new instance.</summary>
        /// <param name="observers">The observers.</param>
        /// <param name="contextConverter">The context converter.</param>
        /// <param name="keyAccessor">The key accessor.</param>
        public KeyOutputFilter(FilterObservable observers, IPipeContextConverter<TInput, TOutput> contextConverter, KeyAccessor<TInput, TKey> keyAccessor)
            : base(observers, contextConverter)
        {
            Filter = new OutputPipeFilter<TInput, TOutput, TKey>(ContextConverter, Observers, keyAccessor);
        }

        /// <summary>Gets the filter.</summary>
        protected override IOutputPipeFilter<TInput, TOutput> Filter { get; }
    }
}
