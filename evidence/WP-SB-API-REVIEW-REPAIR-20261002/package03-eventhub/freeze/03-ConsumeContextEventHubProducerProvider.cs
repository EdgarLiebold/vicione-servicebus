using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Wraps Event Hubs producers so produced messages inherit headers from a consumed message.</summary>
public class ConsumeContextEventHubProducerProvider :
    IEventHubProducerProvider
{
    readonly ConsumeContext _consumeContext;
    readonly IEventHubProducerProvider _provider;

    /// <summary>Creates a consume-context-aware wrapper over an existing producer provider.</summary>
    /// <param name="provider">The underlying Event Hubs producer provider.</param>
    /// <param name="consumeContext">The consume context whose headers are transferred during production.</param>
    public ConsumeContextEventHubProducerProvider(IEventHubProducerProvider provider, ConsumeContext consumeContext)
    {
        _provider = provider;
        _consumeContext = consumeContext;
    }

    /// <summary>Gets a producer wrapper that transfers this provider's consume-context headers.</summary>
    /// <param name="address">The Event Hubs endpoint address.</param>
    /// <param name="cancellationToken">Cancels underlying producer resolution.</param>
    /// <returns>A task whose result is the consume-context-aware producer.</returns>
    public Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default)
    {
        Task<IEventHubProducer> producerTask = _provider.GetProducerAsync(address, cancellationToken: cancellationToken);
        IEventHubProducer producer = new Producer(producerTask, _consumeContext);
        return Task.FromResult(producer);
    }

    /// <summary>Connects a send observer to the underlying provider.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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
                var producer = await _producerTask.WaitAsync(cancellationToken).ConfigureAwait(false);
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
                var producer = await _producerTask.WaitAsync(cancellationToken).ConfigureAwait(false);
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
                var producer = await _producerTask.WaitAsync(cancellationToken).ConfigureAwait(false);
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
                var producer = await _producerTask.WaitAsync(cancellationToken).ConfigureAwait(false);
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
