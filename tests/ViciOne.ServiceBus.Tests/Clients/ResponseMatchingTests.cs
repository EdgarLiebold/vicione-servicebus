using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class ResponseMatchingTests
{
    private const string UnsupportedMessage = "The requested response type is not supported.";
    private static readonly TimeSpan ResponseTaskSafetyTimeout = TimeSpan.FromSeconds(2);

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "transport-single-accepted-response")]
    public async Task TransportSingleResponse_ReturnsTheExactAcceptedMessageAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var requestSeen = new TaskCompletionSource<ConsumeContext<MatchingRequest>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        ConfigureHandler(harness, requestSeen, "transport");

        await harness.StartAsync(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("a79d067b-38d7-4429-b1a4-f36dde9cd808");
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));

            Response<AcceptedResponse> response = await client.Advanced().GetResponseAsync<AcceptedResponse>(
                new MatchingRequest(correlationId),
                cancellationToken: cancellationToken);
            ConsumeContext<MatchingRequest> request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(new AcceptedResponse(correlationId, "transport"), response.Message);
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.Equal(request.Host.MachineName, response.Host.MachineName);
            Assert.True(request.Advanced().IsResponseAccepted<AcceptedResponse>());
            Assert.False(request.Advanced().IsResponseAccepted<UnsupportedResponse>());
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "transport-unsupported-response-fault")]
    public async Task TransportUnsupportedResponse_ProducesAnExactRequestFaultAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConfigureHandler(harness, null, "unused");

        await harness.StartAsync(cancellationToken);
        try
        {
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));
            var request = new MatchingRequest(Guid.Parse("b08d8c54-3ee8-49df-aeb0-8fd33238d633"));

            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.Advanced().GetResponseAsync<UnsupportedResponse>(request, cancellationToken: cancellationToken));
            ExceptionInfo fault = Assert.Single(exception.Fault!.Exceptions);

            Assert.Equal(typeof(MatchingRequest), exception.RequestType);
            Assert.Equal(TypeCache<InvalidOperationException>.ShortName, fault.ExceptionType);
            Assert.Equal(UnsupportedMessage, fault.Message);
            Assert.Equal(request, Assert.IsAssignableFrom<Fault<MatchingRequest>>(exception.Fault).Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "transport-two-response-patterns")]
    public async Task TransportMultipleResponses_ExposeOnlyTheProducedBranchToEveryPatternFormAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConfigureHandler(harness, null, "transport-multiple");

        await harness.StartAsync(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("4f00d66b-3139-42b1-aed7-dbb80cfa8fdd");
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));

            Response<UnsupportedResponse, AcceptedResponse> response =
                await client.Advanced().GetResponseAsync<UnsupportedResponse, AcceptedResponse>(
                    new MatchingRequest(correlationId),
                    cancellationToken: cancellationToken);
            Response untyped = response;
            (Task<Response<UnsupportedResponse>> unsupportedTask, Task<Response<AcceptedResponse>> acceptedTask) = response;

            Assert.False(response.Is(out Response<UnsupportedResponse>? unsupported));
            Assert.Null(unsupported);
            Assert.True(response.Is(out Response<AcceptedResponse>? accepted));
            Assert.Equal(new AcceptedResponse(correlationId, "transport-multiple"), accepted.Message);
            Assert.True(response.Is<AcceptedResponse>(out Response<AcceptedResponse>? generic));
            Assert.Same(accepted, generic);
            Assert.Equal(accepted.Message, (await acceptedTask).Message);
            await AssertCanceledAsync(unsupportedTask, cancellationToken);

            string statementResult = untyped switch
            {
                (_, UnsupportedResponse message) => $"unsupported:{message.Reason}",
                (_, AcceptedResponse message) => $"accepted:{message.Value}",
                _ => "no-match",
            };
            Assert.Equal("accepted:transport-multiple", statementResult);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "transport-first-response-branch")]
    public async Task TransportMultipleResponses_CanSelectTheFirstDeclaredBranchAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConfigureHandler(harness, null, "transport-first");

        await harness.StartAsync(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("974652ec-49bd-4a11-a0f1-c299beab44e4");
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));

            Response<AcceptedResponse, UnsupportedResponse> response =
                await client.Advanced().GetResponseAsync<AcceptedResponse, UnsupportedResponse>(
                    new MatchingRequest(correlationId),
                    cancellationToken: cancellationToken);
            (Task<Response<AcceptedResponse>> acceptedTask, Task<Response<UnsupportedResponse>> unsupportedTask) = response;

            Assert.True(response.Is(out Response<AcceptedResponse>? accepted));
            Assert.Equal(new AcceptedResponse(correlationId, "transport-first"), accepted.Message);
            Assert.False(response.Is(out Response<UnsupportedResponse>? unsupported));
            Assert.Null(unsupported);
            Assert.Same(accepted, await acceptedTask);
            await AssertCanceledAsync(unsupportedTask, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "transport-three-response-patterns")]
    public async Task TransportThreeResponses_SelectTheProducedMiddleBranchAndCancelBothLosersAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConfigureHandler(harness, null, "transport-three");

        await harness.StartAsync(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("18e6599e-6b59-44f0-b2b5-c20bc64835c5");
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, new RequestTimeout(timeout));

            Response<UnsupportedResponse, AcceptedResponse, ThirdResponse> response =
                await client.Advanced().GetResponseAsync<UnsupportedResponse, AcceptedResponse, ThirdResponse>(
                    new MatchingRequest(correlationId),
                    cancellationToken: cancellationToken);
            (Task<Response<UnsupportedResponse>> first,
                Task<Response<AcceptedResponse>> second,
                Task<Response<ThirdResponse>> third) = response;

            Assert.False(response.Is(out Response<UnsupportedResponse>? unsupported));
            Assert.Null(unsupported);
            Assert.True(response.Is(out Response<AcceptedResponse>? accepted));
            Assert.Equal(new AcceptedResponse(correlationId, "transport-three"), accepted.Message);
            Assert.False(response.Is(out Response<ThirdResponse>? thirdResponse));
            Assert.Null(thirdResponse);
            Assert.True(response.Is<AcceptedResponse>(out Response<AcceptedResponse>? generic));
            Assert.Same(accepted, generic);
            await AssertCanceledAsync(first, cancellationToken);
            Assert.Same(accepted, await second);
            await AssertCanceledAsync(third, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "mediator-single-accepted-response")]
    public async Task MediatorSingleResponse_ReturnsTheExactAcceptedMessageAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = CreateMediator("mediator");
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        Guid correlationId = Guid.Parse("eaacba82-61f9-4808-a6e0-426cdb60b9ca");
        IRequestClient<MatchingRequest> client = mediator.CreateRequestClient<MatchingRequest>(new RequestTimeout(OperationTimeout()));

        Response<AcceptedResponse> response = await client.Advanced().GetResponseAsync<AcceptedResponse>(
            new MatchingRequest(correlationId),
            cancellationToken: cancellationToken);

        Assert.Equal(new AcceptedResponse(correlationId, "mediator"), response.Message);
        Assert.NotNull(response.RequestId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "mediator-unsupported-response-exception")]
    public async Task MediatorUnsupportedResponse_ReportsTheOriginalDispatchFailureExactlyAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = CreateMediator("unused");
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        IRequestClient<MatchingRequest> client = mediator.CreateRequestClient<MatchingRequest>(new RequestTimeout(OperationTimeout()));

        RequestException exception = await Assert.ThrowsAsync<RequestException>(() =>
            client.Advanced().GetResponseAsync<UnsupportedResponse>(
                new MatchingRequest(Guid.Parse("e456474c-051a-4dd5-b6ce-75159109f512")),
                cancellationToken: cancellationToken));
        InvalidOperationException inner = Assert.IsType<InvalidOperationException>(exception.InnerException);

        Assert.Equal(UnsupportedMessage, inner.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "mediator-two-response-patterns")]
    public async Task MediatorMultipleResponses_ExposeOnlyTheProducedBranchToEveryPatternFormAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = CreateMediator("mediator-multiple");
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        Guid correlationId = Guid.Parse("ed2ce1a5-750c-42fa-8f59-4a3126b8aa36");
        IRequestClient<MatchingRequest> client = mediator.CreateRequestClient<MatchingRequest>(new RequestTimeout(OperationTimeout()));

        Response<UnsupportedResponse, AcceptedResponse> response =
            await client.Advanced().GetResponseAsync<UnsupportedResponse, AcceptedResponse>(
                new MatchingRequest(correlationId),
                cancellationToken: cancellationToken);
        Response untyped = response;

        Assert.False(response.Is(out Response<UnsupportedResponse>? unsupported));
        Assert.Null(unsupported);
        Assert.True(response.Is(out Response<AcceptedResponse>? accepted));
        Assert.Equal(new AcceptedResponse(correlationId, "mediator-multiple"), accepted.Message);
        Assert.Equal(
            "mediator-multiple",
            untyped switch
            {
                (_, UnsupportedResponse message) => message.Reason,
                (_, AcceptedResponse message) => message.Value,
                _ => "no-match",
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "mediator-three-response-patterns")]
    public async Task MediatorThreeResponses_SelectTheProducedMiddleBranchAndCancelBothLosersAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = CreateMediator("mediator-three");
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        Guid correlationId = Guid.Parse("3773fdf6-f876-497d-a36f-ea0969489c0d");
        IRequestClient<MatchingRequest> client = mediator.CreateRequestClient<MatchingRequest>(new RequestTimeout(OperationTimeout()));

        Response<UnsupportedResponse, AcceptedResponse, ThirdResponse> response =
            await client.Advanced().GetResponseAsync<UnsupportedResponse, AcceptedResponse, ThirdResponse>(
                new MatchingRequest(correlationId),
                cancellationToken: cancellationToken);
        (Task<Response<UnsupportedResponse>> first,
            Task<Response<AcceptedResponse>> second,
            Task<Response<ThirdResponse>> third) = response;

        Assert.False(response.Is(out Response<UnsupportedResponse>? unsupported));
        Assert.Null(unsupported);
        Assert.True(response.Is(out Response<AcceptedResponse>? accepted));
        Assert.Equal(new AcceptedResponse(correlationId, "mediator-three"), accepted.Message);
        Assert.False(response.Is(out Response<ThirdResponse>? thirdResponse));
        Assert.Null(thirdResponse);
        await AssertCanceledAsync(first, cancellationToken);
        Assert.Same(accepted, await second);
        await AssertCanceledAsync(third, cancellationToken);
    }

    private static async Task AssertCanceledAsync(Task task, CancellationToken cancellationToken)
    {
        await Assert.ThrowsAsync<TaskCanceledException>(() => task.WaitAsync(
            ResponseTaskSafetyTimeout,
            cancellationToken));
    }

    private static void ConfigureHandler(
        InMemoryTestHarness harness,
        TaskCompletionSource<ConsumeContext<MatchingRequest>>? requestSeen,
        string responseValue)
    {
        harness.AddHandler<MatchingRequest>(async context =>
        {
            requestSeen?.TrySetResult(context);
            if (!context.Advanced().IsResponseAccepted<AcceptedResponse>())
                throw new InvalidOperationException(UnsupportedMessage);

            await context.RespondAsync(new AcceptedResponse(context.Message.CorrelationId, responseValue));
        });
    }

    private static IMediator CreateMediator(string responseValue) =>
        MediatorFactory.Create(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<MatchingRequest>(context =>
            {
                if (!context.Advanced().IsResponseAccepted<AcceptedResponse>())
                    throw new InvalidOperationException(UnsupportedMessage);

                return context.RespondAsync(new AcceptedResponse(context.Message.CorrelationId, responseValue));
            });
        });

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"response-matching-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record MatchingRequest(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record AcceptedResponse(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    private sealed record UnsupportedResponse(Guid CorrelationId, string Reason) : ICorrelatedBy<Guid>;

    private sealed record ThirdResponse(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;
}
