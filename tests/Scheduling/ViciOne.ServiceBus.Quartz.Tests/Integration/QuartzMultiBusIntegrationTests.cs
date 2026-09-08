using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzMultiBusIntegrationTests
{
    private static readonly DateTimeOffset DueAt = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-MULTIBUS", "independent-factories-contexts-triggers-and-lifecycles")]
    public async Task TwoBuses_KeepSchedulersDeliveryAndLifecycleStrictlyIsolatedAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var deliveries = new MultiBusDeliveryProbe();
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddSingleton(deliveries);
        services.AddViciOneServiceBus<IAlphaBus>(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<AlphaPayloadConsumer>();
            configuration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri("loopback://quartz-alpha/"));
                bus.ConfigureEndpoints(context);
            });
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                ConfigureReliablePolicy(reliable);
                reliable.AddMessageContract<AlphaPayload>("vicione.tests.quartz.alpha");
                reliable.UseInMemoryQuartzScheduler(options => options.QueueName = "quartz-alpha-commands");
            });
        });
        services.AddViciOneServiceBus<IBetaBus>(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<BetaPayloadConsumer>();
            configuration.UsingInMemory((context, bus) =>
            {
                bus.Host(new Uri("loopback://quartz-beta/"));
                bus.ConfigureEndpoints(context);
            });
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                ConfigureReliablePolicy(reliable);
                reliable.AddMessageContract<BetaPayload>("vicione.tests.quartz.beta");
                reliable.UseInMemoryQuartzScheduler(options => options.QueueName = "quartz-beta-commands");
            });
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        IAlphaBus alphaBus = provider.GetRequiredService<IAlphaBus>();
        IBetaBus betaBus = provider.GetRequiredService<IBetaBus>();
        IBusControl alphaControl = (IBusControl)alphaBus;
        IBusControl betaControl = (IBusControl)betaBus;
        bool alphaStarted = false;
        bool betaStarted = false;

        await alphaControl.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        alphaStarted = true;
        await betaControl.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        betaStarted = true;

        try
        {
            QuartzSchedulerBinding<IAlphaBus> alphaBinding =
                provider.GetRequiredService<QuartzSchedulerBinding<IAlphaBus>>();
            QuartzSchedulerBinding<IBetaBus> betaBinding =
                provider.GetRequiredService<QuartzSchedulerBinding<IBetaBus>>();
            IScheduler alphaQuartz = await alphaBinding.GetSchedulerAsync(cancellationToken).AsTask()
                .WaitAsync(timeout, cancellationToken);
            IScheduler betaQuartz = await betaBinding.GetSchedulerAsync(cancellationToken).AsTask()
                .WaitAsync(timeout, cancellationToken);

            Assert.NotSame(alphaQuartz, betaQuartz);
            Assert.NotEqual(alphaQuartz.SchedulerName, betaQuartz.SchedulerName);
            Assert.NotEqual(alphaBinding.Settings.SchedulerNamespace, betaBinding.Settings.SchedulerNamespace);
            IBus alphaRuntime = Assert.IsAssignableFrom<IBus>(alphaQuartz.Context[QuartzSchedulerContextKeys.Bus]);
            IBus betaRuntime = Assert.IsAssignableFrom<IBus>(betaQuartz.Context[QuartzSchedulerContextKeys.Bus]);
            Assert.NotSame(alphaRuntime, betaRuntime);
            Assert.Equal(alphaBus.Address, alphaRuntime.Address);
            Assert.Equal(betaBus.Address, betaRuntime.Address);

            var alphaCommand = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
            var betaCommand = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
            using ConnectHandle alphaObserver = alphaBus.ConnectConsumeObserver(alphaCommand);
            using ConnectHandle betaObserver = betaBus.ConnectConsumeObserver(betaCommand);
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IMessageScheduler alphaScheduler = scope.ServiceProvider
                .GetRequiredService<Bind<IAlphaBus, IMessageScheduler>>().Value;
            IMessageScheduler betaScheduler = scope.ServiceProvider
                .GetRequiredService<Bind<IBetaBus, IMessageScheduler>>().Value;

            ScheduledMessage<AlphaPayload> alphaSchedule = await alphaScheduler.SchedulePublishAsync(
                DueAt,
                new AlphaPayload("alpha"),
                cancellationToken);
            ScheduledMessage<BetaPayload> betaSchedule = await betaScheduler.SchedulePublishAsync(
                DueAt,
                new BetaPayload("beta"),
                cancellationToken);
            await alphaCommand.Completed.WaitAsync(timeout, cancellationToken);
            await betaCommand.Completed.WaitAsync(timeout, cancellationToken);

            ITrigger alphaTrigger = Assert.IsAssignableFrom<ITrigger>(await alphaQuartz.GetTrigger(
                QuartzTriggerKey.ForOneTime(alphaSchedule.TokenId, alphaBinding.Settings.SchedulerNamespace),
                cancellationToken));
            ITrigger betaTrigger = Assert.IsAssignableFrom<ITrigger>(await betaQuartz.GetTrigger(
                QuartzTriggerKey.ForOneTime(betaSchedule.TokenId, betaBinding.Settings.SchedulerNamespace),
                cancellationToken));
            Assert.NotEqual(alphaTrigger.Key, betaTrigger.Key);

            await alphaQuartz.TriggerJob(alphaTrigger.JobKey, alphaTrigger.JobDataMap, cancellationToken)
                .AsTask().WaitAsync(timeout, cancellationToken);
            Assert.Equal("alpha", await deliveries.Alpha.Task.WaitAsync(timeout, cancellationToken));
            Assert.False(deliveries.Beta.Task.IsCompleted);

            await alphaControl.StopAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            alphaStarted = false;
            Assert.Equal(SchedulerStatus.Standby, alphaQuartz.Status);
            Assert.Equal(SchedulerStatus.Running, betaQuartz.Status);
            Assert.False(alphaQuartz.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
            Assert.Same(betaRuntime, betaQuartz.Context[QuartzSchedulerContextKeys.Bus]);

            await betaQuartz.TriggerJob(betaTrigger.JobKey, betaTrigger.JobDataMap, cancellationToken)
                .AsTask().WaitAsync(timeout, cancellationToken);
            Assert.Equal("beta", await deliveries.Beta.Task.WaitAsync(timeout, cancellationToken));
        }
        finally
        {
            if (betaStarted)
                await betaControl.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            if (alphaStarted)
                await alphaControl.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void ConfigureReliablePolicy(IReliableMessagingConfigurator reliable)
    {
        reliable.Store(new ReliableStoreLimits
        {
            MaximumStoredCount = 100,
            MaximumStoredBytes = 1024 * 1024,
        });
        reliable.Delivery(_ => { });
        reliable.Retention(TimeSpan.FromDays(1));
    }

    public interface IAlphaBus : IBus;

    public interface IBetaBus : IBus;

    public sealed record AlphaPayload(string Value);

    public sealed record BetaPayload(string Value);

    public sealed class AlphaPayloadConsumer(MultiBusDeliveryProbe deliveries) : IConsumer<AlphaPayload>
    {
        public Task ConsumeAsync(ConsumeContext<AlphaPayload> context)
        {
            deliveries.Alpha.TrySetResult(context.Message.Value);
            return Task.CompletedTask;
        }
    }

    public sealed class BetaPayloadConsumer(MultiBusDeliveryProbe deliveries) : IConsumer<BetaPayload>
    {
        public Task ConsumeAsync(ConsumeContext<BetaPayload> context)
        {
            deliveries.Beta.TrySetResult(context.Message.Value);
            return Task.CompletedTask;
        }
    }

    public sealed class MultiBusDeliveryProbe
    {
        public TaskCompletionSource<string> Alpha { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<string> Beta { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
