using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class ReliableMessagingRegistrationAndAdmissionTests
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-TYPED-API", "one-builder-route-serializer-offline-idempotency-and-cancellation")]
    public async Task TypedSender_UsesTheCanonicalRouteAndSerializerBeforeOfflineDurableAdmissionAsync()
    {
        var destination = new Uri("loopback://typed-durable/orders");
        var correlationId = Guid.Parse("77777777-2222-3333-4444-555555555555");
        var durableId = new DurableSendId(Guid.Parse("88888888-2222-3333-4444-555555555555"));
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://typed-durable/"));
                bus.Route<TypedMessage>(destination);
            });
            configuration.UseReliableMessaging(durable =>
            {
                durable.UseInMemoryStore();
                durable.AddMessageContract<TypedMessage>("vicione.tests.typed-durable", 3);
                durable.Store(new ReliableStoreLimits
                {
                    MaximumStoredCount = 4,
                    MaximumStoredBytes = 64 * 1024,
                });
                durable.Delivery(_ => { });
                durable.Retention(TimeSpan.FromDays(1));
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IDurableSender<IBus> sender = scope.ServiceProvider.GetRequiredService<IDurableSender<IBus>>();
        IOutboxStore<IBus> store = scope.ServiceProvider.GetRequiredService<IOutboxStore<IBus>>();
        var options = new DurableSendOptions { IdempotencyKey = durableId, CorrelationId = correlationId };
        var message = new TypedMessage("accepted while the broker is offline");

        DurableSendReceipt first = await sender.SendAsync(destination, message, options, TestCancellationToken);
        DurableSendReceipt duplicateThroughRoute = await sender.SendAsync(message, options, TestCancellationToken);

        Assert.True(first.IsNew);
        Assert.Equal(DurableSendAdmissionDisposition.AlreadyAccepted, duplicateThroughRoute.Disposition);
        Assert.Equal((durableId, 1), (duplicateThroughRoute.Id, duplicateThroughRoute.StoredCount));
        DurableSendDelivery retained = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddYears(1),
            1,
            TimeSpan.FromMinutes(1),
            TestCancellationToken));
        Assert.Equal(new MessageContractIdentity("vicione.tests.typed-durable", 3), retained.Message.ContractIdentity);
        Assert.Equal(destination, retained.Message.DestinationAddress);
        Assert.Equal(durableId.Value, retained.Message.MessageId);
        Assert.Equal(correlationId, retained.Message.CorrelationId);
        Assert.Equal("application/vnd.vicione.servicebus+json", retained.Message.ContentType);
        Assert.Contains(message.Value, Encoding.UTF8.GetString(retained.Message.Body.Span), StringComparison.Ordinal);

        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            sender.SendAsync(new TypedMessage("different intent"), options, TestCancellationToken));
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendAsync(
            new TypedMessage("must not persist"),
            new DurableSendOptions { IdempotencyKey = new DurableSendId(Guid.NewGuid()) },
            canceled.Token));
        Assert.Equal(1, (await store.GetSnapshotAsync(TestCancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-TYPED-API", "multibus-facades-routes-stores-and-identities-are-isolated")]
    public async Task TypedSender_MultiBusKeepsFacadeRouteStoreAndPersistenceIdentityIsolatedAsync()
    {
        var primaryDestination = new Uri("loopback://typed-primary/messages");
        var secondaryDestination = new Uri("loopback://typed-secondary/messages");
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://typed-primary/"));
                bus.Route<TypedMessage>(primaryDestination);
            });
            configuration.UseReliableMessaging(durable =>
            {
                durable.UseInMemoryStore();
                ConfigureReliablePolicy(durable);
                durable.AddMessageContract<TypedMessage>("vicione.tests.typed-durable", 3);
            });
        });
        services.AddViciOneServiceBus<IOtherBus>("secondary-v1", configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://typed-secondary/"));
                bus.Route<TypedMessage>(secondaryDestination);
            });
            configuration.UseReliableMessaging(durable =>
            {
                durable.UseInMemoryStore();
                ConfigureReliablePolicy(durable);
                durable.AddMessageContract<TypedMessage>("vicione.tests.typed-durable", 3);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        DurableSendReceipt primary = await scope.ServiceProvider.GetRequiredService<IDurableSender<IBus>>().SendAsync(
            new TypedMessage("primary"),
            new DurableSendOptions { IdempotencyKey = new DurableSendId(GuidFrom(11)) },
            TestCancellationToken);
        DurableSendReceipt secondary = await scope.ServiceProvider.GetRequiredService<IDurableSender<IOtherBus>>().SendAsync(
            new TypedMessage("secondary"),
            new DurableSendOptions { IdempotencyKey = new DurableSendId(GuidFrom(12)) },
            TestCancellationToken);

        IOutboxStore<IBus> primaryStore = scope.ServiceProvider.GetRequiredService<IOutboxStore<IBus>>();
        IOutboxStore<IOtherBus> secondaryStore = scope.ServiceProvider.GetRequiredService<IOutboxStore<IOtherBus>>();
        DurableSendDelivery primaryIntent = Assert.Single(await primaryStore.ClaimDueAsync(
            Epoch.AddYears(1), 1, TimeSpan.FromMinutes(1), TestCancellationToken));
        DurableSendDelivery secondaryIntent = Assert.Single(await secondaryStore.ClaimDueAsync(
            Epoch.AddYears(1), 1, TimeSpan.FromMinutes(1), TestCancellationToken));
        Assert.True(primary.IsNew);
        Assert.True(secondary.IsNew);
        Assert.Equal(primaryDestination, primaryIntent.Message.DestinationAddress);
        Assert.Equal(secondaryDestination, secondaryIntent.Message.DestinationAddress);
        Assert.Contains("primary", Encoding.UTF8.GetString(primaryIntent.Message.Body.Span), StringComparison.Ordinal);
        Assert.Contains("secondary", Encoding.UTF8.GetString(secondaryIntent.Message.Body.Span), StringComparison.Ordinal);
        Assert.Equal("default", scope.ServiceProvider.GetRequiredService<BusPersistenceIdentity<IBus>>().Require("test"));
        Assert.Equal("secondary-v1",
            scope.ServiceProvider.GetRequiredService<BusPersistenceIdentity<IOtherBus>>().Require("test"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULE", "public-scheduler-persists-due-at-and-cancels-through-one-store")]
    public async Task MessageScheduler_PersistsDueAtAndCancelsThroughTheReliableStoreAsync()
    {
        var destination = new Uri("loopback://reliable-scheduler/messages");
        var clock = new FakeTimeProvider(Epoch);
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://reliable-scheduler/"));
                bus.Route<TypedMessage>(destination);
            });
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                ConfigureReliablePolicy(reliable);
                reliable.AddMessageContract<TypedMessage>("vicione.tests.reliable-scheduler");
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IMessageScheduler scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
        IOutboxStore<IBus> store = scope.ServiceProvider.GetRequiredService<IOutboxStore<IBus>>();
        DateTimeOffset dueAt = Epoch.AddMinutes(30);
        TimeSpan lifetime = TimeSpan.FromMinutes(17);
        Guid messageId = GuidFrom(51);
        Guid correlationId = GuidFrom(52);
        Guid conversationId = GuidFrom(53);
        Guid requestId = GuidFrom(54);
        var scheduleOptions = new ScheduleOptions
        {
            Headers = new Dictionary<string, object?> { ["tenant"] = "north" },
            TimeToLive = lifetime,
            MessageId = messageId,
            CorrelationId = correlationId,
            ConversationId = conversationId,
            RequestId = requestId,
        };

        ScheduledMessage<TypedMessage> scheduled = await scheduler.ScheduleSendAsync(
            destination,
            dueAt,
            new TypedMessage("due"),
            scheduleOptions,
            TestCancellationToken);
        ScheduledMessage<TypedMessage> cancelled = await scheduler.ScheduleSendAsync(
            destination,
            dueAt.AddMinutes(1),
            new TypedMessage("cancelled"),
            TestCancellationToken);

        Assert.Equal(dueAt, scheduled.DueAt);
        Assert.Equal(destination, scheduled.Destination);
        Assert.Empty(await store.ClaimDueAsync(
            dueAt.AddTicks(-1),
            10,
            TimeSpan.FromMinutes(1),
            TestCancellationToken));
        await scheduler.CancelScheduledSendAsync(cancelled, TestCancellationToken);
        DurableSendDelivery delivery = Assert.Single(await store.ClaimDueAsync(
            dueAt,
            10,
            TimeSpan.FromMinutes(1),
            TestCancellationToken));
        Assert.Equal(scheduled.TokenId, delivery.Message.Id.Value);
        Assert.Equal(dueAt, delivery.Message.DueAt);
        Assert.Equal(messageId, delivery.Message.MessageId);
        Assert.Equal(correlationId, delivery.Message.CorrelationId);
        Assert.False(delivery.Message.Metadata.IsEmpty);
        var replay = new InMemorySendContext<SerializedTransportMessage>(SerializedTransportMessage.Instance);
        ReliableEnvelopeMetadataCodec.Apply(replay, delivery.Message.Metadata, dueAt);
        Assert.Equal("north", replay.Headers.Get<string>("tenant"));
        Assert.Equal(lifetime, replay.TimeToLive);
        Assert.Equal(conversationId, replay.ConversationId);
        Assert.Equal(requestId, replay.RequestId);
        Assert.Equal(1, (await store.GetSnapshotAsync(TestCancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULE", "default-and-typed-bus-schedulers-have-isolated-owners")]
    public async Task MessageScheduler_MultiBusRegistrationsRemainIsolatedAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://scheduler-primary/")));
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                ConfigureReliablePolicy(reliable);
                reliable.AddMessageContract<TypedMessage>("vicione.tests.scheduler-isolation");
            });
        });
        services.AddViciOneServiceBus<IOtherBus>("scheduler-secondary", configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://scheduler-secondary/")));
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                ConfigureReliablePolicy(reliable);
                reliable.AddMessageContract<TypedMessage>("vicione.tests.scheduler-isolation");
            });
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IMessageScheduler primary = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
        IMessageScheduler secondary = scope.ServiceProvider
            .GetRequiredService<Bind<IOtherBus, IMessageScheduler>>()
            .Value;
        Assert.NotSame(primary, secondary);
        Assert.Equal("ReliableMessageScheduler`1", primary.GetType().Name);
        Assert.Equal("ReliableMessageScheduler`1", secondary.GetType().Name);
        Assert.Equal(typeof(IBus), primary.GetType().GenericTypeArguments.Single());
        Assert.Equal(typeof(IOtherBus), secondary.GetType().GenericTypeArguments.Single());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULER-API", "adapter-selection-is-explicit-and-single-owner")]
    public void MessageScheduler_RejectsASecondExplicitAdapter()
    {
        var services = new ServiceCollection();
        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            services.AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://scheduler-duplicate/")));
                configuration.UseReliableMessaging(reliable =>
                {
                    reliable.UseInMemoryStore();
                    ConfigureReliablePolicy(reliable);
                    reliable.UseTransportScheduler();
                    reliable.UseTransportScheduler();
                });
            }));

        Assert.Contains("scheduler adapter was configured more than once", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("exactly once", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULER-API", "transport-adapter-replaces-store-scheduler-without-fallback")]
    public async Task MessageScheduler_TransportAdapterIsTheResolvedSchedulerAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://scheduler-transport/")));
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                ConfigureReliablePolicy(reliable);
                reliable.AddMessageContract<TypedMessage>("vicione.tests.scheduler-transport");
                reliable.UseTransportScheduler();
            });
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IMessageScheduler scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();

        Assert.Equal("MessageScheduler", scheduler.GetType().Name);
        Assert.DoesNotContain("ReliableMessageScheduler", scheduler.GetType().FullName, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-TYPED-API", "message-data-offload-and-payload-admission-share-normal-send-path")]
    public async Task TypedSender_UsesMessageDataOffloadEvidenceForPayloadAdmissionAsync()
    {
        const string contractName = "vicione.tests.typed-message-data";
        string largeValue = new('z', 2048);
        var repository = new InMemoryMessageDataRepository();
        var destination = new Uri("loopback://typed-message-data/input");
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(new MessageLimits
            {
                MaxBodyBytes = 64 * 1024,
                MaxEnvelopeBytes = 64 * 1024,
                MaxJsonDepth = 32,
                OffloadToMessageDataAboveBytes = 1,
            });
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://typed-message-data/"));
                bus.UseMessageData(repository, new MessageDataPolicy(alwaysWriteToRepository: false, threshold: 16));
                bus.Route<DurableDataMessage>(destination);
            });
            configuration.UseReliableMessaging(durable =>
            {
                durable.UseInMemoryStore();
                ConfigureReliablePolicy(durable);
                durable.AddMessageContract<DurableDataMessage>(contractName);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        DurableSendReceipt receipt = await scope.ServiceProvider.GetRequiredService<IDurableSender<IBus>>().SendAsync(
            new DurableDataMessage { Value = new PutMessageData<string>(largeValue) },
            new DurableSendOptions { IdempotencyKey = new DurableSendId(GuidFrom(21)) },
            TestCancellationToken);

        Assert.True(receipt.IsNew);
        IOutboxStore<IBus> store = scope.ServiceProvider.GetRequiredService<IOutboxStore<IBus>>();
        DurableSendDelivery retained = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddYears(1), 1, TimeSpan.FromMinutes(1), TestCancellationToken));
        string envelope = Encoding.UTF8.GetString(retained.Message.Body.Span);
        Assert.Equal(new MessageContractIdentity(contractName, 1), retained.Message.ContractIdentity);
        Assert.DoesNotContain(largeValue, envelope, StringComparison.Ordinal);
        Assert.Contains("address", envelope, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-TYPED-API", "configured-payload-limit-rejects-before-persistence")]
    public async Task TypedSender_EnforcesPayloadAdmissionBeforePersistentMutationAsync()
    {
        var destination = new Uri("loopback://typed-admission/input");
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(new MessageLimits
            {
                MaxBodyBytes = 128,
                MaxEnvelopeBytes = 1024,
                MaxJsonDepth = 32,
            });
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://typed-admission/"));
                bus.Route<TypedMessage>(destination);
            });
            configuration.UseReliableMessaging(durable =>
            {
                durable.UseInMemoryStore();
                ConfigureReliablePolicy(durable);
                durable.AddMessageContract<TypedMessage>("vicione.tests.typed-admission");
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IDurableSender<IBus> sender = scope.ServiceProvider.GetRequiredService<IDurableSender<IBus>>();
        IOutboxStore<IBus> store = scope.ServiceProvider.GetRequiredService<IOutboxStore<IBus>>();

        PayloadAdmissionException failure = await Assert.ThrowsAsync<PayloadAdmissionException>(() => sender.SendAsync(
            new TypedMessage(new string('x', 4096)),
            new DurableSendOptions { IdempotencyKey = new DurableSendId(GuidFrom(22)) },
            TestCancellationToken));

        Assert.Equal(PayloadAdmissionStage.SerializedBody, failure.Stage);
        Assert.Equal(128, failure.ConfiguredLimitBytes);
        Assert.True(failure.ActualBytes > failure.ConfiguredLimitBytes);
        Assert.Equal(0, (await store.GetSnapshotAsync(TestCancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-ADMISSION-CATALOG", "registered-contract-and-configured-limits-reach-store")]
    public async Task Admission_RegisteredIdentityUsesTheConfiguredLimitsAndApplicationClockAsync()
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
        IDurableSendAdmission<ITestBus> sender = provider.GetRequiredService<IDurableSendAdmission<ITestBus>>();
        SerializedDurableSend message = Message(KnownIdentity);

        DurableSendAdmissionResult result = await sender.AdmitAsync(message, TestCancellationToken);

        Assert.Equal(DurableSendAdmissionDisposition.Accepted, result.Disposition);
        Assert.Equal(1, store.AdmitCalls);
        Assert.Equal(Epoch, store.LastEnqueuedAt);
        Assert.Equal(new DurableSendStoreLimits(7, 23), store.LastLimits);
        Assert.Equal(message.Id, store.LastMessage!.Id);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-PROVIDER-CONTRACT", "invalid-admission-results-fail-closed")]
    public async Task Admission_InvalidProviderResultsFailClosedAsync()
    {
        SerializedDurableSend message = Message(KnownIdentity);
        DurableSendAdmissionResult[] invalidResults =
        [
            default,
            new(new DurableSendId(GuidFrom(91)), DurableSendAdmissionDisposition.Accepted, 1, message.StorageSize),
            new(message.Id, DurableSendAdmissionDisposition.Accepted, 0, message.StorageSize),
            new(message.Id, DurableSendAdmissionDisposition.Accepted, 1, message.StorageSize - 1),
        ];

        foreach (DurableSendAdmissionResult invalidResult in invalidResults)
        {
            IOutboxStore<ITestBus> inner = DurableSenderTestFactory.CreateInMemoryStore<ITestBus>();
            var store = new ObservingStore(inner)
            {
                AdmissionResultOverride = invalidResult,
            };
            using ServiceProvider provider = Services(
                    store,
                    new FakeTimeProvider(Epoch),
                    builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion),
                    options =>
                    {
                        options.MaximumStoredCount = 7;
                        options.MaximumStoredBytes = 23;
                    })
                .BuildServiceProvider();
            IDurableSendAdmission<ITestBus> admission =
                provider.GetRequiredService<IDurableSendAdmission<ITestBus>>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                admission.AdmitAsync(message, TestCancellationToken));

            Assert.Equal(1, store.AdmitCalls);
            Assert.Equal(0, (await inner.GetSnapshotAsync(TestCancellationToken)).StoredCount);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-ADMISSION-CATALOG", "unknown-contract-rejected-before-store")]
    public async Task Admission_UnknownStableIdentityIsRejectedBeforeAnyStoreMutationAsync()
    {
        var store = new ObservingStore(DurableSenderTestFactory.CreateInMemoryStore<ITestBus>());
        using ServiceProvider provider = Services(
                store,
                new FakeTimeProvider(Epoch),
                builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion),
                options =>
                {
                    options.MaximumStoredCount = 7;
                    options.MaximumStoredBytes = 23;
                })
            .BuildServiceProvider();
        IDurableSendAdmission<ITestBus> sender = provider.GetRequiredService<IDurableSendAdmission<ITestBus>>();

        var unknownIdentity = new MessageContractIdentity("vicione.tests.unknown", 1);
        MessageContractException exception = await Assert.ThrowsAsync<MessageContractException>(() =>
            sender.AdmitAsync(Message(unknownIdentity), TestCancellationToken));

        Assert.Contains(unknownIdentity.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, store.AdmitCalls);
        Assert.Equal(0, (await store.GetSnapshotAsync(TestCancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-COMPOSITION", "additive-declarations-build-one-immutable-application-catalog")]
    public void MessageContracts_ComposeAdditiveDeclarationsIntoOneImmutableCatalog()
    {
        var services = new ServiceCollection();
        services.AddViciOneMessageContracts(builder => builder.Register<KnownMessage>(
            KnownIdentity.Name,
            KnownIdentity.MajorVersion));
        services.AddViciOneMessageContracts(builder => builder
            .Register<OtherMessage>("vicione.tests.other")
            .Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion));
        using ServiceProvider provider = services.BuildServiceProvider();
        IMessageContractCatalog first = provider.GetRequiredService<IMessageContractCatalog>();

        Assert.Equal(typeof(KnownMessage), first.GetMessageType(KnownIdentity));
        Assert.Equal(typeof(OtherMessage), first.GetMessageType(new MessageContractIdentity("vicione.tests.other", 1)));
        Assert.Null(first.GetType().GetMethod("Register"));

        var conflict = new ServiceCollection();
        conflict.AddViciOneMessageContracts(builder => builder.Register<KnownMessage>(KnownIdentity.Name));
        conflict.AddViciOneMessageContracts(builder => builder.Register<OtherMessage>(KnownIdentity.Name));
        using ServiceProvider conflictProvider = conflict.BuildServiceProvider();
        Assert.Throws<ConfigurationException>(() =>
            conflictProvider.GetRequiredService<IMessageContractCatalog>());

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
        Action<ReliableMessagingOptions<ITestBus>>[] invalidConfigurations =
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
            options => options.Retention = TimeSpan.FromTicks(-1),
        ];

        foreach (Action<ReliableMessagingOptions<ITestBus>> configure in invalidConfigurations)
        {
            using ServiceProvider provider = Services(
                    DurableSenderTestFactory.CreateInMemoryStore<ITestBus>(),
                    new FakeTimeProvider(Epoch),
                    builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion),
                    configure)
                .BuildServiceProvider();

            Assert.Throws<OptionsValidationException>(() =>
                provider.GetRequiredService<IDurableSendAdmission<ITestBus>>());
        }
    }

    [Theory]
    [InlineData(InvalidComposition.MissingStore, "no persistence store")]
    [InlineData(InvalidComposition.MissingDispatcher, "no transport dispatcher")]
    [InlineData(InvalidComposition.MissingCatalog, "no message-contract catalog")]
    [InlineData(InvalidComposition.DuplicateStore, "multiple persistence store")]
    [InlineData(InvalidComposition.DuplicateDispatcher, "multiple transport dispatcher")]
    [InlineData(InvalidComposition.MissingStoreLimits, "Store limits were not configured")]
    [InlineData(InvalidComposition.MissingDeliveryPolicy, "Delivery policy was not configured")]
    [InlineData(InvalidComposition.MissingRetention, "Retention was not configured")]
    [InlineData(InvalidComposition.InvalidOptions, "MaximumStoredCount")]
    [InlineData(InvalidComposition.InvalidRetention, "Retention")]
    [RequirementCoverage("REQ-VSB-DURABLE-STARTUP", "invalid-static-composition-fails-before-background-delivery")]
    public async Task StartupValidation_RejectsEveryIncompleteOrAmbiguousCompositionAsync(
        InvalidComposition invalid,
        string expectedMessage)
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus<ITestBus>(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory();
            bus.UseReliableMessaging(durable =>
            {
                if (invalid != InvalidComposition.MissingCatalog)
                {
                    durable.AddMessageContract<KnownMessage>(
                        KnownIdentity.Name,
                        KnownIdentity.MajorVersion);
                }
                if (invalid != InvalidComposition.MissingStore)
                    durable.UseInMemoryStore();
                if (invalid != InvalidComposition.MissingStoreLimits)
                {
                    durable.Store(new ReliableStoreLimits
                    {
                        MaximumStoredCount = invalid == InvalidComposition.InvalidOptions ? 0 : 100,
                        MaximumStoredBytes = 1024 * 1024,
                    });
                }
                if (invalid != InvalidComposition.MissingDeliveryPolicy)
                    durable.Delivery(_ => { });
                if (invalid != InvalidComposition.MissingRetention)
                {
                    durable.Retention(invalid == InvalidComposition.InvalidRetention
                        ? TimeSpan.Zero
                        : TimeSpan.FromDays(1));
                }
            });
        });

        if (invalid == InvalidComposition.DuplicateStore)
            services.AddSingleton(DurableSenderTestFactory.CreateInMemoryStore<ITestBus>());
        if (invalid == InvalidComposition.MissingDispatcher)
            services.RemoveAll<IDurableSendDispatcher<ITestBus>>();
        if (invalid == InvalidComposition.DuplicateDispatcher)
            services.AddSingleton<IDurableSendDispatcher<ITestBus>>(new NoOpDispatcher());
        await using ServiceProvider provider = services.BuildServiceProvider();

        Exception failure;
        try
        {
            IHostedService validator = provider.GetServices<IHostedService>().Single(service =>
                service.GetType().IsGenericType
                && service.GetType().GetGenericTypeDefinition().Name == "BusCompositionStartupValidator`1"
                && service.GetType().GetGenericArguments()[0] == typeof(ITestBus));
            failure = await Assert.ThrowsAnyAsync<Exception>(() =>
                validator.StartAsync(TestContext.Current.CancellationToken));
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        Assert.Contains(expectedMessage, failure.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    static void ConfigureReliablePolicy(IReliableMessagingConfigurator reliable)
    {
        reliable.Store(new ReliableStoreLimits
        {
            MaximumStoredCount = 100,
            MaximumStoredBytes = 1024 * 1024,
        });
        reliable.Delivery(_ => { });
        reliable.Retention(TimeSpan.FromDays(1));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-OPERATIONS", "bounded-requeue-and-discard-use-host-clock")]
    public async Task Operations_ValidatePagesAndUseTheInjectedClockForOperatorTransitionsAsync()
    {
        IOutboxStore<ITestBus> store = DurableSenderTestFactory.CreateInMemoryStore<ITestBus>();
        var time = new FakeTimeProvider(Epoch.AddHours(3));
        using ServiceProvider provider = Services(
                store,
                time,
                builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion))
            .BuildServiceProvider();
        IReliableMessagingOperations<ITestBus> operations = provider.GetRequiredService<IReliableMessagingOperations<ITestBus>>();
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

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => operations.GetOutboxQuarantineAsync(
            DurableSendQuarantineQuery.FirstPage(
                DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize + 1),
            TestCancellationToken));
        Assert.Equal(
            ReliableMessagingOperationDisposition.Applied,
            (await operations.RequeueAsync(ReliableMessageReference.Outbox(message.Id), TestCancellationToken)).Disposition);
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
        Assert.Equal(
            ReliableMessagingOperationDisposition.Applied,
            (await operations.DiscardAsync(ReliableMessageReference.Outbox(message.Id), TestCancellationToken)).Disposition);
        Assert.Equal(
            ReliableMessagingOperationDisposition.NotFound,
            (await operations.DiscardAsync(ReliableMessageReference.Outbox(message.Id), TestCancellationToken)).Disposition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-OPERATIONS-QUERY", "read-contracts-validate-before-provider-and-forward-exact-inputs")]
    public async Task Operations_ReadContractsValidateBeforeProviderAndForwardExactInputsAsync()
    {
        IOutboxStore<ITestBus> inner = DurableSenderTestFactory.CreateInMemoryStore<ITestBus>();
        var store = new ObservingStore(inner);
        using ServiceProvider provider = Services(
                store,
                new FakeTimeProvider(Epoch),
                builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion))
            .BuildServiceProvider();
        IReliableMessagingOperations<ITestBus> operations =
            provider.GetRequiredService<IReliableMessagingOperations<ITestBus>>();
        using var cancellation = new CancellationTokenSource();
        var outboxQuery = DurableSendQuarantineQuery.FirstPage(1);
        var inboxQuery = new ReliableInboxQuarantineQuery { PageSize = 1000 };

        DurableSendStoreSnapshot snapshot = await operations.GetSnapshotAsync(cancellation.Token);
        DurableSendQuarantinePage outbox = await operations.GetOutboxQuarantineAsync(outboxQuery, cancellation.Token);
        ReliableInboxQuarantinePage inbox = await operations.GetInboxQuarantineAsync(inboxQuery, cancellation.Token);

        Assert.Equal(default, snapshot);
        Assert.Empty(outbox.Entries);
        Assert.Null(outbox.ContinuationToken);
        Assert.Empty(inbox.Entries);
        Assert.Null(inbox.Next);
        Assert.Equal(cancellation.Token, store.SnapshotCancellationToken);
        Assert.Equal(cancellation.Token, store.OutboxQueryCancellationToken);
        Assert.Equal(cancellation.Token, store.InboxQueryCancellationToken);
        Assert.Same(outboxQuery, store.OutboxQuery);
        Assert.Same(inboxQuery, store.InboxQuery);
        Assert.Equal(1, store.OutboxQueryCalls);
        Assert.Equal(1, store.InboxQueryCalls);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operations.GetOutboxQuarantineAsync(null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            operations.GetOutboxQuarantineAsync(new DurableSendQuarantineQuery { PageSize = 0 }, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            operations.GetOutboxQuarantineAsync(new DurableSendQuarantineQuery { ContinuationToken = "invalid" }, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            operations.GetInboxQuarantineAsync(null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            operations.GetInboxQuarantineAsync(new ReliableInboxQuarantineQuery { PageSize = 0 }, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            operations.GetInboxQuarantineAsync(new ReliableInboxQuarantineQuery { PageSize = 1001 }, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            operations.GetInboxQuarantineAsync(
                new ReliableInboxQuarantineQuery { AfterQuarantinedAt = Epoch },
                TestCancellationToken));

        Assert.Equal(1, store.OutboxQueryCalls);
        Assert.Equal(1, store.InboxQueryCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-OPERATIONS", "inbox-requeue-discard-and-outbox-abandon-boundaries")]
    public async Task Operations_ApplyInboxTransitionsAndRejectOutboxAbandonWithoutLosingStateAsync()
    {
        IOutboxStore<ITestBus> store = DurableSenderTestFactory.CreateInMemoryStore<ITestBus>();
        IInboxStore<ITestBus> inbox = Assert.IsAssignableFrom<IInboxStore<ITestBus>>(store);
        var now = Epoch.AddHours(6);
        using ServiceProvider provider = Services(
                store,
                new FakeTimeProvider(now),
                builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion))
            .BuildServiceProvider();
        IReliableMessagingOperations<ITestBus> operations =
            provider.GetRequiredService<IReliableMessagingOperations<ITestBus>>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            operations.RequeueAsync(default, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            operations.DiscardAsync(default, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            operations.AbandonAsync(default, TestCancellationToken));

        var key = new ReliableInboxKey(GuidFrom(41), GuidFrom(42));
        ReliableInboxAcquireResult firstAcquire = await inbox.AcquireAsync(
            key,
            Epoch,
            TimeSpan.FromMinutes(1),
            TestCancellationToken);
        Assert.True(await inbox.QuarantineAsync(
            key,
            Assert.IsType<ReliableInboxLease>(firstAcquire.Lease),
            "Tests.FirstFailure",
            Epoch,
            TestCancellationToken));

        ReliableMessagingOperationResult requeued = await operations.RequeueAsync(
            ReliableMessageReference.Inbox(key),
            TestCancellationToken);
        ReliableInboxAcquireResult secondAcquire = await inbox.AcquireAsync(
            key,
            now,
            TimeSpan.FromMinutes(1),
            TestCancellationToken);
        Assert.True(await inbox.QuarantineAsync(
            key,
            Assert.IsType<ReliableInboxLease>(secondAcquire.Lease),
            "Tests.SecondFailure",
            now,
            TestCancellationToken));
        ReliableMessagingOperationResult discarded = await operations.DiscardAsync(
            ReliableMessageReference.Inbox(key),
            TestCancellationToken);
        ReliableMessagingOperationResult missing = await operations.DiscardAsync(
            ReliableMessageReference.Inbox(key),
            TestCancellationToken);
        ReliableMessagingOperationResult invalidAbandon = await operations.AbandonAsync(
            ReliableMessageReference.Outbox(new DurableSendId(GuidFrom(43))),
            TestCancellationToken);

        Assert.Equal(ReliableMessagingOperationDisposition.Applied, requeued.Disposition);
        Assert.Equal("Quarantined", requeued.PreviousState);
        Assert.Equal("RetryScheduled", requeued.CurrentState);
        Assert.Equal(ReliableInboxAcquireDisposition.Acquired, secondAcquire.Disposition);
        Assert.Equal(2, secondAcquire.Attempt);
        Assert.Equal(ReliableMessagingOperationDisposition.Applied, discarded.Disposition);
        Assert.Equal("Quarantined", discarded.PreviousState);
        Assert.Equal("Discarded", discarded.CurrentState);
        Assert.Equal(ReliableMessagingOperationDisposition.NotFound, missing.Disposition);
        Assert.Equal(ReliableMessagingOperationDisposition.InvalidState, invalidAbandon.Disposition);
        Assert.Equal("Quarantined", invalidAbandon.PreviousState);
        Assert.Equal("Quarantined", invalidAbandon.CurrentState);
        Assert.Empty((await operations.GetInboxQuarantineAsync(
            new ReliableInboxQuarantineQuery(),
            TestCancellationToken)).Entries);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-OPERATIONS", "abandon-is-retained-logged-metered-once-and-observation-safe")]
    public async Task Operations_AbandonLogsAndMetersOnlyTheAppliedRetainedDecisionAsync()
    {
        IOutboxStore<ITestBus> store = DurableSenderTestFactory.CreateInMemoryStore<ITestBus>();
        IInboxStore<ITestBus> inbox = Assert.IsAssignableFrom<IInboxStore<ITestBus>>(store);
        var time = new FakeTimeProvider(Epoch.AddHours(4));
        var logs = new RecordingLoggerProvider();
        using var measurements = new MeterListener();
        var abandoned = new ConcurrentQueue<long>();
        measurements.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "ViciOne.ServiceBus"
                && instrument.Name == ServiceBusTelemetry.Metrics.ReliabilityAbandoned)
                listener.EnableMeasurementEvents(instrument);
        };
        measurements.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            _ = instrument;
            _ = tags;
            _ = state;
            abandoned.Enqueue(value);
        });
        measurements.Start();

        IServiceCollection services = Services(
            store,
            time,
            builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion));
        services.RemoveAll(typeof(ILogger<>));
        services.AddSingleton(logs);
        services.AddSingleton(typeof(ILogger<>), typeof(RecordingTypedLogger<>));
        using ServiceProvider provider = services.BuildServiceProvider();
        IReliableMessagingOperations<ITestBus> operations = provider.GetRequiredService<IReliableMessagingOperations<ITestBus>>();
        var key = new ReliableInboxKey(GuidFrom(31), GuidFrom(32));
        ReliableInboxAcquireResult acquired = await inbox.AcquireAsync(
            key,
            Epoch,
            TimeSpan.FromMinutes(1),
            TestCancellationToken);
        Assert.True(await inbox.QuarantineAsync(
            key,
            Assert.IsType<ReliableInboxLease>(acquired.Lease),
            "Tests.Permanent",
            Epoch,
            TestCancellationToken));

        ReliableMessagingOperationResult applied = await operations.AbandonAsync(
            ReliableMessageReference.Inbox(key),
            TestCancellationToken);
        ReliableMessagingOperationResult repeated = await operations.AbandonAsync(
            ReliableMessageReference.Inbox(key),
            TestCancellationToken);

        Assert.Equal(ReliableMessagingOperationDisposition.Applied, applied.Disposition);
        Assert.Equal("Abandoned", applied.CurrentState);
        Assert.Equal(ReliableMessagingOperationDisposition.InvalidState, repeated.Disposition);
        string log = Assert.Single(logs.Messages);
        Assert.Contains("abandoned", log, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(key.MessageId.ToString(), log, StringComparison.Ordinal);
        Assert.Equal(1, abandoned.Sum());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-HEALTH", "capacity-is-degraded-with-bounded-nonsensitive-data")]
    public async Task HealthCheck_ReportsCapacityWithoutMessagePayloadOrIdentityAsync()
    {
        IOutboxStore<ITestBus> store = DurableSenderTestFactory.CreateInMemoryStore<ITestBus>();
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
        services.AddHealthChecks().AddViciOneReliableMessagingHealthCheck<ITestBus>();
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
        IOutboxStore<ITestBus> store,
        TimeProvider timeProvider,
        Action<MessageContractCatalogBuilder> catalog,
        Action<ReliableMessagingOptions<ITestBus>>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store);
        if (store is IInboxStore<ITestBus> inboxStore)
            services.AddSingleton(inboxStore);
        if (store is IScheduleStore<ITestBus> scheduleStore)
            services.AddSingleton(scheduleStore);
        services.AddSingleton(timeProvider);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneMessageContracts(catalog);
        services.AddViciOneReliableMessaging(configure);
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

    private static Guid GuidFrom(int value) => new(value, 0, 0, new byte[8]);

    public interface ITestBus : IBus;
    public interface IOtherBus : IBus;
    private sealed record KnownMessage;
    private sealed record OtherMessage;
    private sealed record TypedMessage(string Value);
    public sealed class DurableDataMessage
    {
        public required MessageData<string> Value { get; init; }
    }

    public enum InvalidComposition
    {
        MissingStore,
        MissingDispatcher,
        MissingCatalog,
        DuplicateStore,
        DuplicateDispatcher,
        MissingStoreLimits,
        MissingDeliveryPolicy,
        MissingRetention,
        InvalidOptions,
        InvalidRetention,
    }

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

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        readonly ConcurrentQueue<string> _messages = new();

        public IReadOnlyList<string> Messages => _messages.ToArray();

        public ILogger CreateLogger(string categoryName)
        {
            _ = categoryName;
            return new RecordingLogger(_messages);
        }

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                _ = state;
                return null;
            }

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                _ = eventId;
                if (IsEnabled(logLevel))
                    messages.Enqueue(formatter(state, exception));
            }
        }
    }

    private sealed class RecordingTypedLogger<T>(RecordingLoggerProvider provider) : ILogger<T>
    {
        readonly ILogger _logger = provider.CreateLogger(typeof(T).FullName ?? typeof(T).Name);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _logger.Log(logLevel, eventId, state, exception, formatter);
    }

    private sealed class ObservingStore(IOutboxStore<ITestBus> inner) :
        IOutboxStore<ITestBus>,
        IInboxStore<ITestBus>
    {
        public DurableSendAdmissionResult? AdmissionResultOverride { get; init; }
        public int AdmitCalls { get; private set; }
        public SerializedDurableSend? LastMessage { get; private set; }
        public DurableSendStoreLimits LastLimits { get; private set; }
        public DateTimeOffset LastEnqueuedAt { get; private set; }
        public int OutboxQueryCalls { get; private set; }
        public int InboxQueryCalls { get; private set; }
        public CancellationToken SnapshotCancellationToken { get; private set; }
        public CancellationToken OutboxQueryCancellationToken { get; private set; }
        public CancellationToken InboxQueryCancellationToken { get; private set; }
        public DurableSendQuarantineQuery? OutboxQuery { get; private set; }
        public ReliableInboxQuarantineQuery? InboxQuery { get; private set; }

        private IInboxStore<ITestBus> Inbox =>
            inner as IInboxStore<ITestBus>
            ?? throw new InvalidOperationException("The observing store requires an inbox-capable inner store.");

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
            return AdmissionResultOverride.HasValue
                ? Task.FromResult(AdmissionResultOverride.Value)
                : inner.AdmitAsync(message, limits, enqueuedAt, cancellationToken);
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

        public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            SnapshotCancellationToken = cancellationToken;
            return inner.GetSnapshotAsync(cancellationToken);
        }

        public Task<DurableSendQuarantinePage> GetQuarantineAsync(
            DurableSendQuarantineQuery query,
            CancellationToken cancellationToken = default)
        {
            OutboxQueryCalls++;
            OutboxQuery = query;
            OutboxQueryCancellationToken = cancellationToken;
            return inner.GetQuarantineAsync(query, cancellationToken);
        }

        public Task<DurableSendOperationResult> RequeueAsync(
            DurableSendId id,
            DateTimeOffset dueAt,
            CancellationToken cancellationToken = default) =>
            inner.RequeueAsync(id, dueAt, cancellationToken);

        public Task<DurableSendOperationResult> DiscardQuarantinedAsync(
            DurableSendId id,
            CancellationToken cancellationToken = default) =>
            inner.DiscardQuarantinedAsync(id, cancellationToken);

        public Task<ReliableInboxAcquireResult> AcquireAsync(
            ReliableInboxKey key,
            DateTimeOffset now,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default) =>
            Inbox.AcquireAsync(key, now, leaseDuration, cancellationToken);

        public Task<bool> CompleteAsync(
            ReliableInboxKey key,
            ReliableInboxLease lease,
            DateTimeOffset consumedAt,
            CancellationToken cancellationToken = default) =>
            Inbox.CompleteAsync(key, lease, consumedAt, cancellationToken);

        public Task<bool> ScheduleRetryAsync(
            ReliableInboxKey key,
            ReliableInboxLease lease,
            DateTimeOffset dueAt,
            string? failureType,
            DateTimeOffset failedAt,
            CancellationToken cancellationToken = default) =>
            Inbox.ScheduleRetryAsync(key, lease, dueAt, failureType, failedAt, cancellationToken);

        public Task<bool> QuarantineAsync(
            ReliableInboxKey key,
            ReliableInboxLease lease,
            string? failureType,
            DateTimeOffset quarantinedAt,
            CancellationToken cancellationToken = default) =>
            Inbox.QuarantineAsync(key, lease, failureType, quarantinedAt, cancellationToken);

        public Task<ReliableInboxQuarantinePage> GetQuarantineAsync(
            ReliableInboxQuarantineQuery query,
            CancellationToken cancellationToken = default)
        {
            InboxQueryCalls++;
            InboxQuery = query;
            InboxQueryCancellationToken = cancellationToken;
            return Inbox.GetQuarantineAsync(query, cancellationToken);
        }

        public Task<ReliableMessagingOperationResult> RequeueAsync(
            ReliableInboxKey key,
            DateTimeOffset dueAt,
            CancellationToken cancellationToken = default) =>
            Inbox.RequeueAsync(key, dueAt, cancellationToken);

        public Task<ReliableMessagingOperationResult> DiscardAsync(
            ReliableInboxKey key,
            CancellationToken cancellationToken = default) =>
            Inbox.DiscardAsync(key, cancellationToken);

        public Task<ReliableMessagingOperationResult> AbandonAsync(
            ReliableInboxKey key,
            DateTimeOffset abandonedAt,
            CancellationToken cancellationToken = default) =>
            Inbox.AbandonAsync(key, abandonedAt, cancellationToken);
    }
}
