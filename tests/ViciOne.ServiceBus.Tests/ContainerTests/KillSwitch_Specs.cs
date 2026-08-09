// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ContainerTests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;
    using TestFramework;


    [TestFixture]
    [Category("Flaky")]
    public class KillSwitch_Specs :
        BusTestFixture
    {
        [Test]
        public async Task Should_be_degraded_after_too_many_exceptions()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ILoggerFactory>(_ => LoggerFactory);
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
            services.AddViciOneServiceBus(x =>
                {
                    x.AddConsumer<BadConsumer>();

                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.UseKillSwitch(options => options
                            .SetActivationThreshold(5)
                            .SetTripThreshold(10)
// Measured: the scenario needs one restart cycle per kill switch trip, and it trips more than
                            // once because the bad messages left in the queue at the moment the endpoint stops are
                            // consumed again after the restart. A full core run produced three stops, so at five
                            // seconds per cycle the scenario needed exactly the fifteen seconds the health wait
                            // allows and failed on the edge. The sibling spec in this same project, and the ActiveMQ
                            // one, already use one second. This aligns them; no assertion, threshold or budget changes.
                            .SetRestartTimeout(s: 1));

                        cfg.ConfigureEndpoints(context);
                    });
                });

            // The provider owns the bus, its timers and the kill switch. It was never disposed, so every
            // run of this spec left them behind for the rest of the process.
            await using var provider = services.BuildServiceProvider(true);

            var healthChecks = provider.GetService<HealthCheckService>();

            IHostedService[] hostedServices = provider.GetServices<IHostedService>().ToArray();

            await healthChecks.WaitForHealthStatus(HealthStatus.Unhealthy);

            await Task.WhenAll(hostedServices.Select(x => x.StartAsync(TestCancellationToken)));
            try
            {
                await healthChecks.WaitForHealthStatus(HealthStatus.Healthy);

                using var scope = provider.CreateScope();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                await Task.WhenAll(Enumerable.Range(0, 20).Select(x => publishEndpoint.Publish(new BadMessage())));

                await healthChecks.WaitForHealthStatus(HealthStatus.Degraded);

                await Task.WhenAll(Enumerable.Range(0, 20).Select(x => publishEndpoint.Publish(new GoodMessage())));

                await healthChecks.WaitForHealthStatus(HealthStatus.Healthy);
            }
            finally
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                await Task.WhenAll(hostedServices.Select(x => x.StopAsync(cts.Token)));
            }
        }


        class BadConsumer :
            IConsumer<BadMessage>,
            IConsumer<GoodMessage>
        {
            public Task Consume(ConsumeContext<BadMessage> context)
            {
                throw new IntentionalTestException("Trying to trigger the kill switch");
            }

            public Task Consume(ConsumeContext<GoodMessage> context)
            {
                return Task.CompletedTask;
            }
        }


        class GoodMessage
        {
        }


        class BadMessage
        {
        }


        public KillSwitch_Specs()
            : base(new InMemoryTestHarness())
        {
            TestTimeout = TimeSpan.FromMinutes(1);
        }
    }
}
