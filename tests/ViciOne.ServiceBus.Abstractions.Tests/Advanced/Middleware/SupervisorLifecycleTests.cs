using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Middleware;

public sealed class SupervisorLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "null-agent-is-rejected-without-changing-counts")]
    public void Add_RejectsANullAgentWithoutChangingLifecycleCounts()
    {
        var supervisor = new Supervisor();

        Assert.Equal("agent", Assert.Throws<ArgumentNullException>(() => supervisor.Add(null!)).ParamName);
        Assert.Equal(0, supervisor.TotalCount);
        Assert.Equal(0, supervisor.PeakActiveCount);
        Assert.False(supervisor.Ready.IsCompleted);
        Assert.False(supervisor.Completed.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "invalid-agent-signals-are-rejected-without-changing-counts")]
    public void Add_RejectsMissingAgentSignalsWithoutChangingLifecycleCounts()
    {
        var supervisor = new Supervisor();

        Assert.Equal("agent", Assert.Throws<ArgumentException>(() => supervisor.Add(new MissingReadyAgent())).ParamName);
        Assert.Equal("agent", Assert.Throws<ArgumentException>(() => supervisor.Add(new MissingCompletedAgent())).ParamName);
        Assert.Equal(0, supervisor.TotalCount);
        Assert.Equal(0, supervisor.PeakActiveCount);
        Assert.False(supervisor.Ready.IsCompleted);
        Assert.False(supervisor.Completed.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "stopping-supervisor-rejects-new-agent")]
    public async Task Add_AfterStoppingBeginsRejectsTheAgentWithoutChangingCountsAsync()
    {
        var supervisor = new Supervisor();
        await supervisor.StopAsync(CancellationToken.None);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => supervisor.Add(new Agent()));

        Assert.Equal("The supervisor is stopping and cannot accept another agent.", exception.Message);
        Assert.Equal(0, supervisor.TotalCount);
        Assert.Equal(0, supervisor.PeakActiveCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "ready-fault-propagation")]
    public async Task FaultedChildReadiness_FaultsSupervisorWhileShutdownStillCompletesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        for (var iteration = 0; iteration < 50; iteration++)
        {
            var supervisor = new Supervisor();
            var child = new Agent();
            var expected = new ReadyFailureException(iteration);
            child.SetNotReady(expected);
            supervisor.Add(child);
            supervisor.SetReady();
            ReadyFailureException? exception = null;

            try
            {
                exception = await Assert.ThrowsAsync<ReadyFailureException>(
                    () => supervisor.Ready.WaitAsync(cancellationToken));
            }
            finally
            {
                await supervisor.StopAsync(cancellationToken);
                await supervisor.Completed.WaitAsync(cancellationToken);
            }

            Assert.Same(expected, exception);
            Assert.True(child.Completed.IsCompletedSuccessfully);
            Assert.True(supervisor.Completed.IsCompletedSuccessfully);
            Assert.True(supervisor.Stopping.IsCancellationRequested);
            Assert.True(supervisor.Stopped.IsCancellationRequested);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "empty-stop-completion")]
    public async Task EmptySupervisor_StopsAndCompletesAsync()
    {
        var supervisor = new Supervisor();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        supervisor.SetReady();

        await supervisor.Ready.WaitAsync(cancellationToken);
        await supervisor.StopAsync(cancellationToken);
        await supervisor.Completed.WaitAsync(cancellationToken);

        Assert.True(supervisor.Ready.IsCompletedSuccessfully);
        Assert.True(supervisor.Completed.IsCompletedSuccessfully);
        Assert.True(supervisor.Stopping.IsCancellationRequested);
        Assert.True(supervisor.Stopped.IsCancellationRequested);
        Assert.Equal(0, supervisor.TotalCount);
        Assert.Equal(0, supervisor.PeakActiveCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "nested-shutdown")]
    public async Task NestedSupervisors_StopTheirCompleteAgentChainAsync()
    {
        var outer = new Supervisor();
        var inner = new Supervisor();
        var child = new Agent();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        outer.Add(inner);
        inner.Add(child);
        inner.SetReady();
        outer.SetReady();
        child.SetReady();

        await outer.Ready.WaitAsync(cancellationToken);
        await outer.StopAsync(cancellationToken);
        await outer.Completed.WaitAsync(cancellationToken);

        Assert.True(child.Completed.IsCompletedSuccessfully);
        Assert.True(inner.Completed.IsCompletedSuccessfully);
        Assert.True(outer.Completed.IsCompletedSuccessfully);
        Assert.True(child.Stopped.IsCancellationRequested);
        Assert.True(inner.Stopped.IsCancellationRequested);
        Assert.True(outer.Stopped.IsCancellationRequested);
        Assert.Equal(1, outer.TotalCount);
        Assert.Equal(1, inner.TotalCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "agent-added-after-ready")]
    public async Task AgentAddedAfterReadiness_IsStillStoppedAndCompletedAsync()
    {
        var supervisor = new Supervisor();
        var child = new Agent();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        child.SetReady();
        supervisor.SetReady();
        await supervisor.Ready.WaitAsync(cancellationToken);

        supervisor.Add(child);
        await supervisor.StopAsync(cancellationToken);
        await supervisor.Completed.WaitAsync(cancellationToken);

        Assert.True(child.Ready.IsCompletedSuccessfully);
        Assert.True(child.Completed.IsCompletedSuccessfully);
        Assert.True(child.Stopped.IsCancellationRequested);
        Assert.True(supervisor.Completed.IsCompletedSuccessfully);
        Assert.Equal(1, supervisor.TotalCount);
        Assert.Equal(1, supervisor.PeakActiveCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "faulted-child-completion-remains-observable")]
    public async Task FaultedChildCompletion_RemainsVisibleToEverySupervisorStopAttemptAsync()
    {
        var expected = new ChildCompletionException();
        var child = new FaultedCompletionAgent(expected);
        var supervisor = new Supervisor();
        supervisor.Add(child);

        ChildCompletionException first = await Assert.ThrowsAsync<ChildCompletionException>(
            () => supervisor.StopAsync(CancellationToken.None));
        ChildCompletionException second = await Assert.ThrowsAsync<ChildCompletionException>(
            () => supervisor.StopAsync(CancellationToken.None));
        ChildCompletionException completion = await Assert.ThrowsAsync<ChildCompletionException>(() => supervisor.Completed);

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Same(expected, completion);
        Assert.Equal(2, child.StopCount);
        Assert.True(supervisor.Stopping.IsCancellationRequested);
        Assert.False(supervisor.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "stop-agent-snapshot-is-read-only")]
    public async Task StopContext_ExposesAReadOnlyChildSnapshotAsync()
    {
        var supervisor = new CapturingSupervisor();
        var child = new Agent();
        child.SetReady();
        supervisor.Add(child);

        await supervisor.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(typeof(IReadOnlyList<IAgent>),
            typeof(StopSupervisorContext).GetProperty(nameof(StopSupervisorContext.Agents))!.PropertyType);
        Assert.NotNull(supervisor.CapturedAgents);
        Assert.False(supervisor.CapturedAgents is IAgent[]);
        Assert.Same(child, Assert.Single(supervisor.CapturedAgents));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "public-custom-signal-getter-errors-and-accepted-child-cleanup")]
    public async Task PublicAdd_CustomSignalGetterFailuresRemainObservableAndCleanupableAsync(int mode)
    {
        var supervisor = new Supervisor();
        var primary = new IOException($"q236-add-getter-{mode}");
        var first = new PublicLifecycleAgent(primary)
        {
            FailReadyOnSecondRead = mode == 1,
            FailCompletedGetter = mode == 2
        };
        var later = new PublicLifecycleAgent(primary);
        Task? stop = null;

        try
        {
            Exception? observed = Record.Exception(() => supervisor.Add(first));

            if (mode == 0)
                Assert.Null(observed);
            else
                Assert.Same(primary, observed);

            // Characterizes the current partial-admission route, not a general rollback promise.
            Assert.Equal(mode == 2 ? 0 : 1, supervisor.TotalCount);
            Assert.Equal(mode == 2 ? 1 : 2, first.ReadyReads);
            Assert.Equal(mode == 2 ? 1 : 0, first.CompletedGetterFailures);
            Assert.Equal(0, first.StopCalls);
            first.DisableFailures();
            supervisor.Add(later);
            Assert.Equal(mode == 2 ? 1 : 2, supervisor.TotalCount);

            stop = supervisor.StopAsync("q236-add-cleanup", CancellationToken.None);
            await stop.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);

            Assert.Equal(mode == 2 ? 0 : 1, first.StopCalls);
            Assert.Equal(1, later.StopCalls);
            Assert.True(later.Completed.IsCompletedSuccessfully);
            Assert.True(supervisor.Completed.IsCompletedSuccessfully);
            Assert.True(supervisor.Stopped.IsCancellationRequested);
        }
        finally
        {
            await CleanupPublicSupervisorAsync(supervisor, stop, first, later);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "later-sync-stop-error-retains-and-drains-started-child-operation")]
    public async Task PublicStop_LaterSynchronousChildFailureDrainsTheAlreadyStartedStopAsync(bool failSecond)
    {
        var supervisor = new Supervisor();
        var primary = new IOException("q236-second-synchronous-stop");
        var first = new PublicLifecycleAgent(primary, holdStop: true);
        var second = new PublicLifecycleAgent(primary) { FailStopSynchronously = failSecond };
        var third = new PublicLifecycleAgent(primary);
        supervisor.Add(first);
        supervisor.Add(second);
        supervisor.Add(third);
        Task? operation = null;

        try
        {
            operation = supervisor.StopAsync("q236-stop-drain", CancellationToken.None);

            Assert.Equal(1, first.StopCalls);
            Assert.Equal(1, second.StopCalls);
            Task rawFirst = Assert.Single(first.RawStopTasks);
            Assert.False(rawFirst.IsCompleted);
            Assert.False(first.Completed.IsCompleted);
            if (failSecond)
                Assert.Same(primary, second.LastSynchronousStopFailure);

            // First finite candidate oracle: the public owner must still retain admitted work.
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, third.StopCalls);

            first.ReleaseStop();
            Exception? observed = await Record.ExceptionAsync(
                () => operation.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));

            Assert.True(rawFirst.IsCompletedSuccessfully);
            Assert.True(first.Completed.IsCompletedSuccessfully);
            Assert.True(third.Completed.IsCompletedSuccessfully);
            if (failSecond)
            {
                Assert.Same(primary, observed);
                Assert.True(operation.IsFaulted);
                Assert.False(supervisor.Stopped.IsCancellationRequested);
            }
            else
            {
                Assert.Null(observed);
                Assert.True(operation.IsCompletedSuccessfully);
                Assert.True(supervisor.Completed.IsCompletedSuccessfully);
                Assert.True(supervisor.Stopped.IsCancellationRequested);
            }
        }
        finally
        {
            await CleanupPublicSupervisorAsync(supervisor, operation, first, second, third);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "public-stop-getter-error-before-admission-remains-cleanup-retryable")]
    public async Task PublicStop_CompletedGetterFailureBeforeStopAdmissionRemainsRetryableAsync(bool failGetter)
    {
        var supervisor = new Supervisor();
        var primary = new IOException("q236-stop-snapshot-getter");
        var child = new PublicLifecycleAgent(primary);
        supervisor.Add(child);
        child.FailCompletedGetter = failGetter;
        Task? operation = null;

        try
        {
            operation = supervisor.StopAsync("q236-stop-snapshot", CancellationToken.None);
            Exception? observed = await Record.ExceptionAsync(
                () => operation.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));

            if (failGetter)
            {
                Assert.Same(primary, observed);
                Assert.Equal(1, child.CompletedGetterFailures);
                Assert.Equal(0, child.StopCalls);
                Assert.Empty(child.RawStopTasks);
                Assert.True(operation.IsFaulted);
                child.DisableFailures();
                operation = supervisor.StopAsync("q236-stop-snapshot-retry", CancellationToken.None);
                await operation.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            }
            else
                Assert.Null(observed);

            Assert.Equal(1, child.StopCalls);
            Assert.True(child.Completed.IsCompletedSuccessfully);
            Assert.True(supervisor.Completed.IsCompletedSuccessfully);
            Assert.True(supervisor.Stopped.IsCancellationRequested);
        }
        finally
        {
            await CleanupPublicSupervisorAsync(supervisor, operation, child);
        }
    }

    private static async Task CleanupPublicSupervisorAsync(
        Supervisor supervisor, Task? operation, params PublicLifecycleAgent[] children)
    {
        foreach (PublicLifecycleAgent child in children)
        {
            child.DisableFailures();
            child.ReleaseStop();
        }

        try
        {
            if (operation != null)
                await ObservePublicLifecycleTaskAsync(operation);
        }
        finally
        {
            try
            {
                await JoinPublicChildTasksAsync(children, 0);
            }
            finally
            {
                Task retry = supervisor.StopAsync("q236-final-cleanup", CancellationToken.None);
                try
                {
                    await retry.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                }
                finally
                {
                    try
                    {
                        await JoinPublicChildTasksAsync(children, 0);
                    }
                    finally
                    {
                        await CleanupUnregisteredChildrenAsync(children, 0);
                    }
                }
            }
        }
    }

    private static async Task CleanupUnregisteredChildrenAsync(PublicLifecycleAgent[] children, int index)
    {
        if (index == children.Length)
            return;

        try
        {
            if (!children[index].Completed.IsCompleted)
            {
                Task cleanup = children[index].StopAsync("q236-unregistered-child-cleanup", CancellationToken.None);
                try
                {
                    await cleanup.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                }
                finally
                {
                    await JoinPublicTasksAsync(children[index].RawStopTasks.ToArray(), 0);
                }
            }
        }
        finally
        {
            await CleanupUnregisteredChildrenAsync(children, index + 1);
        }
    }

    private static async Task JoinPublicChildTasksAsync(PublicLifecycleAgent[] children, int index)
    {
        if (index == children.Length)
            return;

        try
        {
            await JoinPublicTasksAsync(children[index].RawStopTasks.ToArray(), 0);
        }
        finally
        {
            await JoinPublicChildTasksAsync(children, index + 1);
        }
    }

    private static async Task JoinPublicTasksAsync(Task[] tasks, int index)
    {
        if (index == tasks.Length)
            return;

        try
        {
            await ObservePublicLifecycleTaskAsync(tasks[index]);
        }
        finally
        {
            await JoinPublicTasksAsync(tasks, index + 1);
        }
    }

    private static async Task ObservePublicLifecycleTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception exception) when (exception is not TimeoutException
            && task.IsCompleted && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private sealed class PublicLifecycleAgent(IOException primary, bool holdStop = false) : IAgent
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _stopRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly List<Task> _rawStopTasks = [];

        public bool FailReadyOnSecondRead { get; set; }
        public bool FailCompletedGetter { get; set; }
        public bool FailStopSynchronously { get; set; }
        public int ReadyReads { get; private set; }
        public int CompletedGetterFailures { get; private set; }
        public int StopCalls { get; private set; }
        public IOException? LastSynchronousStopFailure { get; private set; }
        public IReadOnlyList<Task> RawStopTasks => _rawStopTasks;
        public CancellationToken Stopping => CancellationToken.None;
        public CancellationToken Stopped => CancellationToken.None;

        public Task Ready
        {
            get
            {
                ReadyReads++;
                if (FailReadyOnSecondRead && ReadyReads == 2)
                    throw primary;
                return Task.CompletedTask;
            }
        }

        public Task Completed
        {
            get
            {
                if (FailCompletedGetter)
                {
                    CompletedGetterFailures++;
                    throw primary;
                }
                return _completed.Task;
            }
        }

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            StopCalls++;
            if (FailStopSynchronously)
            {
                LastSynchronousStopFailure = primary;
                throw primary;
            }
            Task task = CompleteStopAsync();
            _rawStopTasks.Add(task);
            return task;
        }

        public void ReleaseStop() => _stopRelease.TrySetResult();

        public void DisableFailures()
        {
            FailReadyOnSecondRead = false;
            FailCompletedGetter = false;
            FailStopSynchronously = false;
        }

        private async Task CompleteStopAsync()
        {
            if (holdStop)
                await _stopRelease.Task.ConfigureAwait(false);
            _completed.TrySetResult();
        }
    }

    private sealed class ReadyFailureException(int iteration)
        : Exception($"Child readiness failed during iteration {iteration}.")
    {
    }

    private abstract class InvalidAgent : IAgent
    {
        public virtual Task Ready => Task.CompletedTask;

        public virtual Task Completed => Task.CompletedTask;

        public CancellationToken Stopping => CancellationToken.None;

        public CancellationToken Stopped => CancellationToken.None;

        public virtual Task StopAsync(StopContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MissingReadyAgent : InvalidAgent
    {
        public override Task Ready => null!;
    }

    private sealed class MissingCompletedAgent : InvalidAgent
    {
        public override Task Completed => null!;
    }

    private sealed class FaultedCompletionAgent(Exception failure) : InvalidAgent
    {
        private readonly Task _completed = Task.FromException(failure);
        private int _stopCount;

        public override Task Completed => _completed;

        public int StopCount => Volatile.Read(ref _stopCount);

        public override Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _stopCount);
            return Task.CompletedTask;
        }
    }

    private sealed class ChildCompletionException : Exception;

    private sealed class CapturingSupervisor : Supervisor
    {
        public IReadOnlyList<IAgent>? CapturedAgents { get; private set; }

        protected override Task StopSupervisorAsync(StopSupervisorContext context)
        {
            CapturedAgents = context.Agents;
            return base.StopSupervisorAsync(context);
        }
    }
}
