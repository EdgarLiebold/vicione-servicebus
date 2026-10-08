using System.Diagnostics;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Batching.Runtime;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class BatchActivityHandoffIsolationTests
{
    [Theory]
    [InlineData(false, DeliveryOutcome.Success)]
    [InlineData(true, DeliveryOutcome.Success)]
    [InlineData(false, DeliveryOutcome.Failure)]
    [InlineData(true, DeliveryOutcome.Failure)]
    [InlineData(false, DeliveryOutcome.Cancellation)]
    [InlineData(true, DeliveryOutcome.Cancellation)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "batch-handoff-preserves-required-pipeline-outcome")]
    public async Task Delivery_PreservesPipelineOutcomeWhenActivityHandoffObserverThrowsAsync(
        bool hostileHandoff, DeliveryOutcome outcome)
    {
        using var caller = new Activity("batch dispatcher caller").Start();
        await using var executor = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        using var admission = new Activity("batch admission").Start();
        Activity.Current = caller;
        using var contextCancellation = new CancellationTokenSource();
        var clock = new ObservableTimeProvider(new DateTimeOffset(2044, 5, 6, 7, 8, 9, TimeSpan.Zero));
        var requiredFailure = new InvalidOperationException("required batch pipeline failed");
        var requiredCancellation = new OperationCanceledException(contextCancellation.Token);
        var observerFailure = new InvalidOperationException("batch activity handoff observer failed");
        var item = new BatchItem("actual batch member");
        ConsumeContext<BatchItem> context = InMemoryOutboxTestContextFactory.Create(item, contextCancellation.Token);
        context.SetTimeProvider(clock);
        var pipe = new OutcomePipe(outcome, requiredFailure, requiredCancellation);
        var batch = new BatchConsumer<BatchItem>(
            new BatchRuntimeSettings(new BatchOptions
            {
                MessageLimit = 1,
                ConcurrencyLimit = 1,
                TimeLimit = TimeSpan.FromDays(1),
                TimeLimitStart = BatchTimeLimitStart.FromFirst,
            }),
            executor, dispatcher, pipe, clock);
        var handoffs = 0;
        void Changed(object? sender, ActivityChangedEventArgs args)
        {
            if (!ReferenceEquals(args.Previous, caller) || !ReferenceEquals(args.Current, admission))
                return;

            if (Interlocked.Increment(ref handoffs) == 1 && hostileHandoff)
                throw observerFailure;
        }

        Exception? escaped;
        Activity.CurrentChanged += Changed;
        try
        {
            await batch.AddAsync(context, admission, TestContext.Current.CancellationToken);
            escaped = await Record.ExceptionAsync(() => batch.ConsumeAsync(context)
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            await dispatcher.DisposeAsync();
            await executor.DisposeAsync();
        }
        finally
        {
            Activity.CurrentChanged -= Changed;
        }

        switch (outcome)
        {
            case DeliveryOutcome.Success:
                Assert.Null(escaped);
                break;
            case DeliveryOutcome.Failure:
                Assert.Same(requiredFailure, escaped);
                break;
            case DeliveryOutcome.Cancellation:
                OperationCanceledException canceled = Assert.IsAssignableFrom<OperationCanceledException>(escaped);
                Assert.Equal(contextCancellation.Token, canceled.CancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        Assert.Equal(1, handoffs);
        Assert.Equal(1, pipe.Calls);
        Assert.Same(admission, pipe.Activity);
        Assert.NotNull(pipe.Context);
        Assert.Equal(BatchCompletionMode.Size, pipe.Context.Message.Mode);
        Assert.Same(context, Assert.Single(pipe.Context.Message));
        Assert.Same(item, pipe.Context.Message[0].Message);
        Assert.Equal(contextCancellation.Token, pipe.Context.CancellationToken);
        Assert.True(batch.IsCompleted);
        Assert.Equal(1, clock.TimerCount);
        Assert.Equal(0, clock.ActiveTimerCount);
        Assert.Same(caller, Activity.Current);
    }

    public enum DeliveryOutcome
    {
        Success,
        Failure,
        Cancellation,
    }

    public sealed record BatchItem(string Value);

    private sealed class OutcomePipe(
        DeliveryOutcome outcome, Exception requiredFailure, OperationCanceledException requiredCancellation)
        : IPipe<ConsumeContext<IMessageBatch<BatchItem>>>
    {
        public int Calls { get; private set; }
        public Activity? Activity { get; private set; }
        public ConsumeContext<IMessageBatch<BatchItem>>? Context { get; private set; }

        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<IMessageBatch<BatchItem>> context)
        {
            Calls++;
            Activity = System.Diagnostics.Activity.Current;
            Context = context;
            return outcome switch
            {
                DeliveryOutcome.Success => Task.CompletedTask,
                DeliveryOutcome.Failure => Task.FromException(requiredFailure),
                DeliveryOutcome.Cancellation => Task.FromException(requiredCancellation),
                _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
            };
        }
    }
}
