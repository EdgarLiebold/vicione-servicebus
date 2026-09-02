using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.InMemoryOutbox;

public sealed class InMemoryOutboxFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-FILTER", "service-scope-payload-control")]
    public void ServiceScopePayload_IsPresentOnTheExactContextReadByTheFilter()
    {
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        ConsumeContext<FilterMessage> context = CreateContext();

        IServiceScope added = context.AddOrUpdatePayload<IServiceScope>(() => scope, existing => existing);

        Assert.Same(scope, added);
        Assert.True(context.TryGetPayload(out IServiceScope? found));
        Assert.Same(scope, found);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-FILTER", "missing-setter-leaves-foreign-scope-alone")]
    public async Task MissingBusBoundSetter_LeavesForeignScopeUntouchedAndContinuesThePipeline()
    {
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        ConsumeContext<FilterMessage> context = CreateContext();
        context.AddOrUpdatePayload<IServiceScope>(() => scope, existing => existing);
        ConsumeContext<FilterMessage>? delivered = null;

        await CreateFilter(null).Send(
            context,
            Pipe(innerContext =>
            {
                delivered = innerContext;
                return Task.CompletedTask;
            }));

        Assert.Null(scope.ServiceProvider.GetService<IScopedConsumeContextProvider>());
        Assert.NotNull(delivered);
        Assert.IsType<InMemoryOutboxConsumeContext<FilterMessage>>(delivered);
        Assert.True(context.TryGetPayload(out IServiceScope? found));
        Assert.Same(scope, found);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-FILTER", "failed-consume-discards-pending-actions")]
    public async Task FailedConsume_DiscardsPendingActionAndPropagatesTheOriginalException()
    {
        ConsumeContext<FilterMessage> context = CreateContext();
        var events = new List<string>();
        var expected = new ExpectedFilterException("consume failed");
        OutboxContext? capturedOutbox = null;

        ExpectedFilterException actual = await Assert.ThrowsAsync<ExpectedFilterException>(() =>
            CreateFilter(null).Send(
                context,
                Pipe(async innerContext =>
                {
                    capturedOutbox = Assert.IsAssignableFrom<OutboxContext>(innerContext);
                    await capturedOutbox.Add(() =>
                    {
                        events.Add("pending action ran");
                        return Task.CompletedTask;
                    });
                    events.Add("consume failed");
                    throw expected;
                })));

        Assert.Same(expected, actual);
        Assert.Equal(["consume failed"], events);
        Assert.NotNull(capturedOutbox);

        await capturedOutbox.ExecutePendingActions(concurrentMessageDelivery: false);

        Assert.Equal(["consume failed"], events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-FILTER", "pending-actions-run-after-successful-consume")]
    public async Task SuccessfulConsume_ExecutesPendingActionOnlyAfterThePipeCompletes()
    {
        ConsumeContext<FilterMessage> context = CreateContext();
        var events = new List<string>();

        await CreateFilter(null).Send(
            context,
            Pipe(async innerContext =>
            {
                OutboxContext outbox = Assert.IsAssignableFrom<OutboxContext>(innerContext);
                await outbox.Add(() =>
                {
                    events.Add("pending action ran");
                    return Task.CompletedTask;
                });
                events.Add("consume completed");
            }));

        Assert.Equal(["consume completed", "pending action ran"], events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-FILTER", "scope-restored-after-success")]
    public async Task SuccessfulConsume_RestoresScopedContextAfterPendingActions()
    {
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        ConsumeContext<FilterMessage> context = CreateContext();
        context.AddOrUpdatePayload<IServiceScope>(() => scope, existing => existing);
        var events = new List<string>();
        var setter = new RecordingSetter(events);

        await CreateFilter(setter).Send(
            context,
            Pipe(async innerContext =>
            {
                OutboxContext outbox = Assert.IsAssignableFrom<OutboxContext>(innerContext);
                await outbox.Add(() =>
                {
                    events.Add("pending action ran");
                    return Task.CompletedTask;
                });
                events.Add("consume completed");
            }));

        Assert.Same(scope, setter.Scope);
        Assert.IsType<InMemoryOutboxConsumeContext<FilterMessage>>(setter.Context);
        Assert.Equal(
            ["scope context replaced", "consume completed", "pending action ran", "scope context restored"],
            events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-FILTER", "scope-restored-after-failure")]
    public async Task FailedConsume_RestoresScopedContextAndPropagatesTheOriginalException()
    {
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        ConsumeContext<FilterMessage> context = CreateContext();
        context.AddOrUpdatePayload<IServiceScope>(() => scope, existing => existing);
        var events = new List<string>();
        var setter = new RecordingSetter(events);
        var expected = new ExpectedFilterException("consume failed");

        ExpectedFilterException actual = await Assert.ThrowsAsync<ExpectedFilterException>(() =>
            CreateFilter(setter).Send(
                context,
                Pipe(innerContext =>
                {
                    Assert.IsType<InMemoryOutboxConsumeContext<FilterMessage>>(innerContext);
                    events.Add("consume failed");
                    throw expected;
                })));

        Assert.Same(expected, actual);
        Assert.Same(scope, setter.Scope);
        Assert.IsType<InMemoryOutboxConsumeContext<FilterMessage>>(setter.Context);
        Assert.Equal(
            ["scope context replaced", "consume failed", "scope context restored"],
            events);
    }

    private static ConsumeContext<FilterMessage> CreateContext() =>
        InMemoryOutboxTestContextFactory.Create(new FilterMessage(Guid.NewGuid()));

    private static InMemoryOutboxFilter<ConsumeContext<FilterMessage>, InMemoryOutboxConsumeContext<FilterMessage>>
        CreateFilter(ISetScopedConsumeContext? setter) =>
        new(setter!, innerContext => new InMemoryOutboxConsumeContext<FilterMessage>(innerContext), false);

    private static IPipe<ConsumeContext<FilterMessage>> Pipe(
        Func<ConsumeContext<FilterMessage>, Task> callback) =>
        new DelegatePipe<ConsumeContext<FilterMessage>>(callback);

    public sealed record FilterMessage(Guid Id);

    private sealed class ExpectedFilterException(string message) : Exception(message);

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task Send(TContext context) => callback(context);

        public void Probe(ProbeContext context) => context.CreateFilterScope("test");
    }

    private sealed class RecordingSetter(List<string> events) : ISetScopedConsumeContext
    {
        public IServiceScope? Scope { get; private set; }

        public ConsumeContext? Context { get; private set; }

        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context)
        {
            Scope = serviceProvider;
            Context = context;
            events.Add("scope context replaced");
            return new Restore(events);
        }

        private sealed class Restore(List<string> events) : IDisposable
        {
            public void Dispose() => events.Add("scope context restored");
        }
    }
}
