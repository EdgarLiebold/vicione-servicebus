// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests;

using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TestFramework.Messages;


[TestFixture]
public class TelemetryMonitor_Specs
{
    [Test]
    public async Task Should_wait_until_published_message_is_consumed()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddConsumer<PingConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        await harness.Bus.Wait(x => x.Publish(new PingMessage()));

        IPublishedMessage<PingHandled> published = await harness.Published.SelectAsync<PingHandled>().First();

        Assert.That(published, Is.Not.Null);
        Assert.That(published.Context.RequestId, Is.Null);
    }

    [Test]
    public async Task Should_wait_until_sent_message_is_consumed()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddConsumer<PingConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        var endpoint = await harness.GetConsumerEndpoint<PingConsumer>();

        await endpoint.Wait(x => x.Send(new PingMessage()));

        IPublishedMessage<PingHandled> published = await harness.Published.SelectAsync<PingHandled>().First();

        Assert.That(published, Is.Not.Null);
        Assert.That(published.Context.RequestId, Is.Null);
    }

    [Test]
    public async Task Should_wait_until_the_request_is_consumed()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddConsumer<PingConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();

        var client = harness.GetRequestClient<PingMessage>();

        Response<PongMessage> response = await client.Wait(x => x.GetResponse<PongMessage>(new PingMessage()));

        IPublishedMessage<PingHandled> published = await harness.Published.SelectAsync<PingHandled>().First();

        Assert.That(published, Is.Not.Null);
        Assert.That(published.Context.RequestId, Is.Null);
    }


    class PingHandled
    {
    }


    class PingConsumer :
        IConsumer<PingMessage>
    {
        public async Task Consume(ConsumeContext<PingMessage> context)
        {
            await context.Publish(new PingHandled());

            if (context.IsResponseAccepted<PongMessage>())
                await context.RespondAsync(new PongMessage(context.Message.CorrelationId));
        }
    }
}
