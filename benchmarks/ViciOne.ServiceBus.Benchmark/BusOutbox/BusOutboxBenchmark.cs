using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Logging;
using ViciOneServiceBusBenchmark.Latency;

namespace ViciOneServiceBusBenchmark.BusOutbox;

/// <summary>
/// Measures elapsed time from the application bus-outbox send through the actual transport send observer and
/// consumer observation. The transport observer is not described as a universal broker durability receipt.
/// </summary>
public class BusOutboxBenchmark
{
    readonly BusOutboxBenchmarkOptions _options;
    readonly string _payload;
    readonly IConfigureBusOutboxTransport _transport;
    MessageMetricCapture _capture;
    TimeSpan _consumeDuration;
    TimeSpan _sendDuration;

    public BusOutboxBenchmark(IConfigureBusOutboxTransport transport, BusOutboxBenchmarkOptions options)
    {
        _transport = transport;
        _options = options;

        if (options.MessageCount / options.Clients * options.Clients != options.MessageCount)
            throw new ArgumentException("The clients must be a factor of message count");

        _payload = _options.PayloadSize > 0 ? new string('*', _options.PayloadSize) : null;
    }

    public async Task RunAsync()
    {
        _capture = new MessageMetricCapture(_options.MessageCount);

        await using var provider = new ServiceCollection()
            .AddTextLogger(Console.Out)
            .AddHostedService<MigrationHostedService<BusOutboxDbContext>>()
            .AddSingleton<IReportConsumerMetric>(_capture)
            .AddSingleton<ISendObserver, MetricSendObserver>()
            .AddViciOneServiceBus(x =>
            {
                x.AddConsumer<BusOutboxMessageConsumer>();

                x.SetKebabCaseEndpointNameFormatter();

                x.Services.AddDbContext<BusOutboxDbContext>(db =>
                {
                    db.UseSqlServer(_options.ResolveDatabaseConnectionString(), options =>
                    {
                        options.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                        options.MigrationsHistoryTable($"__{nameof(BusOutboxDbContext)}");

                        options.MinBatchSize(1);
                    });
                });

                x.ConfigureEntityFrameworkTransactionalStore<BusOutboxDbContext>(o =>
                {
                    o.QueryDelay = TimeSpan.FromMilliseconds(10);

                    o.EnableTransactionalOutbox();
                });

                _transport.Using(x, (context, cfg) =>
                {
                    cfg.PrefetchCount = _options.PrefetchCount;
                    cfg.ConcurrentMessageLimit = _options.ConcurrencyLimit;
                });
            })
            .BuildServiceProvider(true);

        await provider.StartHostedServicesAsync();

        try
        {
            Console.WriteLine("Running Bus Outbox Benchmark");

            await RunBenchmarkAsync(provider);

            Console.WriteLine("Message Count: {0}", _options.MessageCount);
            Console.WriteLine("Clients: {0}", _options.Clients);
            Console.WriteLine("Payload Length: {0}", _payload?.Length ?? 0);
            Console.WriteLine("Prefetch Count: {0}", _options.PrefetchCount);
            Console.WriteLine("Concurrency Limit: {0}", _options.ConcurrencyLimit);

            Console.WriteLine("Total send duration: {0:g}", _sendDuration);
            Console.WriteLine("Send message rate: {0:F2} (msg/s)",
                _options.MessageCount * 1000 / _sendDuration.TotalMilliseconds);
            Console.WriteLine("Total consume duration: {0:g}", _consumeDuration);
            Console.WriteLine("Consume message rate: {0:F2} (msg/s)",
                _options.MessageCount * 1000 / _consumeDuration.TotalMilliseconds);
            Console.WriteLine("Concurrent Consumer Count: {0}", MessageLatencyConsumer.MaxConsumerCount);

            MessageMetric[] messageMetrics = _capture.GetMessageMetrics();

            BenchmarkReporting.WriteLatencySummary("transport send-observer latency", messageMetrics,
                x => x.SendCompletionLatency);
            BenchmarkReporting.WriteLatencySummary("end-to-end consume latency", messageMetrics,
                x => x.ConsumeLatency);

            Console.WriteLine();
            BenchmarkReporting.WriteHistogram("end-to-end consume latency", messageMetrics, x => x.ConsumeLatency);
        }
        finally
        {
            await provider.StopHostedServicesAsync();
        }
    }

    async Task RunBenchmarkAsync(IServiceProvider provider)
    {
        var stripes = new Task[_options.Clients];

        for (var i = 0; i < _options.Clients; i++)
            stripes[i] = Task.Run(() => RunStripeAsync(provider, _options.MessageCount / _options.Clients));

        await Task.WhenAll(stripes).ConfigureAwait(false);

        _sendDuration = await _capture.SendCompleted.ConfigureAwait(false);

        _consumeDuration = await _capture.ConsumeCompleted.ConfigureAwait(false);
    }

    async Task RunStripeAsync(IServiceProvider provider, long messageCount)
    {
        var endpointNameFormatter = provider.GetService<IEndpointNameFormatter>() ?? DefaultEndpointNameFormatter.Instance;

        var address = new Uri($"queue:{endpointNameFormatter.Consumer<BusOutboxMessageConsumer>()}");

        for (long i = 0; i < messageCount; i++)
        {
            await using var scope = provider.CreateAsyncScope();

            await using var dbContext = scope.ServiceProvider.GetService<BusOutboxDbContext>();

            var messageId = NewId.NextGuid();
            var sendEndpoint = await scope.ServiceProvider.GetService<ISendEndpointProvider>().GetSendEndpointAsync(address);
            await _capture.SentAsync(messageId,
                () => sendEndpoint.SendAsync(new BusOutboxMessage(messageId, _payload), x => x.MessageId = messageId), true)
                .ConfigureAwait(false);

            await dbContext.SaveChangesAsync();
        }
    }


    class MetricSendObserver :
        ISendObserver
    {
        readonly IReportConsumerMetric _metric;

        public MetricSendObserver(IReportConsumerMetric metric)
        {
            _metric = metric;
        }

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            return SendMetricReporter.ReportAsync(_metric, context.MessageId);
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            return Task.CompletedTask;
        }
    }
}


public static class BenchmarkServiceCollectionExtensions
{
    public static IServiceCollection AddTextLogger(this IServiceCollection services, TextWriter textWriter)
    {
        services.AddOptions<TextWriterLoggerOptions>();
        services.TryAddSingleton<ILoggerFactory>(provider =>
            new TextWriterLoggerFactory(textWriter, provider.GetRequiredService<IOptions<TextWriterLoggerOptions>>()));
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));

        services.AddOptions<TextWriterLoggerOptions>().Configure(options =>
        {
            options.Disable("Microsoft");
            options.LogLevel = LogLevel.Information;
        });

        return services;
    }
}
