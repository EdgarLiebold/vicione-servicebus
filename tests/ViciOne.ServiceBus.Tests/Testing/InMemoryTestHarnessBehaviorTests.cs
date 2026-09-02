using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class InMemoryTestHarnessBehaviorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "send-consume-respond-observations")]
    public async Task ConsumerHarness_ObservesTheCompleteSendConsumeAndPublishPath()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<ReplyingConsumer> consumer = harness.Consumer<ReplyingConsumer>();

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new RequestMessage("request"), cancellationToken);

            Assert.True(await harness.Sent.Any<RequestMessage>(cancellationToken));
            Assert.True(await harness.Consumed.Any<RequestMessage>(cancellationToken));
            Assert.True(await consumer.Consumed.Any<RequestMessage>(cancellationToken));

            IPublishedMessage<ReplyMessage> response = await harness.Published
                .SelectAsync<ReplyMessage>(cancellationToken)
                .First();
            Assert.Equal("reply:request", response.Context.Message.Value);
            Assert.Null(response.Exception);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "in-flight-completion-without-wall-clock-delay")]
    public async Task ConsumerHarness_DoesNotReportCompletionBeforeTheConsumerCompletes()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<GatedConsumer> consumer = harness.Consumer(() => new GatedConsumer(entered, release));

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new GatedMessage(), cancellationToken);
            await entered.Task.WaitAsync(timeout, cancellationToken);

            Task<bool> observation = consumer.Consumed.Any<GatedMessage>(cancellationToken);
            Assert.False(observation.IsCompleted);

            release.TrySetResult(true);

            Assert.True(await observation.WaitAsync(timeout, cancellationToken));
            Assert.True(await harness.Sent.Any<GatedMessage>(cancellationToken));
            Assert.True(await harness.Consumed.Any<GatedMessage>(cancellationToken));
            IPublishedMessage<GatedResponse> response = await harness.Published
                .SelectAsync<GatedResponse>(cancellationToken)
                .First();
            Assert.Equal("completed", response.Context.Message.Value);
            Assert.Null(response.Exception);
        }
        finally
        {
            release.TrySetResult(true);
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "multiple-message-contracts-and-addressed-responses")]
    public async Task MultiContractConsumer_RecordsBothContractsAndTheirAddressedResponses()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<MultiContractConsumer> consumer = harness.Consumer<MultiContractConsumer>();

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(
                new FirstRequest(),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);
            await harness.InputQueueSendEndpoint.Send(
                new SecondRequest(),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);

            Assert.True(await consumer.Consumed.Any<FirstRequest>(cancellationToken));
            Assert.True(await consumer.Consumed.Any<SecondRequest>(cancellationToken));
            Assert.True(await harness.Sent.Any<FirstRequest>(cancellationToken));
            Assert.True(await harness.Consumed.Any<FirstRequest>(cancellationToken));

            ISentMessage<FirstResponse> first = await harness.Sent.SelectAsync<FirstResponse>(cancellationToken).First();
            ISentMessage<SecondResponse> second = await harness.Sent.SelectAsync<SecondResponse>(cancellationToken).First();
            Assert.Equal(harness.BusAddress, first.Context.DestinationAddress);
            Assert.Equal(harness.BusAddress, second.Context.DestinationAddress);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "interface-contract-projection")]
    public async Task InterfaceConsumer_PreservesConcreteAndInterfaceObservations()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<InterfaceConsumer> consumer = harness.Consumer<InterfaceConsumer>();

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new ConcreteRequest("value"), cancellationToken);

            Assert.True(await harness.Sent.Any<ConcreteRequest>(cancellationToken));
            Assert.True(await harness.Sent.Any<IRequestContract>(cancellationToken));
            Assert.True(await harness.Consumed.Any<IRequestContract>(cancellationToken));
            Assert.True(await consumer.Consumed.Any<IRequestContract>(cancellationToken));
            Assert.True(await harness.Published.Any<ConcreteReply>(cancellationToken));
            Assert.True(await harness.Published.Any<IReplyContract>(cancellationToken));
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "response-destination-and-observation")]
    public async Task HandlerHarness_RecordsTheHandledMessageAndExactResponseDestination()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<HandlerRequest> handler = harness.Handler<HandlerRequest>(context =>
            context.RespondAsync(new HandlerResponse(context.Message.Value + 1)));

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(
                new HandlerRequest(41),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);

            IReceivedMessage<HandlerRequest> handled = await handler.Consumed.SelectAsync(cancellationToken).First();
            ISentMessage<HandlerResponse> response = await harness.Sent.SelectAsync<HandlerResponse>(cancellationToken).First();

            Assert.Equal(41, handled.Context.Message.Value);
            Assert.Null(handled.Exception);
            Assert.Equal(42, response.Context.Message.Value);
            Assert.Equal(harness.BusAddress, response.Context.DestinationAddress);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "failure-recorded-and-propagated")]
    public async Task HandlerHarness_RecordsAndPropagatesTheExactHandlerFailure()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected handler failure");
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<FailingHandlerMessage> handler = harness.Handler<FailingHandlerMessage>(_ => Task.FromException(expected));

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new FailingHandlerMessage(), cancellationToken);

            IReceivedMessage<FailingHandlerMessage> handled = await handler.Consumed.SelectAsync(cancellationToken).First();
            IReceivedMessage<FailingHandlerMessage> pipeline = await harness.Consumed
                .SelectAsync<FailingHandlerMessage>(cancellationToken)
                .First();

            Assert.Same(expected, handled.Exception);
            Assert.Same(expected, pipeline.Exception);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "default-handler-send-observation")]
    public async Task DefaultHandlerHarness_ObservesASentMessageWithoutApplicationBehavior()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<PassiveHandlerMessage> handler = harness.Handler<PassiveHandlerMessage>();

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new PassiveHandlerMessage("sent"), cancellationToken);
            await harness.InputQueueSendEndpoint.Send(new UnconsumedPassiveMessage("also-sent"), cancellationToken);

            IReceivedMessage<PassiveHandlerMessage> handled = await handler.Consumed
                .SelectAsync(cancellationToken)
                .First();

            Assert.Equal("sent", handled.Context.Message.Value);
            Assert.Null(handled.Exception);
            Assert.True(await harness.Sent.Any<PassiveHandlerMessage>(cancellationToken));
            Assert.True(await harness.Sent.Any<UnconsumedPassiveMessage>(cancellationToken));
            Assert.True(await harness.Consumed.Any<PassiveHandlerMessage>(cancellationToken));
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "default-handler-publish-interface-projection")]
    public async Task DefaultHandlerHarness_ObservesPublishedConcreteAndInterfaceContracts()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<IPassiveHandlerContract> handler = harness.Handler<IPassiveHandlerContract>();

        await harness.Start(cancellationToken);
        try
        {
            await harness.Bus.Publish(new ConcretePassiveHandlerMessage("published"), cancellationToken);

            IReceivedMessage<IPassiveHandlerContract> handled = await handler.Consumed
                .SelectAsync(cancellationToken)
                .First();

            Assert.Equal("published", handled.Context.Message.Value);
            Assert.Null(handled.Exception);
            Assert.True(await harness.Published.Any<ConcretePassiveHandlerMessage>(cancellationToken));
            Assert.True(await harness.Published.Any<IPassiveHandlerContract>(cancellationToken));
            Assert.True(await harness.Consumed.Any<IPassiveHandlerContract>(cancellationToken));
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "failure-recorded-and-propagated")]
    public async Task ConsumerHarness_RecordsAndPropagatesTheExactConsumerFailure()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected consumer failure");
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<FailingConsumer> consumer = harness.Consumer(() => new FailingConsumer(expected));

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new FailingConsumerMessage(), cancellationToken);

            IReceivedMessage<FailingConsumerMessage> consumerObservation = await consumer.Consumed
                .SelectAsync<FailingConsumerMessage>(cancellationToken)
                .First();
            IReceivedMessage<FailingConsumerMessage> pipelineObservation = await harness.Consumed
                .SelectAsync<FailingConsumerMessage>(cancellationToken)
                .First();

            Assert.Same(expected, consumerObservation.Exception);
            Assert.Same(expected, pipelineObservation.Exception);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "named-endpoint-configuration")]
    public async Task NamedConsumerHarness_ConsumesOnlyFromItsDedicatedEndpoint()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string queueName = "dedicated-consumer";
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<NamedConsumer> consumer = harness.Consumer<NamedConsumer>(queueName);

        await harness.Start(cancellationToken);
        try
        {
            ISendEndpoint endpoint = await harness.GetSendEndpoint(new Uri(harness.BaseAddress, queueName));
            await endpoint.Send(new NamedConsumerMessage("expected"), cancellationToken);

            IReceivedMessage<NamedConsumerMessage> received = await consumer.Consumed
                .SelectAsync<NamedConsumerMessage>(cancellationToken)
                .First();

            Assert.Equal("expected", received.Context.Message.Value);
            Assert.Equal(new Uri(harness.BaseAddress, queueName), received.Context.ReceiveContext.InputAddress);
            Assert.Null(received.Exception);
        }
        finally
        {
            await harness.Stop();
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
        public Task Consume(ConsumeContext<RequestMessage> context) =>
            context.RespondAsync(new ReplyMessage($"reply:{context.Message.Value}"));
    }

    private sealed record GatedMessage;

    private sealed class GatedConsumer(
        TaskCompletionSource<bool> entered,
        TaskCompletionSource<bool> release) : IConsumer<GatedMessage>
    {
        public async Task Consume(ConsumeContext<GatedMessage> context)
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
        public Task Consume(ConsumeContext<FirstRequest> context) => context.RespondAsync(new FirstResponse());

        public Task Consume(ConsumeContext<SecondRequest> context) => context.RespondAsync(new SecondResponse());
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
        public Task Consume(ConsumeContext<IRequestContract> context) =>
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
        public Task Consume(ConsumeContext<FailingConsumerMessage> context) => Task.FromException(exception);
    }

    private sealed record NamedConsumerMessage(string Value);

    private sealed class NamedConsumer : IConsumer<NamedConsumerMessage>
    {
        public Task Consume(ConsumeContext<NamedConsumerMessage> context) => Task.CompletedTask;
    }
}
