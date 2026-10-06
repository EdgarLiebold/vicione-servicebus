using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusScheduleProviderOwnershipTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-ASB-SCHEDULER-CANCELLATION", "schedule-provider-joins-resolution-and-applied-pipe-before-token-result")]
    public async Task ScheduleSendAsync_JoinsResolutionAndAppliedPipeAndReturnsTheAcceptedTokenAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(caller.Token, cancel: false);
        var suppliedPipe = new RecordingSendPipe();
        var failure = new ExpectedSendFailure();
        Task<ScheduledMessage<ScheduleBody>>? pending = null;
        try
        {
            pending = new ServiceBusScheduleMessageProvider(fixture.Provider).ScheduleSendAsync(
                fixture.Destination, fixture.DueAt, fixture.Payload, suppliedPipe, caller.Token);
            await EntryOrOutcomeAsync(fixture.ResolutionEntered.Task, pending);
            Assert.Equal(1, fixture.ResolveCalls);
            Assert.False(fixture.ActualResolutionTask!.IsCompleted);
            Assert.False(pending.IsCompleted);
            Assert.Equal(0, fixture.SendCalls);
            Assert.Equal(0, suppliedPipe.Calls);
            fixture.Resolution.TrySetResult(fixture.Endpoint);
            await EntryOrOutcomeAsync(fixture.SendEntered.Task, pending);
            await EntryOrOutcomeAsync(fixture.PipeApplied.Task, pending);
            Assert.Equal(1, fixture.SendCalls);
            Assert.NotNull(fixture.ActualSendTask);
            Assert.False(fixture.ActualSendTask.IsCompleted);
            Assert.False(pending.IsCompleted);
            Assert.Equal(1, suppliedPipe.Calls);
            Assert.Same(fixture.Context, suppliedPipe.Context);
            Assert.Same(fixture.Payload, fixture.Context.Message);
            Assert.Equal(caller.Token, fixture.Context.CancellationToken);
            Assert.Equal(fixture.DueAt, fixture.Context.ScheduledEnqueueTimeUtc);
            Assert.Equal("scheduled-pipe-observed", fixture.Context.Headers.Get<string>("owner-marker"));
            Assert.Null(fixture.Context.ScheduledMessageId);

            if (outcome == 0)
            {
                fixture.Send.TrySetResult();
                ScheduledMessage<ScheduleBody> result = await pending.WaitAsync(Timeout, TestToken);
                Assert.True(pending.IsCompletedSuccessfully);
                Assert.True(fixture.ActualSendTask.IsCompletedSuccessfully);
                Assert.True(fixture.Context.TryGetScheduledMessageId(out long sequence));
                Assert.Equal(4242, sequence);
                Assert.Equal(fixture.Context.ScheduledMessageId!.Value, result.TokenId);
                Assert.Equal(fixture.DueAt, result.DueAt);
                Assert.Same(fixture.Destination, result.Destination);
                Assert.Same(fixture.Payload, result.Payload);
            }
            else
                await ReleaseAndAssertOutcomeAsync(outcome, caller, fixture, pending, failure);
        }
        finally
        {
            await fixture.DrainAsync(pending, failure, caller.Token);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-ASB-SCHEDULER-CANCELLATION", "destination-cancel-provider-joins-resolution-and-exact-command-operation")]
    public async Task CancelScheduledSendAsync_JoinsResolutionAndSendsTheExactDestinationCommandAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(caller.Token, cancel: true);
        var failure = new ExpectedSendFailure();
        Task? pending = null;
        try
        {
            pending = new ServiceBusScheduleMessageProvider(fixture.Provider).CancelScheduledSendAsync(
                fixture.Destination, fixture.CancelTokenId, caller.Token);
            await EntryOrOutcomeAsync(fixture.ResolutionEntered.Task, pending);
            Assert.Equal(1, fixture.ResolveCalls);
            Assert.False(fixture.ActualResolutionTask!.IsCompleted);
            Assert.False(pending.IsCompleted);
            Assert.Equal(0, fixture.SendCalls);
            fixture.Resolution.TrySetResult(fixture.Endpoint);
            await EntryOrOutcomeAsync(fixture.SendEntered.Task, pending);
            Assert.Equal(1, fixture.SendCalls);
            Assert.NotNull(fixture.ActualSendTask);
            Assert.False(fixture.ActualSendTask.IsCompleted);
            Assert.False(pending.IsCompleted);
            Assert.Equal(typeof(CancelScheduledMessage), fixture.RequestedContract);
            Assert.NotNull(fixture.CommandValues);
            object values = fixture.CommandValues;
            Assert.Equal(fixture.CancelTokenId, Assert.IsType<Guid>(values.GetType().GetProperty("TokenId")!.GetValue(values)));
            DateTimeOffset timestamp = Assert.IsType<DateTimeOffset>(values.GetType().GetProperty("Timestamp")!.GetValue(values));
            Assert.NotEqual(default(DateTimeOffset), timestamp);
            Assert.Equal(TimeSpan.Zero, timestamp.Offset);
            Assert.Null(fixture.Context.ScheduledMessageId);
            if (outcome == 0)
            {
                fixture.Send.TrySetResult();
                await pending.WaitAsync(Timeout, TestToken);
                Assert.True(pending.IsCompletedSuccessfully);
                Assert.True(fixture.ActualSendTask.IsCompletedSuccessfully);
            }
            else
                await ReleaseAndAssertOutcomeAsync(outcome, caller, fixture, pending, failure);
        }
        finally
        {
            await fixture.DrainAsync(pending, failure, caller.Token);
        }
    }

    private static async Task ReleaseAndAssertOutcomeAsync(int outcome, CancellationTokenSource caller, Fixture fixture,
        Task pending, ExpectedSendFailure failure)
    {
        if (outcome == 1)
        {
            fixture.Send.TrySetException(failure);
            Assert.Same(failure, await Assert.ThrowsAsync<ExpectedSendFailure>(() => pending.WaitAsync(Timeout, TestToken)));
            Assert.True(pending.IsFaulted);
            Assert.True(fixture.ActualSendTask!.IsFaulted);
        }
        else
        {
            caller.Cancel();
            fixture.Send.TrySetCanceled(caller.Token);
            OperationCanceledException cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => pending.WaitAsync(Timeout, TestToken));
            Assert.Equal(caller.Token, cancellation.CancellationToken);
            Assert.True(pending.IsCanceled);
            Assert.True(fixture.ActualSendTask!.IsCanceled);
        }
        Assert.Null(fixture.Context.ScheduledMessageId);
        Assert.Equal(1, fixture.ResolveCalls);
        Assert.Equal(1, fixture.SendCalls);
    }

    private static async Task EntryOrOutcomeAsync(Task entered, Task pending)
    {
        await Task.WhenAny(entered, pending).WaitAsync(Timeout, TestToken);
        if (!entered.IsCompleted)
            await pending.WaitAsync(Timeout, TestToken);
        Assert.True(entered.IsCompletedSuccessfully);
    }

    private static async Task ObserveAsync(Task? task, Exception failure, CancellationToken caller)
    {
        if (task is null)
            return;
        try
        {
            await task.WaitAsync(Timeout, CancellationToken.None);
        }
        catch (Exception observed) when (task.IsFaulted && ReferenceEquals(observed, failure))
        {
        }
        catch (OperationCanceledException observed) when (task.IsCanceled && observed.CancellationToken == caller)
        {
        }
        Assert.True(task.IsCompleted);
    }

    private sealed record ScheduleBody(string Value);
    private sealed class ExpectedSendFailure : Exception;
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class Fixture
    {
        private readonly CancellationToken _caller;
        private readonly bool _cancel;
        public Fixture(CancellationToken caller, bool cancel)
        {
            _caller = caller;
            _cancel = cancel;
            Context = new AzureServiceBusSendContext<ScheduleBody>(Payload, caller);
            Context.GetOrAddPayload<TimeProvider>(() => new FixedClock());
            Endpoint = DispatchProxy.Create<IAdvancedSendEndpoint, RecordingProxy>();
            ((RecordingProxy)Endpoint).Handler = SendInvoke;
            Provider = DispatchProxy.Create<ISendEndpointProvider, RecordingProxy>();
            ((RecordingProxy)Provider).Handler = ResolveInvoke;
        }
        public Uri Destination { get; } = new("sb://schedule-provider.servicebus.invalid/exact-destination");
        public DateTimeOffset DueAt { get; } = new(2026, 10, 4, 12, 17, 0, TimeSpan.Zero);
        public Guid CancelTokenId { get; } = Guid.Parse("44444444-5555-6666-7777-888888888888");
        public ScheduleBody Payload { get; } = new("scheduled-payload");
        public AzureServiceBusSendContext<ScheduleBody> Context { get; }
        public IAdvancedSendEndpoint Endpoint { get; }
        public ISendEndpointProvider Provider { get; }
        public TaskCompletionSource<ISendEndpoint> Resolution { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Send { get; } = NewSignal();
        public TaskCompletionSource ResolutionEntered { get; } = NewSignal();
        public TaskCompletionSource SendEntered { get; } = NewSignal();
        public TaskCompletionSource PipeApplied { get; } = NewSignal();
        public Task<ISendEndpoint>? ActualResolutionTask { get; private set; }
        public Task? ActualSendTask { get; private set; }
        public int ResolveCalls { get; private set; }
        public int SendCalls { get; private set; }
        public Type? RequestedContract { get; private set; }
        public object? CommandValues { get; private set; }

        private object ResolveInvoke(MethodInfo method, object?[] args)
        {
            Assert.Equal(nameof(ISendEndpointProvider.GetSendEndpointAsync), method.Name);
            Assert.Same(Destination, args[0]);
            Assert.Equal(_caller, Assert.IsType<CancellationToken>(args[^1]));
            ResolveCalls++;
            ActualResolutionTask = Resolution.Task;
            Assert.True(method.ReturnType.IsInstanceOfType(ActualResolutionTask));
            ResolutionEntered.TrySetResult();
            return ActualResolutionTask;
        }
        private object SendInvoke(MethodInfo method, object?[] args)
        {
            Assert.Equal(nameof(ISendEndpoint.SendAsync), method.Name);
            Assert.True(method.IsGenericMethod);
            RequestedContract = Assert.Single(method.GetGenericArguments());
            Assert.Equal(_caller, Assert.IsType<CancellationToken>(args[^1]));
            SendCalls++;
            if (_cancel)
            {
                Assert.Equal(2, args.Length);
                Assert.Equal(typeof(CancelScheduledMessage), RequestedContract);
                CommandValues = args[0];
                ActualSendTask = Send.Task;
                SendEntered.TrySetResult();
            }
            else
            {
                Assert.Equal(3, args.Length);
                Assert.Equal(typeof(ScheduleBody), RequestedContract);
                Assert.Same(Payload, args[0]);
                var pipe = Assert.IsAssignableFrom<IPipe<SendContext<ScheduleBody>>>(args[1]);
                ActualSendTask = SendScheduledAsync(pipe);
                SendEntered.TrySetResult();
            }
            Assert.True(method.ReturnType.IsInstanceOfType(ActualSendTask));
            return ActualSendTask;
        }
        private async Task SendScheduledAsync(IPipe<SendContext<ScheduleBody>> pipe)
        {
            await pipe.SendAsync(Context).ConfigureAwait(false);
            PipeApplied.TrySetResult();
            await Send.Task.ConfigureAwait(false);
            Context.SetScheduledMessageId(4242);
        }
        public async Task DrainAsync(Task? pending, Exception failure, CancellationToken caller)
        {
            Resolution.TrySetResult(Endpoint);
            Send.TrySetResult();
            try
            {
                await ObserveAsync(ActualResolutionTask, failure, caller);
            }
            finally
            {
                try
                {
                    await ObserveAsync(pending, failure, caller);
                }
                finally
                {
                    try
                    {
                        await ObserveAsync(ActualSendTask, failure, caller);
                    }
                    finally
                    {
                        await ObserveAsync(Send.Task, failure, caller);
                    }
                }
            }
        }
    }

    public class RecordingProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            Handler(method ?? throw new InvalidOperationException("Missing proxy method."), args ?? []);
    }
    private sealed class RecordingSendPipe : IPipe<SendContext<ScheduleBody>>
    {
        public int Calls { get; private set; }
        public SendContext<ScheduleBody>? Context { get; private set; }
        public Task SendAsync(SendContext<ScheduleBody> context)
        {
            Calls++;
            Context = context;
            context.Headers.Set("owner-marker", "scheduled-pipe-observed");
            return Task.CompletedTask;
        }
        public void Probe(ProbeContext context) { }
    }
    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
