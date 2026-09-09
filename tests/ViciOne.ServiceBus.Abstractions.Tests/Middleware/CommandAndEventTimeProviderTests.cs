using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware;

public sealed class CommandAndEventTimeProviderTests
{
    private static readonly DateTimeOffset ObservationTime = new(2038, 9, 10, 11, 12, 13, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-COMMAND-EVENT-CLOCK", "command-context-owner")]
    public async Task SentCommand_CarriesTheConfiguredClockAndItsExactUtcTimestampAsync()
    {
        var clock = new FakeTimeProvider(ObservationTime);
        var pipe = new CapturePipe<CommandContext>();
        var command = new RuntimeCommand("limit");

        await pipe.SendCommandAsync(command, clock, cancellationToken: TestContext.Current.CancellationToken);

        CommandContext<RuntimeCommand> context = Assert.IsAssignableFrom<CommandContext<RuntimeCommand>>(pipe.Context);
        Assert.Same(command, context.Command);
        Assert.Equal(ObservationTime.UtcDateTime, context.Timestamp);
        Assert.Same(clock, context.GetTimeProvider());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COMMAND-EVENT-CLOCK", "command-cancellation-owner")]
    public async Task SentCommand_CarriesTheCallersCancellationTokenAsync()
    {
        var pipe = new CapturePipe<CommandContext>();
        using var cancellation = new CancellationTokenSource();

        await pipe.SendCommandAsync(new RuntimeCommand("cancelable"), cancellationToken: cancellation.Token);

        Assert.Equal(cancellation.Token, pipe.Context.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COMMAND-EVENT-CLOCK", "event-context-owner")]
    public async Task PublishedEvent_CarriesTheConfiguredClockAndItsExactUtcTimestampAsync()
    {
        var clock = new FakeTimeProvider(ObservationTime);
        var pipe = new CapturePipe<EventContext>();
        var message = new RuntimeEvent("opened");

        await pipe.PublishEventAsync(message, clock, cancellationToken: TestContext.Current.CancellationToken);

        EventContext<RuntimeEvent> context = Assert.IsAssignableFrom<EventContext<RuntimeEvent>>(pipe.Context);
        Assert.Same(message, context.Event);
        Assert.Equal(ObservationTime.UtcDateTime, context.Timestamp);
        Assert.Same(clock, context.GetTimeProvider());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COMMAND-EVENT-CLOCK", "event-cancellation-owner")]
    public async Task PublishedEvent_CarriesTheCallersCancellationTokenAsync()
    {
        var pipe = new CapturePipe<EventContext>();
        using var cancellation = new CancellationTokenSource();

        await pipe.PublishEventAsync(new RuntimeEvent("cancelable"), cancellationToken: cancellation.Token);

        Assert.Equal(cancellation.Token, pipe.Context.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COMMAND-EVENT-CANCELLATION", "pre-canceled-command-does-not-create-or-dispatch-context")]
    public async Task PreCanceledCommand_PreservesItsTokenWithoutReadingTheClockOrInvokingThePipeAsync()
    {
        var pipe = new CapturePipe<CommandContext>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            pipe.SendCommandAsync(new RuntimeCommand("canceled"), new ThrowingTimeProvider(), cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, pipe.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COMMAND-EVENT-CANCELLATION", "pre-canceled-event-does-not-create-or-dispatch-context")]
    public async Task PreCanceledEvent_PreservesItsTokenWithoutReadingTheClockOrInvokingThePipeAsync()
    {
        var pipe = new CapturePipe<EventContext>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            pipe.PublishEventAsync(new RuntimeEvent("canceled"), new ThrowingTimeProvider(), cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, pipe.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COMMAND-EVENT-CLOCK", "concurrency-limit-command-owner")]
    public async Task ConcurrencyLimitCommand_UsesOneClockForEnvelopeAndPayloadTimestampsAsync()
    {
        var clock = new FakeTimeProvider(ObservationTime);
        var pipe = new CapturePipe<CommandContext>();

        await pipe.SetConcurrencyLimitAsync(7, clock, cancellationToken: TestContext.Current.CancellationToken);

        CommandContext<SetConcurrencyLimit> context = Assert.IsAssignableFrom<CommandContext<SetConcurrencyLimit>>(pipe.Context);
        Assert.Equal(ObservationTime.UtcDateTime, context.Timestamp);
        Assert.Equal(ObservationTime.UtcDateTime, context.Command.Timestamp);
        Assert.Equal(7, context.Command.ConcurrencyLimit);
        Assert.Same(clock, context.GetTimeProvider());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTROL-COMMANDS", "pre-canceled-limits-do-not-dispatch-or-read-clock")]
    public async Task PreCanceledLimitCommands_PreserveTheirTokenWithoutInvokingThePipeOrClockAsync()
    {
        var pipe = new CapturePipe<CommandContext>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException concurrencyException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            pipe.SetConcurrencyLimitAsync(2, new ThrowingTimeProvider(), cancellation.Token));
        OperationCanceledException rateException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            pipe.SetRateLimitAsync(3, cancellation.Token));

        Assert.Equal(cancellation.Token, concurrencyException.CancellationToken);
        Assert.Equal(cancellation.Token, rateException.CancellationToken);
        Assert.Equal(0, pipe.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTROL-COMMANDS", "invalid-limit-boundaries")]
    public void LimitCommands_RejectNullPipelinesAndNonPositiveLimitsImmediately()
    {
        var pipe = new CapturePipe<CommandContext>();
        IPipe<CommandContext> nullPipe = null!;

        ArgumentNullException nullRatePipe = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = nullPipe.SetRateLimitAsync(1, TestContext.Current.CancellationToken);
        });
        ArgumentNullException nullConcurrencyPipe = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = nullPipe.SetConcurrencyLimitAsync(1, cancellationToken: TestContext.Current.CancellationToken);
        });
        ArgumentOutOfRangeException rateLimit = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = pipe.SetRateLimitAsync(0, TestContext.Current.CancellationToken);
        });
        ArgumentOutOfRangeException concurrencyLimit = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = pipe.SetConcurrencyLimitAsync(0, cancellationToken: TestContext.Current.CancellationToken);
        });

        Assert.Equal("pipe", nullRatePipe.ParamName);
        Assert.Equal("pipe", nullConcurrencyPipe.ParamName);
        Assert.Equal("rateLimit", rateLimit.ParamName);
        Assert.Equal(0, rateLimit.ActualValue);
        Assert.Equal("concurrencyLimit", concurrencyLimit.ParamName);
        Assert.Equal(0, concurrencyLimit.ActualValue);
    }

    private sealed record RuntimeCommand(string Value);

    private sealed record RuntimeEvent(string Value);

    private sealed class CapturePipe<TContext> : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public TContext Context { get; private set; } = null!;

        public int InvocationCount { get; private set; }

        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context)
        {
            InvocationCount++;
            Context = context;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            throw new InvalidOperationException("A pre-canceled operation must not read the clock.");
    }
}
