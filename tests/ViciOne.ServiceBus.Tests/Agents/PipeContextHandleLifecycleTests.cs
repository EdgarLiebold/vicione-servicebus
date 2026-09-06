using System.Text.Json;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Agents;

public sealed class PipeContextHandleLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-TERMINALITY", "first-creation-outcome-wins")]
    public async Task AsyncHandle_FirstCreationOutcomeWinsWithoutThrowingOnLateSignalsAsync()
    {
        var handle = new AsyncPipeContextHandle<AgentContext>();
        IAsyncPipeContextHandle<AgentContext> asyncHandle = handle;
        PipeContextHandle<AgentContext> contextHandle = handle;
        var expected = new AgentContext();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await asyncHandle.CreatedAsync(expected);
        await asyncHandle.CreateFaultedAsync(new ExpectedFailureException());
        await asyncHandle.CreateCanceledAsync(cancellationTokenSource.Token);

        Assert.Same(expected, await contextHandle.Context);
        Assert.False(contextHandle.IsDisposed);

        await ((IAsyncDisposable)handle).DisposeAsync();

        Assert.True(contextHandle.IsDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-TERMINALITY", "cancellation-token-identity")]
    public async Task AsyncHandle_CanceledCreationPreservesTheExactCancellationTokenAsync()
    {
        var handle = new AsyncPipeContextHandle<AgentContext>();
        IAsyncPipeContextHandle<AgentContext> asyncHandle = handle;
        PipeContextHandle<AgentContext> contextHandle = handle;
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await asyncHandle.CreateCanceledAsync(cancellationTokenSource.Token);
        await asyncHandle.CreatedAsync(new AgentContext());

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => contextHandle.Context);

        Assert.Equal(cancellationTokenSource.Token, actual.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-TERMINALITY", "runtime-failure-identity")]
    public async Task AsyncHandle_RuntimeFailureRemainsObservableAndWinsOverDisposalAsync()
    {
        var handle = new AsyncPipeContextHandle<AgentContext>();
        IAsyncPipeContextHandle<AgentContext> asyncHandle = handle;
        PipeContextHandle<AgentContext> contextHandle = handle;
        var expected = new ExpectedFailureException();

        await asyncHandle.FaultedAsync(expected);
        await ((IAsyncDisposable)handle).DisposeAsync();

        ExpectedFailureException actual = await Assert.ThrowsAsync<ExpectedFailureException>(() => handle.Completion);

        Assert.Same(expected, actual);
        Assert.True(contextHandle.IsDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-TERMINALITY", "agent-late-cancellation-does-not-stop-created-context")]
    public async Task AsyncAgent_LateCreationCancellationDoesNotStopAnAlreadyCreatedContextAsync()
    {
        var agent = new AsyncPipeContextAgent<AgentContext>();
        IAsyncPipeContextAgent<AgentContext> asyncAgent = agent;
        var expected = new AgentContext();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await asyncAgent.CreatedAsync(expected);
        await asyncAgent.CreateCanceledAsync(cancellationTokenSource.Token);

        Assert.Same(expected, await asyncAgent.Context);
        Assert.False(asyncAgent.Completed.IsCompleted);

        await asyncAgent.DisposeAsync();
        await asyncAgent.Completed;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-PIPELINE", "filter-publishes-before-next-and-waits")]
    public async Task AsyncFilter_PublishesBeforeTheNextStageAndWaitsForAgentCompletionAsync()
    {
        var agent = new AsyncPipeContextAgent<AgentContext>();
        IAsyncPipeContextAgent<AgentContext> asyncAgent = agent;
        var expected = new AgentContext();
        var nextInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var filter = new AsyncPipeContextFilter<AgentContext>(asyncAgent);
        var next = new CallbackPipe(context =>
        {
            nextInvoked.TrySetResult();
            Assert.Same(expected, context);
            Assert.True(asyncAgent.Context.IsCompletedSuccessfully);
            Assert.Same(expected, asyncAgent.Context.Result);
            return Task.CompletedTask;
        });

        Task send = filter.SendAsync(expected, next);

        await nextInvoked.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.False(send.IsCompleted);
        Assert.Contains("asyncPipeContext",
            JsonSerializer.Serialize(filter.GetProbeResult(Xunit.TestContext.Current.CancellationToken).Results), StringComparison.Ordinal);

        await ((IAsyncDisposable)asyncAgent).DisposeAsync();
        await send;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-PIPELINE", "pipe-publishes-after-inner-and-waits")]
    public async Task AsyncPipe_PublishesAfterTheInnerPipeAndWaitsForAgentCompletionAsync()
    {
        var agent = new AsyncPipeContextAgent<AgentContext>();
        IAsyncPipeContextAgent<AgentContext> asyncAgent = agent;
        var expected = new AgentContext();
        var innerInvoked = false;
        var inner = new CallbackPipe(context =>
        {
            innerInvoked = true;
            Assert.Same(expected, context);
            Assert.False(asyncAgent.Context.IsCompleted);
            return Task.CompletedTask;
        });
        var pipe = new AsyncPipeContextPipe<AgentContext>(asyncAgent, inner);

        Task send = pipe.SendAsync(expected);

        Assert.True(innerInvoked);
        Assert.True(asyncAgent.Context.IsCompletedSuccessfully);
        Assert.False(send.IsCompleted);

        await ((IAsyncDisposable)asyncAgent).DisposeAsync();
        await send;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-DISPOSAL", "concurrent-callers-share-success")]
    public async Task PipeContextAgent_ConcurrentDisposalWaitsForOneSharedSuccessfulOperationAsync()
    {
        var context = new CoordinatedDisposableContext();
        var agent = new PipeContextAgent<CoordinatedDisposableContext>(context);

        Task first = agent.DisposeAsync().AsTask();
        await context.DisposeEntered.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        Task second = agent.DisposeAsync().AsTask();

        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);
        Assert.Equal(1, context.DisposeCount);

        context.ReleaseDisposal();
        await Task.WhenAll(first, second);
        await agent.Completed;

        Assert.Equal(1, context.DisposeCount);
        Assert.True(((PipeContextHandle<CoordinatedDisposableContext>)agent).IsDisposed);
        Assert.True(agent.Completed.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-DISPOSAL", "concurrent-callers-share-failure")]
    public async Task PipeContextAgent_ConcurrentDisposalSharesTheExactFailureAndStillCompletesLifecycleAsync()
    {
        var expected = new ExpectedFailureException();
        var context = new CoordinatedDisposableContext(expected);
        var agent = new PipeContextAgent<CoordinatedDisposableContext>(context);

        Task first = agent.DisposeAsync().AsTask();
        await context.DisposeEntered.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        Task second = agent.DisposeAsync().AsTask();
        context.ReleaseDisposal();

        ExpectedFailureException firstFailure = await Assert.ThrowsAsync<ExpectedFailureException>(() => first);
        ExpectedFailureException secondFailure = await Assert.ThrowsAsync<ExpectedFailureException>(() => second);
        await agent.Completed;

        Assert.Same(expected, firstFailure);
        Assert.Same(expected, secondFailure);
        Assert.Equal(1, context.DisposeCount);
        Assert.True(agent.Completed.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-DISPOSAL", "pending-context-remains-owned")]
    public async Task PipeContextAgent_DisposalWaitsForAndDisposesAPendingContextAsync()
    {
        var contextSource = new TaskCompletionSource<CoordinatedDisposableContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = new CoordinatedDisposableContext();
        var agent = new PipeContextAgent<CoordinatedDisposableContext>(contextSource.Task);

        Task disposal = agent.DisposeAsync().AsTask();

        Assert.False(disposal.IsCompleted);

        contextSource.SetResult(context);
        await context.DisposeEntered.WaitAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.False(disposal.IsCompleted);
        Assert.Equal(1, context.DisposeCount);

        context.ReleaseDisposal();
        await disposal;
        await agent.Completed;

        Assert.Equal(1, context.DisposeCount);
        Assert.True(agent.Completed.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-DISPOSAL", "constant-handle-shares-operation")]
    public async Task ConstantHandle_ConcurrentDisposalSharesOneOperationAndItsExactFailureAsync()
    {
        var expected = new ExpectedFailureException();
        var context = new CoordinatedDisposableContext(expected);
        var handle = new ConstantPipeContextHandle<CoordinatedDisposableContext>(context);
        IAsyncDisposable disposable = handle;

        Task first = disposable.DisposeAsync().AsTask();
        await context.DisposeEntered.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        Task second = disposable.DisposeAsync().AsTask();

        Assert.False(second.IsCompleted);
        Assert.True(((PipeContextHandle<CoordinatedDisposableContext>)handle).IsDisposed);

        context.ReleaseDisposal();
        ExpectedFailureException firstFailure = await Assert.ThrowsAsync<ExpectedFailureException>(() => first);
        ExpectedFailureException secondFailure = await Assert.ThrowsAsync<ExpectedFailureException>(() => second);

        Assert.Same(expected, firstFailure);
        Assert.Same(expected, secondFailure);
        Assert.Equal(1, context.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-BOUNDARY", "constructors-and-methods-reject-null")]
    public async Task PublicAgentBoundaries_RejectEveryNullRequiredInputAsync()
    {
        var context = new AgentContext();
        var contextTask = Task.FromResult(context);
        var asyncAgent = new AsyncPipeContextAgent<AgentContext>();
        IAsyncPipeContextHandle<AgentContext> asyncHandle = asyncAgent;

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new PipeContextAgent<AgentContext>((AgentContext)null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new PipeContextAgent<AgentContext>((Task<AgentContext>)null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new ConstantPipeContextHandle<AgentContext>(null!)).ParamName);
        Assert.Equal("contextHandle", Assert.Throws<ArgumentNullException>(() => new ActivePipeContext<AgentContext>(null!, contextTask)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new ActivePipeContext<AgentContext>(asyncAgent, (Task<AgentContext>)null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new ActivePipeContext<AgentContext>(asyncAgent, (AgentContext)null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new ActivePipeContextAgent<AgentContext>(null!)).ParamName);
        Assert.Equal("agent", Assert.Throws<ArgumentNullException>(() => new AsyncPipeContextFilter<AgentContext>(null!)).ParamName);
        Assert.Equal("agent", Assert.Throws<ArgumentNullException>(() => new AsyncPipeContextPipe<AgentContext>(null!, new NoOpPipe())).ParamName);
        Assert.Equal("pipe", Assert.Throws<ArgumentNullException>(() => new AsyncPipeContextPipe<AgentContext>(asyncAgent, null!)).ParamName);
        Assert.Equal("contextFactory", Assert.Throws<ArgumentNullException>(() => new PipeContextSupervisor<AgentContext>(null!)).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => asyncHandle.CreatedAsync(null!))).ParamName);
        Assert.Equal("exception", (await Assert.ThrowsAsync<ArgumentNullException>(() => asyncHandle.CreateFaultedAsync(null!))).ParamName);
        Assert.Equal("exception", (await Assert.ThrowsAsync<ArgumentNullException>(() => asyncHandle.FaultedAsync(null!))).ParamName);

        var activeContext = new ActivePipeContext<AgentContext>(asyncAgent, context);
        Assert.Equal("exception", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((ActivePipeContextHandle<AgentContext>)activeContext).FaultedAsync(null!))).ParamName);

        var filter = new AsyncPipeContextFilter<AgentContext>(asyncAgent);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => filter.SendAsync(null!, new NoOpPipe()))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() => filter.SendAsync(context, null!))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => filter.Probe(null!)).ParamName);

        var pipe = new AsyncPipeContextPipe<AgentContext>(asyncAgent, new NoOpPipe());
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => pipe.SendAsync(null!))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => pipe.Probe(null!)).ParamName);

        var supervisor = new PipeContextSupervisor<AgentContext>(new ValidFactory());
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            supervisor.SendAsync(null!, CancellationToken.None))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => supervisor.Probe(null!)).ParamName);

        await ((IAsyncDisposable)asyncAgent).DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-BOUNDARY", "task-result-must-contain-context")]
    public async Task TaskBackedHandles_RejectACompletedNullContextAsync()
    {
        Task<AgentContext> nullContext = Task.FromResult<AgentContext>(null!);
        var owner = new PipeContextAgent<AgentContext>(nullContext);
        var borrowed = new ActivePipeContext<AgentContext>(owner, nullContext);

        InvalidOperationException ownerFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((PipeContextHandle<AgentContext>)owner).Context);
        InvalidOperationException borrowedFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((PipeContextHandle<AgentContext>)borrowed).Context);

        Assert.Equal("The context task completed without a context.", ownerFailure.Message);
        Assert.Equal("The active context task completed without a context.", borrowedFailure.Message);

        await owner.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-BOUNDARY", "factories-must-return-handles")]
    public async Task Supervisor_RejectsFactoriesThatReturnNullHandlesAsync()
    {
        var nullOwnedContextSupervisor = new PipeContextSupervisor<AgentContext>(new NullFactory(returnNullActiveContext: false));
        var nullActiveContextSupervisor = new PipeContextSupervisor<AgentContext>(new NullFactory(returnNullActiveContext: true));

        InvalidOperationException ownedFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullOwnedContextSupervisor.SendAsync(new NoOpPipe(), CancellationToken.None));
        InvalidOperationException activeFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullActiveContextSupervisor.SendAsync(new NoOpPipe(), CancellationToken.None));

        Assert.Contains("returned no ", ownedFailure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("returned no active", ownedFailure.Message, StringComparison.Ordinal);
        Assert.Contains("returned no active ", activeFailure.Message, StringComparison.Ordinal);
    }

    private sealed class CoordinatedDisposableContext(Exception? failure = null) : BasePipeContext, IAsyncDisposable
    {
        private readonly TaskCompletionSource _disposeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public Task DisposeEntered => _disposeEntered.Task;

        public async ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            _disposeEntered.TrySetResult();
            await _releaseDisposal.Task.ConfigureAwait(false);

            if (failure is not null)
                throw failure;
        }

        public void ReleaseDisposal() => _releaseDisposal.TrySetResult();
    }

    private sealed class ValidFactory : IPipeContextFactory<AgentContext>
    {
        public IPipeContextAgent<AgentContext> CreateContext(ISupervisor supervisor) => supervisor.AddContext(new AgentContext());

        public IActivePipeContextAgent<AgentContext> CreateActiveContext(
            ISupervisor supervisor,
            PipeContextHandle<AgentContext> context,
            CancellationToken cancellationToken = default) => supervisor.AddActiveContext(context, context.Context);
    }

    private sealed class NullFactory(bool returnNullActiveContext) : IPipeContextFactory<AgentContext>
    {
        public IPipeContextAgent<AgentContext> CreateContext(ISupervisor supervisor) => returnNullActiveContext
            ? supervisor.AddContext(new AgentContext())
            : null!;

        public IActivePipeContextAgent<AgentContext> CreateActiveContext(
            ISupervisor supervisor,
            PipeContextHandle<AgentContext> context,
            CancellationToken cancellationToken = default) => null!;
    }

    private sealed class NoOpPipe : IPipe<AgentContext>
    {
        public Task SendAsync(AgentContext context) => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class CallbackPipe(Func<AgentContext, Task> callback) : IPipe<AgentContext>
    {
        public Task SendAsync(AgentContext context) => callback(context);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class AgentContext : BasePipeContext;

    private sealed class ExpectedFailureException : Exception;
}
