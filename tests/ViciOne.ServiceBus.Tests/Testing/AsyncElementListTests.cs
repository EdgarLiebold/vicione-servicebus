using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class AsyncElementListTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "existing-any-and-selection")]
    public async Task ExistingMessage_IsReturnedByAnyAndSelectAsync()
    {
        var messages = CreateList();
        Add(messages, new MessageA("expected"));

        Assert.True(await messages.AnyAsync<MessageA>(TestContext.Current.CancellationToken));
        ISentMessage<MessageA> selected = await messages
            .SelectAsync<MessageA>(TestContext.Current.CancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("expected", selected.Context.Message.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "first-included-type")]
    public async Task IncludeFilter_MatchesTheFirstConfiguredTypeAsync()
    {
        var messages = CreateList();
        Add(messages, new MessageA("first"));

        bool found = await messages.AnyAsync(
            filter => filter.Includes.Add<MessageA>().Add<MessageB>(),
            TestContext.Current.CancellationToken);

        Assert.True(found);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "second-included-type")]
    public async Task IncludeFilter_MatchesTheSecondConfiguredTypeAsync()
    {
        var messages = CreateList();
        Add(messages, new MessageB("second"));

        bool found = await messages.AnyAsync(
            filter => filter.Includes.Add<MessageA>().Add<MessageB>(),
            TestContext.Current.CancellationToken);

        Assert.True(found);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "excluded-type")]
    public async Task ExcludeFilter_RejectsTheExcludedTypeAsync()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        var messages = new SentMessageList(TimeSpan.FromMinutes(1), CancellationToken.None, timeProvider);
        Add(messages, new MessageB("excluded"));

        Task<bool> observation = messages.AnyAsync(
            filter => filter.Excludes.Add<MessageB>(),
            TestContext.Current.CancellationToken);

        Assert.False(observation.IsCompleted);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        Assert.False(await observation);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "context-pattern")]
    public async Task Predicate_CanMatchTheMessageContextByPatternAsync()
    {
        var messages = CreateList();
        Add(messages, new MessageB("pattern"));

        bool found = await messages.AnyAsync(
            message => message switch
            {
                (MessageA value, _) => value.Value == "pattern",
                (MessageB value, _) => value.Value == "pattern",
                _ => false,
            },
            TestContext.Current.CancellationToken);

        Assert.True(found);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "later-arrival")]
    public async Task MessageAddedAfterObservationStarts_CompletesTheObservationAsync()
    {
        var messages = CreateList();
        Task<bool> observation = messages.AnyAsync<MessageA>(TestContext.Current.CancellationToken);

        Assert.False(observation.IsCompleted);

        Add(messages, new MessageA("later"));

        Assert.True(await observation);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "observation-cancellation")]
    public async Task CompletionToken_EndsObservationWithoutReturningAnElementAsync()
    {
        using var completed = new CancellationTokenSource();
        var messages = new SentMessageList(TimeSpan.FromMinutes(1), completed.Token, new FakeTimeProvider(StartTime));
        Task<int> observation = messages
            .SelectAsync<MessageA>(TestContext.Current.CancellationToken)
            .CountObservedAsync(TestContext.Current.CancellationToken);

        Assert.False(observation.IsCompleted);

        completed.Cancel();

        Assert.Equal(0, await observation);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "virtual-timeout")]
    public async Task AdvancingTheConfiguredTimeout_ReturnsFalseWithoutWallClockWaitingAsync()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        var messages = new SentMessageList(TimeSpan.FromMinutes(1), CancellationToken.None, timeProvider);
        Task<bool> observation = messages.AnyAsync<MessageA>(TestContext.Current.CancellationToken);

        Assert.False(observation.IsCompleted);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        Assert.False(await observation);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "synchronous-virtual-timeout")]
    public async Task SynchronousSelection_UsesTheSameVirtualTimeoutAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var messages = new SentMessageList(TimeSpan.FromMinutes(1), CancellationToken.None, timeProvider);
        Task<int> observation = Task.Run(
            () => messages.Select<MessageA>(TestContext.Current.CancellationToken).Count(),
            TestContext.Current.CancellationToken);

        await timeProvider.WaitForTimerCountAsync(1);
        timeProvider.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(0, await observation);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "synchronous-filter-outside-monitor")]
    public void SynchronousFilter_DoesNotBlockAConcurrentProducer()
    {
        var messages = CreateList();
        Add(messages, new MessageA("first"));

        ISentMessage<MessageA> first = messages.Select<MessageA>(
            message =>
            {
                var producer = new Thread(() => Add(messages, new MessageB("second")))
                {
                    IsBackground = true,
                };
                producer.Start();
                producer.Join();

                return message.Context.Message.Value == "first";
            },
            TestContext.Current.CancellationToken).First();

        ISentMessage<MessageB> second = messages
            .Select<MessageB>(TestContext.Current.CancellationToken)
            .First();

        Assert.Equal("first", first.Context.Message.Value);
        Assert.Equal("second", second.Context.Message.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-ELEMENT-LIST", "null-time-provider")]
    public void ProviderAwareConstruction_RejectsNullTimeProvider()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new SentMessageList(TimeSpan.FromMinutes(1), CancellationToken.None, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    private static SentMessageList CreateList() =>
        new(TimeSpan.FromMinutes(1), CancellationToken.None, new FakeTimeProvider(StartTime));

    private static void Add<T>(SentMessageList messages, T message)
        where T : class
    {
        messages.Add(new MessageSendContext<T>(message));
    }

    private sealed record MessageA(string Value);

    private sealed record MessageB(string Value);
}
