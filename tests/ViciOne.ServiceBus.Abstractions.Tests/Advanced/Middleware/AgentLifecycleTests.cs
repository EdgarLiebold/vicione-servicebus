using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Middleware;

public sealed class AgentLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-LIFECYCLE", "concurrent-stop-callers-share-one-attempt")]
    public async Task ConcurrentStopCalls_WaitForOneSharedAttemptAsync()
    {
        var agent = new CoordinatedAgent();

        Task first = agent.StopAsync(CancellationToken.None);
        await agent.StopEntered.WaitAsync(TestContext.Current.CancellationToken);
        Task second = agent.StopAsync(CancellationToken.None);

        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);
        Assert.Equal(1, agent.StopCount);
        Assert.True(agent.Stopping.IsCancellationRequested);
        Assert.False(agent.Stopped.IsCancellationRequested);

        agent.ReleaseStop();
        await Task.WhenAll(first, second);

        Assert.Equal(1, agent.StopCount);
        Assert.True(agent.Completed.IsCompletedSuccessfully);
        Assert.True(agent.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-LIFECYCLE", "caller-cancellation-does-not-abandon-owned-stop")]
    public async Task CanceledCaller_CanRejoinTheStillOwnedStopAttemptAsync()
    {
        var agent = new CoordinatedAgent();
        using var cancellationTokenSource = new CancellationTokenSource();

        Task canceledWait = agent.StopAsync(cancellationTokenSource.Token);
        await agent.StopEntered.WaitAsync(TestContext.Current.CancellationToken);
        cancellationTokenSource.Cancel();

        OperationCanceledException cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWait);
        Task rejoinedWait = agent.StopAsync(CancellationToken.None);

        Assert.Equal(cancellationTokenSource.Token, cancellation.CancellationToken);
        Assert.False(rejoinedWait.IsCompleted);
        Assert.Equal(1, agent.StopCount);

        agent.ReleaseStop();
        await rejoinedWait;

        Assert.Equal(1, agent.StopCount);
        Assert.True(agent.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-LIFECYCLE", "failed-stop-remains-retryable")]
    public async Task FailedStopAttempt_IsObservedAndCanBeRetriedAsync()
    {
        var expected = new StopFailureException();
        var agent = new RetriableAgent(expected);

        StopFailureException actual = await Assert.ThrowsAsync<StopFailureException>(
            () => agent.StopAsync(CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(1, agent.StopCount);
        Assert.True(agent.Stopping.IsCancellationRequested);
        Assert.False(agent.Stopped.IsCancellationRequested);
        Assert.False(agent.Completed.IsCompleted);

        await agent.StopAsync(CancellationToken.None);

        Assert.Equal(2, agent.StopCount);
        Assert.True(agent.Completed.IsCompletedSuccessfully);
        Assert.True(agent.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-LIFECYCLE", "lifecycle-callback-failures-cannot-control-stop")]
    public async Task ThrowingLifecycleCallbacks_CannotPreventOrPoisonSuccessfulShutdownAsync()
    {
        var agent = new CallbackResilientAgent();
        using CancellationTokenRegistration stoppingRegistration = agent.Stopping.Register(
            static () => throw new LifecycleCallbackException("stopping"));
        using CancellationTokenRegistration stoppedRegistration = agent.Stopped.Register(
            static () => throw new LifecycleCallbackException("stopped"));

        await agent.StopAsync(CancellationToken.None);
        await agent.StopAsync(CancellationToken.None);

        Assert.Equal(1, agent.StopCount);
        Assert.True(agent.Completed.IsCompletedSuccessfully);
        Assert.True(agent.Stopping.IsCancellationRequested);
        Assert.True(agent.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-LIFECYCLE", "completed-stop-is-idempotent")]
    public async Task CompletedStop_RemainsSuccessfulForAnAlreadyCanceledCallerAsync()
    {
        var agent = new Agent();
        await agent.StopAsync(CancellationToken.None);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await agent.StopAsync(cancellationTokenSource.Token);

        Assert.True(agent.Completed.IsCompletedSuccessfully);
        Assert.True(agent.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-BOUNDARY", "required-inputs-are-rejected")]
    public async Task PublicAndProtectedLifecycleBoundaries_RejectMissingInputsAsync()
    {
        var agent = new ExposedAgent();

        Assert.Equal(
            "agent",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => AgentExtensions.StopAsync(null!, CancellationToken.None))).ParamName);
        Assert.Equal(
            "agent",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => AgentExtensions.StopAsync(null!, "test", CancellationToken.None))).ParamName);
        Assert.Equal(
            "reason",
            (await Assert.ThrowsAsync<ArgumentException>(() => agent.StopAsync(" ", CancellationToken.None))).ParamName);
        Assert.Equal(
            "context",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => agent.StopAsync(null!, CancellationToken.None))).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() => agent.SetNotReady(null!)).ParamName);
        Assert.Equal("readyTask", Assert.Throws<ArgumentNullException>(() => agent.PublishReady(null!)).ParamName);
        Assert.Equal("completedTask", Assert.Throws<ArgumentNullException>(() => agent.PublishCompleted(null!)).ParamName);
        Assert.Equal("task", Assert.Throws<ArgumentNullException>(() => agent.PublishFault(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-LIFECYCLE", "latest-pending-terminal-source-wins")]
    public async Task PendingTerminalSignalTransfers_UseTheLatestRegisteredSourceAsync()
    {
        var staleFailure = new TerminalSignalFailureException();
        var staleReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var currentReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var staleCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var currentCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var agent = new ExposedAgent();

        agent.PublishReady(staleReady.Task);
        agent.PublishReady(currentReady.Task);
        agent.PublishCompleted(staleCompleted.Task);
        agent.PublishCompleted(currentCompleted.Task);

        staleReady.SetException(staleFailure);
        staleCompleted.SetException(staleFailure);
        currentReady.SetResult();
        currentCompleted.SetResult();

        await agent.Ready;
        await agent.Completed;

        Assert.True(agent.Ready.IsCompletedSuccessfully);
        Assert.True(agent.Completed.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-LIFECYCLE", "completed-terminal-source-is-authoritative")]
    public async Task CompletedTerminalSignalTransfers_CannotBeReplacedAsync()
    {
        var readyFailure = new TerminalSignalFailureException();
        var completedFailure = new TerminalSignalFailureException();
        var readyAgent = new ExposedAgent();
        var completedAgent = new ExposedAgent();

        readyAgent.PublishReady(Task.FromException(readyFailure));
        readyAgent.PublishReady(Task.CompletedTask);
        completedAgent.PublishCompleted(Task.FromException(completedFailure));
        completedAgent.PublishCompleted(Task.CompletedTask);

        TerminalSignalFailureException actualReady = await Assert.ThrowsAsync<TerminalSignalFailureException>(() => readyAgent.Ready);
        TerminalSignalFailureException actualCompleted = await Assert.ThrowsAsync<TerminalSignalFailureException>(() => completedAgent.Completed);

        Assert.Same(readyFailure, actualReady);
        Assert.Same(completedFailure, actualCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-LIFECYCLE", "rejected-terminal-source-faults-are-observed")]
    public void RejectedTerminalSignalSources_ObserveFaultsThatArriveAfterRejection()
    {
        string readyMarker = $"rejected-ready-{Guid.NewGuid():N}";
        string completedMarker = $"rejected-completed-{Guid.NewGuid():N}";
        string controlMarker = $"unobserved-control-{Guid.NewGuid():N}";
        var publishedMarkers = new ConcurrentQueue<string>();

        void OnUnobserved(object? _, UnobservedTaskExceptionEventArgs args)
        {
            foreach (Exception exception in args.Exception.Flatten().InnerExceptions)
            {
                if (exception.Message == readyMarker || exception.Message == completedMarker || exception.Message == controlMarker)
                    publishedMarkers.Enqueue(exception.Message);
            }

            args.SetObserved();
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;
        try
        {
            CreateRejectedTerminalFaults(readyMarker, completedMarker, controlMarker);

            CollectUntil(() => publishedMarkers.Contains(controlMarker));
            Assert.Contains(controlMarker, publishedMarkers);

            CollectUntil(static () => false, maximumAttempts: 3);

            Assert.DoesNotContain(readyMarker, publishedMarkers);
            Assert.DoesNotContain(completedMarker, publishedMarkers);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AGENT-LIFECYCLE", "terminal-signals-preserve-cancellation-identity")]
    public async Task TerminalSignalTransfers_PreserveTheExactCancellationTokenAsync()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        CancellationToken expected = cancellationTokenSource.Token;
        var readyAgent = new ExposedAgent();
        var completedAgent = new ExposedAgent();
        var faultedAgent = new ExposedAgent();

        readyAgent.PublishReady(Task.FromCanceled(expected));
        completedAgent.PublishCompleted(Task.FromCanceled(expected));
        faultedAgent.PublishFault(Task.FromCanceled(expected));

        OperationCanceledException ready = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readyAgent.Ready);
        OperationCanceledException completed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completedAgent.Completed);
        OperationCanceledException faulted = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => faultedAgent.Ready);

        Assert.Equal(expected, ready.CancellationToken);
        Assert.Equal(expected, completed.CancellationToken);
        Assert.Equal(expected, faulted.CancellationToken);
        Assert.True(faultedAgent.Completed.IsCompletedSuccessfully);
    }

    private sealed class CoordinatedAgent : Agent
    {
        private readonly TaskCompletionSource _releaseStop = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _stopEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _stopCount;

        public Task StopEntered => _stopEntered.Task;

        public int StopCount => Volatile.Read(ref _stopCount);

        public void ReleaseStop() => _releaseStop.TrySetResult();

        protected override async Task StopAgentAsync(StopContext context)
        {
            Interlocked.Increment(ref _stopCount);
            _stopEntered.TrySetResult();
            await _releaseStop.Task.ConfigureAwait(false);
            SetCompleted(Task.CompletedTask);
        }
    }

    private sealed class RetriableAgent(Exception firstFailure) : Agent
    {
        private int _stopCount;

        public int StopCount => Volatile.Read(ref _stopCount);

        protected override Task StopAgentAsync(StopContext context)
        {
            if (Interlocked.Increment(ref _stopCount) == 1)
                return Task.FromException(firstFailure);

            SetCompleted(Task.CompletedTask);
            return Completed;
        }
    }

    private sealed class CallbackResilientAgent : Agent
    {
        private int _stopCount;

        public int StopCount => Volatile.Read(ref _stopCount);

        protected override Task StopAgentAsync(StopContext context)
        {
            Interlocked.Increment(ref _stopCount);
            SetCompleted(Task.CompletedTask);
            return Task.CompletedTask;
        }
    }

    private sealed class ExposedAgent : Agent
    {
        public void PublishReady(Task task) => SetReady(task);

        public void PublishCompleted(Task task) => SetCompleted(task);

        public void PublishFault(Task task) => SetFaulted(task);
    }

    private sealed class StopFailureException : Exception;

    private sealed class TerminalSignalFailureException(string? message = null) : Exception(message);

    private sealed class LifecycleCallbackException(string message) : Exception(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateRejectedTerminalFaults(string readyMarker, string completedMarker, string controlMarker)
    {
        var readyFailure = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedFailure = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readyAgent = new ExposedAgent();
        var completedAgent = new ExposedAgent();

        readyAgent.SetReady();
        readyAgent.PublishReady(readyFailure.Task);
        completedAgent.PublishCompleted(Task.CompletedTask);
        completedAgent.PublishCompleted(completedFailure.Task);

        readyFailure.SetException(new TerminalSignalFailureException(readyMarker));
        completedFailure.SetException(new TerminalSignalFailureException(completedMarker));
        _ = Task.FromException(new TerminalSignalFailureException(controlMarker));
    }

    private static void CollectUntil(Func<bool> condition, int maximumAttempts = 100)
    {
        for (var attempt = 0; attempt < maximumAttempts && !condition(); attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            Thread.Yield();
        }
    }
}
