using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Util;

namespace ViciOneServiceBusBenchmark.RequestResponse;

/// <summary>
/// Measures request/response completion latency and the time until the consumer reports observing the request.
/// Neither measurement is described as a transport acknowledgement.
/// </summary>
public class RequestResponseBenchmark
{
    readonly IRequestResponseSettings _settings;
    readonly IRequestResponseTransport _transport;
    MessageMetricCapture _capture;
    TimeSpan _consumeDuration;
    TimeSpan _requestDuration;

    public RequestResponseBenchmark(IRequestResponseTransport transport, IRequestResponseSettings settings)
    {
        _transport = transport;
        _settings = settings;

        if (settings.MessageCount / settings.Clients * settings.Clients != settings.MessageCount)
            throw new ArgumentException("The clients must be a factor of message count");
    }

    public void Run(CancellationToken cancellationToken = default)
    {
        _capture = new MessageMetricCapture(_settings.MessageCount);

        _transport.GetBusControl(ConfigureReceiveEndpoint);
        try
        {
            Console.WriteLine("Running Request Response Benchmark");

            TaskBlocking.Wait(RunBenchmark, cancellationToken);

            Console.WriteLine("Message Count: {0}", _settings.MessageCount);
            Console.WriteLine("Clients: {0}", _settings.Clients);
            Console.WriteLine("Durable: {0}", _settings.Durable);
            Console.WriteLine("Prefetch Count: {0}", _settings.PrefetchCount);
            Console.WriteLine("Concurrency Limit: {0}", _settings.ConcurrencyLimit);

            Console.WriteLine("Total consume duration: {0:g}", _consumeDuration);
            Console.WriteLine("Consume message rate: {0:F2} (msg/s)",
                _settings.MessageCount * 1000 / _consumeDuration.TotalMilliseconds);
            Console.WriteLine("Total request duration: {0:g}", _requestDuration);
            Console.WriteLine("Request rate: {0:F2} (msg/s)",
                _settings.MessageCount * 1000 / _requestDuration.TotalMilliseconds);
            Console.WriteLine("Concurrent Consumer Count: {0}", RequestConsumer.MaxConsumerCount);

            MessageMetric[] messageMetrics = _capture.GetMessageMetrics();

            BenchmarkReporting.WriteLatencySummary("request/response completion latency", messageMetrics,
                x => x.RequestLatency);
            BenchmarkReporting.WriteLatencySummary("consumer-reporting latency", messageMetrics,
                x => x.ConsumeLatency);

            Console.WriteLine();

            BenchmarkReporting.WriteHistogram("request/response completion latency", messageMetrics,
                x => x.RequestLatency);
        }
        finally
        {
            _transport.Dispose();
        }
    }

    async Task RunBenchmark()
    {
        await Task.Yield();

        var stripes = new Task[_settings.Clients];

        for (var i = 0; i < _settings.Clients; i++)
        {
            IRequestClient<RequestMessage> requestClient =
                await _transport.GetRequestClient<RequestMessage>(_settings.RequestTimeout).ConfigureAwait(false);

            stripes[i] = RunStripe(requestClient, _settings.MessageCount / _settings.Clients);
        }

        await Task.WhenAll(stripes).ConfigureAwait(false);

        _requestDuration = await _capture.RequestCompleted.ConfigureAwait(false);
        _consumeDuration = await _capture.ConsumeCompleted.ConfigureAwait(false);
    }

    async Task RunStripe(IRequestClient<RequestMessage> client, long messageCount)
    {
        await Task.Yield();

        for (long i = 0; i < messageCount; i++)
        {
            var messageId = NewId.NextGuid();
            await _capture.ResponseReceived(messageId,
                    () => client.GetResponse<ResponseMessage>(new RequestMessage(messageId)))
                .ConfigureAwait(false);
        }
    }

    void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        if (_settings.ConcurrencyLimit > 0)
            configurator.UseConcurrencyLimit(_settings.ConcurrencyLimit);

        configurator.Consumer(() => new RequestConsumer(_capture));
    }
}
