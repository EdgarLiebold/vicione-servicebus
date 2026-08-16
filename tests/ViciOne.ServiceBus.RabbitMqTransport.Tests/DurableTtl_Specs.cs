namespace ViciOne.ServiceBus.RabbitMqTransport.Tests;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;


[TestFixture]
public class When_configuring_a_durable_ttl_queue
{
    [Test]
    public async Task Should_comply_with_new_broker_rules()
    {
        await using var provider = new ServiceCollection()
            .ConfigureRabbitMqTestOptions(options =>
            {
                options.CleanVirtualHost = true;
                options.CreateVirtualHostIfNotExists = true;
            })
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddOptions<RabbitMqTransportOptions>()
                    .Configure(options =>
                    {
                        options.VHost = "test";
                        options.ApplyRunScopedCredentials();
                    });

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.AutoStart = true;

                    cfg.ReceiveEndpoint("durable-ttl", e =>
                    {
                        e.Durable = true;
                        e.QueueExpiration = TimeSpan.FromMinutes(90);
                    });
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var harness = await provider.StartTestHarness();
    }
}
