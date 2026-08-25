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
    public async Task TransportSingleResponse_ReturnsTheExactAcceptedMessage()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var requestSeen = new TaskCompletionSource<ConsumeContext<MatchingRequest>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        ConfigureHandler(harness, requestSeen, "transport");

        await harness.Start(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("a79d067b-38d7-4429-b1a4-f36dde9cd808");
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, timeout);

            Response<AcceptedResponse> response = await client.GetResponse<AcceptedResponse>(
                new MatchingRequest(correlationId),
                cancellationToken);
            ConsumeContext<MatchingRequest> request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(new AcceptedResponse(correlationId, "transport"), response.Message);
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.True(request.IsResponseAccepted<AcceptedResponse>(false));
            Assert.False(request.IsResponseAccepted<UnsupportedResponse>(false));
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "transport-unsupported-response-fault")]
    public async Task TransportUnsupportedResponse_ProducesAnExactRequestFault()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConfigureHandler(harness, null, "unused");

        await harness.Start(cancellationToken);
        try
        {
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, timeout);
            var request = new MatchingRequest(Guid.Parse("b08d8c54-3ee8-49df-aeb0-8fd33238d633"));

            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.GetResponse<UnsupportedResponse>(request, cancellationToken));
            ExceptionInfo fault = Assert.Single(exception.Fault!.Exceptions);

            Assert.Equal(TypeCache<MatchingRequest>.ShortName, exception.RequestType);
            Assert.Equal(TypeCache<InvalidOperationException>.ShortName, fault.ExceptionType);
            Assert.Equal(UnsupportedMessage, fault.Message);
            Assert.Equal(request, Assert.IsAssignableFrom<Fault<MatchingRequest>>(exception.Fault).Message);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "transport-two-response-patterns")]
    public async Task TransportMultipleResponses_ExposeOnlyTheProducedBranchToEveryPatternForm()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConfigureHandler(harness, null, "transport-multiple");

        await harness.Start(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("4f00d66b-3139-42b1-aed7-dbb80cfa8fdd");
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, timeout);

            Response<UnsupportedResponse, AcceptedResponse> response =
                await client.GetResponse<UnsupportedResponse, AcceptedResponse>(
                    new MatchingRequest(correlationId),
                    cancellationToken);
            Response untyped = response;
            (Task<Response<UnsupportedResponse>> unsupportedTask, Task<Response<AcceptedResponse>> acceptedTask) = response;

            Assert.False(response.Is(out Response<UnsupportedResponse>? unsupported));
            Assert.Null(unsupported);
            Assert.True(response.Is(out Response<AcceptedResponse>? accepted));
            Assert.Equal(new AcceptedResponse(correlationId, "transport-multiple"), accepted.Message);
            Assert.True(response.Is<AcceptedResponse>(out Response<AcceptedResponse>? generic));
            Assert.Same(accepted, generic);
            Assert.Equal(accepted.Message, (await acceptedTask).Message);
            await AssertCanceled(unsupportedTask, cancellationToken);

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
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "transport-first-response-branch")]
    public async Task TransportMultipleResponses_CanSelectTheFirstDeclaredBranch()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConfigureHandler(harness, null, "transport-first");

        await harness.Start(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("974652ec-49bd-4a11-a0f1-c299beab44e4");
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, timeout);

            Response<AcceptedResponse, UnsupportedResponse> response =
                await client.GetResponse<AcceptedResponse, UnsupportedResponse>(
                    new MatchingRequest(correlationId),
                    cancellationToken);
            (Task<Response<AcceptedResponse>> acceptedTask, Task<Response<UnsupportedResponse>> unsupportedTask) = response;

            Assert.True(response.Is(out Response<AcceptedResponse>? accepted));
            Assert.Equal(new AcceptedResponse(correlationId, "transport-first"), accepted.Message);
            Assert.False(response.Is(out Response<UnsupportedResponse>? unsupported));
            Assert.Null(unsupported);
            Assert.Same(accepted, await acceptedTask);
            await AssertCanceled(unsupportedTask, cancellationToken);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "transport-three-response-patterns")]
    public async Task TransportThreeResponses_SelectTheProducedMiddleBranchAndCancelBothLosers()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConfigureHandler(harness, null, "transport-three");

        await harness.Start(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("18e6599e-6b59-44f0-b2b5-c20bc64835c5");
            IRequestClient<MatchingRequest> client =
                harness.Bus.CreateRequestClient<MatchingRequest>(harness.InputQueueAddress, timeout);

            Response<UnsupportedResponse, AcceptedResponse, ThirdResponse> response =
                await client.GetResponse<UnsupportedResponse, AcceptedResponse, ThirdResponse>(
                    new MatchingRequest(correlationId),
                    cancellationToken);
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
            await AssertCanceled(first, cancellationToken);
            Assert.Same(accepted, await second);
            await AssertCanceled(third, cancellationToken);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "mediator-single-accepted-response")]
    public async Task MediatorSingleResponse_ReturnsTheExactAcceptedMessage()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = CreateMediator("mediator");
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        Guid correlationId = Guid.Parse("eaacba82-61f9-4808-a6e0-426cdb60b9ca");
        IRequestClient<MatchingRequest> client = mediator.CreateRequestClient<MatchingRequest>(OperationTimeout());

        Response<AcceptedResponse> response = await client.GetResponse<AcceptedResponse>(
            new MatchingRequest(correlationId),
            cancellationToken);

        Assert.Equal(new AcceptedResponse(correlationId, "mediator"), response.Message);
        Assert.NotNull(response.RequestId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "mediator-unsupported-response-exception")]
    public async Task MediatorUnsupportedResponse_ReportsTheOriginalDispatchFailureExactly()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = CreateMediator("unused");
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        IRequestClient<MatchingRequest> client = mediator.CreateRequestClient<MatchingRequest>(OperationTimeout());

        RequestException exception = await Assert.ThrowsAsync<RequestException>(() =>
            client.GetResponse<UnsupportedResponse>(
                new MatchingRequest(Guid.Parse("e456474c-051a-4dd5-b6ce-75159109f512")),
                cancellationToken));
        InvalidOperationException inner = Assert.IsType<InvalidOperationException>(exception.InnerException);

        Assert.Equal(UnsupportedMessage, inner.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-MATCHING", "mediator-two-response-patterns")]
    public async Task MediatorMultipleResponses_ExposeOnlyTheProducedBranchToEveryPatternForm()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = CreateMediator("mediator-multiple");
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        Guid correlationId = Guid.Parse("ed2ce1a5-750c-42fa-8f59-4a3126b8aa36");
        IRequestClient<MatchingRequest> client = mediator.CreateRequestClient<MatchingRequest>(OperationTimeout());

        Response<UnsupportedResponse, AcceptedResponse> response =
            await client.GetResponse<UnsupportedResponse, AcceptedResponse>(
                new MatchingRequest(correlationId),
                cancellationToken);
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
    public async Task MediatorThreeResponses_SelectTheProducedMiddleBranchAndCancelBothLosers()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = CreateMediator("mediator-three");
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        Guid correlationId = Guid.Parse("3773fdf6-f876-497d-a36f-ea0969489c0d");
        IRequestClient<MatchingRequest> client = mediator.CreateRequestClient<MatchingRequest>(OperationTimeout());

        Response<UnsupportedResponse, AcceptedResponse, ThirdResponse> response =
            await client.GetResponse<UnsupportedResponse, AcceptedResponse, ThirdResponse>(
                new MatchingRequest(correlationId),
                cancellationToken);
        (Task<Response<UnsupportedResponse>> first,
            Task<Response<AcceptedResponse>> second,
            Task<Response<ThirdResponse>> third) = response;

        Assert.False(response.Is(out Response<UnsupportedResponse>? unsupported));
        Assert.Null(unsupported);
        Assert.True(response.Is(out Response<AcceptedResponse>? accepted));
        Assert.Equal(new AcceptedResponse(correlationId, "mediator-three"), accepted.Message);
        Assert.False(response.Is(out Response<ThirdResponse>? thirdResponse));
        Assert.Null(thirdResponse);
        await AssertCanceled(first, cancellationToken);
        Assert.Same(accepted, await second);
        await AssertCanceled(third, cancellationToken);
    }

    private static async Task AssertCanceled(Task task, CancellationToken cancellationToken)
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
        harness.Handler<MatchingRequest>(async context =>
        {
            requestSeen?.TrySetResult(context);
            if (!context.IsResponseAccepted<AcceptedResponse>(false))
                throw new InvalidOperationException(UnsupportedMessage);

            await context.RespondAsync(new AcceptedResponse(context.Message.CorrelationId, responseValue));
        });
    }

    private static IMediator CreateMediator(string responseValue) =>
        Bus.Factory.CreateMediator(configurator =>
            configurator.Handler<MatchingRequest>(context =>
            {
                if (!context.IsResponseAccepted<AcceptedResponse>(false))
                    throw new InvalidOperationException(UnsupportedMessage);

                return context.RespondAsync(new AcceptedResponse(context.Message.CorrelationId, responseValue));
            }));

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"response-matching-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record MatchingRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record AcceptedResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record UnsupportedResponse(Guid CorrelationId, string Reason) : CorrelatedBy<Guid>;

    private sealed record ThirdResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
}
