namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class ActiveMqCompressionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-OPENWIRE-COMPRESSION", "configured-compression-roundtrips-the-exact-broker-payload")]
    public async Task OpenWireCompression_RoundTripsExactPayloadAcrossBroker()
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(ActiveMqBroker.OpenWireFlavor, "compression");
        string queueName = fixture.Name("input");
        string expected = string.Concat(
            "ViciOne-Compression-Boundary-ä-",
            new string('x', 256 * 1024),
            "-終-🚀");
        var received = new TaskCompletionSource<CompressedPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(
                configurator,
                host => host.TransportOptions(new Dictionary<string, string> { ["nms.useCompression"] = "true" }));
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<CompressedPayload>(context =>
            {
                received.TrySetResult(context.Message);
                return Task.CompletedTask;
            }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Guid correlationId = Guid.NewGuid();
            await input.Send(new CompressedPayload(correlationId, expected), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            CompressedPayload actual = await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Equal(expected.Length, actual.Value.Length);
            Assert.Equal(expected, actual.Value);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private sealed record CompressedPayload(Guid CorrelationId, string Value);
}
