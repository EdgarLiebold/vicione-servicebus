using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Converts ConsumeContext to ConsumeContext&lt;T&gt; for a given message type
/// type.
/// </summary>
public class ConsumeContextMessageTypeFilter :
    IConsumeContextMessageTypeFilter
{
    readonly IPipe<ConsumeContext> _empty;
    readonly ConsumeObservable _observers;
    readonly Dictionary<Type, IOutputFilter> _outputPipes;

    IOutputFilter[] _outputPipeArray;

    /// <summary>Initializes a new instance.</summary>
    public ConsumeContextMessageTypeFilter()
    {
        _outputPipes = new Dictionary<Type, IOutputFilter>();
        _outputPipeArray = [];

        _empty = Pipe.Empty<ConsumeContext>();

        _observers = new ConsumeObservable();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        foreach (var pipe in _outputPipes.Values)
            pipe.Probe(context);
    }

    /// <summary>Connects message pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectMessagePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return GetMessagePipe<T>().Filter.ConnectPipe(pipe);
    }

    /// <summary>Connects message pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectMessagePipe<T>(Guid key, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return GetMessagePipe<T>().Filter.ConnectPipe(key, pipe);
    }

    /// <summary>Dispatches the consume context through the registered message pipelines and their continuation policy.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        IOutputFilter[] outputPipes = _outputPipeArray;

        if (outputPipes.Length == 0)
            return Task.CompletedTask;

        if (outputPipes.Length == 1)
            return outputPipes[0].SendAsync(context, next);

        async Task SendAsync()
        {
            var outputTasks = new List<Task>(outputPipes.Length);
            try
            {
                for (var i = 0; i < outputPipes.Length; i++)
                {
                    var outputTask = outputPipes[i].SendAsync(context, _empty);
                    if (outputTask.Status == TaskStatus.RanToCompletion)
                        continue;

                    outputTasks.Add(outputTask);
                }
            }
            catch (Exception admissionFailure)
            {
                Task joinedTask = Task.WhenAll(outputTasks);
                try
                {
                    await joinedTask.ConfigureAwait(false);
                }
                catch (Exception outputFailure)
                {
                    var failures = new List<Exception> { admissionFailure };
                    if (joinedTask.Exception is { } joinedFailures)
                    {
                        foreach (Exception failure in joinedFailures.InnerExceptions)
                        {
                            if (!ReferenceEquals(admissionFailure, failure))
                                failures.Add(failure);
                        }
                    }
                    else if (!ReferenceEquals(admissionFailure, outputFailure))
                        failures.Add(outputFailure);

                    if (failures.Count > 1)
                        throw new AggregateException(failures);
                }

                throw;
            }

            await Task.WhenAll(outputTasks).ConfigureAwait(false);
            await next.SendAsync(context).ConfigureAwait(false);
        }

        return SendAsync();
    }

    /// <summary>Connects consume message observer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return GetMessagePipe<T>().Filter.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Connects consume observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _observers.Connect(observer);
    }

    OutputFilter<T> GetMessagePipe<T>()
        where T : class
    {
        lock (_outputPipes)
        {
            if (_outputPipes.TryGetValue(typeof(T), out var outputPipe))
                return (OutputFilter<T>)outputPipe;

            OutputFilter<T> newOutputPipe = CreateOutputPipe<T>();

            _outputPipes.Add(typeof(T), newOutputPipe);

            _outputPipeArray = _outputPipes.Values.ToArray();

            return newOutputPipe;
        }
    }

    OutputFilter<T> CreateOutputPipe<T>()
        where T : class
    {
        return new OutputFilter<T>(_observers);
    }


    interface IOutputFilter :
        IFilter<ConsumeContext>
    {
    }


    class OutputFilter<TMessage> :
        IOutputFilter
        where TMessage : class
    {
        public OutputFilter(ConsumeObservable observers)
        {
            Filter = new ConsumeContextOutputMessageTypeFilter<TMessage>(observers, new RequestIdTeeFilter<TMessage>());
        }

        public virtual ConsumeContextOutputMessageTypeFilter<TMessage> Filter { get; }

        public Task SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            return Filter.SendAsync(context, next);
        }

        public void Probe(ProbeContext context)
        {
            Filter.Probe(context);
        }
    }
}
