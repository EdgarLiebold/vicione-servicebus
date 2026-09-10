using global::Azure;
using global::Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Testing;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests.Configuration;

public sealed class AzureServiceBusTestHarnessBoundaryTests
{
    private static readonly AzureNamedKeyCredential Credential = new("owner", "secret");

    [Fact]
    public void Constructor_RejectsEveryInvalidRequiredInput()
    {
        Assert.Equal("serviceUri", Assert.Throws<ArgumentNullException>(() =>
            new AzureServiceBusTestHarness(null!, Credential)).ParamName);
        Assert.Equal("namedKeyCredential", Assert.Throws<ArgumentNullException>(() =>
            new AzureServiceBusTestHarness(new Uri("sb://namespace.example"), null!)).ParamName);

        Uri[] invalidAddresses =
        [
            new("namespace.example", UriKind.Relative),
            new("https://namespace.example"),
            new("sb://namespace.example/path"),
            new("sb://user@namespace.example"),
            new("sb://namespace.example?query=value"),
            new("sb://namespace.example#fragment"),
        ];

        foreach (Uri invalidAddress in invalidAddresses)
        {
            Assert.Equal("serviceUri", Assert.Throws<ArgumentException>(() =>
                new AzureServiceBusTestHarness(invalidAddress, Credential)).ParamName);
        }

        Assert.Equal("inputQueueName", Assert.Throws<ArgumentException>(() =>
            new AzureServiceBusTestHarness(new Uri("sb://namespace.example"), Credential, " ")).ParamName);
    }

    [Fact]
    public void Constructor_ExposesStableDefaultsAndExplicitValues()
    {
        var defaultHarness = new AzureServiceBusTestHarness(new Uri("sb://namespace.example"), Credential);
        var namedHarness = new AzureServiceBusTestHarness(new Uri("SB://namespace.example"), Credential, "orders");

        Assert.Equal("input_queue", defaultHarness.InputQueueName);
        Assert.True(defaultHarness.UseMessageScheduler);
        Assert.Same(Credential, defaultHarness.NamedKeyCredential);
        Assert.Equal(new Uri("sb://namespace.example"), defaultHarness.HostAddress);
        Assert.Equal("orders", namedHarness.InputQueueName);
    }

    [Fact]
    public async Task CleanAsync_DeletesEveryEnumeratedEntityAndForwardsTheExactTokenAsync()
    {
        var administrationClient = new RecordingAdministrationClient(["orders", "billing"], ["events", "audit"]);
        var harness = new TestableHarness(administrationClient);
        using var cancellationSource = new CancellationTokenSource();

        await harness.CleanAsync(cancellationSource.Token);

        Assert.Equal(["orders", "billing"], administrationClient.DeletedQueues);
        Assert.Equal(["events", "audit"], administrationClient.DeletedTopics);
        Assert.All(administrationClient.ObservedTokens, token => Assert.Equal(cancellationSource.Token, token));
    }

    [Fact]
    public async Task CleanAsync_PropagatesUnexpectedAdministrationFailuresWithoutContinuingAsync()
    {
        var expected = new InvalidOperationException("administration unavailable");
        var administrationClient = new RecordingAdministrationClient(["orders", "billing"], [])
        {
            DeleteFailure = expected,
        };
        var harness = new TestableHarness(administrationClient);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.CleanAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Empty(administrationClient.DeletedQueues);
    }

    [Fact]
    public async Task CleanAsync_AcceptsAnEntityThatDisappearsBetweenEnumerationAndDeletionAsync()
    {
        var administrationClient = new RecordingAdministrationClient(["orders"], [])
        {
            DeleteFailure = new RequestFailedException(404, "The queue no longer exists."),
        };
        var harness = new TestableHarness(administrationClient);

        await harness.CleanAsync(TestContext.Current.CancellationToken);

        Assert.Empty(administrationClient.DeletedQueues);
    }

    private sealed class TestableHarness(ServiceBusAdministrationClient administrationClient)
        : AzureServiceBusTestHarness(new Uri("sb://namespace.example"), Credential)
    {
        protected override ServiceBusAdministrationClient CreateAdministrationClient() => administrationClient;
    }

    private sealed class RecordingAdministrationClient : ServiceBusAdministrationClient
    {
        private readonly IReadOnlyList<QueueProperties> _queues;
        private readonly StubResponse _response = new();
        private readonly IReadOnlyList<TopicProperties> _topics;

        public RecordingAdministrationClient(IEnumerable<string> queueNames, IEnumerable<string> topicNames)
        {
            _queues = queueNames.Select(CreateQueueProperties).ToArray();
            _topics = topicNames.Select(CreateTopicProperties).ToArray();
        }

        public Exception? DeleteFailure { get; init; }

        public List<string> DeletedQueues { get; } = [];

        public List<string> DeletedTopics { get; } = [];

        public List<CancellationToken> ObservedTokens { get; } = [];

        private static QueueProperties CreateQueueProperties(string name) =>
            ServiceBusModelFactory.QueueProperties(
                name,
                lockDuration: TimeSpan.FromMinutes(1),
                maxSizeInMegabytes: 1024,
                requiresDuplicateDetection: false,
                requiresSession: false,
                defaultMessageTimeToLive: TimeSpan.MaxValue,
                autoDeleteOnIdle: TimeSpan.MaxValue,
                deadLetteringOnMessageExpiration: false,
                duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(10),
                maxDeliveryCount: 10,
                enableBatchedOperations: true,
                status: EntityStatus.Active,
                forwardTo: string.Empty,
                forwardDeadLetteredMessagesTo: string.Empty,
                userMetadata: string.Empty,
                enablePartitioning: false);

        private static TopicProperties CreateTopicProperties(string name) =>
            ServiceBusModelFactory.TopicProperties(
                name,
                maxSizeInMegabytes: 1024,
                requiresDuplicateDetection: false,
                defaultMessageTimeToLive: TimeSpan.MaxValue,
                autoDeleteOnIdle: TimeSpan.MaxValue,
                duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(10),
                enableBatchedOperations: true,
                status: EntityStatus.Active,
                enablePartitioning: false);

        public override AsyncPageable<QueueProperties> GetQueuesAsync(CancellationToken cancellationToken = default)
        {
            ObservedTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return AsyncPageable<QueueProperties>.FromPages(
                [Page<QueueProperties>.FromValues(_queues, continuationToken: null, _response)]);
        }

        public override AsyncPageable<TopicProperties> GetTopicsAsync(CancellationToken cancellationToken = default)
        {
            ObservedTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return AsyncPageable<TopicProperties>.FromPages(
                [Page<TopicProperties>.FromValues(_topics, continuationToken: null, _response)]);
        }

        public override Task<global::Azure.Response> DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
        {
            ObservedTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (DeleteFailure != null)
                throw DeleteFailure;

            DeletedQueues.Add(queueName);
            return Task.FromResult<global::Azure.Response>(_response);
        }

        public override Task<global::Azure.Response> DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
        {
            ObservedTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (DeleteFailure != null)
                throw DeleteFailure;

            DeletedTopics.Add(topicName);
            return Task.FromResult<global::Azure.Response>(_response);
        }
    }

    private sealed class StubResponse : global::Azure.Response
    {
        public override int Status => 200;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = "azure-service-bus-test";

        public override void Dispose()
        {
        }

        protected override bool ContainsHeader(string name) => false;

        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];

        protected override bool TryGetHeader(string name, out string value)
        {
            value = null!;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = null!;
            return false;
        }
    }
}
