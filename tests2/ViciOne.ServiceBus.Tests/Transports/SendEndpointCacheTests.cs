using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class SendEndpointCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-ENDPOINT-CACHE-IDENTITY", "concurrent-cold-and-warm-addresses")]
    public async Task ConcurrentColdAndWarmLookups_PreserveIdentityPerAddress()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"send-endpoint-cache-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();

        try
        {
            await harness.Start(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            var firstAddress = new Uri(harness.BaseAddress, "queue-a");
            var secondAddress = new Uri(harness.BaseAddress, "queue-b");

            ISendEndpoint[] cold = await Task.WhenAll(
                    harness.Bus.GetSendEndpoint(firstAddress),
                    harness.Bus.GetSendEndpoint(secondAddress))
                .WaitAsync(operationTimeout, cancellationToken);
            ISendEndpoint[] warm = await Task.WhenAll(
                    harness.Bus.GetSendEndpoint(firstAddress),
                    harness.Bus.GetSendEndpoint(secondAddress))
                .WaitAsync(operationTimeout, cancellationToken);

            Assert.Same(cold[0], warm[0]);
            Assert.Same(cold[1], warm[1]);
            Assert.NotSame(cold[0], cold[1]);
        }
        finally
        {
            await harness.Stop().WaitAsync(operationTimeout, cancellationToken);
        }
    }
}
