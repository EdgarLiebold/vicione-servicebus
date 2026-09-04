using System.Globalization;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Agents;

public sealed class PipeContextSupervisorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-CACHE", "fault-invalidates-cache")]
    public async Task PipelineFailure_DiscardsTheFaultedContextBeforeTheNextSendAsync()
    {
        var factory = new TrackingContextFactory();
        var supervisor = new PipeContextSupervisor<TrackingContext>(factory);
        var observedContextIds = new List<string>();
        var callCount = 0;
        IPipe<TrackingContext> pipe = Pipe.New<TrackingContext>(configurator => configurator.UseExecute(context =>
        {
            observedContextIds.Add(context.Id);
            if (Interlocked.Increment(ref callCount) == 2)
                throw new PipelineFailureException();
        }));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await supervisor.SendAsync(pipe, cancellationToken);
            await Assert.ThrowsAsync<PipelineFailureException>(() => supervisor.SendAsync(pipe, cancellationToken));
            await supervisor.SendAsync(pipe, cancellationToken);
        }
        finally
        {
            await supervisor.StopAsync(cancellationToken);
            await supervisor.Completed.WaitAsync(cancellationToken);
        }

        Assert.Equal(["1", "1", "2"], observedContextIds);
        Assert.Equal(2, factory.Contexts.Count);
        Assert.All(factory.Contexts, context => Assert.Equal(1, context.DisposeCount));
        Assert.True(supervisor.Completed.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-CACHE", "explicit-invalidation-recreates-context")]
    public async Task ExplicitInvalidation_DiscardsTheContextBeforeTheNextSendAsync()
    {
        var factory = new TrackingContextFactory();
        var supervisor = new PipeContextSupervisor<TrackingContext>(factory);
        var observedContextIds = new List<string>();
        var callCount = 0;
        IPipe<TrackingContext> pipe = Pipe.New<TrackingContext>(configurator => configurator.UseExecuteAsync(async context =>
        {
            observedContextIds.Add(context.Id);
            if (Interlocked.Increment(ref callCount) == 2)
                await context.InvalidateAsync();
        }));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await supervisor.SendAsync(pipe, cancellationToken);
            await supervisor.SendAsync(pipe, cancellationToken);
            await supervisor.SendAsync(pipe, cancellationToken);
        }
        finally
        {
            await supervisor.StopAsync(cancellationToken);
            await supervisor.Completed.WaitAsync(cancellationToken);
        }

        Assert.Equal(["1", "1", "2"], observedContextIds);
        Assert.Equal(2, factory.Contexts.Count);
        Assert.All(factory.Contexts, context => Assert.Equal(1, context.DisposeCount));
        Assert.True(supervisor.Completed.IsCompletedSuccessfully);
    }

    private sealed class TrackingContextFactory : IPipeContextFactory<TrackingContext>
    {
        private long _nextId;

        public List<TrackingContext> Contexts { get; } = [];

        public IPipeContextAgent<TrackingContext> CreateContext(ISupervisor supervisor)
        {
            var context = new TrackingContext(Interlocked.Increment(ref _nextId).ToString(CultureInfo.InvariantCulture));
            IPipeContextAgent<TrackingContext> handle = supervisor.AddContext(context);
            context.SetInvalidation(handle.DisposeAsync);
            Contexts.Add(context);

            return handle;
        }

        public IActivePipeContextAgent<TrackingContext> CreateActiveContext(
            ISupervisor supervisor,
            PipeContextHandle<TrackingContext> context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); return supervisor.AddActiveContext(context, context.Context);
        }
    }

    private sealed class TrackingContext(string id) : BasePipeContext, IAsyncDisposable
    {
        private Func<ValueTask>? _invalidate;
        private int _disposeCount;

        public string Id { get; } = id;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void SetInvalidation(Func<ValueTask> invalidate)
        {
            _invalidate = invalidate;
        }

        public async Task InvalidateAsync()
        {
            Func<ValueTask> invalidate = _invalidate
                ?? throw new InvalidOperationException("The context has no invalidation handle.");

            await invalidate();
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PipelineFailureException : Exception
    {
    }
}
