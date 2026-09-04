using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus;
using ViciOneServiceBusBenchmark.BusOutbox;

namespace ViciOneServiceBusBenchmark.Latency;

public class AmazonSqsMessageLatencyTransport :
    IMessageLatencyTransport
{
    readonly AmazonSqsHostSettings _hostSettings;
    readonly IMessageLatencySettings _settings;
    Uri _targetAddress;
    ISendEndpoint _targetEndpoint;
    ServiceProvider _provider;
    AsyncServiceScope _scope;

    public AmazonSqsMessageLatencyTransport(AmazonSqsHostSettings hostSettings, IMessageLatencySettings settings)
    {
        _hostSettings = hostSettings;
        _settings = settings;
    }

    public Task SendAsync(LatencyTestMessage message)
    {
        return _targetEndpoint.SendAsync(message);
    }

    public async Task StartAsync(Action<IReceiveEndpointConfigurator> callback, IReportConsumerMetric reportConsumerMetric)
    {
        _provider = new ServiceCollection()
            .AddTextLogger(Console.Out)
            .AddSingleton(reportConsumerMetric)
            .AddViciOneServiceBus(x =>
            {
                x.AddConsumer<MessageLatencyConsumer>();

                x.UsingAmazonSqs((context, cfg) =>
                {
                    cfg.Host(_hostSettings);

                    cfg.ReceiveEndpoint("latency_consumer" + (_settings.Durable ? "" : "_express"), e =>
                    {
                        e.Durable = _settings.Durable;
                        e.PrefetchCount = _settings.PrefetchCount;

                        if (_settings.ConcurrencyLimit > 0)
                            e.ConcurrentMessageLimit = _settings.ConcurrencyLimit;

                        callback(e);

                        _targetAddress = e.InputAddress;
                    });
                });
            })
            .BuildServiceProvider(true);

        await _provider.StartHostedServicesAsync();

        _scope = _provider.CreateAsyncScope();

        _targetEndpoint = await _scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>().GetSendEndpointAsync(_targetAddress);
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();

        await _provider.StopHostedServicesAsync();
    }
}
