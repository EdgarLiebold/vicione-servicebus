using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using global::Azure;
using global::Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.AzureServiceBus.Testing;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests.Configuration;

public sealed class AzureServiceBusTestHarnessOptionsStartupTests
{
    [Fact]
    public void HarnessOptions_AreBoundToTheStartupValidator()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddAzureServiceBusTestHarness(options => options.CleanNamespaceOnStart = true)
            .BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
        Assert.True(provider.GetRequiredService<IOptions<AzureServiceBusTestHarnessOptions>>().Value.CleanNamespaceOnStart);
    }

    [Fact]
    public void Registration_RejectsEveryMissingRequiredInput()
    {
        Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
            AzureServiceBusDependencyInjectionTestingExtensions.AddAzureServiceBusTestHarness(null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddAzureServiceBusTestHarness(null!)).ParamName);
    }

    [Fact]
    public void Registration_RejectsBusFirstOrderingBeforeMutatingTheCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBus>(_ => null!);
        int originalCount = services.Count;

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            services.AddAzureServiceBusTestHarness(_ => { }));

        Assert.Contains("before AddViciOneServiceBus", exception.Message, StringComparison.Ordinal);
        Assert.Equal(originalCount, services.Count);
    }

    [Fact]
    public async Task HostedService_StopHonorsCancellationAsync()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddAzureServiceBusTestHarness(_ => { })
            .BuildServiceProvider();
        IHostedService hostedService = Assert.Single(provider.GetServices<IHostedService>());
        var cancellationToken = new CancellationToken(canceled: true);

        Task stopTask = hostedService.StopAsync(cancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stopTask);
        Assert.True(stopTask.IsCanceled);
    }

    [Fact]
    public async Task HostedService_StartWithoutCleanup_DoesNotCreateAnAdministrationClientAsync()
    {
        var clientCreationCount = 0;
        var service = new AzureServiceBusTestHarnessHostedService(
            new AzureServiceBusTransportOptions(),
            new AzureServiceBusTestHarnessOptions(),
            NullLogger<AzureServiceBusTestHarnessHostedService>.Instance,
            () =>
            {
                clientCreationCount++;
                return new RecordingAdministrationClient([], []);
            });

        await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, clientCreationCount);
    }

    [Fact]
    public async Task HostedService_DefaultFactoryUsesTheConfiguredConnectionStringAsync()
    {
        var service = new AzureServiceBusTestHarnessHostedService(
            Options.Create(new AzureServiceBusTransportOptions { ConnectionString = "not-a-connection-string" }),
            Options.Create(new AzureServiceBusTestHarnessOptions { CleanNamespaceOnStart = true }),
            NullLogger<AzureServiceBusTestHarnessHostedService>.Instance);

        await Assert.ThrowsAsync<FormatException>(() =>
            service.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HostedService_StartWithCleanup_DeletesEveryEntityAndUsesOneClientAsync()
    {
        var administrationClient = new RecordingAdministrationClient(["orders"], ["events"]);
        var clientCreationCount = 0;
        var service = new AzureServiceBusTestHarnessHostedService(
            new AzureServiceBusTransportOptions(),
            new AzureServiceBusTestHarnessOptions { CleanNamespaceOnStart = true },
            NullLogger<AzureServiceBusTestHarnessHostedService>.Instance,
            () =>
            {
                clientCreationCount++;
                return administrationClient;
            });

        await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, clientCreationCount);
        Assert.Equal(["orders"], administrationClient.DeletedQueues);
        Assert.Equal(["events"], administrationClient.DeletedTopics);
    }

    [Fact]
    public async Task HostedService_NullClientFactoryResult_FailsBeforeCleanupAsync()
    {
        var service = new AzureServiceBusTestHarnessHostedService(
            new AzureServiceBusTransportOptions(),
            new AzureServiceBusTestHarnessOptions { CleanNamespaceOnStart = true },
            NullLogger<AzureServiceBusTestHarnessHostedService>.Instance,
            () => null!);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("returned null", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HostedService_IsolatedConstructorRejectsEveryMissingDependency()
    {
        var transportOptions = new AzureServiceBusTransportOptions();
        var testOptions = new AzureServiceBusTestHarnessOptions();
        var logger = NullLogger<AzureServiceBusTestHarnessHostedService>.Instance;
        Func<ServiceBusAdministrationClient> factory = () => new RecordingAdministrationClient([], []);

        Assert.Equal("transportOptions", Assert.Throws<ArgumentNullException>(() =>
            new AzureServiceBusTestHarnessHostedService(null!, testOptions, logger, factory)).ParamName);
        Assert.Equal("testOptions", Assert.Throws<ArgumentNullException>(() =>
            new AzureServiceBusTestHarnessHostedService(transportOptions, null!, logger, factory)).ParamName);
        Assert.Equal("logger", Assert.Throws<ArgumentNullException>(() =>
            new AzureServiceBusTestHarnessHostedService(transportOptions, testOptions, null!, factory)).ParamName);
        Assert.Equal("createAdministrationClient", Assert.Throws<ArgumentNullException>(() =>
            new AzureServiceBusTestHarnessHostedService(transportOptions, testOptions, logger, null!)).ParamName);
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

        public List<string> DeletedQueues { get; } = [];

        public List<string> DeletedTopics { get; } = [];

        public override AsyncPageable<QueueProperties> GetQueuesAsync(CancellationToken cancellationToken = default) =>
            AsyncPageable<QueueProperties>.FromPages(
                [Page<QueueProperties>.FromValues(_queues, continuationToken: null, _response)]);

        public override AsyncPageable<TopicProperties> GetTopicsAsync(CancellationToken cancellationToken = default) =>
            AsyncPageable<TopicProperties>.FromPages(
                [Page<TopicProperties>.FromValues(_topics, continuationToken: null, _response)]);

        public override Task<global::Azure.Response> DeleteQueueAsync(
            string queueName,
            CancellationToken cancellationToken = default)
        {
            DeletedQueues.Add(queueName);
            return Task.FromResult<global::Azure.Response>(_response);
        }

        public override Task<global::Azure.Response> DeleteTopicAsync(
            string topicName,
            CancellationToken cancellationToken = default)
        {
            DeletedTopics.Add(topicName);
            return Task.FromResult<global::Azure.Response>(_response);
        }

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
    }

    private sealed class StubResponse : global::Azure.Response
    {
        public override int Status => 200;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = "azure-service-bus-hosted-service-test";

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
