namespace ViciOneServiceBusBenchmark.Latency
{
    using System;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus;


    /// <summary>
    /// Measures elapsed time from local send initiation to transport-specific send-task completion and consumer
    /// observation. Send-task completion does not universally mean that a broker durably acknowledged a message.
    /// </summary>
    public class MessageLatencyBenchmark
    {
        readonly string _payload;
        readonly IMessageLatencySettings _settings;
        readonly IMessageLatencyTransport _transport;
        MessageMetricCapture _capture;
        TimeSpan _consumeDuration;
        TimeSpan _sendDuration;

        public MessageLatencyBenchmark(IMessageLatencyTransport transport, IMessageLatencySettings settings)
        {
            _transport = transport;
            _settings = settings;

            if (settings.MessageCount / settings.Clients * settings.Clients != settings.MessageCount)
                throw new ArgumentException("The clients must be a factor of message count");

            _payload = _settings.PayloadSize > 0 ? new string('*', _settings.PayloadSize) : null;
        }

        public async Task Run()
        {
            _capture = new MessageMetricCapture(_settings.MessageCount);

            IReportConsumerMetric report = _capture;

            await _transport.Start(ConfigureReceiveEndpoint, report);
            try
            {
                Console.WriteLine("Running Message Latency Benchmark");

                await RunBenchmark();

                Console.WriteLine("Message Count: {0}", _settings.MessageCount);
                Console.WriteLine("Clients: {0}", _settings.Clients);
                Console.WriteLine("Durable: {0}", _settings.Durable);
                Console.WriteLine("Payload Length: {0}", _payload?.Length ?? 0);
                Console.WriteLine("Prefetch Count: {0}", _settings.PrefetchCount);
                Console.WriteLine("Concurrency Limit: {0}", _settings.ConcurrencyLimit);

                Console.WriteLine("Total send duration: {0:g}", _sendDuration);
                Console.WriteLine("Send message rate: {0:F2} (msg/s)",
                    _settings.MessageCount * 1000 / _sendDuration.TotalMilliseconds);
                Console.WriteLine("Total consume duration: {0:g}", _consumeDuration);
                Console.WriteLine("Consume message rate: {0:F2} (msg/s)",
                    _settings.MessageCount * 1000 / _consumeDuration.TotalMilliseconds);
                Console.WriteLine("Concurrent Consumer Count: {0}", MessageLatencyConsumer.MaxConsumerCount);

                MessageMetric[] messageMetrics = _capture.GetMessageMetrics();

                BenchmarkReporting.WriteLatencySummary("send completion latency", messageMetrics,
                    x => x.SendCompletionLatency);
                BenchmarkReporting.WriteLatencySummary("end-to-end consume latency", messageMetrics,
                    x => x.ConsumeLatency);

                Console.WriteLine();
                BenchmarkReporting.WriteHistogram("end-to-end consume latency", messageMetrics, x => x.ConsumeLatency);
            }
            finally
            {
                await _transport.DisposeAsync();
            }
        }

        async Task RunBenchmark()
        {
            await Task.Yield();

            var stripes = new Task[_settings.Clients];

            var messageCount = _settings.MessageCount / _settings.Clients;
            if (messageCount > int.MaxValue)
                throw new IndexOutOfRangeException("Too many messages");

            for (var i = 0; i < _settings.Clients; i++)
            {
                stripes[i] = Task.Run(() => RunStripe((int)messageCount));
            }

            await Task.WhenAll(stripes).ConfigureAwait(false);

            _sendDuration = await _capture.SendCompleted.ConfigureAwait(false);
            _consumeDuration = await _capture.ConsumeCompleted.ConfigureAwait(false);
        }

        async Task RunStripe(int messageCount)
        {
            await Task.Yield();

            NewId[] ids = NewId.Next(messageCount);

            for (long i = 0; i < messageCount; i++)
            {
                var messageId = ids[i].ToGuid();
                await _capture.Sent(messageId, () => _transport.Send(new LatencyTestMessage(messageId, _payload)))
                    .ConfigureAwait(false);
            }
        }

        void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
        {
            if (_settings.ConcurrencyLimit > 0)
                configurator.UseConcurrencyLimit(_settings.ConcurrencyLimit);

            configurator.Consumer(() => new MessageLatencyConsumer(_capture));
        }
    }
}
