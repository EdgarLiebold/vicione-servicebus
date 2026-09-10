using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Batching;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects individual messages to the batching pipeline for one batch consumer contract.</summary>
/// <typeparam name="TConsumer">The consumer that receives completed batches.</typeparam>
/// <typeparam name="TMessage">The message contract collected into batches.</typeparam>
internal sealed class BatchConsumerMessageConnector<TConsumer, TMessage> :
    IConsumerMessageConnector<TConsumer>
    where TConsumer : class, IConsumer<Batch<TMessage>>
    where TMessage : class
{
    /// <summary>Gets the individual message type accepted by the batching pipeline.</summary>
    public Type MessageType => typeof(TMessage);

    /// <summary>Creates the configuration state for this batch consumer and message pair.</summary>
    /// <returns>A new batch consumer message specification.</returns>
    public IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification()
    {
        return new BatchConsumerMessageSpecification<TConsumer, TMessage>();
    }

    /// <summary>Connects collection, batch delivery, and terminal cleanup to the consume pipeline.</summary>
    /// <param name="consumePipe">The pipeline connector that accepts individual messages.</param>
    /// <param name="consumerFactory">The factory that creates application batch consumers.</param>
    /// <param name="specification">The validated consumer configuration.</param>
    /// <returns>A handle that disconnects the message pipe and drains the batch collector.</returns>
    public ConnectHandle ConnectConsumer(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification)
    {
        ArgumentNullException.ThrowIfNull(consumePipe);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        ArgumentNullException.ThrowIfNull(specification);

        var options = specification.Options<BatchOptions>();

        IConsumerMessageSpecification<TConsumer, Batch<TMessage>> batchMessageSpecification = specification.GetMessageSpecification<Batch<TMessage>>();

        var consumeFilter = new MethodConsumerMessageFilter<TConsumer, Batch<TMessage>>();

        IPipe<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> batchConsumerPipe = batchMessageSpecification.Build(consumeFilter);

        IPipe<ConsumeContext<Batch<TMessage>>> batchMessagePipe = batchMessageSpecification.BuildMessagePipe(x =>
        {
            specification.ConfigureMessagePipe(x);

            x.UseFilter(new ConsumerMessageFilter<TConsumer, Batch<TMessage>>(consumerFactory, batchConsumerPipe));
        });

        IBatchCollector<TMessage> collector;
        if (options.GroupKeyProvider == null)
            collector = new BatchCollector<TMessage>(options, batchMessagePipe);
        else
        {
            if (options.GroupKeyProvider.GetType().TryGetSingleClosedGenericArguments(typeof(IGroupKeyProvider<,>), out Type[] types))
            {
                var collectorType = typeof(BatchCollector<,>).MakeGenericType(typeof(TMessage), types[1]);
                collector = (IBatchCollector<TMessage>)(Activator.CreateInstance(collectorType,
                    options, batchMessagePipe, options.GroupKeyProvider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
            }
            else
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Batch Consumer Message Connector", "unknown", "The GroupKeyProvider does not implement IGroupKeyProvider<TMessage,TKey>", "Correct the named configuration before starting the host"));
        }

        var factory = new BatchConsumerFactory<TMessage>(options, collector);

        IConsumerSpecification<BatchConsumer<TMessage>> messageConsumerSpecification =
            ConsumerConnectorCache<BatchConsumer<TMessage>>.Connector.CreateConsumerSpecification<BatchConsumer<TMessage>>();

        IConsumerMessageSpecification<BatchConsumer<TMessage>, TMessage> messageSpecification =
            messageConsumerSpecification.GetMessageSpecification<TMessage>();

        IPipe<ConsumerConsumeContext<BatchConsumer<TMessage>, TMessage>> consumerPipe =
            messageSpecification.Build(new MethodConsumerMessageFilter<BatchConsumer<TMessage>, TMessage>());

        IPipe<ConsumeContext<TMessage>> messagePipe = messageSpecification.BuildMessagePipe(x =>
        {
            x.UseFilter(new ConsumerMessageFilter<BatchConsumer<TMessage>, TMessage>(factory, consumerPipe));
        });

        var handle = consumePipe.ConnectConsumePipe(messagePipe);

        return new BatchConnectHandle(handle, factory);
    }


    sealed class BatchConnectHandle :
        ConnectHandle
    {
        readonly BatchConsumerFactory<TMessage> _factory;
        readonly ConnectHandle _handle;
        readonly object _lock = new();
        Task? _disposeTask;
        int _observationStarted;

        public BatchConnectHandle(ConnectHandle handle, BatchConsumerFactory<TMessage> factory)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public void Dispose()
        {
            Disconnect();
        }

        public void Disconnect()
        {
            Task cleanup = BeginDisconnectAsync();
            if (Interlocked.Exchange(ref _observationStarted, 1) == 0)
                _ = ObserveCleanupFailureAsync(cleanup);
        }

        public ValueTask DisposeAsync() => new(BeginDisconnectAsync());

        Task BeginDisconnectAsync()
        {
            lock (_lock)
            {
                if (_disposeTask != null)
                    return _disposeTask;

                Exception? disconnectFailure = null;
                try
                {
                    _handle.Disconnect();
                }
                catch (Exception exception)
                {
                    disconnectFailure = exception;
                }

                Task factoryCleanup;
                try
                {
                    factoryCleanup = _factory.DisposeAsync().AsTask();
                }
                catch (Exception exception)
                {
                    factoryCleanup = Task.FromException(exception);
                }

                _disposeTask = CompleteDisconnectAsync(disconnectFailure, factoryCleanup);
                return _disposeTask;
            }
        }

        static async Task CompleteDisconnectAsync(Exception? disconnectFailure, Task factoryCleanup)
        {
            Exception? cleanupFailure = null;
            try
            {
                await factoryCleanup.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                cleanupFailure = exception;
            }

            if (disconnectFailure != null && cleanupFailure != null)
                throw new AggregateException("Batch consumer disconnection encountered multiple failures.", disconnectFailure, cleanupFailure);
            if (disconnectFailure != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(disconnectFailure).Throw();
            if (cleanupFailure != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
        }

        static async Task ObserveCleanupFailureAsync(Task cleanup)
        {
            try
            {
                await cleanup.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogContext.Error?.Log(exception, "Batch consumer factory disposal faulted");
            }
        }
    }
}
