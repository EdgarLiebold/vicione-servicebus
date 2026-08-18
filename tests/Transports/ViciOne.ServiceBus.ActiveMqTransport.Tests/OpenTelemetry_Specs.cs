namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using HarnessContracts;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using OpenTelemetry;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
    using Testing;


    namespace HarnessContracts
    {
        using System;


        public interface SubmitOrder
        {
            Guid OrderId { get; }
            string OrderNumber { get; }
        }


        public interface OrderSubmitted
        {
            Guid OrderId { get; }
            string OrderNumber { get; }
        }
    }


    /// <summary>
    /// Telemetry the product emits, read from the listener the fixture installs. It needs the
    /// pinned broker and nothing else: the tracing here registers a source and no exporter, so
    /// the external infrastructure this fixture was excluded for never existed.
    /// </summary>
    [TestFixture]
    public class OpenTelemetry_Specs
    {
        [Test]
        public async Task Should_report_telemetry_for_a_consumer()
        {
            var services = new ServiceCollection();
            services.AddOpenTelemetry()
                .WithTracing(t => t.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("order-api"))
                    .AddSource(DiagnosticHeaders.DefaultListenerName));

            await using var provider = services
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddConsumer<MonitoredSubmitOrderConsumer>();

                    x.UsingActiveMq((context, cfg) =>
                    {
                        // Without this the configurator keeps its built-in default and the spec
                        // connects to localhost:61616 as admin, which is not the pinned fixture.
                        cfg.ConfigureHost(ActiveMqHostAddress.ActiveMqScheme);

                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            IRequestClient<SubmitOrder> client = harness.GetRequestClient<SubmitOrder>();

            await client.GetResponse<OrderSubmitted>(new
            {
                OrderId = InVar.Id,
                OrderNumber = "123"
            });

            await Assert.MultipleAsync(async () =>
            {
                Assert.That(await harness.Sent.Any<OrderSubmitted>(), Is.True);

                Assert.That(await harness.Consumed.Any<SubmitOrder>(), Is.True);
            });
        }


        class MonitoredSubmitOrderConsumer :
            IConsumer<SubmitOrder>
        {
            public Task Consume(ConsumeContext<SubmitOrder> context)
            {
                return context.RespondAsync<OrderSubmitted>(context.Message);
            }
        }
    }
}
