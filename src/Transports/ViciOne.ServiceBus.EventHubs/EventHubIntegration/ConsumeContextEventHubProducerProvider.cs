using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a consume context event hub producer provider implementation.
/// </summary>
public class ConsumeContextEventHubProducerProvider :
    IEventHubProducerProvider
{
    readonly ConsumeContext _consumeContext;
    readonly IEventHubProducerProvider _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="consumeContext">The consume context value.</param>
    public ConsumeContextEventHubProducerProvider(IEventHubProducerProvider provider, ConsumeContext consumeContext)
    {
        _provider = provider;
        _consumeContext = consumeContext;
    }

    /// <summary>
    /// Gets producer.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default)
    {
        Task<IEventHubProducer> producerTask = _provider.GetProducerAsync(address, cancellationToken: cancellationToken);
        IEventHubProducer producer = new Producer(producerTask, _consumeContext);
        return Task.FromResult(producer);
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _provider.ConnectSendObserver(observer);
    }


    class Producer :
        IEventHubProducer
    {
        readonly ConsumeContext _consumeContext;
        readonly Task<IEventHubProducer> _producerTask;

        public Producer(Task<IEventHubProducer> producerTask, ConsumeContext consumeContext)
        {
            _producerTask = producerTask;
            _consumeContext = consumeContext;
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            var producer = _producerTask.Status == TaskStatus.RanToCompletion
                ? _producerTask.Result
                : TaskBlocking.Wait(() => _producerTask);
            return producer.ConnectSendObserver(observer);
        }

        public Task ProduceAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            return ProduceAsync(message, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
        }

        public Task ProduceAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
            where T : class
        {
            return ProduceAsync(messages, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
        }

        public Task ProduceAsync<T>(T message, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            var sendPipeAdapter = new ConsumeSendPipeAdapter<T>(pipe, _consumeContext);

            if (_producerTask.Status == TaskStatus.RanToCompletion)
                return _producerTask.Result.ProduceAsync(message, sendPipeAdapter, cancellationToken);

            async Task ProduceAsync()
            {
                var producer = await _producerTask.ConfigureAwait(false);
                await producer.ProduceAsync(message, sendPipeAdapter, cancellationToken);
            }

            return ProduceAsync();
        }

        public Task ProduceAsync<T>(IEnumerable<T> messages, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            var sendPipeAdapter = new ConsumeSendPipeAdapter<T>(pipe, _consumeContext);

            if (_producerTask.Status == TaskStatus.RanToCompletion)
                return _producerTask.Result.ProduceAsync(messages, sendPipeAdapter, cancellationToken);

            async Task ProduceAsync()
            {
                var producer = await _producerTask.ConfigureAwait(false);
                await producer.ProduceAsync(messages, sendPipeAdapter, cancellationToken);
            }

            return ProduceAsync();
        }

        public Task ProduceAsync<T>(object values, CancellationToken cancellationToken = default)
            where T : class
        {
            return ProduceAsync(values, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
        }

        public Task ProduceAsync<T>(IEnumerable<object> values, CancellationToken cancellationToken = default)
            where T : class
        {
            return ProduceAsync(values, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
        }

        public Task ProduceAsync<T>(object values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            var sendPipeAdapter = new ConsumeSendPipeAdapter<T>(pipe, _consumeContext);

            if (_producerTask.Status == TaskStatus.RanToCompletion)
                return _producerTask.Result.ProduceAsync(values, sendPipeAdapter, cancellationToken);

            async Task ProduceAsync()
            {
                var producer = await _producerTask.ConfigureAwait(false);
                await producer.ProduceAsync(values, sendPipeAdapter, cancellationToken);
            }

            return ProduceAsync();
        }

        public Task ProduceAsync<T>(IEnumerable<object> values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
            where T : class
        {
            var sendPipeAdapter = new ConsumeSendPipeAdapter<T>(pipe, _consumeContext);

            if (_producerTask.Status == TaskStatus.RanToCompletion)
                return _producerTask.Result.ProduceAsync(values, sendPipeAdapter, cancellationToken);

            async Task ProduceAsync()
            {
                var producer = await _producerTask.ConfigureAwait(false);
                await producer.ProduceAsync(values, sendPipeAdapter, cancellationToken);
            }

            return ProduceAsync();
        }
    }


    class ConsumeSendPipeAdapter<T> :
        IPipe<EventHubSendContext<T>>,
        ISendPipe
        where T : class
    {
        readonly ConsumeContext _consumeContext;
        readonly IPipe<EventHubSendContext<T>> _pipe;

        public ConsumeSendPipeAdapter(IPipe<EventHubSendContext<T>> pipe, ConsumeContext consumeContext)
        {
            _pipe = pipe;
            _consumeContext = consumeContext;
        }

        public async Task SendAsync(EventHubSendContext<T> context)
        {
            if (_consumeContext != null)
                context.TransferConsumeContextHeaders(_consumeContext);

            if (_pipe.IsNotEmpty())
                await _pipe.SendAsync(context).ConfigureAwait(false);
        }

        public void Probe(ProbeContext context)
        {
            _pipe.Probe(context);
        }

        public async Task SendAsync<TMessage>(SendContext<TMessage> context, CancellationToken cancellationToken)
            where TMessage : class
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
