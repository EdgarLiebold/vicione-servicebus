using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Agents;

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
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddContext((ChildContext)null!)).ParamName);
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddContext((Task<ChildContext>)null!)).ParamName);
            Assert.Equal("contextHandle", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddActiveContext(null!, child)).ParamName);
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
                supervisor.AddActiveContext(asyncContext, (ChildContext)null!)).ParamName);
            Assert.Equal("agentFactory", Assert.Throws<ArgumentNullException>(() =>
                supervisor.StartAgent(asyncContext, null!, CancellationToken.None)).ParamName);

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
        }
        finally
        {
            await supervisor.StopAsync(CancellationToken.None);
        }

        Assert.True(asyncContext.Completed.IsCompletedSuccessfully);
    }

    private sealed class TestSupervisor : Supervisor, ISupervisor<OwnerContext>
    {
        private readonly OwnerContext _context = new();

        public Task SendAsync(IPipe<OwnerContext> pipe, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return pipe.SendAsync(_context);
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class OwnerContext : BasePipeContext;

    private sealed class ChildContext : BasePipeContext;

    private sealed class FactoryFailureException : Exception;
}
