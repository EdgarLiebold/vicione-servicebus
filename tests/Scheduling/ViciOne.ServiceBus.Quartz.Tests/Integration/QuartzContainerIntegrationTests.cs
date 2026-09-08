using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
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
public sealed class QuartzContainerIntegrationTests
{
    private static readonly DateTimeOffset DueAt = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULER-API", "in-memory-quartz-is-explicit-single-owner-and-delivers")]
    public async Task ReliableInMemoryScheduler_RegistersOneExplicitAdapterAndDeliversAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var delivered = new ContainerDeliveryProbe();
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddSingleton(delivered);
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<ContainerPayloadConsumer>();
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                ConfigureReliablePolicy(reliable);
                reliable.AddMessageContract<ContainerPayload>("vicione.tests.quartz-reliable");
                reliable.UseInMemoryQuartzScheduler(options => options.QueueName = "reliable-quartz");
            });
        });

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ISchedulerFactory));
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        QuartzSchedulerBinding<IBus> binding = provider.GetRequiredService<QuartzSchedulerBinding<IBus>>();
        Assert.Equal("MessageScheduler", scope.ServiceProvider.GetRequiredService<IMessageScheduler>().GetType().Name);
        Assert.IsType<EndpointRecurringMessageScheduler>(
            scope.ServiceProvider.GetRequiredService<IRecurringMessageScheduler>());

        IMessageContractCatalog contracts = provider.GetRequiredService<IMessageContractCatalog>();
        Assert.Equal(new MessageContractIdentity("vicione.scheduler.schedule", 1), contracts.GetIdentity(typeof(ScheduleMessage)));
        Assert.Equal(new MessageContractIdentity("vicione.scheduler.cancel", 1), contracts.GetIdentity(typeof(CancelScheduledMessage)));
        Assert.Equal(new MessageContractIdentity("vicione.scheduler.recurring.schedule", 1),
            contracts.GetIdentity(typeof(ScheduleRecurringMessage)));
        Assert.Equal(new MessageContractIdentity("vicione.scheduler.recurring.cancel", 1),
            contracts.GetIdentity(typeof(CancelScheduledRecurringMessage)));
        Assert.Equal(new MessageContractIdentity("vicione.scheduler.recurring.pause", 1),
            contracts.GetIdentity(typeof(PauseScheduledRecurringMessage)));
        Assert.Equal(new MessageContractIdentity("vicione.scheduler.recurring.resume", 1),
            contracts.GetIdentity(typeof(ResumeScheduledRecurringMessage)));

        IBusControl bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var scheduledCommand = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
            using ConnectHandle observer = bus.ConnectConsumeObserver(scheduledCommand);
            IMessageScheduler scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();

            ScheduledMessage<ContainerPayload> scheduled = await scheduler.SchedulePublishAsync(
                    DueAt,
                    new ContainerPayload("reliable-in-memory-delivery"),
                    Pipe.Execute<SendContext<ContainerPayload>>(context =>
                        context.Headers.Set("tenant", "factory-reliable")),
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await scheduledCommand.Completed.WaitAsync(timeout, cancellationToken);

            IScheduler quartz = await binding
                .GetSchedulerAsync(cancellationToken).AsTask()
                .WaitAsync(timeout, cancellationToken);
            Assert.Same(bus, quartz.Context[QuartzSchedulerContextKeys.Bus]);
            Assert.Same(provider.GetRequiredService<TimeProvider>(),
                quartz.Context[QuartzSchedulerContextKeys.TimeProvider]);
            TriggerKey triggerKey = QuartzTriggerKey.ForOneTime(scheduled.TokenId, binding.Settings.SchedulerNamespace);
            ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(
                await quartz.GetTrigger(triggerKey, cancellationToken).AsTask().WaitAsync(timeout, cancellationToken));
            await quartz.TriggerJob(trigger.JobKey, trigger.JobDataMap, cancellationToken).AsTask()
                .WaitAsync(timeout, cancellationToken);

            ContainerDelivery received = await delivered.Delivered.WaitAsync(timeout, cancellationToken);
            Assert.Equal("reliable-in-memory-delivery", received.Value);
            Assert.Equal("factory-reliable", received.Tenant);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULER-API", "quartz-has-no-implicit-in-memory-fallback")]
    public void ReliableQuartzScheduler_MissingFactoryFailsContainerValidation()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                ConfigureReliablePolicy(reliable);
                reliable.AddMessageContract<ContainerPayload>("vicione.tests.quartz-no-fallback");
                reliable.UseQuartzScheduler(provider => provider.GetRequiredService<ISchedulerFactory>());
            });
        });

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ISchedulerFactory));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<QuartzSchedulerBinding<IBus>>());
        Assert.Contains(nameof(ISchedulerFactory), failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULER-API", "mixed-adapters-are-rejected")]
    public void ReliableScheduler_RejectsMixedExplicitAdapters()
    {
        var services = new ServiceCollection();
        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            services.AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
                configuration.UseReliableMessaging(reliable =>
                {
                    reliable.UseInMemoryStore();
                    ConfigureReliablePolicy(reliable);
                    reliable.AddMessageContract<ContainerPayload>("vicione.tests.quartz-duplicate");
                    reliable.UseInMemoryQuartzScheduler();
                    reliable.UseQuartzScheduler(static _ => null!);
                });
            }));

        Assert.Contains("already configured", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULER-API", "queue-name-required")]
    public void ReliableScheduler_RejectsAnEmptyQueueName(bool useInMemoryScheduler)
    {
        var services = new ServiceCollection();

        ArgumentException failure = Assert.ThrowsAny<ArgumentException>(() =>
            services.AddViciOneServiceBus(configuration => configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                ConfigureReliablePolicy(reliable);
                if (useInMemoryScheduler)
                    reliable.UseInMemoryQuartzScheduler(options => options.QueueName = "   ");
                else
                    reliable.UseQuartzScheduler(static _ => null!, options => options.QueueName = "   ");
            })));

        Assert.Contains(nameof(QuartzEndpointOptions.QueueName), failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-QUARTZ-CONTAINER", "native-di-composition-and-serializer-roundtrip")]
    public async Task RegisteredQuartzScheduler_DeliversThroughTheConfiguredBusAsync(bool useRawJson)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var delivered = new ContainerDeliveryProbe();
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddSingleton(delivered);
        services.AddQuartz();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddPublishMessageScheduler();
            configuration.AddQuartzScheduling(provider => provider.GetRequiredService<ISchedulerFactory>());
            configuration.AddConsumer<ContainerPayloadConsumer>();
            configuration.UsingInMemory((context, configurator) =>
            {
                if (useRawJson)
                    configurator.UseRawJsonSerializer();

                configurator.ConfigurePublishMessageScheduler();
                configurator.ConfigureEndpoints(context);
            });
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var scheduledCommand = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
            using ConnectHandle observer = bus.ConnectConsumeObserver(scheduledCommand);
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IMessageScheduler scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();

            ScheduledMessage<ContainerPayload> scheduled = await scheduler.SchedulePublishAsync(
                    DueAt,
                    new ContainerPayload("container-delivery"),
                    Pipe.Execute<SendContext<ContainerPayload>>(context =>
                        context.Headers.Set("tenant", "factory-a")),
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await scheduledCommand.Completed.WaitAsync(timeout, cancellationToken);

            IScheduler quartz = await provider.GetRequiredService<ISchedulerFactory>()
                .GetScheduler(cancellationToken).AsTask()
                .WaitAsync(timeout, cancellationToken);
            TriggerKey triggerKey = QuartzTriggerKey.ForOneTime(
                scheduled.TokenId,
                QuartzSchedulerNamespace.ForBus(typeof(IBus)));
            ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(
                await quartz.GetTrigger(triggerKey, cancellationToken).AsTask().WaitAsync(timeout, cancellationToken));
            await quartz.TriggerJob(trigger.JobKey, trigger.JobDataMap, cancellationToken).AsTask()
                .WaitAsync(timeout, cancellationToken);

            ContainerDelivery received = await delivered.Delivered
                .WaitAsync(timeout, cancellationToken);
            Assert.Equal("container-delivery", received.Value);
            Assert.Equal("factory-a", received.Tenant);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-CONTAINER", "manual-endpoint-composition")]
    public async Task ManuallyConfiguredEndpoint_ConsumesRegisteredSchedulingCommandsAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Uri? schedulerEndpointAddress = null;
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddQuartz();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddQuartzScheduling(provider => provider.GetRequiredService<ISchedulerFactory>());
            configuration.UsingInMemory((context, configurator) =>
                configurator.ReceiveEndpoint("manual-quartz", endpoint =>
                {
                    endpoint.ConfigureQuartzScheduling(context);
                    schedulerEndpointAddress = endpoint.InputAddress;
                }));
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Uri schedulerEndpoint = Assert.IsType<Uri>(schedulerEndpointAddress);
            Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f61");
            var consumed = new ConsumeCompletionObserver<ScheduleMessage>(message => message.TokenId == tokenId);
            using ConnectHandle observer = bus.ConnectConsumeObserver(consumed);
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(schedulerEndpoint, cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            await endpoint.SendAsync<ScheduleMessage>(
                new ScheduleMessageCommand<ContainerPayload>(
                    DueAt,
                    new Uri("loopback://localhost/manual-quartz-target"),
                    new ContainerPayload("manual-composition"),
                    tokenId),
                cancellationToken);
            await consumed.Completed.WaitAsync(timeout, cancellationToken);

            IScheduler scheduler = await provider.GetRequiredService<ISchedulerFactory>()
                .GetScheduler(cancellationToken).AsTask()
                .WaitAsync(timeout, cancellationToken);
            ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await scheduler.GetTrigger(
                    QuartzTriggerKey.ForOneTime(tokenId, QuartzSchedulerNamespace.ForBus(typeof(IBus))),
                    cancellationToken).AsTask()
                .WaitAsync(timeout, cancellationToken));
            Assert.Equal(DueAt, trigger.StartTimeUtc);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-CONTAINER", "registered-definitions-cover-every-command")]
    public async Task RegisteredEndpoint_ExecutesEverySchedulingCommandThroughItsBusBindingAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string queueName = $"container-quartz-{NewId.NextGuid():N}";
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddQuartz();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddQuartzScheduling(
                provider => provider.GetRequiredService<ISchedulerFactory>(),
                options => options.QueueName = queueName);
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            QuartzSchedulerBinding<IBus> binding = provider.GetRequiredService<QuartzSchedulerBinding<IBus>>();
            IScheduler scheduler = await binding.GetSchedulerAsync(cancellationToken).AsTask()
                .WaitAsync(timeout, cancellationToken);
            Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f71");
            const string scheduleId = "container-recurring";
            const string scheduleGroup = "operations";
            TriggerKey oneTimeKey = QuartzTriggerKey.ForOneTime(tokenId, binding.Settings.SchedulerNamespace);
            TriggerKey recurringKey = QuartzTriggerKey.ForRecurring(scheduleId, scheduleGroup, binding.Settings.SchedulerNamespace);

            await SendAndAwaitAsync<ScheduleMessage>(
                bus,
                endpoint,
                new ScheduleMessageCommand<ContainerPayload>(
                    DueAt,
                    new Uri("loopback://localhost/container-command-target"),
                    new ContainerPayload("one-time"),
                    tokenId),
                timeout,
                cancellationToken);
            Assert.NotNull(await scheduler.GetTrigger(oneTimeKey, cancellationToken));

            await SendAndAwaitAsync<CancelScheduledMessage>(
                bus,
                endpoint,
                new { Timestamp = DueAt, TokenId = tokenId },
                timeout,
                cancellationToken);
            Assert.Null(await scheduler.GetTrigger(oneTimeKey, cancellationToken));

            await SendAndAwaitAsync<ScheduleRecurringMessage>(
                bus,
                endpoint,
                new ScheduleRecurringMessageCommand<ContainerPayload>(
                    new ContainerRecurringSchedule(scheduleId, scheduleGroup),
                    new Uri("loopback://localhost/container-command-target"),
                    new ContainerPayload("recurring")),
                timeout,
                cancellationToken);
            Assert.Equal(TriggerState.Normal, await scheduler.GetTriggerState(recurringKey, cancellationToken));

            await SendAndAwaitAsync<PauseScheduledRecurringMessage>(
                bus,
                endpoint,
                new { Timestamp = DueAt, ScheduleId = scheduleId, ScheduleGroup = scheduleGroup },
                timeout,
                cancellationToken);
            Assert.Equal(TriggerState.Paused, await scheduler.GetTriggerState(recurringKey, cancellationToken));

            await SendAndAwaitAsync<ResumeScheduledRecurringMessage>(
                bus,
                endpoint,
                new { Timestamp = DueAt, ScheduleId = scheduleId, ScheduleGroup = scheduleGroup },
                timeout,
                cancellationToken);
            Assert.Equal(TriggerState.Normal, await scheduler.GetTriggerState(recurringKey, cancellationToken));

            await SendAndAwaitAsync<CancelScheduledRecurringMessage>(
                bus,
                endpoint,
                new { Timestamp = DueAt, ScheduleId = scheduleId, ScheduleGroup = scheduleGroup },
                timeout,
                cancellationToken);
            Assert.Null(await scheduler.GetTrigger(recurringKey, cancellationToken));
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record ContainerPayload(string Value);

    public sealed record ContainerDelivery(string Value, string? Tenant);

    public sealed class ContainerPayloadConsumer(ContainerDeliveryProbe probe) : IConsumer<ContainerPayload>
    {
        public Task ConsumeAsync(ConsumeContext<ContainerPayload> context)
        {
            probe.Complete(context);
            return Task.CompletedTask;
        }
    }

    public sealed class ContainerDeliveryProbe
    {
        private readonly TaskCompletionSource<ContainerDelivery> _delivered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ContainerDelivery> Delivered => _delivered.Task;

        public void Complete(ConsumeContext<ContainerPayload> context) =>
            _delivered.TrySetResult(new ContainerDelivery(context.Message.Value, context.Headers.Get<string>("tenant")));
    }

    private static async Task SendAndAwaitAsync<T>(
        IBus bus,
        ISendEndpoint endpoint,
        object message,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        where T : class
    {
        var consumed = new ConsumeCompletionObserver<T>(_ => true);
        using ConnectHandle observer = bus.ConnectConsumeObserver(consumed);
        await endpoint.SendAsync<T>(message, cancellationToken);
        await consumed.Completed.WaitAsync(timeout, cancellationToken);
    }

    private sealed class ContainerRecurringSchedule(string scheduleId, string scheduleGroup) : RecurringSchedule
    {
        public string TimeZoneId => TimeZoneInfo.Utc.Id;
        public DateTimeOffset StartTime => DueAt;
        public DateTimeOffset? EndTime => DueAt.AddDays(1);
        public string ScheduleId => scheduleId;
        public string ScheduleGroup => scheduleGroup;
        public string CronExpression => "0 0 0 ? * *";
        public string Description => "Container command coverage";
        public MissedEventPolicy MisfirePolicy => MissedEventPolicy.Skip;
    }

    static void ConfigureReliablePolicy(IReliableMessagingConfigurator reliable)
    {
        reliable.Store(new ReliableStoreLimits
        {
            MaximumStoredCount = 100,
            MaximumStoredBytes = 1024 * 1024,
        });
        reliable.Delivery(_ => { });
        reliable.Retention(TimeSpan.FromDays(1));
    }
}
