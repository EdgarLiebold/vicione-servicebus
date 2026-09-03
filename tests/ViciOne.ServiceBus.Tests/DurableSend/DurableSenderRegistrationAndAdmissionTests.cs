using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.DurableSend;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class DurableSenderRegistrationAndAdmissionTests
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-ADMISSION-CATALOG", "registered-contract-and-configured-limits-reach-store")]
    public async Task Admission_RegisteredIdentityUsesTheConfiguredLimitsAndApplicationClock()
    {
        var time = new FakeTimeProvider(Epoch);
        var store = new ObservingStore(DurableSenderTestFactory.CreateInMemoryStore<ITestBus>());
        using ServiceProvider provider = Services(store, time, builder => builder.Register<KnownMessage>(
                KnownIdentity.Name,
                KnownIdentity.MajorVersion),
            options =>
            {
                options.MaximumStoredCount = 7;
                options.MaximumStoredBytes = 23;
            })
            .BuildServiceProvider();
        IDurableSender<ITestBus> sender = provider.GetRequiredService<IDurableSender<ITestBus>>();
        SerializedDurableSend message = Message(KnownIdentity);

        DurableSendAdmissionResult result = await sender.AdmitAsync(message, TestCancellationToken);

        Assert.Equal(DurableSendAdmissionDisposition.Accepted, result.Disposition);
        Assert.Equal(1, store.AdmitCalls);
        Assert.Equal(Epoch, store.LastEnqueuedAt);
        Assert.Equal(new DurableSendStoreLimits(7, 23), store.LastLimits);
        Assert.Equal(message.Id, store.LastMessage!.Id);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-ADMISSION-CATALOG", "unknown-contract-rejected-before-store")]
    public async Task Admission_UnknownStableIdentityIsRejectedBeforeAnyStoreMutation()
    {
        var store = new ObservingStore(DurableSenderTestFactory.CreateInMemoryStore<ITestBus>());
        using ServiceProvider provider = Services(
                store,
                new FakeTimeProvider(Epoch),
                builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion))
            .BuildServiceProvider();
        IDurableSender<ITestBus> sender = provider.GetRequiredService<IDurableSender<ITestBus>>();

        var unknownIdentity = new MessageContractIdentity("vicione.tests.unknown", 1);
        MessageContractException exception = await Assert.ThrowsAsync<MessageContractException>(() =>
            sender.AdmitAsync(Message(unknownIdentity), TestCancellationToken));

        Assert.Contains(unknownIdentity.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, store.AdmitCalls);
        Assert.Equal(0, (await store.GetSnapshotAsync(TestCancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-COMPOSITION", "one-eager-immutable-application-catalog")]
    public void MessageContracts_BuildOneImmutableCatalogAndRejectEverySecondOwner()
    {
        var services = new ServiceCollection();
        services.AddViciOneMessageContracts(builder => builder.Register<KnownMessage>(
            KnownIdentity.Name,
            KnownIdentity.MajorVersion));
        using ServiceProvider provider = services.BuildServiceProvider();
        IMessageContractCatalog first = provider.GetRequiredService<IMessageContractCatalog>();

        Assert.Equal(typeof(KnownMessage), first.GetMessageType(KnownIdentity));
        Assert.Null(first.GetType().GetMethod("Register"));
        Assert.Throws<ConfigurationException>(() => services.AddViciOneMessageContracts(
            builder => builder.Register<OtherMessage>("vicione.tests.other")));

        var preowned = new ServiceCollection();
        preowned.AddSingleton<IMessageContractCatalog>(new MessageContractCatalogBuilder()
            .Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion)
            .Build());
        Assert.Throws<ConfigurationException>(() => preowned.AddViciOneMessageContracts(_ => { }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DISPATCHER-COMPOSITION", "one-adapter-per-typed-bus")]
    public void InMemoryDispatcher_RejectsDuplicateOwnerButKeepsTypedBusesIndependent()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddViciOneInMemoryDurableSendDispatcher<ITestBus>());
        Assert.Same(services, services.AddViciOneInMemoryDurableSendDispatcher<IOtherBus>());
        Assert.Throws<ConfigurationException>(() => services.AddViciOneInMemoryDurableSendDispatcher<ITestBus>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IDurableSendDispatcher<ITestBus>));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IDurableSendDispatcher<IOtherBus>));

        var preowned = new ServiceCollection();
        preowned.AddSingleton<IDurableSendDispatcher<ITestBus>>(new NoOpDispatcher());
        Assert.Throws<ConfigurationException>(() => preowned.AddViciOneInMemoryDurableSendDispatcher<ITestBus>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SENDER-CONFIGURATION", "all-safety-bounds-fail-before-use")]
    public void Registration_InvalidRuntimePoliciesFailClosedWhenTheTypedSenderMaterializes()
    {
        Action<DurableSenderOptions<ITestBus>>[] invalidConfigurations =
        [
            options => options.MaximumStoredCount = 0,
            options => options.MaximumStoredBytes = 0,
            options => options.MaximumConcurrentDeliveries = 0,
            options => options.MaximumConcurrentDeliveries = DurableSendOperationLimits.AbsoluteMaximumClaimCount + 1,
            options => options.MaximumDeliveryAttempts = 0,
            options => options.InitialRetryDelay = TimeSpan.Zero,
            options =>
            {
                options.InitialRetryDelay = TimeSpan.FromMinutes(2);
                options.MaximumRetryDelay = TimeSpan.FromMinutes(1);
            },
            options => options.RetryJitterFraction = -0.01,
            options => options.RetryJitterFraction = 0.51,
            options => options.LeaseDuration = TimeSpan.Zero,
            options => options.ConsumerCompletionTimeout = TimeSpan.Zero,
            options => options.PollInterval = TimeSpan.Zero,
            options => options.TelemetrySnapshotInterval = TimeSpan.Zero,
            options => options.HealthDegradedAfter = TimeSpan.Zero,
        ];

        foreach (Action<DurableSenderOptions<ITestBus>> configure in invalidConfigurations)
        {
            using ServiceProvider provider = Services(
                    DurableSenderTestFactory.CreateInMemoryStore<ITestBus>(),
                    new FakeTimeProvider(Epoch),
                    builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion),
                    configure)
                .BuildServiceProvider();

            Assert.Throws<ConfigurationException>(() => provider.GetRequiredService<IDurableSender<ITestBus>>());
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-OPERATIONS", "bounded-requeue-and-discard-use-host-clock")]
    public async Task Operations_ValidatePagesAndUseTheInjectedClockForOperatorTransitions()
    {
        IDurableSendStore<ITestBus> store = DurableSenderTestFactory.CreateInMemoryStore<ITestBus>();
        var time = new FakeTimeProvider(Epoch.AddHours(3));
        using ServiceProvider provider = Services(
                store,
                time,
                builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion))
            .BuildServiceProvider();
        IDurableSenderOperations<ITestBus> operations = provider.GetRequiredService<IDurableSenderOperations<ITestBus>>();
        SerializedDurableSend message = Message(KnownIdentity);
        await store.AdmitAsync(message, new DurableSendStoreLimits(10, 100), Epoch, TestCancellationToken);
        DurableSendDelivery delivery = Assert.Single(await store.ClaimDueAsync(
            Epoch,
            1,
            TimeSpan.FromMinutes(1),
            TestCancellationToken));
        await store.QuarantineAsync(
            message.Id,
            delivery.Lease,
            1,
            DurableSendFailureKind.NonRetryable,
            null,
            Epoch,
            TestCancellationToken);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => operations.GetQuarantineAsync(
            DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize + 1,
            TestCancellationToken));
        Assert.True(await operations.RequeueAsync(message.Id, TestCancellationToken));
        Assert.Empty(await store.ClaimDueAsync(
            time.GetUtcNow().AddTicks(-1),
            1,
            TimeSpan.FromMinutes(1),
            TestCancellationToken));
        DurableSendDelivery requeued = Assert.Single(await store.ClaimDueAsync(
            time.GetUtcNow(),
            1,
            TimeSpan.FromMinutes(1),
            TestCancellationToken));
        await store.QuarantineAsync(
            message.Id,
            requeued.Lease,
            1,
            DurableSendFailureKind.NonRetryable,
            null,
            time.GetUtcNow(),
            TestCancellationToken);
        Assert.True(await operations.DiscardAsync(message.Id, TestCancellationToken));
        Assert.False(await operations.DiscardAsync(message.Id, TestCancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-HEALTH", "capacity-is-degraded-with-bounded-nonsensitive-data")]
    public async Task HealthCheck_ReportsCapacityWithoutMessagePayloadOrIdentity()
    {
        IDurableSendStore<ITestBus> store = DurableSenderTestFactory.CreateInMemoryStore<ITestBus>();
        var time = new FakeTimeProvider(Epoch);
        IServiceCollection services = Services(
            store,
            time,
            builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion),
            options =>
            {
                options.MaximumStoredCount = 1;
                options.MaximumStoredBytes = 100;
            });
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddHealthChecks().AddViciOneDurableSenderHealthCheck<ITestBus>();
        using ServiceProvider provider = services.BuildServiceProvider();
        HealthCheckService health = provider.GetRequiredService<HealthCheckService>();

        HealthReport healthy = await health.CheckHealthAsync(TestCancellationToken);
        Assert.Equal(HealthStatus.Healthy, Assert.Single(healthy.Entries).Value.Status);

        await store.AdmitAsync(Message(KnownIdentity), new DurableSendStoreLimits(1, 100), Epoch, TestCancellationToken);
        HealthReport degraded = await health.CheckHealthAsync(TestCancellationToken);
        KeyValuePair<string, HealthReportEntry> entry = Assert.Single(degraded.Entries);
        Assert.Equal(HealthStatus.Degraded, entry.Value.Status);
        Assert.Equal(1, entry.Value.Data["storedCount"]);
        Assert.DoesNotContain("payload", string.Join('|', entry.Value.Data.Keys), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("message", string.Join('|', entry.Value.Data.Keys), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("destination", string.Join('|', entry.Value.Data.Keys), StringComparison.OrdinalIgnoreCase);
    }

    private static IServiceCollection Services(
        IDurableSendStore<ITestBus> store,
        TimeProvider timeProvider,
        Action<MessageContractCatalogBuilder> catalog,
        Action<DurableSenderOptions<ITestBus>>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(timeProvider);
        services.AddViciOneMessageContracts(catalog);
        services.AddViciOneDurableSender(configure);
        return services;
    }

    private static SerializedDurableSend Message(MessageContractIdentity identity) => new()
    {
        Id = new DurableSendId(Guid.Parse("99999999-2222-3333-4444-555555555555")),
        ContractIdentity = identity,
        DestinationAddress = new Uri("loopback://durable-admission"),
        ContentType = "application/octet-stream",
        Body = new byte[] { 1, 2, 3 },
    };

    private static readonly MessageContractIdentity KnownIdentity =
        new("vicione.tests.durable-admission", 2);

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private interface ITestBus : IBus;
    private interface IOtherBus : IBus;
    private sealed record KnownMessage;
    private sealed record OtherMessage;

    private sealed class NoOpDispatcher : IDurableSendDispatcher<ITestBus>
    {
        public Task<DurableSendDispatchResult> DispatchAsync(
            DurableSendDispatchContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(DurableSendDispatchResult.AwaitConsumerCompletion);
        }
    }

    private sealed class ObservingStore(IDurableSendStore<ITestBus> inner) : IDurableSendStore<ITestBus>
    {
        public int AdmitCalls { get; private set; }
        public SerializedDurableSend? LastMessage { get; private set; }
        public DurableSendStoreLimits LastLimits { get; private set; }
        public DateTimeOffset LastEnqueuedAt { get; private set; }

        public Task<DurableSendAdmissionResult> AdmitAsync(
            SerializedDurableSend message,
            DurableSendStoreLimits limits,
            DateTimeOffset enqueuedAt,
            CancellationToken cancellationToken = default)
        {
            AdmitCalls++;
            LastMessage = message;
            LastLimits = limits;
            LastEnqueuedAt = enqueuedAt;
            return inner.AdmitAsync(message, limits, enqueuedAt, cancellationToken);
        }

        public Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(
            DateTimeOffset now,
            int maximumCount,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default) =>
            inner.ClaimDueAsync(now, maximumCount, leaseDuration, cancellationToken);

        public Task<bool> MarkDeliveredAsync(
            DurableSendId id,
            DurableSendLease lease,
            DateTimeOffset deliveredAt,
            CancellationToken cancellationToken = default) =>
            inner.MarkDeliveredAsync(id, lease, deliveredAt, cancellationToken);

        public Task<bool> AwaitConsumerCompletionAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DateTimeOffset nextAttemptAt,
            CancellationToken cancellationToken = default) =>
            inner.AwaitConsumerCompletionAsync(id, lease, deliveryAttempts, nextAttemptAt, cancellationToken);

        public Task<bool> CompleteConsumerDeliveryAsync(
            DurableSendId id,
            Guid generationToken,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken = default) =>
            inner.CompleteConsumerDeliveryAsync(id, generationToken, completedAt, cancellationToken);

        public Task<bool> ScheduleRetryAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DateTimeOffset nextAttemptAt,
            DurableSendFailureKind failureKind,
            string? failureType,
            DateTimeOffset failedAt,
            CancellationToken cancellationToken = default) =>
            inner.ScheduleRetryAsync(
                id,
                lease,
                deliveryAttempts,
                nextAttemptAt,
                failureKind,
                failureType,
                failedAt,
                cancellationToken);

        public Task<bool> QuarantineAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DurableSendFailureKind failureKind,
            string? failureType,
            DateTimeOffset quarantinedAt,
            CancellationToken cancellationToken = default) =>
            inner.QuarantineAsync(
                id,
                lease,
                deliveryAttempts,
                failureKind,
                failureType,
                quarantinedAt,
                cancellationToken);

        public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            inner.GetSnapshotAsync(cancellationToken);

        public Task<IReadOnlyList<DurableSendQuarantineEntry>> GetQuarantineAsync(
            int maximumCount,
            CancellationToken cancellationToken = default) =>
            inner.GetQuarantineAsync(maximumCount, cancellationToken);

        public Task<bool> RequeueAsync(
            DurableSendId id,
            DateTimeOffset dueAt,
            CancellationToken cancellationToken = default) =>
            inner.RequeueAsync(id, dueAt, cancellationToken);

        public Task<bool> DiscardQuarantinedAsync(
            DurableSendId id,
            CancellationToken cancellationToken = default) =>
            inner.DiscardQuarantinedAsync(id, cancellationToken);
    }
}
