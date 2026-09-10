using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus;
using ViciOneServiceBusBenchmark.BusOutbox;

namespace ViciOneServiceBusBenchmark.Latency;

public class SqlMessageLatencyTransport :
    IMessageLatencyTransport
{
    readonly SqlOptionSet _options;
    readonly IMessageLatencySettings _settings;
    ServiceProvider _provider;
    AsyncServiceScope _scope;
    Uri _targetAddress;
    ISendEndpoint _targetEndpoint;
    readonly string _runPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    public SqlMessageLatencyTransport(SqlOptionSet options, IMessageLatencySettings settings)
    {
        _options = options;
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
            .AddPostgreSqlMigrationHostedService(true, true)
            .AddViciOneServiceBus(x =>
            {
                x.Services.AddOptions<SqlTransportOptions>().Configure(options =>
                {
                    options.Host = _options.Host;
                    options.Database = _options.Database;
                    options.Schema = _options.Schema;
                    options.Role = _options.Role;
                    options.Username = "benchmark";
                    options.Password = _runPassword;
                    options.AdminUsername = _options.Username;
                    options.AdminPassword = _options.ResolveAdminPassword();
                });

                x.AddConsumer<MessageLatencyConsumer>();

                x.UsingPostgreSql((context, cfg) =>
                {
                    cfg.ReceiveEndpoint("latency_consumer" + (_settings.Durable ? "" : "_express"), e =>
                    {
                        e.PurgeOnStartup = true;
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
