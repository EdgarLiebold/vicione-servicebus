namespace ViciOne.ServiceBus.SqlTransport.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;


/// <summary>
/// The delivery count limit, engaged and measured on both engines.
/// <para>
/// The imported fixture asserted that a message is not consumed after the limit but never set the limit:
/// <c>MaxDeliveryCount</c> is public on <see cref="ISqlQueueConfigurator"/>, which
/// <see cref="ISqlReceiveEndpointConfigurator"/> inherits, and nothing there touched it, so the queue kept
/// the migrator default of ten. Its consumer also succeeded, so exactly one delivery was the correct
/// outcome and both engines measured exactly that: expected 0, but was 1.
/// </para>
/// <para>
/// The limit is set here and read back from the queue the migrator created, so the configuration is shown
/// to have arrived. A delivery is then driven to that limit, which is the boundary the limit exists for,
/// and the transport must neither hand it to a consumer again nor leave it in the queue.
/// </para>
/// </summary>
[TestFixture(typeof(PostgresDatabaseTestConfiguration))]
[TestFixture(typeof(SqlServerDatabaseTestConfiguration))]
public class Using_message_delivery_count_limit<T>
    where T : IDatabaseTestConfiguration, new()
{
    const int MaxDeliveryCount = 3;

    /// <summary>
    /// A fresh queue per fixture: the transport database outlives a run, so a queue that keeps its name
    /// carries the dead letters of every earlier one and no exact count could be asserted.
    /// </summary>
    readonly string _queue = $"delivery-count-limit-queue-{NewId.Next().ToString("N")}";

    [Test]
    public async Task Should_carry_the_configured_limit_into_the_queue()
    {
        var dialect = TransportInspection.DialectOf(_configuration);

        await using var provider = BuildProvider();

        var harness = await provider.StartTestHarness();

        await using var connection = await provider.OpenTransport(dialect);

        Assert.That(await connection.QueueMaxDeliveryCount(dialect, TransportSchema.Name, _queue),
            Is.EqualTo(MaxDeliveryCount),
            "the queue carries a different delivery count limit than the endpoint configured, so the setting never arrived");

        await harness.Stop();
    }

    [Test]
    public async Task Should_not_consume_the_message_after_the_limit()
    {
        var dialect = TransportInspection.DialectOf(_configuration);

        LimitedConsumer.Reset();

        await using var provider = BuildProvider();

        var harness = await provider.StartTestHarness();

        var endpoint = await harness.Bus.GetSendEndpoint(new Uri($"queue:{_queue}"));
        await endpoint.Send(new ExpiringMessage(NewId.NextGuid()));

        await harness.Consumed.Any<ExpiringMessage>();
        await harness.Stop();

        var consumedBefore = LimitedConsumer.Consumed;

        // A second message, put into the queue by a bus that only sends, so nothing takes it out again
        // before its delivery count has been driven to the limit.
        await using (var sender = BuildSender())
        {
            var senderHarness = await sender.StartTestHarness();

            var senderEndpoint = await senderHarness.Bus.GetSendEndpoint(new Uri($"queue:{_queue}"));
            await senderEndpoint.Send(new ExpiringMessage(NewId.NextGuid()));

            await senderHarness.Stop();
        }

        await using (var connection = await provider.OpenTransport(dialect))
        {
            var exhausted = await connection.ExhaustDeliveryAttempts(dialect, TransportSchema.Name, _queue);

            Assert.That(exhausted, Is.EqualTo(1),
                "the fixture did not reach a delivery to drive to the limit, so nothing below is being measured");
        }

        await harness.Start();

        // Inactivity, not a fixed wait: the endpoint reports that nothing is arriving any more.
        await harness.InactivityTask;

        await using var after = await provider.OpenTransport(dialect);

        var queued = await after.DeliveryCount(dialect, TransportSchema.Name, _queue, 1);
        var deadLettered = await after.DeliveryCount(dialect, TransportSchema.Name, _queue, 3);

        Assert.Multiple(() =>
        {
            Assert.That(LimitedConsumer.Consumed, Is.EqualTo(consumedBefore),
                "the consumer was given a message whose delivery count had already reached the limit");
            Assert.That(queued, Is.Zero, "the exhausted delivery is still in the queue instead of being dead lettered");
            Assert.That(deadLettered, Is.EqualTo(1), "the exhausted delivery did not reach the dead letter queue");
        });

        await harness.Stop();
    }

    ServiceProvider BuildSender()
    {
        return _configuration.Create()
            .AddViciOneServiceBusTestHarness(x =>
            {
                _configuration.Configure(x, (_, _) =>
                {
                });
            })
            .BuildServiceProvider(true);
    }

    ServiceProvider BuildProvider()
    {
        return _configuration.Create()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(5), testTimeout: TimeSpan.FromSeconds(60));
                x.AddConsumer<LimitedConsumer>();

                _configuration.Configure(x, (context, cfg) =>
                {
                    cfg.ReceiveEndpoint(_queue, e =>
                    {
                        e.MaxDeliveryCount = MaxDeliveryCount;
                        e.PollingInterval = TimeSpan.FromMilliseconds(200);
                        e.ConfigureConsumeTopology = false;

                        e.ConfigureConsumer<LimitedConsumer>(context);
                    });
                });
            })
            .BuildServiceProvider(true);
    }

    readonly T _configuration;

    public Using_message_delivery_count_limit()
    {
        _configuration = new T();
    }


    public record ExpiringMessage(Guid Id);


    public class LimitedConsumer :
        IConsumer<ExpiringMessage>
    {
        static int _consumed;

        public static int Consumed => Volatile.Read(ref _consumed);

        public static void Reset()
        {
            Volatile.Write(ref _consumed, 0);
        }

        public Task Consume(ConsumeContext<ExpiringMessage> context)
        {
            Interlocked.Increment(ref _consumed);

            return Task.CompletedTask;
        }
    }
}
