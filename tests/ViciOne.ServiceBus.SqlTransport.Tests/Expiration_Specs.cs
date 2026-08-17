namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;


/// <summary>
/// Open point, measured and left open on purpose: this fixture asserts that the message is not
/// consumed, but it never engages the limit it is named after. The delivery count limit is public,
/// reachable as <c>MaxDeliveryCount</c> on <see cref="ISqlQueueConfigurator"/>, which
/// <see cref="ISqlReceiveEndpointConfigurator"/> inherits — and nothing here sets it, so the queue
/// keeps the migrator default of 10. The consumer also succeeds, so exactly one delivery is the
/// correct outcome, and the run measured exactly that against both engines: expected 0, but was 1.
/// <para>
/// The assurance therefore does not follow from the configuration, and reconstructing the intended
/// mechanism would be a guess. Making it green by relaxing the assurance is not allowed, so the case
/// stays out of the run with its reason written down instead of quietly inventoried.
/// </para>
/// </summary>
[Explicit]
[TestFixture(typeof(PostgresDatabaseTestConfiguration))]
[TestFixture(typeof(SqlServerDatabaseTestConfiguration))]
public class Using_message_delivery_count_limit<T>
    where T : IDatabaseTestConfiguration, new()
{
    [Test]
    public async Task Should_not_consume_the_message_after_the_limit()
    {
        await using var provider = _configuration.Create()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddConsumer<ExpiringMessageConsumer>();
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(10));

                _configuration.Configure(x, (context, cfg) =>
                {
                    cfg.UseSqlMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        await harness.Stop();

        await harness.Bus.Publish(new ExpiringMessage());


        await harness.Start();

        await Task.Delay(TimeSpan.FromSeconds(5));

        using var timeout = new CancellationTokenSource(2000);

        var count = await harness.Consumed.SelectAsync<ExpiringMessage>(timeout.Token).Count();
        Assert.That(count, Is.EqualTo(0));
    }

    readonly T _configuration;

    public Using_message_delivery_count_limit()
    {
        _configuration = new T();
    }


    public record ExpiringMessage;


    public class ExpiringMessageConsumer :
        IConsumer<ExpiringMessage>
    {
        public Task Consume(ConsumeContext<ExpiringMessage> context)
        {
            return Task.CompletedTask;
        }
    }
}
