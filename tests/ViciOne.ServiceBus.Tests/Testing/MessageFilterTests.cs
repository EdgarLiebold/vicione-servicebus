using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class MessageFilterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-FILTER", "empty-set-wildcard-semantics")]
    public void EmptyFilterSet_IsAnExplicitWildcardForAnyButAnEmptyExclusionForNone()
    {
        var filters = new SubjectFilterSet();
        var subject = new FilterSubject(5);

        Assert.True(filters.All(subject));
        Assert.True(filters.Any(subject));
        Assert.False(filters.NotAny(subject));
        Assert.True(filters.None(subject));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-FILTER", "all-any-and-none-composition")]
    public void PopulatedFilterSet_ComposesAllAnyNotAnyAndNoneFromTheSamePredicates()
    {
        var filters = new SubjectFilterSet()
            .AddSubject(subject => subject.Value >= 10)
            .AddSubject(subject => subject.Value % 2 == 0);

        var both = new FilterSubject(12);
        var one = new FilterSubject(11);
        var neither = new FilterSubject(3);

        Assert.True(filters.All(both));
        Assert.True(filters.Any(both));
        Assert.False(filters.NotAny(both));
        Assert.False(filters.None(both));

        Assert.False(filters.All(one));
        Assert.True(filters.Any(one));
        Assert.False(filters.NotAny(one));
        Assert.False(filters.None(one));

        Assert.False(filters.All(neither));
        Assert.False(filters.Any(neither));
        Assert.True(filters.NotAny(neither));
        Assert.True(filters.None(neither));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-FILTER", "typed-include-exclude-composition")]
    public void PublishedMessageFilter_AppliesTypedIncludesAndExcludesToTheSameMessage()
    {
        var filter = new PublishedMessageFilter();
        filter.Includes.Add<FilterMessage>(message => ((FilterMessage)message.MessageObject).Value > 0);
        filter.Excludes.Add<FilterMessage>(message => ((FilterMessage)message.MessageObject).Value == 2);

        IPublishedMessage included = Published(new FilterMessage(1));
        IPublishedMessage excluded = Published(new FilterMessage(2));
        IPublishedMessage otherType = Published(new OtherFilterMessage(1));

        Assert.True(filter.Any(included));
        Assert.False(filter.Any(excluded));
        Assert.False(filter.Any(otherType));
        Assert.False(filter.None(included));
        Assert.False(filter.None(excluded));
        Assert.True(filter.None(otherType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-FILTER", "null-delegates-rejected")]
    public void TypedFilterSets_RejectNullDelegatesAtConfigurationTime()
    {
        var published = new PublishedMessageFilterSet();
        var received = new ReceivedMessageFilterSet();
        var sent = new SentMessageFilterSet();

        ArgumentNullException publishedException = Assert.Throws<ArgumentNullException>(() =>
            published.Add<FilterMessage>(null!));
        ArgumentNullException receivedException = Assert.Throws<ArgumentNullException>(() =>
            received.Add<FilterMessage>(null!));
        ArgumentNullException sentException = Assert.Throws<ArgumentNullException>(() =>
            sent.Add<FilterMessage>(null!));

        Assert.Equal("filter", publishedException.ParamName);
        Assert.Equal("filter", receivedException.ParamName);
        Assert.Equal("filter", sentException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-FILTER", "received-and-sent-include-exclude-composition")]
    public void ReceivedAndSentMessageFilters_ApplyTheSameTypedIncludeExcludeContract()
    {
        var received = new ReceivedMessageFilter();
        received.Includes.Add<FilterMessage>(message => ((FilterMessage)message.MessageObject).Value > 0);
        received.Excludes.Add<FilterMessage>(message => ((FilterMessage)message.MessageObject).Value == 2);
        var sent = new SentMessageFilter();
        sent.Includes.Add<FilterMessage>(message => ((FilterMessage)message.MessageObject).Value > 0);
        sent.Excludes.Add<FilterMessage>(message => ((FilterMessage)message.MessageObject).Value == 2);

        Assert.True(received.Any(new StubReceivedMessage<FilterMessage>(new FilterMessage(1))));
        Assert.False(received.Any(new StubReceivedMessage<FilterMessage>(new FilterMessage(2))));
        Assert.False(received.Any(new StubReceivedMessage<OtherFilterMessage>(new OtherFilterMessage(1))));
        Assert.True(sent.Any(new StubSentMessage<FilterMessage>(new FilterMessage(1))));
        Assert.False(sent.Any(new StubSentMessage<FilterMessage>(new FilterMessage(2))));
        Assert.False(sent.Any(new StubSentMessage<OtherFilterMessage>(new OtherFilterMessage(1))));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-FILTER", "stable-read-only-filter-sets")]
    public void MessageFilters_ExposeStableReadOnlyConfigurationSets()
    {
        var published = new PublishedMessageFilter();
        var received = new ReceivedMessageFilter();
        var sent = new SentMessageFilter();

        Assert.Same(published.Includes, published.Includes);
        Assert.Same(published.Excludes, published.Excludes);
        Assert.Same(received.Includes, received.Includes);
        Assert.Same(received.Excludes, received.Excludes);
        Assert.Same(sent.Includes, sent.Includes);
        Assert.Same(sent.Excludes, sent.Excludes);
        Assert.Null(typeof(PublishedMessageFilter).GetProperty(nameof(PublishedMessageFilter.Includes))!.SetMethod);
        Assert.Null(typeof(PublishedMessageFilter).GetProperty(nameof(PublishedMessageFilter.Excludes))!.SetMethod);
        Assert.Null(typeof(ReceivedMessageFilter).GetProperty(nameof(ReceivedMessageFilter.Includes))!.SetMethod);
        Assert.Null(typeof(ReceivedMessageFilter).GetProperty(nameof(ReceivedMessageFilter.Excludes))!.SetMethod);
        Assert.Null(typeof(SentMessageFilter).GetProperty(nameof(SentMessageFilter.Includes))!.SetMethod);
        Assert.Null(typeof(SentMessageFilter).GetProperty(nameof(SentMessageFilter.Excludes))!.SetMethod);
    }

    private static IPublishedMessage Published<T>(T message)
        where T : class
        => new StubPublishedMessage<T>(message);

    private sealed record FilterSubject(int Value);

    private sealed record FilterMessage(int Value);

    private sealed record OtherFilterMessage(int Value);

    private sealed class StubPublishedMessage<T>(T message) : IPublishedMessage<T>
        where T : class
    {
        public Guid? ElementId => null;

        public PublishContext<T> Context => null!;

        SendContext IPublishedMessage.Context => Context;

        public DateTimeOffset StartTime => default;

        public TimeSpan ElapsedTime => TimeSpan.Zero;

        public Exception Exception => null!;

        public Type MessageType => typeof(T);

        public string ShortTypeName => typeof(T).Name;

        public object MessageObject => message;
    }

    private sealed class StubReceivedMessage<T>(T message) : IReceivedMessage<T>
        where T : class
    {
        public Guid? ElementId => null;

        public ConsumeContext<T> Context => null!;

        ConsumeContext IReceivedMessage.Context => Context.Advanced();

        public DateTimeOffset StartTime => default;

        public TimeSpan ElapsedTime => TimeSpan.Zero;

        public Exception Exception => null!;

        public Type MessageType => typeof(T);

        public string ShortTypeName => typeof(T).Name;

        public object MessageObject => message;
    }

    private sealed class StubSentMessage<T>(T message) : ISentMessage<T>
        where T : class
    {
        public Guid? ElementId => null;

        public SendContext<T> Context => null!;

        SendContext ISentMessage.Context => Context;

        public DateTimeOffset StartTime => default;

        public TimeSpan ElapsedTime => TimeSpan.Zero;

        public Exception Exception => null!;

        public Type MessageType => typeof(T);

        public string ShortTypeName => typeof(T).Name;

        public object MessageObject => message;
    }

    private sealed class SubjectFilterSet : FilterSet<FilterSubject>
    {
        public SubjectFilterSet AddSubject(FilterDelegate<FilterSubject> filter)
        {
            Add(filter);
            return this;
        }
    }
}
