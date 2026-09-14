using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Clients.Requests;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class RequestClientLifecycleTests
{
    private static readonly DateTimeOffset StartTime =
        new(2032, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "transport-ttl-independent-from-client-deadline")]
    public async Task DisabledTransportTimeToLive_DoesNotDisableTheClientDeadlineAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("no-transport-ttl");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            SendRequestAsync,
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)),
            requestId: Guid.Parse("91d9c54e-7818-4327-b24f-aa55ee18871d"));
        handle.TimeToLive = RequestTimeout.None;

        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, TestContext.Current.CancellationToken);

        Assert.Same(request, await handle.Message);
        Assert.NotNull(context.SentContext);
        Assert.Null(context.SentContext.TimeToLive);
        await timeProvider.WaitForTimerCountAsync(1);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        RequestTimeoutException exception =
            await Assert.ThrowsAsync<RequestTimeoutException>(() => response);
        Assert.Equal(
            "Timeout waiting for response, RequestId: 91d9c54e-7818-4327-b24f-aa55ee18871d",
            exception.Message);
        return;

        async Task<LifecycleRequest> SendRequestAsync(
            Guid _,
            IPipe<SendContext<LifecycleRequest>> pipe,
            CancellationToken cancellationToken)
        {
            var sendContext = new MessageSendContext<LifecycleRequest>(request, cancellationToken);
            await pipe.SendAsync(sendContext);
            context.SentContext = sendContext;
            return request;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "deadline-derived-transport-lifetime-requires-deadline")]
    public void DeadlineDerivedTransportLifetime_RequiresAnAbsoluteDeadline()
    {
        var context = new RecordingClientFactoryContext(
            TimeProvider.System,
            new RequestTimeout(TimeSpan.FromMinutes(1)));

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ClientRequestHandle<LifecycleRequest>(
                context,
                (_, _, _) => Task.FromResult(new LifecycleRequest("unexpected")),
                useDeadlineAsTimeToLive: true));

        Assert.Equal("useDeadlineAsTimeToLive", exception.ParamName);
        Assert.StartsWith(
            "A transport lifetime derived from a deadline requires an absolute deadline.",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "second-send-pipeline-invocation-rejects-and-releases-timer")]
    public async Task SecondSendPipelineInvocation_DisposesItsUnownedTimerAndFailsExplicitlyAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(
            timeProvider,
            new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("one-send-pipeline");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                return request;
            });
        Task<Response<LifecycleResponse>> response =
            handle.GetResponseAsync<LifecycleResponse>(readyToSend: true, CancellationToken.None);

        Assert.Same(request, await handle.Message);
        Assert.Equal(1, timeProvider.TimerCount);
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        var duplicateContext = new MessageSendContext<LifecycleRequest>(request);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handle.SendAsync(duplicateContext));

        Assert.Equal("The request timeout timer was initialized more than once.", exception.Message);
        Assert.Equal(2, timeProvider.TimerCount);
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        handle.Dispose();
        await Assert.ThrowsAsync<TaskCanceledException>(() => response);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
    }

    [Theory]
    [InlineData(TerminalOutcome.CallerCancellation)]
    [InlineData(TerminalOutcome.ClientDeadline)]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "deterministic-cancellation-deadline-race")]
    public async Task CancellationAndDeadline_FirstTerminalOutcomeWinsExactlyAsync(TerminalOutcome firstOutcome)
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        using var callerCancellation = new CancellationTokenSource();
        var request = new LifecycleRequest(firstOutcome.ToString());
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                var sendContext = new MessageSendContext<LifecycleRequest>(request, cancellationToken);
                await pipe.SendAsync(sendContext);
                return request;
            },
            callerCancellation.Token,
            new RequestTimeout(TimeSpan.FromMinutes(1)),
            Guid.Parse("2f79d87e-580b-4a1d-83c3-a4fb23ab393e"));
        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1);
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
    public async Task DuplicateResponseType_IsRejectedWithoutReplacingTheOriginalHandlerAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("duplicate-handler");
        var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            (_, _, _) => Task.FromResult(request),
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)));
        Task<Response<LifecycleResponse>> original = handle.GetResponseAsync<LifecycleResponse>(false, TestContext.Current.CancellationToken);

        RequestException exception = Assert.Throws<RequestException>(() =>
        {
            _ = handle.GetResponseAsync<LifecycleResponse>(false, TestContext.Current.CancellationToken);
        });

        Assert.Equal(
            $"Only one handler of type {TypeCache<LifecycleResponse>.ShortName} can be registered",
            exception.Message);
        Assert.False(original.IsCompleted);

        handle.Dispose();
        await Assert.ThrowsAsync<TaskCanceledException>(() => original);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "pre-canceled-handle-never-starts-send")]
    public async Task PreCanceledHandle_PreservesTheCallerTokenWithoutStartingTheSendAsync()
    {
        var context = new RecordingClientFactoryContext(TimeProvider.System, new RequestTimeout(TimeSpan.FromMinutes(1)));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var sendCount = 0;
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            (_, _, _) =>
            {
                Interlocked.Increment(ref sendCount);
                return Task.FromResult(new LifecycleRequest("unexpected"));
            },
            cancellation.Token,
            new RequestTimeout(TimeSpan.FromMinutes(1)));

        TaskCanceledException responseCancellation = await Assert.ThrowsAsync<TaskCanceledException>(
            () => handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None));
        TaskCanceledException messageCancellation = await Assert.ThrowsAsync<TaskCanceledException>(
            () => handle.Message);

        Assert.Equal(cancellation.Token, responseCancellation.CancellationToken);
        Assert.Equal(cancellation.Token, messageCancellation.CancellationToken);
        Assert.Equal(0, sendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "response-wait-token-remains-effective")]
    public async Task ResponseWaitCancellation_PreservesItsOwnTokenAfterTheRequestWasSentAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("separate-response-wait");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                return request;
            },
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)));
        using var responseCancellation = new CancellationTokenSource();
        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, responseCancellation.Token);
        await timeProvider.WaitForTimerCountAsync(1);

        responseCancellation.Cancel();

        TaskCanceledException actual = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(responseCancellation.Token, actual.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "cancel-during-send-cannot-leak-late-timer")]
    public async Task CancellationDuringTheSendPipeline_DisposesATimeoutTimerCreatedAfterCancellationAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("late-timer");
        using var cancellation = new CancellationTokenSource();
        var pipeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePipe = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                return request;
            },
            cancellation.Token,
            new RequestTimeout(TimeSpan.FromMinutes(1)));
        handle.UseExecuteAwaited(async _ =>
        {
            pipeEntered.TrySetResult();
            await releasePipe.Task;
        });
        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None);
        await pipeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        cancellation.Cancel();
        try
        {
            TaskCanceledException actual = await Assert.ThrowsAsync<TaskCanceledException>(() =>
                response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, actual.CancellationToken);
        }
        finally
        {
            releasePipe.TrySetResult();
        }
        await timeProvider.WaitForTimerCountAsync(1);

        Assert.Equal(1, timeProvider.TimerCount);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "cleanup-and-diagnostic-failures-do-not-stop-cancel")]
    public async Task Cancellation_WhenTimerHandlerAndDiagnosticCleanupFail_StillDisconnectsEveryHandlerAsync()
    {
        var timeProvider = new FaultingTimerTimeProvider();
        var context = new CleanupFailureClientFactoryContext(timeProvider);
        var logger = new ThrowingLogger();
        var previousLogContext = LogContext.Current;
        LogContext.ConfigureCurrentLogContext(logger);
        using var cancellation = new CancellationTokenSource();
        var request = new LifecycleRequest("cleanup-failures");

        try
        {
            using var handle = new ClientRequestHandle<LifecycleRequest>(
                context,
                async (_, pipe, cancellationToken) =>
                {
                    await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                    return request;
                },
                cancellation.Token,
                new RequestTimeout(TimeSpan.FromMinutes(1)));
            Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None);
            await timeProvider.TimerCreated.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            cancellation.Cancel();

            TaskCanceledException actual = await Assert.ThrowsAsync<TaskCanceledException>(() =>
                response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            await context.AllDisconnectsAttempted.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            Assert.Equal(cancellation.Token, actual.CancellationToken);
            Assert.Equal(1, timeProvider.Timer.DisposeCount);
            Assert.Equal(2, context.DisconnectCount);
            Assert.Equal(2, logger.CallCount);
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "terminal-cleanup-releases-send-cancellation-source")]
    public async Task DisposingACompletedHandle_ReleasesItsSendCancellationSourceAsync()
    {
        var context = new RecordingClientFactoryContext(TimeProvider.System, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("dispose-cancellation-source");
        var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            (_, _, _) => Task.FromResult(request),
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)));
        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None);
        Assert.Same(request, await handle.Message);

        FieldInfo? sourceField = typeof(ClientRequestHandle<LifecycleRequest>).GetField(
            "_cancellationTokenSource",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(sourceField);
        var source = Assert.IsType<CancellationTokenSource>(sourceField.GetValue(handle));

        handle.Dispose();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        await AssertSourceIsDisposedAsync(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "terminal-cleanup-does-not-capture-synchronization-context")]
    public async Task RequestFailure_CompletesWithoutPumpingTheAmbientSynchronizationContextAsync()
    {
        var synchronizationContext = new QueuedSynchronizationContext();
        SynchronizationContext? previous = SynchronizationContext.Current;
        ClientRequestHandle<LifecycleRequest> handle;
        Task<Response<LifecycleResponse>> response;

        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        try
        {
            handle = new ClientRequestHandle<LifecycleRequest>(
                new RecordingClientFactoryContext(TimeProvider.System, new RequestTimeout(TimeSpan.FromMinutes(1))),
                (_, _, _) => Task.FromException<LifecycleRequest>(new CleanupFailureException("Request send failed.")));
            response = handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        try
        {
            Task timeout = Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Task completed = await Task.WhenAny(response, synchronizationContext.Posted, timeout);

            Assert.Same(response, completed);
            Exception? exception = await Record.ExceptionAsync(() => response);
            Assert.NotNull(exception);
            Assert.False(synchronizationContext.HasPostedCallbacks);
        }
        finally
        {
            synchronizationContext.RunPostedCallbacks();
            handle.Dispose();

            try
            {
                await response;
            }
            catch (Exception)
            {
                // The send failure is the terminal outcome under test.
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "fault-observed-before-response-wins-terminal-race")]
    public async Task FaultObservedBeforeAResponse_RemainsTheExactTerminalOutcomeAsync()
    {
        var timeProvider = new BlockingTimerDisposalTimeProvider();
        var context = new DirectResponseClientFactoryContext(timeProvider);
        var request = new LifecycleRequest("fault-first");
        var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                return request;
            },
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)));
        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(
            readyToSend: true,
            CancellationToken.None);
        var fault = new FaultEvent<LifecycleRequest>(
            request,
            null,
            HostMetadataCache.Empty,
            new CleanupFailureException("The request failed."),
            []);

        try
        {
            Assert.Same(request, await handle.Message);

            await context.DeliverAsync<Fault<LifecycleRequest>>(fault);
            await timeProvider.Timer.DisposalStarted.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken);
            await context.DeliverAsync(new LifecycleResponse("too-late"));

            timeProvider.Timer.ReleaseDisposal();

            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Same(fault, exception.Fault);
            Assert.Equal(typeof(LifecycleRequest), exception.RequestType);
        }
        finally
        {
            timeProvider.Timer.ReleaseDisposal();
            handle.Dispose();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "response-observed-before-fault-wins-terminal-race")]
    public async Task ResponseObservedBeforeAFault_RemainsTheExactTerminalOutcomeAsync()
    {
        var context = new DirectResponseClientFactoryContext(TimeProvider.System);
        var request = new LifecycleRequest("response-first");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                return request;
            },
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)));
        Task<Response<AlternateLifecycleResponse>> losingResponse =
            handle.GetResponseAsync<AlternateLifecycleResponse>(readyToSend: false, CancellationToken.None);
        Task<Response<LifecycleResponse>> winningResponse =
            handle.GetResponseAsync<LifecycleResponse>(readyToSend: true, CancellationToken.None);
        var response = new LifecycleResponse("first-arrival");
        var fault = new FaultEvent<LifecycleRequest>(
            request,
            null,
            HostMetadataCache.Empty,
            new CleanupFailureException("The late fault must not replace the response."),
            []);

        Assert.Same(request, await handle.Message);
        await context.DeliverAsync(response);
        Assert.Same(response, (await winningResponse).Message);

        await context.DeliverAsync<Fault<LifecycleRequest>>(fault);
        handle.Dispose();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            losingResponse.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "first-response-is-the-only-successful-branch")]
    public async Task FirstResponseContractToArrive_IsTheOnlySuccessfulBranchAsync()
    {
        var context = new DirectResponseClientFactoryContext(TimeProvider.System);
        var request = new LifecycleRequest("single-winner");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                return request;
            },
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)));
        Task<Response<AlternateLifecycleResponse>> first = handle.GetResponseAsync<AlternateLifecycleResponse>(
            readyToSend: false,
            CancellationToken.None);
        Task<Response<LifecycleResponse>> second = handle.GetResponseAsync<LifecycleResponse>(
            readyToSend: true,
            CancellationToken.None);

        Assert.Same(request, await handle.Message);
        var winner = new LifecycleResponse("first-arrival");
        await context.DeliverAsync(winner);
        Assert.Same(winner, (await second).Message);

        await context.DeliverAsync(new AlternateLifecycleResponse("too-late"));
        handle.Dispose();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "fault-observation-connects-before-send-starts")]
    public void FaultHandlerConnectionFailure_PreventsTheRequestSendFromStarting()
    {
        var sendCount = 0;
        var context = new FaultConnectionFailureClientFactoryContext();

        Assert.Throws<CleanupFailureException>(() => new ClientRequestHandle<LifecycleRequest>(
            context,
            (_, _, _) =>
            {
                Interlocked.Increment(ref sendCount);
                return Task.FromResult(new LifecycleRequest("unexpected"));
            }));

        Assert.Equal(0, sendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "missing-fault-handler-connection-prevents-send")]
    public void MissingFaultHandlerConnection_PreventsTheRequestSendFromStarting()
    {
        var sendCount = 0;
        var context = new MissingConnectionClientFactoryContext(omitFaultConnection: true);
        ClientRequestHandle<LifecycleRequest>? handle = null;

        Exception? exception = Record.Exception(() =>
            handle = new ClientRequestHandle<LifecycleRequest>(
                context,
                (_, _, _) =>
                {
                    Interlocked.Increment(ref sendCount);
                    return Task.FromResult(new LifecycleRequest("unexpected"));
                }));

        try
        {
            InvalidOperationException actual = Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal("The client-factory context returned no fault-handler connection.", actual.Message);
            Assert.Equal(0, sendCount);
        }
        finally
        {
            handle?.Dispose();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "missing-response-handler-connection-prevents-send")]
    public void MissingResponseHandlerConnection_PreventsTheRequestSendFromStarting()
    {
        var sendCount = 0;
        var context = new MissingConnectionClientFactoryContext(omitFaultConnection: false);
        var request = new LifecycleRequest("missing-response-connection");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                Interlocked.Increment(ref sendCount);
                return request;
            });

        Exception? exception = Record.Exception(() =>
        {
            _ = handle.GetResponseAsync<LifecycleResponse>(readyToSend: true, CancellationToken.None);
        });

        InvalidOperationException actual = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("The client-factory context returned no response-handler connection.", actual.Message);
        Assert.Equal(0, sendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "missing-sent-request-message-faults-owned-tasks")]
    public async Task MissingSentRequestMessage_FaultsEveryOwnedTaskExplicitlyAsync()
    {
        var context = new RecordingClientFactoryContext(
            TimeProvider.System,
            new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("missing-sent-message");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                return null!;
            });
        Task<Response<LifecycleResponse>> response =
            handle.GetResponseAsync<LifecycleResponse>(readyToSend: true, CancellationToken.None);

        InvalidOperationException messageException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handle.Message.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        RequestException responseException = await Assert.ThrowsAsync<RequestException>(() =>
            response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        Assert.Equal("The request send callback returned no request message.", messageException.Message);
        Assert.Same(messageException, responseException.InnerException);
    }

    private static async Task AssertSourceIsDisposedAsync(CancellationTokenSource source)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!timeout.IsCancellationRequested)
        {
            try
            {
                _ = source.Token;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            await Task.Yield();
        }

        Assert.Fail("The request handle did not dispose its send cancellation source after terminal cleanup.");
    }

    public enum TerminalOutcome
    {
        CallerCancellation,
        ClientDeadline,
    }

    private sealed record LifecycleRequest(string Value);

    private sealed record LifecycleResponse(string Value);

    private sealed record AlternateLifecycleResponse(string Value);

    private sealed class DirectResponseClientFactoryContext(TimeProvider timeProvider) : ClientFactoryContext
    {
        private readonly Dictionary<Type, object> _requestPipes = [];

        public RequestTimeout DefaultTimeout => new(TimeSpan.FromMinutes(1));

        public TimeProvider TimeProvider { get; } = timeProvider;

        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();

        public Uri ResponseAddress { get; } = new("loopback://localhost/direct-response");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            _requestPipes.Add(typeof(T), pipe);
            return new EmptyConnectHandle();
        }

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        public Task DeliverAsync<T>(T message)
            where T : class
        {
            var pipe = Assert.IsAssignableFrom<IPipe<ConsumeContext<T>>>(_requestPipes[typeof(T)]);
            DirectConsumeContext<T> context = DispatchProxy.Create<DirectConsumeContext<T>, DirectConsumeContextProxy<T>>();
            ((DirectConsumeContextProxy<T>)(object)context).Message = message;
            return pipe.SendAsync(context);
        }
    }

    private interface DirectConsumeContext<T> : ConsumeContext, ConsumeContext<T>
        where T : class;

    private sealed class FaultConnectionFailureClientFactoryContext : ClientFactoryContext
    {
        public RequestTimeout DefaultTimeout => new(TimeSpan.FromMinutes(1));

        public TimeProvider TimeProvider => TimeProvider.System;

        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();

        public Uri ResponseAddress { get; } = new("loopback://localhost/fault-connection-failure");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class => throw new CleanupFailureException("The fault response connection failed.");

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();
    }

    private sealed class MissingConnectionClientFactoryContext(bool omitFaultConnection) : ClientFactoryContext
    {
        public RequestTimeout DefaultTimeout => new(TimeSpan.FromMinutes(1));

        public TimeProvider TimeProvider => TimeProvider.System;

        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();

        public Uri ResponseAddress { get; } = new("loopback://localhost/missing-connection");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            bool isFaultConnection = typeof(T) == typeof(Fault<LifecycleRequest>);
            return isFaultConnection == omitFaultConnection ? null! : new EmptyConnectHandle();
        }

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();
    }

    private class DirectConsumeContextProxy<T> : DispatchProxy
        where T : class
    {
        public T Message { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_Message" => Message,
                "get_CancellationToken" => CancellationToken.None,
                "get_Headers" => new DictionarySendHeaders(),
                "get_Host" => HostMetadataCache.Empty,
                "TryGetPayload" => false,
                nameof(ConsumeContext.NotifyConsumedAsync) => Task.CompletedTask,
                nameof(ConsumeContext.NotifyFaultedAsync) => Task.CompletedTask,
                _ when targetMethod.Name.StartsWith("get_", StringComparison.Ordinal) => null,
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }
    }

    private sealed class BlockingTimerDisposalTimeProvider : TimeProvider
    {
        public BlockingTimer Timer { get; } = new();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) => Timer;

        public sealed class BlockingTimer : ITimer
        {
            private readonly TaskCompletionSource _disposalStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly ManualResetEventSlim _releaseDisposal = new();
            private int _disposeCount;

            public Task DisposalStarted => _disposalStarted.Task;

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
                if (Interlocked.Increment(ref _disposeCount) != 1)
                    return;

                _disposalStarted.TrySetResult();
                if (!_releaseDisposal.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The request timer disposal was not released by the test.");
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }

            public void ReleaseDisposal() => _releaseDisposal.Set();
        }
    }

    private sealed class RecordingClientFactoryContext(
        TimeProvider timeProvider,
        RequestTimeout defaultTimeout) : ClientFactoryContext
    {
        public SendContext<LifecycleRequest>? SentContext { get; set; }

        public RequestTimeout DefaultTimeout { get; } = defaultTimeout;

        public TimeProvider TimeProvider { get; } = timeProvider;

        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();

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

    private sealed class CleanupFailureClientFactoryContext(TimeProvider timeProvider) : ClientFactoryContext
    {
        private readonly TaskCompletionSource _allDisconnectsAttempted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _connectionCount;
        private int _disconnectCount;

        public Task AllDisconnectsAttempted => _allDisconnectsAttempted.Task;

        public int DisconnectCount => Volatile.Read(ref _disconnectCount);

        public RequestTimeout DefaultTimeout => new(TimeSpan.FromMinutes(1));

        public TimeProvider TimeProvider { get; } = timeProvider;

        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();

        public Uri ResponseAddress { get; } = new("loopback://localhost/cleanup-response");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => CreateConnection();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => CreateConnection();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class => CreateConnection();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        private ConnectHandle CreateConnection()
        {
            int connection = Interlocked.Increment(ref _connectionCount);
            return new CleanupConnectHandle(this, throwOnDisconnect: connection == 2);
        }

        private void RecordDisconnect(bool throwOnDisconnect)
        {
            if (Interlocked.Increment(ref _disconnectCount) == 2)
                _allDisconnectsAttempted.TrySetResult();

            if (throwOnDisconnect)
                throw new CleanupFailureException("Response handler disconnect failed.");
        }

        private sealed class CleanupConnectHandle(CleanupFailureClientFactoryContext owner, bool throwOnDisconnect) : ConnectHandle
        {
            private int _disconnected;

            public void Disconnect()
            {
                if (Interlocked.Exchange(ref _disconnected, 1) == 0)
                    owner.RecordDisconnect(throwOnDisconnect);
            }

            public void Dispose() => Disconnect();
        }
    }

    private sealed class FaultingTimerTimeProvider : TimeProvider
    {
        private readonly TaskCompletionSource _timerCreated =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FaultingTimer Timer { get; } = new();

        public Task TimerCreated => _timerCreated.Task;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _timerCreated.TrySetResult();
            return Timer;
        }

        public sealed class FaultingTimer : ITimer
        {
            private int _disposeCount;

            public int DisposeCount => Volatile.Read(ref _disposeCount);

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
                Interlocked.Increment(ref _disposeCount);
                throw new CleanupFailureException("Request timer disposal failed.");
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
        }
    }

    private sealed class ThrowingLogger : ILogger
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Interlocked.Increment(ref _callCount);
            throw new InvalidOperationException("Diagnostic logger failure.");
        }
    }

    private sealed class QueuedSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _callbacks = new();
        private readonly TaskCompletionSource _posted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool HasPostedCallbacks => !_callbacks.IsEmpty;

        public Task Posted => _posted.Task;

        public override void Post(SendOrPostCallback callback, object? state)
        {
            _callbacks.Enqueue((callback, state));
            _posted.TrySetResult();
        }

        public void RunPostedCallbacks()
        {
            while (_callbacks.TryDequeue(out var callback))
                callback.Callback(callback.State);
        }
    }

    private sealed class CleanupFailureException(string message) : Exception(message);
}
