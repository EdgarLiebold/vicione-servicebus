using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class HostMetadataRoundTripTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-METADATA-TRANSPORT", "system-text-json-inmemory-roundtrip")]
    public async Task PublishedMessage_CarriesEveryHostFieldAcrossTheSerializationBoundaryAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"host-metadata-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            var consumed = new TaskCompletionSource<ConsumeContext<HostMetadataMessage>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            HostReceiveEndpointHandle endpoint = harness.Bus.ConnectReceiveEndpoint(configurator =>
                configurator.Handler<HostMetadataMessage>(context =>
                {
                    consumed.TrySetResult(context);
                    return Task.CompletedTask;
                }));
            await endpoint.Ready.WaitAsync(timeout, cancellationToken);

            try
            {
                await harness.Bus.PublishAsync(new HostMetadataMessage("roundtrip"), cancellationToken);
                HostInfo actual = (await consumed.Task.WaitAsync(timeout, cancellationToken)).Host;
                HostInfo expected = HostMetadataCache.Host;

                Assert.Equal(expected.MachineName, actual.MachineName);
                Assert.Equal(expected.ProcessName, actual.ProcessName);
                Assert.Equal(expected.ProcessId, actual.ProcessId);
                Assert.Equal(expected.Assembly, actual.Assembly);
                Assert.Equal(expected.AssemblyVersion, actual.AssemblyVersion);
                Assert.Equal(expected.FrameworkVersion, actual.FrameworkVersion);
                Assert.Equal(expected.ViciOneServiceBusVersion, actual.ViciOneServiceBusVersion);
                Assert.Equal(expected.OperatingSystemVersion, actual.OperatingSystemVersion);
            }
            finally
            {
                await endpoint.StopAsync(cancellationToken);
            }
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private sealed record HostMetadataMessage(string Value);
}
