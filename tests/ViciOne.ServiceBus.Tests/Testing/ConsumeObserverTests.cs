using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class ConsumeObserverTests
{
    private static readonly DateTimeOffset ObservationTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVER", "bus-success-typed-and-untyped")]
    public async Task BusObservers_RecordTheExactSuccessfulMessageAndObservationMetadataAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FakeTimeProvider(ObservationTime);
        using var harness = CreateHarness(timeout, timeProvider);
        TestConsumeMessageObserver<ObservedMessage> typed = harness.GetConsumeObserver<ObservedMessage>();
        TestConsumeObserver untyped = harness.GetConsumeObserver();
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Handler<ObservedMessage>(_ => Task.CompletedTask);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            using ConnectHandle typedHandle = harness.Bus.ConnectConsumeMessageObserver(typed);
            using ConnectHandle untypedHandle = harness.Bus.ConnectConsumeObserver(untyped);
            var source = new ObservedMessage(NewId.NextGuid(), "expected");

            await harness.Bus.PublishAsync(source, cancellationToken).WaitAsync(timeout, cancellationToken);

            ObservedMessage preConsumed = await typed.PreConsumed.WaitAsync(timeout, cancellationToken);
            ObservedMessage postConsumed = await typed.PostConsumed.WaitAsync(timeout, cancellationToken);
            IConsumedMessage<ObservedMessage> observed = await untyped.Messages
                .SelectAsync<ObservedMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(source, preConsumed);
            Assert.Equal(source, postConsumed);
            Assert.Equal(source, observed.Context.Message);
            Assert.Same(observed.Context.Message, observed.MessageObject);
            Assert.Equal(observed.Context.MessageId, observed.ElementId);
            Assert.Equal(typeof(ObservedMessage), observed.MessageType);
            Assert.Equal(TypeCache<ObservedMessage>.ShortName, observed.ShortTypeName);
            Assert.Null(observed.Exception);
            Assert.InRange(observed.ElapsedTime, TimeSpan.Zero, observed.Context.Advanced().ReceiveContext.ElapsedTime);
            Assert.Equal(ObservationTime.UtcDateTime - observed.ElapsedTime, observed.StartTime);
            Assert.False(typed.ConsumeFaulted.IsCompleted);
            Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
                new ConsumedMessage<ObservedMessage>(observed.Context, null, null!)).ParamName);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVER", "bus-fault-typed-and-untyped")]
    public async Task BusObservers_RecordAndPropagateTheExactConsumeFailureWithoutPostConsumeAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected observer failure");
        using var harness = CreateHarness(timeout, new FakeTimeProvider(ObservationTime));
        TestConsumeMessageObserver<FailingObservedMessage> typed = harness.GetConsumeObserver<FailingObservedMessage>();
        TestConsumeObserver untyped = harness.GetConsumeObserver();
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Handler<FailingObservedMessage>(_ => Task.FromException(expected));

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            using ConnectHandle typedHandle = harness.Bus.ConnectConsumeMessageObserver(typed);
            using ConnectHandle untypedHandle = harness.Bus.ConnectConsumeObserver(untyped);
            var source = new FailingObservedMessage(NewId.NextGuid());

            await harness.Bus.PublishAsync(source, cancellationToken).WaitAsync(timeout, cancellationToken);

            FailingObservedMessage preConsumed = await typed.PreConsumed.WaitAsync(timeout, cancellationToken);
            InvalidOperationException typedFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                typed.ConsumeFaulted.WaitAsync(timeout, cancellationToken));
            IConsumedMessage<FailingObservedMessage> observed = await untyped.Messages
                .SelectAsync<FailingObservedMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(source, preConsumed);
            Assert.Same(expected, typedFailure);
            Assert.Same(expected, observed.Exception);
            Assert.Equal(source, observed.Context.Message);
            Assert.False(typed.PostConsumed.IsCompleted);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVER", "consumer-success-pre-and-post")]
    public async Task TypedObserver_ReportsPreAndPostAroundAConsumerInvocationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, new FakeTimeProvider(ObservationTime));
        TestConsumeMessageObserver<ConsumerObservedMessage> observer = harness.GetConsumeObserver<ConsumerObservedMessage>();
        var consumed = new TaskCompletionSource<ConsumerObservedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Consumer(() => new ObservedConsumer(consumed));

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            using ConnectHandle observerHandle = harness.Bus.ConnectConsumeMessageObserver(observer);
            var source = new ConsumerObservedMessage(NewId.NextGuid());

            await harness.Bus.PublishAsync(source, cancellationToken).WaitAsync(timeout, cancellationToken);

            ConsumerObservedMessage preConsumed = await observer.PreConsumed.WaitAsync(timeout, cancellationToken);
            ConsumerObservedMessage consumerMessage = await consumed.Task.WaitAsync(timeout, cancellationToken);
            ConsumerObservedMessage postConsumed = await observer.PostConsumed.WaitAsync(timeout, cancellationToken);

            Assert.Equal(source, preConsumed);
            Assert.Equal(source, consumerMessage);
            Assert.Equal(source, postConsumed);
            Assert.False(observer.ConsumeFaulted.IsCompleted);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVER", "mediator-request-response")]
    public async Task MediatorObservers_RecordBothSidesOfTheExactRequestResponseConversationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new MediatorTestHarness(new FakeTimeProvider(ObservationTime))
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        TestConsumeMessageObserver<ObservedRequest> typed = harness.GetConsumeObserver<ObservedRequest>();
        TestConsumeObserver untyped = harness.GetConsumeObserver();
        harness.MediatorConfiguring += configurator => configurator.Handler<ObservedRequest>(context =>
            context.RespondAsync(new ObservedResponse(context.Message.CorrelationId, $"response:{context.Message.Value}")));

        await harness.StartAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        using ConnectHandle typedHandle = harness.Mediator.ConnectConsumeMessageObserver(typed);
        using ConnectHandle untypedHandle = harness.Mediator.ConnectConsumeObserver(untyped);
        Guid correlationId = NewId.NextGuid();
        IRequestClient<ObservedRequest> client = harness.CreateRequestClient<ObservedRequest>();

        Response<ObservedResponse> response = await client.GetResponseAsync<ObservedResponse>(
            new ObservedRequest(correlationId, "request"),
            cancellationToken).WaitAsync(timeout, cancellationToken);

        ObservedRequest typedRequest = await typed.PostConsumed.WaitAsync(timeout, cancellationToken);
        IConsumedMessage<ObservedRequest> request = await untyped.Messages
            .SelectAsync<ObservedRequest>(cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        IConsumedMessage<ObservedResponse> observedResponse = await untyped.Messages
            .SelectAsync<ObservedResponse>(cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        Assert.Equal(new ObservedRequest(correlationId, "request"), typedRequest);
        Assert.Equal(typedRequest, request.Context.Message);
        Assert.Equal(correlationId, response.Message.CorrelationId);
        Assert.Equal("response:request", response.Message.Value);
        Assert.Equal(response.Message, observedResponse.Context.Message);
        Assert.Equal(request.Context.RequestId, observedResponse.Context.RequestId);
        Assert.Null(request.Exception);
        Assert.Null(observedResponse.Exception);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout, TimeProvider timeProvider) =>
        new(timeProvider, $"consume-observers-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed record ObservedMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record FailingObservedMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record ObservedRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record ObservedResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record ConsumerObservedMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class ObservedConsumer(TaskCompletionSource<ConsumerObservedMessage> consumed)
        : IConsumer<ConsumerObservedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ConsumerObservedMessage> context)
        {
            consumed.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }
}
