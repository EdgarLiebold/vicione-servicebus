using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class ReliableQuartzBorrowedFactoryContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULER-API", "explicit-reliable-factory-delivers-command-and-remains-caller-owned")]
    public async Task ExplicitReliableFactory_CreatesTheTriggerAndSurvivesBusAndProviderDisposalAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        DateTimeOffset due = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);
        ISchedulerFactory factory = QuartzSchedulerBuilder.Create().Build();
        await using IAsyncDisposable factoryOwner = Assert.IsAssignableFrom<IAsyncDisposable>(factory);
        var services = new ServiceCollection();
        IReliableMessagingConfigurator<IBus>? original = null;
        IReliableMessagingConfigurator<IBus>? returned = null;
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
            registration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                reliable.Store(new ReliableStoreLimits { MaximumStoredCount = 100, MaximumStoredBytes = 1024 * 1024 });
                reliable.Delivery(_ => { });
                reliable.Retention(TimeSpan.FromDays(1));
                reliable.AddMessageContract<Payload>("vicione.tests.quartz-borrowed-reliable");
                original = reliable;
                returned = reliable.UseQuartzScheduler(_ => factory,
                    options => options.QueueName = $"borrowed-reliable-{NewId.NextGuid():N}");
            });
        });
        Assert.NotNull(original);
        Assert.Same(original, returned);
        IScheduler? selected = null;
        await using (ServiceProvider provider = services.BuildServiceProvider(validateScopes: true))
        {
            IBusControl bus = provider.GetRequiredService<IBusControl>();
            try
            {
                await bus.StartAsync(token).WaitAsync(timeout, token);
                await using AsyncServiceScope scope = provider.CreateAsyncScope();
                QuartzSchedulerBinding<IBus> binding = provider.GetRequiredService<QuartzSchedulerBinding<IBus>>();
                selected = await binding.GetSchedulerAsync(token);
                Assert.Same(await factory.GetScheduler(token), selected);
                Assert.Same(bus, selected.Context[QuartzSchedulerContextKeys.Bus]);
                var consumed = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
                using ConnectHandle observer = bus.ConnectConsumeObserver(consumed);
                IMessageScheduler scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
                ScheduledMessage<Payload> scheduled = await scheduler.SchedulePublishAsync(due,
                    new Payload("borrowed"), token);
                await consumed.Completed.WaitAsync(timeout, token);
                ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await selected.GetTrigger(
                    QuartzTriggerKey.ForOneTime(scheduled.TokenId, binding.Settings.SchedulerNamespace), token));
                Assert.Equal(due, trigger.StartTimeUtc);
                Assert.Equal(SchedulerStatus.Running, selected.Status);
            }
            finally
            {
                await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            }
        }
        Assert.NotNull(selected);
        Assert.Equal(SchedulerStatus.Standby, selected.Status);
        Assert.Same(selected, await factory.GetScheduler(token));
    }

    public sealed record Payload(string Value);
}
