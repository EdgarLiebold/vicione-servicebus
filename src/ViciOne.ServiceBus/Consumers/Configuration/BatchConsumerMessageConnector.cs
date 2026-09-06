using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Batching;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects batch consumer message to the service bus pipeline.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class BatchConsumerMessageConnector<TConsumer, TMessage> :
    IConsumerMessageConnector<TConsumer>
    where TConsumer : class, IConsumer<Batch<TMessage>>
    where TMessage : class
{
    /// <summary>Gets the message type.</summary>
    public Type MessageType => typeof(TMessage);

    /// <summary>Creates consumer message specification.</summary>
    /// <returns>The created consumer message specification.</returns>
    public IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification()
    {
        return new BatchConsumerMessageSpecification<TConsumer, TMessage>();
    }

    /// <summary>Connects consumer.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumer(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification)
    {
        var options = specification.Options<BatchOptions>();

        IConsumerMessageSpecification<TConsumer, Batch<TMessage>> batchMessageSpecification = specification.GetMessageSpecification<Batch<TMessage>>();

        var consumeFilter = new MethodConsumerMessageFilter<TConsumer, Batch<TMessage>>();

        IPipe<ConsumerConsumeContext<TConsumer, Batch<TMessage>>> batchConsumerPipe = batchMessageSpecification.Build(consumeFilter);

        IPipe<ConsumeContext<Batch<TMessage>>> batchMessagePipe = batchMessageSpecification.BuildMessagePipe(x =>
        {
            specification.ConfigureMessagePipe(x);

            x.UseFilter(new ConsumerMessageFilter<TConsumer, Batch<TMessage>>(consumerFactory, batchConsumerPipe));
        });

        IBatchCollector<TMessage>? collector = null;
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


    class BatchConnectHandle :
        ConnectHandle
    {
        readonly BatchConsumerFactory<TMessage> _factory;
        readonly ConnectHandle _handle;
        Task _disposeTask = null!;
        int _disconnected;

        public BatchConnectHandle(ConnectHandle handle, BatchConsumerFactory<TMessage> factory)
        {
            _handle = handle;
            _factory = factory;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) != 0)
                return;

            _handle.Disconnect();
            _disposeTask = DisposeConsumerFactoryAsync();
        }

        async Task DisposeConsumerFactoryAsync()
        {
            try
            {
                await _factory.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogContext.Error?.Log(exception, "Batch consumer factory disposal faulted");
            }
        }
    }
}
