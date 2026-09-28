using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

public sealed class BusCompositionStartupValidationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "one-validator-per-bus-runs-before-runtime-host")]
    public void Registration_AddsExactlyOneCompositionValidatorPerBusBeforeTheRuntimeHost()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory();
        });
        services.AddViciOneServiceBus<IOrdersBus>(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory();
        });

        ServiceDescriptor[] hosted = services.Where(static descriptor => descriptor.ServiceType == typeof(IHostedService)).ToArray();
        ServiceDescriptor[] validators = hosted.Where(static descriptor =>
            descriptor.ImplementationType?.IsGenericType == true
            && descriptor.ImplementationType.GetGenericTypeDefinition().Name == "BusCompositionStartupValidator`1").ToArray();
        ServiceDescriptor runtime = Assert.Single(hosted, static descriptor =>
            descriptor.ImplementationType?.Name == "ServiceBusHostedService");

        Assert.Equal(2, validators.Length);
        Assert.Equal([typeof(IBus), typeof(IOrdersBus)], validators
            .Select(static descriptor => descriptor.ImplementationType!.GetGenericArguments()[0])
            .ToArray());
        Assert.All(validators, validator => Assert.True(Array.IndexOf(hosted, validator) < Array.IndexOf(hosted, runtime)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "all-static-ownership-failures-are-aggregated")]
    public async Task StartupValidation_AggregatesMissingTransportAndLimitsAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTextWriterLogger(TextWriter.Null)
            .AddViciOneServiceBus(_ => { })
            .BuildServiceProvider();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            CompositionValidator<IBus>(provider).StartAsync(TestContext.Current.CancellationToken));

        string[] failures = exception.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, failures.Length);
        Assert.Contains(failures, static failure => failure.StartsWith("Transport for bus 'default':", StringComparison.Ordinal));
        Assert.Contains(failures, static failure => failure.StartsWith("Message limits for bus 'default':", StringComparison.Ordinal));
        Assert.All(failures, static failure => Assert.EndsWith(".", failure, StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "orphan-feature-bus-types-fail-at-start")]
    public async Task StartupValidation_RejectsPayloadAdmissionForAnUnregisteredBusAsync()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus<IOrdersBus>(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory();
        });
        services.AddViciOnePayloadAdmission<IBillingBus>(options =>
        {
            options.MaximumSerializedBodyBytes = 1024;
            options.MaximumTransportEnvelopeBytes = 2048;
        });
        await using ServiceProvider provider = services.BuildServiceProvider();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            CompositionValidator<IOrdersBus>(provider).StartAsync(TestContext.Current.CancellationToken));

        Assert.StartsWith("Payload admission for bus '", exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(IBillingBus).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains("no matching bus is registered", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddViciOneServiceBus<TBus>", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "duplicate-feature-ownership-fails-before-bus-start")]
    public async Task StartupValidation_RejectsDuplicatePayloadAdmissionOwnershipAsync()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory();
        });
        services.AddViciOnePayloadAdmission<IBus>(options =>
        {
            options.MaximumSerializedBodyBytes = 1024;
            options.MaximumTransportEnvelopeBytes = 2048;
        });
        await using ServiceProvider provider = services.BuildServiceProvider();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            CompositionValidator<IBus>(provider).StartAsync(TestContext.Current.CancellationToken));

        Assert.Single(exception.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("Payload admission for bus 'default':", exception.Message, StringComparison.Ordinal);
        Assert.Contains("multiple feature owners are registered", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Configure the feature exactly once", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "durable-components-report-every-missing-owner")]
    public async Task StartupValidation_AggregatesEveryMissingDurableOwnerAsync()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory();
            bus.UseReliableMessaging(reliable => ConfigurePolicy(reliable));
        });
        await using ServiceProvider provider = services.BuildServiceProvider();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            CompositionValidator<IBus>(provider).StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("no message-contract catalog", exception.Message, StringComparison.Ordinal);
        Assert.Contains("no persistence store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("no inbox store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("no schedule store", exception.Message, StringComparison.Ordinal);
        Assert.Equal(4, exception.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "materialization-failures-do-not-short-circuit-aggregation")]
    public async Task StartupValidation_AggregatesCatalogMaterializationAndMissingStoreFailuresAsync()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory();
            bus.UseReliableMessaging(durable =>
            {
                ConfigurePolicy(durable);
                durable.AddMessageContract<JournalProbe>("vicione.tests.journal-probe");
                durable.AddMessageContract<JournalProbe>("vicione.tests.conflicting-journal-probe");
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            CompositionValidator<IBus>(provider).StartAsync(TestContext.Current.CancellationToken));

        string[] failures = exception.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, failures.Length);
        Assert.Contains(failures, static failure => failure.Contains("cannot also be", StringComparison.Ordinal));
        Assert.Contains(failures, static failure => failure.Contains("no persistence store", StringComparison.Ordinal));
        Assert.Contains(failures, static failure => failure.Contains("no inbox store", StringComparison.Ordinal));
        Assert.Contains(failures, static failure => failure.Contains("no schedule store", StringComparison.Ordinal));
        Assert.All(failures, static failure => Assert.StartsWith("Reliable messaging for bus 'default':", failure, StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "serializer-owner-is-unique-before-runtime-start")]
    public async Task StartupValidation_RejectsAnAmbiguousSerializerOwnerAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTextWriterLogger(TextWriter.Null)
            .AddViciOneServiceBus(bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.UsingInMemory((_, transport) =>
                {
                    transport.ClearSerialization();
                    var json = new SystemTextJsonMessageSerializerFactory();
                    transport.AddSerializer(json, isSerializer: false);
                    transport.AddSerializer(new SystemTextJsonRawMessageSerializerFactory(), isSerializer: false);
                    transport.AddDeserializer(json, isDefault: true);
                });
            })
            .BuildServiceProvider();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            CompositionValidator<IBus>(provider).StartAsync(TestContext.Current.CancellationToken));

        Assert.StartsWith("Bus composition for bus 'default':", exception.Message, StringComparison.Ordinal);
        Assert.Contains("SerializerContentType", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("Correct the named configuration", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "contracts-and-redaction-live-in-owning-bus-block")]
    public async Task BusBlock_RegistersContractsAndDiagnosticRedactionForItsOwnerAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTextWriterLogger(TextWriter.Null)
            .AddViciOneServiceBus(bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.Contracts(contracts => contracts.Register<JournalProbe>("vicione.tests.journal-probe"));
                bus.Redaction();
                bus.UsingInMemory();
            })
            .BuildServiceProvider();

        await CompositionValidator<IBus>(provider).StartAsync(TestContext.Current.CancellationToken);

        IMessageContractCatalog contracts = provider.GetRequiredService<IMessageContractCatalog>();
        Assert.Equal(
            typeof(JournalProbe),
            contracts.GetMessageType(new MessageContractIdentity("vicione.tests.journal-probe", 1)));
        Assert.IsType<MessageDiagnosticRedactor>(provider.GetRequiredService<IMessageDiagnosticRedactor>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-ACTIVATION", "bus-block-connects-send-and-consume-observers")]
    public async Task UseMessageJournal_ConnectsOnlyTheOwningBusAndObservesBothDirectionsAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        var store = new RecordingStore();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(bus =>
            {
                bus.UseMessageJournal(journal => journal
                    .UseStore(store)
                    .Policy(new SanitizingPolicy())
                    .Options(MessageJournalOptions.ContinueMessageFlow(TimeSpan.FromSeconds(1), TimeProvider.System)));
                bus.AddHandler<JournalProbe>((ConsumeContext<JournalProbe> _) => Task.CompletedTask);
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(TestContext.Current.CancellationToken)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        try
        {
            ISendEndpoint endpoint = await harness.GetHandlerEndpointAsync<JournalProbe>(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            await endpoint.SendAsync(
                    new JournalProbe("journalled"),
                    TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            await store.SendAndConsumeObserved.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Contains(store.Entries, static entry => entry.Operation == MessageJournalOperation.Send);
        Assert.Contains(store.Entries, static entry => entry.Operation == MessageJournalOperation.Consume);
        Assert.All(store.Entries, static entry => Assert.Empty(entry.Body.ToArray()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-ACTIVATION", "journal-observers-stay-on-selected-bus-with-live-neighbor")]
    public async Task UseMessageJournal_ObservesOnlyItsSelectedBusWhileBothBusesDeliverAsync(bool journalOnSecondary)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var store = new RecordingStore();
        var primaryDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondaryDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Guid primaryId = NewId.NextGuid();
        Guid secondaryId = NewId.NextGuid();
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            if (!journalOnSecondary)
                bus.UseMessageJournal(journal => journal
                    .UseStore(store)
                    .Policy(new CorrelationOnlyPolicy())
                    .Options(MessageJournalOptions.ContinueMessageFlow(TimeSpan.FromSeconds(1), TimeProvider.System)));
            bus.UsingInMemory((_, transport) =>
            {
                transport.Host(new Uri("loopback://t58-journal-primary/"));
                transport.ReceiveEndpoint("input", endpoint => endpoint.Handler<OwnedJournalProbe>(_ =>
                {
                    primaryDelivered.TrySetResult();
                    return Task.CompletedTask;
                }));
            });
        });
        services.AddViciOneServiceBus<IOrdersBus>(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            if (journalOnSecondary)
                bus.UseMessageJournal(journal => journal
                    .UseStore(store)
                    .Policy(new CorrelationOnlyPolicy())
                    .Options(MessageJournalOptions.ContinueMessageFlow(TimeSpan.FromSeconds(1), TimeProvider.System)));
            bus.UsingInMemory((_, transport) =>
            {
                transport.Host(new Uri("loopback://t58-journal-secondary/"));
                transport.ReceiveEndpoint("input", endpoint => endpoint.Handler<OwnedJournalProbe>(_ =>
                {
                    secondaryDelivered.TrySetResult();
                    return Task.CompletedTask;
                }));
            });
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        IBusControl primary = provider.GetRequiredService<IBusControl>();
        IBusControl secondary = (IBusControl)provider.GetRequiredService<IOrdersBus>();
        try
        {
            await primary.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            await secondary.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

            ISendEndpoint primaryInput = await primary.GetSendEndpointAsync(
                new Uri("loopback://t58-journal-primary/input"), cancellationToken: cancellationToken);
            ISendEndpoint secondaryInput = await secondary.GetSendEndpointAsync(
                new Uri("loopback://t58-journal-secondary/input"), cancellationToken: cancellationToken);
            await primaryInput.SendAsync(new OwnedJournalProbe(primaryId), cancellationToken);
            await secondaryInput.SendAsync(new OwnedJournalProbe(secondaryId), cancellationToken);
            await primaryDelivered.Task.WaitAsync(timeout, cancellationToken);
            await secondaryDelivered.Task.WaitAsync(timeout, cancellationToken);
            await store.SendAndConsumeObserved.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await secondary.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await primary.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(2, store.Entries.Count);
        Assert.Equal(1, store.Entries.Count(static entry => entry.Operation == MessageJournalOperation.Send));
        Assert.Equal(1, store.Entries.Count(static entry => entry.Operation == MessageJournalOperation.Consume));
        string ownerId = (journalOnSecondary ? secondaryId : primaryId).ToString("D");
        Assert.All(store.Entries, entry => Assert.Equal(
            ownerId, Assert.Single(entry.Metadata).Value));
        Assert.All(store.Entries, static entry => Assert.Equal(
            MessageJournalMetadataKeys.CorrelationId, Assert.Single(entry.Metadata).Key));
        Assert.All(store.Entries, static entry => Assert.Empty(entry.Body.ToArray()));

    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-ACTIVATION", "incomplete-builder-reports-all-required-parts")]
    public void UseMessageJournal_RejectsEveryMissingRequiredPartTogether()
    {
        var services = new ServiceCollection();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            services.AddViciOneServiceBus(bus => bus.UseMessageJournal(_ => { })));

        Assert.Contains("no persistence store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("no policy", exception.Message, StringComparison.Ordinal);
        Assert.Contains("no runtime options", exception.Message, StringComparison.Ordinal);
        Assert.Equal(3, exception.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
    }

    static IHostedService CompositionValidator<TBus>(IServiceProvider provider)
        where TBus : class, IBus
        => provider.GetServices<IHostedService>().Single(service =>
            service.GetType().IsGenericType
            && service.GetType().GetGenericTypeDefinition().Name == "BusCompositionStartupValidator`1"
            && service.GetType().GetGenericArguments()[0] == typeof(TBus));

    static void ConfigurePolicy<TBus>(IReliableMessagingConfigurator<TBus> reliable)
        where TBus : class, IBus
    {
        reliable.Store(new ReliableStoreLimits
        {
            MaximumStoredCount = 10_000,
            MaximumStoredBytes = 16 * 1024 * 1024,
        });
        reliable.Delivery(_ => { });
        reliable.Retention(TimeSpan.FromDays(7));
    }

    public interface IOrdersBus : IBus;

    public interface IBillingBus : IBus;

    public sealed record JournalProbe(string Value);

    public sealed record OwnedJournalProbe(Guid CorrelationId) : ICorrelatedBy<Guid>;

    sealed class RecordingStore : IMessageJournalStore
    {
        public ConcurrentQueue<MessageJournalEntry> Entries { get; } = new();

        public TaskCompletionSource SendAndConsumeObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public MessageJournalStoreLimits Limits { get; } = new(
            64 * 1024,
            maximumEntries: 100,
            retentionPeriod: TimeSpan.FromHours(1));

        public ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Entries.Enqueue(entry);
            if (Entries.Any(static candidate => candidate.Operation == MessageJournalOperation.Send)
                && Entries.Any(static candidate => candidate.Operation == MessageJournalOperation.Consume))
                SendAndConsumeObserved.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }

    sealed class SanitizingPolicy : IMessageJournalPolicy
    {
        public ValueTask<MessageJournalProjection?> ProjectAsync(
            MessageJournalCapture capture,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
                MessageJournalDataClassification.Internal,
                capture.ContentType,
                capture.MessageTypes));
        }
    }

    sealed class CorrelationOnlyPolicy : IMessageJournalPolicy
    {
        public ValueTask<MessageJournalProjection?> ProjectAsync(
            MessageJournalCapture capture,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string correlationId = capture.Metadata[MessageJournalMetadataKeys.CorrelationId];
            return ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
                MessageJournalDataClassification.Internal,
                capture.ContentType,
                capture.MessageTypes,
                new Dictionary<string, string>
                {
                    [MessageJournalMetadataKeys.CorrelationId] = correlationId,
                }));
        }
    }
}
