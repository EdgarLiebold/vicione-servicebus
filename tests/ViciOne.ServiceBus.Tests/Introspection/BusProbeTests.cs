using ViciOne.ServiceBus.Introspection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Introspection;

public sealed class BusProbeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-PROBE-ENDPOINTS", "configured-and-dynamic-addresses")]
    public async Task Probe_ReportsEveryConfiguredReceiveEndpointAddressExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HostReceiveEndpointHandle? dynamicEndpoint = null;

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            dynamicEndpoint = harness.Bus.ConnectReceiveEndpoint(
                $"probe-{NewId.NextGuid():N}",
                configurator => configurator.Handler<ProbeMessage>(_ => Task.CompletedTask));
            ReceiveEndpointReady dynamicReady = await dynamicEndpoint.Ready.WaitAsync(timeout, cancellationToken);

            Uri[] addresses = ReadReceiveEndpointAddresses(harness.Bus.GetProbeResult(cancellationToken));

            Assert.Equal(3, addresses.Length);
            Assert.Equal(3, addresses.Distinct().Count());
            Assert.Contains(harness.InputQueueAddress, addresses);
            Assert.Contains(dynamicReady.InputAddress, addresses);
        }
        finally
        {
            if (dynamicEndpoint is not null)
                await dynamicEndpoint.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-PROBE-ENDPOINTS", "stopped-dynamic-endpoint-removed")]
    public async Task Probe_OmitsADynamicEndpointAfterItsHandleStopsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            HostReceiveEndpointHandle dynamicEndpoint = harness.Bus.ConnectReceiveEndpoint(
                $"probe-removed-{NewId.NextGuid():N}",
                configurator => configurator.Handler<ProbeMessage>(_ => Task.CompletedTask));
            Uri removedAddress = (await dynamicEndpoint.Ready.WaitAsync(timeout, cancellationToken)).InputAddress;
            await dynamicEndpoint.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

            Uri[] addresses = ReadReceiveEndpointAddresses(harness.Bus.GetProbeResult(cancellationToken));

            Assert.Equal(2, addresses.Length);
            Assert.Equal(2, addresses.Distinct().Count());
            Assert.Contains(harness.InputQueueAddress, addresses);
            Assert.DoesNotContain(removedAddress, addresses);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static Uri[] ReadReceiveEndpointAddresses(ProbeResult result)
    {
        IDictionary<string, object> bus = GetScope(result.Results, "bus");
        IDictionary<string, object> host = GetScope(bus, "host");
        object endpointValue = Assert.Contains("receiveEndpoint", host);
        IEnumerable<IDictionary<string, object>> endpoints = endpointValue switch
        {
            IDictionary<string, object> single => [single],
            IEnumerable<IDictionary<string, object>> multiple => multiple,
            _ => throw new Xunit.Sdk.XunitException(
                $"The receiveEndpoint probe node has unsupported type '{endpointValue.GetType()}'."),
        };

        return endpoints
            .Select(endpoint => GetScope(endpoint, "receiveTransport"))
            .Select(transport => Assert.IsType<Uri>(Assert.Contains("address", transport)))
            .OrderBy(address => address.AbsoluteUri, StringComparer.Ordinal)
            .ToArray();
    }

    private static IDictionary<string, object> GetScope(IDictionary<string, object> parent, string key) =>
        Assert.IsAssignableFrom<IDictionary<string, object>>(Assert.Contains(key, parent));

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"bus-probe-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record ProbeMessage(string Value);
}
