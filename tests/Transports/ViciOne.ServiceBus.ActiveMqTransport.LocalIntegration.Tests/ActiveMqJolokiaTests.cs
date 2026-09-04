using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

public sealed class ActiveMqJolokiaTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-RUN-SCOPED-ADMIN", "nondefault-jolokia-endpoint-deletes-only-the-owned-entity")]
    public async Task RunScopedAdminEndpoint_CleansExactlyThisBrokerAsync()
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(ActiveMqBroker.OpenWireFlavor, "admin-cleanup");
        string target = fixture.Name("target");
        string sentinel = fixture.Name("sentinel");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using (IConnection connection = fixture.CreateConnection())
        {
            connection.Start();
            using ISession session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
            using IMessageConsumer targetConsumer = session.CreateConsumer(session.GetQueue(target));
            using IMessageConsumer sentinelConsumer = session.CreateConsumer(session.GetQueue(sentinel));

            Assert.True(await fixture.ClassicQueueExistsAsync(target, cancellationToken));
            Assert.True(await fixture.ClassicQueueExistsAsync(sentinel, cancellationToken));
        }

        try
        {
            await fixture.DeleteClassicQueueAsync(target, cancellationToken);

            Assert.False(await fixture.ClassicQueueExistsAsync(target, cancellationToken));
            Assert.True(await fixture.ClassicQueueExistsAsync(sentinel, cancellationToken));
        }
        finally
        {
            if (await fixture.ClassicQueueExistsAsync(target, CancellationToken.None))
                await fixture.DeleteClassicQueueAsync(target, CancellationToken.None);
            if (await fixture.ClassicQueueExistsAsync(sentinel, CancellationToken.None))
                await fixture.DeleteClassicQueueAsync(sentinel, CancellationToken.None);
        }
    }
}
