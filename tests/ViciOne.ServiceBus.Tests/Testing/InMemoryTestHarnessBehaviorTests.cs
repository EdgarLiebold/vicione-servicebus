using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class InMemoryTestHarnessBehaviorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "send-consume-respond-observations")]
    public async Task ConsumerHarness_ObservesTheCompleteSendConsumeAndPublishPathAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<ReplyingConsumer> consumer = harness.Consumer<ReplyingConsumer>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new RequestMessage("request"), cancellationToken);

            Assert.True(await harness.Sent.AnyAsync<RequestMessage>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<RequestMessage>(cancellationToken));
            Assert.True(await consumer.Consumed.AnyAsync<RequestMessage>(cancellationToken));

            IPublishedMessage<ReplyMessage> response = await harness.Published
                .SelectAsync<ReplyMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("reply:request", response.Context.Message.Value);
            Assert.Null(response.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "in-flight-completion-without-wall-clock-delay")]
    public async Task ConsumerHarness_DoesNotReportCompletionBeforeTheConsumerCompletesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<GatedConsumer> consumer = harness.Consumer(() => new GatedConsumer(entered, release));

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new GatedMessage(), cancellationToken);
            await entered.Task.WaitAsync(timeout, cancellationToken);

            Task<bool> observation = consumer.Consumed.AnyAsync<GatedMessage>(cancellationToken);
            Assert.False(observation.IsCompleted);

            release.TrySetResult(true);

            Assert.True(await observation.WaitAsync(timeout, cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<GatedMessage>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<GatedMessage>(cancellationToken));
            IPublishedMessage<GatedResponse> response = await harness.Published
                .SelectAsync<GatedResponse>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("completed", response.Context.Message.Value);
            Assert.Null(response.Exception);
        }
        finally
        {
            release.TrySetResult(true);
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "multiple-message-contracts-and-addressed-responses")]
    public async Task MultiContractConsumer_RecordsBothContractsAndTheirAddressedResponsesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<MultiContractConsumer> consumer = harness.Consumer<MultiContractConsumer>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                new FirstRequest(),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(
                new SecondRequest(),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);

            Assert.True(await consumer.Consumed.AnyAsync<FirstRequest>(cancellationToken));
            Assert.True(await consumer.Consumed.AnyAsync<SecondRequest>(cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<FirstRequest>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<FirstRequest>(cancellationToken));

            ISentMessage<FirstResponse> first = await harness.Sent.SelectAsync<FirstResponse>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            ISentMessage<SecondResponse> second = await harness.Sent.SelectAsync<SecondResponse>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(harness.BusAddress, first.Context.DestinationAddress);
            Assert.Equal(harness.BusAddress, second.Context.DestinationAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "interface-contract-projection")]
    public async Task InterfaceConsumer_PreservesConcreteAndInterfaceObservationsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<InterfaceConsumer> consumer = harness.Consumer<InterfaceConsumer>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new ConcreteRequest("value"), cancellationToken);

            Assert.True(await harness.Sent.AnyAsync<ConcreteRequest>(cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<IRequestContract>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<IRequestContract>(cancellationToken));
            Assert.True(await consumer.Consumed.AnyAsync<IRequestContract>(cancellationToken));
            Assert.True(await harness.Published.AnyAsync<ConcreteReply>(cancellationToken));
            Assert.True(await harness.Published.AnyAsync<IReplyContract>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "response-destination-and-observation")]
    public async Task HandlerHarness_RecordsTheHandledMessageAndExactResponseDestinationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<HandlerRequest> handler = harness.Handler<HandlerRequest>(context =>
            context.RespondAsync(new HandlerResponse(context.Message.Value + 1)));

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                new HandlerRequest(41),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);

            IReceivedMessage<HandlerRequest> handled = await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            ISentMessage<HandlerResponse> response = await harness.Sent.SelectAsync<HandlerResponse>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(41, handled.Context.Message.Value);
            Assert.Null(handled.Exception);
            Assert.Equal(42, response.Context.Message.Value);
            Assert.Equal(harness.BusAddress, response.Context.DestinationAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "failure-recorded-and-propagated")]
    public async Task HandlerHarness_RecordsAndPropagatesTheExactHandlerFailureAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected handler failure");
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<FailingHandlerMessage> handler = harness.Handler<FailingHandlerMessage>(_ => Task.FromException(expected));

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new FailingHandlerMessage(), cancellationToken);

            IReceivedMessage<FailingHandlerMessage> handled = await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IReceivedMessage<FailingHandlerMessage> pipeline = await harness.Consumed
                .SelectAsync<FailingHandlerMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Same(expected, handled.Exception);
            Assert.Same(expected, pipeline.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "default-handler-send-observation")]
    public async Task DefaultHandlerHarness_ObservesASentMessageWithoutApplicationBehaviorAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<PassiveHandlerMessage> handler = harness.Handler<PassiveHandlerMessage>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new PassiveHandlerMessage("sent"), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new UnconsumedPassiveMessage("also-sent"), cancellationToken);

            IReceivedMessage<PassiveHandlerMessage> handled = await handler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("sent", handled.Context.Message.Value);
            Assert.Null(handled.Exception);
            Assert.True(await harness.Sent.AnyAsync<PassiveHandlerMessage>(cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<UnconsumedPassiveMessage>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<PassiveHandlerMessage>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "default-handler-publish-interface-projection")]
    public async Task DefaultHandlerHarness_ObservesPublishedConcreteAndInterfaceContractsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<IPassiveHandlerContract> handler = harness.Handler<IPassiveHandlerContract>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.Bus.PublishAsync(new ConcretePassiveHandlerMessage("published"), cancellationToken);

            IReceivedMessage<IPassiveHandlerContract> handled = await handler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("published", handled.Context.Message.Value);
            Assert.Null(handled.Exception);
            Assert.True(await harness.Published.AnyAsync<ConcretePassiveHandlerMessage>(cancellationToken));
            Assert.True(await harness.Published.AnyAsync<IPassiveHandlerContract>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<IPassiveHandlerContract>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "failure-recorded-and-propagated")]
    public async Task ConsumerHarness_RecordsAndPropagatesTheExactConsumerFailureAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected consumer failure");
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<FailingConsumer> consumer = harness.Consumer(() => new FailingConsumer(expected));

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new FailingConsumerMessage(), cancellationToken);

            IReceivedMessage<FailingConsumerMessage> consumerObservation = await consumer.Consumed
                .SelectAsync<FailingConsumerMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IReceivedMessage<FailingConsumerMessage> pipelineObservation = await harness.Consumed
                .SelectAsync<FailingConsumerMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Same(expected, consumerObservation.Exception);
            Assert.Same(expected, pipelineObservation.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "named-endpoint-configuration")]
    public async Task NamedConsumerHarness_ConsumesOnlyFromItsDedicatedEndpointAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string queueName = "dedicated-consumer";
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<NamedConsumer> consumer = harness.Consumer<NamedConsumer>(queueName);

        await harness.StartAsync(cancellationToken);
        try
        {
            ISendEndpoint endpoint = await harness.GetSendEndpointAsync(new Uri(harness.BaseAddress, queueName), TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new NamedConsumerMessage("expected"), cancellationToken);

            IReceivedMessage<NamedConsumerMessage> received = await consumer.Consumed
                .SelectAsync<NamedConsumerMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("expected", received.Context.Message.Value);
            Assert.Equal(new Uri(harness.BaseAddress, queueName), received.Context.Advanced().ReceiveContext.InputAddress);
            Assert.Null(received.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"testing-harness-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed record RequestMessage(string Value);

    private sealed record ReplyMessage(string Value);

    private sealed class ReplyingConsumer : IConsumer<RequestMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RequestMessage> context) =>
            context.RespondAsync(new ReplyMessage($"reply:{context.Message.Value}"));
    }

    private sealed record GatedMessage;

    private sealed class GatedConsumer(
        TaskCompletionSource<bool> entered,
        TaskCompletionSource<bool> release) : IConsumer<GatedMessage>
    {
        public async Task ConsumeAsync(ConsumeContext<GatedMessage> context)
        {
            entered.TrySetResult(true);
            await release.Task.WaitAsync(context.CancellationToken);
            await context.RespondAsync(new GatedResponse("completed"));
        }
    }

    private sealed record GatedResponse(string Value);

    private sealed record FirstRequest;

    private sealed record FirstResponse;

    private sealed record SecondRequest;

    private sealed record SecondResponse;

    private sealed class MultiContractConsumer :
        IConsumer<FirstRequest>,
        IConsumer<SecondRequest>
    {
        public Task ConsumeAsync(ConsumeContext<FirstRequest> context) => context.RespondAsync(new FirstResponse());

        public Task ConsumeAsync(ConsumeContext<SecondRequest> context) => context.RespondAsync(new SecondResponse());
    }

    public interface IRequestContract
    {
        string Value { get; }
    }

    private sealed record ConcreteRequest(string Value) : IRequestContract;

    public interface IReplyContract
    {
        string Value { get; }
    }

    private sealed record ConcreteReply(string Value) : IReplyContract;

    private sealed class InterfaceConsumer : IConsumer<IRequestContract>
    {
        public Task ConsumeAsync(ConsumeContext<IRequestContract> context) =>
            context.RespondAsync(new ConcreteReply(context.Message.Value));
    }

    private sealed record HandlerRequest(int Value);

    private sealed record HandlerResponse(int Value);

    private sealed record FailingHandlerMessage;

    private sealed record PassiveHandlerMessage(string Value);

    private sealed record UnconsumedPassiveMessage(string Value);

    public interface IPassiveHandlerContract
    {
        string Value { get; }
    }

    private sealed record ConcretePassiveHandlerMessage(string Value) : IPassiveHandlerContract;

    private sealed record FailingConsumerMessage;

    private sealed class FailingConsumer(Exception exception) : IConsumer<FailingConsumerMessage>
    {
        public Task ConsumeAsync(ConsumeContext<FailingConsumerMessage> context) => Task.FromException(exception);
    }

    private sealed record NamedConsumerMessage(string Value);

    private sealed class NamedConsumer : IConsumer<NamedConsumerMessage>
    {
        public Task ConsumeAsync(ConsumeContext<NamedConsumerMessage> context) => Task.CompletedTask;
    }
}
