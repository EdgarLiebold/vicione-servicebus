using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ViciOne.ServiceBus.Quartz.Tests.QuartzIntegration;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzContainerIntegrationTests
{
    private static readonly DateTimeOffset DueAt = new(2100, 2, 3, 4, 5, 6, TimeSpan.Zero);

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
            configuration.AddQuartzConsumers();
            configuration.AddConsumer<ContainerPayloadConsumer>();
            configuration.UsingInMemory((context, configurator) =>
            {
                if (useRawJson)
                    configurator.UseRawJsonSerializer();

                configurator.UsePublishMessageScheduler();
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
            TriggerKey triggerKey = new(scheduled.TokenId.ToString("N"));
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
}
