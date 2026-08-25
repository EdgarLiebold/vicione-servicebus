using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class RequestClientLifecycleTests
{
    private static readonly DateTimeOffset StartTime =
        new(2032, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "transport-ttl-independent-from-client-deadline")]
    public async Task DisabledTransportTimeToLive_DoesNotDisableTheClientDeadline()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, RequestTimeout.After(m: 1));
        var request = new LifecycleRequest("no-transport-ttl");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            SendRequest,
            timeout: RequestTimeout.After(m: 1),
            requestId: Guid.Parse("91d9c54e-7818-4327-b24f-aa55ee18871d"));
        handle.TimeToLive = RequestTimeout.None;

        Task<Response<LifecycleResponse>> response = handle.GetResponse<LifecycleResponse>(true);

        Assert.Same(request, await handle.Message);
        Assert.NotNull(context.SentContext);
        Assert.Null(context.SentContext.TimeToLive);
        await timeProvider.WaitForTimerCount(1);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        RequestTimeoutException exception =
            await Assert.ThrowsAsync<RequestTimeoutException>(() => response);
        Assert.Equal(
            "Timeout waiting for response, RequestId: 91d9c54e-7818-4327-b24f-aa55ee18871d",
            exception.Message);
        return;

        async Task<LifecycleRequest> SendRequest(
            Guid _,
            IPipe<SendContext<LifecycleRequest>> pipe,
            CancellationToken cancellationToken)
        {
            var sendContext = new MessageSendContext<LifecycleRequest>(request, cancellationToken);
            await pipe.Send(sendContext);
            context.SentContext = sendContext;
            return request;
        }
    }

    [Theory]
    [InlineData(TerminalOutcome.CallerCancellation)]
    [InlineData(TerminalOutcome.ClientDeadline)]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "deterministic-cancellation-deadline-race")]
    public async Task CancellationAndDeadline_FirstTerminalOutcomeWinsExactly(TerminalOutcome firstOutcome)
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, RequestTimeout.After(m: 1));
        using var callerCancellation = new CancellationTokenSource();
        var request = new LifecycleRequest(firstOutcome.ToString());
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                var sendContext = new MessageSendContext<LifecycleRequest>(request, cancellationToken);
                await pipe.Send(sendContext);
                return request;
            },
            callerCancellation.Token,
            RequestTimeout.After(m: 1),
            Guid.Parse("2f79d87e-580b-4a1d-83c3-a4fb23ab393e"));
        Task<Response<LifecycleResponse>> response = handle.GetResponse<LifecycleResponse>(true);
        await timeProvider.WaitForTimerCount(1);
        Assert.Same(request, await handle.Message);

        if (firstOutcome == TerminalOutcome.CallerCancellation)
        {
            callerCancellation.Cancel();
            TaskCanceledException exception = await Assert.ThrowsAsync<TaskCanceledException>(() => response);
            Assert.Equal(callerCancellation.Token, exception.CancellationToken);

            timeProvider.Advance(TimeSpan.FromMinutes(1));
            TaskCanceledException repeated = await Assert.ThrowsAsync<TaskCanceledException>(() => response);
            Assert.Equal(callerCancellation.Token, repeated.CancellationToken);
        }
        else
        {
            timeProvider.Advance(TimeSpan.FromMinutes(1));
            RequestTimeoutException exception =
                await Assert.ThrowsAsync<RequestTimeoutException>(() => response);
            Assert.Equal(
                "Timeout waiting for response, RequestId: 2f79d87e-580b-4a1d-83c3-a4fb23ab393e",
                exception.Message);

            callerCancellation.Cancel();
            RequestTimeoutException repeated =
                await Assert.ThrowsAsync<RequestTimeoutException>(() => response);
            Assert.Equal(exception.Message, repeated.Message);
        }

    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "one-handler-per-response-type")]
    public async Task DuplicateResponseType_IsRejectedWithoutReplacingTheOriginalHandler()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, RequestTimeout.After(m: 1));
        var request = new LifecycleRequest("duplicate-handler");
        var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            (_, _, _) => Task.FromResult(request),
            timeout: RequestTimeout.After(m: 1));
        Task<Response<LifecycleResponse>> original = handle.GetResponse<LifecycleResponse>(false);

        RequestException exception = Assert.Throws<RequestException>(() =>
        {
            _ = handle.GetResponse<LifecycleResponse>(false);
        });

        Assert.Equal(
            $"Only one handler of type {TypeCache<LifecycleResponse>.ShortName} can be registered",
            exception.Message);
        Assert.False(original.IsCompleted);

        handle.Dispose();
        await Assert.ThrowsAsync<TaskCanceledException>(() => original);
    }

    public enum TerminalOutcome
    {
        CallerCancellation,
        ClientDeadline,
    }

    private sealed record LifecycleRequest(string Value);

    private sealed record LifecycleResponse(string Value);

    private sealed class RecordingClientFactoryContext(
        TimeProvider timeProvider,
        RequestTimeout defaultTimeout) : ClientFactoryContext
    {
        public SendContext<LifecycleRequest>? SentContext { get; set; }

        public RequestTimeout DefaultTimeout { get; } = defaultTimeout;

        public TimeProvider TimeProvider { get; } = timeProvider;

        public Uri ResponseAddress { get; } = new("loopback://localhost/response");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumePipe<T>(
            IPipe<ConsumeContext<T>> pipe,
            ConnectPipeOptions options)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectRequestPipe<T>(
            Guid requestId,
            IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(
            Uri destinationAddress,
            ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();
    }
}
