using System.Text.Json;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Lifecycle;

public sealed class SupervisorAgentCreationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "supervisor-extension-boundaries")]
    public async Task PublicExtensions_RejectNullSupervisorsHandlesContextsAndFactoriesAsync()
    {
        var supervisor = new TestSupervisor();
        IAsyncPipeContextAgent<ChildContext> asyncContext = supervisor.AddAsyncContext<ChildContext>();
        var child = new ChildContext();

        try
        {
            Assert.Equal("supervisor", Assert.Throws<ArgumentNullException>(() =>
                SupervisorExtensions.AddContext<ChildContext>(null!, child)).ParamName);
            Assert.Equal("supervisor", Assert.Throws<ArgumentNullException>(() =>
                SupervisorExtensions.AddContext<ChildContext>(null!, Task.FromResult(child))).ParamName);
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddContext((ChildContext)null!)).ParamName);
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddContext((Task<ChildContext>)null!)).ParamName);
            Assert.Equal("supervisor", Assert.Throws<ArgumentNullException>(() =>
                SupervisorExtensions.AddActiveContext(null!, asyncContext, child)).ParamName);
            Assert.Equal("contextHandle", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddActiveContext(null!, child)).ParamName);
            Assert.Equal("contextHandle", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddActiveContext(null!, Task.FromResult(child))).ParamName);
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddActiveContext(asyncContext, (ChildContext)null!)).ParamName);
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddActiveContext(asyncContext, (Task<ChildContext>)null!)).ParamName);
            Assert.Equal("supervisor", Assert.Throws<ArgumentNullException>(() =>
                SupervisorExtensions.AddAsyncContext<ChildContext>(null!)).ParamName);
            Assert.Equal("supervisor", Assert.Throws<ArgumentNullException>(() =>
                SupervisorExtensions.StartAgent<OwnerContext, ChildContext>(
                    null!, asyncContext, (_, _) => Task.FromResult(child), CancellationToken.None)).ParamName);
            Assert.Equal("asyncContext", Assert.Throws<ArgumentNullException>(() =>
                supervisor.StartAgent<OwnerContext, ChildContext>(null!, (_, _) => Task.FromResult(child), CancellationToken.None)).ParamName);
            Assert.Equal("agentFactory", Assert.Throws<ArgumentNullException>(() =>
                supervisor.StartAgent(asyncContext, null!, CancellationToken.None)).ParamName);

            ArgumentNullException nullSupervisor = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                SupervisorExtensions.CreateAgentAsync<OwnerContext, ChildContext>(
                    null!, asyncContext, (_, _) => Task.FromResult(child), CancellationToken.None));
            Assert.Equal("supervisor", nullSupervisor.ParamName);

            ArgumentNullException nullAsyncContext = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                supervisor.CreateAgentAsync<OwnerContext, ChildContext>(
                    null!, (_, _) => Task.FromResult(child), CancellationToken.None));
            Assert.Equal("asyncContext", nullAsyncContext.ParamName);

            ArgumentNullException createException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                supervisor.CreateAgentAsync(asyncContext, null!, CancellationToken.None));
            Assert.Equal("agentFactory", createException.ParamName);
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
        }

    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "available-and-task-context-overloads-share-supervisor-lifecycle")]
    public async Task AvailableAndTaskContexts_ShareTheExactSupervisorLifecycleAsync()
    {
        var supervisor = new TestSupervisor();
        var directChild = new ChildContext();
        var taskChild = new ChildContext();
        IPipeContextAgent<ChildContext> directOwner = supervisor.AddContext(directChild);
        IPipeContextAgent<ChildContext> taskOwner = supervisor.AddContext(Task.FromResult(taskChild));
        IActivePipeContextAgent<ChildContext> directBorrower = supervisor.AddActiveContext(directOwner, directChild);
        IActivePipeContextAgent<ChildContext> taskBorrower = supervisor.AddActiveContext(taskOwner, Task.FromResult(taskChild));

        Assert.Same(directChild, await directOwner.Context);
        Assert.Same(taskChild, await taskOwner.Context);
        Assert.Same(directChild, await directBorrower.Context);
        Assert.Same(taskChild, await taskBorrower.Context);
        Assert.Equal(4, supervisor.TotalCount);
        Assert.False(directBorrower.IsDisposed);
        Assert.False(taskBorrower.IsDisposed);
        Assert.Equal("Active<ChildContext>", directBorrower.ToString());
        Assert.Equal("Active<ChildContext>", taskBorrower.ToString());

        await supervisor.StopAsync(CancellationToken.None);

        Assert.True(directBorrower.IsDisposed);
        Assert.True(taskBorrower.IsDisposed);
        Assert.True(directOwner.Completed.IsCompletedSuccessfully);
        Assert.True(taskOwner.Completed.IsCompletedSuccessfully);
        Assert.True(directBorrower.Completed.IsCompletedSuccessfully);
        Assert.True(taskBorrower.Completed.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "factory-must-return-context")]
    public async Task CreateAgent_RejectsAFactoryThatReturnsNoContextAsync()
    {
        var supervisor = new TestSupervisor();
        IAsyncPipeContextAgent<ChildContext> asyncContext = supervisor.AddAsyncContext<ChildContext>();

        try
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                supervisor.CreateAgentAsync(
                    asyncContext,
                    (_, _) => Task.FromResult<ChildContext>(null!),
                    TestContext.Current.CancellationToken));

            Assert.Equal("The agent factory returned no ChildContext context.", exception.Message);
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "supervisor-must-publish-creation-outcome")]
    public async Task CreateAgent_RejectsASupervisorThatCompletesWithoutInvokingTheCreationPipeAsync()
    {
        var supervisor = new NonInvokingSupervisor();
        IAsyncPipeContextAgent<ChildContext> asyncContext = supervisor.AddAsyncContext<ChildContext>();

        try
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                supervisor.CreateAgentAsync(
                    asyncContext,
                    (_, _) => Task.FromResult(new ChildContext()),
                    TestContext.Current.CancellationToken));

            Assert.Equal("The supervisor completed agent creation without publishing an outcome.", exception.Message);
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "outcome-publication-failure-is-observed")]
    public async Task CreateAgent_SurfacesAnOutcomePublicationFailureInsteadOfLeavingAnUnobservedTaskAsync()
    {
        var supervisor = new TestSupervisor();
        var expected = new OutcomePublicationFailureException();
        var asyncContext = new FailingOutcomeContext(expected);

        OutcomePublicationFailureException actual = await Assert.ThrowsAsync<OutcomePublicationFailureException>(() =>
            supervisor.CreateAgentAsync<OwnerContext, ChildContext>(
                asyncContext,
                (_, _) => Task.FromException<ChildContext>(new FactoryFailureException()),
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.True(asyncContext.PublicationAttempts >= 2);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "start-bridge-publishes-factory-fault")]
    public async Task StartAgent_TransfersTheExactFactoryFailureToTheOwnedAsyncContextAsync()
    {
        var supervisor = new TestSupervisor();
        IAsyncPipeContextAgent<ChildContext> asyncContext = supervisor.AddAsyncContext<ChildContext>();
        var expected = new FactoryFailureException();

        try
        {
            supervisor.StartAgent(
                asyncContext,
                (_, _) => Task.FromException<ChildContext>(expected),
                TestContext.Current.CancellationToken);

            FactoryFailureException actual = await Assert.ThrowsAsync<FactoryFailureException>(() => asyncContext.Context);

            Assert.Same(expected, actual);
            Assert.True(asyncContext.Context.IsFaulted);
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "start-bridge-publishes-created-context")]
    public async Task StartAgent_PublishesTheExactCreatedContextAndLeavesItUnderSupervisorLifecycleAsync()
    {
        var supervisor = new TestSupervisor();
        IAsyncPipeContextAgent<ChildContext> asyncContext = supervisor.AddAsyncContext<ChildContext>();
        var expected = new ChildContext();

        try
        {
            supervisor.StartAgent(
                asyncContext,
                (_, token) =>
                {
                    Assert.Equal(TestContext.Current.CancellationToken, token);
                    return Task.FromResult(expected);
                },
                TestContext.Current.CancellationToken);

            ChildContext actual = await asyncContext.Context;

            Assert.Same(expected, actual);
            Assert.Equal(1, supervisor.TotalCount);
            Assert.False(asyncContext.Completed.IsCompleted);
            Assert.Contains(
                "\"createAgent\"",
                JsonSerializer.Serialize(Assert.IsAssignableFrom<IProbeResult>(supervisor.LastPipeProbe).Results),
                StringComparison.Ordinal);
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
        }

        Assert.True(asyncContext.Completed.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "factory-cancellation-token-identity")]
    public async Task StartAgent_PreservesTheFactoryCancellationTokenAsync()
    {
        var supervisor = new TestSupervisor();
        IAsyncPipeContextAgent<ChildContext> asyncContext = supervisor.AddAsyncContext<ChildContext>();
        using var factoryCancellation = new CancellationTokenSource();
        factoryCancellation.Cancel();

        try
        {
            supervisor.StartAgent(
                asyncContext,
                (_, _) => Task.FromCanceled<ChildContext>(factoryCancellation.Token),
                TestContext.Current.CancellationToken);

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => asyncContext.Context);

            Assert.Equal(factoryCancellation.Token, actual.CancellationToken);
            await asyncContext.Completed;
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "supervisor-fault-is-transferred-to-pending-context")]
    public async Task CreateAgent_TransfersTheExactSupervisorFailureToThePendingContextAsync()
    {
        var expected = new SupervisorFailureException();
        var supervisor = new FailingSupervisor(Task.FromException(expected));
        IAsyncPipeContextAgent<ChildContext> asyncContext = supervisor.AddAsyncContext<ChildContext>();

        try
        {
            SupervisorFailureException actual = await Assert.ThrowsAsync<SupervisorFailureException>(() =>
                supervisor.CreateAgentAsync(
                    asyncContext,
                    (_, _) => Task.FromResult(new ChildContext()),
                    TestContext.Current.CancellationToken));
            SupervisorFailureException published = await Assert.ThrowsAsync<SupervisorFailureException>(() => asyncContext.Context);

            Assert.Same(expected, actual);
            Assert.Same(expected, published);
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "supervisor-cancellation-token-is-transferred-to-pending-context")]
    public async Task CreateAgent_TransfersTheExactSupervisorCancellationTokenToThePendingContextAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var supervisor = new FailingSupervisor(Task.FromCanceled(cancellation.Token));
        IAsyncPipeContextAgent<ChildContext> asyncContext = supervisor.AddAsyncContext<ChildContext>();

        try
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                supervisor.CreateAgentAsync(
                    asyncContext,
                    (_, _) => Task.FromResult(new ChildContext()),
                    TestContext.Current.CancellationToken));
            OperationCanceledException published = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => asyncContext.Context);

            Assert.Equal(cancellation.Token, actual.CancellationToken);
            Assert.Equal(cancellation.Token, published.CancellationToken);
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BACKGROUND-AGENT-OWNERSHIP", "supervisor-cancellation-without-token-uses-call-token")]
    public async Task CreateAgent_UsesTheCallTokenWhenSupervisorCancellationHasNoTokenAsync()
    {
        using var fallbackCancellation = new CancellationTokenSource();
        var fallbackSupervisor = new FailingSupervisor(Task.FromException(new OperationCanceledException()));
        IAsyncPipeContextAgent<ChildContext> fallbackContext = fallbackSupervisor.AddAsyncContext<ChildContext>();

        try
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                fallbackSupervisor.CreateAgentAsync(
                    fallbackContext,
                    (_, _) => Task.FromResult(new ChildContext()),
                    fallbackCancellation.Token));
            OperationCanceledException published = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                fallbackContext.Context);

            Assert.Equal(fallbackCancellation.Token, actual.CancellationToken);
            Assert.Equal(fallbackCancellation.Token, published.CancellationToken);
        }
        finally
        {
            await fallbackSupervisor.StopAsync(CancellationToken.None);
        }
    }

    private sealed class TestSupervisor : Supervisor, ISupervisor<OwnerContext>
    {
        private readonly OwnerContext _context = new();

        public IProbeResult? LastPipeProbe { get; private set; }

        public Task SendAsync(IPipe<OwnerContext> pipe, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastPipeProbe = pipe.GetProbeResult(cancellationToken);
            return pipe.SendAsync(_context);
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class NonInvokingSupervisor : Supervisor, ISupervisor<OwnerContext>
    {
        public Task SendAsync(IPipe<OwnerContext> pipe, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class FailingSupervisor(Task outcome) : Supervisor, ISupervisor<OwnerContext>
    {
        public Task SendAsync(IPipe<OwnerContext> pipe, CancellationToken cancellationToken = default) => outcome;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class FailingOutcomeContext(Exception failure) : IAsyncPipeContextAgent<ChildContext>
    {
        private readonly TaskCompletionSource<ChildContext> _context = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _publicationAttempts;

        public int PublicationAttempts => Volatile.Read(ref _publicationAttempts);

        public bool IsDisposed => false;

        public Task<ChildContext> Context => _context.Task;

        public Task Ready => _context.Task;

        public Task Completed => Task.CompletedTask;

        public CancellationToken Stopping => CancellationToken.None;

        public CancellationToken Stopped => CancellationToken.None;

        public Task CreatedAsync(ChildContext context) => Task.CompletedTask;

        public Task CreateCanceledAsync(CancellationToken cancellationToken) => PublishFailureAsync();

        public Task CreateFaultedAsync(Exception exception) => PublishFailureAsync();

        public Task FaultedAsync(Exception exception) => PublishFailureAsync();

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private Task PublishFailureAsync()
        {
            Interlocked.Increment(ref _publicationAttempts);
            return Task.FromException(failure);
        }
    }

    private sealed class OwnerContext : BasePipeContext;

    private sealed class ChildContext : BasePipeContext;

    private sealed class FactoryFailureException : Exception;

    private sealed class OutcomePublicationFailureException : Exception;

    private sealed class SupervisorFailureException : Exception;
}
