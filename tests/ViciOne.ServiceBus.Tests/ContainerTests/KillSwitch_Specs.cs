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
                        // The restart timeout is how long the endpoint stays stopped between trips. It
                        // is not what this spec asserts: the subject is the state sequence Healthy →
                        // Degraded → Healthy under a fault storm, and both thresholds above, which
                        // decide when the switch activates and trips, are unchanged. The five seconds
                        // this carried from the imported suite were never asserted against, exercised
                        // or named anywhere; they were the interval between the same transitions.
                        //
                        // Measured: the scenario needs one restart cycle per trip, and it trips more
                        // than once because the bad messages still queued when the endpoint stops are
                        // consumed again after the restart. A full core run produced three stops, so at
                        // five seconds per cycle the scenario needed exactly the fifteen seconds the
                        // health wait allows and failed on its edge. The health wait itself must not be
                        // extended, so the interval between the transitions was aligned with the
                        // sibling spec in this project and the ActiveMQ one, which both already use one
                        // second. Every state transition the spec asserts is preserved; no assertion,
                        // no threshold and no wait budget changes.
                        cfg.UseKillSwitch(options => options
                            .SetActivationThreshold(5)
                            .SetTripThreshold(10)
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
