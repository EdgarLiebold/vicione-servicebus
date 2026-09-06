using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzMissingSagaRedeliveryIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-MISSING-SAGA", "redelivery-finds-later-instance")]
    public async Task MissingSaga_IsScheduledThroughQuartzAndDeliveredAfterTheInstanceExistsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        string queueName = $"quartz-missing-saga-{NewId.NextGuid():N}";
        var inputAddress = new Uri($"loopback://localhost/{queueName}");
        var stateMachine = new ServiceStateMachine();
        var repository = new InMemorySagaRepository<ServiceInstance>();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator => configurator.ReceiveEndpoint(queueName, endpoint =>
                endpoint.StateMachineSaga(stateMachine, repository)));
        var scheduledCommands = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(scheduledCommands);
        IRequestClient<CheckServiceStatus> requestClient = fixture.Bus.CreateRequestClient<CheckServiceStatus>(inputAddress, timeout);
        Task<Response<ServiceStatus, ServiceInstanceNotFound>> responseTask = requestClient.Advanced().GetResponseAsync<
            ServiceStatus,
            ServiceInstanceNotFound>(
            new CheckServiceStatus("scheduler"),
            cancellationToken: TestContext.Current.CancellationToken);

        await scheduledCommands.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        ISendEndpoint input = await fixture.Bus.GetSendEndpointAsync(inputAddress, TestContext.Current.CancellationToken).WaitAsync(timeout, TestContext.Current.CancellationToken);
        Guid serviceId = NewId.NextGuid();
        await input.SendAsync(new StartService("scheduler", serviceId), TestContext.Current.CancellationToken);
        await stateMachine.Started.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Response<ServiceStatus, ServiceInstanceNotFound> response = await responseTask
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        bool hasStatus = response.Is(out Response<ServiceStatus>? status);
        bool hasNotFound = response.Is(out Response<ServiceInstanceNotFound>? _);

        Assert.True(hasStatus);
        Assert.NotNull(status);
        Assert.False(hasNotFound);
        Assert.Equal("scheduler", status.Message.ServiceName);
        Assert.Equal("Running", status.Message.Status);
        Assert.Equal(1, scheduledCommands.ObservedCount);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed class ServiceInstance : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public State CurrentState { get; set; } = null!;
        public string ServiceName { get; set; } = string.Empty;
    }

    public sealed class ServiceStateMachine : ViciOneServiceBusStateMachine<ServiceInstance>
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ServiceStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => ServiceStarted, configuration => configuration
                .CorrelateBy(instance => instance.ServiceName, context => context.Message.ServiceName)
                .SelectId(context => context.Message.ServiceId));
            Event(() => StatusRequested, configuration =>
            {
                configuration.CorrelateBy(instance => instance.ServiceName, context => context.Message.ServiceName);
                configuration.OnMissingInstance(missing => missing.Redeliver(redelivery =>
                {
                    redelivery.Interval(3, TimeSpan.FromSeconds(1));
                    redelivery.OnRedeliveryLimitReached(limit => limit.ExecuteAsync(context =>
                        context.RespondAsync(new ServiceInstanceNotFound(context.Message.ServiceName))));
                }));
            });

            Initially(
                When(ServiceStarted)
                    .Then(context =>
                    {
                        context.Saga.ServiceName = context.Message.ServiceName;
                        _started.TrySetResult();
                    })
                    .TransitionTo(Running));
            During(Running,
                When(StatusRequested)
                    .Respond(context => new ServiceStatus("Running", context.Saga.ServiceName)));
        }

        public Task Started => _started.Task;
        public State Running { get; private set; } = null!;
        public Event<StartService> ServiceStarted { get; private set; } = null!;
        public Event<CheckServiceStatus> StatusRequested { get; private set; } = null!;
    }

    public sealed record StartService(string ServiceName, Guid ServiceId);

    public sealed record CheckServiceStatus(string ServiceName);

    public sealed record ServiceStatus(string Status, string ServiceName);

    public sealed record ServiceInstanceNotFound(string ServiceName);
}
