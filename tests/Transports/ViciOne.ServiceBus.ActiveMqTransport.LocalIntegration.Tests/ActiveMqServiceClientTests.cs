using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

public sealed class ActiveMqServiceClientTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-SERVICE-INSTANCE", "registered-service-instance-completes-default-request-routing")]
    public async Task RegisteredServiceInstance_ConnectsAndCompletesRequest(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "service-instance");
        string serviceEndpoint = fixture.Name("service");
        var handled = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(
                configurator,
                host =>
                {
                    if (flavor == ActiveMqBroker.OpenWireFlavor)
                        host.TransportOptions(new Dictionary<string, string> { ["nms.useCompression"] = "true" });
                });
            var options = new ServiceInstanceOptions()
                .SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);
            configurator.ServiceInstance(options, instance =>
            {
                instance.ReceiveEndpoint(serviceEndpoint, endpoint => endpoint.Handler<ServiceRequest>(context =>
                {
                    handled.TrySetResult(context.Message.CorrelationId);
                    return context.RespondAsync(new ServiceResponse(context.Message.CorrelationId, context.Message.Target));
                }));
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IRequestClient<ServiceRequest> client = bus.CreateRequestClient<ServiceRequest>(
                RequestTimeout.After(ms: checked((int)fixture.OperationTimeout.TotalMilliseconds)));
            Guid correlationId = Guid.NewGuid();
            Response<ServiceResponse> response = await client.GetResponse<ServiceResponse>(
                    new ServiceRequest(correlationId, "Bogey"),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(correlationId, await handled.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal("Bogey", response.Message.Target);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private sealed record ServiceRequest(Guid CorrelationId, string Target);
    private sealed record ServiceResponse(Guid CorrelationId, string Target);
}
