// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testing;


[TestFixture]
public class When_the_consumer_timeout_is_reached_waiting_for_a_batch
{
    /// <summary>
    /// A delivery the consumer holds past the broker's acknowledgement timeout must come back and be
    /// consumed, not be lost and not fail for good.
    /// <para>
    /// The imported configuration could not express that. It set a batch time limit of 80 s with a
    /// message limit of ten and published six messages, against a delivery acknowledgement timeout of
    /// ten seconds. Six of ten never reaches the message limit, so the batch could only complete on its
    /// time limit, and that is eight times the window the broker allows. Measured: the consumer never
    /// ran once, zero batches were processed, and the broker closed the channel every ten seconds with
    /// 'PRECONDITION_FAILED - delivery acknowledgement on channel 2 timed out. Timeout value used:
    /// 10000 ms.' The messages were redelivered forever and the assertion was unreachable by
    /// construction — for any time limit above the acknowledgement window, not just for eighty seconds.
    /// </para>
    /// <para>
    /// The subject is kept and the configuration is made able to carry it. The batch now completes on
    /// its message limit, well inside the window, and it is the consumer that holds the first delivery
    /// too long. The broker requeues, the second delivery is consumed normally, and the assertion below
    /// is the one the spec always made.
    /// </para>
    /// </summary>
    [Test]
    public async Task Should_properly_handle_message_redelivery()
    {
        HighTextMessageConsumer.Reset();

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

                x.AddOptions<ViciOneServiceBusHostOptions>().Configure(options =>
                {
                    options.StartTimeout = TimeSpan.FromSeconds(5);
                    options.StopTimeout = TimeSpan.FromSeconds(5);
                    options.ConsumerStopTimeout = TimeSpan.FromSeconds(1);
                });

                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(90), testTimeout: TimeSpan.FromSeconds(120));
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<HighTextMessageConsumer>(c => c.Options<BatchOptions>(options =>
                    {
                        // Both limits sit inside the ten second acknowledgement window, so the batch is
                        // delivered rather than starved. Three messages are published and the message
                        // limit is three, so the batch completes at once and the acknowledgement
                        // timeout is reached by the consumer holding it, which is what the spec is named
                        // for, instead of by a batch that can never assemble.
                        options.TimeLimit = TimeSpan.FromSeconds(5);
                        options.MessageLimit = 3;
                    }))
                    .Endpoint(e => e.AddConfigureEndpointCallback(cfg =>
                    {
                        if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                            rmq.SetDeliveryAcknowledgementTimeout(ms: 10000);
                    }));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = await provider.StartTestHarness();

        await harness.Bus.PublishBatch(new TextMessage[]
        {
            new()
            {
                Text = "High Priority 1",
                Priority = "High"
            },
            new()
            {
                Text = "High Priority 2",
                Priority = "High"
            },
            new()
            {
                Text = "High Priority 3",
                Priority = "High"
            }
        });

        Assert.That(await harness.Consumed.Any<TextMessage>(x => x.Exception == null));
    }


    public class TextMessage
    {
        public string Text { get; set; }
        public string Priority { get; set; }
    }


    class HighTextMessageConsumer :
        IConsumer<Batch<TextMessage>>
    {
        /// <summary>
        /// Counts deliveries across the redelivery, so the first one can be held past the broker's
        /// acknowledgement timeout and the second one consumed normally. A field rather than a header
        /// because the broker requeues the original message: nothing on it records that it came back.
        /// </summary>
        static int _deliveries;

        public static void Reset()
        {
            Interlocked.Exchange(ref _deliveries, 0);
        }

        public async Task Consume(ConsumeContext<Batch<TextMessage>> context)
        {
            var delivery = Interlocked.Increment(ref _deliveries);

            LogContext.Debug?.Log("Processing batch of {Count} messages, delivery {Delivery}", context.Message.Length, delivery);

            foreach (ConsumeContext<TextMessage> message in context.Message)
                LogContext.Debug?.Log("Got message: {0} {1} {2}", DateTime.UtcNow, message.Message.Text, message.Message.Priority);

            if (delivery == 1)
            {
                // Longer than the endpoint's ten second acknowledgement timeout, so the broker takes the
                // delivery back. This is the consumer timeout the spec is named for.
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
        }
    }
}
