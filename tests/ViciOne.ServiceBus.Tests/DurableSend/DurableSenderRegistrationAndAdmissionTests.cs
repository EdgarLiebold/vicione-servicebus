using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class DurableSenderRegistrationAndAdmissionTests
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
            configuration.UseDurableSender(durable =>
            {
                durable.UseInMemoryStore();
                durable.AddMessageContract<TypedMessage>("vicione.tests.typed-durable", 3);
                durable.Configure(options =>
                {
                    options.MaximumStoredCount = 4;
                    options.MaximumStoredBytes = 64 * 1024;
                });
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IDurableSender<IBus> sender = scope.ServiceProvider.GetRequiredService<IDurableSender<IBus>>();
        IDurableSendStore<IBus> store = scope.ServiceProvider.GetRequiredService<IDurableSendStore<IBus>>();
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
            configuration.UseDurableSender(durable =>
            {
                durable.UseInMemoryStore();
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
            configuration.UseDurableSender(durable =>
            {
                durable.UseInMemoryStore();
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

        IDurableSendStore<IBus> primaryStore = scope.ServiceProvider.GetRequiredService<IDurableSendStore<IBus>>();
        IDurableSendStore<IOtherBus> secondaryStore = scope.ServiceProvider.GetRequiredService<IDurableSendStore<IOtherBus>>();
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
            configuration.UseDurableSender(durable =>
            {
                durable.UseInMemoryStore();
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
        IDurableSendStore<IBus> store = scope.ServiceProvider.GetRequiredService<IDurableSendStore<IBus>>();
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
            configuration.UseDurableSender(durable =>
            {
                durable.UseInMemoryStore();
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
        IDurableSendStore<IBus> store = scope.ServiceProvider.GetRequiredService<IDurableSendStore<IBus>>();

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
    [RequirementCoverage("REQ-VSB-DURABLE-ADMISSION-CATALOG", "unknown-contract-rejected-before-store")]
    public async Task Admission_UnknownStableIdentityIsRejectedBeforeAnyStoreMutationAsync()
    {
        var store = new ObservingStore(DurableSenderTestFactory.CreateInMemoryStore<ITestBus>());
        using ServiceProvider provider = Services(
                store,
                new FakeTimeProvider(Epoch),
                builder => builder.Register<KnownMessage>(KnownIdentity.Name, KnownIdentity.MajorVersion))
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
    [InlineData(InvalidComposition.InvalidOptions, "MaximumStoredCount")]
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
            bus.UseDurableSender(durable =>
            {
                if (invalid != InvalidComposition.MissingCatalog)
                {
                    durable.AddMessageContract<KnownMessage>(
                        KnownIdentity.Name,
                        KnownIdentity.MajorVersion);
                }
                if (invalid != InvalidComposition.MissingStore)
                    durable.UseInMemoryStore();
                if (invalid == InvalidComposition.InvalidOptions)
                    durable.Configure(options => options.MaximumStoredCount = 0);
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

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-OPERATIONS", "bounded-requeue-and-discard-use-host-clock")]
    public async Task Operations_ValidatePagesAndUseTheInjectedClockForOperatorTransitionsAsync()
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
            DurableSendQuarantineQuery.FirstPage(
                DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize + 1),
            TestCancellationToken));
        Assert.Equal(
            DurableSendOperationOutcome.Requeued,
            (await operations.RequeueAsync(message.Id, TestCancellationToken)).Outcome);
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
            DurableSendOperationOutcome.Discarded,
            (await operations.DiscardAsync(message.Id, TestCancellationToken)).Outcome);
        Assert.Equal(
            DurableSendOperationOutcome.NotFound,
            (await operations.DiscardAsync(message.Id, TestCancellationToken)).Outcome);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-HEALTH", "capacity-is-degraded-with-bounded-nonsensitive-data")]
    public async Task HealthCheck_ReportsCapacityWithoutMessagePayloadOrIdentityAsync()
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
        InvalidOptions,
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
}
