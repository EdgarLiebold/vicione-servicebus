using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryDurableSendIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DURABLE-COMPLETION", "full-pipeline-success-only-process-local-capability")]
    public async Task Dispatch_WaitsForFullLogicalConsumerCompletionWithoutSerializingTheCapabilityAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(15);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ConsumerObservation();
        await using ServiceProvider provider = BuildProvider(observation, shouldFail: false);
        IBus bus = provider.GetRequiredService<IBus>();
        var store = new SignalingDurableSendStore(
            DurableSenderTestFactory.CreateInMemoryStore<IBus>());
        IDurableSendDispatcher<IBus> dispatcher = provider.GetRequiredService<IDurableSendDispatcher<IBus>>();
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-09-03T12:00:00+00:00"));
        using DurableSenderDeliveryTestDriver<IBus> driver = DurableSenderTestFactory.CreateDeliveryDriver(
            store,
            dispatcher,
            time,
            options => options.ConsumerCompletionTimeout = TimeSpan.FromMinutes(5));
        SerializedDurableSend message = Message();
        await store.AdmitAsync(
            message,
            new DurableSendStoreLimits(10, 100_000),
            time.GetUtcNow(),
            cancellationToken);

        await ((IBusControl)bus).StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Task<bool> batch = driver.DeliverDueBatchAsync(cancellationToken);
            ConsumerSnapshot snapshot = await observation.Entered.Task.WaitAsync(timeout, cancellationToken);
            Assert.True(await batch.WaitAsync(timeout, cancellationToken));

            DurableSendStoreSnapshot waiting = await store.GetSnapshotAsync(cancellationToken);
            Assert.Equal(1, waiting.StoredCount);
            Assert.Equal(1, waiting.AwaitingConsumerCompletionCount);
            Assert.False(observation.Release.Task.IsCompleted);
            Assert.Equal("accepted", snapshot.Value);
            Assert.Equal(message.Body.ToArray(), snapshot.RawBody);
            Assert.DoesNotContain(snapshot.Headers, header =>
                header.Key.Contains("durable", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(message.Id.ToString(), Encoding.UTF8.GetString(snapshot.RawBody), StringComparison.Ordinal);

            observation.Release.TrySetResult();
            Assert.True(await store.ConsumerCompletionApplied.Task.WaitAsync(timeout, cancellationToken));
            Assert.Equal(0, (await store.GetSnapshotAsync(cancellationToken)).StoredCount);
            Assert.Equal(0, (await store.GetSnapshotAsync(cancellationToken)).StoredBytes);
        }
        finally
        {
            observation.Release.TrySetResult();
            await ((IBusControl)bus).StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DURABLE-COMPLETION", "consumer-failure-never-retires-intent")]
    public async Task Dispatch_ConsumerFailureLeavesTheIntentAwaitingRecoveryAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(15);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ConsumerObservation();
        await using ServiceProvider provider = BuildProvider(observation, shouldFail: true);
        IBus bus = provider.GetRequiredService<IBus>();
        var store = new SignalingDurableSendStore(
            DurableSenderTestFactory.CreateInMemoryStore<IBus>());
        IDurableSendDispatcher<IBus> dispatcher = provider.GetRequiredService<IDurableSendDispatcher<IBus>>();
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-09-03T12:00:00+00:00"));
        using DurableSenderDeliveryTestDriver<IBus> driver = DurableSenderTestFactory.CreateDeliveryDriver(
            store,
            dispatcher,
            time);
        await store.AdmitAsync(
            Message(),
            new DurableSendStoreLimits(10, 100_000),
            time.GetUtcNow(),
            cancellationToken);

        await ((IBusControl)bus).StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Assert.True(await driver.DeliverDueBatchAsync(cancellationToken).WaitAsync(timeout, cancellationToken));
            await observation.Failed.Task.WaitAsync(timeout, cancellationToken);
            Assert.True(await store.AwaitingConsumerCompletionPersisted.Task.WaitAsync(timeout, cancellationToken));

            DurableSendStoreSnapshot waiting = await store.GetSnapshotAsync(cancellationToken);
            Assert.Equal(1, waiting.StoredCount);
            Assert.Equal(1, waiting.AwaitingConsumerCompletionCount);
            Assert.Empty((await store.GetQuarantineAsync(
                DurableSendQuarantineQuery.FirstPage(10),
                cancellationToken)).Entries);
        }
        finally
        {
            await ((IBusControl)bus).StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static ServiceProvider BuildProvider(ConsumerObservation observation, bool shouldFail)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(observation);
        services.AddViciOneMessageContracts(builder => builder.Register<DurablePayload>(
            ContractIdentity.Name,
            ContractIdentity.MajorVersion));
        services.AddViciOneServiceBus(configuration => configuration.UsingInMemory((_, bus) =>
        {
            bus.Host(new Uri("loopback://durable-host/"));
            bus.ReceiveEndpoint("durable-input", endpoint => endpoint.Handler<DurablePayload>(context =>
            {
                var snapshot = new ConsumerSnapshot(
                    context.Message.Value,
                    context.Advanced().ReceiveContext.Body.GetBytes(),
                    context.Headers.GetAll().ToArray());
                observation.Entered.TrySetResult(snapshot);
                if (shouldFail)
                {
                    observation.Failed.TrySetResult();
                    return Task.FromException(new ExpectedConsumerException());
                }

                context.Advanced().AddConsumeTask(observation.Release.Task);
                return Task.CompletedTask;
            }));
        }));
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private static SerializedDurableSend Message()
    {
        string urn = MessageUrn.ForTypeString<DurablePayload>();
        string body = $$"""
            {
              "message": {
                "value": "accepted"
              },
              "messageType": [
                "{{urn}}"
              ]
            }
            """;
        return new SerializedDurableSend
        {
            Id = new DurableSendId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            ContractIdentity = ContractIdentity,
            DestinationAddress = new Uri("loopback://durable-host/durable-input"),
            ContentType = SystemTextJsonMessageSerializer.JsonContentType.MediaType,
            Body = Encoding.UTF8.GetBytes(body),
            Metadata = Encoding.UTF8.GetBytes("process-local-metadata"),
        };
    }

    private static readonly MessageContractIdentity ContractIdentity =
        new("vicione.tests.inmemory-durable", 1);

    public sealed record DurablePayload(string Value);

    private sealed record ConsumerSnapshot(
        string Value,
        byte[] RawBody,
        KeyValuePair<string, object>[] Headers);

    private sealed class ConsumerObservation
    {
        public TaskCompletionSource<ConsumerSnapshot> Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Failed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class SignalingDurableSendStore(IDurableSendStore<IBus> inner) : IDurableSendStore<IBus>
    {
        public TaskCompletionSource<bool> AwaitingConsumerCompletionPersisted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ConsumerCompletionApplied { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<DurableSendAdmissionResult> AdmitAsync(
            SerializedDurableSend message,
            DurableSendStoreLimits limits,
            DateTimeOffset enqueuedAt,
            CancellationToken cancellationToken = default) =>
            inner.AdmitAsync(message, limits, enqueuedAt, cancellationToken);

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

        public async Task<bool> AwaitConsumerCompletionAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DateTimeOffset nextAttemptAt,
            CancellationToken cancellationToken = default)
        {
            bool persisted = await inner.AwaitConsumerCompletionAsync(
                    id,
                    lease,
                    deliveryAttempts,
                    nextAttemptAt,
                    cancellationToken)
                .ConfigureAwait(false);
            AwaitingConsumerCompletionPersisted.TrySetResult(persisted);
            return persisted;
        }

        public async Task<bool> CompleteConsumerDeliveryAsync(
            DurableSendId id,
            Guid generationToken,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken = default)
        {
            bool completed = await inner.CompleteConsumerDeliveryAsync(
                    id,
                    generationToken,
                    completedAt,
                    cancellationToken)
                .ConfigureAwait(false);
            ConsumerCompletionApplied.TrySetResult(completed);
            return completed;
        }

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

        public Task<DurableSendQuarantinePage> GetQuarantineAsync(
            DurableSendQuarantineQuery query,
            CancellationToken cancellationToken = default) =>
            inner.GetQuarantineAsync(query, cancellationToken);

        public Task<DurableSendOperationResult> RequeueAsync(
            DurableSendId id,
            DateTimeOffset dueAt,
            CancellationToken cancellationToken = default) =>
            inner.RequeueAsync(id, dueAt, cancellationToken);

        public Task<DurableSendOperationResult> DiscardQuarantinedAsync(
            DurableSendId id,
            CancellationToken cancellationToken = default) =>
            inner.DiscardQuarantinedAsync(id, cancellationToken);
    }

    private sealed class ExpectedConsumerException : Exception;
}
