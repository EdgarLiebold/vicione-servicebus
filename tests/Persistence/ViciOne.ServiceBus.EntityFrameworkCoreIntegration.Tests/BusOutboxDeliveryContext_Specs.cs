namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests
{
    using System;
    using System.Diagnostics.Metrics;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.Metrics.Testing;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Middleware;
    using Middleware.Outbox;
    using Monitoring;
    using NUnit.Framework;
    using TestFramework;
    using ViciOne.ServiceBus.Logging;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// The outbox delivery service is a background service. It starts on the execution context of the host, which
    /// carries no log context of its own provider, while the outbox delivery instruments record through
    /// <see cref="LogContext.Current" />. The entry therefore has to establish that context itself, and it has to do
    /// so even when something else is already ambient.
    /// </summary>
    [TestFixture]
    public class BusOutboxDeliveryContext_Specs
    {
        [Test]
        public async Task Should_deliver_through_the_meter_scope_of_its_own_provider()
        {
            var entered = new TaskCompletionSource<ILogContext>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using ServiceProvider provider = new ServiceCollection()
                .AddViciOneServiceBusTestHarness()
                .AddScoped(_ => new RecordingDbContext(new DbContextOptionsBuilder<RecordingDbContext>().Options, entered))
                .BuildServiceProvider();

            var harness = provider.GetTestHarness();

            await harness.Start();

            // A foreign context is ambient on the flow that starts the background service. The entry must not adopt it.
            LogContext.ConfigureCurrentLogContext(BusTestFixture.LoggerFactory);

            var service = new BusOutboxDeliveryService<RecordingDbContext>(provider.GetRequiredService<IBusControl>(),
                Options.Create(new OutboxDeliveryServiceOptions()),
                Options.Create(new EntityFrameworkOutboxOptions<RecordingDbContext>()),
                new NoNotification(),
                NullLogger<BusOutboxDeliveryService<RecordingDbContext>>.Instance,
                provider);

            var options = provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value;

            using var ownScope = new MetricCollector<long>(provider.GetRequiredService<IMeterFactory>(), InstrumentationOptions.MeterName,
                options.OutboxDeliveryTotal);
            using var outsideAnyScope = new MetricCollector<long>(null, InstrumentationOptions.MeterName, options.OutboxDeliveryTotal);

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await service.StartAsync(cancellation.Token);
            try
            {
                ILogContext deliveryLogContext = await entered.Task.OrCanceled(cancellation.Token);

                Assert.That(deliveryLogContext, Is.Not.Null, "The delivery loop must run on an established log context");

                // Exactly what the delivery path does: it records through the context it runs on.
                deliveryLogContext.StartOutboxDeliveryInstrument(new OutboxMessage
                {
                    DestinationAddress = new Uri("loopback://localhost/outbox-delivery")
                });

                Assert.Multiple(() =>
                {
                    Assert.That(ownScope.GetMeasurementSnapshot(), Has.Count.EqualTo(1),
                        "The delivery loop must record through the meter scope of its own provider");
                    Assert.That(outsideAnyScope.GetMeasurementSnapshot(), Is.Empty,
                        "The delivery loop must not record outside the scope of its own provider");
                });
            }
            finally
            {
                await service.StopAsync(TestContext.CurrentContext.CancellationToken);
            }
        }


        /// <summary>
        /// Resolved by the delivery loop inside the scope of its own provider, which makes its constructor the exact
        /// point at which the established log context becomes observable.
        /// </summary>
        class RecordingDbContext :
            DbContext
        {
            public RecordingDbContext(DbContextOptions<RecordingDbContext> options, TaskCompletionSource<ILogContext> entered)
                : base(options)
            {
                entered.TrySetResult(LogContext.Current);
            }
        }


        class NoNotification :
            IBusOutboxNotification
        {
            public Task WaitForDelivery(CancellationToken cancellationToken)
            {
                return Task.Delay(Timeout.Infinite, cancellationToken);
            }

            public void Delivered()
            {
            }
        }
    }
}
