using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class MessageJournalConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-ACTIVATION", "connection-failure-rolls-back-prior-observers")]
    public void ConnectionFailure_RollsBackEveryEarlierObserverAndPreservesTheOriginalFailure()
    {
        var expectedFailure = new ExpectedConnectionException();
        var disconnectOrder = new List<string>();
        var connector = new FailingConnector(expectedFailure, disconnectOrder);

        Exception actualFailure = Assert.Throws<ExpectedConnectionException>(() =>
            connector.ConnectMessageJournal(
                new RecordingStore(),
                new ExcludingPolicy(),
                MessageJournalOptions.ContinueMessageFlow(TimeSpan.FromSeconds(1), TimeProvider.System)));

        Assert.Same(expectedFailure, actualFailure);
        Assert.Equal(["publish", "send"], disconnectOrder);
    }

    private sealed class FailingConnector(
        Exception connectionFailure,
        List<string> disconnectOrder) :
        ISendObserverConnector,
        IPublishObserverConnector,
        IConsumeObserverConnector
    {
        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            Assert.NotNull(observer);
            return new RecordingHandle("send", disconnectOrder, throwOnDisconnect: true);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        {
            Assert.NotNull(observer);
            return new RecordingHandle("publish", disconnectOrder, throwOnDisconnect: false);
        }

        public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
        {
            Assert.NotNull(observer);
            throw connectionFailure;
        }
    }

    private sealed class RecordingHandle(
        string name,
        List<string> disconnectOrder,
        bool throwOnDisconnect) : ConnectHandle
    {
        public void Disconnect()
        {
            disconnectOrder.Add(name);
            if (throwOnDisconnect)
                throw new ExpectedDisconnectException();
        }

        public void Dispose() => Disconnect();
    }

    private sealed class RecordingStore : IMessageJournalStore
    {
        public MessageJournalStoreLimits Limits { get; } =
            new(1024, maximumEntries: 1, retentionPeriod: TimeSpan.FromMinutes(1));

        public ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }

    private sealed class ExcludingPolicy : IMessageJournalPolicy
    {
        public ValueTask<MessageJournalProjection?> ProjectAsync(
            MessageJournalCapture capture,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<MessageJournalProjection?>(null);
    }

    private sealed class ExpectedConnectionException : Exception;
    private sealed class ExpectedDisconnectException : Exception;
}
