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
    public async Task SentCommand_CarriesTheConfiguredClockAndItsExactUtcTimestamp()
    {
        var clock = new FakeTimeProvider(ObservationTime);
        var pipe = new CapturePipe<CommandContext>();
        var command = new RuntimeCommand("limit");

        await pipe.SendCommand(command, clock);

        CommandContext<RuntimeCommand> context = Assert.IsAssignableFrom<CommandContext<RuntimeCommand>>(pipe.Context);
        Assert.Same(command, context.Command);
        Assert.Equal(ObservationTime.UtcDateTime, context.Timestamp);
        Assert.Same(clock, context.GetTimeProvider());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COMMAND-EVENT-CLOCK", "event-context-owner")]
    public async Task PublishedEvent_CarriesTheConfiguredClockAndItsExactUtcTimestamp()
    {
        var clock = new FakeTimeProvider(ObservationTime);
        var pipe = new CapturePipe<EventContext>();
        var message = new RuntimeEvent("opened");

        await pipe.PublishEvent(message, clock);

        EventContext<RuntimeEvent> context = Assert.IsAssignableFrom<EventContext<RuntimeEvent>>(pipe.Context);
        Assert.Same(message, context.Event);
        Assert.Equal(ObservationTime.UtcDateTime, context.Timestamp);
        Assert.Same(clock, context.GetTimeProvider());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COMMAND-EVENT-CLOCK", "concurrency-limit-command-owner")]
    public async Task ConcurrencyLimitCommand_UsesOneClockForEnvelopeAndPayloadTimestamps()
    {
        var clock = new FakeTimeProvider(ObservationTime);
        var pipe = new CapturePipe<CommandContext>();

        await pipe.SetConcurrencyLimit(7, clock);

        CommandContext<SetConcurrencyLimit> context = Assert.IsAssignableFrom<CommandContext<SetConcurrencyLimit>>(pipe.Context);
        Assert.Equal(ObservationTime.UtcDateTime, context.Timestamp);
        Assert.Equal(ObservationTime.UtcDateTime, context.Command.Timestamp);
        Assert.Equal(7, context.Command.ConcurrencyLimit);
        Assert.Same(clock, context.GetTimeProvider());
    }

    private sealed record RuntimeCommand(string Value);

    private sealed record RuntimeEvent(string Value);

    private sealed class CapturePipe<TContext> : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public TContext Context { get; private set; } = null!;

        public void Probe(ProbeContext context)
        {
        }

        public Task Send(TContext context)
        {
            Context = context;
            return Task.CompletedTask;
        }
    }
}
