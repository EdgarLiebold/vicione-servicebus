// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.SagaStateMachineTests
{
    using System;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using TestFramework.Sagas.ChoirConcurrency;


    public class When_testing_concurrency_with_the_choir
    {
        [Test]
        public async Task Should_work_as_expected()
        {
            await using var provider = new ServiceCollection()
                .AddTelemetryListener()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddSagaStateMachine<ChoirStateMachine, ChoirState>();

                    x.AddConfigureEndpointsCallback((name, cfg) =>
                    {
                        cfg.UseMessageRetry(r => r.Intervals(500, 1000, 2000, 2000, 2000));
                        cfg.UseInMemoryOutbox();
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var correlationId = NewId.NextGuid();

            await harness.Bus.Publish(new RehearsalBegins { CorrelationId = correlationId });

            ISagaStateMachineTestHarness<ChoirStateMachine, ChoirState> sagaHarness = harness.GetSagaStateMachineHarness<ChoirStateMachine, ChoirState>();

            Guid? sagaId = await sagaHarness.Exists(correlationId, x => x.Warmup);
            Assert.That(sagaId.HasValue, Is.True);

            await Task.WhenAll(
                harness.Bus.Publish(new Bass
                {
                    CorrelationId = correlationId,
                    Name = "John"
                }),
                harness.Bus.Publish(new Baritone
                {
                    CorrelationId = correlationId,
                    Name = "Mark"
                }),
                harness.Bus.Publish(new Tenor
                {
                    CorrelationId = correlationId,
                    Name = "Anthony"
                }),
                harness.Bus.Publish(new Countertenor
                {
                    CorrelationId = correlationId,
                    Name = "Tom"
                })
            );

            sagaId = await sagaHarness.Exists(correlationId, x => x.Harmony);
            Assert.That(sagaId.HasValue, Is.True);
        }
    }
}
