using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contexts;

public sealed class MessageContextFlowTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTEXT", "addressed-send-envelope")]
    public async Task AddressedSend_PreservesTheCompleteExpectedContext()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<ContextMessage> handler = harness.Handler<ContextMessage>();
        Guid correlationId = Guid.Parse("18c63967-6284-4db6-97ac-720fd842ff7f");

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(
                new ContextMessage(correlationId, "sent"),
                context => context.Headers.Set("One", "1"),
                cancellationToken);
            ConsumeContext<ContextMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).First()).Context;

            Assert.Equal(new ContextMessage(correlationId, "sent"), context.Message);
            Assert.NotNull(context.MessageId);
            Assert.NotEqual(Guid.Empty, context.MessageId);
            Assert.Null(context.RequestId);
            Assert.Equal(correlationId, context.CorrelationId);
            Assert.NotNull(context.ConversationId);
            Assert.NotEqual(Guid.Empty, context.ConversationId);
            Assert.Null(context.InitiatorId);
            Assert.Equal(harness.BusAddress, context.SourceAddress);
            Assert.Equal(harness.InputQueueAddress, context.DestinationAddress);
            Assert.Null(context.ResponseAddress);
            Assert.Null(context.FaultAddress);
            Assert.True(context.Headers.TryGetHeader("One", out object? header));
            Assert.Equal("1", header);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CONTEXT", "addressed-request-response-envelope")]
    public async Task AddressedRequestAndResponse_PreserveCausationAddressesAndAcceptedTypes()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var requestSeen = new TaskCompletionSource<ConsumeContext<RequestMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Handler<RequestMessage>(async context =>
        {
            requestSeen.TrySetResult(context);
            await context.RespondAsync(new AcceptedResponse(context.Message.CorrelationId, "accepted"));
        });
        Guid correlationId = Guid.Parse("fba2a449-ebd3-435c-a6e5-4725a6cc13e6");
        Guid conversationId = Guid.Parse("45e030ae-2d81-4bf1-9860-77bb17299366");

        await harness.Start(cancellationToken);
        try
        {
            Task<ConsumeContext<AcceptedResponse>> subscriber = harness.SubscribeHandler<AcceptedResponse>();
            IRequestClient<RequestMessage> client =
                harness.Bus.CreateRequestClient<RequestMessage>(harness.InputQueueAddress, timeout);
            Response<AcceptedResponse> response = await client.GetResponse<AcceptedResponse>(
                new RequestMessage(correlationId, "request"),
                handle => handle.UseExecute(context => context.ConversationId = conversationId),
                cancellationToken);
            ConsumeContext<RequestMessage> request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<AcceptedResponse> subscribedResponse =
                await subscriber.WaitAsync(timeout, cancellationToken);
            IList<string> acceptedTypes = request.GetHeader<IList<string>>(MessageHeaders.Request.Accept)!;

            Assert.Equal(correlationId, request.CorrelationId);
            Assert.NotNull(request.RequestId);
            Assert.NotEqual(Guid.Empty, request.RequestId);
            Assert.Equal(conversationId, request.ConversationId);
            Assert.Equal(harness.BusAddress, request.SourceAddress);
            Assert.Equal(harness.InputQueueAddress, request.DestinationAddress);
            Assert.Equal(harness.BusAddress, request.ResponseAddress);
            Assert.Null(request.FaultAddress);
            Assert.Equal([MessageUrn.ForTypeString<AcceptedResponse>()], acceptedTypes);

            Assert.Equal(new AcceptedResponse(correlationId, "accepted"), response.Message);
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.Equal(correlationId, response.CorrelationId);
            Assert.Equal(conversationId, response.ConversationId);
            Assert.Equal(harness.InputQueueAddress, response.SourceAddress);
            Assert.Equal(harness.BusAddress, response.DestinationAddress);
            Assert.Null(response.ResponseAddress);
            Assert.Null(response.FaultAddress);

            Assert.Equal(response.Message, subscribedResponse.Message);
            Assert.Equal(response.RequestId, subscribedResponse.RequestId);
            Assert.Equal(response.CorrelationId, subscribedResponse.CorrelationId);
            Assert.Equal(response.ConversationId, subscribedResponse.ConversationId);
            Assert.Equal(response.SourceAddress, subscribedResponse.SourceAddress);
            Assert.Equal(response.DestinationAddress, subscribedResponse.DestinationAddress);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CONTEXT", "published-request-response")]
    public async Task PublishedRequest_ReceivesTheExactHandlerResponse()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var requestSeen = new TaskCompletionSource<ConsumeContext<RequestMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Handler<RequestMessage>(async context =>
        {
            requestSeen.TrySetResult(context);
            await context.RespondAsync(new AcceptedResponse(context.Message.CorrelationId, "published"));
        });
        Guid correlationId = Guid.Parse("12ad3fe6-2a11-4eb8-b4e7-94897a3eac6f");

        await harness.Start(cancellationToken);
        try
        {
            IRequestClient<RequestMessage> client = harness.Bus.CreateRequestClient<RequestMessage>(timeout);
            Response<AcceptedResponse> response = await client.GetResponse<AcceptedResponse>(
                new RequestMessage(correlationId, "publish"),
                cancellationToken);
            ConsumeContext<RequestMessage> request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(new AcceptedResponse(correlationId, "published"), response.Message);
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.Equal(correlationId, response.CorrelationId);
            Assert.Equal(request.ConversationId, response.ConversationId);
            Assert.Equal(harness.InputQueueAddress, response.SourceAddress);
            Assert.Equal(request.ResponseAddress, response.DestinationAddress);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CONTEXT", "multiple-response-terminal-branch")]
    public async Task MultipleAcceptedResponses_CompleteOnlyTheProducedBranch()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        harness.Handler<RequestMessage>(context =>
            context.RespondAsync(new RejectedResponse(context.Message.CorrelationId, "not-supported")));
        Guid correlationId = Guid.Parse("22d12caf-c8ec-461b-a4c4-6acfa7e02a51");

        await harness.Start(cancellationToken);
        try
        {
            IRequestClient<RequestMessage> client =
                harness.Bus.CreateRequestClient<RequestMessage>(harness.InputQueueAddress, timeout);
            Response<AcceptedResponse, RejectedResponse> response =
                await client.GetResponse<AcceptedResponse, RejectedResponse>(
                    new RequestMessage(correlationId, "two-types"),
                    cancellationToken);
            (Task<Response<AcceptedResponse>> acceptedTask, Task<Response<RejectedResponse>> rejectedTask) = response;

            Assert.False(response.Is(out Response<AcceptedResponse>? accepted));
            Assert.Null(accepted);
            Assert.True(response.Is(out Response<RejectedResponse>? rejected));
            Assert.Equal(new RejectedResponse(correlationId, "not-supported"), rejected.Message);
            Assert.Equal(rejected.Message, (await rejectedTask).Message);
            await Assert.ThrowsAsync<TaskCanceledException>(() => acceptedTask);

            ISendEndpoint busEndpoint = await harness.Bus.GetSendEndpoint(harness.BusAddress);
            await busEndpoint.Send(new AcceptedResponse(correlationId, "late"), cancellationToken);
            await Assert.ThrowsAsync<TaskCanceledException>(() => acceptedTask);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CONTEXT", "unanswered-request-timeout")]
    public async Task UnansweredRequest_ThrowsTheExactTimeoutForItsRequestIdentifier()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var timeProvider = new ObservableTimeProvider(
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));

        await harness.Start(cancellationToken);
        try
        {
            var clientFactory = new ClientFactory(new BusClientFactoryContext(
                harness.Bus,
                RequestTimeout.After(m: 1),
                timeProvider));
            IRequestClient<RequestMessage> client = clientFactory.CreateRequestClient<RequestMessage>(
                harness.InputQueueAddress,
                RequestTimeout.After(m: 1));
            var requestMessage = new RequestMessage(
                Guid.Parse("2f651398-caa1-45c6-b23d-48133e00709f"),
                "unanswered");
            using RequestHandle<RequestMessage> request = client.Create(
                requestMessage,
                cancellationToken);
            var concreteRequest = Assert.IsType<ClientRequestHandle<RequestMessage>>(request);
            Task<Response<AcceptedResponse>> response = request.GetResponse<AcceptedResponse>();

            // Message represents completion of the request send callback. Establish that causal
            // boundary before advancing the virtual timeout; otherwise the timeout may race the
            // send callback and fault Message even though this test is about response timeout.
            Assert.Same(requestMessage, await request.Message.WaitAsync(timeout, cancellationToken));

            await timeProvider.WaitForTimerCount(1).WaitAsync(cancellationToken);
            timeProvider.Advance(TimeSpan.FromMinutes(1));

            RequestTimeoutException exception =
                await Assert.ThrowsAsync<RequestTimeoutException>(() =>
                    response.WaitAsync(timeout, cancellationToken));

            Assert.Equal(
                $"Timeout waiting for response, RequestId: {concreteRequest.RequestId}",
                exception.Message);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CONTEXT", "caller-cancellation")]
    public async Task CanceledRequest_ReportsCancellationInsteadOfTimeout()
    {
        TimeSpan timeout = OperationTimeout();
        using var harness = CreateHarness(timeout);
        await harness.Start(TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        try
        {
            IRequestClient<RequestMessage> client =
                harness.Bus.CreateRequestClient<RequestMessage>(harness.InputQueueAddress, timeout);

            TaskCanceledException exception = await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetResponse<AcceptedResponse>(
                new RequestMessage(Guid.Parse("acff2021-8c44-440d-9d8d-c77f172ed296"), "canceled"),
                cancellation.Token));

            Assert.Equal(cancellation.Token, exception.CancellationToken);
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
        new($"message-context-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed record ContextMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record RequestMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record AcceptedResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record RejectedResponse(Guid CorrelationId, string Reason) : CorrelatedBy<Guid>;
}
