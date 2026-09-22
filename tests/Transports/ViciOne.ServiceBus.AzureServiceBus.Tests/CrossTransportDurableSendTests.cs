using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class CrossTransportDurableSendTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SEND-FAILURE", "azure-first-activemq-permanent-failure-quarantines")]
    public async Task AzureRegisteredFirst_ActiveMqConfigurationFailureQuarantinesWithoutRetryAsync()
    {
        using ServiceProvider provider = RegisterBothTransports();
        ITransportSendFailureClassifier[] classifiers = provider.GetServices<ITransportSendFailureClassifier>().ToArray();
        Assert.Collection(classifiers,
            classifier => Assert.IsType<ServiceBusSendFailureClassifier>(classifier),
            classifier => Assert.IsType<ActiveMqSendFailureClassifier>(classifier));

        IOutboxStore<IActiveMqBus> store = DurableSenderTestFactory.CreateInMemoryStore<IActiveMqBus>();
        var failure = new ActiveMqConnectionException("broker connection",
            new ActiveMqTransportConfigurationException("invalid endpoint", new TimeoutException("socket timed out")));
        var dispatcher = new FailingDispatcher(failure);
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));
        using DurableSenderDeliveryTestDriver<IActiveMqBus> driver = DurableSenderTestFactory.CreateDeliveryDriver(
            store, dispatcher, clock, options => options.MaximumDeliveryAttempts = 3, classifiers);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await store.AdmitAsync(Message(), new DurableSendStoreLimits(10, 1024),
            clock.GetUtcNow(), cancellationToken);

        Assert.True(await driver.DeliverDueBatchAsync(cancellationToken));

        Assert.Equal(1, dispatcher.DispatchCount);
        DurableSendStoreSnapshot snapshot = await store.GetSnapshotAsync(cancellationToken);
        Assert.Equal(0, snapshot.RetryScheduledCount);
        DurableSendQuarantineEntry quarantined = Assert.Single((await store.GetQuarantineAsync(
            DurableSendQuarantineQuery.FirstPage(10), cancellationToken)).Entries);
        Assert.Equal(DurableSendFailureKind.NonRetryable, quarantined.FailureKind);
        Assert.Equal(1, quarantined.DeliveryAttempts);
        Assert.False(await driver.DeliverDueBatchAsync(cancellationToken));
    }

    public interface IActiveMqBus : IBus;

    private static ServiceProvider RegisterBothTransports()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration => configuration.UsingAzureServiceBus());
        services.AddViciOneServiceBus<IActiveMqBus>(configuration => configuration.UsingActiveMq());
        return services.BuildServiceProvider();
    }

    private static SerializedDurableSend Message() => new()
    {
        Id = new DurableSendId(Guid.Parse("00000000-0000-0000-0000-000000000001")),
        ContractIdentity = new MessageContractIdentity("vicione.tests.cross-transport", 1),
        DestinationAddress = new Uri("activemq://broker/queue"),
        ContentType = "application/octet-stream",
        Body = new byte[] { 1, 2, 3 }
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FailingDispatcher(Exception failure) : IDurableSendDispatcher<IActiveMqBus>
    {
        public int DispatchCount { get; private set; }

        public Task<DurableSendDispatchResult> DispatchAsync(
            DurableSendDispatchContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DispatchCount++;
            return Task.FromException<DurableSendDispatchResult>(failure);
        }
    }
}
