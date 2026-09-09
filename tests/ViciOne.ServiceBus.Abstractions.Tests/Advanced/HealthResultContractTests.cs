using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced;

public sealed class HealthResultContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-RESULT", "immutable-endpoint-snapshot")]
    public void BusHealthResult_CapturesAnImmutableEndpointSnapshot()
    {
        IReceiveEndpoint endpoint = CreateEndpoint(new Uri("loopback://localhost/input"));
        EndpointHealthResult endpointResult = EndpointHealthResult.Healthy(endpoint, "ready");
        var source = new Dictionary<string, EndpointHealthResult>
        {
            [endpoint.InputAddress.AbsoluteUri] = endpointResult
        };

        BusHealthResult result = BusHealthResult.Healthy("all endpoints ready", source);
        source.Clear();

        KeyValuePair<string, EndpointHealthResult> captured = Assert.Single(result.Endpoints);
        Assert.Equal(endpoint.InputAddress.AbsoluteUri, captured.Key);
        Assert.Equal(endpointResult, captured.Value);
        Assert.Equal(BusHealthStatus.Healthy, result.Status);
        Assert.Equal("all endpoints ready", result.Description);
        Assert.Null(result.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-RESULT", "required-bus-result-inputs")]
    public void BusHealthResultFactories_RejectEachMissingRequiredInput()
    {
        var endpoints = new Dictionary<string, EndpointHealthResult>();

        Assert.Equal(
            "description",
            Assert.Throws<ArgumentNullException>(() => BusHealthResult.Healthy(null!, endpoints)).ParamName);
        Assert.Equal(
            "endpoints",
            Assert.Throws<ArgumentNullException>(() => BusHealthResult.Healthy("ready", null!)).ParamName);
        Assert.Equal(
            "description",
            Assert.Throws<ArgumentNullException>(() => BusHealthResult.Degraded(null!, null, endpoints)).ParamName);
        Assert.Equal(
            "endpoints",
            Assert.Throws<ArgumentNullException>(() => BusHealthResult.Unhealthy("faulted", null, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-RESULT", "complete-endpoint-result")]
    public void EndpointHealthResultFactories_PreserveTheCompleteEndpointObservation()
    {
        var address = new Uri("loopback://localhost/faulted");
        IReceiveEndpoint endpoint = CreateEndpoint(address);
        var failure = new InvalidOperationException("transport unavailable");

        EndpointHealthResult result = EndpointHealthResult.Unhealthy(endpoint, "not connected", failure);

        Assert.Equal(BusHealthStatus.Unhealthy, result.Status);
        Assert.Same(endpoint, result.ReceiveEndpoint);
        Assert.Equal(address, result.InputAddress);
        Assert.Equal("not connected", result.Description);
        Assert.Same(failure, result.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-RESULT", "required-endpoint")]
    public void EndpointHealthResultFactories_RejectAMissingEndpoint()
    {
        Assert.Equal(
            "receiveEndpoint",
            Assert.Throws<ArgumentNullException>(() => EndpointHealthResult.Healthy(null!, "ready")).ParamName);
        Assert.Equal(
            "receiveEndpoint",
            Assert.Throws<ArgumentNullException>(() => EndpointHealthResult.Degraded(null!, "recovering")).ParamName);
        Assert.Equal(
            "receiveEndpoint",
            Assert.Throws<ArgumentNullException>(() => EndpointHealthResult.Unhealthy(null!, "faulted", null)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-RESULT", "required-endpoint-address")]
    public void EndpointHealthResultFactories_RejectAnEndpointWithoutAnInputAddress()
    {
        IReceiveEndpoint endpoint = CreateEndpoint(null!);

        Assert.Equal(
            "receiveEndpoint",
            Assert.Throws<ArgumentException>(() => EndpointHealthResult.Healthy(endpoint, "ready")).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-RESULT", "property-based-api")]
    public void HealthResultTypes_ExposeStateThroughPropertiesInsteadOfPublicFields()
    {
        Assert.Empty(typeof(BusHealthResult).GetFields(BindingFlags.Instance | BindingFlags.Public));
        Assert.Empty(typeof(EndpointHealthResult).GetFields(BindingFlags.Instance | BindingFlags.Public));

        Assert.Equal(
            ["Description", "Endpoints", "Exception", "Status"],
            typeof(BusHealthResult).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            ["Description", "Exception", "InputAddress", "ReceiveEndpoint", "Status"],
            typeof(EndpointHealthResult).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }

    private static IReceiveEndpoint CreateEndpoint(Uri inputAddress)
    {
        IReceiveEndpoint endpoint = DispatchProxy.Create<IReceiveEndpoint, ReceiveEndpointProxy>();
        ((ReceiveEndpointProxy)(object)endpoint).InputAddress = inputAddress;
        return endpoint;
    }

    private class ReceiveEndpointProxy : DispatchProxy
    {
        public required Uri InputAddress { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == $"get_{nameof(IReceiveEndpoint.InputAddress)}")
                return InputAddress;

            throw new NotSupportedException($"Unexpected receive endpoint member: {targetMethod?.Name}");
        }
    }
}
