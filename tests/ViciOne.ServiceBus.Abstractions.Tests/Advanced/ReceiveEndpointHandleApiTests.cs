using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced;

public sealed class ReceiveEndpointHandleApiTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-ENDPOINT-HANDLE-API", "canonical-interface-shape")]
    public void ReceiveEndpointHandle_UsesCanonicalInterfaceShape()
    {
        Type handleType = typeof(IReceiveEndpointHandle);

        Assert.True(handleType.IsPublic);
        Assert.True(handleType.IsInterface);
        Assert.Null(handleType.Assembly.GetType("ViciOne.ServiceBus.Advanced.ReceiveEndpointHandle", throwOnError: false));

        PropertyInfo ready = Assert.Single(handleType.GetProperties(BindingFlags.Instance | BindingFlags.Public));
        Assert.Equal(nameof(IReceiveEndpointHandle.Ready), ready.Name);
        Assert.Equal(typeof(Task<ReceiveEndpointReady>), ready.PropertyType);

        MethodInfo stop = Assert.Single(
            handleType.GetMethods(BindingFlags.Instance | BindingFlags.Public),
            static method => !method.IsSpecialName);
        Assert.Equal(nameof(IReceiveEndpointHandle.StopAsync), stop.Name);
        Assert.Equal(typeof(Task), stop.ReturnType);

        ParameterInfo cancellationToken = Assert.Single(stop.GetParameters());
        Assert.Equal("cancellationToken", cancellationToken.Name);
        Assert.Equal(typeof(CancellationToken), cancellationToken.ParameterType);
        Assert.True(cancellationToken.IsOptional);
    }
}
