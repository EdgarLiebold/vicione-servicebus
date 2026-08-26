using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.Configuration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzContainerIntegrationTests
{
    private static readonly DateTime ScheduledTime = new(2100, 2, 3, 4, 5, 6, DateTimeKind.Utc);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-QUARTZ-CONTAINER", "native-di-composition-and-serializer-roundtrip")]
    public async Task RegisteredQuartzScheduler_DeliversThroughTheConfiguredBus(bool useRawJson)
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

            ScheduledMessage<ContainerPayload> scheduled = await scheduler.SchedulePublish(
                    ScheduledTime,
                    new ContainerPayload("container-delivery"),
                    Pipe.Execute<SendContext<ContainerPayload>>(context =>
                        context.Headers.Set("tenant", "factory-a")),
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await scheduledCommand.Completed.WaitAsync(timeout, cancellationToken);

            IScheduler quartz = await provider.GetRequiredService<ISchedulerFactory>()
                .GetScheduler(cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            TriggerKey triggerKey = new(scheduled.TokenId.ToString("N"));
            ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(
                await quartz.GetTrigger(triggerKey, cancellationToken).WaitAsync(timeout, cancellationToken));
            await quartz.TriggerJob(trigger.JobKey, trigger.JobDataMap, cancellationToken)
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
        public Task Consume(ConsumeContext<ContainerPayload> context)
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
