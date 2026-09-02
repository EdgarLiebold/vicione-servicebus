using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryBusLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "start-request-stop-and-restart")]
    public async Task StartRequestStopAndRestart_PreservesExactRequestRoutingAcrossBothRuns()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
            configuration.ReceiveEndpoint($"lifecycle-{NewId.NextGuid():N}", endpoint =>
                endpoint.Handler<LifecycleRequest>(context =>
                    context.RespondAsync(new LifecycleResponse(context.Message.Id, context.Message.Run)))));

        LifecycleResponse first = await RunOnce(bus, new LifecycleRequest(NewId.NextGuid(), 1), timeout, cancellationToken);
        LifecycleResponse second = await RunOnce(bus, new LifecycleRequest(NewId.NextGuid(), 2), timeout, cancellationToken);

        Assert.Equal(1, first.Run);
        Assert.Equal(2, second.Run);
        Assert.NotEqual(first.Id, second.Id);
    }

    private static async Task<LifecycleResponse> RunOnce(
        IBusControl bus,
        LifecycleRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Response<LifecycleResponse> response = await bus.CreateRequestClient<LifecycleRequest>()
                .GetResponse<LifecycleResponse>(request, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Assert.Equal(request.Id, response.Message.Id);
            Assert.Equal(request.Run, response.Message.Run);
            return response.Message;
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record LifecycleRequest(Guid Id, int Run);
    public sealed record LifecycleResponse(Guid Id, int Run);
}
