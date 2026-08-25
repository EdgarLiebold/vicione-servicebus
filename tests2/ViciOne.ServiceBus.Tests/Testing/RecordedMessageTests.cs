using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class RecordedMessageTests
{
    private static readonly DateTimeOffset ObservationTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-RECORDED-MESSAGE", "send-context-metadata-and-clock")]
    public void SentMessage_PreservesContextPayloadFailureIdentityAndConfiguredTime()
    {
        var source = new ObservedMessage("sent");
        var context = new MessageSendContext<ObservedMessage>(source);
        var expected = new InvalidOperationException("send failed");
        var timeProvider = new FakeTimeProvider(ObservationTime);

        var recorded = new SentMessage<ObservedMessage>(context, expected, timeProvider);
        ISentMessage<ObservedMessage> typed = recorded;
        ISentMessage untyped = recorded;

        Assert.Same(context, typed.Context);
        Assert.Same(context, untyped.Context);
        Assert.Same(source, untyped.MessageObject);
        Assert.Same(expected, untyped.Exception);
        Assert.Equal(context.MessageId, recorded.ElementId);
        Assert.Equal(typeof(ObservedMessage), untyped.MessageType);
        Assert.Equal(TypeCache<ObservedMessage>.ShortName, recorded.ShortTypeName);
        Assert.Equal(context.SentTime, recorded.StartTime);
        Assert.Equal(ObservationTime.UtcDateTime - recorded.StartTime, recorded.ElapsedTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECORDED-MESSAGE", "publish-context-metadata-and-clock")]
    public void PublishedMessage_PreservesContextPayloadFailureIdentityAndConfiguredTime()
    {
        var source = new ObservedMessage("published");
        var context = new TestPublishContext<ObservedMessage>(source);
        var expected = new InvalidOperationException("publish failed");
        var timeProvider = new FakeTimeProvider(ObservationTime);

        var recorded = new PublishedMessage<ObservedMessage>(context, expected, timeProvider);
        IPublishedMessage<ObservedMessage> typed = recorded;
        IPublishedMessage untyped = recorded;

        Assert.Same(context, typed.Context);
        Assert.Same(context, untyped.Context);
        Assert.Same(source, untyped.MessageObject);
        Assert.Same(expected, recorded.Exception);
        Assert.Equal(context.MessageId, recorded.ElementId);
        Assert.Equal(typeof(ObservedMessage), recorded.MessageType);
        Assert.Equal(TypeCache<ObservedMessage>.ShortName, recorded.ShortTypeName);
        Assert.Equal(context.SentTime, recorded.StartTime);
        Assert.Equal(ObservationTime.UtcDateTime - recorded.StartTime, recorded.ElapsedTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECORDED-MESSAGE", "missing-dependencies-rejected")]
    public void Construction_RejectsMissingContextAndTimeProviderForEveryRecordedMessageKind()
    {
        var sendContext = new MessageSendContext<ObservedMessage>(new ObservedMessage("sent"));
        var publishContext = new TestPublishContext<ObservedMessage>(new ObservedMessage("published"));

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new SentMessage<ObservedMessage>(null!, null, TimeProvider.System)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new SentMessage<ObservedMessage>(sendContext, null, null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new PublishedMessage<ObservedMessage>(null!, null, TimeProvider.System)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
            new PublishedMessage<ObservedMessage>(publishContext, null, null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new ReceivedMessage<ObservedMessage>(null!, null, TimeProvider.System)).ParamName);
    }

    private sealed record ObservedMessage(string Value);

    private sealed class TestPublishContext<T>(T message) : MessageSendContext<T>(message), PublishContext<T>
        where T : class;
}
