using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class ContainerRoutingSlipOutboxRequestTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-COURIER-OUTBOX", "activity-request-completes-with-outbox-on-every-endpoint")]
    public async Task ExecuteActivityRequest_CompletesWhileEveryConfiguredEndpointOwnsAnOutboxAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ActivityRequestObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ActivityRequestConsumer>()
                    .Endpoint(endpoint => endpoint.Name = "container-activity-request");
                configuration.AddExecuteActivity<RequestingActivity, RequestingArguments>()
                    .Endpoint(endpoint => endpoint.Name = "container-requesting-activity_execute");
                configuration.AddRequestClient<ActivityRequest>(new Uri("queue:container-activity-request"));
                configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
                    endpoint.UseVolatileOutbox(context));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddSubscription(harness.Bus.Address, RoutingSlipEvents.All);
            builder.AddActivity(
                "RequestingActivity",
                new Uri("queue:container-requesting-activity_execute"),
                new RequestingArguments("Hello"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            ISentMessage<RoutingSlipActivityCompleted> activityCompleted = await harness.Sent
                .SelectAsync<RoutingSlipActivityCompleted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            ISentMessage<RoutingSlipCompleted> completed = await harness.Sent
                .SelectAsync<RoutingSlipCompleted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            ActivityRequestSnapshot snapshot = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(trackingNumber, activityCompleted.Context.Message.TrackingNumber);
            Assert.Equal(trackingNumber, completed.Context.Message.TrackingNumber);
            Assert.Equal(new ActivityRequestSnapshot("Hello", "Hello, Hello", 1, 1), snapshot);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Sent.Select<RoutingSlipActivityCompleted>(SnapshotOnlyToken()));
        Assert.Single(harness.Sent.Select<RoutingSlipCompleted>(SnapshotOnlyToken()));
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public sealed record RequestingArguments(string Value);

    public sealed record ActivityRequest(string Value);

    public sealed record ActivityResponse(string Value);

    public sealed record ActivityRequestSnapshot(
        string RequestValue,
        string ResponseValue,
        int ConsumerCount,
        int ActivityCount);

    public sealed class ActivityRequestObservation
    {
        private readonly object _lock = new();
        private string? _requestValue;
        private string? _responseValue;
        private int _consumerCount;
        private int _activityCount;

        public TaskCompletionSource<ActivityRequestSnapshot> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordConsumer(string requestValue)
        {
            lock (_lock)
            {
                _requestValue = requestValue;
                _consumerCount++;
                TryComplete();
            }
        }

        public void RecordActivity(string responseValue)
        {
            lock (_lock)
            {
                _responseValue = responseValue;
                _activityCount++;
                TryComplete();
            }
        }

        private void TryComplete()
        {
            if (_requestValue is null || _responseValue is null)
                return;

            Completed.TrySetResult(new ActivityRequestSnapshot(
                _requestValue,
                _responseValue,
                _consumerCount,
                _activityCount));
        }
    }

    public sealed class RequestingActivity(
        IRequestClient<ActivityRequest> client,
        ActivityRequestObservation observation) : IExecuteActivity<RequestingArguments>
    {
        public async Task<ExecutionResult> ExecuteAsync(ExecuteContext<RequestingArguments> context)
        {
            Response<ActivityResponse> response = await client.GetResponseAsync<ActivityResponse>(
                new ActivityRequest(context.Arguments.Value),
                context.CancellationToken);
            observation.RecordActivity(response.Message.Value);
            return context.Completed();
        }
    }

    public sealed class ActivityRequestConsumer(ActivityRequestObservation observation) : IConsumer<ActivityRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<ActivityRequest> context)
        {
            observation.RecordConsumer(context.Message.Value);
            await context.RespondAsync(new ActivityResponse($"Hello, {context.Message.Value}"));
        }
    }
}
